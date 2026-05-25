using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode.Transports.UTP;
using TMPro;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private GameObject matchingPanel;
    [SerializeField] private GameObject ModeSelectPanel;
    [SerializeField] private GameObject roomCodePanel;
    [SerializeField] private TMP_Text roomCodeText;
    [SerializeField] private Toggle allowRandomMatchToggle;

    [SerializeField] private GameObject inputRoomCodePanel;
    [SerializeField] private TMP_InputField inputRoomCodeField;

    private Lobby hostedLobby;

    private bool isMatching = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (matchingPanel != null)
            matchingPanel.SetActive(false);
        if (ModeSelectPanel != null)
            ModeSelectPanel.SetActive(false);
        if (roomCodePanel != null)
            roomCodePanel.SetActive(false);

        if (inputRoomCodePanel != null)
            inputRoomCodePanel.SetActive(false);

        if (allowRandomMatchToggle != null)
            allowRandomMatchToggle.isOn = false;
    }

    private async void Start()
    {
        //서버/클라이언트 연결 이벤트 구독
        if(NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }

        await InitUgsAsync();
    }

    private async Task InitUgsAsync()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        Debug.Log($"UGS Ready: {AuthenticationService.Instance.PlayerId}");
    }

    private void OnDestroy()
    {
        //서버/클라이언트 연결 해제
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    public void OnPlayButtonClick()
    {
        ModeSelectPanel.SetActive(true);
    }

    //모드 선택 판넬

    //방 만들기(호스트로 방 참가하도록)
    public async void OnCreateButtonClick()
    {
        if (!CanStartMatching()) return;

        isMatching = true;

        bool allowRandomMatch = allowRandomMatchToggle != null && allowRandomMatchToggle.isOn;
        await CreateRoomAsync(allowRandomMatch, showRoomCode: true);
    }

    public async void OnRoomCodePanelExitButtonClick()
    {
        if (roomCodePanel != null)
            roomCodePanel.SetActive(false);

        if (hostedLobby != null)
        {
            try
            {
                await LobbyService.Instance.DeleteLobbyAsync(hostedLobby.Id);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"로비 삭제 실패: {e.Message}");
            }

            hostedLobby = null;
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        isMatching = false;
    }

    private async Task CreateRoomAsync(bool allowRandomMatch, bool showRoomCode)
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            var options = new CreateLobbyOptions
            {
                IsPrivate = !allowRandomMatch,
                Data = new Dictionary<string, DataObject>
            {
                { "joinCode", new DataObject(DataObject.VisibilityOptions.Member, joinCode) }
            }
            };

            hostedLobby = await LobbyService.Instance.CreateLobbyAsync("PingPongRoom", 2, options);
            StartCoroutine(HeartbeatLobby());

            if (showRoomCode)
            {
                if (roomCodePanel != null)
                    roomCodePanel.SetActive(true);

                if (roomCodeText != null)
                    roomCodeText.text = joinCode;
            }

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));

            NetworkManager.Singleton.StartHost();

            Debug.Log($"방 생성 완료. 코드: {joinCode}, 랜덤매칭 허용: {allowRandomMatch}");
        }
        catch (Exception e)
        {
            Debug.LogError($"방 생성 실패: {e.Message}");
            isMatching = false;
        }
    }

    //방 join (클라이언트로 방 참가하도록)
    public void OnJoinButtonClick()
    {
        if (isMatching) return;

        if (inputRoomCodePanel != null)
            inputRoomCodePanel.SetActive(true);
    }

    public async void OnJoinRoomConfirmButtonClick()
    {
        if (!CanStartMatching()) return;
        
        if (inputRoomCodeField == null)
        {
            Debug.LogWarning("방 코드 입력 필드 연결안됨.");
            return;
        }

        string joinCode = inputRoomCodeField.text.Trim();

        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogWarning("방 코드를 입력하세요.");
            return;
        }

        isMatching = true;

        await JoinByCodeAsync(joinCode);
    }

    public void OnJoinRoomSubmit(string _)
    {
        OnJoinRoomConfirmButtonClick();
    }

    public void OnInputRoomCodePanelExitButtonClick()
    {
        if (inputRoomCodePanel != null)
            inputRoomCodePanel.SetActive(false);
    }


    private async Task JoinByCodeAsync(string joinCode)
    {
        try
        {
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));

            NetworkManager.Singleton.StartClient();

            Debug.Log($"코드로 방 참가: {joinCode}");
        }
        catch (Exception e)
        {
            Debug.LogError($"방 참가 실패: {e.Message}");
            isMatching = false;
        }
    }

    private async Task<bool> TryQuickJoinAsync()
    {
        try
        {
            Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync();

            string joinCode = lobby.Data["joinCode"].Value;
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));

            NetworkManager.Singleton.StartClient();

            Debug.Log("Room joined");
            return true;
        }
        catch (LobbyServiceException)
        {
            Debug.Log("랜덤 매칭 가능한 방 없음");
            return false;
        }
    }

    //게임모드 나가기 버튼
    public void OnGameModeExitButtonClick()
    {
        ModeSelectPanel.SetActive(false);
    }

    public async void OnMatchingButtonClick()
    {
        if (!CanStartMatching()) return;

        isMatching = true;
        matchingPanel.SetActive(true);
        ModeSelectPanel.SetActive(false);

        while (isMatching && NetworkManager.Singleton != null && !NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            bool joined = await TryQuickJoinAsync();

            if (joined)
            {
                return;
            }

            await Task.Delay(1000);
        }
    }

    public void OnCancelButtonClick()
    {
        if (matchingPanel != null)
            matchingPanel.SetActive(false);

        if (roomCodePanel != null)
            roomCodePanel.SetActive(false);

        if (inputRoomCodePanel != null)
            inputRoomCodePanel.SetActive(false);

        isMatching = false;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
    }


    //게임종료 버튼
    public void OnQuitButtonClick()
    {
        
    }     

    private bool CanStartMatching()
    {
        if(isMatching)
        {
            Debug.Log("이미 매칭 중입니다.");
            return false;
        }

        if(NetworkManager.Singleton == null)
        {
            Debug.Log("네트워크 매니저가 없습니다.");
            return false;
        }

        if(NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
        {
            Debug.Log("이미 네트워크에 연결된 상태입니다.");
            return false;
        }

        return true;
    }

    private void MatchingCancel()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
            Debug.Log("매칭을 취소합니다.");
        }

    }


    // 모드 선택 판넬 끝


    private void OnClientConnected(ulong id)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            int playerCount = NetworkManager.Singleton.ConnectedClients.Count;
            Debug.Log($"현재 접속 인원: {playerCount}");

            //플레이어 2명 매칭 시 게임 씬으로
            
            if(playerCount == 2)
            {
                Debug.Log("매칭 성공. 게임 씬으로 이동합니다.");
                NetworkManager.Singleton.SceneManager.LoadScene("MainScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
                
            }
        }
    }

    private IEnumerator ConnectionTimeoutCheck(float waitTime)
    {
        Debug.Log($"{waitTime}초 동안 서버를 찾습니다...");
        yield return new WaitForSeconds(waitTime);

        if(!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            Debug.Log("서버를 찾지 못해 호스트합니다.");
            NetworkManager.Singleton.Shutdown();
            yield return new WaitUntil(() => !NetworkManager.Singleton.ShutdownInProgress);
            NetworkManager.Singleton.StartHost();
        }
    }

    private IEnumerator HeartbeatLobby()
    {
        while (hostedLobby != null)
        {
            LobbyService.Instance.SendHeartbeatPingAsync(hostedLobby.Id);
            yield return new WaitForSeconds(15f);
        }
    }

}

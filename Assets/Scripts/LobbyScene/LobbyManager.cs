using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private GameObject matchingPanel;
    [SerializeField] private GameObject ModeSelectPanel;
    [SerializeField] private GameObject CreateRoomPanel;
    [SerializeField] private GameObject JoinRoomPanel;

    private bool isMatching = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (matchingPanel != null)
            matchingPanel.SetActive(false);
        if (ModeSelectPanel != null)
            ModeSelectPanel.SetActive(false);
        if (CreateRoomPanel != null)
            CreateRoomPanel.SetActive(false);
        if (JoinRoomPanel != null)
            JoinRoomPanel.SetActive(false);
    }

    private void Start()
    {
        //서버/클라이언트 연결 이벤트 구독
        if(NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
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

    //방 만들기 판넬
    public void OnCreateButtonClick()
    {
        if (isMatching)
        {
            //매칭 중 방만들기 안되도록
            Debug.Log("매칭중에는 방을 만들 수 없습니다.");
            return;
        }

        CreateRoomPanel.SetActive(true);
    }


    //방 join 판넬
    public void OnJoinButtonClick()
    {
        JoinRoomPanel.SetActive(true);
    }


    //게임모드 나가기 버튼
    public void OnGameModeExitButtonClick()
    {
        ModeSelectPanel.SetActive(false);
    }

    public void OnMatchingButtonClick()
    {
        if(isMatching)
        {
            //플레이 버튼 중복으로 눌렀을 때 오류 방지
            Debug.Log("이미 매칭중입니다.");
            return;
        }
        isMatching = true;
        matchingPanel.SetActive(true);
        ModeSelectPanel.SetActive(false);
        MatchingStart();
    }

    private void MatchingStart()
    {
        NetworkManager.Singleton.StartClient();
        Debug.Log("서버를 찾고있습니다.");

        StartCoroutine(ConnectionTimeoutCheck(3.0f));
        
    }   
    public void OnCancelButtonClick()
    {
        matchingPanel.SetActive(false);
        isMatching = false;
        MatchingCancel();
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
    
}

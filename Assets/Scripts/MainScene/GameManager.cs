using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    [Header("Player Prefab")]
    [SerializeField] private GameObject playerPrefab;

    [Header("Restart Vote UI")]
    public GameObject backToLobbyButton;
    public TextMeshProUGUI restartVoteText;

    private HashSet<ulong> restartVotes = new HashSet<ulong>();
    
    public NetworkVariable<int> restartVoteCount = new NetworkVariable<int>(
    0,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
    );

    [Header("Timer Text")]
    public NetworkVariable<float> TotalGameTime = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public TMP_Text timerText;
    private bool isGameStarted = false;
    

    [Header("Score Text")]
    public TextMeshProUGUI left;
    public TextMeshProUGUI right;
    [Header("Winner Text")]
    public GameObject leftWin;
    public GameObject rightWin;

    [Header("Max Score")]
    public int maxScore;

    [Header("Ball Prefab")]
    public GameObject ballPrefab;

    [Header("UI Objects")]
    public GameObject restartButton;

    private bool ballSpawned;

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [Header("Network Variables")]
    public NetworkVariable<int> scoreLeft = new NetworkVariable<int>(0);
    public NetworkVariable<int> scoreRight = new NetworkVariable<int>(0);
    public NetworkVariable<bool> isGameOver = new NetworkVariable<bool>(false);
    
    ItemController itemCntrl;

    void Update()
    {
        if (IsServer)
        {
            // 플레이어가 2명 이상 모이면 게임 시작
            if (!isGameStarted && NetworkManager.Singleton.ConnectedClients.Count >= 2)
            {
                isGameStarted = true;
            }

            // 게임이 시작되었다면 서버에서 시간을 계속 더함
            if (isGameStarted)
            {
                TotalGameTime.Value += Time.deltaTime;
            }
        }

        // 3. (서버/클라이언트 공통) UI에 시간 표시
        if (timerText != null)
        {
            
            float time = TotalGameTime.Value;
            int minutes = Mathf.FloorToInt(time / 60);
            int seconds = Mathf.FloorToInt(time % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
    
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            SpawnPlayersIfNeeded();
            SpawnBallIfNeeded();

            isGameOver.Value = false;
            restartVoteCount.Value = 0;
            restartVotes.Clear();
        }

        // 기본 Ui 초기화
        if (restartButton != null)
        {
            restartButton.SetActive(false);
        }

        if (backToLobbyButton != null)
        {
            backToLobbyButton.SetActive(false);
        }

        if (restartVoteText != null)
        {
            restartVoteText.gameObject.SetActive(false);
            restartVoteText.text = "Restart Vote\n0/2";
        }

        // 점수 ui 초기화
        left.text = scoreLeft.Value.ToString();
        right.text = scoreRight.Value.ToString();

        if (IsServer)
        {
            isGameOver.Value = false;
            restartVotes.Clear();
            restartVoteCount.Value = 0;
        } 

        leftWin.SetActive(false);
        rightWin.SetActive(false);

        // 점수 변경
        scoreLeft.OnValueChanged += (oldValue, newValue) =>
        {
            left.text = newValue.ToString();
            if (IsServer) ClearLeftoverItem();
            if (newValue >= maxScore)
            {
                leftWin.SetActive(true);
                if (IsServer)
                {
                    HandleGameOver();
                }
            }
        };
        scoreRight.OnValueChanged += (oldValue, newValue) =>
        {
            right.text = newValue.ToString();
            if (IsServer) ClearLeftoverItem();
            if (newValue >= maxScore)
            {
                rightWin.SetActive(true);
                
                if (IsServer) {
                    HandleGameOver();
                }
            }
        };

        // 다시하기 투표 수 동기화
        restartVoteCount.OnValueChanged += (oldValue, newValue) =>
        {
            UpdateRestartVoteText();
        };

    }

    private void HandleGameOver()
    {
        if (!IsServer) return;

        if (isGameOver.Value) return;

        isGameOver.Value = true;

        restartVotes.Clear();
        restartVoteCount.Value = 0;

        ShowGameOverButtonsClientRpc();
    }

    private void SpawnPlayersIfNeeded()
    {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject != null)
                continue;

            Vector3 spawnPos = clientId == NetworkManager.ServerClientId
                ? new Vector3(-8f, 0f, 0f)
                : new Vector3(8f, 0f, 0f);

            GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, false);
        }
    }

    private void SpawnBallIfNeeded()
    {
        if (ballSpawned) return;

        ballSpawned = true;

        GameObject ball = Instantiate(ballPrefab, Vector3.zero, Quaternion.identity);
        ball.GetComponent<NetworkObject>().Spawn();
    }

    void SpawnBall()
    {
        GameObject ball = Instantiate(ballPrefab, Vector3.zero, Quaternion.identity);
        ball.GetComponent<NetworkObject>().Spawn();
    }

    public void OnBackToLobbyButtonClicked()
    {
        RequestBackToLobbyRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestBackToLobbyRpc()
    {
        BackToLobbyClientRpc();
    }

    [ClientRpc]
    private void BackToLobbyClientRpc()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
    }


    public void OnRestartButtonClicked()
    {
        RequestRestartRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestRestartRpc(RpcParams rpcParams = default)
    {
        if (!isGameOver.Value) return;

        ulong senderId = rpcParams.Receive.SenderClientId;

        if (restartVotes.Add(senderId))
        {
            restartVoteCount.Value = restartVotes.Count;
        }

        int playerCount = NetworkManager.Singleton.ConnectedClients.Count;

        if (restartVotes.Count >= playerCount)
        {
            RestartGame();
        }
    }

    private void RestartGame()
    {
        scoreLeft.Value = 0;
        scoreRight.Value = 0;
        isGameOver.Value = false;

        restartVotes.Clear();
        restartVoteCount.Value = 0;

        BallController reball = FindFirstObjectByType<BallController>();
        if (reball != null)
        {
            reball.Invoke("StartBall", 2f);
        }

        ResetUIClientRpc();
    }

    [ClientRpc]
    void ResetUIClientRpc()
    {
        leftWin.SetActive(false);
        rightWin.SetActive(false);
        
        if (restartButton != null)
            restartButton.SetActive(false);

        if (backToLobbyButton != null)
            backToLobbyButton.SetActive(false);

        if (restartVoteText != null)
            restartVoteText.gameObject.SetActive(false);

    }

    [ClientRpc]
    private void ShowGameOverButtonsClientRpc()
    {
        if (restartButton != null)
        {
            restartButton.SetActive(true);
        }

        if (backToLobbyButton != null)
        {
            backToLobbyButton.SetActive(true);
        }

        if (restartVoteText != null)
        {
            restartVoteText.gameObject.SetActive(true);
            UpdateRestartVoteText();
        }
    }

    private void UpdateRestartVoteText()
    {
        if (restartVoteText == null) return;

        int playerCount = 2;

        if (NetworkManager.Singleton != null)
        {
            playerCount = NetworkManager.Singleton.ConnectedClients.Count;
        }

        restartVoteText.text = $"Restart Vote\n{restartVoteCount.Value}/{playerCount}";
    }

    public void ClearLeftoverItem()
    {
        if (!IsServer) return;

        TotalGameTime.Value = 0;

        ItemObject activeItem = FindFirstObjectByType<ItemObject>();
        if (activeItem != null)
        {
            activeItem.GetComponent<NetworkObject>().Despawn();
        }

        if (itemCntrl == null)
        {
            itemCntrl = FindFirstObjectByType<ItemController>();
        }

        if (itemCntrl != null)
        {
            itemCntrl.ResetAllItemEffects();
            itemCntrl.ResetSpawnTimer();
        }
        
    }
}

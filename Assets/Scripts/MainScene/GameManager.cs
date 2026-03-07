using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SocialPlatforms.Impl;
using Unity.VisualScripting;

public class GameManager : NetworkBehaviour
{
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
        if (restartButton != null)
        {
            restartButton.SetActive(false);
        }
        left.text = scoreLeft.Value.ToString();
        right.text = scoreRight.Value.ToString();
        if(IsServer) isGameOver.Value = false;
        leftWin.SetActive(false);
        rightWin.SetActive(false);


        scoreLeft.OnValueChanged += (oldValue, newValue) =>
        {
            left.text = newValue.ToString();
            if (IsServer) ClearLeftoverItem();
            if (newValue >= maxScore)
            {
                leftWin.SetActive(true);
                if (IsServer)
                {
                    isGameOver.Value = true;
                    restartButton.SetActive(true);
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
                    isGameOver.Value = true;
                    restartButton.SetActive(true);
                }
            }
        };

        if (IsServer)
        {
            
            NetworkManager.Singleton.OnClientConnectedCallback += (id) =>
            {
                if(NetworkManager.Singleton.ConnectedClients.Count == 2)
                {
                    SpawnBall();
                }
            };
        }
    }
    void SpawnBall()
    {
        GameObject ball = Instantiate(ballPrefab, Vector3.zero, Quaternion.identity);
        ball.GetComponent<NetworkObject>().Spawn();
    }

    public void OnRestartButtonClicked()
    {
        if(!IsServer) return;

        scoreLeft.Value = 0;
        scoreRight.Value = 0;
        isGameOver.Value = false;
        BallController reball = FindFirstObjectByType<BallController>();
        if(reball != null)
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
        restartButton.SetActive(false);
        
    }

    public void ClearLeftoverItem()
    {
        if (!IsServer) return;

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
            itemCntrl.ResetSpawnTimer();
        }
        
    }
}

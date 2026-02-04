using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SocialPlatforms.Impl;
using Unity.VisualScripting;

public class GameManager : NetworkBehaviour
{
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

    public NetworkVariable<int> scoreLeft = new NetworkVariable<int>(0);
    public NetworkVariable<int> scoreRight = new NetworkVariable<int>(0);

    public NetworkVariable<bool> isGameOver = new NetworkVariable<bool>(false);

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
            if(newValue >= maxScore)
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
}

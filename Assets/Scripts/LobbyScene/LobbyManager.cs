using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private GameObject matchingPanel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (matchingPanel != null)
            matchingPanel.SetActive(false);
    }

    public void OnPlayButtonClick()
    {
        matchingPanel.SetActive(true);
        MatchingStart();
    }

    public void OnCancelButtonClick()
    {
        matchingPanel.SetActive(false);
        MatchingCancel();
    }

    private void MatchingStart()
    {
        if (NetworkManager.Singleton.StartClient())
        {
            Debug.Log("기존 방에 접속을 시도합니다");
        }
        else
        {
            Debug.Log("방이 없어 호스트로 시작합니다.");
            NetworkManager.Singleton.StartHost();
        }
    }

    private void MatchingCancel()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
            Debug.Log("매칭을 취소합니다.");
        }
    }
}

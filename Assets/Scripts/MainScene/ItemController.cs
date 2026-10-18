using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;


public class ItemController : NetworkBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public enum Itemtype {PlayerSpeedUp, BallSpeedUp, BallSizeDown};
    public GameObject itemPrefab;
    public float multipleItem;

    BallController ballCntrl;

    Coroutine spawnCoroutine;
    Coroutine speedCoroutine;
    Coroutine sizeCoroutine;
    Coroutine paddleCoroutine;

    GameManager manager;
    
    [SerializeField] private float playerSpeedMultiplier = 1.5f;
    [SerializeField] private float itemEffectDuration = 5f;

    [Header("Spawn Range")]
    [SerializeField] private Vector2 minPos;
    [SerializeField] private Vector2 maxPos;

    [Header ("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip playerSpeedUpClip;
    [SerializeField] private AudioClip speedUpClip;
    [SerializeField] private AudioClip ballSizeDownClip;

    public float itemDespawnTimer = 10f;
    public float itemSpawnInterval = 10f;
    public bool nextItem = true;
    List<Itemtype> itemlist = new List<Itemtype> { Itemtype.PlayerSpeedUp, Itemtype.BallSpeedUp, Itemtype.BallSizeDown };


    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += CheckPlayerAndStart;
            
            if (NetworkManager.Singleton.ConnectedClients.Count >= 2)
            {
                ResetSpawnTimer();
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if(IsServer && NetworkManager.Singleton != null){
            NetworkManager.Singleton.OnClientConnectedCallback -= CheckPlayerAndStart;
        }
    }



    private void CheckPlayerAndStart(ulong clientId)
    {
        if(NetworkManager.Singleton.ConnectedClients.Count >= 2 && IsServer)
        {
            ResetSpawnTimer();
        }
    }

    //nextItem이 true일 때 10초 후 아이템 스폰
    IEnumerator SpawnLoop()
    {
        Debug.Log("SpawnLoop 시작");
        while (true)
        {
            yield return new WaitUntil(()=>nextItem == true);
            Debug.Log("아이템 스폰 대기");

            nextItem = false;
            yield return new WaitForSeconds(itemSpawnInterval);
            Debug.Log("아이템 생성");
            NetworkObject spawnedItem = SetItemtype();
            
            yield return new WaitForSeconds(itemDespawnTimer);
            if(spawnedItem != null && spawnedItem.IsSpawned)
            {
                spawnedItem.Despawn();
            }
            nextItem = true;
        }
    }
    


    //아이템 랜덤으로 선택
    private NetworkObject SetItemtype()
    {
        int randomIndex = Random.Range(0, itemlist.Count);
        Itemtype selectedItem = itemlist[randomIndex];
        return SpawnItem(selectedItem);
    }

    private NetworkObject SpawnItem(Itemtype item)
    {
        Debug.Log("아이템 Instantiate");
        GameObject obj = Instantiate(itemPrefab, GetRandomPosition(), Quaternion.identity);

        ItemObject itemObject = obj.GetComponent<ItemObject>();
        itemObject.myType = item;

        NetworkObject netObj = obj.GetComponent<NetworkObject>();
        netObj.Spawn();

        itemObject.SetSprite(item);

        return netObj;
    }

    //아이템 스폰 위치 선정
    private Vector3 GetRandomPosition()
    {
        float x = Random.Range(minPos.x, maxPos.x);
        float y = Random.Range(minPos.y, maxPos.y);

        return new Vector3(x, y, 0f);
    }

    //아이템 먹었으면 다음 아이템 나올 수 있도록
    /* 
    리셋스폰타이머로 쓸모 없어짐
    public void OnItemEaten()
    {
        if(!IsServer) return;
        nextItem = true;
    }

*/
    //아이템 타입에 따라서 효과 적용
    public void ApplyItem(ulong Id, Itemtype type)
    {
        if(!IsServer) return;

        PlayItemSfxClientRpc(type);

        switch (type)
        {
            case Itemtype.PlayerSpeedUp:
                Debug.Log("paddlesizeup입니다");
                paddleCoroutine = StartCoroutine(PlayerSpeedUp(Id));
                break;

            case Itemtype.BallSizeDown:
                Debug.Log("ballsizedown입니다");
                sizeCoroutine = StartCoroutine(BallSizeDown());
                break;

            case Itemtype.BallSpeedUp:
                Debug.Log("speedup입니다");
                speedCoroutine = StartCoroutine(SpeedUp());
                break;
        }
    }

    [ClientRpc]
    private void PlayItemSfxClientRpc(Itemtype type)
    {
        if(audioSource == null) return;
        switch (type)
        {
            case Itemtype.PlayerSpeedUp:
                if (playerSpeedUpClip != null)
                    audioSource.PlayOneShot(playerSpeedUpClip);
                break;

            case Itemtype.BallSpeedUp:
                if (speedUpClip != null)
                    audioSource.PlayOneShot(speedUpClip);
                break;

            case Itemtype.BallSizeDown:
                if (ballSizeDownClip != null)
                    audioSource.PlayOneShot(ballSizeDownClip);
                break;
        }
    }

    IEnumerator PlayerSpeedUp(ulong targetId)
    {
        ChangePlayerSpeedClientRpc(targetId, playerSpeedMultiplier);

        yield return new WaitForSeconds(itemEffectDuration);

        ChangePlayerSpeedClientRpc(targetId, 1f);
    }

    [ClientRpc]
    private void ChangePlayerSpeedClientRpc(ulong targetId, float speedMultiplier)
    {
        foreach (NetworkObject obj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
        {
            if (!obj.IsPlayerObject) continue;

            if (obj.OwnerClientId == targetId)
            {
                PlayerController player = obj.GetComponent<PlayerController>();
                if (player != null)
                {
                    player.SetSpeedMultiplier(speedMultiplier);
                }

                return;
            }
        }
    }


    IEnumerator BallSizeDown()
    {
        BallController ball = FindFirstObjectByType<BallController>();
        if (ball == null) yield break;
        ball.SetBallSize(0.5f);
        yield return new WaitForSeconds(5f);

        ball.ResetBallSize();
    }

    IEnumerator SpeedUp()
    {
        BallController ball = FindFirstObjectByType<BallController>();
        if (ball == null) yield break;

        ball.SetSpeedMultiplier(1.5f);

        yield return new WaitForSeconds(5f);

        ball.SetSpeedMultiplier(1f);

    }

    public void ResetSpawnTimer()
    {
        if(!IsServer) return;

        if(spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }

        nextItem = true;
        spawnCoroutine = StartCoroutine(SpawnLoop());
    }
    
    public void ResetAllItemEffects()
    {
        if(!IsServer) return;

        if (speedCoroutine != null){
            StopCoroutine(speedCoroutine);
        }
        if (sizeCoroutine != null){
            StopCoroutine(sizeCoroutine);
        }
        if (paddleCoroutine != null){
            StopCoroutine(paddleCoroutine);
        }

        BallController ball = FindFirstObjectByType<BallController>();
        if(ball != null)
        {
            ball.speed = ball.originSpeed;
            ball.SetSpeedMultiplier(1f);
            ball.ResetBallSize();
        }
        ResetAllPaddlesClientRpc();
        Debug.Log("아이템 효과 초기화 완료");
    }

    [ClientRpc]
    private void ResetAllPaddlesClientRpc()
    {
        foreach(NetworkObject obj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
        {
            if(!obj.IsPlayerObject) continue;

            PlayerController player = obj.GetComponent<PlayerController>();
            if (player != null)
            {
                player.SetSpeedMultiplier(1f);
            }
        }
    }
}

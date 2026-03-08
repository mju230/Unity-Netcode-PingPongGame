//TODO 매칭시스템 구현



using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;
using Unity.VisualScripting;
using System.Security.Cryptography;


public class ItemController : NetworkBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public enum Itemtype {PaddleSizeUp, SpeedUp, BallSizeDown};
    public GameObject itemPrefab;
    public float multipleItem;

    BallController ballCntrl;

    Coroutine spawnCoroutine;
    Coroutine speedCoroutine;
    Coroutine sizeCoroutine;
    Coroutine paddleCoroutine;

    GameManager manager;
    [Header("Spawn Range")]
    [SerializeField] private Vector2 minPos;
    [SerializeField] private Vector2 maxPos;

    

    public bool nextItem = true;
    List<Itemtype> itemlist = new List<Itemtype> { Itemtype.PaddleSizeUp, Itemtype.SpeedUp, Itemtype.BallSizeDown };


    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += CheckPlayerAndStart;
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
            yield return new WaitForSeconds(10f);
            Debug.Log("아이템 생성");
            SetItemtype();
            
        }
    }
    

    //아이템 랜덤으로 선택
    private void SetItemtype()
    {
        int randomIndex = Random.Range(0, itemlist.Count);
        Itemtype selectedItem = itemlist[randomIndex];
        SpawnItem(selectedItem);
    }

    private void SpawnItem(Itemtype item)
    {
        Debug.Log("아이템 Instantiate");
        GameObject obj = Instantiate(itemPrefab, GetRandomPosition(), Quaternion.identity);

        obj.GetComponent<ItemObject>().myType = item;
        obj.GetComponent<NetworkObject>().Spawn();
       
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

        switch (type)
        {
            case Itemtype.PaddleSizeUp:
                Debug.Log("paddlesizeup입니다");
                paddleCoroutine = StartCoroutine(PaddingSizeUp(Id));
                break;

            case Itemtype.BallSizeDown:
                Debug.Log("ballsizedown입니다");
                sizeCoroutine = StartCoroutine(BallSizeDown());
                break;

            case Itemtype.SpeedUp:
                Debug.Log("speedup입니다");
                speedCoroutine = StartCoroutine(SpeedUp());
                break;
        }
    }

    IEnumerator PaddingSizeUp(ulong targetId)
    {
        ChangePaddleSizeClientRpc(targetId, multipleItem);
        yield return new WaitForSeconds(5f);
        ChangePaddleSizeClientRpc(targetId, 1f);

    }

    [ClientRpc]
    private void ChangePaddleSizeClientRpc(ulong targetId, float sizeMultiplier)
    {
        foreach(NetworkObject obj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values){
            if(!obj.IsPlayerObject) continue;
            {
                if(obj.OwnerClientId == targetId){
                    obj.transform.localScale = new Vector3(1f, 3f * sizeMultiplier, 1f);
                    return;
                }
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

        Rigidbody2D rigid = ball.GetComponent<Rigidbody2D>();

        if(rigid == null) yield break;

        float originSpeed = ball.speed;
        ball.speed = originSpeed * 2f;

        rigid.linearVelocity = rigid.linearVelocity.normalized * ball.speed;

        yield return new WaitForSeconds(5f);

        ball.speed = originSpeed;
        rigid.linearVelocity *= rigid.linearVelocity.normalized * ball.speed;
        
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
            Rigidbody2D rigid = ball.GetComponent<Rigidbody2D>();

            ball.speed = ball.originSpeed;

            ball.ResetBallSize();

            if(rigid != null)
            {
                rigid.linearVelocity = rigid.linearVelocity.normalized * ball.originSpeed;
            }
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

            obj.transform.localScale = new Vector3(1f,3f,1f);
        }
    }
}

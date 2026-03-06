using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;
using Unity.VisualScripting;


public class ItemController : NetworkBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public enum Itemtype {PaddleSizeUp, SpeedUp, BallSizeDown};
    public GameObject itemPrefab;

    [Header("Spawn Range")]
    [SerializeField] private Vector2 minPos;
    [SerializeField] private Vector2 maxPos;

    private bool nextItem = true;
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
            nextItem = true;
            StartCoroutine(SpawnLoop());
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
    public void OnItemEaten()
    {
        if(!IsServer) return;
        nextItem = true;
    }

    //아이템 타입에 따라서 효과 적용
    public void ApplyItem(ulong Id, Itemtype type)
    {
        if(!IsServer) return;
        
    }

    IEnumerator PaddingSizeUp()
    {
        yield return new WaitForSeconds(5f);
    }
}

using Unity.Netcode;
using UnityEngine;

public class ItemObject : NetworkBehaviour
{

    public ItemController.Itemtype myType;
    GameManager manager;
    ItemController item;


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;

        if (collision.CompareTag("Ball"))
        {
                BallController ball = collision.GetComponent<BallController>();
                if(ball != null){

                    ulong HittedId = ball.LastHitClientId;

                    ItemController controller = FindFirstObjectByType<ItemController>();

                    if(controller != null){
                        controller.ApplyItem(HittedId, myType);
                        controller.ResetSpawnTimer();
                }
            }

            GetComponent<NetworkObject>().Despawn();
        }
    }
}

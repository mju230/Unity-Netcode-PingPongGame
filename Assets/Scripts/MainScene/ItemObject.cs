using Unity.Netcode;
using UnityEngine;

public class ItemObject : NetworkBehaviour
{

    public ItemController.Itemtype myType;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;

        if (collision.CompareTag("Ball"))
        {
            BallController ball = collision.GetComponent<BallController>();

            ItemController manager = FindFirstObjectByType<ItemController>();
            manager.ApplyItem(ball.LastHitClientId, myType);

            manager.OnItemEaten();

            GetComponent<NetworkObject>().Despawn();
        }
    }
}

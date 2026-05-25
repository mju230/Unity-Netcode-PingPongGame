using Unity.Netcode;
using UnityEngine;

public class ItemObject : NetworkBehaviour
{
    [SerializeField] private Sprite paddleSizeUpSprite;
    [SerializeField] private Sprite speedUpSprite;
    [SerializeField] private Sprite ballSizeDownSprite;

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

    public void SetSprite(ItemController.Itemtype type)
    {
        if (!IsServer) return;

        SetSpriteClientRpc(type);
    }

    [ClientRpc]
    private void SetSpriteClientRpc(ItemController.Itemtype type)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        switch (type)
        {
            case ItemController.Itemtype.PaddleSizeUp:
                sr.sprite = paddleSizeUpSprite;
                break;

            case ItemController.Itemtype.SpeedUp:
                sr.sprite = speedUpSprite;
                break;

            case ItemController.Itemtype.BallSizeDown:
                sr.sprite = ballSizeDownSprite;
                break;
        }

        sr.color = Color.white;
    }
}

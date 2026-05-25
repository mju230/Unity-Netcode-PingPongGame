using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerController : NetworkBehaviour
{

    public float speed = 6f;
    SpriteRenderer sprite;
    Collider2D paddleCollider;

    [Header("Movement Bounds")]
    [SerializeField] private float topBoundY = 4.75f;
    [SerializeField] private float bottomBoundY = -4.75f;
    

    void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        paddleCollider = GetComponent<Collider2D>();
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"Player Spawn / Owner:{OwnerClientId} / IsOwner:{IsOwner} / IsServer:{IsServer}");
        if (IsOwner)
        {
            if (OwnerClientId == NetworkManager.ServerClientId)
            {
                sprite.color = Color.blue;
                transform.position = new Vector3(-8f, 0f, 0f);
            }
            else
            {
                sprite.color = Color.red;
                transform.position = new Vector3(8f, 0f, 0f);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        Debug.Log($"Player Despawn / Owner:{OwnerClientId} / IsOwner:{IsOwner} / IsServer:{IsServer}");
    }
    
    void Update()
    {
        if(!IsOwner) return;

        float move = 0f;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
        {
            move = 1f;
        }
        else if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
        {
            move = -1f;
        }

        transform.Translate(0, move * speed * Time.deltaTime, 0);
        ClampToMovementBounds();

    }

    private void ClampToMovementBounds()
    {
        float halfHeight = 0f;

        if (paddleCollider != null)
        {
            halfHeight = paddleCollider.bounds.extents.y;
        }

        float minY = bottomBoundY + halfHeight;
        float maxY = topBoundY - halfHeight;

        Vector3 pos = transform.position;
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        transform.position = pos;
    }

}

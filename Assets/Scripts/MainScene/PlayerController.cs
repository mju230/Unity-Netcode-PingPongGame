using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerController : NetworkBehaviour
{

    public float speed = 6f;
    SpriteRenderer sprite;
    void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            if(OwnerClientId == 0){
                sprite.color = Color.blue;
                transform.position = new Vector3(-8f,0f,0f);
            }
            else{
                sprite.color = Color.red;
                transform.position = new Vector3(8f,0f,0f);
            }
        }
    }


    void Update()
    {
        if(!IsOwner) return;

        float move = 0f;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
        {
            if(transform.position.y >= 3.75) return;
            move = 1f;
        }
        else if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
        {
            if(transform.position.y <= -3.75) return;
            move = -1f;
        }

        transform.Translate(0, move * speed * Time.deltaTime, 0);

    }

}

using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerController : NetworkBehaviour
{

    public float speed = 6f;
    SpriteRenderer sprite;
    Collider2D paddleCollider;
    private float originSpeed;

    [SerializeField] private Transform paddleVisual;
    [SerializeField] private SpriteRenderer paddleVisualRenderer;
    [SerializeField] private GameObject youIndicator;
    [SerializeField] private float youIndicatorDuration = 2f;
    

    [Header("Movement Bounds")]
    [SerializeField] private float topBoundY = 4.75f;
    [SerializeField] private float bottomBoundY = -4.75f;



    void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        paddleCollider = GetComponent<Collider2D>();
        originSpeed = speed;
    }

    public override void OnNetworkSpawn()
    {
        
        Debug.Log($"Player Spawn / Owner:{OwnerClientId} / IsOwner:{IsOwner} / IsServer:{IsServer}");

        bool isHostPlayer = OwnerClientId == NetworkManager.ServerClientId;
        if(paddleVisualRenderer != null) paddleVisualRenderer.color = isHostPlayer ? Color.red : Color.blue;

        if (sprite != null)
        {
            sprite.enabled = false;
        }

        if (youIndicator != null)
        {
            youIndicator.SetActive(false);

            if (IsOwner)
            {
                StartCoroutine(ShowYouIndicatorRoutine());
            }
        }

        transform.position = isHostPlayer ? new Vector3(-7.2f, 0f, 0f): new Vector3(7.2f, 0f, 0f);
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

    public void SetSpeedMultiplier(float multiplier)
    {
        speed = originSpeed * multiplier;
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

    // YouIndicator 관련
    private IEnumerator ShowYouIndicatorRoutine()
    {
        youIndicator.SetActive(true);

        yield return new WaitForSeconds(youIndicatorDuration);

        youIndicator.SetActive(false);
    }



    //헬퍼 함수들

    private bool IsHostPlayer()
    {
        return OwnerClientId == NetworkManager.ServerClientId;
    }

    public void PlayHitFlash()
    {
        StartCoroutine(HitFeedbackRoutine());
    }

    private IEnumerator HitFeedbackRoutine()
    {
        Vector3 originScale = paddleVisual.localScale;
        paddleVisual.localScale = originScale * 1.5f;
        yield return new WaitForSeconds(0.08f);
        paddleVisual.localScale = originScale;
    }

}

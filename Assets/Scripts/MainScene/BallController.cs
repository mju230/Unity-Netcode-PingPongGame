using UnityEngine;
using Unity.Netcode;


public class BallController : NetworkBehaviour
{
    Rigidbody2D rigid;
    GameManager score;
    public ulong LastHitClientId;
    public Vector3 originalBallSize = Vector3.one;
    public GameObject hitEffectPrefab;

    [Header ("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip paddleHitClip;
    [SerializeField] private AudioClip wallHitClip;
    private enum BallSfxType
    {
        PaddleHit,
        WallHit
    }

    

    [Header("Speed Scaling")]
    public float speed = 5f;
    public float maxSpeed = 10f; // 게임 시간에 따른 최대 속도
    public float maxFinalSpeed = 13f; // 아이템 포함 최종 속도 제한
    public float speedIncreasePerSecond = 0.15f;
    private float speedMultiplier = 1f;
    public float originSpeed;

    [Header("Out Of Bounds Recovery")]
    [SerializeField] private Vector2 outOfBoundsLimit = new Vector2(12f, 7f);
    [SerializeField] private float respawnDelay = 1f;
    private bool isRespawning;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();

        originSpeed = speed;
        originalBallSize = transform.localScale;
    }

    void Update()
    {
        if (!IsServer) return;

        if (rigid == null) return;

        if (score != null && score.isGameOver.Value) return;

        if (IsOutOfBounds())
        {
            RespawnBallAtCenter();
            return;
        }

        if (rigid.linearVelocity.sqrMagnitude <= 0.01f) return;

        speed = Mathf.Min(speed + speedIncreasePerSecond * Time.deltaTime, maxSpeed);
        rigid.linearVelocity = rigid.linearVelocity.normalized * GetFinalSpeed();
    }

    public float GetFinalSpeed()
    {
        return Mathf.Min(speed * speedMultiplier, maxFinalSpeed);
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = multiplier;

        if (rigid != null && rigid.linearVelocity.sqrMagnitude > 0.01f)
        {
            rigid.linearVelocity = rigid.linearVelocity.normalized * GetFinalSpeed();
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            score = FindFirstObjectByType<GameManager>();
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            Invoke("StartBall", 2f);
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.Count == 2)
        {
            Invoke("StartBall", 2f);
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
        }
    }

    void StartBall()
    {
        isRespawning = false;

        float x = Random.Range(0,2) == 0 ? -1 : 1;
        float y = Random.Range(0, 2) == 0 ? -1 : 1;

        Vector2 dir = new Vector2(x, y).normalized;
        rigid.linearVelocity = dir * GetFinalSpeed();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if(!IsServer) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            PlayBallSfxClientRpc(BallSfxType.PaddleHit);
            BounceFromPaddle(collision);
            return;
        }

        if (collision.gameObject.CompareTag("Wall"))
        {
            PlayBallSfxClientRpc(BallSfxType.WallHit);
            Vector2 reflectDir = Vector2.Reflect(rigid.linearVelocity, collision.contacts[0].normal);
            rigid.linearVelocity = reflectDir.normalized * GetFinalSpeed();
        }
    }

    [ClientRpc]
    private void PlayBallSfxClientRpc(BallSfxType type)
    {
        switch (type)
        {
            case BallSfxType.PaddleHit:
                PlaySfx(paddleHitClip);
                break;

            case BallSfxType.WallHit:
                PlaySfx(wallHitClip);
                break;
        }
    }

    private void PlaySfx(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;

        audioSource.PlayOneShot(clip);
    }

    private void BounceFromPaddle(Collision2D collision)
    {
        Transform paddle = collision.transform;

        float paddleHeight = collision.collider.bounds.size.y;
        float hitOffset = transform.position.y - paddle.position.y;

        float normalizedOffset = hitOffset / (paddleHeight * 0.5f);
        normalizedOffset = Mathf.Clamp(normalizedOffset, -0.75f, 0.75f);

        if (Mathf.Abs(normalizedOffset) < 0.2f)
        {
            float randomSign = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            normalizedOffset = 0.2f * randomSign;
        }

        float xDirection = transform.position.x < paddle.position.x ? -1f : 1f;

        Vector2 bounceDir = new Vector2(xDirection, normalizedOffset).normalized;

        rigid.linearVelocity = bounceDir * GetFinalSpeed();

        var networkObj = collision.gameObject.GetComponent<NetworkObject>();
        if (networkObj != null)
        {
            LastHitClientId = networkObj.OwnerClientId;
            Vector2 hitPoint = collision.GetContact(0).point;
            PlayPaddleHitEffectClientRpc(LastHitClientId, hitPoint);
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if(!IsServer) return;
        
        if (collision.CompareTag("PlayerGoal1"))
        {
            score.scoreRight.Value += 1;
            speed = originSpeed;
            speedMultiplier = 1f;
            isRespawning = true;
            rigid.linearVelocity = Vector2.zero;
            transform.position = new Vector3(0,0,0);
            if(!score.isGameOver.Value)
                Invoke("StartBall", 2f);
            
        }
        else if (collision.CompareTag("PlayerGoal2"))
        {
            score.scoreLeft.Value += 1;
            speed = originSpeed;
            speedMultiplier = 1f;
            isRespawning = true;
            rigid.linearVelocity = Vector2.zero;
            transform.position = new Vector3(0, 0, 0);
            if(!score.isGameOver.Value)
                Invoke("StartBall", 2f);
            
        }
    }
    public void SetBallSize(float multiplier)
    {
        transform.localScale = originalBallSize * multiplier;
    }

    public void ResetBallSize()
    {
        transform.localScale = originalBallSize;    
    }

    private bool IsOutOfBounds()
    {
        Vector3 pos = transform.position;
        return Mathf.Abs(pos.x) > outOfBoundsLimit.x || Mathf.Abs(pos.y) > outOfBoundsLimit.y;
    }

    private void RespawnBallAtCenter()
    {
        if (isRespawning) return;

        isRespawning = true;
        speed = originSpeed;
        speedMultiplier = 1f;
        rigid.linearVelocity = Vector2.zero;
        transform.position = Vector3.zero;

        if (score == null || !score.isGameOver.Value)
        {
            Invoke(nameof(StartBall), respawnDelay);
        }
    }
    
    //공 쳤을때 패들 이펙트 

    [ClientRpc]
    private void PlayPaddleHitEffectClientRpc(ulong hitterClientId, Vector2 hitPoint)
    {
        Instantiate(hitEffectPrefab, hitPoint, Quaternion.identity);
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        foreach (PlayerController player in players)
        {
            if (player.OwnerClientId == hitterClientId)
            {
                player.PlayHitFlash();
                return;
            }
        }
    }
}

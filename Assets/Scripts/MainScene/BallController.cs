using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem.LowLevel;


public class BallController : NetworkBehaviour
{
    Rigidbody2D rigid;
    public float speed = 5f;
    GameManager score;
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        
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
        float x = Random.Range(0,2) == 0 ? -1 : 1;
        float y = Random.Range(0, 2) == 0 ? -1 : 1;
        rigid.linearVelocity = new Vector2(x * speed, y * speed);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if(!IsServer) return;

        if (collision.gameObject.CompareTag("Wall") || collision.gameObject.CompareTag("Player"))
        {
            Vector2 reflectDir = Vector2.Reflect(rigid.linearVelocity, collision.contacts[0].normal);
            rigid.linearVelocity = reflectDir.normalized * speed;
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if(!IsServer) return;
        
        if (collision.CompareTag("PlayerGoal1"))
        {
            score.scoreRight.Value += 1;
            rigid.linearVelocity = Vector2.zero;
            transform.position = new Vector3(0,0,0);
            if(!score.isGameOver.Value)
                Invoke("StartBall", 2f);
            
        }
        else if (collision.CompareTag("PlayerGoal2"))
        {
            score.scoreLeft.Value += 1;
            rigid.linearVelocity = Vector2.zero;
            transform.position = new Vector3(0, 0, 0);
            if(!score.isGameOver.Value)
                Invoke("StartBall", 2f);
            
        }
    }
}

using Unity.Netcode;
using UnityEngine;

public class NetworkManagerUI : MonoBehaviour
{
    void OnGUI()
    {
        if (NetworkManager.Singleton == null) return;
        
        GUILayout.BeginArea(new Rect(10,10,300,300));

        if(!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if(GUILayout.Button("Host (방 만들기)"))
            {
                NetworkManager.Singleton.StartHost();
            }

            if(GUILayout.Button("Client (참가하기)"))
            {
                NetworkManager.Singleton.StartClient();
            }
        }
        GUILayout.EndArea();
    }
}

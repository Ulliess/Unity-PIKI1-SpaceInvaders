using UnityEngine;
using Unity.Netcode;

public class NetworkBootstrap : MonoBehaviour
{
    void OnGUI()
    {
        if (NetworkManager.Singleton == null) return;
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer) return;

        GUIStyle style = new GUIStyle(GUI.skin.button);
        style.fontSize = 20;

        if (GUI.Button(new Rect(10, 10, 200, 50), "Start Host", style))
            NetworkManager.Singleton.StartHost();

        if (GUI.Button(new Rect(10, 70, 200, 50), "Start Client", style))
            NetworkManager.Singleton.StartClient();
    }
}
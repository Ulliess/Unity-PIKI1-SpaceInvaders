using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Temporary debug UI for testing multiplayer.
/// Shows Start Host / Start Client buttons on screen.
/// Delete this when real lobby UI is ready.
/// </summary>
public class NetworkDebugUI : MonoBehaviour
{
    private void OnGUI()
    {
        // Don't show buttons if already connected
        if (NetworkManager.Singleton == null) return;
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 300));

        if (GUILayout.Button("Start Host", GUILayout.Height(50)))
        {
            NetworkManager.Singleton.StartHost();
        }

        if (GUILayout.Button("Start Client", GUILayout.Height(50)))
        {
            NetworkManager.Singleton.StartClient();
        }

        GUILayout.EndArea();
    }
}

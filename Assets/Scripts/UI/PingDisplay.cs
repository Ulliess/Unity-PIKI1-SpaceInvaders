using Unity.Netcode;
using UnityEngine;
using TMPro;

public class PingDisplay : MonoBehaviour
{
    [Tooltip("Ссылка на текстовый компонент UI")]
    public TextMeshProUGUI pingText;
    
    private float updateTimer = 0f;

    void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            if (pingText != null) pingText.text = "";
            return;
        }

        updateTimer += Time.deltaTime;
        
        // Обновляем пинг раз в секунду, чтобы текст не мельтешил
        if (updateTimer >= 1f) 
        {
            updateTimer = 0f;

            if (NetworkManager.Singleton.IsServer)
            {
                // Если мы хост, пинг до самого себя равен 0
                if (pingText != null) pingText.text = "Ping: 0 ms";
            }
            else
            {
                // Если мы клиент, получаем пинг (Round Trip Time) до сервера
                ulong rtt = NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.ServerClientId);
                if (pingText != null) pingText.text = $"Ping: {rtt} ms";
            }
        }
    }
}

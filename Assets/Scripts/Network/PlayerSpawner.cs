using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Кладётся на сцену Game. Когда сцена загружается,
/// сервер спавнит игрока для каждого подключённого клиента.
/// </summary>
public class PlayerSpawner : NetworkBehaviour
{
    public GameObject playerPrefab;

    [Header("Spawn Positions")]
    public Vector3 hostSpawnPosition = new Vector3(-3f, -3f, 0f);
    public Vector3 clientSpawnPosition = new Vector3(3f, -3f, 0f);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Спавним игрока для каждого уже подключённого клиента
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                SpawnPlayer(client.ClientId);
            }

            // Подписываемся на новые подключения
            NetworkManager.Singleton.OnClientConnectedCallback += SpawnPlayer;
        }
    }

    private void SpawnPlayer(ulong clientId)
    {
        // Не спавнить если у клиента уже есть игрок
        if (NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject != null)
            return;

        // Определяем позицию: хост слева, клиент справа
        Vector3 spawnPos = clientId == NetworkManager.ServerClientId ? hostSpawnPosition : clientSpawnPosition;

        GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        NetworkObject netObj = player.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(clientId);
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= SpawnPlayer;
        }
    }
}

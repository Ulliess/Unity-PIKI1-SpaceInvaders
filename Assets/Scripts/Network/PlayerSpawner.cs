using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Кладётся на сцену Game. Когда сцена загружается,
/// сервер спавнит игрока для каждого подключённого клиента.
/// </summary>
public class PlayerSpawner : NetworkBehaviour
{
    public GameObject playerPrefab;
    [Header("Ship Prefabs (0=Circle, 1=Rect, 2=Triangle)")]
    public GameObject[] shipPrefabs;

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

        // Достаём выбранный корабль из ReadyRoomManager
        int shipIndex = 0;
        if (ReadyRoomManager.Instance != null && ReadyRoomManager.Instance.playerShipChoices.ContainsKey(clientId))
        {
            shipIndex = ReadyRoomManager.Instance.playerShipChoices[clientId];
        }

        // Защита от выхода за пределы массива
        GameObject prefabToSpawn = (shipPrefabs != null && shipPrefabs.Length > 0) 
            ? shipPrefabs[shipIndex % shipPrefabs.Length] 
            : playerPrefab; // Fallback на старую переменную, если массив пуст

        GameObject player = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
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

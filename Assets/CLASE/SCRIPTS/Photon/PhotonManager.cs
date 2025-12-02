using Fusion;
using UnityEngine;
using Fusion.Sockets;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.Events;

public class PhotonManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkPrefabRef prefab;
    [SerializeField] private NetworkRunner runner;
    [SerializeField] NetworkSceneManagerDefault sceneManager;
    [SerializeField] private Transform[] spawnPoint;

    private Dictionary<PlayerRef, NetworkObject> players = new Dictionary<PlayerRef, NetworkObject>();
    private int connectionRetries = 0;
    private const int MAX_RETRIES = 5;
    private const float RETRY_DELAY = 2f;

    [SerializeField] UnityEvent onPlayerJoinedToGame;

    #region Metodos de Photon
    public void OnPlayerJoined(NetworkRunner runner_, PlayerRef player)
    {
        if (runner_.IsServer)
        {
            int randomSpawn = UnityEngine.Random.Range(0, spawnPoint.Length);
            NetworkObject networkPlayer = runner_.Spawn(prefab, spawnPoint[randomSpawn].position, spawnPoint[randomSpawn].rotation, player);
            players.Add(player, networkPlayer);
        }
        onPlayerJoinedToGame?.Invoke();
    }

    public void OnPlayerLeft(NetworkRunner runner_, PlayerRef player)
    {
        if (players.TryGetValue(player, out NetworkObject networkPlayer))
        {
            runner_.Despawn(networkPlayer);
            players.Remove(player);
            Debug.Log($"Player {player} desconectado y removido");
        }
    }

    public void OnInput(NetworkRunner runner_, NetworkInput input)
    {
        // Verificar que InputManager exista antes de usarlo
        if (InputManager.Instance == null)
        {
            Debug.LogWarning("InputManager.Instance es null en OnInput");
            return;
        }

        NetworkInputData data = new NetworkInputData()
        {
            move = InputManager.Instance.GetMoveInput(),
            look = InputManager.Instance.GetMouseDelta(),
            isRunning = InputManager.Instance.WasRunInputPressed(),
            yRotation = Camera.main != null ? Camera.main.transform.eulerAngles.y : 0f,
            shoot = InputManager.Instance.ShootInputPressed()
        };

        input.Set(data);
    }

    public void OnInputMissing(NetworkRunner runner_, PlayerRef player, NetworkInput input) { }

    public void OnConnectedToServer(NetworkRunner runner_)
    {
        Debug.Log("✅ Conectado al servidor exitosamente");
        connectionRetries = 0;
    }

    public void OnConnectFailed(NetworkRunner runner_, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"❌ Error de conexión: {reason}");

        if ((runner_.Mode & SimulationModes.Client) == SimulationModes.Client && connectionRetries < MAX_RETRIES)
        {
            connectionRetries++;
            Debug.Log($"🔄 Reintentando conexión ({connectionRetries}/{MAX_RETRIES})...");
            StartCoroutine(RetryConnection());
        }
    }

    public void OnConnectRequest(NetworkRunner runner_, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }

    public void OnCustomAuthenticationResponse(NetworkRunner runner_, Dictionary<string, object> data) { }

    public void OnDisconnectedFromServer(NetworkRunner runner_, NetDisconnectReason reason)
    {
        Debug.Log($"⚠️ Desconectado del servidor: {reason}");

        if ((runner_.Mode & SimulationModes.Client) == SimulationModes.Client && connectionRetries < MAX_RETRIES)
        {
            connectionRetries++;
            Debug.Log($"🔄 Reintentando conexión ({connectionRetries}/{MAX_RETRIES})...");
            StartCoroutine(RetryConnection());
        }
    }

    public void OnHostMigration(NetworkRunner runner_, HostMigrationToken hostMigrationToken) { }

    public void OnObjectEnterAOI(NetworkRunner runner_, NetworkObject obj, PlayerRef player) { }

    public void OnObjectExitAOI(NetworkRunner runner_, NetworkObject obj, PlayerRef player) { }

    public void OnReliableDataProgress(NetworkRunner runner_, PlayerRef player, ReliableKey key, float progress) { }

    public void OnReliableDataReceived(NetworkRunner runner_, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }

    public void OnSceneLoadDone(NetworkRunner runner_)
    {
        Debug.Log("✅ Escena cargada completamente");
    }

    public void OnSceneLoadStart(NetworkRunner runner_)
    {
        Debug.Log("⏳ Comenzando carga de escena");
    }

    public void OnSessionListUpdated(NetworkRunner runner_, List<SessionInfo> sessionList) { }

    public void OnShutdown(NetworkRunner runner_, ShutdownReason shutdownReason)
    {
        Debug.Log($"🛑 Runner apagado: {shutdownReason}");
        players.Clear();
    }

    public void OnUserSimulationMessage(NetworkRunner runner_, SimulationMessagePtr message) { }
    #endregion

    private System.Collections.IEnumerator RetryConnection()
    {
        yield return new WaitForSeconds(RETRY_DELAY);
        StartGame(GameMode.Client);
    }

    private async void StartGame(GameMode mode)
    {
        // Limpiar runner anterior si existe
        if (runner != null && runner.IsRunning)
        {
            Debug.LogWarning("⚠️ NetworkRunner ya está en ejecución, cerrando...");
            await runner.Shutdown();
            await System.Threading.Tasks.Task.Delay(100);
        }

        // Crear nuevo runner si no existe
        if (runner == null)
        {
            runner = gameObject.AddComponent<NetworkRunner>();
        }

        runner.AddCallbacks(this);
        runner.ProvideInput = true;

        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var sceneInfo = new NetworkSceneInfo();

        if (scene.IsValid)
        {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
        }

        try
        {
            var startGameArgs = new StartGameArgs()
            {
                GameMode = mode,
                SessionName = "#01",
                Scene = scene,
                CustomLobbyName = "EmilioMomasoz",
                SceneManager = sceneManager,
                // FIX PARA "Array too small"
                PlayerCount = 2, // Número máximo de jugadores
            };

            var result = await runner.StartGame(startGameArgs);

            if (result.Ok)
            {
                Debug.Log($"✅ Juego iniciado correctamente como {mode}");
            }
            else
            {
                Debug.LogError($"❌ Error al iniciar juego: {result.ShutdownReason}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Excepción al iniciar juego: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public void StartGameAsHost()
    {
        Debug.Log("🎮 Intentando iniciar como Host...");
        connectionRetries = 0;
        StartGame(GameMode.Host);
    }

    public void StartGameAsClient()
    {
        Debug.Log("👥 Intentando iniciar como Client...");
        connectionRetries = 0;
        StartGame(GameMode.Client);
    }

    private void OnDestroy()
    {
        if (runner != null && runner.IsRunning)
        {
            runner.Shutdown();
        }
    }
}
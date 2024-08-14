using Unity.Netcode;
using UnityEngine;

public class ServerManager : MonoBehaviour
{
    private NetworkManager m_NetworkManager;
    public static ServerManager Instance;

    void Awake()
    {
        if (Instance && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;

        m_NetworkManager = GetComponent<NetworkManager>();
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));

        if (!m_NetworkManager.IsClient && !m_NetworkManager.IsServer)
            StartButtons();
        else
            StatusLabels();

        GUILayout.EndArea();
    }

    void StartButtons()
    {
        if (GUILayout.Button("Host")) m_NetworkManager.StartHost();
        if (GUILayout.Button("Client")) m_NetworkManager.StartClient();
        if (GUILayout.Button("Server")) m_NetworkManager.StartServer();
    }

    void StatusLabels()
    {
        var mode = m_NetworkManager.IsHost ?
            "Host" : m_NetworkManager.IsServer ? "Server" : "Client";

        GUILayout.Label("Transport: " +
            m_NetworkManager.NetworkConfig.NetworkTransport.GetType().Name);
        GUILayout.Label("Mode: " + mode);

        if (m_NetworkManager.IsHost || m_NetworkManager.IsServer)
            if (GUILayout.Button("Shutdown Server")) ShutdownServer();

        if (m_NetworkManager.IsClient && !m_NetworkManager.IsHost)
            if (GUILayout.Button("Disconnect Client")) ShutdownClient();
    }

    public void ShutdownServer()
    {
        CameraManager.Instance.EnableDefaultCamera();
        m_NetworkManager.Shutdown();
    }

    void ShutdownClient()
    {
        CameraManager.Instance.EnableDefaultCamera();
        m_NetworkManager.Shutdown();
    }
}

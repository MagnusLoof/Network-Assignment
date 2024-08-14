using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class CameraManager : NetworkBehaviour
{
    public Camera defaultCamera;
    public static CameraManager Instance;

    private void Awake()
    {
        if (Instance && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    void Start()
    {
        if (IsClient)
            defaultCamera.gameObject.SetActive(true);
    }

    public override void OnNetworkSpawn()
    {
        defaultCamera.gameObject.SetActive(false);
    }

    public void EnableDefaultCamera()
    {
        defaultCamera.gameObject.SetActive(true);
    }
}
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Checkpoint : NetworkBehaviour
{
    private BoxCollider boxCollider;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        var player = other.GetComponent<Player>();
        if (player != null && player.IsOwner)
            UpdateCheckpointServerRpc(player.OwnerClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateCheckpointServerRpc(ulong clientId)
    {
        var player = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<Player>();
        player.UpdateCheckpoint(transform.position);
    }
}

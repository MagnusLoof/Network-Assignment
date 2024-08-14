using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Goal : NetworkBehaviour
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
            GameOverServerRpc(player.OwnerClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void GameOverServerRpc(ulong clientId)
    {
        ChatManager.Instance.BroadcastMessageClientRpc("Game over player " + clientId + " won!");
        StartCoroutine(Shutdown());
    }

    IEnumerator Shutdown()
    {
        yield return new WaitForSeconds(5);
        ServerManager.Instance.ShutdownServer();
        yield return null;
    }
}

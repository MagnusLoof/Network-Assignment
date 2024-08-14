using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

public class ChatManager : NetworkBehaviour
{
    public TMP_InputField chatInputField;
    public GameObject chatPanel;
    public GameObject chatMessagePrefab;
    public List<TextMeshProUGUI> messages = new List<TextMeshProUGUI>();
    private float totalHeight = 0f;

    public static ChatManager Instance;

    private void Awake()
    {
        if (Instance && Instance != this) 
            Destroy(gameObject);
        else 
            Instance = this;
    }

    public void OpenChat(InputAction disableIA)
    {
        disableIA.Disable();
        chatInputField.gameObject.SetActive(true);
        chatInputField.ActivateInputField();
    }

    public void CloseChat(InputAction enableIA)
    {
        chatInputField.gameObject.SetActive(false);
        chatInputField.text = string.Empty;
        enableIA.Enable();
    }

    public void SendChat(InputAction enableIA)
    {
        string message = chatInputField.text;
        if (string.IsNullOrWhiteSpace(message))
        {
            CloseChat(enableIA);
            return;
        }

        SendMessageToServerRpc(message);
        chatInputField.text = string.Empty;
        chatInputField.gameObject.SetActive(false);
        enableIA.Enable();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendMessageToServerRpc(string message, ServerRpcParams rpcParams = default)
    {
        BroadcastMessageClientRpc(message);
    }

    [ClientRpc]
    public void BroadcastMessageClientRpc(string message)
    {
        GameObject newMessageObject = Instantiate(chatMessagePrefab, chatPanel.transform);
        TextMeshProUGUI newMessageText = newMessageObject.GetComponent<TextMeshProUGUI>();
        newMessageText.text = message;

        messages.Add(newMessageText);

        // Update the total height with the height of the new message
        // Check if the total height greater to or equal the height of the chat panel
        totalHeight += newMessageText.GetComponent<RectTransform>().rect.height;

        if (totalHeight >= chatPanel.GetComponent<RectTransform>().rect.height)
            RemoveOldestMessage();
    }

    private void RemoveOldestMessage()
    {
        if (messages.Count <= 0)
            return;

        TextMeshProUGUI oldestMessage = messages[0];
        messages.RemoveAt(0);
        totalHeight -= oldestMessage.GetComponent<RectTransform>().rect.height;
        Destroy(oldestMessage.gameObject);
    }
}

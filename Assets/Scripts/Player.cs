using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public class Player : NetworkBehaviour
{
    public float moveSpeed = 10.0f;
    public Camera playerCamera;

    public PlayerControls playerControls;
    private InputAction movementInput;

    private Rigidbody rb;
    private Vector3 currentCheckpoint;

    // Netcode general
    NetworkTimer timer;
    const float serverTickRate = 128.0f;
    const int bufferSize = 1024;

    // Netcode client
    CircularBuffer<StatePayload> clientStateBuffer;
    CircularBuffer<InputPayload> clientInputBuffer;
    StatePayload lastServerState;
    StatePayload lastProcessedState;
    
    bool respawning = false;

    // Netcode server
    CircularBuffer<StatePayload> serverStateBuffer;
    Queue<InputPayload> serverInputQueue;
    float reconciliationThreshold = 10.0f;

    public struct InputPayload : INetworkSerializable
    {
        public int tick;
        public Vector3 inputVector;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref tick);
            serializer.SerializeValue(ref inputVector);
        }
    }

    public struct StatePayload : INetworkSerializable
    {
        public int tick;
        public Vector3 position;
        public Vector3 velocity;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref tick);
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref velocity);
        }
    }

    public void UpdateCheckpoint(Vector3 checkpointPosition)
    {
        currentCheckpoint = checkpointPosition;
        currentCheckpoint.y += 0.5f;
    }

    private void Awake()
    {
        playerControls = new PlayerControls();

        timer = new NetworkTimer(serverTickRate);
        clientStateBuffer = new CircularBuffer<StatePayload>(bufferSize);
        clientInputBuffer = new CircularBuffer<InputPayload>(bufferSize);
        serverStateBuffer = new CircularBuffer<StatePayload>(bufferSize);
        serverInputQueue = new Queue<InputPayload>();
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentCheckpoint = new Vector3(0, 0.5f, 0);
    }

    private void OnEnable()
    {
        movementInput = playerControls.Player.Movement;
        movementInput.Enable();

        playerControls.UI.Enable();
        if(ChatManager.Instance != null)
        {
            playerControls.UI.OpenChat.performed += ctx => ChatManager.Instance.OpenChat(movementInput);
            playerControls.UI.CloseChat.performed += ctx => ChatManager.Instance.CloseChat(movementInput);
            playerControls.UI.SendChat.performed += ctx => ChatManager.Instance.SendChat(movementInput);
        }  
    }

    private void OnDisable()
    {
        movementInput.Disable();
        playerControls.UI.Disable();

        if(ChatManager.Instance != null)
        {
            playerControls.UI.OpenChat.performed -= ctx => ChatManager.Instance.OpenChat(movementInput);
            playerControls.UI.CloseChat.performed -= ctx => ChatManager.Instance.CloseChat(movementInput);
            playerControls.UI.SendChat.performed -= ctx => ChatManager.Instance.SendChat(movementInput);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
            playerCamera.gameObject.SetActive(true);
    }

    private void Update()
    {
        timer.Update(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        while (timer.ShouldTick() && !respawning)
        {
            HandleClientTick();
            HandleServerTick();
        }
    }

    void HandleServerTick()
    {
        if (!IsServer) return;
        var bufferIndex = -1;
        while(serverInputQueue.Count > 0)
        {
            InputPayload inputPayload = serverInputQueue.Dequeue();

            bufferIndex = inputPayload.tick % bufferSize;

            StatePayload statePayload = ProcessMovement(inputPayload);
            serverStateBuffer.Add(statePayload, bufferIndex);
        }

        if (bufferIndex == -1) return;
        SendToClientRpc(serverStateBuffer.Get(bufferIndex));
    }

    [ClientRpc]
    void SendToClientRpc(StatePayload statePayload)
    {
        if (!IsOwner) return;
        lastServerState = statePayload;
    }

    void HandleClientTick()
    {
        if(!IsClient || !IsOwner) return;

        var currentTick = timer.currentTick;
        var bufferIndex = currentTick % bufferSize;

        InputPayload inputPayload = new InputPayload()
        {
            tick = currentTick,
            inputVector = new Vector3(movementInput.ReadValue<Vector2>().x, 0, movementInput.ReadValue<Vector2>().y).normalized
        };

        clientInputBuffer.Add(inputPayload, bufferIndex);
        SendToServerRpc(inputPayload);

        StatePayload statePayload = ProcessMovement(inputPayload);
        clientStateBuffer.Add(statePayload, bufferIndex);

        HandleServerReconciliation();
    }

    bool ShouldReconcile()
    {
        bool isNewServerState = !lastServerState.Equals(default);
        bool isLastStateUndefinedOrDifferent = lastProcessedState.Equals(default) ||
                                              !lastProcessedState.Equals(lastServerState);

        return isNewServerState && isLastStateUndefinedOrDifferent;
    }

    void HandleServerReconciliation()
    {
        if (!ShouldReconcile()) return;

        float positionError;
        int bufferIndex;
        StatePayload rewindState = default;

        bufferIndex = lastServerState.tick % bufferSize;
        if (bufferIndex - 1 < 0) return; // Not enough information to reconcile

        rewindState = IsHost ? serverStateBuffer.Get(bufferIndex - 1) : lastServerState; // Host RPCs execute immediately
        positionError = Vector3.Distance(rewindState.position, clientStateBuffer.Get(bufferIndex).position);

        if(positionError > reconciliationThreshold)
            ReconcileState(rewindState);

        lastProcessedState = lastServerState;
    }

    void ReconcileState(StatePayload rewindState)
    {
        transform.position = rewindState.position;
        rb.velocity = rewindState.velocity;

        if (!rewindState.Equals(lastServerState)) return;

        clientStateBuffer.Add(rewindState, rewindState.tick);

        // Replay all inputs from the rewind state to the current state
        int tickToReplay = lastServerState.tick;

        while(tickToReplay < timer.currentTick)
        {
            int bufferIndex = tickToReplay % bufferSize;
            StatePayload statePayload = ProcessMovement(clientInputBuffer.Get(bufferIndex));
            clientStateBuffer.Add(statePayload, bufferIndex);
            tickToReplay++;
        }
    }

    [ServerRpc]
    void SendToServerRpc(InputPayload inputPayload)
    {
        serverInputQueue.Enqueue(inputPayload);
    }

    StatePayload ProcessMovement(InputPayload input)
    {
        MovePlayer(input.inputVector);

        return new StatePayload()
        {
            tick = input.tick,
            position = transform.position,
            velocity = rb.velocity
        };
    }

    private void MovePlayer(Vector3 move)
    {
        Vector3 currentVelocity = rb.velocity;  
        Vector3 targetVelocity = move * moveSpeed * timer.minTimeBetweenTicks;
        rb.velocity = new Vector3(targetVelocity.x, currentVelocity.y, targetVelocity.z);
    }

    public void Respawn()
    {
        if(!IsServer) return;
            respawning = true;

        clientStateBuffer.Clear();
        clientInputBuffer.Clear();
        serverStateBuffer.Clear();
        serverInputQueue.Clear();

        transform.position = currentCheckpoint;
        rb.velocity = Vector3.zero;

        lastServerState = new StatePayload
        {
            tick = timer.currentTick,
            position = currentCheckpoint,
            velocity = rb.velocity
        };

        RespawnClientRpc(currentCheckpoint);
    }

    [ClientRpc]
    void RespawnClientRpc(Vector3 checkpoint)
    {
        if (!IsOwner) return;

        clientStateBuffer.Clear();
        clientInputBuffer.Clear();
        serverStateBuffer.Clear();
        serverInputQueue.Clear();

        transform.position = checkpoint;
        rb.velocity = Vector3.zero;

        lastServerState = new StatePayload
        {
            tick = timer.currentTick,
            position = checkpoint,
            velocity = Vector3.zero
        };

        lastProcessedState = lastServerState;

        SendToServerRpc(new InputPayload
        {
            tick = timer.currentTick,
            inputVector = Vector3.zero
        });

        respawning = false;
    }
}
using UnityEngine;
using UnityEngine.Networking;

using System.Linq;
using System.Collections;
using System.Collections.Generic;

public struct PlayerLocomotionState
{
    public Vector3 position;
    public Vector3 forward;

    public PlayerLocomotionState(Vector3 position, Vector3 forward)
    {
        this.position = position;
        this.forward = forward;
        clientAcknowledged = false;
        serverAcknowledged = false;
    }

    public void apply(Rigidbody targetRbody)
    {
        targetRbody.position = position;
        targetRbody.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }



    public bool clientAcknowledged;
    public bool serverAcknowledged;
}

[NetworkSettings(channel = 0, sendInterval = 0.333f)]
public class PlayerLocomotion : NetworkBehaviour
{
    private float speed = 10f;

    [SerializeField]
    private Rigidbody attachedRigidbody;

    // Commands sent to the server from the client. Only valid on the server.
    private Queue<PlayerInputState> serverCommandHistory = new Queue<PlayerInputState>();

    // Last valid state as reported by the server.
    [SyncVar]
    public PlayerLocomotionState serverState;

    // The ID of the last command acknowledged by the server.
    [SyncVar]
    private int lastAcknowledgedCommandID = -1;

    // Number of commands issued.
    private int commandCounter = 0;

    const int commandHistorySize = 10;

    // Commands issued by this client. Only valid on client with authority.
    private PlayerInputState[] commandHistory;

    public PlayerLocomotionState GetFinalState()
    {
        return (isServer ? GetSimulatedState() : GetPredictedState());
    }

    [Server]
    public PlayerLocomotionState GetSimulatedState()
    {
        PlayerInputState workingCommand;
        PlayerLocomotionState workingState = serverState;

        while (serverCommandHistory.Count > 0)
        {
            workingCommand = serverCommandHistory.Peek();

            //Debug.Log("Simulating " + workingCommand.id);

            workingState = Simulate(workingState, workingCommand);


            lastAcknowledgedCommandID = workingCommand.id;

            serverCommandHistory.Dequeue();
        }

        return workingState;
    }

    [Client]
    public PlayerLocomotionState GetPredictedState()
    {
        // last server-auth state
        var workingState = serverState;

        // first command yet to be processed by server
        var workingCommand = commandHistory[(lastAcknowledgedCommandID + 1) % commandHistorySize];

        int failSafe = 0;

        // TODO: have a breakout condition
        while(true && failSafe < 100)
        {
            var newState = Simulate(workingState, workingCommand);
            if (workingCommand.id == commandCounter - 1)
                break;

            workingState = newState;
            workingCommand = commandHistory[(workingCommand.id + 1) % commandHistorySize];

            failSafe++;
        }

        return workingState;
    }

    public PlayerLocomotionState Simulate(PlayerLocomotionState startState, PlayerInputState input)
    {
        var newState = startState;

        newState.position.x += input.moveRight   * input.duration * speed;
        newState.position.z += input.moveForward * input.duration * speed;

        return newState;
    }

    public void AcceptInput(PlayerInputState input)
    {
        int index = commandCounter % commandHistorySize;
        input.id = commandCounter++;
        commandHistory[index] = input;

        CmdUploadInput(input);
    }

    [Command(channel = 0)]
    private void CmdUploadInput(PlayerInputState input)
    {
        //Debug.Log("received " + input.id);
        serverCommandHistory.Enqueue(input);
    }

    //
    // Unity Events
    //

    // Initialize server-side bookkeeping information.
    public override void OnStartServer()
    {
        base.OnStartServer();

        serverState = new PlayerLocomotionState(transform.position, transform.forward);
    }

    // Initializes bookkeeping structures with respect to latency.
    void Start()
    {
        // Determine appropriate sizes for buffer based on latency.
        if (isClient)
        {
            // TODO: resize the buffer based on latency
            //var rtt = NetworkManager.singleton.client.GetRTT();
            commandHistory = new PlayerInputState[commandHistorySize];
        }
    }

    // Updates the state of the player object for rendering and/or transmission.
    void FixedUpdate()
    {
        if (isServer) { serverState = GetFinalState(); }

        GetFinalState().apply(attachedRigidbody);
    }

    // Invoked at Editor-time to automatically retrieve and assign dependencies.
    void Reset()
    {
        attachedRigidbody = GetComponent<Rigidbody>();
    }
}
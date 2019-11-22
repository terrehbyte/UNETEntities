using UnityEngine;
using Mirror;
using System.Collections;

// Actions as determined by input signals from the player.
[System.Serializable]
public struct PlayerInputState
{
    // Identifier for 
    public int id;

    public float duration;

    public float moveForward;
    public float moveRight;
}

//[NetworkSettings(channel = 2, sendInterval = 0.333f)]
public class PlayerEntity : NetworkBehaviour
{
    // The prefab for the default player character.
    [SyncVar]
    public GameObject pawnPrefab;

    // Refers to the GameObject representing the character in the game.
    public GameObject pawn;
    
    // The component controlling character movement. Only valid on the server.
    public PlayerLocomotion locomotion { private set; get; }

    [Client]
    PlayerInputState GetInput()
    {
        PlayerInputState inputState = new PlayerInputState();

        inputState.id           = -1;
        inputState.duration     = Time.deltaTime;
        inputState.moveForward  = Input.GetAxisRaw("Vertical");
        inputState.moveRight    = Input.GetAxisRaw("Horizontal");

        return inputState;
    }

    void Possess(GameObject candidate)
    {
        locomotion = candidate.GetComponent<PlayerLocomotion>();
    }
    void Unpossess()
    {
        pawn = null;
        locomotion = null;
    }
    [Server]
    GameObject SpawnPlayer()
    {
        var playerStart = NetworkManager.singleton.GetStartPosition();
        var babyPlayer = Instantiate(pawnPrefab,
                                     playerStart.position,
                                     playerStart.rotation) as GameObject;

        // replicate to clients
        NetworkServer.SpawnWithClientAuthority(babyPlayer, gameObject);

        Possess(babyPlayer);
        RpcAssignPawn(babyPlayer);

        return babyPlayer;
    }

    [ClientRpc]
    void RpcAssignPawn(GameObject newPawn)
    {
        pawn = newPawn;
        OnPawnChanged(pawn);
    }

    protected virtual void OnPawnChanged(GameObject newPawn)
    {
        Debug.Log("Client recieving new pawn.");
        pawn = newPawn;
        if(newPawn)
        {
            OnPawnAssigned(newPawn);
        }
        else
        {
            OnPawnRemoved();
        }
    }

    protected virtual void OnPawnAssigned(GameObject newPawn)
    {
        Possess(newPawn);
    }

    protected virtual void OnPawnRemoved()
    {

    }

    //
    // Unity Events
    //
    void Start()
    {
        // automatically spawn a player
        if(isServer)
        {
            pawn = SpawnPlayer();
        }
    }
    void FixedUpdate()
    {
        Debug.Log("Local: Am I the local player? " + isLocalPlayer, this);
        // poll and process input
        if(isLocalPlayer && pawn != null)
        {
            Debug.Log("Local: Processing input...!", this);
            // pass to playerlocomotion for processing
            locomotion.AcceptInput(GetInput());
        }
    }
}
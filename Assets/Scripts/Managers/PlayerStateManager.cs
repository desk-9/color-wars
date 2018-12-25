using Photon.Pun;
using System;
using System.Collections.Generic;
using UnityEngine;

// TODO dkonik: Bad place for this comment but I just want to make sure to 
// get it written somewhere before I get distracted refactoring something else.
// Wherever the actual possession logic goes needs to make sure to handle the case
// where two players both take possession of the ball at the same time. It just needs to 
// make sure that the second player who picked up the ball gracefully gives up possession.
// Actually, on second thought, every client should check (whenever they get a possession event)
// what the time stamp is of the message, and give it to whoever has the oldest timestamp.

/// <summary>
/// Represents all of the possible states a player can be in.
/// NOTE: If you add a state, you should add it the state to the [PlayerStateManager.states]
/// </summary>
public enum State : byte
{
    // Initial state
    StartupState = 0, 
    NormalMovement = 1,
    ChargeDash = 2,
    Dash = 3, 
    Possession = 4,
    ChargeShot = 5,
    Stun = 7,
    FrozenAfterGoal = 8,
    LayTronWall = 9,
    StartOfMatch = 11,
    ControllerDisconnected = 12,
}

public class PlayerStateManager : MonoBehaviourPun, IPunObservable
{
    public State CurrentState { private set; get; } = State.StartupState;

    // TODO dkonik: I can potentially see an ordering issue with this being one event. This may need to be split
    // up into two events (OnStateEnded and OnStateEntered) which are always called latter after the former. 
    // We may not need to do this, but the situation I am thinking of is something like the following:
    // let's say we add two states which both change the color of the player. One changes to color to orange and the 
    // other to green. If we transition from the orange state to the green state, if we are not careful, we can get something
    // like the Green state first handles the OnStateChange event, which changes the players color to green. Then, the 
    // Orange state handles it, and if we are not careful, it might change the player's color back to regular. There are
    // obviously multiple ways to handle this, one being that the orange state would check if the new state also changes the color
    // and not change it back to regular in that case (not great because of coupling between the two). The better one would be to have
    // a PlayerColor component which is responsible for all player color changes, based on the state. That way, that component can change
    // the color however it likes and not worry about it being changed elsewhere.
    /// <summary>
    /// An event that gets fired whenever the player changes state. It provides the old
    /// and the new state, respectively.
    /// </summary>
    public event Action<State, State> OnStateChange;

    /// <summary>
    /// Dictionary of information objects that states can use. Not every state needs this,
    /// so for any given state this may be null
    /// </summary>
    private Dictionary<State, StateTransitionInformation> stateInfos = new Dictionary<State, StateTransitionInformation>()
    {
        { State.StartupState,               null },
        { State.NormalMovement,             new NormalMovementInformation() },
        { State.ChargeDash,                 null },
        { State.Dash,                       new DashInformation() },
        { State.Possession,                 new PossessBallInformation() },
        { State.ChargeShot,                 null },
        { State.Stun,                       new StunInformation() },
        { State.FrozenAfterGoal,            null },
        { State.LayTronWall,                new TronWallInformation() },
        { State.StartOfMatch,               null },
        { State.ControllerDisconnected,     null },
    };


    /// <summary>
    /// Current state information.
    /// NOTE: This throws if it is not the information that was expected
    /// </summary>
    public T CurrentStateInformation_Exn<T>() where T : StateTransitionInformation
    {
        if (stateInfos[CurrentState] is T)
        {
            return stateInfos[CurrentState] as T;
        }
        else
        {
            throw new Exception(string.Format("Current state information was not what was expected. Expected {0}, got {1}", typeof(T), stateInfos[CurrentState].GetType()));
        }
    }

    /// <summary>
    /// If a non owner changed our state, we should not listen to the owner until they have 
    /// confirmed this (by serializing the state). So, for example, if player 1 stuns player 2, 
    /// they will force everyone to make player 2 go to the stun state. So when player 3 gets that RPC,
    /// they will lock player 2 to the stun state (ignoring anything else player 2 sends) until player 2 
    /// sends (via serialization) that they are in the stun state.
    /// </summary>
    private bool nonOwnerForcedState = false;

    /// <summary>
    /// To reduce garbage collection and not allocate everytime a state change is made, we reuse the same
    /// StateTransitionInformation objects, this is the method that allows another component to get the
    /// information object for writing
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    public T GetStateInformationForWriting<T>(State state) where T : StateTransitionInformation
    {
        if (state == CurrentState)
        {
            // TODO: I am totally on the fence on whether we should be treating these state informations as
            // transition information, or as legitimate information used throughout the life of the state...
            // figure that out
            throw new System.Exception("PlayerStateManager: Should not be modifying current state information");
        }
        return stateInfos[state] as T;
    }

    /// <summary>
    /// Networked transition to state. 
    /// NOTE: If this state requires PlayerStateTransitionInformation, it should be filled in before calling
    /// this and passed in as the second argument (just for error checking). Except for NormalMovement.
    /// It is special because it is the most commonly transitioned to state and it would be tedious to
    /// always have to pass in the infromation with it
    /// </summary>
    /// <param name="state"></param>
    public void TransitionToState(State state, StateTransitionInformation transitionInfo = null)
    {
        // Some error checking. If this is a state which contains an information object
        if (stateInfos[state] != null)
        {
            // But we didn't pass one in (exclude NormalMovement, see thefunction comment)
            if (transitionInfo == null && state != State.NormalMovement)
            {
                Debug.LogError("Called TransitionToState but did not provide the transition information");
            }

            if (stateInfos[state] != transitionInfo && state != State.NormalMovement)
            {
                Debug.LogError("Not reusing the pre allocated state transition information. You should not be calling new. Call GetStateInformationForWriting");
            }
        }

        State oldState = CurrentState;
        CurrentState = state;

        // Set the time of the event
        if (stateInfos[CurrentState] != null)
        {
            stateInfos[CurrentState].EventTimeStamp = PhotonNetwork.Time;
        }

        OnStateChange?.Invoke(oldState, CurrentState);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(CurrentState);
            if (stateInfos[CurrentState] != null)
            {
                stateInfos[CurrentState].Serialize(stream, info);
            }
        } else
        {
            State newState = (State)stream.ReceiveNext();

            if (nonOwnerForcedState)
            {
                if (newState != CurrentState)
                {
                    // Received an update from the owner, but it is not a confirmation
                    // of the forced state, so we ignore it
                    Debug.Log("Received a state update from owner while locked to a forced state");
                    return;
                } else
                {
                    // The owner confirmed the forced state. We are already in that state, so we don't need
                    // to actually do anything
                    nonOwnerForcedState = false;
                    return;
                }
            }

            State oldState = CurrentState;
            CurrentState = newState;

            // If this state has information in it, read it
            if (stateInfos[CurrentState] != null)
            {
                stateInfos[CurrentState].Deserialize(stream, info);
            }

            // Fire the event
            if (oldState != CurrentState)
            {
                OnStateChange?.Invoke(oldState, CurrentState);
            }
        }
    }

    // TODO dkonik: Immediately, I don't see us needing this for anything other than stun.
    // But this paradigm can be generalized to any state. I.e. have a "forceToState" function
    // which allows any playerto force any other player to a given state. Though if we do this
    // we will have to have some
    /// <summary>
    /// Forces the player to the stun state for everyone
    /// </summary>
    /// <param name="startPosition"></param>
    /// <param name="blowbackVelocity"></param>
    /// <param name="duration"></param>
    public void StunNetworked(Vector2 startPosition, Vector2 blowbackVelocity, float duration, bool stolenFrom)
    {
        if (photonView.IsMine)
        {
            // If we are the local player, just serialize out as usual
            StunInformation info = GetStateInformationForWriting<StunInformation>(State.Stun);
            info.StartPosition = startPosition;
            info.Velocity = blowbackVelocity;
            info.Duration = duration;
            info.StolenFrom = stolenFrom;
            TransitionToState(State.Stun, info);
        } else
        {
            // TODO dkonik: Using AllViaServer so that photon can guarantee ordering. However, this might
            // not be the right thing to do. The reason I we need to guarantee ordering (or something along those lines)
            // is because if the situation described in the comment in [StunNetworked_Interal], namely: "Say, for example,
            // player 1 destroys player 2s tron wall while they are laying it at the same time that player 3 
            // possesses the ball right by player 2. Both player 1 and player 3 will send an RPC to stun player
            // 2" happens, then we need to be able to guarantee ordering. 
            photonView.RPC("StunNetworked_Interal", RpcTarget.AllViaServer, startPosition, blowbackVelocity, duration, stolenFrom);
        }
    }

    [PunRPC]
    private void StunNetworked_Interal(Vector2 startPosition, Vector2 blowbackVelocity, float duration, bool stolenFrom, PhotonMessageInfo rpcInfo)
    {
        // Situations can arise where two players try to stun third at roughly the same time. Say, for example,
        // player 1 destroys player 2s tron wall while they are laying it at the same time that player 3 
        // possesses the ball right by player 2. Both player 1 and player 3 will send an RPC to stun player
        // 2. Photon guarantees ordering (at least the way we are currently doing it). So we will just have
        // everyoen ignore the second rpc.
        //
        // Or, another situation that might arise is that player 1 stuns player 2, and so sends the RPC out to force 
        // player 2 to stun state. Player 2 might get this, and serialize out the stun information before player 3
        // even gets the RPC, in which case we can also ignore it.
        if (CurrentState == State.Stun)
        {
            StunInformation stunInfo = CurrentStateInformation_Exn<StunInformation>();
            bool isSameInformation = stunInfo.EventTimeStamp == rpcInfo.timestamp;
            Debug.LogFormat("Got an RPC to enter stun state while already in stun, ignoring. IsSameInformation: {0}", isSameInformation);
            
            return;
        }

        if (nonOwnerForcedState)
        {
            Debug.Log("Received an RPC forcing to enter Stun state, but we are already in a forced state.");
            return;
        }

        // Just enter stun as usual, except using the rpc timestamp as the timestamp
        // If we are the owner, this will get serialized out as usual as well, which is what we want.
        // If we are not, nonOwnerState will get set to true so we will ignore any other updates
        // from the owner until they confirm the forced state.
        nonOwnerForcedState = !photonView.IsMine;

        StunInformation info = GetStateInformationForWriting<StunInformation>(State.Stun);
        info.StartPosition = startPosition;
        info.Velocity = blowbackVelocity;
        info.Duration = duration;
        info.EventTimeStamp = rpcInfo.timestamp;

        State oldState = CurrentState;
        CurrentState = State.Stun;
        OnStateChange?.Invoke(oldState, CurrentState);
    }

    private void Start()
    {
        GameManager.Instance.NotificationManager.RegisterPlayer(this);
    }

    public bool IsInState(params State[] states)
    {
        foreach (State state in states)
        {
            if (CurrentState == state)
            {
                return true;
            }
        }
        return false;
    }
}

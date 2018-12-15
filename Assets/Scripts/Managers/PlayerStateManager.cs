using Photon.Pun;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum OldState
{
    StartupState,
    NormalMovement,
    ChargeDash,
    Dash,
    Posession,
    ChargeShot,
    Stun,
    FrozenAfterGoal,
    LayTronWall
};

/// <summary>
/// Represents all of the possible states a player can be in. Micro states, or
/// states that transition to another state automatically are designated with the 
/// "_micro" suffix. 
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
    Possession_micro = 10, // Transitions to possession
    StartOfMatch = 11,
    ControllerDisconnected = 12,
}

public delegate void ToggleCallback(bool isEnteringState);
public delegate void TransitionCallback(OldState start, OldState end);

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
        { State.StartupState,      null },
        { State.NormalMovement,    new NormalMovementInformation() },
        { State.ChargeDash,        null },
        { State.Dash,              new DashInformation() },
        { State.Possession,        null },
        { State.ChargeShot,        null },
        { State.Stun,              new StunInformation() },
        { State.FrozenAfterGoal,   null },
        { State.LayTronWall,       null },
        // TODO dkonik: Get rid of this micro state
        { State.Possession_micro,  new PossessBallInformation() },
    };

    /// <summary>
    /// Current state information.
    /// NOTE: This may be null if the state does not have any relevant information
    /// </summary>
    public StateTransitionInformation CurrentStateInformation => stateInfos[CurrentState];

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
            } else if (stateInfos[state] != transitionInfo)
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
            State oldState = CurrentState;
            CurrentState = (State)stream.ReceiveNext();

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


    private SortedDictionary<OldState, ToggleCallback> onToggleState =
        new SortedDictionary<OldState, ToggleCallback>();
    private SortedDictionary<OldState, Callback> onStartState =
        new SortedDictionary<OldState, Callback>();
    private SortedDictionary<OldState, Callback> onEndState =
        new SortedDictionary<OldState, Callback>();
    private TransitionCallback onAnyChange = delegate { };
    public OldState oldState { get; private set; }

    private Callback stopCurrentState;
    private OldState defaultState = OldState.NormalMovement;
    private Callback startDefaultState;
    private Callback stopDefaultState;

    private void Awake()
    {
        oldState = OldState.StartupState;
        stopCurrentState = delegate { };
        startDefaultState = delegate { };
        stopDefaultState = delegate { };
        foreach (OldState state in (OldState[])System.Enum.GetValues(typeof(OldState)))
        {
            onToggleState[state] = delegate { };
            onStartState[state] = delegate { };
            onEndState[state] = delegate { };
        }
    }

    private void Start()
    {
        GameManager.instance.NotificationManager.RegisterPlayer(this);
    }

    public void AttemptPossession(Callback start, Callback stop)
    {
        if (IsInState(OldState.NormalMovement, OldState.Dash, OldState.ChargeDash, OldState.LayTronWall))
        {
            SwitchToState(OldState.Posession, start, stop);
        }
    }

    public void AttemptStun(Callback start, Callback stop)
    {
        if (IsInState(OldState.NormalMovement, OldState.Posession, OldState.LayTronWall, OldState.Dash))
        {
            SwitchToState(OldState.Stun, start, stop);
        }
    }

    public void AttemptLayTronWall(Callback start, Callback stop)
    {
        if (IsInState(OldState.NormalMovement))
        {
            SwitchToState(OldState.LayTronWall, start, stop);
        }
    }

    public void AttemptFrozenAfterGoal(Callback start, Callback stop)
    {
        SwitchToState(OldState.FrozenAfterGoal, start, stop);
    }

    public void AttemptStartState(Callback start, Callback stop)
    {
        SwitchToState(OldState.StartupState, start, stop);
    }

    private void SwitchToState(OldState state, Callback start, Callback stop)
    {
        Utility.Print("Switching from", oldState, "to", state);
        stopCurrentState();
        AlertSubscribers(oldState, false, state);

        oldState = state;
        start();
        stopCurrentState = stop;
        AlertSubscribers(state, true);
    }

    private void AlertSubscribers(OldState state, bool isEnteringState, OldState? nextState = null)
    {
        onToggleState[state](isEnteringState);
        if (isEnteringState)
        {
            onStartState[state]();
        }
        else
        {
            onEndState[state]();

            if (nextState != null)
            {
                onAnyChange(state, nextState.Value);
            }
        }
    }

    public bool IsInState(params OldState[] states)
    {
        foreach (OldState state in states)
        {
            if (oldState == state)
            {
                return true;
            }
        }
        return false;
    }
}

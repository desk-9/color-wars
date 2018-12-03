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
    ShootBall_micro = 6, // Transitions to normal
    Stun = 7,
    FrozenAfterGoal = 8,
    LayTronWall = 9,
    Steal_micro = 10, // Transitions to possession
    StartOfMatch = 11,
    ControllerDisconnected = 12,
}

public delegate void ToggleCallback(bool isEnteringState);
public delegate void TransitionCallback(OldState start, OldState end);

public class PlayerStateManager : MonoBehaviourPun, IPunObservable
{
    public State CurrentState { private set; get; } = State.StartupState;

    /// <summary>
    /// An event that gets fired whenever the player changes state. It provides the old
    /// and the new state, respectively.
    /// </summary>
    public event Action<State, State> OnStateChange;

    /// <summary>
    /// Dictionary of information objects that states can use. Not every state needs this,
    /// so for any given state this may be null
    /// </summary>
    private Dictionary<State, PlayerStateInformation> stateInfos = new Dictionary<State, PlayerStateInformation>()
    {
        { State.StartupState,      null },
        { State.NormalMovement,    null },
        { State.ChargeDash,        null },
        { State.Dash,              new DashInformation() },
        { State.Possession,        null },
        { State.ChargeShot,        null },
        { State.ShootBall_micro,   new ShootBallInformation() },
        { State.Stun,              new StunInformation() },
        { State.FrozenAfterGoal,   null },
        { State.LayTronWall,       null },
        { State.Steal_micro,       new StealBallInformation() },
    };

    /// <summary>
    /// Current state information.
    /// NOTE: This may be null if the state does not have any relevant information
    /// </summary>
    public PlayerStateInformation StateTransitionInformation => stateInfos[CurrentState];

    /// <summary>
    /// To reduce garbage collection and not allocate everytime a state change is made, we reuse the same
    /// StateTransitionInformation objects, this is the method that allows another component to get the
    /// information object for writing
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    public PlayerStateInformation GetStateInformationForWriting(State state)
    {
        if (state == CurrentState)
        {
            // TODO: I am totally on the fence on whether we should be treating these state informations as
            // transition information, or as legitimate information used throughout the life of the state...
            // figure that out
            throw new System.Exception("PlayerStateManager: Should not be modifying current state information");
        }
        return stateInfos[state];
    }

    /// <summary>
    /// Networked transition to state. 
    /// NOTE: If this state requires PlayerStateTransitionInformation, it should be set before calling this
    /// </summary>
    /// <param name="state"></param>
    public void TransitionToState(State state)
    {
        State oldState = CurrentState;
        CurrentState = state;
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

            if (stateInfos[CurrentState] != null)
            {
                stateInfos[CurrentState].Deserialize(stream, info);
            }

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
    public OldState currentState { get; private set; }

    private Callback stopCurrentState;
    private OldState defaultState = OldState.NormalMovement;
    private Callback startDefaultState;
    private Callback stopDefaultState;

    private void Awake()
    {
        currentState = OldState.StartupState;
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
        GameManager.instance.notificationManager.RegisterPlayer(this);
    }

    // Schedules a callback whenever information with respect to a certain state
    // changes
    public void CallOnSpecficStateChange(OldState state, ToggleCallback callback)
    {
        onToggleState[state] += callback;
    }

    public void CallOnAnyStateChange(TransitionCallback callback)
    {
        onAnyChange += callback;
    }

    public void CallOnStateExit(OldState state, Callback callback)
    {
        onEndState[state] += callback;
    }

    // This method should be called if a state exits without being forced, such as
    // the end of a dash, or after giving away possession of ball.
    public void CurrentStateHasFinished()
    {
        SwitchToState(defaultState, startDefaultState, stopDefaultState);
    }

    public void AttemptNormalMovement(Callback start, Callback stop)
    {
        if (IsInState(OldState.StartupState, OldState.NormalMovement))
        {
            currentState = OldState.NormalMovement;
            startDefaultState = start;
            stopDefaultState = stop;
            stopCurrentState = stop;
            start();
        }
        else
        {
            Debug.LogErrorFormat("Tried to start NormalMovementState while in {0}", currentState);
        }
    }

    public void AttemptDashCharge(Callback start, Callback stop)
    {
        if (IsInState(OldState.NormalMovement))
        {
            SwitchToState(OldState.ChargeDash, start, stop);
        }
    }

    public void AttemptPossession(Callback start, Callback stop)
    {
        if (IsInState(OldState.NormalMovement, OldState.Dash, OldState.ChargeDash, OldState.LayTronWall))
        {
            SwitchToState(OldState.Posession, start, stop);
        }
    }

    public void AttemptDash(Callback start, Callback stop)
    {
        if (IsInState(OldState.ChargeDash))
        {
            SwitchToState(OldState.Dash, start, stop);
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
        Utility.Print("Switching from", currentState, "to", state);
        stopCurrentState();
        AlertSubscribers(currentState, false, state);

        currentState = state;
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
            if (currentState == state)
            {
                return true;
            }
        }
        return false;
    }
}

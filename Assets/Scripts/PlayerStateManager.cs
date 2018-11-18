using System.Collections.Generic;
using UnityEngine;

// To add a state, do the following:
// 1) Add a state to the enum
// 2) Add an Attempt[YourState] method
// 3) Be sure to add any valid exit transitions to the other AttemptState methods
//    i.e. if it is valid to transition from your new state to ChargeDash, add that in
//    AttemptChargeDash

public enum State
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

public delegate void ToggleCallback(bool isEnteringState);
public delegate void TransitionCallback(State start, State end);

public class PlayerStateManager : MonoBehaviour
{
    private SortedDictionary<State, ToggleCallback> onToggleState =
        new SortedDictionary<State, ToggleCallback>();
    private SortedDictionary<State, Callback> onStartState =
        new SortedDictionary<State, Callback>();
    private SortedDictionary<State, Callback> onEndState =
        new SortedDictionary<State, Callback>();
    private TransitionCallback onAnyChange = delegate { };
    public State currentState { get; private set; }

    private Callback stopCurrentState;
    private State defaultState = State.NormalMovement;
    private Callback startDefaultState;
    private Callback stopDefaultState;

    private void Awake()
    {
        currentState = State.StartupState;
        stopCurrentState = delegate { };
        startDefaultState = delegate { };
        stopDefaultState = delegate { };
        foreach (State state in (State[])System.Enum.GetValues(typeof(State)))
        {
            onToggleState[state] = delegate { };
            onStartState[state] = delegate { };
            onEndState[state] = delegate { };
        }
    }

    private void Start()
    {
        GameModel.instance.notificationCenter.RegisterPlayer(this);
    }

    // Schedules a callback whenever information with respect to a certain state
    // changes
    public void CallOnSpecficStateChange(State state, ToggleCallback callback)
    {
        onToggleState[state] += callback;
    }

    public void CallOnAnyStateChange(TransitionCallback callback)
    {
        onAnyChange += callback;
    }

    public void CallOnStateEnter(State state, Callback callback)
    {
        onStartState[state] += callback;
    }

    public void CallOnStateExit(State state, Callback callback)
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
        if (IsInState(State.StartupState, State.NormalMovement))
        {
            currentState = State.NormalMovement;
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
        if (IsInState(State.NormalMovement))
        {
            SwitchToState(State.ChargeDash, start, stop);
        }
    }

    public void AttemptPossession(Callback start, Callback stop)
    {
        if (IsInState(State.NormalMovement, State.Dash, State.ChargeDash, State.LayTronWall))
        {
            SwitchToState(State.Posession, start, stop);
        }
    }

    public void AttemptDash(Callback start, Callback stop)
    {
        if (IsInState(State.ChargeDash))
        {
            SwitchToState(State.Dash, start, stop);
        }
    }

    public void AttemptStun(Callback start, Callback stop)
    {
        if (IsInState(State.NormalMovement, State.Posession, State.LayTronWall, State.Dash))
        {
            SwitchToState(State.Stun, start, stop);
        }
    }

    public void AttemptLayTronWall(Callback start, Callback stop)
    {
        if (IsInState(State.NormalMovement))
        {
            SwitchToState(State.LayTronWall, start, stop);
        }
    }

    public void AttemptFrozenAfterGoal(Callback start, Callback stop)
    {
        SwitchToState(State.FrozenAfterGoal, start, stop);
    }

    public void AttemptStartState(Callback start, Callback stop)
    {
        SwitchToState(State.StartupState, start, stop);
    }

    private void SwitchToState(State state, Callback start, Callback stop)
    {
        Utility.Print("Switching from", currentState, "to", state);
        stopCurrentState();
        AlertSubscribers(currentState, false, state);

        currentState = state;
        start();
        stopCurrentState = stop;
        AlertSubscribers(state, true);
    }

    private void AlertSubscribers(State state, bool isEnteringState, State? nextState = null)
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

    public bool IsInState(params State[] states)
    {
        foreach (State state in states)
        {
            if (currentState == state)
            {
                return true;
            }
        }
        return false;
    }

}

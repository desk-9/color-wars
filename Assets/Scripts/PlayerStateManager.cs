using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

// To add a state, do the following:
// 1) Add a state to the enum
// 2) Add an Attempt[YourState] method
// 3) Be sure to add any valid exit transitions to the other AttemptState methods
//    i.e. if it is valid to transition from your new state to ChargeDash, add that in
//    AttemptChargeDash

public enum State
{ StartupState,
  NormalMovement,
  ChargeDash,
  Dash,
  Posession,
  ChargeShot,
  Stun,
};

public delegate void ToggleCallback(bool isEnteringState);
public delegate void TransitionCallback(State start, State end);

public class PlayerStateManager : MonoBehaviour {


    SortedDictionary<State, ToggleCallback> on_toggle_state =
        new SortedDictionary<State, ToggleCallback>();
    SortedDictionary<State, Callback> on_start_state =
        new SortedDictionary<State, Callback>();
    SortedDictionary<State, Callback> on_end_state =
        new SortedDictionary<State, Callback>();
    TransitionCallback on_any_change = delegate{};
    State currentState;
    Callback stopCurrentState;
    State defaultState = State.NormalMovement;
    Callback startDefaultState;
    Callback stopDefaultState;

    void Awake() {
        currentState = State.StartupState;
        stopCurrentState = null;
        foreach (var state in (State[]) System.Enum.GetValues(typeof(State))) {
            on_toggle_state[state] = delegate{};
            on_start_state[state] = delegate{};
            on_end_state[state] = delegate{};
        }
    }

    void Start() {
        GameModel.instance.nc.HookUpPlayer(this);
    }

    // Schedules a callback whenever information with respect to a certain state
    // changes
    public void CallOnSpecficStateChange(State state, ToggleCallback callback) {
        on_toggle_state[state] += callback;
    }

    public void CallOnAnyStateChange(TransitionCallback callback) {
        on_any_change += callback;
    }

    public void CallOnStateEnter(State state, Callback callback) {
        on_start_state[state] += callback;
    }

    public void CallOnStateExit(State state, Callback callback) {
        on_end_state[state] += callback;
    }

    // This method should be called if a state exits without being forced, such as
    // the end of a dash, or after giving away possession of ball.
    public void CurrentStateHasFinished() {
        SwitchToState(defaultState, startDefaultState, stopDefaultState);
    }

    public void AttemptNormalMovement(Callback start, Callback stop){
        if (IsInState(State.StartupState)) {
            currentState = State.NormalMovement;
            startDefaultState = start;
            stopDefaultState = stopCurrentState = stop;
            start();
        } else {
            Debug.LogErrorFormat("Tried to start NormalMovementState while in {0}", currentState);
        }
    }

    public void AttemptDashCharge(Callback start, Callback stop) {
        if (IsInState(State.NormalMovement)) {
            SwitchToState(State.ChargeDash, start, stop);
        }
    }

    public void AttemptPossession(Callback start, Callback stop) {
        if (IsInState(State.NormalMovement, State.Dash, State.ChargeDash)) {
            SwitchToState(State.Posession, start, stop);
        }
    }

    public void AttemptDash(Callback start, Callback stop) {
        if (IsInState(State.ChargeDash)) {
            SwitchToState(State.Dash, start, stop);
        }
    }

    public void AttemptStun(Callback start, Callback stop) {
        if (IsInState(State.NormalMovement, State.Posession)) {
            SwitchToState(State.Stun, start, stop);
        }
    }

    void SwitchToState(State state, Callback start, Callback stop) {
        stopCurrentState();
        AlertSubscribers(currentState, false, state);

        currentState = state;
        start();
        stopCurrentState = stop;
        AlertSubscribers(state, true);
    }

    void AlertSubscribers(State state, bool isEnteringState, State? nextState = null) {
        on_toggle_state[state](isEnteringState);
        if (isEnteringState) {
            on_start_state[state]();
        } else {
            on_end_state[state]();

            if (nextState != null) {
                on_any_change(state, nextState.Value);
            }
        }
    }

    public bool IsInState(params State[] states){
        foreach (var state in states){
            if (currentState == state) {
                return true;
            }
        }
        return false;
    }

}

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
  Knockback,
};

public class PlayerStateManager : MonoBehaviour {

    public delegate void SubscriberCallback(bool isEnteringState);

    SortedDictionary<State, List<SubscriberCallback>> subscribers =
        new SortedDictionary<State, List<SubscriberCallback>>();
    State currentState;
    Callback stopCurrentState;
    State defaultState = State.NormalMovement;
    Callback startDefaultState;
    Callback stopDefaultState;
    
    void Awake() {
        currentState = State.StartupState;
        stopCurrentState = null; 
        foreach (var state in (State[])System.Enum.GetValues(typeof(State))) {
            subscribers[state] = new List<SubscriberCallback>();
        }
    }

    // Schedules a callback whenever information with respect to a state changes
    public void SignUpForStateAlert(State onEntry, SubscriberCallback callback){
        subscribers[onEntry].Add(callback);
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

    public void AttemptDashCharge(Callback start, Callback stop){
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

    void SwitchToState(State state, Callback start, Callback stop){
        stopCurrentState();
        AlertSubscribers(currentState, false);
        
        currentState = state;
        start();
        stopCurrentState = stop;
        AlertSubscribers(state, true);
    }

    void AlertSubscribers(State state, bool isEnteringState){
        foreach (var subscriberCallback in subscribers[state]){
            subscriberCallback(isEnteringState);
        }
    }

    bool IsInState(params State[] states){
        foreach(var state in states){
            if (currentState == state) {
                return true;
            }
        }
        return false;
    }
    
}

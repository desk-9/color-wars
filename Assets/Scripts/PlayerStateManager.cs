using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

public enum States
{ StartupState,
  NormalMovement,
  Dash,
  Posession,
  ChargeShot,
  Knockback,
};

public class PlayerStateManager : MonoBehaviour {

    public delegate void SubscriberCallback(bool isEnteringState);

    SortedDictionary<States, List<SubscriberCallback>> subscribers =
	new SortedDictionary<States, List<SubscriberCallback>>();
    States currentState;
    Callback stopCurrentState;
    States defaultState = States.NormalMovement;
    Callback startDefaultState;
    Callback stopDefaultState;
    
    void Start() {
	currentState = States.StartupState;
	stopCurrentState = null; 
	foreach (var state in (States[])System.Enum.GetValues(typeof(States))) {
	    subscribers[state] = new List<SubscriberCallback>();
	}
    }

    public void SignUpForStateAlert(States onEntry, SubscriberCallback callback){
	subscribers[onEntry].Add(callback);
    }

    public void CurrentStateHasFinished() {
	SwitchToState(defaultState, startDefaultState, stopDefaultState);
    }

    public void AttemptNormalMovement(Callback start, Callback stop){
	if (IsInState(States.StartupState)) {
	    currentState = States.NormalMovement;
	    startDefaultState = start;
	    stopDefaultState = stopCurrentState = stop;
	    start();
	} else {
	    Debug.LogErrorFormat("Tried to start NormalMovementState while in {0}", currentState);
	}
    }

    public void AttemptDash(Callback start, Callback stop){
	if (IsInState(States.NormalMovement)) {
	    SwitchToState(States.Dash, start, stop);
	}
    }

    public void AttemptPossession(Callback start, Callback stop) {
	if (IsInState(States.NormalMovement, States.Dash)) {
	    SwitchToState(States.Posession, start, stop);
	}
    }

    void SwitchToState(States state, Callback start, Callback stop){
	stopCurrentState();
	AlertSubscribers(currentState, false);
	
	currentState = state;
	start();
	stopCurrentState = stop;
	AlertSubscribers(state, true);
    }

    void AlertSubscribers(States state, bool isEnteringState){
	foreach (var subscriberCallback in subscribers[state]){
	    subscriberCallback(isEnteringState);
	}
    }

    bool IsInState(params States[] states){
	foreach(var state in states){
	    if (currentState == state) {
		return true;
	    }
	}
	return false;
    }
    
}

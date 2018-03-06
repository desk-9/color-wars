using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum States {StartupState, NormalMovement, Dash, ChargeDash, Posession, Knockback};

public class PlayerStateManager : MonoBehaviour {

    States currentState;
    Coroutine currentStateCoroutine;
    
    void Start() {
	currentState = States.StartupState;
    }

    public void AttemptDashCharge(IEnumerator dashCharge){
	if (IsInState(States.NormalMovement)) {
	    SwitchToState(States.ChargeDash, dashCharge);
	}
    }

    public void AttemptDash(IEnumerator dash) {
	if (IsInState(States.ChargeDash)){
	    SwitchToState(States.Dash, dash);
	} else {
	    Debug.LogError("A state other than ChargeDash attempted to dash");
	}
    }

    public void ExitDash() {
	SwitchToState(State state, IEnumerator newCoroutine)
    }

    void SwitchToState(States state, IEnumerator newCoroutine){
	currentState = state;
	StopCoroutine(currentStateCoroutine);
	currentStateCoroutine = StartCoroutine(newCoroutine);
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

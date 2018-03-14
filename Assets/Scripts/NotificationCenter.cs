using System;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

// This is a class that deals with global events, where the subscribers can't
// necessarily get access to all the actual publisher object. Events that are
// tied to individual, non-singleton objects shouldn't go here.

public delegate void PlayerCallback(Player player);
public delegate void PlayerTransitionCallback(Player player, State start, State end);
// Replacement for EventHandler without the EventArgs
public delegate void EventCallback(object sender);

public class NotificationCenter {

    // PlayerState addons
    SortedDictionary<State, PlayerCallback> on_any_player_start_subscribers =
        new SortedDictionary<State, PlayerCallback>();
    SortedDictionary<State, PlayerCallback> on_any_player_end_subscribers =
        new SortedDictionary<State, PlayerCallback>();
    PlayerTransitionCallback on_any_change_subscribers = delegate{};

    public void HookUpPlayer(PlayerStateManager player) {
        var playerComponent = player.GetComponent<Player>();
        foreach (var state in (State[]) System.Enum.GetValues(typeof(State))) {
            player.CallOnStateEnter(
                state, () => on_any_player_start_subscribers[state](playerComponent));
            player.CallOnStateExit(
                state, () => on_any_player_end_subscribers[state](playerComponent));
            player.CallOnAnyStateChange(
                (State start, State end) => on_any_change_subscribers(playerComponent, start, end));
        }
    }

    public void CallOnStateStart(State state, PlayerCallback callback) {
        on_any_player_start_subscribers[state] += callback;
    }

    public void CallOnStateEnd(State state, PlayerCallback callback) {
        on_any_player_end_subscribers[state] += callback;
    }

    public void CallOnStateTransition(PlayerTransitionCallback callback) {
        on_any_change_subscribers += callback;
    }

    // String-based event system

    // Useful for hacky/quick setup of simple events without writing extra code,
    // but isn't typesafe/you're gonna have to assume whatever implicit
    // contracts you have are kept
    SortedDictionary<string, EventCallback> string_events =
        new SortedDictionary<string, EventCallback>();

    public void CallOnStringEventWithSender(string identifier, EventCallback callback) {
        string_events[identifier] += callback;
    }

    public void CallOnStringEvent(string identifier, Callback callback) {
        string_events[identifier] += (object o) => callback();
    }

    public void NotifyStringEvent(string identifier, object sender) {
        if (string_events.ContainsKey(identifier)) {
            string_events[identifier](sender);
        }
    }
}

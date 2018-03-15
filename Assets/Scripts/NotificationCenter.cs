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


public enum Message {

};

public class NotificationCenter {

    // PlayerState addons
    SortedDictionary<State, PlayerCallback> on_any_player_start_subscribers =
        new SortedDictionary<State, PlayerCallback>();
    SortedDictionary<State, PlayerCallback> on_any_player_end_subscribers =
        new SortedDictionary<State, PlayerCallback>();
    PlayerTransitionCallback on_any_change_subscribers = delegate{};

    public NotificationCenter() {
        foreach (var state in (State[]) System.Enum.GetValues(typeof(State))) {
            on_any_player_start_subscribers[state] = delegate{};
            on_any_player_end_subscribers[state] = delegate{};
        }

        foreach (var event_type in (Message[]) System.Enum.GetValues(typeof(Message))) {
            on_message[event_type] = delegate{};
        }
    }

    public void RegisterPlayer(PlayerStateManager player) {
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

    // Enum-based callback system
    //
    // Useful for publishing "system-wide" events that are meant to stick around
    // for a while/be maintainable. You must add a new event name to the Message
    // enum, then ensure something is calling NotifyMessage appropriately.
    SortedDictionary<Message, EventCallback> on_message =
        new SortedDictionary<Message, EventCallback>();

    public void CallOnMessageWithSender(Message event_type, EventCallback callback) {
        on_message[event_type] += callback;
    }

    public void CallOnMessage(Message event_type, Callback callback) {
        on_message[event_type] += (object o) => callback();
    }

    public void NotifyMessage(Message event_type, object sender) {
        on_message[event_type](sender);
    }

    // String-based event system
    //
    // Useful for hacky/quick setup of simple events without writing extra code,
    // but isn't typesafe/you're gonna have to assume whatever implicit
    // contracts you have are kept. Good for dynamically-created event systems,
    // "private" systems (communicate some arbitrary string), or one-off cases
    // where you're really pretty sure only one pair of producer/consumer needs
    // this an a whole new enum value would be over the top. Think carefully
    // before using this system.
    SortedDictionary<string, EventCallback> string_events =
        new SortedDictionary<string, EventCallback>();

    public void CallOnStringEventWithSender(string identifier, EventCallback callback) {
        if (!string_events.ContainsKey(identifier)) {
            string_events[identifier] = delegate{};
        }
        string_events[identifier] += callback;
    }

    public void CallOnStringEvent(string identifier, Callback callback) {
        if (!string_events.ContainsKey(identifier)) {
            string_events[identifier] = delegate{};
        }
        string_events[identifier] += (object o) => callback();
    }

    public void NotifyStringEvent(string identifier, object sender) {
        if (string_events.ContainsKey(identifier)) {
            string_events[identifier](sender);
        }
    }
}

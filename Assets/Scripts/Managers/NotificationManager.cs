using System.Collections.Generic;
using UnityEngine;

// This is a class that deals with global events, where the subscribers can't
// necessarily get access to all the actual publisher object. Events that are
// tied to individual, non-singleton objects shouldn't go here.

public delegate void PlayerCallback(Player player);
public delegate void PlayerTransitionCallback(Player player, OldState start, OldState end);
// Replacement for EventHandler without the EventArgs
public delegate void EventCallback(object sender);
public delegate void GameObjectCallback(GameObject thing);


public delegate bool EventPredicate(object sender);

public enum Message
{
    BallIsPossessed,
    BallIsUnpossessed,
    StartCountdown,
    CountdownFinished,
    GoalScored,
    ScoreChanged,
    SlowMoEntered,
    SlowMoExited,
    BallSetNeutral,
    BallPossessedWhileNeutral,
    BallCharged,
    BallPossessedWhileCharged,
    NullChargePrevention,
    StolenFrom,
    TronWallDestroyed,
    TronWallDestroyedWhileLaying,

    InputDeviceAssigned,

    PlayerStick,

    PlayerPressedA,
    PlayerReleasedA,

    PlayerPressedLeftBumper,
    PlayerReleasedLeftBumper,

    PlayerPressedRightBumper,
    PlayerReleasedRightBumper,

    PlayerPressedB,
    PlayerReleasedB,

    PlayerPressedX,
    PlayerReleasedX,

    PlayerPressedY,
    PlayerReleasedY,

    PlayerPressedBack,
    PlayerReleasedBack,

    PlayerPressedDash,
    PlayerReleasedDash,
    PlayerPressedShoot,
    PlayerReleasedShoot,
    PlayerPressedWall,
    PlayerReleasedWall,

    RecordingInterrupt,
    RecordingFinished,
};

public class NotificationManager
{

    // PlayerState addons
    private SortedDictionary<State, PlayerCallback> onAnyPlayerEnterStateSubscribers =
        new SortedDictionary<State, PlayerCallback>();
    private SortedDictionary<State, PlayerCallback> onAnyPlayerExitStateSubscribers =
        new SortedDictionary<State, PlayerCallback>();

    public NotificationManager()
    {
        foreach (State state in (State[])System.Enum.GetValues(typeof(State)))
        {
            onAnyPlayerEnterStateSubscribers[state] = delegate { };
            onAnyPlayerExitStateSubscribers[state] = delegate { };
        }

        foreach (Message event_type in (Message[])System.Enum.GetValues(typeof(Message)))
        {
            onMessage[event_type] = delegate { };
        }
    }

    public void RegisterPlayer(PlayerStateManager player)
    {
        Player playerComponent = player.GetComponent<Player>();

        player.OnStateChange += (oldState, newState) =>
        {
            onAnyPlayerEnterStateSubscribers[newState](playerComponent);
            onAnyPlayerExitStateSubscribers[oldState](playerComponent);
        };
    }

    public void CallOnStateStart(State state, PlayerCallback callback)
    {
        onAnyPlayerEnterStateSubscribers[state] += callback;
    }

    public void CallOnStateEnd(State state, PlayerCallback callback)
    {
        onAnyPlayerExitStateSubscribers[state] += callback;
    }

    // Enum-based callback system
    //
    // Useful for publishing "system-wide" events that are meant to stick around
    // for a while/be maintainable. You must add a new event name to the Message
    // enum, then ensure something is calling NotifyMessage appropriately.
    private SortedDictionary<Message, EventCallback> onMessage =
        new SortedDictionary<Message, EventCallback>();

    public void CallOnMessageWithSender(Message event_type, EventCallback callback)
    {
        onMessage[event_type] += callback;
    }

    public void CallOnMessage(Message event_type, Callback callback)
    {
        onMessage[event_type] += (object o) => callback();
    }

    public void CallOnMessageIf(Message event_type, EventCallback callback,
                                EventPredicate predicate)
    {
        onMessage[event_type] += (object o) =>
        {
            if (predicate(o))
            {
                callback(o);
            }
        };
    }

    public void CallOnMessageIfSameObject(Message event_type, Callback callback, GameObject thing)
    {
        CallOnMessageIf(event_type, o => callback(), o => (o as GameObject) == thing);
    }

    public void NotifyMessage(Message event_type, object sender)
    {
        onMessage[event_type](sender);
    }

    public void UnsubscribeMessage(Message event_type, EventCallback callback)
    {
        onMessage[event_type] -= callback;
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
    private SortedDictionary<string, EventCallback> stringEvents =
        new SortedDictionary<string, EventCallback>();

    public void CallOnStringEventWithSender(string identifier, EventCallback callback)
    {
        if (!stringEvents.ContainsKey(identifier))
        {
            stringEvents[identifier] = delegate { };
        }
        stringEvents[identifier] += callback;
    }

    public void CallOnStringEvent(string identifier, Callback callback)
    {
        if (!stringEvents.ContainsKey(identifier))
        {
            stringEvents[identifier] = delegate { };
        }
        stringEvents[identifier] += (object o) => callback();
    }

    public void NotifyStringEvent(string identifier, object sender)
    {
        if (stringEvents.ContainsKey(identifier))
        {
            stringEvents[identifier](sender);
        }
    }
}

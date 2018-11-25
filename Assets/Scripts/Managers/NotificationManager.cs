using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using Photon.Realtime;
using Photon.Pun;

using EventData = ExitGames.Client.Photon.EventData;
using SendOptions = ExitGames.Client.Photon.SendOptions;

// This is a class that deals with global events, where the subscribers can't
// necessarily get access to all the actual publisher object. Events that are
// tied to individual, non-singleton objects shouldn't go here.

public delegate void PlayerCallback(Player player);
public delegate void PlayerTransitionCallback(Player player, State start, State end);
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

    private HashSet<Message> networkBlacklist = new HashSet<Message> {
        Message.PlayerStick,
        Message.StartCountdown,
        Message.CountdownFinished,
        Message.SlowMoEntered,
        Message.SlowMoExited,
        Message.BallIsPossessed,
        Message.BallIsUnpossessed
    };
    // PlayerState addons
    private SortedDictionary<State, PlayerCallback> onAnyPlayerStartSubscribers =
        new SortedDictionary<State, PlayerCallback>();
    private SortedDictionary<State, PlayerCallback> onAnyPlayerEndSubscribers =
        new SortedDictionary<State, PlayerCallback>();
    private PlayerTransitionCallback onAnyChangeSubscribers = delegate { };

    // StringEventCode is reserved for string events. Other event codes are used
    // for Message enum values.
    private const byte StringEventCode = 199;

    public NotificationManager()
    {
        foreach (State state in (State[])System.Enum.GetValues(typeof(State)))
        {
            onAnyPlayerStartSubscribers[state] = delegate { };
            onAnyPlayerEndSubscribers[state] = delegate { };
        }

        foreach (Message event_type in (Message[])System.Enum.GetValues(typeof(Message)))
        {
            onMessage[event_type] = delegate { };
        }

        PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
    }

    public void RegisterPlayer(PlayerStateManager player)
    {
        Player playerComponent = player.GetComponent<Player>();
        foreach (State state in (State[])System.Enum.GetValues(typeof(State)))
        {
            player.CallOnStateEnter(
                state, () => onAnyPlayerStartSubscribers[state](playerComponent));
            player.CallOnStateExit(
                state, () => onAnyPlayerEndSubscribers[state](playerComponent));
            player.CallOnAnyStateChange(
                (State start, State end) => onAnyChangeSubscribers(playerComponent, start, end));
        }
    }

    public void CallOnStateStart(State state, PlayerCallback callback)
    {
        onAnyPlayerStartSubscribers[state] += callback;
    }

    public void CallOnStateEnd(State state, PlayerCallback callback)
    {
        onAnyPlayerEndSubscribers[state] += callback;
    }

    public void CallOnStateTransition(PlayerTransitionCallback callback)
    {
        onAnyChangeSubscribers += callback;
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

    public void CallOnMessageWithPlayerObject(Message eventType, EventCallback callback) {
        onMessage[eventType] += (object o) => {
            var playerNumber = o as int?;
            if (playerNumber.HasValue) {
                callback(GameManager.instance.GetPlayerFromNumber(playerNumber.Value));
            } else {
                Utility.Print("No player number", LogLevel.Error);
            }

        };
    }

    [PunRPC]
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

    public void CallOnMessageIfSamePlayer(Message eventType, Callback callback,
                                                int playerNumber) {
        CallOnMessageIf(eventType, o => callback(), o => (o as int?) == (playerNumber as int?));
    }

    public void CallOnMessageIfSamePlayer(Message eventType, Callback callback,
                                                GameObject thing) {
        var player = thing?.GetComponent<Player>();
        if (player != null) {
            CallOnMessageIfSamePlayer(eventType, callback, player.playerNumber);
        } else {
            Utility.Print("Tried to check player number on a non-player object", LogLevel.Error);
        }
    }

    public void NotifyMessage(Message event_type, object sender)
    {
        NotifyMessage_Internal(event_type, sender);
        SendNetworkedMessageEvent(event_type, sender);
    }

    public void NotifyMessagePlayer(Message eventType, int playerNumber) {
        NotifyMessage(eventType, playerNumber);
    }

    public void NotifyMessagePlayer(Message eventType, Player player) {
        NotifyMessagePlayer(eventType, player.playerNumber);
    }

    public void NotifyMessagePlayer(Message eventType, GameObject player) {
        Utility.Print("Notify message player");
        var playerComponent = player?.GetComponent<Player>();
        if (playerComponent != null) {
            NotifyMessagePlayer(eventType, playerComponent.playerNumber);
        } else {
            Utility.Print("Tried to check player number on a non-player object", LogLevel.Error);
        }
    }

    private void NotifyMessage_Internal(Message event_type, object sender) {
        onMessage[event_type](sender);
    }


    private const byte eventCodeMessageOffset = 15;
    private static Message EventCodeToMessage(byte eventCode) {
        return (Message) (eventCode - eventCodeMessageOffset);
    }

    private static byte MessageToEventCode(Message message) {
        return (byte) (((byte) message) + eventCodeMessageOffset);
    }


    private void SendNetworkedMessageEvent(Message event_type, object sender) {
        if (networkBlacklist.Contains(event_type)) {
            return;
        }
        Utility.Print("Event code", event_type.ToString(), LogLevel.Error);

        byte code = MessageToEventCode(event_type);
        var content = new object[] {sender};
        var sendOptions = new SendOptions { Reliability = true };
        PhotonNetwork.RaiseEvent(code, content, null, sendOptions);
    }

    private void DispatchNetworkedMessageEvent(byte eventCode,
                                               object[] data, int senderId) {
        NotifyMessage_Internal(EventCodeToMessage(eventCode), data[0]);
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
        NotifyStringEvent_Internal(identifier, sender);
        SendNetworkedStringEvent(identifier, sender);
    }

    private void NotifyStringEvent_Internal(string identifier, object sender)
    {
        if (stringEvents.ContainsKey(identifier))
        {
            stringEvents[identifier](sender);
        }
    }

    private void SendNetworkedStringEvent(string identifier, object sender) {
        // TODO
    }

    private void DispatchNetworkedStringEvent(object[] data, int senderId) {
        // TODO
    }

    private bool EventCodeIsMessage(byte eventCode) {
        var maxMessageCode = Enum.GetValues(typeof(Message)).Cast<int>().Max();
        return (byte) EventCodeToMessage(eventCode) <= maxMessageCode;
    }

    // Networked event handling
    public void OnEvent(EventData photonEvent) {
        if (photonEvent.Code == StringEventCode) {
            DispatchNetworkedStringEvent((object[]) photonEvent.CustomData, photonEvent.Sender);
        } else if (EventCodeIsMessage(photonEvent.Code)) {
            Utility.Print("Event code", EventCodeToMessage(photonEvent.Code).ToString());
            DispatchNetworkedMessageEvent(photonEvent.Code, (object[]) photonEvent.CustomData,
                                          photonEvent.Sender);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;
using Action = System.Action;


// Sources:
// https://www.gamasutra.com/blogs/VivekTank/20180703/321126/How_to_use_C_Events_in_Unity.php
// 
// https://forum.unity.com/threads/c-events-affecting-all-objects-with-attached-script-am-i-approaching-events-the-right-way.415707/
//
// http://csharpindepth.com/Articles/Chapter2/Events.aspx
// https://stackoverflow.com/questions/1437699/in-a-c-sharp-event-handler-why-must-the-sender-parameter-be-an-object
// https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/generics/generic-delegates
public class EventsManager : MonoBehaviour
{
    public static EventsManager instance;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }


    // Possession events
    public static event Action<onBallStolenArgs> onBallStolen;
    public class onBallStolenArgs : System.EventArgs
    {
        public BallCarrier oldOwner;
        public BallCarrier newOwner;
        public Ball ball;
    }
    public static void RaiseOnBallStolen(onBallStolenArgs args)
    {
        onBallStolen?.Invoke(args);
    }


    public class BallCarrierArgs : System.EventArgs
    {
        public BallCarrier ballCarrier;
        public Ball ball;
    }


    public static event Action<BallCarrierArgs> onBallDropped;
    public static void RaiseOnBallDropped(BallCarrierArgs args)
    {
        onBallDropped?.Invoke(args);
    }

    public static event Action<BallCarrierArgs> onStartedCarryingBall;
    public static void RaiseOnStartedCarryingBall(BallCarrierArgs args)
    {
        onStartedCarryingBall?.Invoke(args);
    }

    public static event Action<BallCarrierArgs> onStoppedCarryingBall;
    public static void RaiseOnStoppedCarryingBall(BallCarrierArgs args)
    {
        onStoppedCarryingBall?.Invoke(args);
    }

    // Charging/shooting ball
    public static event Action<BallCarrierArgs> onStartedChargingBall;
    public static void RaiseOnStartedChargingBall(BallCarrierArgs args)
    {
        onStartedChargingBall?.Invoke(args);
    }

    public static event Action<BallCarrierArgs> onStoppedChargingBall;
    public static void RaiseOnStoppedChargingBall(BallCarrierArgs args)
    {
        onStoppedChargingBall?.Invoke(args);
    }

    public static event Action<BallCarrierArgs> onBallShot;
    public static void RaiseOnBallShot(BallCarrierArgs args)
    {
        onBallShot?.Invoke(args);
    }

    // Ball activation events
    public static event Action<BallCarrierArgs> onBallContested;
    public static void RaiseOnBallContested(BallCarrierArgs args)
    {
        onBallContested?.Invoke(args);
    }

    public static event Action<BallCarrierArgs> onBallDominated;
    public static void RaiseOnBallDominated(BallCarrierArgs args)
    {
        onBallDominated?.Invoke(args);
    }

    public static event Action onBallNeutralized;
    public static void RaiseOnBallNeutralized() {onBallNeutralized?.Invoke();}


    // Goal events
    public static event Action<onGoalScoredArgs> onGoalScored;
    public class onGoalScoredArgs : System.EventArgs{}
    public static void RaiseOnGoalScored(onGoalScoredArgs args)
    {
        onGoalScored?.Invoke(args);
    }

    public static event Action onResetAfterGoal;
    public static void RaiseOnResetAfterGoal()
    {
        onResetAfterGoal?.Invoke();
    }


    // Player states
    // ----------------
    public class PlayerArgs : System.EventArgs {}
    delegate void PlayerEventHandler<Player, PlayerArgs>(
        Player sender, PlayerArgs eventArgs);


    public static event PlayerEventHandler<Player, PlayerArgs> PlayerStunned;
    public static void RaiseOnPlayerStunned(Player sender, PlayerArgs args)
    {
        PlayerStunned?.Invoke(sender, args);
    }

    public static event PlayerEventHandler<Player, PlayerArgs> PlayerUnstunned;
    public static void RaiseOnPlayerUnstunned(Player sender, PlayerArgs args)
    {
        PlayerUnstunned?.Invoke(sender, args);
    }

    // public static event PlayerEventHandler<Player, PlayerArgs>
    /*
      OnStartedChargingDash
      OnStoppedChargingDash
      OnStartedDashing
      OnStoppedDashing // called regardless of HOW they stopped dashing

      // Ways to break tron walls
      OnTronWallBroken
      OnTronWallBrokenDuringConstruction
      OnTronWallTimedOut
      OnTronWallLruRemoved
      OnTronWallRemoved // last thing in tron wall's OnDestroy

      // TronWallMechanic events
      OnStartedBuildingTronWall
      OnStoppedBuildingTronWall
      OnFinishedTronWall

      // Tron wall removal effects
      OnStartedShatteringTronWall
      OnFinishedShatteringTronWall
      OnStartedCollapsingTronWall
      OnFinishedCollapsingTronWall

     */



}





// Sources:
// https://www.c-sharpcorner.com/article/custom-generic-eventargs/
// public class EventArgs<T> : EventArgs
// {
//     public EventArgs(T value) {m_value = value;}
//     private T m_value;
//     public T Value
//     {
//         get { return m_value; }
//     }
// }
// public class EventArgs<T, U> : EventArgs<T>
// {
//     public EventArgs(T value, U value2)
//         : base(value) {m_value2 = value2;}
//     private U m_value2;
//     public U Value2
//     {
//         get { return m_value2; }
//     }
// }
// public class EventArgs<T, U, V> : EventArgs<T>
// {
//     public EventArgs(T value, U value2, V value3)
//         : base(value, value2) {m_value3 = value3;}
//     private V m_value3;
//     public V Value3
//     {
//         get { return m_value3; }
//     }
// }



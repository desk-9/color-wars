using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class that all player states inherit from that provides an "interface" way to
/// serialize and deserialize
/// </summary>
public abstract class StateTransitionInformation
{
    /// <summary>
    /// This is the photon time that the original event happened
    /// </summary>
    public double EventTimeStamp { get; set; }

    public virtual void Deserialize(PhotonStream stream, PhotonMessageInfo info)
    {
        EventTimeStamp = (double)stream.ReceiveNext();
    }

    public virtual void Serialize(PhotonStream stream, PhotonMessageInfo info)
    {
        stream.SendNext(EventTimeStamp);
    }
}

public class DashInformation : StateTransitionInformation
{
    public Vector2 StartPosition { get; set; }
    public Vector2 Velocity { get; set; }

    public override void Deserialize(PhotonStream stream, PhotonMessageInfo info)
    {
        base.Deserialize(stream, info);
        StartPosition = (Vector2)stream.ReceiveNext();
        Velocity = (Vector2)stream.ReceiveNext();
    }

    public override void Serialize(PhotonStream stream, PhotonMessageInfo info)
    {
        base.Serialize(stream, info);
        stream.SendNext(StartPosition);
        stream.SendNext(Velocity);
    }
}

public class PossessBallInformation : StateTransitionInformation
{
    /// <summary>
    /// True if we stole the ball from someone
    /// </summary>
    public bool StoleBall { get; set; } = false;

    /// <summary>
    /// Player from whom the ball was stolen (only valid if StoleBall is true)
    /// </summary>
    public int VictimPlayerNumber { get; set; }

    public override void Deserialize(PhotonStream stream, PhotonMessageInfo info)
    {
        StoleBall = (bool)stream.ReceiveNext();
        if (StoleBall)
        {
            VictimPlayerNumber = (int)stream.ReceiveNext();
        }
    }

    public override void Serialize(PhotonStream stream, PhotonMessageInfo info)
    {
        stream.SendNext(StoleBall);
        if (StoleBall)
        {
            stream.SendNext(VictimPlayerNumber);
        }
    }
}

public class NormalMovementInformation : StateTransitionInformation
{
    public bool ShotBall { get; set; } = false;
    public Vector2 BallStartPosition { get; set; }
    public Vector2 Velocity { get; set; }

    public override void Deserialize(PhotonStream stream, PhotonMessageInfo info)
    {
        ShotBall = (bool)stream.ReceiveNext();
        if (ShotBall)
        {
            BallStartPosition = (Vector2)stream.ReceiveNext();
            Velocity = (Vector2)stream.ReceiveNext();
        }
    }

    public override void Serialize(PhotonStream stream, PhotonMessageInfo info)
    {
        stream.SendNext(ShotBall);
        if (ShotBall)
        {
            stream.SendNext(BallStartPosition);
            stream.SendNext(Velocity);
        }
    }
}

public class StunInformation : StateTransitionInformation
{
    public Vector2 StartPosition { get; set; }
    public Vector2 Velocity { get; set; }
    public float Duration { get; set; }
    public bool StolenFrom { get; set; } = false;

    public override void Deserialize(PhotonStream stream, PhotonMessageInfo info)
    {
        StartPosition = (Vector2)stream.ReceiveNext();
        Velocity = (Vector2)stream.ReceiveNext();
        Duration = (float)stream.ReceiveNext();
        StolenFrom = (bool)stream.ReceiveNext();
    }

    public override void Serialize(PhotonStream stream, PhotonMessageInfo info)
    {
        stream.SendNext(StartPosition);
        stream.SendNext(Velocity);
        stream.SendNext(Duration);
        stream.SendNext(StolenFrom);
    }
}

public class TronWallInformation : StateTransitionInformation
{
    /// <summary>
    /// The point at which the player started laying the tron wall
    /// </summary>
    public Vector2 StartPosition { get; set; }
    public Vector2 Direction { get; set; }


    public override void Deserialize(PhotonStream stream, PhotonMessageInfo info)
    {
        StartPosition = (Vector2)stream.ReceiveNext();
        Direction = (Vector2)stream.ReceiveNext();
    }

    public override void Serialize(PhotonStream stream, PhotonMessageInfo info)
    {
        stream.SendNext(StartPosition);
        stream.SendNext(Direction);
    }
}

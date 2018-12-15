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
    public Vector2 Direction { get; set; }
    public float Strength { get; set; }

    public override void Deserialize(PhotonStream stream, PhotonMessageInfo info)
    {
        base.Deserialize(stream, info);
        StartPosition = (Vector3)stream.ReceiveNext();
        Direction = (Vector3)stream.ReceiveNext();
        Strength = (float)stream.ReceiveNext();
    }

    public override void Serialize(PhotonStream stream, PhotonMessageInfo info)
    {
        base.Serialize(stream, info);
        stream.SendNext(StartPosition);
        stream.SendNext(Direction);
        stream.SendNext(Strength);
    }
}

public class PossessBallInformation : StateTransitionInformation
{
    public class BlowBackInformation
    {
        public int PlayerNumber { get; set; }
        public Vector2 StartingPosition { get; set; }
        public Vector2 BlowBackVelocity { get; set; }
    }

    /// <summary>
    /// True if we stole the ball from someone
    /// </summary>
    public bool StoleBall { get; set; } = false;

    /// <summary>
    /// Player number from whom the ball was stolen from. Only a
    /// valid number if StoleBall is true
    /// </summary>
    public BlowBackInformation StealVictimInformation { get; set; }

    public int NumberOfPlayersBlownBack { get; set; }

    /// <summary>
    /// List that is reused
    /// </summary>
    public List<BlowBackInformation> BlownBackPlayers { get; set; }

    public PossessBallInformation()
    {
        // Can't blow back self, so maxPlayers - 1. Ignoring the fact that can't blow
        // back teammate either for now because it's not a big deal
        BlownBackPlayers = new List<BlowBackInformation>(PlayerInputManager.maxPlayers - 1);

        for (int i = 0; i < PlayerInputManager.maxPlayers - 1; ++i)
        {
            BlownBackPlayers.Add(new BlowBackInformation());
        }
    }

    public override void Deserialize(PhotonStream stream, PhotonMessageInfo info)
    {
        StoleBall = (bool)stream.ReceiveNext();
        if (StoleBall)
        {
            StealVictimInformation.PlayerNumber = (int)stream.ReceiveNext();
            StealVictimInformation.StartingPosition = (Vector2)stream.ReceiveNext();
            StealVictimInformation.BlowBackVelocity = (Vector2)stream.ReceiveNext();
        }
        NumberOfPlayersBlownBack = (int)stream.ReceiveNext();
        Debug.Assert(NumberOfPlayersBlownBack <= PlayerInputManager.maxPlayers - 1);
        for (int i = 0; i < NumberOfPlayersBlownBack; ++i)
        {
            BlownBackPlayers[i].PlayerNumber = (int)stream.ReceiveNext();
            BlownBackPlayers[i].StartingPosition = (Vector2)stream.ReceiveNext();
            BlownBackPlayers[i].BlowBackVelocity = (Vector2)stream.ReceiveNext();
        }
    }

    public override void Serialize(PhotonStream stream, PhotonMessageInfo info)
    {
        Debug.Assert(NumberOfPlayersBlownBack <= PlayerInputManager.maxPlayers - 1);
        stream.SendNext(StoleBall);
        if (StoleBall)
        {
            stream.SendNext(StealVictimInformation.PlayerNumber);
            stream.SendNext(StealVictimInformation.StartingPosition);
            stream.SendNext(StealVictimInformation.BlowBackVelocity);
        }
        stream.SendNext(NumberOfPlayersBlownBack);
        for (int i = 0; i < NumberOfPlayersBlownBack; ++i)
        {
            stream.SendNext(BlownBackPlayers[i].PlayerNumber);
            stream.SendNext(BlownBackPlayers[i].StartingPosition);
            stream.SendNext(BlownBackPlayers[i].BlowBackVelocity);
        }
    }
}

public class NormalMovementInformation : StateTransitionInformation
{
    public bool ShotBall { get; set; } = false;
    public Vector2 BallStartPosition { get; set; }
    public Vector2 Direction { get; set; }
    public float Strength { get; set; }

    public override void Deserialize(PhotonStream stream, PhotonMessageInfo info)
    {
        ShotBall = (bool)stream.ReceiveNext();
        if (ShotBall)
        {
            BallStartPosition = (Vector3)stream.ReceiveNext();
            Direction = (Vector3)stream.ReceiveNext();
            Strength = (float)stream.ReceiveNext();
        }
    }

    public override void Serialize(PhotonStream stream, PhotonMessageInfo info)
    {
        stream.SendNext(ShotBall);
        if (ShotBall)
        {
            stream.SendNext(BallStartPosition);
            stream.SendNext(Direction);
            stream.SendNext(Strength);
        }
    }
}

public class StunInformation : StateTransitionInformation
{
    public Vector2 StartPosition { get; set; }
    public Vector2 Direction { get; set; }
    public float Strength { get; set; }

    public override void Deserialize(PhotonStream stream, PhotonMessageInfo info)
    {
        StartPosition = (Vector3)stream.ReceiveNext();
        Direction = (Vector3)stream.ReceiveNext();
        Strength = (float)stream.ReceiveNext();
    }

    public override void Serialize(PhotonStream stream, PhotonMessageInfo info)
    {
        stream.SendNext(StartPosition);
        stream.SendNext(Direction);
        stream.SendNext(Strength);
    }
}

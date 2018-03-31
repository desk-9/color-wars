using UnityEngine;

public interface IPlayerMovement {
    void RotatePlayer(Vector2? snapAngle = null);
    void FreezePlayer();
    void UnFreezePlayer();
}

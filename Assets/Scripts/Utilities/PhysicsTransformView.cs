using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

[RequireComponent(typeof(PhotonView))]
public class PhysicsTransformView : MonoBehaviour, IPunObservable {
    private PhotonView photonView;
    private Rigidbody2D rigidbody;

    private bool firstUpdate = true;

    private Vector2 networkPosition;
    private float networkRotation;

    void Awake() {
        photonView = GetComponent<PhotonView>();
        rigidbody = GetComponent<Rigidbody2D>();
        networkPosition = transform.position;
        networkRotation = rigidbody.rotation;
    }

    void FixedUpdate() {
        if (!photonView.IsMine) {
            if (firstUpdate) {
                Utility.Print("First update", LogLevel.Error);
                rigidbody.position = networkPosition;
                rigidbody.rotation = networkRotation;
                firstUpdate = false;
            } else {
                rigidbody.position = Vector2.MoveTowards(rigidbody.position, networkPosition, Time.fixedDeltaTime * 1f);
                rigidbody.rotation = Mathf.LerpAngle(rigidbody.rotation, networkRotation, Time.fixedDeltaTime * 10f);

            }

        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
        if (stream.IsWriting) {
            stream.SendNext(rigidbody.position);
            stream.SendNext(rigidbody.velocity);
            stream.SendNext(rigidbody.angularVelocity);
            stream.SendNext(rigidbody.rotation);
        } else {
            networkPosition = (Vector2) stream.ReceiveNext();
            rigidbody.velocity = (Vector2) stream.ReceiveNext();
            rigidbody.angularVelocity = (float) stream.ReceiveNext();
            networkRotation = (float) stream.ReceiveNext();
            float lag = Mathf.Abs((float) (PhotonNetwork.Time - info.timestamp));
            networkPosition += rigidbody.velocity * lag;
        }
    }
}

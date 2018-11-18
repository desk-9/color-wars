using UnityEngine;

public class SceneLoadTrigger : MonoBehaviour
{

    public Scene associatedScene;

    public void LoadAssociatedScene()
    {
        SceneStateManager.instance.Load(associatedScene);
    }
}

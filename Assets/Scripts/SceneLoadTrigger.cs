using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneLoadTrigger : MonoBehaviour {

    public Scene associatedScene;
	
    public void LoadAssociatedScene() {
        SceneStateController.instance.Load(associatedScene);
    }
}

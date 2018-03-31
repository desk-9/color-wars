using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

public class SpriteResource {
    const string spriteDirectory = "Sprites";
    string resourcePath;
    Sprite sprite;
    public SpriteResource(string path) {
        this.resourcePath = string.Format("{0}/{1}", spriteDirectory, path) ;
        this.sprite = Resources.Load<Sprite>(this.resourcePath);
        if (this.sprite == null) {
            throw new ArgumentException(string.Format("No such sprite at {0}", this.resourcePath));
        }
    }

    public static implicit operator SpriteResource(string path) {
        return new SpriteResource(path);
    }

    public static implicit operator Sprite(SpriteResource resource) {
        return resource.sprite;
    }
}

public class SpriteManager : MonoBehaviour {
    public static SpriteManager instance;

    public void Awake() {
        if (instance != null) {
            Destroy(this);
        } else {
            instance = this;
        }
    }
}

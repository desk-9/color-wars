using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UtilityExtensions;

public class RichText : MonoBehaviour {

    public int fontSize = 10;
    public float initialSpacing = 0;
    public float elementSpacing = 5;
    public float imageVerticalSpacing = 5;
    public bool center = true;
    public Color textColor = Color.black;
    public Color color {
        get {
            return textColor;
        } set {
            textColor = value;
            SetColors();
        }
    }

    public string textPrefabName = "RichTextPrefabs/RichElementText";
    public string imagePrefabName = "RichTextPrefabs/RichElementImage";

    public string initialContent = "";

    GameObject textPrefab;
    GameObject imagePrefab;
    RectTransform rectTransform;

    string rawText = "";
    public string text {
        get {
            return rawText;
        }

        set {
            var old = rawText;
            rawText = value;
            if (rawText != old) {
                RenderText();
            }
        }
    }

    void Awake() {
        textPrefab = Resources.Load<GameObject>(textPrefabName);
        imagePrefab = Resources.Load<GameObject>(imagePrefabName);
        rectTransform = this.EnsureComponent<RectTransform>();
    }

    // Use this for initialization
    void Start () {
        if (initialContent != "") {
            text = initialContent;
        }
    }

    void OnEnable() {
        RenderText();
    }

    void SetColors() {
        foreach (Transform element in transform) {
            var text = element.gameObject?.GetComponent<Text>();
            var image = element.gameObject?.GetComponent<Image>();
            if (text != null) {
                text.color = textColor;
            } else if (image != null) {
                var imageColor = image.color;
                imageColor.a = textColor.a;
                image.color = imageColor;
            }
        }
    }

    public class RichTextElement {
        public bool isFilename = false;
        public string content;
        public RichTextElement(string content, bool isFilename) {
            this.content = content;
            this.isFilename = isFilename;
        }
    }

    public List<RichTextElement> ParseRichText(string text) {
        // Rich text format specification:
        //
        // A = name-of-existing-image-file
        // B = <A>
        // C = alphanumeric-string-without-[<>]
        // D = C*B*C*
        // L = D*
        //
        // So any amount of alphanumeric characters excluding [<>]
        var result = new List<RichTextElement>();
        string currentContent = "";
        foreach (char c in text) {
            if (c == '<') {
                if (currentContent != "") {
                    result.Add(new RichTextElement(currentContent, false));
                }
                currentContent = "";
            } else if (c == '>') {
                result.Add(new RichTextElement(currentContent, true));
                currentContent = "";
            } else {
                currentContent += c.ToString();
            }
        }
        if (currentContent != "") {
            result.Add(new RichTextElement(currentContent, false));
        }
        return result;
    }

    public void RenderText() {
        foreach (Transform element in transform) {
            Destroy(element.gameObject);
        }
        var richElements = ParseRichText(rawText);
        float elementStart = initialSpacing;
        foreach (var elementContent in richElements) {
            RectTransform elementTransform;
            if (elementContent.isFilename) {
                var image = CreateImage(elementContent.content);
                float aspectRatio = image.sprite.rect.width / image.sprite.rect.height;

                float inducedWidth = (rectTransform.rect.height - imageVerticalSpacing * 2) * aspectRatio;
                elementTransform = image.GetComponent<RectTransform>();
                elementTransform.sizeDelta = new Vector2(
                    inducedWidth, elementTransform.sizeDelta.y - imageVerticalSpacing * 2);
                Canvas.ForceUpdateCanvases();
            } else {
                var text = CreateText(elementContent.content);
                elementTransform = text.GetComponent<RectTransform>();
                Canvas.ForceUpdateCanvases();
            }
            elementTransform.anchoredPosition = new Vector2(elementStart, 0);
            elementStart += elementTransform.rect.width + elementSpacing;
        }
        if (center) {
            rectTransform.sizeDelta = new Vector2(elementStart, rectTransform.sizeDelta.y);
        }
    }

    Sprite LoadSprite(string name) {
        var result = Resources.Load<Sprite>(name);
        if (result == null) {
            Utility.Print("No such sprite as", name, "exists!", LogLevel.Error);
        }
        return result;
    }

    Text CreateText(string content){
        var element = Instantiate(textPrefab, transform);
        Text textComponent = element.EnsureComponent<Text>();
        textComponent.text = content;
        textComponent.fontSize = fontSize;
        textComponent.color = textColor;
        return textComponent;
    }

    Image CreateImage(string content) {
        var element = Instantiate(imagePrefab, transform);
        Image imageComponent = element.EnsureComponent<Image>();
        imageComponent.sprite = LoadSprite(content);
        return imageComponent;
    }

    // Update is called once per frame
    void Update () {

    }
}

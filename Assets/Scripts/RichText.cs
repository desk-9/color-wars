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
    CanvasGroup group;
    Coroutine textRendering;

    string rawText = "";
    public string text {
        get {
            return rawText;
        }

        set {
            Utility.Print("Rich text set to", value, LogLevel.Error);
            var old = rawText;
            rawText = value;
            if (rawText != old) {
                RenderText();
            }
        }
    }

    // Use this for initialization
    void Start () {
        textPrefab = Resources.Load<GameObject>(textPrefabName);
        imagePrefab = Resources.Load<GameObject>(imagePrefabName);
        rectTransform = this.EnsureComponent<RectTransform>();
        group = gameObject.AddComponent<CanvasGroup>();
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
        if (textRendering != null) {
            StopCoroutine(textRendering);
        }
        textRendering = StartCoroutine(TextRendering());
    }

    IEnumerator TextRendering() {
        yield return null;
        group.alpha = 0;
        var richElements = ParseRichText(rawText);
        float elementStart = initialSpacing;
        Utility.Print("Rendering...");
        foreach (var elementContent in richElements) {
            Utility.Print(elementStart, elementContent.content);
            RectTransform elementTransform;
            if (elementContent.isFilename) {
                var image = CreateImage(elementContent.content);
                float aspectRatio = image.sprite.rect.width / image.sprite.rect.height;

                float inducedWidth = (rectTransform.rect.height - imageVerticalSpacing * 2) * aspectRatio;
                elementTransform = image.GetComponent<RectTransform>();
                Utility.Print(elementTransform.rect.width, LogLevel.Warning);
                elementTransform.sizeDelta = new Vector2(
                    inducedWidth, elementTransform.sizeDelta.y - imageVerticalSpacing * 2);
                yield return null;
            } else {
                var text = CreateText(elementContent.content);
                elementTransform = text.GetComponent<RectTransform>();
                yield return null;
                Utility.Print(elementTransform.rect.width, LogLevel.Warning);
            }
            elementTransform.anchoredPosition = new Vector2(elementStart, 0);
            elementStart += elementTransform.rect.width + elementSpacing;
        }
        if (center) {
            rectTransform.sizeDelta = new Vector2(elementStart, rectTransform.sizeDelta.y);
        }
        group.alpha = 1;
        textRendering = null;
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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UtilityExtensions;

public class RichText : MonoBehaviour
{

    public int fontSize = 10;
    public float initialSpacing = 0;
    public float elementSpacing = 5;
    public float imageVerticalSpacing = 5;
    public bool center = true;
    public Color textColor = Color.black;
    public Color color
    {
        get
        {
            return textColor;
        }
        set
        {
            textColor = value;
            SetColors();
        }
    }

    public string textPrefabName = "RichTextPrefabs/RichElementText";
    public string imagePrefabName = "RichTextPrefabs/RichElementImage";

    public string initialContent = "";
    private GameObject textPrefab;
    private GameObject imagePrefab;
    private RectTransform rectTransform;
    private string rawText = "";
    public string text
    {
        get
        {
            return rawText;
        }

        set
        {
            string old = rawText;
            rawText = value;
            if (rawText != old)
            {
                RenderText();
            }
        }
    }

    private void Awake()
    {
        textPrefab = Resources.Load<GameObject>(textPrefabName);
        imagePrefab = Resources.Load<GameObject>(imagePrefabName);
        rectTransform = this.EnsureComponent<RectTransform>();
    }

    // Use this for initialization
    private void Start()
    {
        if (initialContent != "")
        {
            text = initialContent;
        }
    }

    private void OnEnable()
    {
        RenderText();
    }

    private void SetColors()
    {
        foreach (Transform element in transform)
        {
            Text text = element.gameObject?.GetComponent<Text>();
            Image image = element.gameObject?.GetComponent<Image>();
            if (text != null)
            {
                text.color = textColor;
            }
            else if (image != null)
            {
                Color imageColor = image.color;
                imageColor.a = textColor.a;
                image.color = imageColor;
            }
        }
    }

    public class RichTextElement
    {
        public bool isFilename = false;
        public string content;
        public RichTextElement(string content, bool isFilename)
        {
            this.content = content;
            this.isFilename = isFilename;
        }
    }

    public List<RichTextElement> ParseRichText(string text)
    {
        // Rich text format specification:
        //
        // A = name-of-existing-image-file
        // B = <A>
        // C = alphanumeric-string-without-[<>]
        // D = C*B*C*
        // L = D*
        //
        // So any amount of alphanumeric characters excluding [<>]
        List<RichTextElement> result = new List<RichTextElement>();
        string currentContent = "";
        foreach (char c in text)
        {
            if (c == '<')
            {
                if (currentContent != "")
                {
                    result.Add(new RichTextElement(currentContent, false));
                }
                currentContent = "";
            }
            else if (c == '>')
            {
                result.Add(new RichTextElement(currentContent, true));
                currentContent = "";
            }
            else
            {
                currentContent += c.ToString();
            }
        }
        if (currentContent != "")
        {
            result.Add(new RichTextElement(currentContent, false));
        }
        return result;
    }

    public void RenderText()
    {
        foreach (Transform element in transform)
        {
            Destroy(element.gameObject);
        }
        List<RichTextElement> richElements = ParseRichText(rawText);
        float elementStart = initialSpacing;
        foreach (RichTextElement elementContent in richElements)
        {
            RectTransform elementTransform;
            if (elementContent.isFilename)
            {
                Image image = CreateImage(elementContent.content);
                float aspectRatio = image.sprite.rect.width / image.sprite.rect.height;

                float inducedWidth = (rectTransform.rect.height - imageVerticalSpacing * 2) * aspectRatio;
                elementTransform = image.GetComponent<RectTransform>();
                elementTransform.sizeDelta = new Vector2(
                    inducedWidth, elementTransform.sizeDelta.y - imageVerticalSpacing * 2);
                Canvas.ForceUpdateCanvases();
            }
            else
            {
                Text text = CreateText(elementContent.content);
                elementTransform = text.GetComponent<RectTransform>();
                Canvas.ForceUpdateCanvases();
            }
            elementTransform.anchoredPosition = new Vector2(elementStart, 0);
            elementStart += elementTransform.rect.width + elementSpacing;
        }
        if (center)
        {
            rectTransform.sizeDelta = new Vector2(elementStart, rectTransform.sizeDelta.y);
        }
    }

    private Sprite LoadSprite(string name)
    {
        Sprite result = Resources.Load<Sprite>(name);
        if (result == null)
        {
            Utility.Print("No such sprite as", name, "exists!", LogLevel.Error);
        }
        return result;
    }

    private Text CreateText(string content)
    {
        GameObject element = Instantiate(textPrefab, transform);
        Text textComponent = element.EnsureComponent<Text>();
        textComponent.text = content;
        textComponent.fontSize = fontSize;
        textComponent.color = textColor;
        return textComponent;
    }

    private Image CreateImage(string content)
    {
        GameObject element = Instantiate(imagePrefab, transform);
        Image imageComponent = element.EnsureComponent<Image>();
        imageComponent.sprite = LoadSprite(content);
        return imageComponent;
    }

    // Update is called once per frame
    private void Update()
    {

    }
}

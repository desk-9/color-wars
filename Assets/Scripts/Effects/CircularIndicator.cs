using UnityEngine;
using UnityEngine.UI;

public class CircularIndicator : MonoBehaviour
{

    public Image fillImage = null;

    public float minFillAmount = 0.0f;
    public float maxFillAmount = 1.0f;
    public virtual float FillAmount
    {
        get { return fillImage.fillAmount; }
        set { fillImage.fillAmount = (value - minFillAmount) / (maxFillAmount - minFillAmount); }
    }

    public virtual Color fillColor
    {
        get { return fillImage.color; }
        set { fillImage.color = value; }
    }

    public virtual void Start()
    {
        FillAmount = minFillAmount;
        Hide();
    }

    public virtual void Show() { fillImage.enabled = true; }
    public virtual void Hide() { fillImage.enabled = false; }

    // Remark: this is not the opposite of "Start".
    public virtual void Stop()
    {
        FillAmount = minFillAmount;
        Hide();
    }

    public virtual void Update()
    {
        transform.rotation = Quaternion.identity;
    }

}

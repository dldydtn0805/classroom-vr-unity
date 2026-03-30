using UnityEngine;

namespace AgoraVR.Presentation.UI
{

public sealed class PreparationTimerView : MonoBehaviour
{
    [SerializeField] private GameObject root;

    public float LastRenderedSeconds { get; private set; }

    public void Show()
    {
        if (root != null)
        {
            root.SetActive(true);
        }
    }

    public void Hide()
    {
        if (root != null)
        {
            root.SetActive(false);
        }
    }

    public void SetRemainingSeconds(float remainingSeconds)
    {
        LastRenderedSeconds = remainingSeconds;
    }
}
}

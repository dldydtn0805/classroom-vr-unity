using System;
using UnityEngine;

namespace AgoraVR.Presentation.UI
{

public sealed class TopicSelectionPanelView : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private string defaultTopicId = "interview_strength";

    public event Action<string> TopicSelected;

    public string DefaultTopicId => defaultTopicId;

    public void Show()
    {
        SetVisible(true);
    }

    public void Hide()
    {
        SetVisible(false);
    }

    public void SelectTopic(string topicId)
    {
        TopicSelected?.Invoke(topicId);
    }

    private void SetVisible(bool isVisible)
    {
        if (root != null)
        {
            root.SetActive(isVisible);
        }
    }
}
}

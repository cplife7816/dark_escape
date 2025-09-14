using UnityEngine;

public class SubtitleObject : MonoBehaviour
{
    [TextArea]
    [SerializeField] private string subtitleText;

    public string GetSubtitleText()
    {
        return subtitleText;
    }
}

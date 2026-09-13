using Localization;
using TMPro;
using UnityEngine;

public class BasicLocalizationTextExample : MonoBehaviour
{
    [SerializeField] private string template = "<UI|WELCOME(<UI|PLAYER_NAME>, \"Yes,It's you!\")>";
    public TextMeshProUGUI textMesh;

    void Start()
    {
        textMesh ??= GetComponent<TextMeshProUGUI>();
        
        Refresh();
    }

    void OnEnable()
    {
        BasicLocalizationExample.Instance.AddListener(Refresh);
    }

    void OnDisable()
    {
        BasicLocalizationExample.Instance.RemoveListener(Refresh);
    }

    private void Refresh()
    {
       textMesh.text = BasicLocalizationExample.Instance.GetLocalizedText(template);
    }
}
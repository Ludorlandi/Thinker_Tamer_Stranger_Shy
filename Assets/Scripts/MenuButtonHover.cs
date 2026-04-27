using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Gestisce hover e click del bottone "Clicca per hackerare i The_sign_files".
/// Attach questo script al GameObject del bottone (stesso del CanvasGroup).
/// </summary>
public class MenuButtonHover : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] MenuController    controller;
    [SerializeField] TextMeshProUGUI   label;
    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color hoverColor  = new Color(1f, 1f, 0.72f, 1f);

    public void OnPointerEnter(PointerEventData e)
    {
        controller?.ButtonHoverEnter();
        if (label) label.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData e)
    {
        controller?.ButtonHoverExit();
        if (label) label.color = normalColor;
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Left)
            controller?.OnHackClick();
    }
}

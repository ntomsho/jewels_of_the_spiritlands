using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class PopupText : MonoBehaviour
{
    TextMeshPro popupText;

    public void ActivatePopup(string textNum, Color textColor)
    {
        popupText = GetComponent<TextMeshPro>();
        popupText.color = textColor;
        popupText.text = textNum;
        StartTween();
    }

    void StartTween()
    {
        var sequence = DOTween.Sequence();
        sequence.Append(transform.DOLocalMoveY(transform.position.y + 1f, 0.25f)).SetEase(Ease.InQuad);
        sequence.Join(transform.DOScale(1.5f, 1f)).SetEase(Ease.OutBack);
        sequence.AppendInterval(1.5f);
        sequence.Append(popupText.DOFade(0, 1.5f));
        sequence.Join(transform.DOLocalMoveY(transform.position.y + 1.5f, 1.5f));
        sequence.OnComplete(() => Destroy(gameObject));
    }
}

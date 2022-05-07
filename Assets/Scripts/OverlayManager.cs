using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class OverlayManager : MonoBehaviour
{
    [Header("Turn Canvases")]
    [SerializeField] CanvasGroup playerTurnCanvas;
    [SerializeField] CanvasGroup enemyTurnCanvas;
    [SerializeField] TextMeshProUGUI enemyTurnCanvasText;
    
    [Header("Battle End Canvases")]
    [SerializeField] CanvasGroup victoryCanvas;
    [SerializeField] CanvasGroup defeatCanvas;
    
    [Header("Fade Out Canvas")]
    [SerializeField] CanvasGroup fadeOutCanvas;

    public void ShowTurnCanvas(bool playerTurn, System.Action OnComplete, Enemy enemy = null)
    {
        if (!playerTurn) enemyTurnCanvasText.text = enemy.enemyName + " Turn";
        CanvasGroup canvas = playerTurn ? playerTurnCanvas : enemyTurnCanvas;
        TextMeshProUGUI canvasText = canvas.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        canvasText.transform.localPosition = new Vector3(250f, 0f, 0f);
        var sequence = DOTween.Sequence();
        sequence.Append(canvas.DOFade(1f, 0.2f));
        sequence.Join(canvasText.transform.DOLocalMoveX(-50, 1.5f).SetEase(Ease.OutExpo));
        sequence.Append(canvas.DOFade(0f, 0.2f));
        sequence.Join(canvasText.transform.DOLocalMoveX(-250, 0.2f).SetEase(Ease.OutExpo));
        sequence.OnComplete(() => OnComplete());
    }

    public void ShowEndCanvas(bool playerWon)
    {
        CanvasGroup endCanvas = playerWon ? victoryCanvas : defeatCanvas;
        endCanvas.gameObject.SetActive(true);
        endCanvas.DOFade(1f, 1f);
    }

    public void ClearEndCanvases()
    {
        victoryCanvas.alpha = 0;
        defeatCanvas.alpha = 0;
        victoryCanvas.gameObject.SetActive(false);
        defeatCanvas.gameObject.SetActive(false);
    }
}

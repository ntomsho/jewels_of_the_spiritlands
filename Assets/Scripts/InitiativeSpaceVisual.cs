using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class InitiativeSpaceVisual : MonoBehaviour
{
    [Header("Initiative Space Visual")]
    [SerializeField] Image fullImage;
    [SerializeField] TextMeshProUGUI playerTurnText;
    [SerializeField] Image enemyTurnImage;

    [System.NonSerialized] public bool assigned;
    Enemy enemy = null;
    
    public void Initialize(int numberOfTurns/*, Vector2 destPos, float tweenInDelay*/)
    {
        fullImage.color = Color.white;
        SetPlayerTurnText(numberOfTurns);
        playerTurnText.enabled = true;
        enemyTurnImage.enabled = false;
        // TweenTo(destPos.x, tweenInDelay);
    }

    public void Initialize(Enemy _enemy/*, Vector2 destPos, float tweenInDelay*/)
    {
        fullImage.color = Color.white;
        enemy = _enemy;
        SetEnemyTurnImage(enemy.initSprite);
        playerTurnText.enabled = false;
        enemyTurnImage.enabled = true;
        // TweenTo(destPos.x, tweenInDelay);
    }

    void OnMouseDown()
    {
        if (enemy != null)
        {
            enemy.HandleTap();
        }
    }

    public void SetPlayerTurnText(int numberOfTurns)
    {
        playerTurnText.text = numberOfTurns.ToString();
    }

    public void SetEnemyTurnImage(Sprite enemySprite)
    {
        enemyTurnImage.sprite = enemySprite;
    }

    public Transform GetTransform()
    {
        return gameObject.transform;
    }
    
    public void Clear()
    {
        
        var sequence = DOTween.Sequence();
        sequence.Append(gameObject.transform.DOLocalMoveY(gameObject.transform.position.y + 1f, 0.5f));
        sequence.Join(fullImage.DOFade(0, 0.5f));
        sequence.OnComplete(() => {
            assigned = false;
            gameObject.SetActive(false);
        });
    }

    public void TweenTo(float newPos, float delay)
    {
        gameObject.transform.DOLocalMoveX(newPos, 0.25f).SetDelay(delay);
    }

    public void MergeTween(float newPos)
    {
        var sequence = DOTween.Sequence();
        sequence.Append(gameObject.transform.DOLocalMoveX(newPos, 0.5f));
        sequence.Join(fullImage.DOFade(0, 0.5f));
        sequence.OnComplete(() => Destroy(gameObject));
    }
}

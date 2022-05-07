using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public enum TileColor {Red, Yellow, Green, Blue, Purple};
public enum SpecialTile { None, ClearColumn, ClearRow, ColorBomb };
public enum TileStatus { Free, Frozen, Inactive, Locked };
//free == free, frozen == can be matched but not moved, inactive == can be matched, but doesn't count, locked == can't be matched or moved

//Implement new Tile prefab system to allow for tile status effects

public class Tile : MonoBehaviour
{
    [Header("Control")]
    [SerializeField] float dragThreshold = 20f;
    
    [Header("Sprites")]
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] SpriteRenderer iceSprite;
    [SerializeField] SpriteRenderer lockSprite;
    [SerializeField] SpriteRenderer hintSprite;
    [SerializeField] SpriteRenderer glowSprite;

    [Header("Particles")]
    [SerializeField] ParticleSystem inactiveParticle;
    [SerializeField] ParticleSystem frozenParticle;
    [SerializeField] ParticleSystem frozenShatterParticle;

    [Header("Audio")]
    [SerializeField] AudioClip inactiveSound;
    [SerializeField] AudioClip frozenSound;
    [SerializeField] AudioClip lockSound;

    [System.NonSerialized] public TileColor tileColor;
    [System.NonSerialized] public SpecialTile specialType;
    [System.NonSerialized] public BoardManager boardManager;
    [System.NonSerialized] public int x;
    [System.NonSerialized] public int y;
    [System.NonSerialized] public bool stable = true;
    [System.NonSerialized] public bool matched = false;
    [System.NonSerialized] public TileStatus status = TileStatus.Free;
    [System.NonSerialized] public bool hintOn = false;
    Vector2 touchPosition;
    [System.NonSerialized] public Vector3 dropDestination;
    bool isBeingDragged;
    Sequence hintTween;

    void Start()
    {
        iceSprite.enabled = false;
        lockSprite.enabled = false;
        hintSprite.enabled = false;
        glowSprite.color = new Color(1f,1f,1f,0f);
    }

    public void Setup(BoardManager _boardManager, TileColor _color, Sprite _sprite, SpecialTile _specialType)
    {
        boardManager = _boardManager;
        tileColor = _color;
        sprite.sprite = _sprite;
        specialType = _specialType;
        glowSprite.sprite = boardManager.glowSprites[(int)tileColor];
    }

    int[] GetBoardPos()
    {
        return new int[] {y, x};
    }

    void OnMouseDown()
    {
        if (!boardManager.canSwap)
        {
            return;
        }
        touchPosition = Input.mousePosition;
        isBeingDragged = true;
    }

    void OnMouseUp()
    {
        isBeingDragged = false;
    }

    void OnMouseDrag()
    {
        if (isBeingDragged)
        {
            if (Input.mousePosition.x < touchPosition.x - dragThreshold && x > 0)
            {
                InitiateSwap(boardManager.GetTileFromGrid(x - 1, y));
            }
            else if (Input.mousePosition.x > touchPosition.x + dragThreshold && x < boardManager.boardSize - 1)
            {
                InitiateSwap(boardManager.GetTileFromGrid(x + 1, y));
            }
            else if (Input.mousePosition.y < touchPosition.y - dragThreshold && y < boardManager.boardSize - 1)
            {
                InitiateSwap(boardManager.GetTileFromGrid(x, y + 1));
            }
            else if (Input.mousePosition.y > touchPosition.y + dragThreshold && y > 0)
            {
                InitiateSwap(boardManager.GetTileFromGrid(x, y - 1));
            }
        }
    }

    void InitiateSwap(Tile swappingTile)
    {
        isBeingDragged = false;
        boardManager.InitiateSwap(this, swappingTile);
    }

    public void MoveSpriteTo(Vector3 newPosition, float duration, bool checkForMatchAtEnd)
    {
        stable = false;
        StartCoroutine(MoveTween(newPosition, duration, checkForMatchAtEnd));
    }

    public void ClearTile()
    {
        StartCoroutine(ClearTileAnimation());
    }

    IEnumerator ClearTileAnimation()
    {
        boardManager.board[y, x] = null;
        ClearTileTween();
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
    }

    void ClearTileTween()
    {
        transform.DOScale(0, 0.25f).SetEase(Ease.InBack);
    }

    public void Drop(float dropAmount)
    {
        if (dropDestination == null || dropDestination.y == transform.localPosition.y || matched) return;
        if (transform.localPosition.y - dropAmount <= dropDestination.y)
        {
            transform.localPosition = dropDestination;
            stable = true;
            boardManager.CheckForBoardMatches(this);
        }
        else
        {
            transform.Translate(Vector3.down * dropAmount);
        }
    }

    public void ToggleGrayOut(bool value)
    {
        if (status != TileStatus.Inactive)
        {
            sprite.color = value ? Color.gray : Color.white;
        }
    }

    public void ApplyStatus(TileStatus newStatus)
    {
        if (newStatus != TileStatus.Free && status != TileStatus.Free) return;
        switch (newStatus)
        {
            case TileStatus.Inactive:
                ApplyInactive();
                break;
            case TileStatus.Frozen:
                ApplyFrozen();
                break;
            case TileStatus.Locked:
                ApplyLocked();
                break;
            default:
                switch (status)
                {
                    case TileStatus.Frozen:
                        RemoveFrozen();
                        break;
                    case TileStatus.Locked:
                        RemoveLocked();
                        break;
                    default:
                        sprite.color = Color.white;
                        break;
                }
                break;
        }
        status = newStatus;
    }

    void ApplyInactive()
    {
        Instantiate(inactiveParticle, gameObject.transform, false);
        GameManager.Instance.PlaySound(inactiveSound);
        sprite.DOColor(Color.gray, 1f);
    }

    void ApplyFrozen()
    {
        Instantiate(frozenParticle, gameObject.transform, false);
        GameManager.Instance.PlaySound(frozenSound);
        iceSprite.color = new Color(1f,1f,1f,0f);
        iceSprite.enabled = true;
        var tween = iceSprite.DOFade(1, 1f);
        tween.SetEase(Ease.OutExpo);
    }

    void ApplyLocked()
    {
        GameManager.Instance.PlaySound(lockSound);
        lockSprite.color = new Color(1f,1f,1f,0f);
        lockSprite.transform.localScale = new Vector2(1.2f, 1.2f);
        lockSprite.enabled = true;
        var sequence = DOTween.Sequence();
        sequence.Append(lockSprite.DOFade(1, 0.5f));
        sequence.Insert(0, lockSprite.transform.DOScale(Vector3.one, 0.5f));
        sequence.SetEase(Ease.InCirc);
    }

    void RemoveFrozen()
    {
        Instantiate(frozenShatterParticle, gameObject.transform, false);
        GameManager.Instance.PlaySound(frozenSound);
        var tween = iceSprite.DOFade(0, 0.5f);
        tween.OnComplete(() => iceSprite.enabled = false);
    }

    void RemoveLocked()
    {
        GameManager.Instance.PlaySound(lockSound);
        var sequence = DOTween.Sequence();
        sequence.Append(lockSprite.DOFade(0,0.25f));
        sequence.Join(lockSprite.transform.DOScale(new Vector2(1.2f, 1.2f), 0.25f));
        sequence.OnComplete(() => lockSprite.enabled = false);
    }

    public void ApplyHint()
    {
        hintSprite.enabled = true;
        hintTween = DOTween.Sequence();
        hintTween.Append(hintSprite.transform.DOScale(Vector3.one * 1.1f, 0.5f));
        hintTween.SetEase(Ease.InOutSine);
        hintTween.SetLoops(-1);
    }

    public void RemoveHint()
    {
        if (hintTween != null)
        {
            hintTween.Kill(true);
        }
        hintSprite.enabled = false;
    }

    public bool IsMatchable()
    {
        return (stable && !matched && status != TileStatus.Locked);
    }

    public bool IsMovable()
    {
        return (stable && !matched && status != TileStatus.Frozen && status != TileStatus.Locked);
    }

    public void StartGlow(Color glowColor)
    {
        // glowSprite.color = glowColor;
        var sequence = DOTween.Sequence();
        glowSprite.DOColor(glowColor, 0.1f);
        sequence.SetLoops(-1);
        sequence.SetEase(Ease.OutSine);
        sequence.Append(glowSprite.transform.DOScale(1.1f, 0.75f));
        sequence.SetEase(Ease.InOutSine);
        sequence.Append(glowSprite.transform.DOScale(0.9f, 0.75f));
    }

    void RemoveGlow()
    {
        glowSprite.DOFade(0f, 0.5f);
    }

    IEnumerator MoveTween(Vector3 newPosition, float duration, bool checkForMatchAtEnd)
    {
        var tween = transform.DOLocalMove(newPosition, duration);
        yield return tween.WaitForCompletion();
        stable = true;
        //Maybe don't need this conditional, just use this logic during swap?
        if (checkForMatchAtEnd)
        {
            boardManager.CheckForBoardMatches(this);
        }
    }
}

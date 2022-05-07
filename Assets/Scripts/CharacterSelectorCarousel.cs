using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class CharacterSelectorCarousel : MonoBehaviour
{
    [SerializeField] Transform panelTransform;

    [Header("Characters")]
    public List<CharacterTemplate> characterTemplates;
    public List<Sprite> characterSprites;

    [Header("Party Preview")]
    [SerializeField] PartyPreview partyPreview;
    [SerializeField] Vector3[] spritePositions;

    [Header("Carousel")]
    [SerializeField] List<Image> spriteContainers;
    [SerializeField] CanvasGroup difficultyButtons;
    [SerializeField] float carouselSpeed;
    [SerializeField] GameObject[] gemButtons;
    [SerializeField] GameObject[] gemButtonsInactive;
    [SerializeField] AudioClip scrollSFX;
    [SerializeField] AudioClip assignSFX;

    [Header("Character Info")]
    [SerializeField] TextMeshProUGUI classNameText;
    [SerializeField] GameObject classAttackContainer;
    [SerializeField] GameObject classSpecialContainer;
    [SerializeField] TextMeshProUGUI classAttackText;
    [SerializeField] TextMeshProUGUI classSpecialText;

    [Header("Guard and Spirit")]
    [SerializeField] CanvasGroup guardSpiritGroup;
    [SerializeField] TextMeshProUGUI startingGuardText;
    [SerializeField] TextMeshProUGUI spiritModText;
    
    int selectorIndex = 2;
    bool canChange = true;
    
    void Start()
    {
        GameManager.Instance.selector = this;
        Setup();
    }

    public void Setup()
    {
        for (int ind = 0; ind < 5; ind++)
        {
            // gemButtons[ind].GetComponent<Image>().color = Color.gray;
            gemButtonOpenTween(ind);
            int checkInd = GetSpriteIndexFromSelector(-2 + ind);
            Image container = spriteContainers[ind];
            // container.sprite = characterSprites[ind];
            SetSprite(container, ind);
            if (checkInd != selectorIndex)
            {
                container.color = Color.gray;
                container.transform.localScale = new Vector3(4f, 4f, 1f);
            }
            else
            {
                container.transform.localScale = new Vector3(5f, 5f, 1f);
            }
        }
        SetClassText();

        for (int ind = 0; ind < GameManager.Instance.party.Length; ind++)
        {
            if (GameManager.Instance.party[ind] != null)
            {
                AutoSelect(GameManager.Instance.party[ind], ind);
            }
        }

        panelTransform.DOMoveX(5.5f, 1f).SetEase(Ease.OutBack).SetDelay(0f);
    }

    void SetSprite(Image container, int spriteIndex)
    {
        container.sprite = characterSprites[spriteIndex];
        container.transform.localPosition = new Vector2(container.transform.localPosition.x, characterTemplates[spriteIndex].characterPrefab.GetComponent<PartyMember>().groundPlaneMod);
    }

    int GetSpriteIndexFromSelector(int offset)
    {
        int checkInd = selectorIndex + offset;
        if (checkInd < 0)
        {
            checkInd = characterSprites.Count + checkInd;
        }
        else if (checkInd >= characterSprites.Count)
        {
            checkInd = checkInd - characterSprites.Count;
        }
        return checkInd;
    }

    void SetClassText()
    {
        PartyMember selected = characterTemplates[selectorIndex].characterPrefab.GetComponent<PartyMember>();
        classNameText.text = selected.characterClass.ToString();
        int? selectedPartyIndex = partyPreview.GetPartyMemberIndex(characterTemplates[selectorIndex]);
        classAttackText.text = selected.ReturnFormattedDescText(selected.attackDescription, selectedPartyIndex);
        classSpecialText.text = selected.ReturnFormattedDescText(selected.specialDescription, selectedPartyIndex);
        StartCoroutine(RebuildLayout());

        if (selectedPartyIndex == null)
        {
            guardSpiritGroup.alpha = 0;
        }
        else
        {
            startingGuardText.text = "Starting Guard: <b>" + GameManager.Instance.startingGuard[(int)selectedPartyIndex] + "</b>";
            spiritModText.text = "Spirit per Match: <b>" + GameManager.Instance.spiritMod[(int)selectedPartyIndex] + "%</b>";
            guardSpiritGroup.alpha = 1;
        }
    }

    void MoveSpriteContainer(Image movingSprite, int destinationInd, bool instant)
    {
        if (instant)
        {
            movingSprite.transform.localPosition = new Vector2(spritePositions[destinationInd].x, movingSprite.transform.localPosition.y);
        }
        else
        {
            var sequence = DOTween.Sequence();
            sequence.Append(movingSprite.transform.DOLocalMoveX(spritePositions[destinationInd].x, carouselSpeed));
            sequence.Join(movingSprite.transform.DOScale(destinationInd == 2 ? new Vector3(5f, 5f, 1f) : new Vector3(4f, 4f, 1f), carouselSpeed));
        }

        movingSprite.color = (destinationInd == 2 ? Color.white : Color.gray);
    }

    public void ChangeSelectorIndex(bool inc)
    {
        if (!canChange) return;
        canChange = false;

        GameManager.Instance.PlaySound(scrollSFX);

        if (inc)
        {
            selectorIndex = selectorIndex == characterSprites.Count - 1 ? 0 : selectorIndex + 1;
        }
        else
        {
            selectorIndex = selectorIndex == 0 ? characterSprites.Count - 1 : selectorIndex - 1;
        }
        
        Image movingContainer = null;
        
        for (int ind = 0; ind < spriteContainers.Count; ind++)
        {
            Image container = spriteContainers[ind];
            int destInd;
            bool instant = false;
            if (ind == 0 && inc)
            {
                destInd = spriteContainers.Count - 1;
                instant = true;
                // spriteContainers[ind].sprite = characterSprites[GetSpriteIndexFromSelector(2)];
                SetSprite(spriteContainers[ind], GetSpriteIndexFromSelector(2));
                movingContainer = spriteContainers[ind];
            }
            else if (ind == spriteContainers.Count - 1 && !inc)
            {
                destInd = 0;
                instant = true;
                // spriteContainers[ind].sprite = characterSprites[GetSpriteIndexFromSelector(-2)];
                SetSprite(spriteContainers[ind], GetSpriteIndexFromSelector(-2));
                movingContainer = spriteContainers[ind];
            }
            else
            {
                destInd = ind + (inc ? -1 : 1);
            }
            MoveSpriteContainer(spriteContainers[ind], destInd, instant);
        }

        spriteContainers.Remove(movingContainer);
        if (inc)
        {
            spriteContainers.Add(movingContainer);
        }
        else 
        {
            spriteContainers.Insert(0, movingContainer);
        }
        StartCoroutine(SetCanChange());
        SetClassText();
    }

    public void SelectForGem(int gemInd)
    {
        GameManager.Instance.PlaySound(assignSFX);
        partyPreview.AddPartyMember(characterTemplates[selectorIndex], gemInd);
        for (int ind = 0; ind < gemButtons.Length; ind++)
        {
            if (partyPreview.characterTemplates[ind] == null)
            {
                gemButtons[ind].SetActive(false);
                gemButtonsInactive[ind].SetActive(true);
            }
            else
            {
                gemButtons[ind].SetActive(true);
                gemButtonsInactive[ind].SetActive(false);
            }

            // Image buttonImage = gemButtons[ind].GetComponent<Image>();
            // buttonImage.color = partyPreview.characterTemplates[ind] == null ? Color.gray : Color.white;
            // if (partyPreview.characterTemplates[ind] == null)
            // {
            //     // gemButtonOpenTween(ind);
            // }
            // else
            // {
            //     // GemButtonKillTweens(ind);
            // }
        }
        SetClassText();
        SetDifficultyButtonsActive(partyPreview.PartyFull());
    }

    void AutoSelect(CharacterTemplate template, int gemInd)
    {
        partyPreview.AddPartyMember(template, gemInd);
        for (int ind = 0; ind < gemButtons.Length; ind++)
        {
            if (partyPreview.characterTemplates[ind] == null)
            {
                gemButtons[ind].SetActive(false);
                gemButtonsInactive[ind].SetActive(true);
            }
            else
            {
                gemButtons[ind].SetActive(true);
                gemButtonsInactive[ind].SetActive(false);
            }

            // Image buttonImage = gemButtons[ind].GetComponent<Image>();
            // buttonImage.color = partyPreview.characterTemplates[ind] == null ? Color.gray : Color.white;
            // if (partyPreview.characterTemplates[ind] == null)
            // {
            //     // gemButtonOpenTween(ind);
            // }
            // else
            // {
            //     // GemButtonKillTweens(ind);
            // }
        }
    }

    void SetDifficultyButtonsActive(bool partyFull)
    {
        difficultyButtons.alpha = partyFull ? 1 : 0;
    }

    void gemButtonOpenTween(int gemInd)
    {
        gemButtonsInactive[gemInd].GetComponent<RectTransform>().DOScale(1.1f, 0.5f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
    }

    public void TempStart(int diff)
    {
        List<CharacterTemplate> random = new List<CharacterTemplate>();
        while (random.Count < 5)
        {
            CharacterTemplate character = characterTemplates[Random.Range(0,5)];
            while (random.Contains(character))
            {
                character = characterTemplates[Random.Range(0,5)];
            }
            random.Add(character);
        }
        partyPreview.characterTemplates = random.ToArray();
        StartBattle(diff);
    }

    public void StartBattle(int difficulty)
    {
        if (!partyPreview.PartyFull()) return;
        GameManager.Instance.party = partyPreview.characterTemplates;
        GameManager.Instance.StartBattle(difficulty);
    }

    IEnumerator SetCanChange()
    {
        yield return new WaitForSeconds(carouselSpeed + 0.05f);
        canChange = true;
    }

    IEnumerator RebuildLayout()
    {
        yield return new WaitForSeconds(0.01f);
        classAttackContainer.transform.GetChild(0).gameObject.SetActive(false);
        classSpecialContainer.transform.GetChild(0).gameObject.SetActive(false);
        yield return 0;
        classAttackContainer.transform.GetChild(0).gameObject.SetActive(true);
        classSpecialContainer.transform.GetChild(0).gameObject.SetActive(true);
        classAttackContainer.GetComponent<ScrollRect>().verticalNormalizedPosition = 1f;
        classSpecialContainer.GetComponent<ScrollRect>().verticalNormalizedPosition = 1f;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class StatusManager : MonoBehaviour
{
    [SerializeField] GameObject statusEffectPrefab; 
    List<GameObject> icons = new List<GameObject>();
    [SerializeField] float iconMargin;

    CharacterEntity character;
    [System.NonSerialized] public Dictionary<StatusEffect, GameObject> effectIcons;
    float iconWidth;

    void Start()
    {
        effectIcons = new Dictionary<StatusEffect, GameObject>();
        iconWidth = statusEffectPrefab.GetComponent<RectTransform>().rect.width;
    }

    public void AddNewIcon(StatusEffect baseEffect)
    {
        GameObject newIcon = Instantiate(statusEffectPrefab, gameObject.transform);
        // newIcon.transform.localPosition = new Vector2(-1 * GetRightOffset() - 1f, -1.75f);
        newIcon.transform.localPosition = new Vector2(GetIconsStartX() - iconWidth - iconMargin, -1.75f);
        newIcon.GetComponent<Image>().sprite = baseEffect.icon;
        newIcon.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = baseEffect.NumToDisplay().ToString();
        icons.Add(newIcon);
        effectIcons[baseEffect] = newIcon;
        ReorderIcons();
    }

    float GetIconsHalfWidth()
    {
        return (icons.Count * iconWidth + (icons.Count - 1) * iconMargin) / 2f;
    }

    float GetIconsStartX()
    {
        int amtMod = (icons.Count - 1) / 2;
        return 0 - (amtMod * iconWidth + amtMod * iconMargin);
    }

    float GetIconDestX(int iconInd)
    {
        float percentage = (iconInd + 1f) / (icons.Count + 1f);
        float answer = Mathf.Lerp(GetIconsHalfWidth(), -GetIconsHalfWidth(), percentage);
        return answer;
    }

    public void UpdateIconNumber(StatusEffect baseEffect, int newNum)
    {
        Transform textTransform = effectIcons[baseEffect].transform.GetChild(0);
        TextMeshProUGUI textObj = textTransform.GetComponent<TextMeshProUGUI>();
        var sequence = DOTween.Sequence();
        sequence.Append(textTransform.DOLocalMoveY(textTransform.position.y + 1f, 0.2f));
        sequence.Join(textObj.DOFade(0f, 0.2f));
        sequence.AppendCallback(() => {
            textTransform.Translate(Vector3.down * 2f);
            textObj.text = newNum.ToString();
        });
        sequence.Append(textTransform.DOLocalMoveY(0, 0.2f));
        sequence.Join(textObj.DOFade(1f, 0.2f));
    }

    public void RemoveIcon(StatusEffect baseEffect)
    {
        //this is not working correctly
        GameObject icon = effectIcons[baseEffect];
        icons.Remove(icon);
        effectIcons.Remove(baseEffect);
        var sequence = DOTween.Sequence();
        sequence.Append(icon.transform.DOLocalMoveY(icon.transform.localPosition.y + 0.75f, 0.25f));
        sequence.Join(icon.GetComponent<Image>().DOFade(0f, 0.25f));
        sequence.OnComplete(() => Destroy(icon.gameObject));
        ReorderIcons();
    }

    void ReorderIcons()
    {
        for (int ind = 0; ind < icons.Count; ind++)
        {
            icons[ind].transform.DOLocalMoveX(GetIconDestX(ind), 0.25f);
        }
    }

    // float GetRightOffset()
    // {
    //     return (iconWidth * icons.Count + iconMargin * (icons.Count - 1)) / 2;
    // }

    // float GetIconDestX(int iconInd)
    // {
    //     //figure this out later
    //     return GetRightOffset() - (iconWidth * (iconInd + 1) + iconInd * iconMargin);
    // }
}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InfoManager : MonoBehaviour
{
    [Header("Info Manager")]
    [SerializeField] ScrollRect scrollRect;
    [SerializeField] Transform textContainerTransform;
    [SerializeField] TextMeshProUGUI infoCanvasText;
    [SerializeField] GameObject statusContainerPrefab;
    List<GameObject> statusContainers = new List<GameObject>();
    void Start()
    {
        GameManager.Instance.infoManager = this;
        gameObject.SetActive(false);
    }

    public void SetInfo(PartyMember target)
    {
        SetActive(true);
        if (target != null)
        {
            DestroyStatusContainers();
            string[] headers = new string[] { "Attack", "Special" };
            string[] bodies = new string[] { target.ReturnFormattedDescText(target.attackDescription, (int)target.tileColor), target.ReturnFormattedDescText(target.specialDescription, (int)target.tileColor) };
            infoCanvasText.text = FormatInfoText(headers, bodies);
            AddStatusContainers(target.statusEffects);
            StartCoroutine(RebuildLayout());
        }
    }
    public void SetInfo(Enemy target)
    {
        SetActive(true);
        if (target != null)
        {
            DestroyStatusContainers();
            string[] headers = new string[] { target.enemyName };
            string[] bodies = new string[] { target.ReturnFormattedDescText(target.enemyDescription) };
            infoCanvasText.text = FormatInfoText(headers, bodies);
            AddStatusContainers(target.statusEffects);
            StartCoroutine(RebuildLayout());
        }
    }

    public void SetInfo(string specialText)
    {
        SetActive(true);
        DestroyStatusContainers();
        infoCanvasText.text = specialText;
        StartCoroutine(RebuildLayout());
    }

    public void SetActive(bool active)
    {
        // scrollRect.normalizedPosition = new Vector2(0f, 1f);
        if (gameObject.activeSelf != active)
        {
            DestroyStatusContainers();
            gameObject.SetActive(active);
        }
    }

    GameObject CreateStatusContainer (StatusEffect statusEffect)
    {
        GameObject container = Instantiate(statusContainerPrefab, scrollRect.transform);
        statusContainers.Add(container);
        return container;
    }

    void DestroyStatusContainers()
    {
        foreach(GameObject container in statusContainers)
        {
            Destroy(container);
        }
    }

    string FormatInfoText(string[] headers, string[] bodies)
    {
        string finalString = "";
        for (int ind = 0; ind < headers.Length; ind++)
        {
            if (ind != 0)
            {
                finalString += "\n";
            }
            finalString += "<b>" + headers[ind] + "</b>: ";
            finalString += bodies[ind];
        }
        return finalString;
    }

    void AddStatusContainers(List<StatusEffect> statusEffects)
    {
        foreach(StatusEffect statusEffect in statusEffects)
        {
            GameObject container = Instantiate(statusContainerPrefab, textContainerTransform, false);
            container.transform.GetChild(0).GetComponent<Image>().sprite = statusEffect.icon;
            string statusText = "<b>" + statusEffect.effectName + "</b>: " + statusEffect.description;
            TextMeshProUGUI textObj = container.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            textObj.text = statusText;
            statusContainers.Add(container);
        }
    }

    public void Cancel()
    {
        if (GameManager.Instance.battleManager.specialTargeting)
        {
            GameManager.Instance.battleManager.ToggleSpecialTargeting(0, true);
        }
        SetActive(false);
    }

    IEnumerator RebuildLayout()
    {
        yield return new WaitForSeconds(0.01f);
        textContainerTransform.gameObject.SetActive(false);
        yield return 0;
        textContainerTransform.gameObject.SetActive(true);
        scrollRect.verticalNormalizedPosition = 1f;
    }
}

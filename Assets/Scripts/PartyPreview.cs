using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PartyPreview : MonoBehaviour
{
    GameObject[] previewPrefabs = new GameObject[5];
    [System.NonSerialized] public CharacterTemplate[] characterTemplates = new CharacterTemplate[5];
    
    public bool PartyFull()
    {
        foreach(CharacterTemplate character in characterTemplates)
        {
            if (character == null)
            {
                return false;
            }
        }
        return true;
    }
    public void AddPartyMember(CharacterTemplate template, int gemIndex)
    {
        if (characterTemplates[gemIndex])
        {
            Destroy(previewPrefabs[gemIndex].gameObject);
        }

        for (int ind = 0; ind < characterTemplates.Length; ind++)
        {
            if (characterTemplates[ind] == template)
            {
                characterTemplates[ind] = null;
                Destroy(previewPrefabs[ind].gameObject);
                break;
            }
        }

        characterTemplates[gemIndex] = template;
        previewPrefabs[gemIndex] = Instantiate(template.previewPrefab, GameManager.Instance.partyPositions[gemIndex], Quaternion.identity);
        previewPrefabs[gemIndex].transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = GameManager.Instance.gemSprites[gemIndex];
    }

    public int? GetPartyMemberIndex(CharacterTemplate template)
    {
        for (int ind = 0; ind < characterTemplates.Length; ind++)
        {
            if (characterTemplates[ind] && characterTemplates[ind] == template)
            {
                return ind;
            }
        }
        return null;
    }
}

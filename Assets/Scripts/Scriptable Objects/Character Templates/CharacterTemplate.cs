using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "Character Template", order = 52)]
public class CharacterTemplate : ScriptableObject
{
    public GameObject characterPrefab;
    public GameObject previewPrefab;
    [System.NonSerialized] public int health;
    public List<CharacterUpgrade> characterUpgrades;
    [System.NonSerialized] public bool knockedOut = false;
}


[CreateAssetMenu(fileName = "Data", menuName = "Character Upgrade", order = 53)]
public class CharacterUpgrade : ScriptableObject
{
    public CharacterClass characterClass;
    public string upgradeName;
    [TextArea] public string upgradeDescription;
}

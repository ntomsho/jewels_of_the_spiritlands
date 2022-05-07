using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusEffect : ScriptableObject
{
    [Header("Main")]
    [System.NonSerialized] public bool isToken;
    public string effectName;
    [TextArea] public string description;

    [Header("Visuals")]
    public Sprite icon;
    public Color spriteColor;

    [Header("Attack Modifiers")]
    public int powerChange;
    public float powerMod;
    public float missChance;

    [Header("Defense Modifiers")]
    public int damageTakenChange;
    public float damageTakenMod;
    public float dodgeChance;

    [Header("Damage over Time")]
    public int damageToApply;

    public virtual bool IsToken()
    {
        return false;
    }

    public virtual int NumToDisplay()
    {
        return 0;
    }
}

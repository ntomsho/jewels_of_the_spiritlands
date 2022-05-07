using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "Status Effect/Status Token", order = 51)]
public class StatusToken : StatusEffect
{
    [System.NonSerialized] new public bool isToken = true;
    [Header("Token Settings")]
    public int maxStacks;
    [System.NonSerialized] public int numStacks = 1;

    public override bool IsToken()
    {
        return true;
    }
    public override int NumToDisplay()
    {
        return numStacks;
    }
}

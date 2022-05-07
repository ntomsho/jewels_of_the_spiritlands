using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "Status Effect/Timed Status", order = 51)]
public class TimedStatus : StatusEffect
{
    [System.NonSerialized] new public bool isToken = false;
    [Header("Duration Settings")]
    public int duration;
    [System.NonSerialized] public int durationTimer = 0;
    public override int NumToDisplay()
    {
        return duration - durationTimer;
    }
}

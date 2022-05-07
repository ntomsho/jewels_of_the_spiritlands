using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Witch : PartyMember
{
    [Header("Attack Settings")]
    public StatusEffect curseBase;
    public int baseDamage;
    public int[] baseDurationMod;

    [Header("Special Settings")]
    public StatusEffect specialCurseBase;
    public int specialDamage;

    public override string GetAttrString(string replaceStr, int ?positionTileColor)
    {
        switch (replaceStr)
        {
            case "baseDamage":
                return baseDamage.ToString();
            case "baseDurationMod":
                return positionTileColor == null ? "" + baseDurationMod[0] + "-" + baseDurationMod[4] : baseDurationMod[(int)positionTileColor].ToString();
            case "specialDamage":
                return specialDamage.ToString();
            default:
                return base.GetAttrString(replaceStr, positionTileColor);
        }
    }

    public override List<CharacterEntity> GetSpecialTargets()
    {
        List<CharacterEntity> availableTargets = new List<CharacterEntity>();
        foreach(Enemy target in battleManager.enemies)
        {
            if (!target.dead)
            {
                availableTargets.Add(target);
            }
        }
        return availableTargets;
    }

    public override void ActivateSpecial(CharacterEntity target)
    {
        base.ActivateSpecial(target);
        int damage = specialDamage;
        List<Enemy> targets = new List<Enemy>();
        foreach(Enemy enemy in battleManager.enemies)
        {
            if (enemy.HasStatusEffect("Curse"))
            {
                targets.Add(enemy);
            }
        }
        StartCoroutine(ResolveSpecial(damage, (Enemy) target));
        // List<Enemy> targets = GetTargets();
        // if (targets.Count > 0)
        // {
        //     Instantiate(attackParticle, targets[0].transform.position, Quaternion.identity);
        //     TimedStatus curse = (TimedStatus) StatusEffect.Instantiate(curseBase);
        //     curse.duration = specialCurseDuration;
        //     targets[0].AddStatusEffect(curse);
        // }
    }

    public override void InitiateAction(int matchValue, List<Enemy> targets)
    {
        int damage = baseDamage;
        int curseDuration = baseDurationMod[(int)tileColor] + matchValue;
        StartCoroutine(ResolveAction(damage, curseDuration));
    }

    IEnumerator ResolveSpecial(int damage, Enemy target)
    {
        AnimatorStateInfo animState = animator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(animState.length);
        Instantiate(attackParticle, target.transform.position, Quaternion.identity);
        GameManager.Instance.PlaySound(specialSFX);

        int durationBonus = 0;
        if (target.HasStatusEffect("Curse"))
        {
            StatusEffect existingCurse = target.GetStatusEffectByName("Curse");
            durationBonus += existingCurse.NumToDisplay();
            target.RemoveStatusEffect(existingCurse);
        }
        TimedStatus specialCurse = (TimedStatus) StatusEffect.Instantiate(specialCurseBase);
        specialCurse.duration += durationBonus;
        target.AddStatusEffect(specialCurse);

        yield return new WaitForSeconds(0.1f);
        StartCoroutine(EndSpecial(false));
    }

    IEnumerator ResolveAction(int damage, int curseDuration)
    {
        AnimatorStateInfo animState = animator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(animState.length);
        GameManager.Instance.PlaySound(hitSFX);
        if (battleManager.targetedEnemy != null)
        {
            Instantiate(attackParticle, battleManager.targetedEnemy.transform.position, Quaternion.identity);
            TimedStatus curse = (TimedStatus) StatusEffect.Instantiate(curseBase);
            curse.duration = curseDuration;
            battleManager.targetedEnemy.AddStatusEffect(curse);
            gemChargeParticle.Stop();
            StartCoroutine(EndAction(false));
        }
    }
}

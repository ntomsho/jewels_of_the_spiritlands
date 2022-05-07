using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Huntress : PartyMember
{
    [Header("Attack Settings")]
    public int[] baseDamageMod;

    [Header("Special Settings")]
    public int specialDamage;
    
    public override string GetAttrString(string replaceStr, int ?positionTileColor)
    {
        switch (replaceStr)
        {
            case "baseDamageMod":
                return positionTileColor == null ? "" + baseDamageMod[0] + "-" + baseDamageMod[4] : baseDamageMod[(int)positionTileColor].ToString();
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
        StartCoroutine(ResolveSpecial(specialDamage, (Enemy) target));
    }

    public override void InitiateAction(int matchValue, List<Enemy> targets)
    {
        int damage = matchValue * baseDamageMod[(int)tileColor];
        StartCoroutine(ResolveAction(damage, targets[0]));
    }

    IEnumerator PlaySpecialSFX()
    {
        for (int ind = 0; ind < 3; ind++)
        {
            GameManager.Instance.PlaySound(specialSFX);
            yield return new WaitForSeconds(0.05f);
        }
    }

    IEnumerator ResolveSpecial(int damage, Enemy target)
    {
        AnimatorStateInfo animState = animator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(animState.length);
        GameManager.Instance.PlaySound(specialSFX);
        Instantiate(attackParticle, target.transform.position, Quaternion.identity);
        bool kill = target.TakeDamage(damage);
        StartCoroutine(EndSpecial(kill));
    }

    IEnumerator ResolveAction(int damage, Enemy target)
    {
        // yield return new WaitForSeconds(1.2f);
        AnimatorStateInfo animState = animator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(animState.length);
        GameManager.Instance.PlaySound(hitSFX);
        bool kill = false;
        if (target != null)
        {
            Instantiate(attackParticle, target.transform.position, Quaternion.identity);
            kill = target.TakeDamage(ProcessDamageDealt(damage));
        }
        gemChargeParticle.Stop();
        StartCoroutine(EndAction(kill));
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Knight : PartyMember
{
    [Header("Attack Settings")]
    public int[] baseDamageMod;
    public int[] baseGuardMod;

    [Header("Special Settings")]
    [SerializeField] ParticleSystem guardParticle;
    public int specialGuardAmount;

    public override string GetAttrString(string replaceStr, int ?positionTileColor)
    {
        switch (replaceStr)
        {
            case "baseDamageMod":
                return positionTileColor == null ? "" + baseDamageMod[0] + "-" + baseDamageMod[4] : baseDamageMod[(int)positionTileColor].ToString();
            case "baseGuard":
                return positionTileColor == null ? "" + baseGuardMod[0] + "-" + baseGuardMod[4] : baseGuardMod[(int)positionTileColor].ToString();
            default:
                return base.GetAttrString(replaceStr, positionTileColor);
        }
    }

    public override List<CharacterEntity> GetSpecialTargets()
    {
        List<CharacterEntity> availableTargets = new List<CharacterEntity>();
        foreach(PartyMember target in battleManager.partyMembers)
        {
            if (!target.knockedOut)
            {
                availableTargets.Add(target);
            }
        }
        return availableTargets;
    }

    public override void ActivateSpecial(CharacterEntity target)
    {
        base.ActivateSpecial(target);
        StartCoroutine(ResolveSpecial(specialGuardAmount, (PartyMember) target));
    }

    public override void InitiateAction(int matchValue, List<Enemy> targets)
    {
        int damage = matchValue * baseDamageMod[(int)tileColor];
        int guard = matchValue * baseGuardMod[(int)tileColor];
        StartCoroutine(ResolveAction(damage, guard, targets[0], battleManager.GetMostDamagedPartyMember()));
    }

    public override List<Enemy> GetTargets()
    {
        for (int ind = 0; ind < battleManager.enemies.Count; ind++)
        {
            if (battleManager.enemies[ind] != null && !battleManager.enemies[ind].dead)
            {
                return new List<Enemy> { battleManager.enemies[ind] };
            }
        }
        return null;
    }

    IEnumerator ResolveSpecial(int guard, PartyMember target)
    {
        AnimatorStateInfo animState = animator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(animState.length);
        GameManager.Instance.PlaySound(specialSFX);
        Instantiate(guardParticle, target.transform.position, Quaternion.identity);
        target.GainGuard(guard);
        yield return new WaitForSeconds(0.1f);
        StartCoroutine(EndSpecial(false));
    }

    IEnumerator ResolveAction(int damage, int guard, Enemy target, PartyMember guardTarget)
    {
        // yield return new WaitForSeconds(0.5f);
        AnimatorStateInfo animState = animator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(animState.length);
        GameManager.Instance.PlaySound(hitSFX);
        bool kill = false;
        if (target != null)
        {
            Instantiate(attackParticle, target.transform.position, Quaternion.identity);
            kill = target.TakeDamage(ProcessDamageDealt(damage));
        }
        GainGuard(guard);
        gemChargeParticle.Stop();
        StartCoroutine(EndAction(kill));
    }
}

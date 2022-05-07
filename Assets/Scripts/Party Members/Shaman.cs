using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shaman : PartyMember
{
    [Header("Attack Settings")]
    public int[] baseDamageMod;

    [Header("Special Settings")]
    [SerializeField] ParticleSystem spiritParticle;
    [SerializeField] StatusToken spiritToken;

    public override string GetAttrString(string replaceStr, int ?positionTileColor)
    {
        switch (replaceStr)
        {
            case "baseDamageMod":
                return positionTileColor == null ? "" + baseDamageMod[0] + "-" + baseDamageMod[4] : baseDamageMod[(int)positionTileColor].ToString();;
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
        StartCoroutine(ResolveSpecial((PartyMember) target));
    }

    public override void InitiateAction(int matchValue, List<Enemy> targets)
    {
        int damage = matchValue * baseDamageMod[(int)tileColor];
        StartCoroutine(ResolveAction(damage, targets[0]));
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

    IEnumerator ResolveSpecial(PartyMember target)
    {
        AnimatorStateInfo animState = animator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(animState.length);
        Instantiate(spiritParticle, target.transform.position, Quaternion.identity);
        GameManager.Instance.PlaySound(specialSFX);
        target.AddStatusEffect(spiritToken);
        yield return new WaitForSeconds(0.1f);
        StartCoroutine(EndSpecial(false));
    }

    IEnumerator ResolveAction(int damage, Enemy target)
    {
        // yield return new WaitForSeconds(1f);
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

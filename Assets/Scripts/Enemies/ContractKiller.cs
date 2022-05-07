using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContractKiller : Enemy
{
    [Header("Attack Settings")]
    [SerializeField] int minDamage;
    [SerializeField] int maxDamage;
    [SerializeField] TimedStatus contractStatusPrefab;
    [SerializeField] ParticleSystem contractParticle;
    [SerializeField] AudioClip contractSFX;
    bool targetHasContract;

    public override void PlayHitSound()
    {
        if (targetHasContract)
        {
            GameManager.Instance.PlaySound(hitSFX);
        }
    }

    public override PartyMember GetTarget(int[] weights)
    {
        foreach (PartyMember pm in battleManager.partyMembers)
        {
            if (pm.HasStatusEffect("Assassination Contract"))
            {
                targetHasContract = true;
                return pm;
            }
        }
        targetHasContract = false;
        return base.GetTarget(weights);
    }

    public override void InitiateAction()
    {
        animator.SetTrigger("callAttack");
        PartyMember target = GetTarget(new int[] { 25, 25, 15, 10, 10 });
        GameManager.Instance.PlaySound(targetHasContract ? attackSFX : contractSFX);
        if (target != null)
        {
            StartCoroutine(ResolveAction(target));
        }
        else
        {
            StartCoroutine(EndAction());
        }
    }

    IEnumerator ResolveAction(PartyMember target)
    {
        AnimatorStateInfo animState = animator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(animState.length);
        if (target != null)
        {
            ParticleSystem particle;
            if (!targetHasContract)
            {
                particle = contractParticle;
                target.AddStatusEffect(Instantiate(contractStatusPrefab));
            }
            else
            {
                particle = attackParticle;
                target.TakeDamage(ProcessDamageDealt(Random.Range(minDamage, maxDamage)));
            }
            Instantiate(particle, target.transform.position, Quaternion.identity);
        }
        StartCoroutine(EndAction());
    }
}

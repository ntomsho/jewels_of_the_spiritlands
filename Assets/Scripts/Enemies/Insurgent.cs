using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Insurgent : Enemy
{
    [Header("Attack Settings")]
    [SerializeField] int minDamage;
    [SerializeField] int maxDamage;
    [SerializeField] StatusToken concealmentPrefab;

    public override void InitiateAction()
    {
        animator.SetTrigger("callAttack");
        GameManager.Instance.PlaySound(attackSFX);
        PartyMember target = GetTarget(new int[] { 10, 10, 20, 30, 30 });
        if (!HasStatusEffect("Concealment"))
        {
            StatusToken concealmentToken = (StatusToken) StatusEffect.Instantiate(concealmentPrefab);
            AddStatusEffect(concealmentToken);
            target = GetTarget(new int[] { 30, 30, 20, 10, 10 });
        }
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
            Instantiate(attackParticle, target.transform.position, Quaternion.identity);
            target.TakeDamage(ProcessDamageDealt(Random.Range(minDamage, maxDamage)));
        }
        StartCoroutine(EndAction());
    }
}

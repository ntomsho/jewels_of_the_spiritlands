using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Revenant : Enemy
{
    [Header("Attack Settings")]
    [SerializeField] int minDamage;
    [SerializeField] int maxDamage;
    public override void InitiateAction()
    {
        animator.SetTrigger("callAttack");
        GameManager.Instance.PlaySound(attackSFX);
        PartyMember target = GetTarget(new int[] { 45, 30, 15, 5, 5 });
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

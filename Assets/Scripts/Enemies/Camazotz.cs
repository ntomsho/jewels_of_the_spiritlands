using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camazotz : Enemy
{
    [Header("Attack Settings")]
    [SerializeField] int minDamage;
    [SerializeField] int maxDamage;
    [SerializeField] int tileStatusChance;
    public override void InitiateAction()
    {
        animator.SetTrigger("callAttack");
        GameManager.Instance.PlaySound(attackSFX);
        PartyMember target = GetTarget(new int[] { 5, 10, 15, 35, 35 });
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
            if (Random.Range(1,100) <= tileStatusChance)
            {
                battleManager.boardManager.ApplyTileStatus(battleManager.boardManager.GetTilesOfColor(target.tileColor, 1), TileStatus.Inactive);
            }
        }
        battleManager.NextTurn();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coldcap : Enemy
{
    [Header("Attack Settings")]
    [SerializeField] int minDamage;
    [SerializeField] int maxDamage;
    [SerializeField] int tileStatusChance;
    
    public List<PartyMember> GetTargets()
    {
        List<PartyMember> targets = new List<PartyMember>();
        foreach(PartyMember pm in battleManager.partyMembers)
        {
            if (!pm.knockedOut)
            {
                targets.Add(pm);
                if (targets.Count == 2)
                {
                    break;
                }
            }
        }
        return targets;
    }
    
    public override void InitiateAction()
    {
        animator.SetTrigger("callAttack");
        GameManager.Instance.PlaySound(attackSFX);
        List<PartyMember> targets = GetTargets();
        if (targets.Count > 0)
        {
            StartCoroutine(ResolveAction(targets));
        }
        else
        {
            StartCoroutine(EndAction());
        }
    }

    IEnumerator ResolveAction(List<PartyMember> targets)
    {
        AnimatorStateInfo animState = animator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(animState.length);
        foreach(PartyMember target in targets)
        {
            Instantiate(attackParticle, target.transform.position, Quaternion.identity);
            target.TakeDamage(ProcessDamageDealt(Random.Range(minDamage, maxDamage)));
            if (Random.Range(1,100) <= tileStatusChance)
            {
                battleManager.boardManager.ApplyTileStatus(battleManager.boardManager.GetTilesOfColor(target.tileColor, 1), TileStatus.Frozen);
            }
        }
        battleManager.NextTurn();
    }
}

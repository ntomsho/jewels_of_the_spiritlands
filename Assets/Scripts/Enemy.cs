using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class Enemy : CharacterEntity
{
    [Header("Enemy Info")]
    public string enemyName;
    [TextArea] public string enemyDescription;

    [Header("Stats")]
    public int health;
    public int[] speedRange;
    public int difficultyValue;

    [Header("Visuals")]
    public Animator animator;
    public Sprite initSprite;
    
    [Header("UI")]
    [SerializeField] Canvas enemyCanvas;
    [SerializeField] Image healthBar;
    [SerializeField] Image damageBar;
    [SerializeField] TextMeshProUGUI healthDisplay;
    [SerializeField] GameObject popupPrefab;

    [Header("Audio")]
    [SerializeField] protected AudioClip attackSFX;
    [SerializeField] protected AudioClip hitSFX;
    [System.NonSerialized] public BattleManager battleManager;

    int startingHealth;
    float actionTimer;
    [System.NonSerialized] public bool dead;
    
    void Start()
    {
        startingHealth = health;
    }

    public override void HandleTap()
    {
        battleManager.SetTargetIndicator(this, true);
        battleManager.HandleTap(this);
    }

    public override bool TakeDamage(int baseDamage, bool fromAttack = true)
    {
        int damage = ProcessDamageTaken(baseDamage);
        health -= damage;
        CreateHealthPopup(damage, true);
        UpdateHealthBar(false, Mathf.Min(health, damage));
        animator.SetTrigger("takeHit");
        TakeHitTween(damage);
        if (health <= 0)
        {
            dead = true;
            animator.SetBool("isDead", true);
            StartCoroutine(Die());
            return true;
        }
        return false;
    }

    void TakeHitTween(int damage)
    {
        float intensity = damage / startingHealth < 0.1f ? 0.5f : 1f;
        var sequence = DOTween.Sequence();
        sequence.Append(transform.DOLocalMoveX(transform.position.x + intensity, 0.3f)).SetEase(Ease.OutSine);
        sequence.Append(transform.DOLocalMoveX(transform.position.x, 0.5f)).SetEase(Ease.OutBack);
        sequence.PrependInterval(0.2f);
    }

    void UpdateHealthBar(bool instant, int damage = 0)
    {
        float percentage = Mathf.Max(0f, (float)health / (float)startingHealth);
        healthBar.fillAmount = percentage;
        healthDisplay.text = "Health: " + health + "/" + startingHealth;
        if (instant)
        {
            damageBar.fillAmount = percentage;
        }
    }

    public override void ClearDamageBars()
    {
        float percentage = (float)health / (float)startingHealth;
        damageBar.DOFillAmount(percentage, 0.5f);
    }

    public virtual PartyMember GetTarget(int[] weights)
    {
        int weightSum = 0;
        for (int ind = 0; ind < weights.Length; ind++)
        {
            weightSum += battleManager.partyMembers[ind].knockedOut ? 0 : weights[ind];
        }
        int roll = Random.Range(1, weightSum);
        int currentWeight = 0;

        for (int ind = 0; ind < weights.Length; ind++)
        {
            if (battleManager.partyMembers[ind].knockedOut) continue;
            currentWeight += weights[ind];
            if (weights[ind] != 0 && roll <= currentWeight)
            {
                return battleManager.partyMembers[ind];
            }
        }
        return null;
    }

    public int InitRoll(int initMod)
    {
        int valueSum = 0;
        foreach(int value in speedRange)
        {
            valueSum += value;
        }
        int roll = Random.Range(1, valueSum);
        int currentValue = 0;

        for (int ind = 0; ind < speedRange.Length; ind++)
        {
            currentValue += speedRange[ind];
            if (roll < currentValue)
            {
                return ind + 1 + initMod;
            }
        }
        return speedRange.Length;
    }

    void CreateHealthPopup(int number, bool damage)
    {
        GameObject popup = Instantiate(popupPrefab, enemyCanvas.transform.position + Vector3.up * 1.5f, Quaternion.identity, enemyCanvas.transform);
        popup.GetComponent<PopupText>().ActivatePopup(number.ToString(), damage ? Color.red : Color.green);
    }

    // public override string ReturnFormattedDescText()
    // {
    //     return enemyDescription;
    // }

    public override void SetSelected(InfoManager infoManager)
    {
        selectorSprite.enabled = true;
        infoManager.SetInfo(this);
    }

    public override bool IsDead()
    {
        return dead;
    }

    public void StandardAction()
    {
        InitiateAction();
    }

    public virtual void PlayHitSound()
    {
        GameManager.Instance.PlaySound(hitSFX);
    }

    public IEnumerator EndAction()
    {
        yield return new WaitForSeconds(2f);
        battleManager.NextTurn();
    }

    public virtual void InitiateAction()
    {
        //
    }

    IEnumerator Die()
    {
        yield return new WaitForSeconds(2f);
        battleManager.RemoveEnemy(this);
    }
}

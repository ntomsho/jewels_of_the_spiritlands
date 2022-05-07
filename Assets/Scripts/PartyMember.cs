using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public enum CharacterClass { Assassin, Huntress, Knight, Shaman, Witch };

public class PartyMember : CharacterEntity
{
    [Header("Party Member Info")]
    public CharacterClass characterClass;
    public string characterName;
    [TextArea] public string characterDescription;
    [TextArea] public string attackDescription;
    [TextArea] public string specialDescription;

    [Header("Gems")]
    public Image gemIndicator;
    public Sprite specialIcon;
    
    [Header("Visuals")]
    [SerializeField] float actionDelay;
    public Animator animator;
    public float groundPlaneMod;

    [Header("UI")]
    [SerializeField] Canvas characterCanvas;
    [SerializeField] Image healthBar;
    [SerializeField] Image damageBar;
    [SerializeField] Image guardBar;
    [SerializeField] Image guardDamageBar;
    [SerializeField] TextMeshProUGUI guardDisplay;
    [SerializeField] TextMeshProUGUI healthDisplay;
    [SerializeField] GameObject popupPrefab;

    [Header("Audio")]
    public AudioClip hitSFX;
    public AudioClip specialSFX;

    [System.NonSerialized] public List<Match> actionQueue = new List<Match>();
    float actionTimer;
    [System.NonSerialized] public BattleManager battleManager;
    [System.NonSerialized] List<CharacterUpgrade> characterUpgrades;
    [System.NonSerialized] public ParticleSystem gemChargeParticle;
    [System.NonSerialized] public bool knockedOut;
    [System.NonSerialized] public int guard; 
    [System.NonSerialized] public int health;
    [System.NonSerialized] public int maxHealth = 100;
    [System.NonSerialized] public int startingGuard;
    [System.NonSerialized] public TileColor tileColor;
    [System.NonSerialized] public bool usesTargeting;
    
    void Start()
    {
        actionTimer = actionDelay;
        //set gem indicator based on party index
    }

    // Update is called once per frame
    void Update()
    {
        if (battleManager.battleOver) return;
        if (actionTimer < actionDelay)
        {
            actionTimer += Time.deltaTime;
        }
        if (actionQueue.Count > 0 && actionTimer >= actionDelay)
        {
            actionTimer = 0;
            Match nextActionMatch = actionQueue[0];
            actionQueue.RemoveAt(0);
            StandardAction(nextActionMatch.value);
        }
    }

    public void WriteAttributesFromTemplate(CharacterTemplate template)
    {
        knockedOut = template.knockedOut;
        // health = template.health;
        health = maxHealth;
        characterUpgrades = template.characterUpgrades;
        UpdateHealthBars(true);
    }

    public override bool TakeDamage(int baseDamage, bool fromAttack = true)
    {
        int damage = ProcessDamageTaken(baseDamage);
        int healthDamage = 0;
        if (damage < guard)
        {
            guard -= damage;
        }
        else
        {
            healthDamage = damage - guard;
            guard = 0;
        }

        health -= healthDamage;
        CreateHealthPopup(damage, true);
        if (damage > 0)
        {
            UpdateHealthBars(false);
            animator.SetTrigger("takeHit");
            TakeHitTween(damage);
        }
        if (health <= 0)
        {
            knockedOut = true;
            animator.SetBool("isDead", true);
            battleManager.KnockOutPartyMember(this);
            return true;
        }
        return false;
    }

    void TakeHitTween(int damage)
    {
        float intensity = damage / maxHealth < 0.1f ? 0.5f : 1f;
        var sequence = DOTween.Sequence();
        sequence.Append(transform.DOLocalMoveX(transform.position.x - intensity, 0.3f)).SetEase(Ease.OutSine);
        sequence.Append(transform.DOLocalMoveX(transform.position.x, 0.5f)).SetEase(Ease.OutBack);
        sequence.PrependInterval(0.2f);
    }

    public void TakeHealing(int healing)
    {
        healing = (int)Mathf.Min(healing, maxHealth - health);
        health += healing;
        CreateHealthPopup(healing, false);
        UpdateHealthBars();
    }

    public void GainGuard(int guardGain)
    {
        guard += guardGain;
        CreateHealthPopup(guardGain, false);
        UpdateHealthBars();
    }

    void CreateHealthPopup(int number, bool damage)
    {
        GameObject popup = Instantiate(popupPrefab, characterCanvas.transform.position + Vector3.up * 1.5f, Quaternion.identity, characterCanvas.transform);
        popup.GetComponent<PopupText>().ActivatePopup(number.ToString(), damage ? Color.red : Color.green);
    }

    void UpdateHealthBars(bool instant = false)
    {
        float percentage = Mathf.Max(0f, (float)health / (float)maxHealth);
        healthBar.fillAmount = percentage;
        guardBar.fillAmount = Mathf.Min(guard / 100f, 1f);
        guardDisplay.text = "Guard: " + guard;
        healthDisplay.text = "Health: " + health + "/" + maxHealth;
        if (instant)
        {
            damageBar.fillAmount = percentage;
            guardDamageBar.fillAmount = Mathf.Min(guard / 100f, 1f);
        }
    }

    public override void ClearDamageBars()
    {
        float percentage = (float)health / (float)maxHealth;
        float guardPercentage = (float)guard / 100f;
        var sequence = DOTween.Sequence();
        if (guardDamageBar.fillAmount != guardPercentage)
        {
            sequence.Append(guardDamageBar.DOFillAmount(Mathf.Min(guardPercentage, 1f), 0.5f));
        }
        sequence.Append(damageBar.DOFillAmount(percentage, 0.5f));
    }

    public override void SetSelected(InfoManager infoManager)
    {
        selectorSprite.enabled = true;
        infoManager.SetInfo(this);
    }

    public void StandardAction(int matchValue)
    {
        animator.SetTrigger("callAttack");
        List<Enemy> targets = GetTargets();
        if (targets != null)
        {
            InitiateAction(matchValue, targets);
        }
        else
        {
            StartCoroutine(EndAction(false));
        }
    }

    public virtual List<Enemy> GetTargets()
    {
        if (battleManager.targetedEnemy != null)
        {
            return new List<Enemy> { battleManager.targetedEnemy };
        }
        else
        {
            return null;
        }
    }

    public virtual void InitiateAction(int matchValue, List<Enemy> targets)
    {
        //
    }

    public virtual List<CharacterEntity> GetSpecialTargets()
    {
        return new List<CharacterEntity>();
    }

    public override bool IsDead()
    {
        return knockedOut;
    }

    public override void HandleTap()
    {
        battleManager.HandleTap(this);
    }

    public virtual void ActivateSpecial(CharacterEntity target)
    {
        animator.SetTrigger("callAttack");
    }

    public IEnumerator EndAction(bool kill)
    {
        yield return new WaitForSeconds(kill ? 3f : 1f);
        battleManager.NextMatchInQueue();
    }

    public IEnumerator EndSpecial(bool kill)
    {
        yield return new WaitForSeconds(kill ? 3f : 1f);
        battleManager.boardManager.StartPlayerTurn();
    }
}

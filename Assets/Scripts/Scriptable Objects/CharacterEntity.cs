using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class CharacterEntity : MonoBehaviour
{
    [Header("Visuals")]
    public SpriteRenderer characterSprite;
    public Image selectorSprite;
    public ParticleSystem attackParticle;
    public CanvasGroup infoDisplay;

    [Header("Status Effects")]
    [System.NonSerialized] public List<StatusEffect> statusEffects = new List<StatusEffect>();
    [SerializeField] StatusManager statusManager;
    public void AddStatusEffect(StatusEffect newStatusEffect)
    {
        int existingEffectInd = statusEffects.FindIndex(effect => effect.effectName == newStatusEffect.effectName);
        if (existingEffectInd == -1)
        {
            statusEffects.Add(newStatusEffect);
            statusManager.AddNewIcon(newStatusEffect);
            if (!newStatusEffect.spriteColor.Equals(Color.clear))
            {
                characterSprite.color = newStatusEffect.spriteColor;
            }
            return;
        }
        if (newStatusEffect.isToken)
        {
            StatusToken newToken = (StatusToken) newStatusEffect;
            StatusToken existingToken = (StatusToken) statusEffects[existingEffectInd];
            if (existingToken.maxStacks > 0 && existingToken.numStacks < existingToken.maxStacks)
            existingToken.numStacks += Mathf.Min(newToken.numStacks, existingToken.maxStacks - existingToken.numStacks);
        }
        else
        {
            TimedStatus newStatus = (TimedStatus) newStatusEffect;
            TimedStatus existingStatus = (TimedStatus) statusEffects[existingEffectInd];
            existingStatus.duration += newStatus.duration;
        }
        statusManager.UpdateIconNumber(statusEffects[existingEffectInd], statusEffects[existingEffectInd].NumToDisplay());
    }

    public void AddStatusEffect(StatusToken newStatusEffect)
    {
        int existingEffectInd = statusEffects.FindIndex(effect => effect.effectName == newStatusEffect.effectName);
        if (existingEffectInd == -1)
        {
            statusEffects.Add(newStatusEffect);
            statusManager.AddNewIcon(newStatusEffect);
            if (!newStatusEffect.spriteColor.Equals(Color.clear))
            {
                characterSprite.color = newStatusEffect.spriteColor;
            }
            return;
        }
        StatusToken existingToken = (StatusToken) statusEffects[existingEffectInd];
        if (existingToken.maxStacks > 0 && existingToken.numStacks < existingToken.maxStacks)
        existingToken.numStacks += Mathf.Min(newStatusEffect.numStacks, existingToken.maxStacks - existingToken.numStacks);
        statusManager.UpdateIconNumber(statusEffects[existingEffectInd], statusEffects[existingEffectInd].NumToDisplay());
    }

    public void RemoveStatusEffect(StatusEffect effect)
    {
        if (!statusEffects.Contains(effect)) return;
        statusEffects.Remove(effect);
        statusManager.RemoveIcon(effect);
        Color newColor = Color.white;
        foreach(StatusEffect statusEffect in statusEffects)
        {
            if (!statusEffect.spriteColor.Equals(Color.clear))
            {
                newColor = statusEffect.spriteColor;
                break;
            }
        }
        characterSprite.color = newColor;
    }

    public void UpdateStatusEffects()
    {
        List<StatusEffect> effectsToClear = new List<StatusEffect>();
        for (int ind = 0; ind < statusEffects.Count; ind++)
        {
            if (statusEffects[ind].damageToApply != 0)
            {
                bool killed = TakeDamage(statusEffects[ind].damageToApply, false);
                if (killed) return;
            }

            if (!statusEffects[ind].IsToken())
            {
                TimedStatus statusEffect = (TimedStatus) statusEffects[ind];
                statusEffect.durationTimer++;
                if (statusEffect.durationTimer >= statusEffect.duration)
                {
                    effectsToClear.Add(statusEffect);
                }
                else
                {
                    statusManager.UpdateIconNumber(statusEffect, statusEffect.NumToDisplay());
                }
            }
        }
        foreach (StatusEffect effect in effectsToClear)
        {
            RemoveStatusEffect(effect);
        }
    }

    public bool HasStatusEffect(string statusEffectName)
    {
        foreach (StatusEffect statusEffect in statusEffects)
        {
            if (statusEffect.effectName == statusEffectName)
            {
                return true;
            }
        }
        return false;
    }

    public StatusEffect GetStatusEffectByName(string statusEffectName)
    {
        foreach (StatusEffect statusEffect in statusEffects)
        {
            if (statusEffect.effectName == statusEffectName)
            {
                return statusEffect;
            }
        }
        return null;
    }

    void RemoveTokenStack(StatusToken token)
    {
        token.numStacks -= 1;
        if (token.numStacks <= 0)
        {
            RemoveStatusEffect(token);
        }
        else
        {
            statusManager.UpdateIconNumber(token, token.NumToDisplay());
        }
    }

    public int ProcessDamageDealt(int baseDamage)
    {
        float newDamage = (float) baseDamage;
        float missChance = 0f;
        foreach (StatusEffect statusEffect in statusEffects)
        {
            if (statusEffect.powerMod != 0)
            {
                newDamage += baseDamage * statusEffect.powerMod - baseDamage;
                if (statusEffect.isToken) RemoveTokenStack((StatusToken) statusEffect);
            }
            if (statusEffect.powerChange != 0)
            {
                newDamage += statusEffect.powerChange;
                if (statusEffect.isToken) RemoveTokenStack((StatusToken) statusEffect);
            }
            if (statusEffect.missChance != 0)
            {
                missChance += statusEffect.missChance;
                if (statusEffect.isToken) RemoveTokenStack((StatusToken) statusEffect);
            }
        }
        if (missChance > 0f && Random.Range(1,100) <= missChance)
        {
            return 0;
        }
        return (int) newDamage;
    }

    public int ProcessDamageTaken(int baseDamage)
    {
        float newDamage = (float) baseDamage;
        float dodgeChance = 0f;
        foreach (StatusEffect statusEffect in statusEffects)
        {
            if (statusEffect.damageTakenMod != 0)
            {
                newDamage += baseDamage * statusEffect.damageTakenMod - baseDamage;
                if (statusEffect.isToken) RemoveTokenStack((StatusToken) statusEffect);
            }
            if (statusEffect.damageTakenChange != 0)
            {
                newDamage += statusEffect.damageTakenChange;
                if (statusEffect.isToken) RemoveTokenStack((StatusToken) statusEffect);
            }
            if (statusEffect.dodgeChance != 0)
            {
                dodgeChance += statusEffect.dodgeChance;
                if (statusEffect.isToken) RemoveTokenStack((StatusToken) statusEffect);
            }
        }
        if (dodgeChance >= 0 && Random.Range(1,100) <= dodgeChance)
        {
            return 0;
        }
        return (int) newDamage;
    }

    void OnMouseDown()
    {
        HandleTap();
    }

    public string ReturnFormattedDescText(string str, int ?positionTileColor = null)
    {
        string newString = str;
        while (newString.IndexOf("{") != -1)
        {
            int start = newString.IndexOf("{");
            string replaceText = newString.Substring(start, newString.IndexOf("}") - (start - 1));
            string replaceVar = replaceText.Substring(1, replaceText.Length - 2);
            // newString = newString.Replace(replaceText, this.GetType().GetField(replaceVar).GetValue(this).ToString());
            newString = newString.Replace(replaceText, GetAttrString(replaceVar, positionTileColor));
        }
        return newString;
    }

    public abstract bool IsDead();

    public virtual string GetAttrString(string replaceStr, int ?positionTileColor)
    {
        return this.GetType().GetField(replaceStr).GetValue(this).ToString();
    }

    public abstract bool TakeDamage(int damage, bool fromAttack = true);
    public abstract void HandleTap();
    public abstract void ClearDamageBars();
    public abstract void SetSelected(InfoManager infoManager);
}

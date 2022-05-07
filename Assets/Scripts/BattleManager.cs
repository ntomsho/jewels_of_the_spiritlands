using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class BattleManager : MonoBehaviour
{
    public BoardManager boardManager;
    [SerializeField] List<Sprite> bgSprites;
    
    [Header("Party Members")]
    public PartyMember[] partyMembers;

    [Header("Enemies")]
    public List<Enemy> enemies;

    [Header("UI")]
    [SerializeField] GameObject targetIndicatorPrefab;
    [SerializeField] List<ParticleSystem> gemChargeParticles;
    [SerializeField] ParticleSystem fireworks;
    [SerializeField] AudioClip onBattleEndSound;

    [System.NonSerialized] public List<Match> activeMatches = new List<Match>();
    GameObject background;
    Dictionary<TileColor, PartyMember> colorToCharacter;
    [System.NonSerialized] public bool battleOver;
    [System.NonSerialized] public InitiativeManager initiativeManager;
    [System.NonSerialized] public InfoManager infoCanvas;
    Dictionary<PartyMember, int> matchQueue = new Dictionary<PartyMember, int>();
    int partyMemberSpecialInd;
    [System.NonSerialized] public bool specialTargeting;
    List<CharacterEntity> specialTargets;
    [System.NonSerialized] public List<PartyMember> knockedOutPartyMembers = new List<PartyMember>();
    [System.NonSerialized] public Enemy targetedEnemy;
    GameObject targetIndicator;

    public void Setup(CharacterTemplate[] characterTemplates, List<GameObject> enemyGroup)
    {
        SetupBG();
        SetupUI();
        for (int ind = 0; ind < characterTemplates.Length; ind++)
        {
            partyMembers[ind] = CreatePartyMember(characterTemplates[ind], ind);
        }
        CreateStartingEnemies(enemyGroup);
        initiativeManager.Setup(enemies);

        //
        // EndBattle(true);
    }

    void SetupBG()
    {
        background = GameObject.Find("Battle BG");
        background.GetComponent<SpriteRenderer>().sprite = bgSprites[Random.Range(0, bgSprites.Count)];
        background.GetComponent<BattleBG>().battleManager = this;
    }

    void SetupUI()
    {
        initiativeManager = GameObject.Find("Initiative Canvas").GetComponent<InitiativeManager>();
        colorToCharacter = new Dictionary<TileColor, PartyMember>();
        targetIndicator = Instantiate(targetIndicatorPrefab, Vector3.zero, Quaternion.Euler(0,0,90f));
    }

    PartyMember CreatePartyMember(CharacterTemplate template, int partyIndex)
    {
        GameObject pmPrefab = Instantiate(template.characterPrefab, GameManager.Instance.partyPositions[partyIndex], Quaternion.identity);
        PartyMember newPM = pmPrefab.GetComponent<PartyMember>();
        pmPrefab.transform.Translate(new Vector2(0f, newPM.groundPlaneMod));
        newPM.gemChargeParticle = Instantiate(gemChargeParticles[(int)newPM.tileColor], newPM.gemIndicator.transform.position, Quaternion.identity);
        newPM.battleManager = this;
        newPM.guard = GameManager.Instance.startingGuard[partyIndex];
        newPM.tileColor = (TileColor)partyIndex;
        newPM.WriteAttributesFromTemplate(template);
        colorToCharacter.Add(newPM.tileColor, newPM);
        return newPM;
    }

    void CreateStartingEnemies(List<GameObject> enemyGroup)
    {
        foreach(GameObject enemy in enemyGroup)
        {
            Enemy newEnemy = CreateEnemy(enemy, enemies.Count, enemies.Count == enemyGroup.Count - 1);
            newEnemy.battleManager = this;
            enemies.Add(newEnemy);
        }
        targetIndicator.SetActive(true);
        SetTargetIndicator(enemies[0]);
    }

    Enemy CreateEnemy(GameObject enemyPrefab, int enemyIndex, bool lastPosition = false)
    {
        return Instantiate(enemyPrefab, GetEnemyPos(enemyIndex, lastPosition), Quaternion.identity).GetComponent<Enemy>();
    }

    Vector2 GetEnemyPos(int enemyIndex, bool lastPosition = false)
    {
        int yPosInd = 1;
        if (enemyIndex != 0 && !lastPosition)
        {
            yPosInd = (enemyIndex % 2 == 0 ? 2 : 0);
        }
        return new Vector2(GameManager.Instance.enemyXPositions[enemyIndex], GameManager.Instance.enemyYPositions[yPosInd]);
    }

    public void AddToActiveMatches(Match matchToAdd)
    {
        activeMatches.Add(matchToAdd);
        TileColor matchColor = matchToAdd.matchColor;
        PartyMember character = partyMembers[(int)matchColor];
        character.gemChargeParticle.Play();
        var tween = character.gemIndicator.gameObject.transform.DOScale(Vector3.one * 1.5f, 0.2f).SetLoops(1, LoopType.Yoyo);
        // tween.OnComplete(() => tween.Rewind());
    }

    public void ProcessMatches()
    {
        foreach (Match match in activeMatches)
        {
            PartyMember character = colorToCharacter[match.matchColor];
            if (!character.knockedOut)
            {
                int destInt;
                if (!matchQueue.TryGetValue(character, out destInt))
                {
                    matchQueue[character] = match.value;
                }
                else
                {
                    matchQueue[character] += match.value;
                }
            }
        }
        activeMatches = new List<Match>();
        NextMatchInQueue();
    }

    public void NextTurn()
    {
        //would be better with a system that handles the turn and communicates back to here what happened
        bool anyoneDead = false;
        foreach(CharacterEntity character in GetAllCharacters())
        {
            character.UpdateStatusEffects();
            if (character.IsDead())
            {
                anyoneDead = true;
            }
        }
        ClearDamageBars();
        if (battleOver) return;

        StartCoroutine(ProcessNextTurn(anyoneDead ? 2f : 0f));
    }

    public void SetTargetIndicator(Enemy newTarget, bool manual = false)
    {
        //change this to decouple it from canSwap
        if (manual && !boardManager.canSwap) return;
        targetedEnemy = newTarget;
        targetIndicator.transform.position = newTarget.transform.position + (Vector3.down * 2f);
    }

    public PartyMember GetMostDamagedPartyMember()
    {
        PartyMember lowest = null;
        float lowestPercentHealth = partyMembers[0].health / partyMembers[0].maxHealth;
        
        for (int ind = 0; ind < partyMembers.Length; ind++)
        {
            if (partyMembers[ind].knockedOut) continue;
            if (lowest == null)
            {
                lowest = partyMembers[ind];
            }
            else
            {
                float percentHealth = partyMembers[ind].health / partyMembers[ind].maxHealth;
                if (percentHealth < lowestPercentHealth)
                {
                    lowest = partyMembers[ind];
                    lowestPercentHealth = percentHealth;
                }
            }
        }
        return lowest;
    }

    public void RemoveEnemy(Enemy enemy)
    {
        if (targetedEnemy == enemy)
        {
            if (enemies.Count == 1)
            {
                targetedEnemy = null;
                targetIndicator.SetActive(false);
            }
            else
            {
                int currentTargetIndex = enemies.IndexOf(enemy);
                SetTargetIndicator(currentTargetIndex == 0 ? enemies[currentTargetIndex + 1] : enemies[currentTargetIndex - 1]);
            }
        }
        if (enemy.selectorSprite.enabled)
        {
            infoCanvas.SetActive(false);
        }
        initiativeManager.RemoveFromQueue(enemy);
        enemies.Remove(enemy);
        Destroy(enemy.gameObject);
        if (enemies.Count == 0)
        {
            EndBattle(true);
        }
        else
        {
            ReorderEnemies();
        }
    }

    void ReorderEnemies()
    {
        for (int ind = 0; ind < enemies.Count; ind++)
        {
            Vector2 destPos = GetEnemyPos(ind, ind == enemies.Count - 1);
            if (enemies[ind].transform.position.x != destPos.x && enemies[ind].transform.position.y != destPos.y)
            {
                var tween = enemies[ind].transform.DOMove(destPos, 0.5f);
                tween.SetEase(Ease.InBack);
                if (enemies[ind] == targetedEnemy)
                {
                    var arrowTween = targetIndicator.transform.DOMove(destPos + (Vector2.down * 2f), 0.5f);
                    arrowTween.SetEase(Ease.InBack);
                }
            }
        }
    }

    public void KnockOutPartyMember(PartyMember character)
    {
        knockedOutPartyMembers.Add(character);
        boardManager.ApplyStatusToColor(character.tileColor, TileStatus.Inactive);
    }

    void EndBattle(bool playerWon)
    {
        battleOver = true;
        boardManager.battleOver = true;
        initiativeManager.EndBattle();
        GameManager.Instance.PlaySound(onBattleEndSound);
        GameManager.Instance.overlayManager.ShowEndCanvas(playerWon);
        if (playerWon)
        {
            StartCoroutine(VictoryFireworks());
        }
    }

    public void Restart()
    {
        GameManager.Instance.Restart();
        GameManager.Instance.overlayManager.ClearEndCanvases();
    }

    List<CharacterEntity> GetAllCharacters()
    {
        List<CharacterEntity> allCharacters = new List<CharacterEntity>();
        foreach(PartyMember partyMember in partyMembers)
        {
            allCharacters.Add(partyMember);
        }
        foreach(Enemy enemy in enemies)
        {
            allCharacters.Add(enemy);
        }
        return allCharacters;
    }

    public void HandleTap(CharacterEntity tappedCharacter)
    {
        if (specialTargeting)
        {
            if (specialTargets.Contains(tappedCharacter))
            {
                ActivatePlayerSpecial(tappedCharacter);
            }
        }
        else
        {
            foreach(CharacterEntity character in GetAllCharacters())
            {
                if (character != tappedCharacter)
                {
                    character.selectorSprite.enabled = false;
                    character.infoDisplay.alpha = 0;
                }
                else
                {
                    if (character.selectorSprite.enabled)
                    {
                        character.selectorSprite.enabled = false;
                        character.infoDisplay.alpha = 0;
                        infoCanvas.SetActive(false);
                    }
                    else
                    {
                        character.infoDisplay.alpha = 1;
                        character.SetSelected(infoCanvas);
                    }
                }
            }
        }
    }

    void ClearDamageBars()
    {
        foreach(CharacterEntity character in GetAllCharacters())
        {
            character.ClearDamageBars();
        }
    }

    public void NextMatchInQueue()
    {
        ClearDamageBars();
        if (battleOver) return;
        for (int ind = 0; ind < partyMembers.Length; ind++)
        {
            int matchValue; 
            matchQueue.TryGetValue(partyMembers[ind], out matchValue);
            if (matchValue != 0)
            {
                partyMembers[ind].StandardAction(matchValue);
                matchQueue[partyMembers[ind]] = 0;
                return;
            }
        }
        matchQueue = new Dictionary<PartyMember, int>();
        NextTurn();
    }

    public void ToggleSpecialTargeting(int pmIndex, bool cancel = false)
    {
        if (specialTargeting && cancel)
        {
            StopSpecialTargeting();
        }
        else
        {
            StartSpecialTargeting(pmIndex);
        }
    }

    void StartSpecialTargeting(int pmIndex)
    {
        specialTargeting = true;
        partyMemberSpecialInd = pmIndex;
        specialTargets = partyMembers[pmIndex].GetSpecialTargets();
        //clear currently selected
        //update info canvas to show special info
        infoCanvas.SetInfo("SELECT TARGET\n" + partyMembers[pmIndex].specialDescription);
        foreach (CharacterEntity character in GetAllCharacters())
        {
            character.infoDisplay.alpha = 0;
            if (specialTargets.Contains(character))
            {
                character.selectorSprite.enabled = true;
            }
            else
            {
                character.selectorSprite.enabled = false;
            }
        }
    }

    void StopSpecialTargeting()
    {
        specialTargeting = false;
        specialTargets = null;
        boardManager.specialSelect.CancelSelect();
        foreach (CharacterEntity character in GetAllCharacters())
        {
            character.selectorSprite.enabled = false;
        }
    }

    public void ActivatePlayerSpecial(CharacterEntity target)
    {
        Instantiate(boardManager.activateParticle, partyMembers[partyMemberSpecialInd].gemIndicator.transform.position, Quaternion.identity);
        partyMembers[partyMemberSpecialInd].ActivateSpecial(target);
        infoCanvas.Cancel();
        boardManager.specialSelect.OnSpecialActivate(partyMemberSpecialInd);
        StopSpecialTargeting();
    }

    IEnumerator ProcessNextTurn(float delay)
    {
        yield return new WaitForSeconds(delay);
        Enemy enemy = initiativeManager.AdvanceTurn();
        System.Action waitForTurnCanvas;
        if (enemy == null)
            {
                waitForTurnCanvas = new System.Action(boardManager.StartPlayerTurn);
            }
            else
            {
                waitForTurnCanvas = new System.Action(enemy.StandardAction);
            }
        GameManager.Instance.overlayManager.ShowTurnCanvas(enemy == null, waitForTurnCanvas, enemy);
    }

    IEnumerator VictoryFireworks()
    {
        for (int ind = 0; ind < 8; ind++)
        {
            ParticleSystem burst = Instantiate(fireworks, new Vector3(Random.Range(-3f, 3f), Random.Range(-1.5f, 1.5f), -1f), Quaternion.identity);
            yield return new WaitForSeconds(0.5f);
        }
    }
}

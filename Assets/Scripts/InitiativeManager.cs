using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitiativeManager : MonoBehaviour
{
    [Header("Initiative Sprites")]
    [SerializeField] GameObject initiativeSpritePrefab;
    [SerializeField] Vector2[] spritePositions;
    
    List<Enemy> enemies;
    List<InitiativeSpace> queue;
    Dictionary<Enemy, int> enemyInitiativeRolls = new Dictionary<Enemy, int>();
    List<InitiativeSpaceVisual> initiativeSprites = new List<InitiativeSpaceVisual>();

    void Start()
    {
        for(int ind = 0; ind < 7; ind++)
        {
            initiativeSprites.Add(Instantiate(initiativeSpritePrefab, gameObject.transform).GetComponent<InitiativeSpaceVisual>());
            initiativeSprites[ind].transform.localPosition = spritePositions[5];
            initiativeSprites[ind].gameObject.SetActive(false);
        }
    }
    public void Setup(List<Enemy> _enemies)
    {
        queue = new List<InitiativeSpace>();
        enemies = _enemies;
        foreach (Enemy enemy in enemies)
        {
            enemyInitiativeRolls[enemy] = enemy.InitRoll(0);
        }
        
        RefillQueue();
        StartCoroutine(TweenInFirstRound());
    }

    void RefillQueue()
    {
        while (queue.Count < 7)
        {
            ProcessTurn();
        }
    }

    void ProcessTurn()
    {
        bool enemyTurnAssigned = false;
        foreach(Enemy enemy in enemies)
        {
            if (!enemyInitiativeRolls.ContainsKey(enemy))
            {
                enemies.Remove(enemy);
                break;
            }
            int roll = enemyInitiativeRolls[enemy];
            if (roll == 0)
            {
                AddToQueue(enemy);
                enemyInitiativeRolls[enemy] = enemy.InitRoll(enemies.Count);
                enemyTurnAssigned = true;
            }
            else
            {
                enemyInitiativeRolls[enemy] -= 1;
            }
        }
        if (!enemyTurnAssigned)
        {
            AddToQueue();
        }
    }

    void AddToQueue(Enemy enemy = null)
    {
        if (enemy == null)
        {
            if (queue.Count > 0 && queue[queue.Count - 1].isPlayerSpace)
            {
                queue[queue.Count - 1].ChangeTurnsNum(1);
            }
            else
            {
                PlayerInitiativeSpace newSpace = new PlayerInitiativeSpace();
                newSpace.AssignSprite(GetFreeSprite());
                newSpace.initiativeSprite.transform.localPosition = spritePositions[5];
                queue.Add(newSpace);
            }
        }
        else
        {
            EnemyInitiativeSpace newSpace = new EnemyInitiativeSpace(enemy);
            newSpace.AssignSprite(GetFreeSprite());
            newSpace.initiativeSprite.transform.localPosition = spritePositions[5];
            queue.Add(newSpace);
        }
    }

    public void RemoveFromQueue(Enemy enemy)
    {
        int ind = 0;
        enemyInitiativeRolls.Remove(enemy);
        while (ind < queue.Count)
        {
            if (queue[ind].GetEnemy() == enemy)
            {
                queue[ind].initiativeSprite.Clear();
                queue.RemoveAt(ind);
            }
            else
            {
                ind++;
            }

            if (ind < queue.Count - 1 && queue[ind].isPlayerSpace && queue[ind + 1].isPlayerSpace)
            {
                CombinePlayerSpaces(ind, ind + 1);
            }
        }
        RefillQueue();
    }

    public void RemoveFromQueue(int index)
    {
        if (index < 0 || index >= queue.Count) return;
        if (queue[index].isPlayerSpace && queue[index].GetNumTurns() > 1)
        {
            queue[index].ChangeTurnsNum(-1);
        }
        else
        {
            queue[index].initiativeSprite.Clear();
            queue.RemoveAt(index);
        }
        RefillQueue();
    }

    void CombinePlayerSpaces(int space1Ind, int space2Ind)
    {
        queue[space1Ind].ChangeTurnsNum(queue[space2Ind].GetNumTurns());
        queue[space2Ind].initiativeSprite.MergeTween(queue[space1Ind].initiativeSprite.transform.position.x);
        queue.RemoveAt(space2Ind);
    }

    public Enemy AdvanceTurn()
    {
        RemoveFromQueue(0);
        ReorderQueue();
        return queue[0].GetEnemy();
    }

    void ReorderQueue(bool firstRound = false)
    {
        for (int ind = 0; ind < queue.Count; ind++)
        {
            queue[ind].initiativeSprite.TweenTo(spritePositions[Mathf.Min(ind, 5)].x, firstRound ? 0.25f * ind : 0f);
        }
    }

    public void EndBattle()
    {
        foreach(InitiativeSpace space in queue)
        {
            space.initiativeSprite.Clear();
        }
    }

    InitiativeSpaceVisual GetFreeSprite()
    {
        for (int ind = 0; ind < 7; ind++)
        {
            if (!initiativeSprites[ind].assigned)
            {
                return initiativeSprites[ind];
            }
        }
        return Instantiate(initiativeSpritePrefab, gameObject.transform).GetComponent<InitiativeSpaceVisual>();
    }

    IEnumerator TweenInFirstRound()
    {
        yield return new WaitForSeconds(2f);
        ReorderQueue(true);
    }
}

public abstract class InitiativeSpace
{
    public bool isPlayerSpace;
    public InitiativeSpaceVisual initiativeSprite;

    public virtual int GetNumTurns()
    {
        return 0;
    }

    public virtual Enemy GetEnemy()
    {
        return null;
    }

    public virtual void ChangeTurnsNum(int numToAdd) {}
}

public class PlayerInitiativeSpace : InitiativeSpace
{
    public int numTurns;

    public PlayerInitiativeSpace(int _numTurns = 1)
    {
        numTurns = _numTurns;
        isPlayerSpace = true;
    }

    public void AssignSprite(InitiativeSpaceVisual sprite)
    {
        initiativeSprite = sprite;
        initiativeSprite.gameObject.SetActive(true);
        initiativeSprite.assigned = true;
        initiativeSprite.Initialize(numTurns);
    }

    public override int GetNumTurns()
    {
        return numTurns;
    }
    public override void ChangeTurnsNum(int numToAdd)
    {
        numTurns += numToAdd;
        initiativeSprite.GetComponent<InitiativeSpaceVisual>().SetPlayerTurnText(numTurns);
    }
}

public class EnemyInitiativeSpace : InitiativeSpace
{
    public Enemy enemy;

    public EnemyInitiativeSpace(Enemy _enemy)
    {
        enemy = _enemy;
        isPlayerSpace = false;
    }

    public void AssignSprite(InitiativeSpaceVisual sprite)
    {
        initiativeSprite = sprite;
        initiativeSprite.gameObject.SetActive(true);
        initiativeSprite.assigned = true;
        initiativeSprite.Initialize(enemy);
    }

    public override Enemy GetEnemy()
    {
        return enemy;
    }
}

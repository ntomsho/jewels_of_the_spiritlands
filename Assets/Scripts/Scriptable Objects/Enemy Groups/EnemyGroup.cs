using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "Enemy Group", order = 54)]
public class EnemyGroup : ScriptableObject
{
    public List<GameObject> enemies;

    public int DifficultyValue()
    {
        int total = 0;
        foreach(GameObject enemy in enemies)
        {
            total += enemy.GetComponent<Enemy>().difficultyValue;
        }
        return total;
    }
}

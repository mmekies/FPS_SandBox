using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemySoldierManager : MonoBehaviour
{
    PlayerCharacterController m_PlayerController;

    public List<EnemySoldier> enemies { get; private set; }
    public int numberOfEnemiesTotal { get; private set; }
    public int numberOfEnemiesRemaining => enemies.Count;
    
    public UnityAction<EnemySoldier, int> onRemoveEnemy;

    private void Awake()
    {
        m_PlayerController = FindObjectOfType<PlayerCharacterController>();
        DebugUtility.HandleErrorIfNullFindObject<PlayerCharacterController, EnemySoldier>(m_PlayerController, this);

        enemies = new List<EnemySoldier>();
    }

    public void RegisterEnemy(EnemySoldier enemy)
    {
        enemies.Add(enemy);

        numberOfEnemiesTotal++;
    }

    public void UnregisterEnemy(EnemySoldier enemyKilled)
    {
        int enemiesRemainingNotification = numberOfEnemiesRemaining - 1;

        if (onRemoveEnemy != null)
        {
            onRemoveEnemy.Invoke(enemyKilled, enemiesRemainingNotification);
        }

        // removes the enemy from the list, so that we can keep track of how many are left on the map
        enemies.Remove(enemyKilled);
    }
}

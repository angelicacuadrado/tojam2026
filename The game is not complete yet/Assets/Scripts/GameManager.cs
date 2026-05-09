using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Level Win Conditions")]
    [SerializeField, Tooltip("Current number of keys required to win the level")]
    private int keyAmount = 0;
    [SerializeField, Tooltip("Current number of enemies required to win the level")]
    private int enemiesKilled = 0;
    [SerializeField, Tooltip("Win condition for the current level")]
    private LevelData winCondition;
    [SerializeField, Tooltip("All possible win conditions for the levels")]
    private List<LevelData> allWinConditions;

    [HideInInspector] public UnityEvent OpenExit;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void AddKey()
    {
        keyAmount++;
        CheckWinCondition();
    }

    public void AddEnemyKill()
    {
        enemiesKilled++;
        CheckWinCondition();
    }

    private void CheckWinCondition()
    {
        if (keyAmount >= winCondition.keysRequired &&
            enemiesKilled >= winCondition.enemiesRequired)
        {
            OpenExit.Invoke();
        }
    }
}
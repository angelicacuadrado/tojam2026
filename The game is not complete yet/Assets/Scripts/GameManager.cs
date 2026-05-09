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
        // No DontDestroyOnLoad: each level scene additively loads its own GameManager.
        // The most recently awakened manager wins; old one is replaced when a level scene unloads.
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
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
using UnityEngine;
using UnityEngine.InputSystem;

public class EnemyDamageTester : MonoBehaviour
{
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private Animator animator;

    [Header("Animation States")]
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string walkStateName = "Walking";
    [SerializeField] private string punchStateName = "Punching";
    [SerializeField] private string hitStateName = "Reaction Hit";
    [SerializeField] private string dieStateName = "Dying";

    [Header("Test Settings")]
    [SerializeField] private float crossFadeDuration = 0.1f;
    [SerializeField] private bool controlRootMotionForTesting = true;

    private void Awake()
    {
        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null && controlRootMotionForTesting)
            animator.applyRootMotion = false;
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.tKey.wasPressedThisFrame)
            TestDamage();

        if (Keyboard.current.iKey.wasPressedThisFrame)
            TestIdle();

        if (Keyboard.current.wKey.wasPressedThisFrame)
            TestWalk();

        if (Keyboard.current.pKey.wasPressedThisFrame)
            TestPunch();

        if (Keyboard.current.hKey.wasPressedThisFrame)
            TestHit();

        if (Keyboard.current.dKey.wasPressedThisFrame)
            TestDie();
    }

    [ContextMenu("Test/Damage")]
    public void TestDamage()
    {
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(1);
        }
    }

    [ContextMenu("Test/Idle Animation")]
    public void TestIdle()
    {
        PlayAnimation(idleStateName, false);
    }

    [ContextMenu("Test/Walk Animation")]
    public void TestWalk()
    {
        PlayAnimation(walkStateName, false);
    }

    [ContextMenu("Test/Punch Animation")]
    public void TestPunch()
    {
        PlayAnimation(punchStateName, false);
    }

    [ContextMenu("Test/Hit Animation")]
    public void TestHit()
    {
        PlayAnimation(hitStateName, false);
    }

    [ContextMenu("Test/Die Animation")]
    public void TestDie()
    {
        PlayAnimation(dieStateName, true);
    }

    private void PlayAnimation(string stateName, bool useRootMotion)
    {
        if (animator == null)
        {
            Debug.LogWarning($"{nameof(EnemyDamageTester)} on {name} has no Animator assigned.", this);
            return;
        }

        if (string.IsNullOrWhiteSpace(stateName))
        {
            Debug.LogWarning($"{nameof(EnemyDamageTester)} on {name} has an empty animation state name.", this);
            return;
        }

        if (controlRootMotionForTesting)
            animator.applyRootMotion = useRootMotion;

        animator.CrossFadeInFixedTime(stateName, crossFadeDuration, 0, 0f);
    }
}

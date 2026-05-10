using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class WalkablePlatform : MonoBehaviour
{
    private NavMeshAgent agent;
    public Vector3 Veclocity => agent.velocity;
    
    private void Awake() => agent = GetComponent<NavMeshAgent>();
}

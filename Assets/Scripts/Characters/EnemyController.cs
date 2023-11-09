using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyStates { GUARD, PATROL, CHASE, DEAD }
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    private EnemyStates enemyStates;
    private NavMeshAgent agent;
    [Header("Basic Settings")]
    public float sightRadius;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void SwitchStates()
    {
        switch(enemyStates)
        {
            case EnemyStates.GUARD:
                break;
            case EnemyStates.PATROL:
                break; 
            case EnemyStates.CHASE:
                break;
            case EnemyStates.DEAD:
                break;
        }
    }
    bool FoundPlayer()
    {
        var colliders=Physics.OverlapSphere(transform.position, sightRadius);
        return false;
    }
}

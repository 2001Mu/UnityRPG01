using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PlayerController : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator anim;
    private CharacterStats characterStats;
    private GameObject attackTarget;
    private float lastAttackTime;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        characterStats = GetComponent<CharacterStats>();
    }
    void Start()
    {
        MouseManager.Instance.OnMouseClicked += MoveToTarget;
        MouseManager.Instance.OnEnemyClicked+= EventAttack;
        characterStats.MaxHealth = 2;
    }
    void Update()
    {
        SwitchAnimation();
        lastAttackTime-= Time.deltaTime;
    }
    private void SwitchAnimation()
    {
        anim.SetFloat("Speed", agent.velocity.sqrMagnitude);
    }
    public void MoveToTarget(Vector3 target)
    {
        StopAllCoroutines();
        agent.isStopped= false;
        agent.destination= target;
    }
    private void EventAttack(GameObject obj)
    {
        if(obj != null)
        {
            attackTarget= obj;
            StartCoroutine(MoveToAttackTarget());
        }
    }
    IEnumerator MoveToAttackTarget()
    {
        agent.isStopped= false;
        transform.LookAt(transform.position);
        while (Vector3.Distance(attackTarget.transform.position, transform.position) > characterStats.attackData.attackRange)
        {
            agent.destination=attackTarget.transform.position;
            yield return null;
        }
        agent.isStopped= true;
        if (lastAttackTime < 0)
        {
            anim.SetTrigger("Attack");
            lastAttackTime= 0.5f;
        }
    }
}

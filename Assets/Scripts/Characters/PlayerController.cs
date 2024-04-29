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
    private float stopDistance;
    private bool isDead;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        characterStats = GetComponent<CharacterStats>();

        stopDistance = agent.stoppingDistance;
    }
    void Start()
    {
        MouseManager.Instance.OnMouseClicked += MoveToTarget;
        MouseManager.Instance.OnEnemyClicked += EventAttack;
        //characterStats.MaxHealth = 2;
        GameManager.Instance.RigisterPlayer(characterStats);
    }
    void Update()
    {
        isDead = characterStats.CurrentHealth == 0;
        if(isDead)
        {
            GameManager.Instance.NotifyObservers();
        }
        SwitchAnimation();
        lastAttackTime-= Time.deltaTime;
    }
    private void SwitchAnimation()
    {
        anim.SetFloat("Speed", agent.velocity.sqrMagnitude);
        anim.SetBool("Death", isDead);
    }
    public void MoveToTarget(Vector3 target)
    {
        StopAllCoroutines();
        if (isDead) return;
        agent.stoppingDistance= stopDistance;
        agent.isStopped= false;
        agent.destination= target;
    }
    private void EventAttack(GameObject obj)
    {
        if (isDead) return;

        if (obj != null)
        {
            attackTarget= obj;
            characterStats.isCritical = UnityEngine.Random.value < characterStats.attackData.criticalChance;
            StartCoroutine(MoveToAttackTarget());
        }
    }
    IEnumerator MoveToAttackTarget()
    {
        agent.isStopped= false;
        agent.stoppingDistance = characterStats.attackData.attackRange;

        transform.LookAt(transform.position);
        while (Vector3.Distance(attackTarget.transform.position, transform.position) > characterStats.attackData.attackRange)
        {
            agent.destination=attackTarget.transform.position;
            yield return null;
        }
        agent.isStopped= true;
        if (lastAttackTime < 0)
        {
            anim.SetBool("Critical", characterStats.isCritical);
            transform.LookAt(attackTarget.transform);
            anim.SetTrigger("Attack");
            lastAttackTime= characterStats.attackData.coolDown;
        }
    }

    void Hit()
    {
        var targetStats = attackTarget.GetComponent<CharacterStats>();
        characterStats.TakeDamage(characterStats, targetStats);
    }
}

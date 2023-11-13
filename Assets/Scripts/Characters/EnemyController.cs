using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyStates { GUARD, PATROL, CHASE, DEAD }  //ö�ٹ���״̬
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    private EnemyStates enemyStates;    //����״̬
    private NavMeshAgent agent;     //NavMeshAgent���
    private Animator anim;      //����
    [Header("Patrol State")]
    public float patrolRange;   //Ѳ�߷�Χ
    [Header("Basic Settings")]  //Ϊ����Ŀ���Զ����NavMeshAgent���
    public float sightRadius;   //��ް뾶
    public bool isGuard;    //�Ƿ�Ϊվ׮����
    public float lookAtTime;    //�۲�ʱ��
    private float remainLookAtTime;     //ʣ��۲�ʱ��
    private float lastAttackTime;       //������ҡ
    private float speed;     //�ٶ�
    private GameObject attackTarget;    //����Ŀ��
    private Vector3 wayPoint;       //Ѳ�ߵ�
    private Vector3 guardPos;       //��ʼ��
    private CharacterStats characterStats;
    //����ת������
    bool isWalk;
    bool isChase;
    bool isFollow;
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();   //��ȡ���
        anim = GetComponent<Animator>();    //��ȡ���
        speed = agent.speed;    //��ȡ��ʼ�ٶ�
        guardPos= transform.position;   //��ȡ��ʼ����
        remainLookAtTime = lookAtTime;
        characterStats = GetComponent<CharacterStats>();
    }
    void Start()
    {
        characterStats.MaxHealth = 2;
        if (isGuard)
        {
            enemyStates= EnemyStates.GUARD;
        }
        else
        {
            enemyStates= EnemyStates.PATROL;
            GetNewWayPoint();
        }

    }
    void Update()
    {
        SwitchStates();
        SwitchAnimation();
        lastAttackTime -= Time.deltaTime;
    }
    void SwitchAnimation() 
    {
        //����boolֵ
        anim.SetBool("Walk", isWalk);
        anim.SetBool("Chase", isChase);
        anim.SetBool("Follow", isFollow);
        anim.SetBool("Critical",characterStats.isCritical);
    }
    void SwitchStates()
    {
        if (FoundPlayer())
        {
            enemyStates = EnemyStates.CHASE;    //����״̬��Ϊ׷��
            //Debug.Log("����Ŀ��");        
        }


        switch (enemyStates)    //����״̬����
        {
            case EnemyStates.GUARD:
                break;
            case EnemyStates.PATROL:
                /*Ѳ��ģʽ
                 */
                isChase = false;
                agent.speed= speed*0.5f;
                if (Vector3.Distance(wayPoint, transform.position) <= agent.stoppingDistance)
                {
                    isWalk= false;
                    if (remainLookAtTime > 0)
                        remainLookAtTime -= Time.deltaTime;
                    else
                        GetNewWayPoint();
                }
                else
                {
                    isWalk= true;
                    agent.destination= wayPoint;
                }
                break; 
            case EnemyStates.CHASE:
                /*׷��ģʽ
                 * ׷��Ŀ��
                 * ��Ŀ�괦�ڹ�����Χ������й���
                 * ����Ķ����л�
                 * �����޺󷵻���һ��״̬
                 */
                isWalk = false;
                isChase = true;
                agent.speed = speed;
                if (!FoundPlayer())
                {
                    //������
                    isFollow= false;
                    if (remainLookAtTime > 0)
                    {
                        agent.destination = transform.position;
                        remainLookAtTime -= Time.deltaTime;
                    }
                    else if (isGuard)
                        enemyStates = EnemyStates.GUARD;
                    else
                        enemyStates = EnemyStates.PATROL;
                }
                else
                {
                    isFollow= true;
                    agent.isStopped = false;
                    agent.destination = attackTarget.transform.position;    //׷��
                }
                if (TargetInAttackRange() || TargetInSkillRange())
                {
                    isFollow = false;
                    agent.isStopped = true;

                    if (lastAttackTime < 0)
                    {
                        lastAttackTime = characterStats.attackData.coolDown;
                        characterStats.isCritical = Random.value < characterStats.attackData.criticalChance;
                        Attack();
                    }
                }
                break;
            case EnemyStates.DEAD:
                break;
        }
    }

    void Attack()
    {
        transform.LookAt(attackTarget.transform);
        if (TargetInAttackRange())
        {
            anim.SetTrigger("Attack");
        }
        if (TargetInSkillRange())
        {
            anim.SetTrigger("Skill");
        }
    }
    bool FoundPlayer()  //�ж�����Ƿ��ڳ�޷�Χ��
    {
        var colliders=Physics.OverlapSphere(transform.position, sightRadius);   //Physics.OverlapSphere Unity�Դ���һ���״﹦�ܣ����㲢�洢�Ӵ������λ�������ڲ�����ײ�壩����ֵΪCollider[] ��һ�����飬���а���������Ӵ���λ�������ڲ���������ײ�壩
        foreach (var target in colliders)
        {
            if (target.CompareTag("Player"))
            {
                attackTarget = target.gameObject;   //�����޷�Χ
                return true;
            }
        }
        attackTarget = null;    //������
        return false;
    }

    bool TargetInAttackRange()
    {
        if (attackTarget != null)
            return Vector3.Distance(attackTarget.transform.position, transform.position) <= characterStats.attackData.attackRange;
        else
            return false;
    }

    bool TargetInSkillRange()
    {
        if (attackTarget != null)
            return Vector3.Distance(attackTarget.transform.position, transform.position) <= characterStats.attackData.skillRange;
        else
            return false;
    }

    void GetNewWayPoint()   //��ȡ���Ѳ�ߵ�
    {
        remainLookAtTime = lookAtTime;
        float randomX = Random.Range(-patrolRange, patrolRange);
        float randomZ = Random.Range(-patrolRange, patrolRange);
        Vector3 randomPoint = new Vector3(guardPos.x + randomX, transform.position.y, guardPos.z + randomZ);
        NavMeshHit hit;
        wayPoint = NavMesh.SamplePosition(randomPoint, out hit, patrolRange, 1) ? hit.position : transform.position;
    }

    void OnDrawGizmosSelected()     //��ѡ��ʱ��ʾ��Χ
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRadius);     //��ط�Χ
        
    }

}

using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemySoldier))]
public class EnemyMobileSoldier : MonoBehaviour
{
    public enum AIState
    {
        Patrol,
        Follow,
        Attack,
    }

    public Animator animator;
    [Tooltip("Fraction of the enemy's attack range at which it will stop moving towards target while attacking")]
    [Range(0f, 1f)]
    public float attackStopDistanceRatio = 0.5f;
    //[Tooltip("The random hit damage effects")]
    //public ParticleSystem[] randomHitSparks;
    //[Header("Sound")]
    //public AudioClip MovementSound;
    //public MinMaxFloat PitchDistortionMovementSpeed;

    public AIState aiState { get; private set; }
    EnemySoldier m_EnemySoldier;
    //AudioSource m_AudioSource;

    const string k_AnimMoveSpeedParameter = "MoveSpeed";
    const string k_AnimAttackParameter = "Attack";
    const string k_AnimAlertedParameter = "Alerted";
    const string k_AnimOnDamagedParameter = "OnDamaged";




    void Start()
    {
        m_EnemySoldier = GetComponent<EnemySoldier>();
        DebugUtility.HandleErrorIfNullGetComponent<EnemySoldier, EnemyMobileSoldier>(m_EnemySoldier, this, gameObject);

        m_EnemySoldier.onAttack += OnAttack;
        m_EnemySoldier.onDetectedTarget += OnDetectedTarget;
        m_EnemySoldier.onLostTarget += OnLostTarget;
        m_EnemySoldier.SetPathDestinationToClosestNode();
        m_EnemySoldier.onDamaged += OnDamaged;

        // Start patrolling
        aiState = AIState.Patrol;


        // adding a audio source to play the movement sound on it
        //m_AudioSource = GetComponent<AudioSource>();
        //DebugUtility.HandleErrorIfNullGetComponent<AudioSource, EnemyMobile>(m_AudioSource, this, gameObject);
       // m_AudioSource.clip = MovementSound;
        //m_AudioSource.Play();
    }

    void Update()
    {
        UpdateAIStateTransitions();
        UpdateCurrentAIState();

        float moveSpeed = m_EnemySoldier.m_NavMeshAgent.velocity.magnitude;

        // Update animator speed parameter
        //animator.SetFloat(k_AnimMoveSpeedParameter, moveSpeed);

        // changing the pitch of the movement sound depending on the movement speed
        //m_AudioSource.pitch = Mathf.Lerp(PitchDistortionMovementSpeed.min, PitchDistortionMovementSpeed.max, moveSpeed / m_EnemySoldier.m_NavMeshAgent.speed);
    }

    void UpdateAIStateTransitions()
    {
        // Handle transitions 
        switch (aiState)
        {
            case AIState.Follow:
                // Transition to attack when there is a line of sight to the target
                if (m_EnemySoldier.isSeeingTarget && m_EnemySoldier.isTargetInAttackRange)
                {
                    aiState = AIState.Attack;
                    m_EnemySoldier.EnemyActions.Attack();
                    m_EnemySoldier.SetNavDestination(transform.position);
                }
                break;
            case AIState.Attack:
                // Transition to follow when no longer a target in attack range
                if (!m_EnemySoldier.isTargetInAttackRange||!m_EnemySoldier.isSeeingTarget )
                {
                    aiState = AIState.Follow;
                    m_EnemySoldier.EnemyActions.Run();
                    m_EnemySoldier.SetNavDestination(m_EnemySoldier.knownDetectedTarget.transform.position);
                }
                break;
        }
    }

    void UpdateCurrentAIState()
    {
        // Handle logic 
        switch (aiState)
        {
            case AIState.Patrol:
                m_EnemySoldier.EnemyActions.Walk();
                m_EnemySoldier.UpdatePathDestination();
                m_EnemySoldier.SetNavDestination(m_EnemySoldier.GetDestinationOnPath());
                break;
            case AIState.Follow:
                m_EnemySoldier.SetNavDestination(m_EnemySoldier.knownDetectedTarget.transform.position);
                //m_EnemySoldier.OrientWeaponsTowards(m_EnemySoldier.knownDetectedTarget.transform.position);
                break;
            case AIState.Attack:
                if (Vector3.Distance(m_EnemySoldier.knownDetectedTarget.transform.position, m_EnemySoldier.m_DetectionModule.detectionSourcePoint.position) 
                    >= (attackStopDistanceRatio * m_EnemySoldier.m_DetectionModule.attackRange))
                {
                    m_EnemySoldier.SetNavDestination(m_EnemySoldier.knownDetectedTarget.transform.position);
                }
                else
                {
                    m_EnemySoldier.SetNavDestination(transform.position);
                }
                m_EnemySoldier.OrientTowards(m_EnemySoldier.knownDetectedTarget.transform.position);
                m_EnemySoldier.TryAtack(m_EnemySoldier.knownDetectedTarget.transform.position);
                break;
        }
    }

    void OnAttack()
    {   
        Debug.Log("Attacking Player");
        m_EnemySoldier.EnemyActions.Attack();
    }

    void OnDetectedTarget()
    {
        if (aiState == AIState.Patrol)
        {
            aiState = AIState.Follow;
            Debug.Log("Player Detected");
        }
        m_EnemySoldier.EnemyActions.Run();

    }

    void OnLostTarget()
    {
        if (aiState == AIState.Follow || aiState == AIState.Attack)
        {
            aiState = AIState.Patrol;
            Debug.Log("Player Lost");
        }
        m_EnemySoldier.EnemyActions.Walk();
    }

    void OnDamaged()
    {
        /*
        if (randomHitSparks.Length > 0)
        {
            int n = Random.Range(0, randomHitSparks.Length - 1);
            randomHitSparks[n].Play();
        }
        */
        m_EnemySoldier.EnemyActions.Damage();
    }
}

//Put this script on your blue cube.

using System.Collections;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using UnityEngine.Assertions;

public class AgentTouchBack : Agent
{
    [SerializeField]
    public int curr_step;

    [HideInInspector]
    public FightSystem fightSystem;

    //[HideInInspector]
    public int statsId;

    //[HideInInspector]
    public bool statsIsFixed;

    /// <summary>
    /// The ground. The bounds are used to spawn the elements.
    /// </summary>
    [HideInInspector]
    public GameObject ground;

    [HideInInspector]
    public GameObject rayCastCude;

    /// <summary>
    /// The area bounds.
    /// </summary>
    [HideInInspector]
    public Bounds areaBounds;

    TouchBackSettings m_TouchBackSettings;

    /// <summary>
    /// The opponent. He is informed when he is touched.
    /// </summary>
    [SerializeField]
    public GameObject opponent;

    [HideInInspector]
    public AgentTouchBack opponentAgent;

    Rigidbody m_AgentRb;  //cached on initialization
    Material m_GroundMaterial; //cached on Awake()

    [SerializeField]
    GameObject back;

    /// <summary>
    /// Detects when the player has touched an opponent's back.
    /// </summary>
    [HideInInspector]
    public BackDetect backDetect;

    /// <summary>
    /// We will be changing the ground material based on success/failue
    /// </summary>
    Renderer m_GroundRenderer;

    [HideInInspector]
    public bool isAlive;

    EnvironmentParameters m_ResetParams;

    [HideInInspector]
    public int currSaveTime;

    void Awake()
    {
        m_TouchBackSettings = FindObjectOfType<TouchBackSettings>();

        rayCastCude = transform.parent.Find("RayCast").gameObject;

        ground = transform.parent.parent.Find("Ground").gameObject;

        if (m_TouchBackSettings.collectStats) {
            curr_step = 0;
        }

        fightSystem = FindObjectOfType<FightSystem>();

        // Debug.Log("Awake");
        isAlive = true;
        currSaveTime = 0;
    }

    public override void Initialize()
    {
        backDetect = back.GetComponent<BackDetect>();
        backDetect.agent = this;
        backDetect.agentGO = transform.gameObject;

        // Cache the agent rigidbody
        m_AgentRb = GetComponent<Rigidbody>();
        // Get the ground's bounds
        areaBounds = ground.GetComponent<Collider>().bounds;
        // Get the ground renderer so we can change the material when a goal is scored
        m_GroundRenderer = ground.GetComponent<Renderer>();
        // Starting material
        m_GroundMaterial = m_GroundRenderer.material;

        m_ResetParams = Academy.Instance.EnvironmentParameters;

        SetResetParameters();
    }

    /// <summary>
    /// Use the ground's bounds to pick a random spawn position.
    /// </summary>
    public Vector3 GetRandomSpawnPos()
    {

        // if (statsIsFixed) {
        //     return new Vector3(3.0f, 1.2f, 3.0f);
        // } else {
        //     return new Vector3(-3.0f, 1.2f, -3.0f);
        // }


        var foundNewSpawnLocation = false;
        var randomSpawnPos = Vector3.zero;
        while (foundNewSpawnLocation == false)
        {
            var randomPosX = Random.Range(-areaBounds.extents.x * m_TouchBackSettings.spawnAreaMarginMultiplier,
                areaBounds.extents.x * m_TouchBackSettings.spawnAreaMarginMultiplier);

            var randomPosZ = Random.Range(-areaBounds.extents.z * m_TouchBackSettings.spawnAreaMarginMultiplier,
                areaBounds.extents.z * m_TouchBackSettings.spawnAreaMarginMultiplier);
            randomSpawnPos = ground.transform.position + new Vector3(randomPosX, 1f, randomPosZ);
            if (Physics.CheckBox(randomSpawnPos, new Vector3(2.5f, 0.01f, 2.5f)) == false)
            {
                foundNewSpawnLocation = true;
            }
        }
        return randomSpawnPos;
    }

    /// <summary>
    /// Saves statistics after a certain period.
    /// </summary>
    public void CheckSave() {
        if (m_TouchBackSettings.saveTime == currSaveTime) {
            currSaveTime = 0;
            fightSystem.SaveStats(statsId);
        } else {
            currSaveTime++;
        }
        curr_step = 0;
    }

    /// <summary>
    /// Called when the agent moves the block into the goal.
    /// </summary>
    public void TouchedAnOpponent()
    {
        if (m_TouchBackSettings.collectStats && !isAlive) {
            return;
        }

        // Debug.Log($"touched, {statsIsFixed}, {transform.position}\n");

        opponentAgent = opponent.GetComponentInChildren<AgentTouchBack>();

        // Swap ground material for a bit to indicate we scored.
        if (statsIsFixed) {
            StartCoroutine(GoalScoredSwapGroundMaterial(m_TouchBackSettings.goalScoredMaterial, 0.5f));
        } 

        // We use a reward of 5.
        AddReward(5f);
        // Punishing the opponent for losing.
        opponentAgent.AddReward(-2f);

        if (m_TouchBackSettings.collectStats) {
            isAlive = statsIsFixed;
            opponentAgent.isAlive = opponentAgent.statsIsFixed;
        }


        // By marking an agent as done AgentReset() will be called automatically.
        EndEpisode();
        // Marking opponent as done.
        opponentAgent.EndEpisode();

        if (m_TouchBackSettings.collectStats) {
            fightSystem.RecalculateELO(1f, statsId, opponentAgent.statsId);
            
            fightSystem.FindNewOpponent(statsIsFixed ? opponent : transform.parent.gameObject); // this.gameObject

            CheckSave();
            fightSystem.ResetScores(statsId);
        }
    }

    /// <summary>
    /// Swap ground material, wait time seconds, then swap back to the regular material.
    /// </summary>
    IEnumerator GoalScoredSwapGroundMaterial(Material mat, float time)
    {
        m_GroundRenderer.material = mat;
        yield return new WaitForSeconds(time); // Wait for 0.5 sec
        m_GroundRenderer.material = m_GroundMaterial;
    }

    /// <summary>
    /// Moves the agent according to the selected action.
    /// </summary>
    public void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var action = act[0];

        switch (action)
        {
            case 1:
                dirToGo = transform.forward * 1f;
                break;
            case 2:
                dirToGo = transform.forward * -1f;
                break;
            case 3:
                rotateDir = transform.up * 1f;
                break;
            case 4:
                rotateDir = transform.up * -1f;
                break;
            case 5:
                dirToGo = transform.right * -0.75f;
                break;
            case 6:
                dirToGo = transform.right * 0.75f;
                break;
        }
        transform.Rotate(rotateDir, Time.fixedDeltaTime * 200f);
        m_AgentRb.AddForce(dirToGo * m_TouchBackSettings.agentRunSpeed,
            ForceMode.VelocityChange);
    }

    /// <summary>
    /// Reward the agent for focusing (facing) on the opponent.
    /// </summary>
    protected void RewardFocus() {
        RaycastHit hit;

        // Vector3 rayOrigin = new Vector3(transform.position.x + 0.5f, transform.position.y + 0.5f, transform.position.z - 1.1f);

        // float distance = 30.0f;
        // Debug.DrawRay(rayCastCude.transform.position, transform.forward * distance, Color.red, 1.0f);
        if (Physics.Raycast(rayCastCude.transform.position, transform.forward, out hit, 30))
        {
            if (hit.collider.gameObject.CompareTag("agent") || hit.collider.gameObject.CompareTag("goal"))
            {
                // Debug.Log("Agent patrzy na obiekt o tagu:\n");
                if (m_TouchBackSettings.collectStats) {
                    fightSystem.IncFocus(statsId);
                }

                AddReward(0.5f / MaxStep);
            }
        }
    }

    /// <summary>
    /// Punishing for keeping the distance.
    /// </summary>
    protected void PunishingForKeepingTheDistance() {
        // Using 25x25 area results in a distance measure of about 1.
        float distance = Vector3.Distance(transform.position, opponent.transform.position) / 10;
        // Debug.Log($"{distance}");

        if (m_TouchBackSettings.collectStats) {
            fightSystem.IncDistance(statsId, distance);
        }

        AddReward(-distance / MaxStep);
    }

    /// <summary>
    /// Called every step of the engine. Here the agent takes an action.
    /// </summary>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Move the agent using the action.
        MoveAgent(actionBuffers.DiscreteActions);

        curr_step++;
        if (m_TouchBackSettings.collectStats && curr_step >= MaxStep) {
            opponentAgent = opponent.GetComponentInChildren<AgentTouchBack>();

            fightSystem.RecalculateELO(0.5f, statsId, opponentAgent.statsId);
            
            fightSystem.FindNewOpponent(statsIsFixed ? opponent : transform.parent.gameObject);

            CheckSave();
        }

        // Penalty given each step to encourage agent to finish task quickly and not to be far from each other.
        AddReward(-1f / MaxStep);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[0] = 3;
        }
        else if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[0] = 4;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }
    }

    /// <summary>
    /// In the editor, if "Reset On Done" is checked then AgentReset() will be
    /// called automatically anytime we mark done = true in an agent script.
    /// </summary>
    public override void OnEpisodeBegin()
    {        

        // Debug.Log("is alive?\n");
        
        if (m_TouchBackSettings.collectStats && !isAlive) {
            return;
        }

        // Debug.Log("new game\n");

        transform.position = GetRandomSpawnPos();
        m_AgentRb.velocity = Vector3.zero;
        m_AgentRb.angularVelocity = Vector3.zero;

        SetResetParameters();
    }

    public void SetStatsInfo(int id, bool isFixed) {
        statsId = id;
        statsIsFixed = isFixed;
    }

    public void SetGroundMaterialFriction()
    {
        var groundCollider = ground.GetComponent<Collider>();

        groundCollider.material.dynamicFriction = m_ResetParams.GetWithDefault("dynamic_friction", 0);
        groundCollider.material.staticFriction = m_ResetParams.GetWithDefault("static_friction", 0);
    }

    void SetResetParameters()
    {
        SetGroundMaterialFriction();
    }
}

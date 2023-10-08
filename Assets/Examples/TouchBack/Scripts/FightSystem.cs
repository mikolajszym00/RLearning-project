using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class Tuple 
{
    public GameObject go;
    public int id;

    public Tuple(GameObject go, int id) {
        this.go = go;
        this.id = id;
    }
}

public class FightSystem : MonoBehaviour
{
    [SerializeField]
    public GameObject arenaPrefab;

    [SerializeField]
    public List<Tuple> trainedAgentPrefabsIds;

    [SerializeField]
    public int trainedAgentCopies;

    private List<GameObject> arenas;

    [SerializeField]
    public StatsContainerSO statsCont;

    private void InitTrainedAgentPrefabs() {
        int i = 0;
        foreach (StatsSO stat in statsCont.stats) 
        {
            if (stat.toTrain) {
                trainedAgentPrefabsIds.Add(new Tuple(stat.agentPrefab, i));
            }

            i++;
        }
    }

    /// <summary>
    /// Initiates a trained agent based on the arena and the position of the prefab on the statsContainer list.
    /// </summary>
    private GameObject InitTrainedAgent(GameObject parent, int prefabId) {
        GameObject newAgent = Instantiate(trainedAgentPrefabsIds[prefabId].go, parent.transform);
        newAgent.GetComponentInChildren<AgentTouchBack>().SetStatsInfo(trainedAgentPrefabsIds[prefabId].id, true);
        // newAgent.GetComponent<AgentTouchBack>().SetStatsInfo(trainedAgentPrefabsIds[prefabId].id, true);


        return newAgent;
    }

    private Vector3 DetermineAreaPos(int trainedAgentsNum, int currI) {
        return new Vector3(50 * (currI / trainedAgentsNum), 50 * (currI % trainedAgentsNum), 0);
    }

    /// <summary>
    /// Initiates train arenas, creates a trained agent and selects randomly its opponent.
    /// </summary>
    private void InitArenas() {
        int trainedAgentsNum = trainedAgentPrefabsIds.Count;

        for (int i = 0; i < trainedAgentsNum * trainedAgentCopies; i++) {
            Vector3 areaPos = DetermineAreaPos(trainedAgentsNum, i);

            GameObject newArena = Instantiate(arenaPrefab, areaPos, Quaternion.identity);
            arenas.Add(newArena);

            GameObject op = InitTrainedAgent(newArena, i % trainedAgentsNum);
            FindOpponent(areaPos, newArena, op);
        }
    }

    void Start() {
        trainedAgentPrefabsIds = new List<Tuple>();

        InitTrainedAgentPrefabs();

        arenas = new List<GameObject>();

        InitArenas();
    }

    /// <summary>
    /// Sets up opponent.
    /// </summary>
    private void SetOpponents(GameObject agent1, GameObject agent2) {
        agent1.GetComponentInChildren<AgentTouchBack>().opponent = agent2;
        agent2.GetComponentInChildren<AgentTouchBack>().opponent = agent1;
        
    }

    /// <summary>
    /// Instantiate opponent with given position and parent.
    /// </summary>
    public void FindOpponent(Vector3 pos, GameObject parent, GameObject secondAgent) {
        // Debug.Log("find oppoenent");

        int selectedId = Random.Range(0, statsCont.GetListLength());

        GameObject agentPrefab = statsCont.stats[selectedId].agentPrefab;

        GameObject newAgent = Instantiate(agentPrefab, parent.transform);
        newAgent.transform.position = pos;
        newAgent.GetComponentInChildren<AgentTouchBack>().SetStatsInfo(selectedId, false);
        // newAgent.GetComponent<AgentTouchBack>().SetStatsInfo(selectedId, false);

        SetOpponents(newAgent, secondAgent);
    }


    /// <summary>
    /// Prepare to instantiate opponent with given position and parent by examining previous opponent.
    /// </summary>
    public void FindNewOpponent(GameObject opponent) {
        Vector3 pos = opponent.transform.position;
        GameObject parent = opponent.transform.parent.gameObject;

        // GameObject op = opponent.GetComponent<AgentTouchBack>().opponent;
        GameObject op = opponent.GetComponentInChildren<AgentTouchBack>().opponent;

        Destroy(opponent);

        FindOpponent(pos, parent, op);
    }

    public void ResetScores(int agentId) {
        StatsSO agentStats = statsCont.stats[agentId];

        agentStats.focusScore = 0;
        agentStats.distScore = 0;
    }

    /// <summary>
    /// Recalculate ELO for agent and opponent.
    /// </summary>
    public void RecalculateELO(float win, int agentId, int opponentId) {
        if (agentId == opponentId) {
            // Debug.Log("on sam");
            return;
        }

        StatsSO agentStats = statsCont.stats[agentId];
        StatsSO opponentStats = statsCont.stats[opponentId];

        int K = 32;
        int old_ELO = opponentStats.ELO;
        float EA = 1 / (1 + Mathf.Pow(10, (opponentStats.ELO - agentStats.ELO) / 400.0f));
        agentStats.ELO = agentStats.ELO + (int)(K * (win - EA));

        EA = 1 / (1 + Mathf.Pow(10, (old_ELO - opponentStats.ELO) / 400.0f));
        opponentStats.ELO = opponentStats.ELO + (int)(K * ((1 - win) - EA)); // TODO czy aktualizuje oponenta
    }

    public void SaveStats(int agentId) { // to powinna byc srednia
        StatsSO agentStats = statsCont.stats[agentId];

        using (StreamWriter writer = new StreamWriter(Application.dataPath + "/mydata.txt", true)) // TODO rózne pliki dla róznych modeli
        {
            writer.WriteLine($"{agentStats.ELO},{agentStats.focusScore},{agentStats.distScore}");
        }

        ResetScores(agentId);
    }

    /// <summary>
    /// Increases focus score;
    /// </summary>
    public void IncFocus(int agentId) {
        StatsSO agentStats = statsCont.stats[agentId];

        agentStats.focusScore++;
    }

    /// <summary>
    /// Increases distance score;
    /// </summary>
    public void IncDistance(int agentId, float distance) {
        StatsSO agentStats = statsCont.stats[agentId];

        agentStats.distScore += distance;
    }
}

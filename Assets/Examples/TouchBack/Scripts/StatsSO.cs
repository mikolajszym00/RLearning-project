using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AgentStats")]
public class StatsSO : ScriptableObject
{
    /// <summary>
    /// Inform whether the agant is currently trained.
    /// </summary>
    public bool toTrain;
    public int ELO;
    public int focusScore;
    public float distScore;

    public GameObject agentPrefab;
}

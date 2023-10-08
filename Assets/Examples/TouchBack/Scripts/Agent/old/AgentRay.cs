using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;


public class AgentRay : AgentTouchBack
{
    /// <summary>
    /// Called every step of the engine. Here the agent takes an action.
    /// </summary>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Move the agent using the action.
        MoveAgent(actionBuffers.DiscreteActions);

        float distance = Vector3.Distance(transform.position, opponent.transform.position) / 10; // 35 to przybliżona przekątna planszy
        // Debug.Log(distance);

        // Penalty given each step to encourage agent to finish task quickly and not to be far from each other.
        AddReward(-(1f + distance) / MaxStep);
    }
}

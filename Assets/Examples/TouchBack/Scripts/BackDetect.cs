using UnityEngine;

public class BackDetect : MonoBehaviour
{
    /// <summary>
    /// The associated agent.
    /// This will be set by the agent script on Initialization.
    /// Don't need to manually set.
    /// </summary>
    [HideInInspector]
    public AgentTouchBack agent;

    [HideInInspector]
    public GameObject agentGO;

    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject == agentGO) {
            Debug.Log("Dotknal sam siebie!");
        }
        // Touched goal.
        if (col.gameObject.CompareTag("agent"))
        {
            // Debug.Log("agent!");
            agent.TouchedAnOpponent();
        }
    }
}

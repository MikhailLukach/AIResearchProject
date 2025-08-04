using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class FormationMaintainer : MonoBehaviour
{
    public float correctionThreshold = 1.5f; // Distance tolerance before correction
    public float checkInterval = 0.5f;

    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= checkInterval)
        {
            timer = 0f;
            MaintainFormation();
        }
    }

    void MaintainFormation()
    {
        AgentSelection[] agents = FindObjectsOfType<AgentSelection>();

        foreach (var agent in agents)
        {
            if (!agent.IsSelected && agent.AssignedSlotPosition.HasValue)
            {
                float dist = Vector3.Distance(agent.transform.position, agent.AssignedSlotPosition.Value);

                if (dist > correctionThreshold && agent.Agent.remainingDistance < 0.1f)
                {
                    agent.Agent.SetDestination(agent.AssignedSlotPosition.Value);
                }
            }
        }
    }
}

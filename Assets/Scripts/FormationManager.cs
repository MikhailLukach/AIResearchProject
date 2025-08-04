using UnityEngine;
using System.Collections.Generic;

public class FormationManager : MonoBehaviour
{
    [SerializeField]
    private AgentSelectionManager selectionManager;

    [SerializeField]
    private Transform formationAnchor;

    [SerializeField]
    private float spacing = 2.0f;

    [SerializeField]
    private int maxPerRow = 20;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            FormLine();
        }
    }

    public void FormLine()
    {
        List<AgentSelection> agents = selectionManager.GetSelectedAgents();
        if (agents.Count == 0) return;

        // Step 1: Compute center
        Vector3 center = Vector3.zero;
        foreach (var agent in agents)
        {
            center += agent.transform.position;
        }
        center /= agents.Count;

        // Step 2: Generate formation slots
        List<Vector3> slotPositions = new();
        int numAgents = agents.Count;

        for (int i = 0; i < numAgents; i++)
        {
            int row = i / maxPerRow;
            int col = i % maxPerRow;
            Vector3 offset = new Vector3(col * spacing, 0f, -row * spacing);
            Vector3 slot = center + offset;

            slot.y = agents[0].transform.position.y;

            slotPositions.Add(slot);
        }

        // Step 3: Assign nearest slot to each agent
        HashSet<int> assignedSlots = new();
        foreach (var agent in agents)
        {
            float bestDistance = float.MaxValue;
            int bestSlotIndex = -1;

            for (int i = 0; i < slotPositions.Count; i++)
            {
                if (assignedSlots.Contains(i)) continue;

                float dist = Vector3.SqrMagnitude(slotPositions[i] - agent.transform.position);
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    bestSlotIndex = i;
                }
            }

            if (bestSlotIndex >= 0)
            {
                assignedSlots.Add(bestSlotIndex);
                //agent.Agent.SetDestination(slotPositions[bestSlotIndex]);
                agent.AssignSlot(slotPositions[bestSlotIndex]);
            }
        }

        Vector3 min = center + new Vector3(-spacing, 0, -((agents.Count / maxPerRow) + 1) * spacing);
        Vector3 max = center + new Vector3(maxPerRow * spacing + spacing, 0, spacing);
        Bounds formationBounds = new Bounds();
        formationBounds.SetMinMax(min, max);

        // 4. Find all unselected agents
        AgentSelection[] allAgents = FindObjectsOfType<AgentSelection>();
        foreach (var other in allAgents)
        {
            if (agents.Contains(other)) continue;

            Vector3 pos = other.transform.position;
            if (formationBounds.Contains(new Vector3(pos.x, center.y, pos.z)))
            {
                // 5. Move the unselected agent away

                Vector3 away = (pos - center).normalized;
                Vector3 target = pos + away * 5f;

                other.Agent.SetDestination(target);
            }
        }
    }
}

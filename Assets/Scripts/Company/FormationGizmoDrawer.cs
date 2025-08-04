using UnityEngine;

[ExecuteAlways]
public class FormationGizmoDrawer : MonoBehaviour
{
    [SerializeField] private CompanyController company;
    [SerializeField] private float gizmoRadius = 0.25f;
    [SerializeField] private Color slotColor = Color.green;
    [SerializeField] private Color agentColor = Color.cyan;
    [SerializeField] private bool showSlotNumbers = true;

    private void OnDrawGizmos()
    {
        if (company == null || company.agents == null || company.agents.Count == 0) return;

        // Force update slot layout for current center (only for drawing)
        var slots = company.GenerateDebugSlotPositions();

        for (int i = 0; i < slots.Count; i++)
        {
            Gizmos.color = slotColor;
            Gizmos.DrawSphere(slots[i], gizmoRadius);

            if (showSlotNumbers)
            {
#if UNITY_EDITOR
                UnityEditor.Handles.Label(slots[i] + Vector3.up * 0.5f, $"Slot {i}");
#endif
            }
        }

        foreach (var agent in company.agents)
        {
            if (agent.AssignedSlotPosition.HasValue)
            {
                Gizmos.color = agentColor;
                Gizmos.DrawLine(agent.transform.position, agent.AssignedSlotPosition.Value);
            }
        }
    }
}

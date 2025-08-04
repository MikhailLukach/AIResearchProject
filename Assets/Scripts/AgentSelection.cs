using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AgentSelection : MonoBehaviour
{
    private Renderer m_Renderer;
    private Color m_DefaultColor;
    private static readonly Color SelectedColor = Color.yellow;

    public bool IsSelected { get; private set; } = false;

    public NavMeshAgent Agent { get; private set; }

    public Vector3? AssignedSlotPosition { get; private set; }
    public int AssignedSlotIndex { get; set; } = -1;

    private bool isSteppingAside = false;

    private bool hasDiverged = false;

    private void Awake()
    {
        m_Renderer = GetComponent<Renderer>();
        m_DefaultColor = m_Renderer.material.color;
        Agent = GetComponent<NavMeshAgent>();

        Agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        Agent.avoidancePriority = Random.Range(0, 100);
    }

    private void Update()
    {
        if (AssignedSlotPosition.HasValue &&
            Vector3.Distance(transform.position, AssignedSlotPosition.Value) > 0.5f)
        {
            if (Vector3.Distance(Agent.destination, AssignedSlotPosition.Value) > 0.5f)
            {
                if (!hasDiverged)
                {
                    //Debug.LogWarning($"[AgentSelection] {name} diverged from slot {AssignedSlotIndex}. Reissuing destination.");
                    hasDiverged = true;
                }

                Agent.SetDestination(AssignedSlotPosition.Value);
            }
        }
        else
        {
            hasDiverged = false;
        }
    }

    public void ToggleSelection() 
    {
        IsSelected = !IsSelected;
        m_Renderer.material.color = IsSelected ? SelectedColor : m_DefaultColor;
    }

    public void Deselect()
    {
        IsSelected = false;
        m_Renderer.material.color = m_DefaultColor;
    }

    public void AssignSlot(Vector3 position, int slotIndex)
    {
        if (AssignedSlotIndex != -1 && AssignedSlotIndex != slotIndex)
        {
            Debug.LogWarning($"[AgentSelection] Slot reassignment detected on {gameObject.name}: {AssignedSlotIndex} -> {slotIndex}");
        }

        AssignedSlotPosition = position;
        AssignedSlotIndex = slotIndex;
        Agent.SetDestination(position);
    }


    public void AssignSlot(Vector3 slot)
    {
        if (AssignedSlotPosition.HasValue)
        {
            float distance = Vector3.Distance(AssignedSlotPosition.Value, slot);
            if (distance > 0.5f)
            {
                Debug.LogWarning($"[AssignSlot] {gameObject.name} reassigned to a different slot. " +
                                 $"Old: {AssignedSlotPosition.Value}, New: {slot}, Distance: {distance}");
            }
        }

        AssignedSlotPosition = slot;
        Agent.SetDestination(slot);
    }

    public void TryStepAside(List<Vector3> allTargetSlots)
    {
        if (isSteppingAside || AssignedSlotPosition == null) return;

        Vector3 mySlot = AssignedSlotPosition.Value;

        // If another target is close to my position but isn't mine
        foreach (var target in allTargetSlots)
        {
            if (target == mySlot) continue;

            float dist = Vector3.Distance(transform.position, target);
            if (dist < 1.5f)
            {
                StartCoroutine(StepAsideRoutine());
                break;
            }
        }
    }

    private IEnumerator StepAsideRoutine()
    {
        isSteppingAside = true;

        Vector3[] directions = {
        transform.right,
        -transform.right,
        -transform.forward,
        transform.forward
    };

        foreach (var dir in directions)
        {
            Vector3 testPos = transform.position + dir * 1.5f;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(testPos, out hit, 2f, NavMesh.AllAreas))
            {
                Agent.SetDestination(hit.position);
                break;
            }
        }

        yield return new WaitForSeconds(1.2f);

        if (AssignedSlotPosition.HasValue)
            Agent.SetDestination(AssignedSlotPosition.Value);

        isSteppingAside = false;
    }
}

using UnityEngine;
using System.Collections.Generic;
using Unity.Behavior;

public enum CompanyFormation
{
    Line,
    Skirmisher
}

public class CompanyController : MonoBehaviour
{
    [Header("Company Settings")]
    public List<AgentSelection> agents = new();
    public CompanyFormation currentFormation = CompanyFormation.Line;

    [SerializeField] private float spacing = 2f;
    [SerializeField] private int maxPerRow = 20;

    [SerializeField] private BehaviorGraphAgent graphAgent;

    [SerializeField] private float slotTolerance = 1.0f;

    [SerializeField] private float marchTolerance = 2f;

    [SerializeField] private GameObject slotVisualPrefab;

    public BattalionController Battalion { get; private set; }

    private List<GameObject> slotVisuals = new();

    private Vector3 formationRight;

    private Vector3 cachedFormationCenter;

    private Vector3 marchTarget;

    private Vector3 targetCameraRight;

    private bool isMarching = false;

    private bool followCenter = true;

    private bool useCustomRotationDirection = false;

    private bool isInBattalionMode = false;

    private void Start()
    {
        if (graphAgent == null)
        {
            graphAgent = GetComponent<BehaviorGraphAgent>();
            if (graphAgent == null)
            {
                Debug.LogError("[CompanyController] BehaviorAgent not assigned!");
                return;
            }
        }

        graphAgent.SetVariableValue("Company", this);
        graphAgent.SetVariableValue("IsBlockingOtherCompany", false);
        graphAgent.SetVariableValue("ShouldHold", false);
        graphAgent.SetVariableValue("IsAdvancing", false);
        graphAgent.SetVariableValue("AllSlotsFilled", false);

        graphAgent.SetVariableValue("IsAlignedToTarget", true);

        graphAgent.SetVariableValue("TargetPosition", transform.position);
    }

    private void Update()
    {
        if (followCenter && agents.Count > 0)
        {
            transform.position = ComputeGroupCenter();
        }

        if (isMarching)
        {
            UpdateMarchSlotDestinations();

            if (HasReachedMarchTarget())
            {
                isMarching = false;
                graphAgent.SetVariableValue("IsAdvancing", false);
                graphAgent.SetVariableValue("ShouldHold", true);
            }
        }

        UpdateSlotFillStatus();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(cachedFormationCenter, 0.3f);
    }

    private void UpdateSlotFillStatus()
    {
        if (graphAgent == null || agents.Count == 0) return;

        int filled = 0;

        foreach (var agent in agents)
        {
            if (!agent.AssignedSlotPosition.HasValue) continue;

            float dist = Vector3.Distance(agent.transform.position, agent.AssignedSlotPosition.Value);
            if (dist < slotTolerance)
                filled++;
        }

        bool allInPlace = filled >= agents.Count;

        if (graphAgent.GetVariable<bool>("AllSlotsFilled", out var blackboardVar))
        {
            if (blackboardVar.Value != allInPlace)
            {
                blackboardVar.Value = allInPlace;
            }
        }
        else
        {
            graphAgent.SetVariableValue("AllSlotsFilled", allInPlace);
        }
    }

    public void FormFormation(CompanyFormation formationType, bool fixedIndex = false)
    {
        if (agents == null || agents.Count == 0) return;

        currentFormation = formationType;

        // Use cached center if not first time
        if (cachedFormationCenter == Vector3.zero)
            cachedFormationCenter = ComputeGroupCenter();

        List<Vector3> slotPositions = GenerateFormationSlots(
                    cachedFormationCenter,
                    formationRight == Vector3.zero ? Vector3.right : formationRight,
                    formationType
                    );

        AssignSlotsToAgents(slotPositions, cachedFormationCenter, fixedIndex);
    }

    private Vector3 ComputeGroupCenter()
    {
        Vector3 center = Vector3.zero;
        foreach (var agent in agents)
        {
            center += agent.transform.position;
        }
        return center / agents.Count;
    }

    public List<Vector3> GenerateFormationSlots(Vector3 center, CompanyFormation formation)
    {
        List<Vector3> slotPositions = new();

        if (formation == CompanyFormation.Line)
        {
            int rowCount = Mathf.CeilToInt(agents.Count / (float)maxPerRow);
            float totalWidth = (maxPerRow - 1) * spacing;
            float totalDepth = (rowCount - 1) * spacing;

            for (int i = 0; i < agents.Count; i++)
            {
                int row = i / maxPerRow;
                int col = i % maxPerRow;

                Vector3 offset = new Vector3(
                    col * spacing - totalWidth / 2f,
                    0f,
                    -row * spacing + totalDepth / 2f
                );

                Vector3 slot = center + offset;
                slot.y = agents[0].transform.position.y;

                slotPositions.Add(slot);
            }
        }

        return slotPositions;
    }

    public List<Vector3> GenerateFormationSlots(Vector3 center, Vector3 right, CompanyFormation formation)
    {
        List<Vector3> slotPositions = new();

        if (formation == CompanyFormation.Line)
        {
            int rowCount = Mathf.CeilToInt(agents.Count / (float)maxPerRow);
            float totalWidth = (maxPerRow - 1) * spacing;
            float totalDepth = (rowCount - 1) * spacing;

            Vector3 forward = Vector3.Cross(right, Vector3.up).normalized;

            for (int i = 0; i < agents.Count; i++)
            {
                int row = i / maxPerRow;
                int col = i % maxPerRow;

                Vector3 offset =
                    right * (col * spacing - totalWidth / 2f) +
                    -forward * (row * spacing - totalDepth / 2f);

                Vector3 slot = center + offset;
                slot.y = agents[0].transform.position.y;
                slotPositions.Add(slot);
            }
        }

        return slotPositions;
    }

    public void AssignSlotsToAgents(List<Vector3> slots, Vector3 center, bool fixedIndex = false)
    {
        foreach (var visual in slotVisuals)
        {
            if (visual) Destroy(visual);
        }
        slotVisuals.Clear();

        if (fixedIndex)
        {
            foreach (var agent in agents)
            {
                int i = agent.AssignedSlotIndex;
                if (i < 0 || i >= slots.Count) continue;

                agent.AssignSlot(slots[i], i);
                agent.Agent.avoidancePriority = 50 + i;
            }
        }
        else // initial dynamic matching
        {
            HashSet<int> assigned = new();

            for (int agentIndex = 0; agentIndex < agents.Count; agentIndex++)
            {
                var agent = agents[agentIndex];
                float bestDist = float.MaxValue;
                int bestIndex = -1;

                for (int i = 0; i < slots.Count; i++)
                {
                    if (assigned.Contains(i)) continue;

                    float dist = Vector3.SqrMagnitude(agent.transform.position - slots[i]);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestIndex = i;
                    }
                }

                if (bestIndex != -1)
                {
                    assigned.Add(bestIndex);
                    agent.AssignSlot(slots[bestIndex], bestIndex);
                    agent.Agent.avoidancePriority = 50 + bestIndex;
                }
            }
        }

        for (int i = 0; i < slots.Count; i++)
        {
            var visual = Instantiate(slotVisualPrefab, slots[i], Quaternion.identity);
            visual.name = $"SlotVisual_{i}";
            slotVisuals.Add(visual);
        }

        if (!isInBattalionMode)
        {
            ClearUnselectedFromBounds(center);
        }

        HashSet<int> indices = new();
        foreach (var agent in agents)
        {
            if (!indices.Add(agent.AssignedSlotIndex))
            {
                //Debug.LogError($"[CompanyController] Duplicate slot index: {agent.AssignedSlotIndex} in {name}");
            }
        }
    }

    private void ClearUnselectedFromBounds(Vector3 center)
    {
        float totalWidth = maxPerRow * spacing + spacing;
        float totalDepth = ((agents.Count / maxPerRow) + 1) * spacing;

        Vector3 min = center + new Vector3(-spacing, 0, -totalDepth);
        Vector3 max = center + new Vector3(totalWidth, 0, spacing);
        Bounds formationBounds = new Bounds();
        formationBounds.SetMinMax(min, max);

        AgentSelection[] allAgents = FindObjectsOfType<AgentSelection>();
        foreach (var other in allAgents)
        {
            if (agents.Contains(other)) continue;

            Vector3 pos = other.transform.position;
            if (formationBounds.Contains(new Vector3(pos.x, center.y, pos.z)))
            {
                Vector3 away = (pos - center).normalized;
                Vector3 target = pos + away * 5f;
                other.Agent.SetDestination(target);
            }
        }
    }

    public void CommandStepAside()
    {
        Vector3 center = ComputeGroupCenter();
        List<Vector3> slotPositions = GenerateFormationSlots(center, currentFormation);

        foreach (var agent in agents)
        {
            agent.TryStepAside(slotPositions);
        }
    }

    public void HoldCommand() 
    {
        foreach (var agent in agents) 
        {
            agent.Agent.isStopped = true;
        }
    }

    public void ResumeMovement() 
    {
        /*foreach(var agent in agents) 
        {
            agent.Agent.isStopped = false;
            if (agent.AssignedSlotPosition.HasValue)
            {
                agent.Agent.SetDestination(agent.AssignedSlotPosition.Value);
            }
        }*/

        foreach (var agent in agents)
        {
            agent.Agent.isStopped = false;

            if (agent.AssignedSlotPosition.HasValue)
            {
                if (Vector3.Distance(agent.Agent.destination, agent.AssignedSlotPosition.Value) > 0.5f)
                {
                    agent.Agent.SetDestination(agent.AssignedSlotPosition.Value);
                }
            }
        }
    }

    public void BeginMarchTo(Vector3 target)
    {
        //Debug.Log("[CompanyController] BeginMarchTo called");
        isMarching = true;
        marchTarget = target;

        if (ShouldUseCustomRotationDirection())
        {
            // Use custom direction assigned earlier
            formationRight = GetTargetCameraRight();
        }
        else
        {
            // Default: camera-aligned formation right
            Vector3 cameraForward = Camera.main.transform.forward;
            cameraForward.y = 0f;
            cameraForward.Normalize();
            formationRight = Vector3.Cross(Vector3.up, cameraForward);

            SetTargetCameraRight(formationRight); // also update for consistency
        }

        cachedFormationCenter = target;
        ResumeMovement();

        UpdateMarchSlotDestinations(); // call once to initialize
    }

    public bool HasReachedMarchTarget()
    {
        foreach (var agent in agents)
        {
            if (!agent.AssignedSlotPosition.HasValue) return false;

            float dist = Vector3.Distance(agent.transform.position, agent.AssignedSlotPosition.Value);
            if (dist > marchTolerance)
                return false;
        }

        return true;
    }

    private void UpdateMarchSlotDestinations()
    {
        cachedFormationCenter = Vector3.MoveTowards(cachedFormationCenter, marchTarget, Time.deltaTime * 2f);

        List<Vector3> slotPositions = GenerateFormationSlots(cachedFormationCenter, formationRight, currentFormation);
        AssignSlotsToAgents(slotPositions, cachedFormationCenter, true);
    }

    public List<Vector3> GenerateDebugSlotPositions()
    {
        return GenerateFormationSlots(cachedFormationCenter, currentFormation);
    }

    public Vector3 GetFormationRight() => formationRight;

    public void SetFormationRight(Vector3 right) => formationRight = right.normalized;

    public Vector3 GetCameraRight()
    {
        var camForward = Camera.main.transform.forward;
        camForward.y = 0f;
        camForward.Normalize();
        return Vector3.Cross(Vector3.up, camForward);
    }

    public BehaviorGraphAgent GetGraphAgent() => graphAgent;

    public Vector3 GetCachedFormationCenter() => cachedFormationCenter;

    public void SetCachedFormationCenter(Vector3 center)
    {
        cachedFormationCenter = center;
    }

    public void SetTargetCameraRight(Vector3 right)
    {
        targetCameraRight = right.normalized;
    }

    public Vector3 GetTargetCameraRight()
    {
        return targetCameraRight;
    }

    public void SortAgentsByFormationRight()
    {
        if (agents == null || agents.Count == 0 || formationRight == Vector3.zero)
            return;

        Vector3 center = GetCachedFormationCenter();
        Vector3 right = formationRight.normalized;

        agents.Sort((a, b) =>
        {
            float aOffset = Vector3.Dot(a.transform.position - center, right);
            float bOffset = Vector3.Dot(b.transform.position - center, right);
            return aOffset.CompareTo(bOffset);
        });

        for (int i = 0; i < agents.Count; i++)
        {
            agents[i].AssignedSlotIndex = i;
        }
    }

    public void AssignToBattalion(BattalionController battalion)
    {
        Battalion = battalion;
    }

    public void UseCustomRotationDirection(bool enabled)
    {
        useCustomRotationDirection = enabled;
    }

    public bool ShouldUseCustomRotationDirection() => useCustomRotationDirection;

    public void SetBattalionMode(bool enabled)
    {
        isInBattalionMode = enabled;
    }
}

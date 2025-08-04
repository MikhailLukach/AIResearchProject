using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "RotateToFormationAlignment", story: "Rotate [Self] to formation alignment if not [IsAlignedToTarget] until [AllSlotsFilled] only if [IsAdvancing]", category: "Action", id: "72b157d2186473ac3087ab2a8909cca8")]
public partial class RotateToFormationAlignmentAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<bool> IsAlignedToTarget;
    [SerializeReference] public BlackboardVariable<bool> AllSlotsFilled;
    [SerializeReference] public BlackboardVariable<bool> IsAdvancing;
    private float rotationSpeed = 10f; // degrees per second
    private const float alignThreshold = 5f; // degrees

    protected override Status OnStart()
    {
        if (!Self.Value || IsAlignedToTarget == null || AllSlotsFilled == null || !IsAdvancing.Value)
            return Status.Failure;

        var controller = Self.Value.GetComponent<CompanyController>();
        if (!controller) return Status.Failure;

        AllSlotsFilled.Value = false;
        IsAlignedToTarget.Value = false;

        Vector3 right;

        if (controller.ShouldUseCustomRotationDirection())
        {
            right = controller.GetTargetCameraRight(); 
        }
        else
        {
            right = controller.GetCameraRight(); 
        }

        var center = controller.GetCachedFormationCenter();
        var slots = controller.GenerateFormationSlots(center, right, controller.currentFormation);

        controller.SetFormationRight(right); // update right vector now
        controller.AssignSlotsToAgents(slots, center, fixedIndex: true);
        controller.ResumeMovement(); // agents start moving into new intermediate slots

        //Debug.Log("[AlignRotationByStaging] Intermediate formation set, waiting for alignment.");
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (!Self.Value || AllSlotsFilled == null || IsAlignedToTarget == null)
            return Status.Failure;

        var controller = Self.Value.GetComponent<CompanyController>();
        if (!controller) return Status.Failure;

        if (AllSlotsFilled.Value)
        {
            IsAlignedToTarget.Value = true;
            //Debug.Log("[AlignRotationByStaging] Alignment complete.");
            return Status.Success;
        }

        // Smooth rotation logic
        Vector3 currentRight = controller.GetFormationRight();
        Vector3 desiredRight = controller.GetTargetCameraRight();

        float angle = -Vector3.SignedAngle(currentRight, desiredRight, Vector3.up);

        if (Mathf.Abs(angle) > 0.1f)
        {
            float step = Mathf.Sign(angle) * rotationSpeed * Time.deltaTime;
            if (Mathf.Abs(step) > Mathf.Abs(angle)) step = angle;

            Quaternion rotation = Quaternion.AngleAxis(step, Vector3.up);
            Vector3 center = controller.GetCachedFormationCenter();

            foreach (var agent in controller.agents)
            {
                Vector3 offset = agent.transform.position - center;
                Vector3 rotated = rotation * offset;
                agent.transform.position = center + rotated;
            }

            controller.SetFormationRight(rotation * currentRight);
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
    }
}


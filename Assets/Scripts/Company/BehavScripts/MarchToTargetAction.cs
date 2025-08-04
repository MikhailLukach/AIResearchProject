using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "MarchToTargetAction", story: "[Company] [IsAdvancing] to [TargetPosition] and when reached [ShouldHold] only if [IsAlignedToTarget]", category: "Action", id: "3b324e26a7275ae45fc57a3968a8dcd6")]
public partial class MarchToTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Company;
    [SerializeReference] public BlackboardVariable<bool> IsAdvancing;
    [SerializeReference] public BlackboardVariable<Vector3> TargetPosition;
    [SerializeReference] public BlackboardVariable<bool> ShouldHold;
    [SerializeReference] public BlackboardVariable<bool> IsAlignedToTarget;

    protected override Status OnStart()
    {
        Debug.Log("[MarchToTargetAction] OnStart entered");

        if (!Company.Value || !IsAlignedToTarget.Value || !IsAdvancing.Value) 
        {
            Debug.LogWarning("[MarchToTargetAction] Conditions not met");
            return Status.Failure;
        }

        Debug.Log("[MarchToTargetAction] Starting march");
        Company.Value.GetComponent<CompanyController>().BeginMarchTo(TargetPosition.Value);
        //IsAdvancing.Value = true;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (!Company.Value || !IsAdvancing.Value)
            return Status.Failure;

        if (Company.Value.GetComponent<CompanyController>().HasReachedMarchTarget())
        {
            // Stop marching and hold
            IsAdvancing.Value = false;
            ShouldHold.Value = true;
            return Status.Success;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
    }
}


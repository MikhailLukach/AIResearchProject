using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "HoldFormation", story: "[Company] holds formation if they [ShouldHold] and [AllSlotsFilled]", category: "Action", id: "7f9655bcd9758eda7f9346c09ae47af9")]
public partial class HoldFormationAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Company;
    [SerializeReference] public BlackboardVariable<bool> ShouldHold;
    [SerializeReference] public BlackboardVariable<bool> AllSlotsFilled;
    protected override Status OnStart()
    {
        if (!Company.Value || !ShouldHold.Value || !AllSlotsFilled.Value)
        {
            return Status.Failure;
        }

        Company.Value.GetComponent<CompanyController>().HoldCommand();
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (!ShouldHold.Value || !AllSlotsFilled.Value)
        {
            return Status.Failure;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (Company.Value)
        {
            Company.Value.GetComponent<CompanyController>().ResumeMovement();
        }
    }
}


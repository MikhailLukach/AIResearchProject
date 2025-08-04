using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "AlignFormationIfScatteredAction", story: "Align formation of [company] if [AllSlotsFilled] isn't true", category: "Action", id: "9dbea36b75b6e9d71cc28fc10bacb9b1")]
public partial class AlignFormationIfScatteredAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Company;
    [SerializeReference] public BlackboardVariable<bool> AllSlotsFilled;

    protected override Status OnStart()
    {
        if (!Company.Value || AllSlotsFilled.Value)
            return Status.Failure;

        Company.Value.GetComponent<CompanyController>().FormFormation(Company.Value.GetComponent<CompanyController>().currentFormation, true);
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        return AllSlotsFilled.Value ? Status.Success : Status.Running;
    }

    protected override void OnEnd() { }
}


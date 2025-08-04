using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "StepAsideIfBlockingAction", story: "[Company] will step aside if [blocking]", category: "Action", id: "cca0bb1a1acda7c8d2cc56b616cad5fc")]
public partial class StepAsideIfBlockingAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Company;
    [SerializeReference] public BlackboardVariable<bool> Blocking;

    protected override Status OnStart()
    {
        if (!Company.Value || !Blocking.Value)
            return Status.Failure;

        //Company.Value.GetComponent<CompanyController>().CommandStepAside();
        return Status.Success;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}


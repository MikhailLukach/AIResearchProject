using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum FormationState
{
    None,
    Line,
    Column,
    Square
}

public class BattalionController : MonoBehaviour
{
    public List<CompanyController> Companies { get; private set; }

    void Start()
    {
        Debug.Log($"[Battalion] Formed with {Companies.Count} companies.");
    }

    public void Initialize(List<CompanyController> companies)
    {
        Companies = companies;

        foreach (var company in Companies)
        {
            company.transform.parent = transform;
            company.AssignToBattalion(this);
            //Debug.Log($"[BattalionController] Assigned company {company.name}");
            //Debug.Log($"[BattalionController] Initialized with {companies.Count} companies");
        }
    }

    public Vector3 GetAveragePosition()
    {
        Vector3 center = Vector3.zero;
        int count = 0;

        foreach (var company in Companies)
        {
            center += company.transform.position;
            count++;
        }

        return count > 0 ? center / count : transform.position;
    }

    public bool AreAllCompaniesSelected(List<AgentSelection> selectedAgents)
    {
        foreach (var company in Companies)
        {
            foreach (var agent in company.agents)
            {
                if (!selectedAgents.Contains(agent))
                    return false;
            }
        }
        return true;
    }

    public FormationState CurrentFormationState { get; private set; } = FormationState.None;

    public void SetFormationState(FormationState state)
    {
        CurrentFormationState = state;
    }
}

using System.Collections.Generic;
using UnityEngine;

public class CompanySpawner : MonoBehaviour
{
    [SerializeField] private AgentSelectionManager selectionManager;
    [SerializeField] private GameObject companyPrefab;

    private List<CompanyController> activeCompanies = new();

    public static List<CompanyController> AllCompanies { get; } = new();

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            TryFormCompanyFromSelection();
        }
    }

    private void TryFormCompanyFromSelection()
    {
        List<AgentSelection> selected = selectionManager.GetSelectedAgents();
        if (selected.Count < 60)
        {
            PopupMessageUI.Instance.ShowMessage("60 agents aren't selected!");
            Debug.Log("Need 60 agents to form a company.");
            return;
        }

        GameObject obj = Instantiate(companyPrefab);
        CompanyController company = obj.GetComponent<CompanyController>();

        Vector3 avg = Vector3.zero;
        foreach (var agent in selected)
        {
            avg += agent.transform.position;
        }
        avg /= selected.Count;

        obj.transform.position = avg;

        List<AgentSelection> assigned = selected.GetRange(0, 60);
        company.agents = new List<AgentSelection>(assigned);

        foreach (var agent in assigned)
        {
            agent.Deselect();
        }

        selectionManager.RemoveAgents(assigned);

        company.FormFormation(CompanyFormation.Line);

        activeCompanies.Add(company);

        AllCompanies.Add(company.GetComponent<CompanyController>());

        Debug.Log("New company formed.");
    }
}

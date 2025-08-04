using System.Collections.Generic;
using UnityEngine;

public class BattalionSpawner : MonoBehaviour
{
    [SerializeField] private GameObject battalionPrefab;
    public AgentSelectionManager selectionManager;

    public void TryFormBattalion(List<CompanyController> allCompanies)
    {
        var selectedAgents = selectionManager.GetSelectedAgents();
        var selectedCompanies = new List<CompanyController>();

        foreach (var company in allCompanies)
        {
            bool allSelected = company.agents.TrueForAll(a => selectedAgents.Contains(a));
            if (allSelected)
            {
                selectedCompanies.Add(company);
            }
        }

        if (selectedCompanies.Count == 6)
        {
            Vector3 center = Vector3.zero;
            foreach (var c in selectedCompanies)
                center += c.transform.position;

            center /= selectedCompanies.Count;

            var battalionGO = Instantiate(battalionPrefab, center, Quaternion.identity);
            var battalion = battalionGO.GetComponent<BattalionController>();

            Vector3 avgCompanyPos = Vector3.zero;
            foreach (var company in selectedCompanies)
            {
                avgCompanyPos += company.transform.position;
            }
            avgCompanyPos /= selectedCompanies.Count;

            battalionGO.transform.position = avgCompanyPos;

            battalion.Initialize(selectedCompanies);

            BattalionManager.Instance.RegisterBattalion(battalion);


            foreach (var c in selectedCompanies)
            {
                selectionManager.RemoveAgents(c.agents);
            }

            Debug.Log("[BattalionSpawner] Battalion formed.");
        }
        else 
        {
            PopupMessageUI.Instance.ShowMessage("6 companies must be selected!");
        }
    }
}

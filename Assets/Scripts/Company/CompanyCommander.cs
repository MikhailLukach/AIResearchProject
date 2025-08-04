using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;

public class CompanyCommander : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private AgentSelectionManager selectionManager;
    [SerializeField] private List<CompanyController> allCompanies = new();

    [SerializeField] private GameObject marchTargetIndicatorPrefab;

    private float clickTime = 0f;
    private const float doubleClickDelay = 0.3f;

    void Update()
    {
        if (Input.GetMouseButtonDown(1)) // Left-click
        {
            float timeSinceLastClick = Time.time - clickTime;
            clickTime = Time.time;

            if (timeSinceLastClick <= doubleClickDelay)
            {
                IssueMarchCommand();
            }
        }
    }

    private void IssueMarchCommand()
    {
        allCompanies = new List<CompanyController>(FindObjectsOfType<CompanyController>());

        if (!Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out RaycastHit hit)) return;

        var selectedCompanies = selectionManager.GetSelectedCompanies(allCompanies);
        if (selectedCompanies.Count != 1)
        {
            PopupMessageUI.Instance.ShowMessage("No company has been created!");
            return;
        }
        else if(selectedCompanies.Count > 1) 
        {
            PopupMessageUI.Instance.ShowMessage("Only a single company/battalion can be moved!");
            //Debug.Log("[CompanyCommander] Only a single company can be moved this way.");
            return;
        }

            CompanyController selectedCompany = selectedCompanies[0];
        BehaviorGraphAgent selectedCompanyBehavAgent = selectedCompany.GetComponent<BehaviorGraphAgent>();

        if (selectedCompanyBehavAgent.GetVariable<bool>("IsAdvancing", out var isAdvancing) && isAdvancing)
        {
            //Debug.Log("[CompanyCommander] Cannot issue move: Company is already advancing.");
            return;
        }

        if (marchTargetIndicatorPrefab != null)
        {
            GameObject indicator = Instantiate(marchTargetIndicatorPrefab, hit.point, Quaternion.identity);
        }

        selectedCompany.UseCustomRotationDirection(false);

        selectedCompanyBehavAgent.SetVariableValue("TargetPosition", hit.point);
        selectedCompanyBehavAgent.SetVariableValue("IsAdvancing", true);
        selectedCompanyBehavAgent.SetVariableValue("ShouldHold", false);
    }
}

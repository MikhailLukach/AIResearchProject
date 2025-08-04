using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class AgentSelectionManager : MonoBehaviour
{
    public RectTransform SelectionBox; // Assign in inspector

    private Vector2 startMousePos;
    private Vector2 endMousePos;

    [SerializeField]
    private List<AgentSelection> selectedAgents = new();

    [SerializeField] private TextMeshProUGUI selectionCountText;

    private Stack<List<AgentSelection>> selectionHistory = new();

    void Update()
    {
        HandleMouseInput();
        HandleUndo();
    }

    void HandleMouseInput()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            startMousePos = Mouse.current.position.ReadValue();
            SelectionBox.gameObject.SetActive(true);
        }
        else if (Mouse.current.leftButton.isPressed)
        {
            endMousePos = Mouse.current.position.ReadValue();
            UpdateSelectionBox();
        }
        else if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            SelectionBox.gameObject.SetActive(false);
            SelectAgentsInBox();
        }
    }

    void HandleUndo()
    {
        if (Keyboard.current.ctrlKey.isPressed && Keyboard.current.zKey.wasPressedThisFrame)
        {
            if (selectionHistory.Count > 0)
            {
                DeselectAll();
                var last = selectionHistory.Pop();
                foreach (var agent in last)
                {
                    agent.ToggleSelection();
                    selectedAgents.Add(agent);
                }
            }
        }
    }

    void UpdateSelectionBox()
    {
        Vector2 boxStart = startMousePos;
        Vector2 boxEnd = Mouse.current.position.ReadValue();
        Vector2 boxCenter = (boxStart + boxEnd) / 2;
        SelectionBox.position = boxCenter;

        Vector2 size = new Vector2(Mathf.Abs(boxStart.x - boxEnd.x), Mathf.Abs(boxStart.y - boxEnd.y));
        SelectionBox.sizeDelta = size;
    }

    void SelectAgentsInBox()
    {
        // Save history
        selectionHistory.Push(new List<AgentSelection>(selectedAgents));

        DeselectAll();

        Vector2 min = Vector2.Min(startMousePos, endMousePos);
        Vector2 max = Vector2.Max(startMousePos, endMousePos);

        Camera cam = Camera.main;
        foreach (var agent in FindObjectsOfType<AgentSelection>())
        {
            Vector3 screenPos = cam.WorldToScreenPoint(agent.transform.position);
            if (screenPos.z > 0 && screenPos.x >= min.x && screenPos.x <= max.x && screenPos.y >= min.y && screenPos.y <= max.y)
            {
                agent.ToggleSelection();
                selectedAgents.Add(agent);
            }
        }

        UpdateSelectionCounter();
    }

    void DeselectAll()
    {
        foreach (var agent in selectedAgents)
        {
            agent.Deselect();
        }
        selectedAgents.Clear();

        UpdateSelectionCounter();
    }

    public List<AgentSelection> GetSelectedAgents()
    {
        return selectedAgents;
    }

    public void RemoveAgents(List<AgentSelection> agentsToRemove)
    {
        foreach (var agent in agentsToRemove)
        {
            if (selectedAgents.Contains(agent))
            {
                agent.Deselect();
                selectedAgents.Remove(agent);
            }
        }
    }

    public CompanyController TryGetSelectedCompany(List<CompanyController> allCompanies)
    {
        var selected = GetSelectedAgents();

        foreach (var company in allCompanies)
        {
            if (company.agents.Count != selected.Count)
                continue;

            bool allMatch = true;
            foreach (var agent in selected)
            {
                if (!company.agents.Contains(agent))
                {
                    allMatch = false;
                    break;
                }
            }

            if (allMatch)
                return company;
        }

        return null;
    }

    public List<CompanyController> GetSelectedCompanies(List<CompanyController> allCompanies)
    {
        var selected = GetSelectedAgents();
        List<CompanyController> matchedCompanies = new();

        foreach (var company in allCompanies)
        {
            if (company.agents.Count != selected.Count)
                continue;

            if (selected.All(agent => company.agents.Contains(agent)))
            {
                matchedCompanies.Add(company);
            }
        }

        return matchedCompanies;
    }

    public BattalionController TryGetSelectedBattalion(List<BattalionController> allBattalions)
    {
        var selectedAgents = GetSelectedAgents();

        foreach (var battalion in allBattalions)
        {
            bool allMatch = battalion.Companies.SelectMany(c => c.agents).All(agent => selectedAgents.Contains(agent));
            if (allMatch)
                return battalion;
        }

        return null;
    }

    private void UpdateSelectionCounter()
    {
        if (selectionCountText != null)
            selectionCountText.text = $"Selected: {selectedAgents.Count}";
    }
}

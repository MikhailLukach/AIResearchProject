using System.Collections.Generic;
using UnityEngine;

public class BattalionManager : MonoBehaviour
{
    public static BattalionManager Instance { get; private set; }

    private readonly List<BattalionController> allBattalions = new();

    [SerializeField] private AgentSelectionManager selectionManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    public void RegisterBattalion(BattalionController battalion)
    {
        if (!allBattalions.Contains(battalion))
            allBattalions.Add(battalion);
    }

    public List<BattalionController> GetAllBattalions() => allBattalions;
}

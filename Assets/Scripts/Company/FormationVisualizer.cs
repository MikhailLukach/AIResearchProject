using System.Collections.Generic;
using UnityEngine;

public class FormationVisualizer : MonoBehaviour
{
    public GameObject slotIndicatorPrefab;
    public float indicatorLifetime = 5f;

    private readonly List<GameObject> activeIndicators = new();

    public void ShowFormation(List<Vector3> slotPositions)
    {
        ClearExistingIndicators();

        foreach (var pos in slotPositions)
        {
            GameObject indicator = Instantiate(slotIndicatorPrefab, pos, Quaternion.identity);
            activeIndicators.Add(indicator);

            Destroy(indicator, indicatorLifetime);
        }
    }

    private void ClearExistingIndicators()
    {
        foreach (var obj in activeIndicators)
        {
            if (obj != null) Destroy(obj);
        }

        activeIndicators.Clear();
    }
}

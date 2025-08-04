using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BattalionFormationTrigger : MonoBehaviour
{
    [SerializeField] private BattalionSpawner battalionSpawner;

    void Update()
    {
        if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            battalionSpawner.TryFormBattalion(CompanySpawner.AllCompanies);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Behavior;
using UnityEngine;

public class BattalionCommander : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private AgentSelectionManager selectionManager;
    [SerializeField] private BattalionManager battalionManager;
    [SerializeField] private GameObject targetIndicatorPrefab;

    [SerializeField] private float spacingBetweenCompanies = 5f;

    private Dictionary<CompanyController, Vector3> _companySlotOffsets = new();

    private const float ColumnLateralSpacing = 50f;
    private const float ColumnDepthSpacing = 40f; //og 30

    private float lastClickTime = 0f;
    private const float doubleClickThreshold = 0.3f;

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            var allBattalions = battalionManager.GetAllBattalions();
            if (allBattalions.Count == 0)
            {
                return;
            }

            var firstBattalion = allBattalions[0];

            if (AreAnyCompaniesAdvancing(firstBattalion.Companies))
            {
                Debug.LogWarning("[BattalionCommander] Cannot move battalion: companies still advancing.");
                return;
            }

            if (firstBattalion.CurrentFormationState != FormationState.Square || firstBattalion.CurrentFormationState != FormationState.None)
            {
                float timeSinceLastClick = Time.time - lastClickTime;
                lastClickTime = Time.time;

                if (timeSinceLastClick <= doubleClickThreshold)
                {
                    HandleDoubleClickMovement();
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            var allBattalions = battalionManager.GetAllBattalions();
            if (allBattalions.Count == 0)
            {
                PopupMessageUI.Instance.ShowMessage("Battalion not formed/selected!");
                return;
            }

            var firstBattalion = allBattalions[0];
            Vector3 formationRight = Vector3.right;
            IssueLineFormation(firstBattalion, formationRight, 20f);
            firstBattalion.SetFormationState(FormationState.Line);
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            var allBattalions = battalionManager.GetAllBattalions();
            if (allBattalions.Count == 0)
            {
                PopupMessageUI.Instance.ShowMessage("Battalion not formed/selected!");
                return;
            }

            var firstBattalion = allBattalions[0];
            Vector3 formationRight = Vector3.right;

            StartCoroutine(IssueStagedColumnFormation(firstBattalion, formationRight, 50f, 40f, 15f));
            firstBattalion.SetFormationState(FormationState.Column);
        }

        if (Input.GetKeyDown(KeyCode.K)) 
        {
            var allBattalions = battalionManager.GetAllBattalions();
            if (allBattalions.Count == 0)
            {
                PopupMessageUI.Instance.ShowMessage("Battalion not formed/selected!");
                return;
            }

            var firstBattalion = allBattalions[0];
            Vector3 formationRight = Vector3.right;
            bool offsetCenterBack = firstBattalion.CurrentFormationState == FormationState.Line;
            IssueSquareFormation(firstBattalion, formationRight, 30f, 10f);
            firstBattalion.SetFormationState(FormationState.Square);
        }
    }

    private void IssueSquareFormation(BattalionController battalion, Vector3 formationRight, float sideOffset, float frontBackOffset)
    {
        if (battalion.Companies.Count < 6) return;

        switch (battalion.CurrentFormationState)
        {
            case FormationState.Line:
                IssueLineToSquareFormation(battalion, formationRight, sideOffset, frontBackOffset);
                break;
            case FormationState.Column:
                IssueColumnToSquareFormation(battalion, formationRight, sideOffset, frontBackOffset);
                break;
            default:
                IssueStandardSquareFormation(battalion, formationRight, sideOffset, frontBackOffset, false);
                break;
        }
    }

    private void IssueLineToSquareFormation(BattalionController battalion, Vector3 formationRight, float sideOffset, float frontBackOffset)
    {
        Vector3 lateral = formationRight.normalized;

        List<CompanyController> sorted = new List<CompanyController>(battalion.Companies);
        sorted.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

        if (sorted.Count < 6)
        {
            Debug.LogWarning("[BattalionCommander] Need at least 6 companies to form square from line.");
            return;
        }

        var topCompanies = new List<CompanyController> { sorted[0], sorted[5] }; // outer
        var bottomCompanies = new List<CompanyController> { sorted[1], sorted[4] }; // next inner
        var sideCompanies = new List<CompanyController> { sorted[2], sorted[3] }; // middle

        Vector3 center = battalion.GetAveragePosition();
        Vector3 forward = Vector3.Cross(Vector3.up, lateral).normalized;
        center -= forward * 60f; 

        Vector3 leftTarget = center - lateral * sideOffset;
        Vector3 rightTarget = center + lateral * sideOffset;
        Vector3 leftIntermediate = leftTarget - lateral * 30f;
        Vector3 rightIntermediate = rightTarget + lateral * 30f;

        sideCompanies[0].SetFormationRight(lateral);
        sideCompanies[1].SetFormationRight(lateral);
        IssueControlledMarchToCompany(sideCompanies[0], leftIntermediate, lateral);
        IssueControlledMarchToCompany(sideCompanies[1], rightIntermediate, lateral);

        StartCoroutine(WaitAndReissueFinalVerticalSquareTargets(
            sideCompanies[0], sideCompanies[1],
            leftTarget, rightTarget,
            sideOffset,
            topCompanies, bottomCompanies
        ));
    }

    private void IssueColumnToSquareFormation(BattalionController battalion, Vector3 formationRight, float sideOffset, float frontBackOffset)
    {
        var all = new List<CompanyController>(battalion.Companies);
        all.Sort((a, b) => a.transform.position.z.CompareTo(b.transform.position.z)); 

        int midIndex = all.Count / 2;

        //CompanyController rightSideCompany = all[midIndex - 1]; // lower one
        //CompanyController leftSideCompany = all[midIndex];      // upper one

        var middleCompanies = new List<CompanyController> { all[midIndex - 1], all[midIndex] };

        middleCompanies.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

        CompanyController leftSideCompany = middleCompanies[0];  
        CompanyController rightSideCompany = middleCompanies[1]; 

        var rawBottomCompanies = new List<CompanyController> { all[0], all[1] };

        rawBottomCompanies.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
        List<CompanyController> bottomCompanies = new()
        {
            rawBottomCompanies[0],
            rawBottomCompanies[1]  
        };

        List<CompanyController> topCompanies = new() { all[^2], all[^1] };

        Vector3 center = battalion.GetAveragePosition();
        Vector3 lateral = formationRight.normalized;
        Vector3 forward = Vector3.Cross(Vector3.up, lateral).normalized;

        Vector3 leftFinal = center - lateral * sideOffset;
        Vector3 rightFinal = center + lateral * sideOffset;

        Vector3 leftIntermediate = leftFinal - lateral * 30f;
        Vector3 rightIntermediate = rightFinal + lateral * 30f;

        leftSideCompany.SetFormationRight(lateral);
        rightSideCompany.SetFormationRight(lateral);

        IssueControlledMarchToCompany(leftSideCompany, leftIntermediate, lateral);
        IssueControlledMarchToCompany(rightSideCompany, rightIntermediate, lateral);

        StartCoroutine(WaitAndReissueFinalVerticalSquareTargets(
            leftSideCompany,
            rightSideCompany,
            leftFinal,
            rightFinal,
            sideOffset,
            topCompanies,
            bottomCompanies,
            true
        ));
    }

    private void IssueControlledMarchToCompany(CompanyController company, Vector3 position, Vector3 formationRight)
    {
        if (company == null) return;

        var graph = company.GetGraphAgent();
        if (graph == null)
        {
            Debug.LogWarning("[BattalionCommander] Company has no BehaviorGraphAgent.");
            return;
        }

        company.SetBattalionMode(true);
        company.SetTargetCameraRight(formationRight);
        company.UseCustomRotationDirection(true);

        graph.SetVariableValue("TargetPosition", position);
        graph.SetVariableValue("IsAdvancing", true);
        graph.SetVariableValue("ShouldHold", false);

        if (targetIndicatorPrefab != null)
        {
            Instantiate(targetIndicatorPrefab, position, Quaternion.identity);
        }

        //Debug.Log($"[BattalionCommander] Issued march to {company.name} -> Pos: {position}, Dir: {formationRight}");
    }

    private void IssueLineFormation(BattalionController battalion, Vector3 formationRight, float spacingBetweenCompanies)
    {
        var companies = battalion.Companies;
        if (companies.Count == 0) return;

        Vector3 center = Vector3.zero;
        foreach (var c in companies) center += c.transform.position;
        center /= companies.Count;

        Vector3 lateral = formationRight.normalized;
        int count = companies.Count;
        float estimatedCompanyWidth = EstimateCompanyWidth(companies[0]);

        List<Vector3> targetPositions = new();
        float totalWidth = (count - 1) * (estimatedCompanyWidth + spacingBetweenCompanies);
        for (int i = 0; i < count; i++)
        {
            float offsetFromCenter = i * (estimatedCompanyWidth + spacingBetweenCompanies) - totalWidth / 2f;
            Vector3 offset = lateral * offsetFromCenter;
            targetPositions.Add(center + offset);
        }

        var pairs = new List<(CompanyController, Vector3)>();
        var availableTargets = new List<Vector3>(targetPositions);
        var remainingCompanies = new List<CompanyController>(companies);

        while (availableTargets.Count > 0 && remainingCompanies.Count > 0)
        {
            float bestDist = float.MaxValue;
            int bestCompanyIndex = -1;
            int bestTargetIndex = -1;

            for (int i = 0; i < remainingCompanies.Count; i++)
            {
                for (int j = 0; j < availableTargets.Count; j++)
                {
                    float dist = Vector3.SqrMagnitude(remainingCompanies[i].transform.position - availableTargets[j]);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestCompanyIndex = i;
                        bestTargetIndex = j;
                    }
                }
            }

            var chosenCompany = remainingCompanies[bestCompanyIndex];
            var chosenTarget = availableTargets[bestTargetIndex];

            pairs.Add((chosenCompany, chosenTarget));
            remainingCompanies.RemoveAt(bestCompanyIndex);
            availableTargets.RemoveAt(bestTargetIndex);
        }

        foreach (var (company, target) in pairs)
        {
            if (!IsOverlappingOtherCompanies(target, estimatedCompanyWidth, companies, company, formationRight))
            {
                IssueControlledMarchToCompany(company, target, formationRight);
            }
            else
            {
                Debug.LogWarning($"[BattalionCommander] Skipped {company.name} due to overlap risk at {target}");
            }
        }

        //Debug.Log($"[BattalionCommander] Issued distance-optimized LINE formation for {count} companies facing {formationRight}");
    }

    private IEnumerator IssueStagedColumnFormation(BattalionController battalion, Vector3 formationRight, float lateralSpacing, float depthSpacing,
        float delayBetweenPairs = 1f)
    {
        if (battalion.Companies.Count == 0) yield break;

        Vector3 center = Vector3.zero;
        foreach (var company in battalion.Companies)
            center += company.transform.position;
        center /= battalion.Companies.Count;

        Vector3 lateral = formationRight.normalized;
        Vector3 forward = Vector3.Cross(Vector3.up, lateral).normalized;

        int cols = 2;
        int rows = Mathf.CeilToInt(battalion.Companies.Count / (float)cols);

        List<Vector3> slotTargets = new();
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                float xOffset = (col - (cols - 1) / 2f) * lateralSpacing;
                float zOffset = -row * depthSpacing;
                Vector3 offset = lateral * xOffset + forward * zOffset;
                Vector3 pos = center + offset;
                slotTargets.Add(pos);
            }
        }

        slotTargets.Sort((a, b) => b.z.CompareTo(a.z));

        var remainingCompanies = new List<CompanyController>(battalion.Companies);
        List<(CompanyController, Vector3)> assignments = new();

        foreach (var target in slotTargets)
        {
            CompanyController closest = null;
            float closestDist = float.MaxValue;

            foreach (var company in remainingCompanies)
            {
                float dist = Vector3.SqrMagnitude(company.transform.position - target);
                if (dist < closestDist)
                {
                    closest = company;
                    closestDist = dist;
                }
            }

            if (closest != null)
            {
                assignments.Add((closest, target));
                remainingCompanies.Remove(closest);
            }
        }

        for (int i = 0; i < assignments.Count; i += 2)
        {
            List<CompanyController> currentPair = new();

            if (i < assignments.Count)
            {
                var (c1, pos1) = assignments[i];
                IssueControlledMarchToCompany(c1, pos1, formationRight);
                currentPair.Add(c1);
            }

            if (i + 1 < assignments.Count)
            {
                var (c2, pos2) = assignments[i + 1];
                IssueControlledMarchToCompany(c2, pos2, formationRight);
                currentPair.Add(c2);
            }

            yield return StartCoroutine(WaitForCompaniesToStopAdvancing(currentPair));
        }

        //Debug.Log("[BattalionCommander] Completed staged column formation.");
    }

    private void IssueStandardSquareFormation(BattalionController battalion, Vector3 formationRight, float sideOffset, float frontBackOffset,
        bool offsetCenterBack = true)
    {
        if (battalion.Companies.Count < 2) return;

        Vector3 center = Vector3.zero;
        foreach (var company in battalion.Companies)
            center += company.transform.position;
        center /= battalion.Companies.Count;

        Vector3 lateral = formationRight.normalized;
        Vector3 forward = Vector3.Cross(Vector3.up, lateral).normalized;

        if (offsetCenterBack)
        {
            float backOffsetAmount = 60f; //tune this as needed
            center -= forward * backOffsetAmount;
        }

        Vector3 leftFinalTarget = center - lateral * sideOffset;
        Vector3 rightFinalTarget = center + lateral * sideOffset;

        Vector3 leftIntermediate = leftFinalTarget - lateral * 30f;
        Vector3 rightIntermediate = rightFinalTarget + lateral * 30f;

        var allCompanies = new List<CompanyController>(battalion.Companies);

        CompanyController leftCompany = SelectTopLeftCompany(allCompanies);
        allCompanies.Remove(leftCompany);

        CompanyController rightCompany = SelectBottomRightCompany(allCompanies);
        allCompanies.Remove(rightCompany);

        // First move to intermediate (horizontal alignment)
        leftCompany.SetFormationRight(lateral);
        rightCompany.SetFormationRight(lateral);

        IssueControlledMarchToCompany(leftCompany, leftIntermediate, lateral);
        IssueControlledMarchToCompany(rightCompany, rightIntermediate, lateral);

        // Coroutine to wait for them to arrive, then assign final vertical move
        StartCoroutine(WaitAndReissueFinalVerticalSquareTargets(leftCompany, rightCompany, leftFinalTarget, rightFinalTarget, sideOffset));
    }

    private IEnumerator WaitAndReissueFinalVerticalSquareTargets(
    CompanyController leftCompany,
    CompanyController rightCompany,
    Vector3 leftFinalTarget,
    Vector3 rightFinalTarget,
    float sideOffset,
    List<CompanyController> topCompanies = null,
    List<CompanyController> bottomCompanies = null,
    bool FromSquare = false)
    {
        yield return StartCoroutine(WaitForCompaniesToStopAdvancing(new List<CompanyController> { leftCompany, rightCompany }));

        Vector3 verticalRight = Vector3.forward;

        float extraSideSpacing = 15f; 
        Vector3 lateral = Vector3.right; 

        Vector3 adjustedLeftFinal = leftFinalTarget - lateral * extraSideSpacing;
        Vector3 adjustedRightFinal = rightFinalTarget + lateral * extraSideSpacing;

        leftCompany.SetFormationRight(verticalRight);
        rightCompany.SetFormationRight(verticalRight);

        IssueControlledMarchToCompany(leftCompany, adjustedLeftFinal, verticalRight);
        IssueControlledMarchToCompany(rightCompany, adjustedRightFinal, verticalRight);

        yield return StartCoroutine(WaitForCompaniesToStopAdvancing(new List<CompanyController> { leftCompany, rightCompany }));

        if (topCompanies != null && bottomCompanies != null)
        {
            if (FromSquare ==  false)
            {
                StartCoroutine(AssignTopAndBottomCompaniesExplicit(topCompanies, bottomCompanies, leftFinalTarget, rightFinalTarget, sideOffset));
            }
            else 
            {
                StartCoroutine(AssignTopAndBottomCompaniesFromColumnToSquare(topCompanies, bottomCompanies, leftFinalTarget, rightFinalTarget,
                    sideOffset));
            }
        }
        else
        {
            StartCoroutine(AssignTopAndBottomCompaniesAfterSidesMove(leftCompany, rightCompany, leftFinalTarget, rightFinalTarget, sideOffset));
        }

        //Debug.Log("[BattalionCommander] Issued final vertical positions to side companies after delay.");
    }

    private IEnumerator AssignTopAndBottomCompaniesAfterSidesMove(
    CompanyController leftCompany,
    CompanyController rightCompany,
    Vector3 leftFinalTarget,
    Vector3 rightFinalTarget,
    float sideOffset)
    {
        // Choose top and bottom companies based on Z (forward)
        var allBattalions = battalionManager.GetAllBattalions();
        if (allBattalions.Count == 0) yield break;

        var companies = new List<CompanyController>();
        foreach (var b in allBattalions) companies.AddRange(b.Companies);

        companies.Remove(leftCompany);
        companies.Remove(rightCompany);

        if (companies.Count < 4) yield break;

        companies.Sort((a, b) => b.transform.position.z.CompareTo(a.transform.position.z));
        var topCompanies = new List<CompanyController> { companies[0], companies[1] };

        companies.Sort((a, b) => a.transform.position.z.CompareTo(b.transform.position.z));
        var bottomCompanies = new List<CompanyController> { companies[0], companies[1] };

        Vector3 center = (leftFinalTarget + rightFinalTarget) / 2f;
        Vector3 verticalRight = Vector3.forward;
        Vector3 lateral = Vector3.right;

        float zSpacing = 27f; 
        float xSpacing = 25f; 

        Vector3 topLeft = center + verticalRight * zSpacing + -lateral * xSpacing;
        Vector3 topRight = center + verticalRight * zSpacing + lateral * xSpacing;

        Vector3 bottomLeft = center - verticalRight * zSpacing + -lateral * xSpacing;
        Vector3 bottomRight = center - verticalRight * zSpacing + lateral * xSpacing;

        topCompanies[0].SetFormationRight(lateral);
        topCompanies[1].SetFormationRight(lateral);
        IssueControlledMarchToCompany(topCompanies[0], topLeft, lateral);
        IssueControlledMarchToCompany(topCompanies[1], topRight, lateral);

        bottomCompanies[0].SetFormationRight(lateral);
        bottomCompanies[1].SetFormationRight(lateral);
        IssueControlledMarchToCompany(bottomCompanies[0], bottomLeft, lateral);
        IssueControlledMarchToCompany(bottomCompanies[1], bottomRight, lateral);

        //Debug.Log("[BattalionCommander] Assigned top and bottom companies to square formation.");
    }

    private IEnumerator AssignTopAndBottomCompaniesExplicit(
    List<CompanyController> topCompanies,
    List<CompanyController> bottomCompanies,
    Vector3 leftFinalTarget,
    Vector3 rightFinalTarget,
    float sideOffset)
    {
        Vector3 center = (leftFinalTarget + rightFinalTarget) / 2f;
        Vector3 lateral = Vector3.right; 
        Vector3 vertical = Vector3.forward;

        float zSpacing = 27f;
        float xSpacing = 25f;
        float intermediateOffset = 50f;

        Vector3 topLeftFinal = center + vertical * zSpacing - lateral * xSpacing;
        Vector3 topRightFinal = center + vertical * zSpacing + lateral * xSpacing;
        Vector3 bottomLeft = center - vertical * zSpacing - lateral * xSpacing;
        Vector3 bottomRight = center - vertical * zSpacing + lateral * xSpacing;

        Vector3 topLeftIntermediate = topLeftFinal - lateral * intermediateOffset;
        Vector3 topRightIntermediate = topRightFinal + lateral * intermediateOffset;

        bottomCompanies[0].SetFormationRight(lateral);
        bottomCompanies[1].SetFormationRight(lateral);
        IssueControlledMarchToCompany(bottomCompanies[0], bottomLeft, lateral);
        IssueControlledMarchToCompany(bottomCompanies[1], bottomRight, lateral);

        topCompanies[0].SetFormationRight(lateral);
        topCompanies[1].SetFormationRight(lateral);
        IssueControlledMarchToCompany(topCompanies[0], topLeftIntermediate, lateral);
        IssueControlledMarchToCompany(topCompanies[1], topRightIntermediate, lateral);

        yield return StartCoroutine(WaitForCompaniesToStopAdvancing(topCompanies));

        IssueControlledMarchToCompany(topCompanies[0], topLeftFinal, lateral);
        IssueControlledMarchToCompany(topCompanies[1], topRightFinal, lateral);

        yield return null;
    }

    private IEnumerator AssignTopAndBottomCompaniesFromColumnToSquare(
    List<CompanyController> topCompanies,
    List<CompanyController> bottomCompanies,
    Vector3 leftFinalTarget,
    Vector3 rightFinalTarget,
    float sideOffset)
    {
        Vector3 center = (leftFinalTarget + rightFinalTarget) / 2f;
        Vector3 lateral = Vector3.right;
        Vector3 vertical = Vector3.forward;

        float zSpacing = 27f;
        float xSpacing = 25f;

        Vector3 topLeft = center + vertical * zSpacing - lateral * xSpacing;
        Vector3 topRight = center + vertical * zSpacing + lateral * xSpacing;
        Vector3 bottomLeft = center - vertical * zSpacing - lateral * xSpacing;
        Vector3 bottomRight = center - vertical * zSpacing + lateral * xSpacing;

        CompanyController topLeftCompany = (Vector3.SqrMagnitude(topCompanies[0].transform.position - topLeft) <
                                            Vector3.SqrMagnitude(topCompanies[1].transform.position - topLeft))
                                            ? topCompanies[0] : topCompanies[1];

        CompanyController topRightCompany = (topLeftCompany == topCompanies[0]) ? topCompanies[1] : topCompanies[0];

        bottomCompanies[0].SetFormationRight(lateral);
        bottomCompanies[1].SetFormationRight(lateral);
        IssueControlledMarchToCompany(bottomCompanies[0], bottomLeft, lateral);
        IssueControlledMarchToCompany(bottomCompanies[1], bottomRight, lateral);

        topLeftCompany.SetFormationRight(lateral);
        topRightCompany.SetFormationRight(lateral);
        IssueControlledMarchToCompany(topLeftCompany, topLeft, lateral);
        IssueControlledMarchToCompany(topRightCompany, topRight, lateral);

        yield return null;
    }

    // -- Movement
    private void HandleDoubleClickMovement()
    {
        if (!Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out RaycastHit hit)) return;

        var allBattalions = battalionManager.GetAllBattalions();
        var selectedBattalion = selectionManager.TryGetSelectedBattalion(allBattalions);

        if (selectedBattalion == null) 
        {
            PopupMessageUI.Instance.ShowMessage("No battalion to move!");
            return;
        }

        Vector3 cameraForward = cam.transform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();
        Vector3 formationRight = Vector3.Cross(Vector3.up, cameraForward).normalized;

        //Debug.Log("[BattalionCommander] Called Move");
        if (selectedBattalion.CurrentFormationState == FormationState.Line)
        {
            RealignBattalionInPlace(selectedBattalion, formationRight);
            MoveBattalionAfterAlignment(selectedBattalion, hit.point, formationRight);
        }
        else if (selectedBattalion.CurrentFormationState == FormationState.Column) 
        {
            RealignColumnBattalionInPlace(selectedBattalion, formationRight);
            MoveColumnBattalionAfterAlignment(selectedBattalion, hit.point, formationRight);
        }
    }

    private void RealignBattalionInPlace(BattalionController battalion, Vector3 formationRight)
    {
        var companies = new List<CompanyController>(battalion.Companies);
        if (companies.Count == 0) return;

        Vector3 center = battalion.GetAveragePosition();
        Vector3 lateral = formationRight.normalized;
        Vector3 forward = Vector3.Cross(Vector3.up, formationRight).normalized;

        center = battalion.GetAveragePosition();
        lateral = formationRight.normalized;
        forward = Vector3.Cross(Vector3.up, formationRight).normalized;

        float estimatedCompanyWidth = EstimateCompanyWidth(companies[0]);
        float spacing = 20f;
        float totalWidth = (companies.Count - 1) * (estimatedCompanyWidth + spacing);

        List<(Vector3 worldPos, Vector3 localOffset)> slots = new();
        for (int i = 0; i < companies.Count; i++)
        {
            float offsetFromCenter = i * (estimatedCompanyWidth + spacing) - totalWidth / 2f;
            Vector3 offset = lateral * offsetFromCenter;
            slots.Add((center + offset, offset));
        }

        _companySlotOffsets.Clear();
        foreach (var company in companies.ToList())
        {
            int bestIndex = -1;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < slots.Count; i++)
            {
                float dist = Vector3.Distance(company.transform.position, slots[i].worldPos);
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    bestIndex = i;
                }
            }

            if (bestIndex >= 0)
            {
                var (targetPos, localOffset) = slots[bestIndex];

                company.SetFormationRight(lateral);
                IssueControlledMarchToCompany(company, targetPos, lateral);
                _companySlotOffsets[company] = localOffset;

                slots.RemoveAt(bestIndex);
                companies.Remove(company);
            }
        }
    }

    private void RealignColumnBattalionInPlace(BattalionController battalion, Vector3 formationRight) 
    {
        var companies = battalion.Companies;
        if (companies.Count == 0) return;

        Vector3 center = battalion.GetAveragePosition();
        Vector3 lateral = formationRight.normalized;
        Vector3 forward = Vector3.Cross(Vector3.up, lateral).normalized;

        Debug.Log("[BattleCommander] called alignment for column");

        int cols = 2;
        int rows = 3;

        List<CompanyController> sorted = companies.OrderByDescending(c => c.transform.position.z).ToList();

        int middleRowIndex = 2;
        int startIndex = middleRowIndex * cols;

        if (startIndex >= sorted.Count - 1)
        {
            Debug.LogWarning("Not enough companies to determine middle row.");
            return;
        }

        //Vector3 posA = sorted[startIndex].transform.position;
        //Vector3 posB = sorted[startIndex + 1].transform.position;

        //Vector3 formationCenter = (posA + posB) / 2f;

        Vector3 formationCenter;

        float dotForwardZ = Mathf.Abs(Vector3.Dot(forward, Vector3.forward));
        float dotForwardX = Mathf.Abs(Vector3.Dot(forward, Vector3.right));

        if (dotForwardZ > dotForwardX)
        {
            Vector3 posA = sorted[startIndex].transform.position;
            Vector3 posB = sorted[startIndex + 1].transform.position;
            formationCenter = (posA + posB) / 2f;
        }
        else
        {
            formationCenter = battalion.GetAveragePosition();
        }

        float xDot = Mathf.Abs(Vector3.Dot(forward, Vector3.right));
        if (xDot > 0.9f)
        {
            float forwardShift = -0.5f * ColumnDepthSpacing;
            formationCenter += forward * forwardShift;
        }

        List<Vector3> slotPositions = new();
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                float xOffset = (col - (cols - 1) / 2f) * ColumnLateralSpacing;
                float zOffset = -row * ColumnDepthSpacing;
                Vector3 offset = lateral * xOffset + forward * zOffset;
                slotPositions.Add(formationCenter + offset);
            }
        }

        var remainingCompanies = new List<CompanyController>(companies);
        _companySlotOffsets.Clear();

        foreach (var slot in slotPositions)
        {
            CompanyController closest = null;
            float closestDist = float.MaxValue;

            foreach (var c in remainingCompanies)
            {
                float dist = Vector3.SqrMagnitude(c.transform.position - slot);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = c;
                }
            }

            if (closest != null)
            {
                closest.SetFormationRight(formationRight);
                IssueControlledMarchToCompany(closest, slot, formationRight);

                Vector3 localOffset = slot - formationCenter;
                _companySlotOffsets[closest] = localOffset;

                remainingCompanies.Remove(closest);
            }
        }
    }

    public void MoveBattalionAfterAlignment(BattalionController battalion, Vector3 targetCenter, Vector3 formationRight)
    {
        StartCoroutine(MoveAfterAlignmentRoutine(battalion, targetCenter, formationRight));
    }

    public void MoveColumnBattalionAfterAlignment(BattalionController battalion, Vector3 targetCenter, Vector3 formationRight) 
    {
        StartCoroutine(MoveColumnAfterAlignmentRoutine(battalion, targetCenter, formationRight));
    }

    private IEnumerator MoveAfterAlignmentRoutine(BattalionController battalion, Vector3 targetCenter, Vector3 formationRight)
    {
        Debug.Log("[MoveAfterAlignmentRoutine] Started");
        yield return StartCoroutine(WaitForCompaniesToStopAdvancing(battalion.Companies));

        Debug.Log($"[MoveAfterAlignmentRoutine] Slot offset count: {_companySlotOffsets.Count}");

        foreach (var company in battalion.Companies)
        {
            if (!_companySlotOffsets.TryGetValue(company, out var localOffset))
            {
                Debug.LogWarning($"[BattalionCommander] No slot offset found for company {company.name}, skipping.");
                continue;
            }

            Vector3 finalTarget = targetCenter + localOffset;
            Debug.Log($"[MoveAfterAlignmentRoutine] Moving {company.name} to {finalTarget}");

            company.SetFormationRight(formationRight);
            company.UseCustomRotationDirection(true);
            IssueControlledMarchToCompany(company, finalTarget, formationRight);
        }

        Debug.Log("[BattalionCommander] Formation moved to final target after alignment.");
    }

    private IEnumerator MoveColumnAfterAlignmentRoutine(BattalionController battalion, Vector3 targetCenter, Vector3 formationRight) 
    {
        yield return StartCoroutine(WaitForCompaniesToStopAdvancing(battalion.Companies));

        Vector3 forward = Vector3.Cross(Vector3.up, formationRight).normalized;

        if (battalion.CurrentFormationState == FormationState.Column)
        {
            foreach (var company in battalion.Companies)
            {
                if (!_companySlotOffsets.TryGetValue(company, out var localOffset))
                {
                    Debug.LogWarning($"[BattalionCommander] No offset found for company {company.name}, skipping.");
                    continue;
                }

                Vector3 finalPosition = targetCenter + localOffset;

                company.SetFormationRight(formationRight);
                company.UseCustomRotationDirection(true);
                IssueControlledMarchToCompany(company, finalPosition, formationRight);
            }

            Debug.Log("[BattalionCommander] Column moved using preserved slot offsets.");
        }
    }
    // -- Movement

    private float EstimateCompanyWidth(CompanyController company)
    {
        if (company == null || company.agents.Count == 0) return 0f;

        int maxPerRow = 20;
        float spacing = 2f;

        int widthAgents = Mathf.Min(company.agents.Count, maxPerRow);
        return (widthAgents - 1) * spacing;
    }

    private bool IsOverlappingOtherCompanies(Vector3 proposedCenter, float width, List<CompanyController> all, CompanyController self, Vector3 right)
    {
        Bounds proposedBounds = new Bounds(proposedCenter, new Vector3(width, 5f, 5f));

        Vector3 min = proposedBounds.min;
        Vector3 max = proposedBounds.max;

        Vector3 p1 = new Vector3(min.x, 0, min.z);
        Vector3 p2 = new Vector3(max.x, 0, min.z);
        Vector3 p3 = new Vector3(max.x, 0, max.z);
        Vector3 p4 = new Vector3(min.x, 0, max.z);

        Debug.DrawLine(p1, p2, Color.red, 2f);
        Debug.DrawLine(p2, p3, Color.red, 2f);
        Debug.DrawLine(p3, p4, Color.red, 2f);
        Debug.DrawLine(p4, p1, Color.red, 2f);


        foreach (var other in all)
        {
            if (other == self) continue;

            Vector3 otherCenter = other.transform.position;
            float otherWidth = EstimateCompanyWidth(other);
            Bounds otherBounds = new Bounds(otherCenter, new Vector3(otherWidth, 5f, 5f));

            if (proposedBounds.Intersects(otherBounds))
            {
                return true;
            }
        }

        return false;
    }

    private CompanyController SelectTopLeftCompany(List<CompanyController> candidates)
    {
        CompanyController best = null;
        float bestScore = float.PositiveInfinity;

        foreach (var company in candidates)
        {
            Vector3 pos = company.transform.position;
            float score = pos.z * 1000f + pos.x; // prioritize top (low Z), then left (low X)

            if (score < bestScore)
            {
                bestScore = score;
                best = company;
            }
        }

        return best;
    }

    private CompanyController SelectBottomRightCompany(List<CompanyController> candidates)
    {
        CompanyController best = null;
        float bestScore = float.NegativeInfinity;

        foreach (var company in candidates)
        {
            Vector3 pos = company.transform.position;
            float score = pos.z * 1000f + pos.x; // prioritize bottom (high Z), then right (high X)

            if (score > bestScore)
            {
                bestScore = score;
                best = company;
            }
        }

        return best;
    }

    private IEnumerator WaitForCompaniesToStopAdvancing(List<CompanyController> companies)
    {
        bool allStopped = false;

        while (!allStopped)
        {
            allStopped = true;

            foreach (var company in companies)
            {
                var graphAgent = company.GetGraphAgent();
                if (graphAgent != null && graphAgent.GetVariable<bool>("IsAdvancing", out var isAdvancing) && isAdvancing)
                {
                    allStopped = false;
                    break;
                }
            }

            if (!allStopped)
                yield return new WaitForSeconds(0.5f);
        }

    }

    public bool AreAnyCompaniesAdvancing(List<CompanyController> companies)
    {
        foreach (var company in companies)
        {
            var graphAgent = company.GetGraphAgent();
            if (graphAgent != null && graphAgent.GetVariable<bool>("IsAdvancing", out var isAdvancing) && isAdvancing)
            {
                return true;
            }
        }
        return false;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationRunner : MonoBehaviour
{
    [Tooltip("The simulation time to wait")]
    public float SimulationTime = 0.1f;
    [Tooltip("The maximum movement allowed before detecting a failure")]
    public float MaxMovementDelta = 0.01f;
    public delegate void SimulationFinished();

    List<GameObject> bottomLines;
    List<GameObject> simulatedLayerLines;
    Dictionary<GameObject, Vector3> lineStartPositions;
    SimulationFinished simulationFinished;
    bool simulationActive;
    float timeWaited;
    bool paused;

    private void Awake()
    {
        paused = false;
        simulationActive = false;
        timeWaited = 0f;
    }

    public void BeginSimulation(List<GameObject> bottomLines, List<GameObject> simulatedLayerLines, SimulationFinished simulationFinished)
    {
        this.bottomLines = bottomLines;
        this.simulatedLayerLines = simulatedLayerLines;
        this.simulationFinished = simulationFinished;

        lineStartPositions = new Dictionary<GameObject, Vector3>();
        foreach (var line in simulatedLayerLines)
        {
            lineStartPositions.Add(line, line.transform.position);
        }

        timeWaited = 0f;
        simulationActive = true;
    }

    private void FixedUpdate()
    {
        if (!simulationActive || paused)
        {
            return;
        }

        timeWaited += Time.fixedDeltaTime;

        if (timeWaited >= SimulationTime)
        {
            if (AnalyzeSimulationResult())
            {
                paused = true;
            }
            else
            {
                simulationActive = false;
                StartCoroutine(WaitAndPrint(0.1f));
            }
        }
    }

    bool AnalyzeSimulationResult()
    {
        var errorFound = false;
        foreach (var line in simulatedLayerLines)
        {
            var startPos = lineStartPositions[line];
            var posNow = line.transform.position;
            line.GetComponent<Rigidbody>().isKinematic = true;

            if ((posNow - startPos).magnitude > MaxMovementDelta)
            {
                Debug.DrawLine(startPos, startPos + new Vector3(0, 3, 0), Color.red);
                line.GetComponent<MeshRenderer>().material.SetColor("_Color", Color.red);
                line.name = "ERROR";
                errorFound = true;
                //Debug.LogError("Error in object identified");
            }
        }

        return errorFound;
    }

    private IEnumerator WaitAndPrint(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        simulationFinished();
    }
}

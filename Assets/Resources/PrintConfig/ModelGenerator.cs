using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(LayerGenerator))]
public class ModelGenerator : MonoBehaviour
{
    [Tooltip("Amount of layers to test physics on in parallel. There will always be a static bottom layer in addition")]
    public int LayersToAnalyze = 1;
    public Text LayerInfoText;

    LayerGenerator layerGenerator;
    bool runningSimulation;
    List<Tuple<int, List<GameObject>>> layers;
    int nextLayerToFetch = 0;

    // Start is called before the first frame update
    void Start()
    {
        layers = new List<Tuple<int, List<GameObject>>>();
        layerGenerator = GetComponent<LayerGenerator>();
        runningSimulation = false;
        layerGenerator.FetchLayer(0, LayerGenerated);
        LayerInfoText.text = "Analyzing layer " + 1;
    }

    void LayerGenerated(int layerID, List<GameObject> layerLines)
    {
        if (layerID < 0)
        {
            // Layer not found, end analysis
            return;
        }
        layers.Add(new Tuple<int, List<GameObject>>(layerID, layerLines));

        if (layers.Count() < 2)
        {
            nextLayerToFetch++;
            layerGenerator.FetchLayer(nextLayerToFetch, LayerGenerated);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!runningSimulation && layers.Count == 2)
        {
            runningSimulation = true;
            InitializeAnalysis();
        }
    }

    void InitializeAnalysis()
    {
        // TODO: Optimize
        // Activate physics for the layers to test
        foreach (var bottomLine in layers.First().Item2)
        {
            var bottomLineMeshRenderer = bottomLine.GetComponent<MeshRenderer>();
            bottomLine.GetComponent<Collider>().enabled = true;
            bottomLineMeshRenderer.enabled = true;
            bottomLine.GetComponent<Rigidbody>().isKinematic = true;
            bottomLineMeshRenderer.material.SetColor("_Color", Color.blue);
            bottomLine.layer = 8;
            bottomLineMeshRenderer.enabled = true;
        }

        var lastLayer = layers.Last().Item2;
        foreach (var line in lastLayer)
        {
            line.layer = 9;
            var lineCollider = line.GetComponent<BoxCollider>();
            var lineLength = lineCollider.size.magnitude;
            var lineIndex = lastLayer.IndexOf(line);

            lineCollider.enabled = true;
            var lineRigidbody = line.GetComponent<Rigidbody>();
            lineRigidbody.isKinematic = false;
            lineRigidbody.useGravity = true;
            lineRigidbody.sleepThreshold = 0;
            lineRigidbody.WakeUp();
            lineRigidbody.AddForce(new Vector3(float.MinValue, 0, 0));
            var lineMeshRenderer = line.GetComponent<MeshRenderer>();
            lineMeshRenderer.enabled = true;
            lineMeshRenderer.material.SetColor("_Color", Color.green);
            lineMeshRenderer.enabled = true;
        }

        GetComponent<SimulationRunner>().BeginSimulation(
            layers.First().Item2,
            layers.Last().Item2,
            AnalysisDone);
    }

    void AnalysisDone()
    {
        layerGenerator.MarkLayerAsNotneeded(layers.First().Item1);
        layers.Remove(layers.First());
        foreach (var line in layers.First().Item2)
        {
            line.GetComponent<ProceduralMesh>().Reset();
        }
        runningSimulation = false;
        nextLayerToFetch++;
        LayerInfoText.text = "Analyzing layer " + nextLayerToFetch;
        layerGenerator.FetchLayer(nextLayerToFetch, LayerGenerated);
    }
}

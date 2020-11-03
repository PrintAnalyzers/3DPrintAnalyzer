using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(LayerGenerator))]
public class ModelGenerator : MonoBehaviour
{
    [Tooltip("Amount of layers to test physics on in parallel. There will always be a static bottom layer in addition")]
    public int LayersToAnalyze = 1;

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
            bottomLine.GetComponent<Collider>().enabled = true;
            bottomLine.GetComponent<MeshRenderer>().enabled = true;
            bottomLine.GetComponent<Rigidbody>().isKinematic = true;
            bottomLine.GetComponent<MeshRenderer>().material.SetColor("_Color", Color.blue);
        }

        var lastLayer = layers.Last().Item2;
        foreach (var line in lastLayer)
        {
            var lineCollider = line.GetComponent<BoxCollider>();
            var lineLength = lineCollider.size.magnitude;
            var lineIndex = lastLayer.IndexOf(line);

            for (int i = lineIndex + 1; i < lastLayer.Count(); i++)
            {
                var otherLine = lastLayer[i];
                var otherCollider = otherLine.GetComponent<BoxCollider>();
                var totalLineLength = lineLength + otherCollider.size.magnitude;
                var distance = (otherLine.transform.position - line.transform.position).magnitude;
                if (distance <= totalLineLength)
                {
                    Physics.IgnoreCollision(lineCollider, otherLine.GetComponent<Collider>());
                }
            }

            lineCollider.enabled = true;
            var lineRigidbody = line.GetComponent<Rigidbody>();
            lineRigidbody.isKinematic = false;
            lineRigidbody.useGravity = true;
            lineRigidbody.sleepThreshold = 0;
            lineRigidbody.WakeUp();
            lineRigidbody.AddForce(new Vector3(float.MinValue, 0, 0));
            line.GetComponent<MeshRenderer>().enabled = true;
            line.GetComponent<MeshRenderer>().material.SetColor("_Color", Color.green);
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
        layerGenerator.FetchLayer(nextLayerToFetch, LayerGenerated);
    }
}

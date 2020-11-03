using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using static GcodeReader;

public class LayerGenerator : MonoBehaviour
{
    [Tooltip("The textfile containing the sliced gcode")]
    public TextAsset gcodeFile;
    [Tooltip("The printhead gameobject")]
    public GameObject printHeadGO;
    [Tooltip("Amount of commands to run between every UI update")]
    public int CommandsPerIteration = 1;
    [Tooltip("Minimum possible layer height used by slicer in mm. Too small is better than too large")]
    public float MinimumLayerHeight = 0.01f;
    [Tooltip("Will change first extrusion commands to movement commands. Used to skip extruder initialization steps.")]
    public int FirstExtrusionsToSkip = 0;

    public delegate void LayerGenerated(int layerID, List<GameObject> lines);

    private PrintHead printHead;
    private PrintMaterial material;
    private GcodeReader reader;
    private Dictionary<int, List<GameObject>> linesPerLayer = new Dictionary<int, List<GameObject>>();
    private int initExtrusionCommandCounter = 0;
    private LayerGenerated layerGeneratedCallback;
    int nextLayerToFetch;
    private List<int> LayersToDelete;
    // Start is called before the first frame update

    public void FetchLayer(int nextLayerToFetch, LayerGenerated layerGenerated)
    {
        this.nextLayerToFetch = nextLayerToFetch;
        if (linesPerLayer.Keys.Count == 0)
        {
            // First layer is added
            linesPerLayer.Add(0, new List<GameObject>());
        }

        // Make sure we have reset the previous layer(s) because we need them for placing the new layer
        foreach (var layer in linesPerLayer.Select(x => x.Value))
        {
            foreach (var line in layer)
            {
                line.GetComponent<ProceduralMesh>().Reset();
            }
        }

        layerGeneratedCallback = layerGenerated;
        GenerateNextLayer(new List<GameObject>());
    }

    public void MarkLayerAsNotneeded(int layerID)
    {
        if (!linesPerLayer.ContainsKey(layerID))
        {
            Debug.LogError("Attempting to remove nonexisting layer " + layerID);
        }
        LayersToDelete.Add(layerID);
    }

    private void Awake()
    {
        LayersToDelete = new List<int>();
        printHead = printHeadGO.GetComponent<PrintHead>();
        material = PrintMaterial.InitializaSingleton(gameObject);
        reader = new GcodeReader(gcodeFile);
    }

    void Start()
    {
    }

    void RemoveUnneededLayers()
    {
        foreach (var layerID in LayersToDelete.ToList())
        {
            if (linesPerLayer.Keys.Max() >= layerID + 2)
            {
                //layerID is no longer needed in any calculations, remove it
                foreach (var line in linesPerLayer[layerID])
                {
                    Destroy(line);
                }
                linesPerLayer.Remove(layerID);
                LayersToDelete.Remove(layerID);
            }
        }
    }


    void GenerateNextLayer(List<GameObject> linesCreated)
    {
        foreach (var line in linesCreated)
        {
            AddLine(line);
        }

        RemoveUnneededLayers();

        if (linesPerLayer.Keys.Max() > nextLayerToFetch)
        {
            layerGeneratedCallback(nextLayerToFetch, linesPerLayer[nextLayerToFetch]);
            return;
        }

        var commands = new List<GCommand>();
        for (int i = 0; i < CommandsPerIteration; i++)
        {
            var command = reader.PeekCommand();

            if (initExtrusionCommandCounter < FirstExtrusionsToSkip && command is Move moveCommand2)
            {
                if (moveCommand2.E > 0)
                {
                    initExtrusionCommandCounter++;
                    moveCommand2.E = 0;
                    moveCommand2.HasE = false;
                }
            }
            if (command == null)
            {
                break;
            }
            reader.PopCommand();
            commands.Add(command);
        }

        // End of gcode file?
        if (commands.Count == 0)
        {
            if (linesPerLayer.ContainsKey(nextLayerToFetch))
            {
                layerGeneratedCallback(nextLayerToFetch, linesPerLayer[nextLayerToFetch]);
            }
            else
            {
                layerGeneratedCallback(-1, new List<GameObject>());
            }
            return;
        }
        StartCoroutine(printHead.NextCommands(commands, GenerateNextLayer));
    }

    bool SameLayer(float heightLine1, float heightLine2)
    {
        return Mathf.Abs(heightLine1 - heightLine2) < MinimumLayerHeight;
    }

    void AddLine(GameObject newLine)
    {
        var layer = linesPerLayer[linesPerLayer.Keys.Max()];
        if (layer.Any() && !SameLayer(layer.First().transform.position.y, newLine.transform.position.y))
        {
            layer = new List<GameObject>();
            linesPerLayer.Add(linesPerLayer.Keys.Max() + 1, layer);
        }

        if (linesPerLayer.Keys.Max() == 0)
        {
            // No fancy line segmentation for the first layer
            layer.Add(newLine);
        }
        else
        {
            layer.AddRange(CreateLineSegments(newLine));
        }
    }

    /// <summary>
    /// Analyzes layer underneath and divides the newline into segments depending on if the line is not always supported from the underlying layer
    /// </summary>
    /// <param name="newLine"></param>
    /// <returns></returns>
    List<GameObject> CreateLineSegments(GameObject newLine)
    {
        var segments = new List<GameObject>();
        newLine.GetComponent<Collider>().enabled = false;
        var collider = newLine.GetComponent<BoxCollider>();
        var start = newLine.GetComponent<ProceduralMesh>().StartPos;
        var stop = newLine.GetComponent<ProceduralMesh>().EndPos;
        var lineSize = (stop - start).magnitude;
        float resolution = 0.01f;
        var direction = (stop - start).normalized * resolution;
        var posNowRelative = new Vector3(0, 0, 0);
        var lastLineEnd = start;
        while (posNowRelative.magnitude < lineSize)
        {
            posNowRelative += direction;
            var posNow = posNowRelative + start;
            if (!Physics.Raycast(posNow, new Vector3(0, -1, 0), printHead.LayerHeight * 1.1f))
            {
                // No support
                var gapStart = posNow - direction;
                    var gapEndRelative = posNowRelative + direction;
                var gapEnd = gapEndRelative + start;
                while (gapEndRelative.magnitude < lineSize &&
                        !Physics.Raycast(gapEnd, new Vector3(0, -1, 0), printHead.LayerHeight * 1.1f))
                {
                    gapEndRelative += direction;
                    gapEnd = gapEndRelative + start;
                }
                if (gapEndRelative.magnitude > lineSize)
                {
                    gapEndRelative = (stop - start);
                    gapEnd = gapEndRelative + start;
                }
                posNowRelative = gapEndRelative;
                if (gapStart != start)
                {
                    // Create the line segment before the gap
                    var lineBeforeGap = Instantiate(printHead.PrintMaterial);
                    lineBeforeGap.GetComponent<ProceduralMesh>().GenerateLine(lastLineEnd - direction, gapStart + direction, printHead.ExtruderWidth, printHead.LayerHeight);
                    segments.Add(lineBeforeGap);
                    lastLineEnd = gapStart;
                }
                // Create the line segment over the gap
                var line = Instantiate(printHead.PrintMaterial);
                line.GetComponent<ProceduralMesh>().GenerateLine(gapStart - direction, gapEnd + direction, printHead.ExtruderWidth, printHead.LayerHeight);
                segments.Add(line);
                lastLineEnd = gapEnd;
            }
            else
            {
                // Support
                int a = 5;
            }
        }

        if (!(Math.Abs(posNowRelative.magnitude - collider.size.magnitude) < 0.0001f))
        {
            // Add the last line
            var line = Instantiate(printHead.PrintMaterial);
            line.GetComponent<ProceduralMesh>().GenerateLine(lastLineEnd - direction, stop, printHead.ExtruderWidth, printHead.LayerHeight);
            segments.Add(line);
        }

        if (!segments.Any())
        {
            newLine.GetComponent<Collider>().enabled = true;
            return new List<GameObject>() { newLine };
        }

        /*
        GameObject previousSegment = null;
        foreach (var segment in segments)
        {
            if (previousSegment != null)
            {
                previousSegment.AddComponent<FixedJoint>();
                var fixedJoint = previousSegment.GetComponent<FixedJoint>();
                fixedJoint.breakForce = 30f;
                fixedJoint.breakTorque = 20f;
                fixedJoint.connectedBody = segment.GetComponent<Rigidbody>();
            }
            previousSegment = segment;
        }
        */

        DestroyImmediate(newLine);
        return segments;
    }
}

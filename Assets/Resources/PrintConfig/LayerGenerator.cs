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

    public delegate void LayerGenerated(List<GameObject> lines);

    private PrintHead printHead;
    private PrintMaterial material;
    private GcodeReader reader;
    private Dictionary<int, List<GameObject>> linesPerLayer = new Dictionary<int, List<GameObject>>();
    private int initExtrusionCommandCounter = 0;
    private LayerGenerated layerGeneratedCallback;
    // Start is called before the first frame update

    public void GenerateNextLayer(LayerGenerated layerGenerated)
    {
        if (linesPerLayer.Keys.Count == 0)
        {
            // First layer is added
            linesPerLayer.Add(0, new List<GameObject>());
        }

        nextLayerStarted = false;
        layerGeneratedCallback = layerGenerated;
        GenerateNextLayer(new List<GameObject>());
    }

    private void Awake()
    {
        printHead = printHeadGO.GetComponent<PrintHead>();
        material = PrintMaterial.InitializaSingleton(gameObject);
        reader = new GcodeReader(gcodeFile);
    }

    void Start()
    {
    }

    bool nextLayerStarted = false;
    void GenerateNextLayer(List<GameObject> linesCreated)
    {
        foreach (var line in linesCreated)
        {
            var layer = linesPerLayer[linesPerLayer.Keys.Last()];
            if (layer.Any() && !SameLayer(layer.First().transform.position.y, line.transform.position.y))
            {
                layer = new List<GameObject>();
                linesPerLayer.Add(linesPerLayer.Keys.Last() + 1, layer);
                nextLayerStarted = true;
            }
            layer.Add(line);
        }

        if (nextLayerStarted)
        {
            layerGeneratedCallback(linesPerLayer[linesPerLayer.Keys.Last() - 1]);
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

        if (commands.Count == 0)
        {
            if (!linesPerLayer[linesPerLayer.Keys.Last()].Any())
            {
                linesPerLayer.Remove(linesPerLayer.Keys.Last());
                layerGeneratedCallback(new List<GameObject>());
            }
            else
            {
                layerGeneratedCallback(linesPerLayer[linesPerLayer.Keys.Last()]);
            }
            return;
        }
        StartCoroutine(printHead.NextCommands(commands, GenerateNextLayer));
    }

    bool SameLayer(float heightLine1, float heightLine2)
    {
        return Mathf.Abs(heightLine1 - heightLine2) < MinimumLayerHeight;
    }
}

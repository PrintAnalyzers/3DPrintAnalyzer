using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(LayerGenerator))]
public class ModelGenerator : MonoBehaviour
{
    LayerGenerator layerGenerator;
    bool layerGenerated;

    // Start is called before the first frame update
    void Start()
    {
        layerGenerator = GetComponent<LayerGenerator>();
        layerGenerated = false;
        layerGenerator.GenerateNextLayer(LayerGenerated);
    }

    int ctr = 0;
    void LayerGenerated(List<GameObject> layerLines)
    {
        if (!layerLines.Any())
        {
            return;
        }
        
        // TODO: Optimize
        // Activate physics for the new layer
        foreach (var line in layerLines)
        {
            var lineCollider = line.GetComponent<BoxCollider>();
            var lineLength = lineCollider.size.magnitude;
            var lineIndex = layerLines.IndexOf(line);

            for (int i = lineIndex+1; i < layerLines.Count(); i++)
            {
                var otherLine = layerLines[i];
                var otherCollider = otherLine.GetComponent<BoxCollider>();
                var totalLineLength = lineLength + otherCollider.size.magnitude;
                var distance = (otherLine.transform.position - line.transform.position).magnitude;
                if (distance <= totalLineLength)
                {
                    ctr++;
                    Physics.IgnoreCollision(lineCollider, otherLine.GetComponent<Collider>());
                }
            }
        }

        foreach (var line in layerLines)
        {
            var lineCollider = line.GetComponent<Collider>();
            lineCollider.enabled = true;
        }

        layerGenerated = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (layerGenerated)
        {
            layerGenerated = false;
            layerGenerator.GenerateNextLayer(LayerGenerated);
        }
    }
}

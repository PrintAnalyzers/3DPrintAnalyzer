using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using static GcodeReader;

public class PrintHead : MonoBehaviour
{
    public enum Movement
    {
        Relative,
        Absolute
    };

    [Tooltip("The print material gameobject. Must be specifically configured to work as expected")]
    public GameObject PrintMaterial;
    [Tooltip("The speed of printhead movement when not immediate")]
    public float MoveStep = 10;
    [Tooltip("True if printhead movement should be immediate - no travel time")]
    public bool ImmediateMove = true;
    [Tooltip("Extruder width (mm) used to set the width of lines. Is usually the same as the width of the printhead nozzle")]
    public float ExtruderWidth = 0.2f;
    [Tooltip("The height (mm) of a layer. So far we only support static layer heights")]
    public float LayerHeight = 0.12f;
    [Tooltip("The minimum length (mm) for an extrusion to happen")]
    public float MinimumExtrusionLength = 0.01f;
    [Tooltip("The starting size of available lines in the linebuffer. We reuse lines from previous layer to avoid the time to create new lines and garbage collect old ones.")]
    public int StartingLineBufferSize = 10000;
    public delegate void DoneDelegate(List<GameObject> linesCreated);

    private List<GameObject> linesCreated = new List<GameObject>();
    private List<GameObject> freeLines = new List<GameObject>();
    private int nextIncrementalBufferSize;

    public Movement MovementType
    {
        get
        {
            return movementType;
        }
    }

    public Vector3 Position
    {
        get
        {
            return transform.position;
        }
    }

    public bool IsExtruding
    {
        get
        {
            return isExtruding;
        }
    }

    private Movement movementType;
    private bool isExtruding;

    // Start is called before the first frame update
    void Start()
    {
    }

    private void Awake()
    {
        movementType = Movement.Absolute;
        isExtruding = false;
        nextIncrementalBufferSize = StartingLineBufferSize;
        freeLines = new List<GameObject>();
        CreateMoreBufferLines();

    }

    private void CreateMoreBufferLines()
    {
        for (int i = 0; i < nextIncrementalBufferSize; i++)
        {
            var line = Instantiate(PrintMaterial);
            line.SetActive(false);
            freeLines.Add(line);
        }
        nextIncrementalBufferSize = nextIncrementalBufferSize * 2;
    }

    public GameObject GetFreeLine()
    {
        if (!freeLines.Any())
        {
            CreateMoreBufferLines();            
        }
        var line = freeLines[0];
        freeLines.RemoveAt(0);
        line.SetActive(true);
        line.GetComponent<MeshRenderer>().enabled = true;
        return line;
    }

    public void ReturnFreeLine(GameObject line)
    {
        line.SetActive(false);
        freeLines.Add(line);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public IEnumerator NextCommands(List<GCommand> commands, DoneDelegate done)
    {
        linesCreated = new List<GameObject>();

        foreach (var command in commands)
        {
            if (command.Type == GCommand.CommandType.AbsolutePositioning)
            {
                movementType = Movement.Absolute;
            }
            else if (command.Type == GCommand.CommandType.RelativePositioning)
            {
                movementType = Movement.Relative;
            }
            else if (command.Type == GCommand.CommandType.Move)
            {
                if (ImmediateMove)
                {
                    MoveHeadImmediate(command, ExtruderWidth, LayerHeight);
                }
                else
                {
                    MoveHead(command, ExtruderWidth, LayerHeight);
                }
            }
            else if (command.Type == GCommand.CommandType.Home)
            {
                if (ImmediateMove)
                {
                    HomeImmediate();
                }
                else
                {
                    Home();
                }
            }
            else
            {
                throw new NotSupportedException("PrintHead does not support GCommand " + command.ToString());
            }

            command.ReleaseCommand();
        }
        yield return null;
        done(linesCreated);
    }

    private void HomeImmediate()
    {
        transform.position = new Vector3(0, 0, 0);
    }

    private IEnumerator Home()
    {
        var nextPosition = new Vector3(0, 0, 0);
        while (Vector3.Distance(transform.position, nextPosition) > .0001)
        {
            transform.position = Vector3.MoveTowards(transform.position, nextPosition, MoveStep * Time.deltaTime);
            yield return null;
        }

        transform.position = nextPosition;
    }

    private void MoveHeadImmediate(GCommand move, float width, float height)
    {
        var startPosition = transform.position;
        var nextPosition = transform.position;
        
        if (move.HasE && move.E > 0)
        {
            isExtruding = true;
        }
        
        if (MovementType == Movement.Relative)
        {
            nextPosition.x += move.X;
            nextPosition.y += move.Y;
            nextPosition.z += move.Z;
        }
        else
        {
            if (move.HasX)
            {
                nextPosition.x = move.X;
            }
            if (move.HasY)
            {
                nextPosition.z = move.Y;
            }
            if (move.HasZ)
            {
                nextPosition.y = move.Z;
            }
        }

        // Only extrude if we actually moved the nozzle
        if (isExtruding && (move.HasX || move.HasY || move.HasZ) && (transform.position - nextPosition).magnitude >= MinimumExtrusionLength)
        {
            var line = GetFreeLine();
            line.GetComponent<ProceduralMesh>().GenerateLine(startPosition, nextPosition, width, height);
            linesCreated.Add(line);
        }

        transform.position = nextPosition;

        isExtruding = false;
    }

    private IEnumerator MoveHead(GCommand move, float width, float height)
    {
        var startPosition = transform.position;
        var nextPosition = transform.position;
        if (move.HasE && move.E > 0)
        {
            isExtruding = true;
        }

        if (MovementType == Movement.Relative)
        {
            nextPosition.x += move.X;
            nextPosition.y += move.Y;
            nextPosition.z += move.Z;
        }
        else
        {
            if (move.HasX)
            {
                nextPosition.x = move.X;
            }
            if (move.HasY)
            {
                nextPosition.z = move.Y;
            }
            if (move.HasZ)
            {
                nextPosition.y = move.Z;
            }
        }

        while (Vector3.Distance(transform.position, nextPosition) > .0001)
        {
            transform.position = Vector3.MoveTowards(transform.position, nextPosition, MoveStep * Time.deltaTime);
            yield return null;
        }

        transform.position = nextPosition;

        if (isExtruding)
        {
            var line = GetFreeLine();
            line.GetComponent<ProceduralMesh>().GenerateLine(startPosition, nextPosition, width, height);
            linesCreated.Add(line);
        }

        isExtruding = false;
    }
}

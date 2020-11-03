using System;
using System.Collections;
using System.Collections.Generic;
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
    public delegate void DoneDelegate(List<GameObject> linesCreated);

    private List<GameObject> linesCreated = new List<GameObject>();

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
            if (command is AbsolutePositioning)
            {
                movementType = Movement.Absolute;
            }
            else if (command is RelativePositioning)
            {
                movementType = Movement.Relative;
            }
            else if (command is Move move)
            {
                if (ImmediateMove)
                {
                    MoveHeadImmediate(move, ExtruderWidth, LayerHeight);
                }
                else
                {
                    MoveHead(move, ExtruderWidth, LayerHeight);
                }
            }
            else if (command is Home)
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

    private void MoveHeadImmediate(Move move, float width, float height)
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
        if (isExtruding && (move.HasX || move.HasY || move.HasZ))
        {
            var line = Instantiate(PrintMaterial);
            line.GetComponent<ProceduralMesh>().GenerateLine(startPosition, nextPosition, width, height);
            linesCreated.Add(line);
        }

        transform.position = nextPosition;

        isExtruding = false;
    }

    private IEnumerator MoveHead(Move move, float width, float height)
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
            var line = Instantiate(PrintMaterial);
            line.GetComponent<ProceduralMesh>().GenerateLine(startPosition, nextPosition, width, height);
            linesCreated.Add(line);
        }

        isExtruding = false;
    }
}

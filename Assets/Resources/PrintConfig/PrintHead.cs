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

    public GameObject PrintMaterial;
    public float MoveStep = 10;
    public bool ImmediateMove = true;
    public float ExtruderWidth = 0.2f;
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

        if (isExtruding)
        {
            // Extrude small chunks of line
            var currentPosition = startPosition;
            var prevPosition = startPosition;
            var direction = (nextPosition - startPosition).normalized;
            while(currentPosition != nextPosition)
            {
                currentPosition += direction;
                if ((startPosition - currentPosition).magnitude > (startPosition - nextPosition).magnitude)
                {
                    currentPosition = nextPosition;
                }

                var line = Instantiate(PrintMaterial);
                line.GetComponent<ProceduralMesh>().GenerateLine(prevPosition, currentPosition, width, height);
                linesCreated.Add(line);

                prevPosition = currentPosition;
            }
            //var line = Instantiate(PrintMaterial);
            //line.GetComponent<ProceduralMesh>().GenerateLine(startPosition, nextPosition, width, height);
            //linesCreated.Add(line);
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

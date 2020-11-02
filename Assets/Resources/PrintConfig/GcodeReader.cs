using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GcodeReader : IEnumerable<GcodeReader.GCommand>
{
    public TextAsset gcode;

    private List<GCommand> gcommands;
    private int queuePosition = 0;

    // Start is called before the first frame update
    public GcodeReader(TextAsset gcodeAsset)
    {
        if (gcodeAsset == null)
        {
            throw new NotSupportedException("Cannot handle no gcode asset");
        }
        gcode = gcodeAsset;
        gcommands = new List<GCommand>();

        foreach (var line in gcode.text.Split("\n"[0]))
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.Contains(";"))
            {
                trimmedLine = trimmedLine.Substring(0, trimmedLine.IndexOf(";"));
            }
            AddNewCommand(ParseGCommand(trimmedLine));
        }
    }

    public void ResetQueuePosition()
    {
        queuePosition = 0;
    }

    public GCommand PeekCommand()
    {
        if (queuePosition >= gcommands.Count())
        {
            return null;
        }

        var next = gcommands[queuePosition];
        return next;
    }

    public GCommand PopCommand()
    {
        if (queuePosition >= gcommands.Count())
        {
            return null;
        }

        var next = gcommands[queuePosition];
        queuePosition++;
        return next;
    }

    public IEnumerator<GCommand> GetEnumerator()
    {
        return gcommands.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return gcommands.GetEnumerator();
    }

    private void AddNewCommand(GCommand command)
    {
        if (command != null)
        {
            gcommands.Add(command);
        }
    }

    private GCommand ParseGCommand(string line)
    {
        if (line.Length == 0 || 
            line.StartsWith(";") ||
            line.StartsWith("M140") ||
            line.StartsWith("M190")
            )
        {
            return null;
        }

        if (line.Length == 0)
        {
            return null;
        }

        if (line.StartsWith("G28"))
        {
            return new Home();
        }

        if (line.StartsWith("G90"))
        {
            return new AbsolutePositioning();
        }

        if (line.StartsWith("G91"))
        {
            return new RelativePositioning();
        }

        if (line.StartsWith("G0") || line.StartsWith("G1"))
        {
            return new Move(line);
        }

        Debug.Log("Unhandled gcommand: " + line);
        return null;
    }

    public class GCommand
    {

    }

    public class AbsolutePositioning : GCommand
    {

    }

    public class RelativePositioning : GCommand
    {

    }

    public class Home: GCommand
    {

    }

    public class Move : GCommand
    {
        public float X = 0;
        public float Y = 0;
        public float Z = 0;
        public float E = 0;

        public bool HasX = false;
        public bool HasY = false;
        public bool HasZ = false;
        public bool HasE = false;

        public Move(string line)
        {
            foreach (var part in line.Split(' '))
            {
                if (part.StartsWith("X"))
                {
                    HasX = true;
                    X = float.Parse(part.Substring(1));
                }

                if (part.StartsWith("Y"))
                {
                    HasY = true;
                    Y = float.Parse(part.Substring(1));
                }

                if (part.StartsWith("Z"))
                {
                    HasZ = true;
                    Z = float.Parse(part.Substring(1));
                }

                if (part.StartsWith("E"))
                {
                    HasE = true;
                    E = float.Parse(part.Substring(1));
                }
            }
        }
    }
}
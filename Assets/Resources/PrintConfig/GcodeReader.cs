using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GcodeReader
{
    public TextAsset gcode;

    int gcommandBatchSize = 100;
    List<GCommand> freeCommands;
    List<string> gcodeText;
    GCommand nextCommand = null;
    int listLocation;

    // Start is called before the first frame update
    public GcodeReader(TextAsset gcodeAsset)
    {
        freeCommands = new List<GCommand>();

        if (gcodeAsset == null)
        {
            throw new NotSupportedException("Cannot handle no gcode asset");
        }
        gcode = gcodeAsset;
        //gcommands = new List<GCommand>();
        gcodeText = gcode.text.Split("\n"[0]).ToList();
        ResetQueuePosition();
    }

    private void ReturnUnusedCommand(GCommand command)
    {
        command.ResetCommand();
        freeCommands.Add(command);
    }

    GCommand GetFreeCommand()
    {
        if (!freeCommands.Any())
        {
            for (int i = 0; i < gcommandBatchSize; i++)
            {
                freeCommands.Add(new GCommand(GCommand.CommandType.Unknown, ReturnUnusedCommand));
            }
            gcommandBatchSize *= 2;
        }

        var command = freeCommands[0];
        freeCommands.RemoveAt(0);
        return command;
    }
    public void ResetQueuePosition()
    {
        listLocation = 0;
        nextCommand = ParseNextCommand();
    }

    public GCommand PeekCommand()
    {
        return nextCommand;
    }

    public GCommand PopCommand()
    {
        var returnCommand = nextCommand;
        nextCommand = ParseNextCommand();
        return returnCommand;
    }

    private GCommand ParseNextCommand()
    {
        GCommand command = null;
        while (command == null && listLocation < gcodeText.Count())
        {
            var trimmedLine = gcodeText[listLocation].Trim();
            if (trimmedLine.Contains(";"))
            {
                trimmedLine = trimmedLine.Substring(0, trimmedLine.IndexOf(";"));
            }
            command = ParseGCommand(trimmedLine);
            listLocation++;
        }

        return command;
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
            var command = GetFreeCommand();
            command.SetType(GCommand.CommandType.Home);
            return command;
        }

        if (line.StartsWith("G90"))
        {
            var command = GetFreeCommand();
            command.SetType(GCommand.CommandType.AbsolutePositioning);
            return command;
        }

        if (line.StartsWith("G91"))
        {
            var command = GetFreeCommand();
            command.SetType(GCommand.CommandType.RelativePositioning);
            return command;
        }

        if (line.StartsWith("G0") || line.StartsWith("G1"))
        {
            var command = GetFreeCommand();
            command.SetType(GCommand.CommandType.Move);
            command.ParseMoveCommand(line);
            return command;
        }

        Debug.Log("Unhandled gcommand: " + line);
        return null;
    }

    public class GCommand
    {
        public delegate void Release(GCommand command);

        public enum CommandType
        {
            AbsolutePositioning,
            RelativePositioning,
            Home,
            Move,
            Unknown
        }

        public CommandType Type;
        private readonly Release release;
        public float X = 0;
        public float Y = 0;
        public float Z = 0;
        public float E = 0;

        public bool HasX = false;
        public bool HasY = false;
        public bool HasZ = false;
        public bool HasE = false;

        public void ResetCommand()
        {
            Type = CommandType.Unknown;
            X = 0;
            Y = 0;
            Z = 0;
            E = 0;
            HasX = false;
            HasY = false;
            HasZ = false;
            HasE = false;
        }

        public void SetType(CommandType type)
        {
            Type = type;
        }

        public GCommand(CommandType type, Release release)
        {
            Type = type;
            this.release = release;
        }

        /// <summary>
        /// Called when command is no longer used, will place it back to be reused later.
        /// </summary>
        public void ReleaseCommand()
        {
            release(this);
        }

        public void ParseMoveCommand(string moveCommand)
        {
            foreach (var part in moveCommand.Split(' '))
            {
                if (part.Length == 0)
                {
                    continue;
                }

                if (part[0] == 'X')
                {
                    HasX = true;
                    X = float.Parse(part.Substring(1));
                }

                if (part[0] == 'Y')
                {
                    HasY = true;
                    Y = float.Parse(part.Substring(1));
                }

                if (part[0] == 'Z')
                {
                    HasZ = true;
                    Z = float.Parse(part.Substring(1));
                }

                if (part[0] == 'E')
                {
                    HasE = true;
                    E = float.Parse(part.Substring(1));
                }
            }
        }
    }
}
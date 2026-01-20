using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CustomBluetoothController;

public class SequenceTreeNode(InputType type, string action = "")
{
    public InputType Type = type;
    public DateTime Timestamp = DateTime.Now;
    public string Action = action;

    public Dictionary<InputType, SequenceTreeNode> Next = [];
}
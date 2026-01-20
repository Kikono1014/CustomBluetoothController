using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CustomBluetoothController;

public record ConfigDto(List<string> Sequence, string Action);
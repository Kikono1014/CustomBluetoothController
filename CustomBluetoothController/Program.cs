using System.Diagnostics;
using System.Formats.Asn1;
using System.Text.Json;

namespace CustomBluetoothController;

public class Program
{
    private const int _comboDelay = 2000;
    private const int _singleDelay = 800;

    private static SequenceTreeNode _tree = new(InputType.Undefined);
    private static SequenceTreeNode _current = new(InputType.Undefined);

    private static Timer _timer = null!;

    private static void Init()
    {
        _tree = BuildTree();
        _current = _tree;
        
        _timer = new Timer(_ => 
        {
            CommitAction(_current.Action);
            _current = _tree;
        }, null, Timeout.Infinite, Timeout.Infinite);

    }

    static async Task Main()
    {
        Init();

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };
        
        using var listener = new HciListener(65535);

        try
        {
            await foreach (byte[] packet in listener.ListenAsync(cts.Token))
            {
                if (packet[0] != 5) continue;
                
                var input = InputParser.ParseInput(packet);
                
                if (input == InputType.Undefined) continue;
                
                // Console.WriteLine($"{input}");

                ProcessInput(input);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\nListening stopped by user.");
        }
    }
    
    
    private static void ProcessInput(InputType input)
    {
        _timer?.Change(Timeout.Infinite, Timeout.Infinite);

        if (!_current.Next.TryGetValue(input, out var nextNode))
        {
            if ((input == InputType.Play || input == InputType.Pause) && 
                _current.Next.TryGetValue(InputType.PlayPause, out nextNode))
            {
                // Successfully mapped Play/Pause to the PlayPause node
            }
        }

        if (nextNode == null)
        {
            _current = _tree;
        }
        else
        {
            _current = nextNode;

            if (_current.Next.Count == 0)
            {
                CommitAction(_current.Action);
                _current = _tree;
            }
            else
            {
                var delay = _tree.Next.ContainsValue(_current) ? _singleDelay : _comboDelay;
                _timer?.Change(delay, Timeout.Infinite);
            }
        }

        _current.Timestamp = DateTime.Now;
    }

    private static SequenceTreeNode BuildTree()
    {
        var config = JsonSerializer.Deserialize<List<ConfigDto>>(
            File.ReadAllText("./config.json"),
            new JsonSerializerOptions(){ PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
        )!;
        
        var root = new SequenceTreeNode(InputType.Undefined);
        var current = root;

        foreach (var configuration in config)
        {
            foreach (var input in configuration.Sequence)
            {
                var inputType = InputParser.ToEnum(input);
                if (!current.Next.TryGetValue(inputType, out var next))
                {
                    current.Next[inputType] = new(inputType);
                    current = current.Next[inputType];
                }
                else
                {
                    current = next;
                }
            }
            current.Action = configuration.Action;
            current = root;
        }

        return root;
    }

    private static void CommitAction(string action)
    {
        if (action != "")
        {
            Process.Start(new ProcessStartInfo("bash", $"-c \"{action}\""));
        }
    }
}
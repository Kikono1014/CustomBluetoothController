using System.Diagnostics;
using System.Formats.Asn1;
using System.Text.Json;

namespace CustomBluetoothController;

public class Program
{
    private static byte _volume = 93;
    private const int ComboDelay = 2000;
    private const int SingleDelay = 800;
    private static bool? IsPaused = null;

    static async Task Main()
    {
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };
        
        using var listener = new HciListener(65535);

        var tree = BuildTree();
        var current = tree;
        
        Timer timer = null!;
        timer = new Timer(_ => 
        {
            CommitAction(current.Action);
            current = tree;
        }, null, Timeout.Infinite, Timeout.Infinite);

        try
        {
            await foreach (byte[] packet in listener.ListenAsync(cts.Token))
            {
                if (packet[0] != 5) continue;
                
                var input = ParseActionType(packet);

                // it sends those ad "keep alive"
                if (input == InputType.Undefined) continue;
                if (IsPaused != null && (bool)IsPaused && input == InputType.Pause) continue;
                if (IsPaused != null && !(bool)IsPaused && input == InputType.Play) continue;

                IsPaused = input switch
                {
                    InputType.Play  => false,
                    InputType.Pause => true,
                    _ => IsPaused
                };
                

                Console.WriteLine($"{input}");
                
                timer?.Change(Timeout.Infinite, Timeout.Infinite);

                if (!current.Next.TryGetValue(input, out var nextNode))
                {
                    if ((input == InputType.Play || input == InputType.Pause) && 
                        current.Next.TryGetValue(InputType.PlayPause, out nextNode))
                    {
                        // Successfully mapped Play/Pause to the PlayPause node
                    }
                }

                if (nextNode == null)
                {
                    current = tree;
                }
                else
                {
                    current = nextNode;

                    if (current.Next.Count == 0)
                    {
                        CommitAction(current.Action);
                        current = tree;
                    }
                    else
                    {
                        var delay = tree.Next.ContainsValue(current) ? SingleDelay : ComboDelay;
                        timer?.Change(delay, Timeout.Infinite);
                    }
                }

                current.Timestamp = DateTime.Now;
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\nListening stopped by user.");
        }
    }

    public static InputType ParseActionType(byte[] packet) => packet.Length switch
    {
        22 => packet[^2] switch
        {
            0x44 => InputType.Play,
            0x46 => InputType.Pause,
            0x4B => InputType.Next,
            0x4C => InputType.Prev,
            _    => InputType.Undefined
        },
        29 => HandleVolumePacket(packet[^1]),
        _  => InputType.Undefined
    };

    private static InputType HandleVolumePacket(byte newVolume)
    {
        var direction = newVolume.CompareTo(_volume);
        _volume = newVolume;

        return direction switch {
            > 0 => InputType.Up,
            < 0 => InputType.Down,
            _   => InputType.Undefined
        };
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
                var inputType = ToEnum(input);
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

    private static InputType ToEnum(string value)
    {
        return value switch
        {
            "Pause" => InputType.Pause,
            "Play" => InputType.Play,
            "PlayPause" => InputType.PlayPause,
            "Next" => InputType.Next,
            "Prev" => InputType.Prev,
            "Up" => InputType.Up,
            "Down" => InputType.Down,
            _ => InputType.Undefined,
        };
    }
}
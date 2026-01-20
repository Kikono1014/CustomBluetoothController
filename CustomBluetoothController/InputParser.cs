using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CustomBluetoothController;

public static class InputParser
{
    private static byte _volume = 93; // value at which you usually keep headphones, in [0..127]
    private static bool? _isPaused = null; // tracks current state, headphones can send pause when it's paused as "keep alive"

    public static InputType ParseInput(byte[] packet)
    {
        var input = ParseInputType(packet);

        // "keep alive"
        if (_isPaused != null && (bool)_isPaused && input == InputType.Pause) return InputType.Undefined;
        if (_isPaused != null && !(bool)_isPaused && input == InputType.Play) return InputType.Undefined;

        _isPaused = input switch
        {
            InputType.Play  => false,
            InputType.Pause => true,
            _ => _isPaused
        };

        return input;
    }

    // Parsing packet sent from headphones to specified input type
    private static InputType ParseInputType(byte[] packet) => packet.Length switch
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

    // Usually volume is send as a value without identification which button was pressed Up or Down
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

    // Convert notations used in config to those used in code    
    public static InputType ToEnum(string value)
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
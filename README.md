# Bluetooth mapper
This program allows a configuration of custom actions for specified input sequences from your bluetooth headphones.
This program was made for SkullCandy Crusher ANC 2, so if your headphones have different way of handling input you would need to rewrite InputParser.cs.

You can use HciListener.c for testing.

# Setting up a daemon
To set up this as a daemon with systemd you need to create ~/.config/systemd/user/bt-mapper.service and configure it.
As an example:
```txt
[Unit]
Description=Bluetooth HCI Button Mapper
After=network.target pipewire.service

[Service]
WorkingDirectory=/path/to/working/directory/
# Point this to your published binary or the dotnet command
ExecStart=/path/to/bin/Release/net9.0/linux-x64/publish/CustomBluetoothController

# Required for playerctl/wpctl to find your session
Environment=XDG_RUNTIME_DIR=/run/user/1000
Environment=DBUS_SESSION_BUS_ADDRESS=unix:path=/run/user/1000/bus

Restart=on-failure

[Install]
WantedBy=default.target
```
and then run ./build.sh

# Config
To set up custom actions you need to create config.json file in working directory.
You can create any sequence from available inputs and write any terminal command to be executed as an action.

```json
[
    { "sequence": ["PlayPause"], "action": "playerctl play-pause" },
    { "sequence": ["Up"], "action": "wpctl set-volume -l 1.0 @DEFAULT_AUDIO_SINK@ 0.0667+" },
    { "sequence": ["Down"], "action": "wpctl set-volume @DEFAULT_AUDIO_SINK@ 0.0667-" },
    { "sequence": ["Next"], "action": "playerctl next" },
    { "sequence": ["Prev"], "action": "playerctl previous" },

    { "sequence": ["Up", "Down"], "action": "pactl set-sink-mute @DEFAULT_SINK@ toggle" },
    { "sequence": ["PlayPause", "Up", "Down", "Next"], "action": "systemctl suspend" },
    { "sequence": ["Up", "PlayPause", "Down"], "action": "espeak 'yay'; python3 ./scripts/open_playlist.py" },
]
```

# Notes
This program was made for Linux.  

Note that you would need to disable all system reactions to headphone input if you don't want to change volume while entering the sequence.
You can remap same behaviour for single command sequence.
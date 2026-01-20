#!/bin/bash

systemctl --user stop bt-mapper.service
dotnet publish -c Release -r linux-x64 --self-contained false -p:PublishSingleFile=true   
sudo setcap 'cap_net_raw,cap_net_admin+eip' ./CustomBluetoothController/bin/Release/net9.0/linux-x64/publish/CustomBluetoothController
systemctl --user daemon-reload
systemctl --user restart bt-mapper.service
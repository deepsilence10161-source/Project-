#!/usr/bin/env bash
# Installs Blender 4.2 LTS + VRM add-on (MIT) + system libs on an Ubuntu runner.
set -euo pipefail
sudo apt-get update -q >/dev/null
sudo apt-get install -y -q libxkbcommon0 libsm6 libice6 libxi6 libxrender1 libxfixes3 libxxf86vm1 libgl1 libegl1 libgl1-mesa-dri >/dev/null
mkdir -p "$HOME/bl" && cd "$HOME/bl"
if [ ! -x blender/blender ]; then
  curl -sL -o b.tar.xz https://download.blender.org/release/Blender4.2/blender-4.2.9-linux-x64.tar.xz
  tar -xf b.tar.xz && mv blender-4.2.9-linux-x64 blender && rm b.tar.xz
fi
curl -sL -o vrm_addon.zip https://github.com/saturday06/VRM-Addon-for-Blender/releases/download/v4.7.2/VRM_Addon_for_Blender-Extension-4_7_2.zip
./blender/blender -b --command extension install-file -r user_default -e vrm_addon.zip
pip install -q imageio-ffmpeg
echo "BLENDER=$HOME/bl/blender/blender" >> "$GITHUB_ENV"

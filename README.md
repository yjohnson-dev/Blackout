# Blackout

Blackout turns the screen black when you are not at the game. It keeps the blackout safe for OLED
monitors and out of the way.

## What it does

- The screen goes black when the game is in the background or when you do not touch the controls.
- The screen stays on during cutscenes.
- You can change the behavior for a duty, for crafting, and for other situations.
- A dim reminder shows that the game is still running. The reminder moves to a new position each time.
- The game can mute chosen audio channels while the screen is black.
- A duty that is ready brings the picture back.

## Requirements

- Windows.
- XIVLauncher with Dalamud, API level 15 or later.

## Install

1. In the game, open `/xlsettings`.
2. Open the **Experimental** tab.
3. Add this URL to **Custom Plugin Repositories**:

   ```
   https://raw.githubusercontent.com/yjohnson-dev/Blackout/main/pluginmaster.json
   ```

4. Select the **+** button and save the settings.
5. Open `/xlplugins`, search for **Blackout**, and install it.

This is a third-party plugin. It is not reviewed by the Dalamud team.

## Commands

| Command | Result |
| --- | --- |
| `/blackout` | Open or close the settings window. |
| `/blackout now` | Turn the screen black immediately. Any input brings it back. |
| `/blackout preview` | Turn the screen black for 5 seconds. |
| `/blackout on` | Turn the plugin on. |
| `/blackout off` | Turn the plugin off. |

You can also bind `/blackout now` to a macro.

## Build

The build runs in a container, so no .NET SDK is necessary on the host.

```sh
./scripts/build.sh
```

The script builds the plugin and copies it to the Dalamud `devPlugins` folder.

## License

MIT. See `LICENSE`.

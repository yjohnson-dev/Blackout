# Blackout

Blackout turns the screen black when the game isn't actively in focus or when AFK.

> [!warning]
> This plugin was written with assistance of an LLM. I have verified the functionality of the app and some surface-level security aspects, but I recommend you do the same before deciding to use untrusted code from the web. 

## What it does

- The screen goes black when the game is in the background or when you don't interact with the keyboard or controller.
- You can change the behavior for duties, cutscenes, and other contexts.
- A dim reminder shows that the game is still running, so you can tell an OLED is still on.
- You can mute certain audio channels while blacked out.

## Requirements

- XIVLauncher with Dalamud

## Install

Add this URL to **Custom Plugin Repositories**:

   ```
   https://raw.githubusercontent.com/yjohnson-dev/xiv-blackout/main/pluginmaster.json
   ```

This is a third-party plugin. It is not reviewed by the Dalamud team.

## Commands

| Command | Result |
| --- | --- |
| `/blackout` | Open or close the settings window. |
| `/blackout now` | Start or stop an immediate blackout. Any input brings the screen back. |
| `/blackout preview` | Show the blackout for 5 seconds. |
| `/blackout on` | Turn the plugin on. |
| `/blackout off` | Turn the plugin off. |

## Build

```sh
./scripts/build.sh
```

## License

MIT

# Sinden Companion

Sinden Companion is an extension application for the Sinden Lightgun.

It allows to switch recoil profiles based on the game being played, and to switch profiles dynamically based on memory values.

I built it for my own arcade cabinet, but it should work for anyone using the Sinden Lightgun on Windows.

## Features
- Executable name and/or Window title recoil profile switching
- Dynamic recoil profile switching based on memory values
- Supports manual switching of profiles with the tray icon.
- Support for Dolphin memory reading
- Start Lightgun.exe automatically
- Start automatically at boot

## What's missing
- Support for dynamically switching input profiles
  - Have not encountered a game where I need the mouse input but the basis is already implemented
- Support for dynamically remapping buttons / offscreen reload
    - Same as above
- Support for string/float/long/double memory values
    - Not sure if relevant. Though I could see swapping profiles based on a weapon delay between bullets.
- Support for recoil on events (i.e. recoil when a shot is actually fired)
    - Could be useful to get properly aware recoil events as opposed to blindly recoiling.
- Support for more emulators
    - Model 2
- More offsets added to the [wiki](https://github.com/sruon/SindenCompanion/wiki/Pointer-paths)!

## Requirements
- Sinden Windows **2.05beta** driver
- Firmware **1.9**
- If using **Dolphin**, the most recent x64 build is likely best. x86 is not supported.

Any other combination is **untested**.

## Getting started
- Download the most recent release from the [release page](https://github.com/sruon/SindenCompanion/releases)
- Unzip the content in the same folder as Lightgun.exe
  - Alternatively, you may unzip it anywhere and set the Lightgun.exe location in the configuration file.
- Edit the **config.yaml** file to your liking. The bundled configuration contains certain working examples.
- Start **SindenCompanion.exe**
- You may need to add the binary to your antivirus/Microsoft Defender exception list as the included memory reader and DLL injector are identified as threats


## Configuration file format

```yaml
global:
    # If true, will recoil every time a profile is switched. Useful for debugging.
    # recoil_on_switch: true

    # If set, will start Lightgun.exe when SindenCompanion starts.
    # If not set, will assume Lightgun.exe is already running.
    # Note: You do not need to set this if SindenCompanion is located in the same folder as Lightgun.exe
    # lightgun: "C:\\PATH_TO\\Lightgun.exe"

    # If set, will output debug information to the console.
    # debug: true
recoil_profiles:
    # List of recoil profiles
    # Name of the profile, to reference in Game profiles below.
    - name: "Single"
    # Automatic or single recoil
      automatic: false
    # Pulse Length slider where 40 is Weakest and 80 is Strongest
      pulse_length: 60
    # Delay between pulses in milliseconds where 0 is Fastest and 50 is Slowest
      delay_between_pulses: 0
    # Should offscreen recoil be enabled?
      offscreen: false
    # Strength of recoil where 0 is Weakest and 10 is Strongest
      strength: 10
    # Delay after first pulse where 0 is shortest and 16 is longest
      delay_after_first_pulse: 0
    # Pump recoil on event
      pump_on: false
    # Pump recoil off event
      pump_off: false
    # TBD  
      recoil_front_left: false
      recoil_front_right: false
      recoil_back_left: false
      recoil_back_right: false
game_profiles:
    # List of game profiles
    # Name of the profile
    - name: "Game 1"
    # Recoil profile to use - this does not apply if using memory reader
      profile: "Single"
    # Match rules
      match:
    # Setting an exe will match the binary name
        exe: "mygame.exe"
    # Setting a title will match the window title. Partial matches are supported.
    # Setting both will && the match
        title: "My Game"
    # Optional : Set a memory reader to read the game's memory and change profile dynamically
    memscan:
        # Pointer paths to use. First element maps to Player 1, second to Player 2.
        # For Dolphin, the first element is the 0x8xxxxxxx or 0x9xxxxxxx address within the Wii memory space. Do not add bases
        paths: 
          - mygame.exe+0x0018AC00,0x364,0x4
          # - "0x80507308,0xa5f" # Dolphin example
        # Type of value to read (byte, short, int, uint, dolphinbyte)
        type: "byte"
        # Matching values
        match:
        # Key corresponds to the value found in memory
        # Value corresponds to the profile to switch to
          0x0: "Single" # Knife
          0x1: "Single" # Glock
          0x6: "Auto" # AR
          0x7: "Single" # Sniper
```

The bundled configuration file includes working pointer paths for certain Lightgun titles and **Assault Cube** which may be easier to test.

- Download **Assault Cube** ([v1.3.0.2](https://assault.cubers.net/download.html))
- Switching between weapons should produce recoil profile swaps and recoil tests, if enabled

## Support
Please open an issue on Github for any bug report or feature request. I consider the project to be more or less completed for my needs and will only follow on a best-effort basis.

## Credits
- [memory.dll](https://github.com/erfg12/memory.dll/)
- [ManagedInjector.lib](https://github.com/holly-hacker/ManagedInjector)
- [Dolphin.Memory.Access](https://github.com/Sewer56/Dolphin.Memory.Access)
- [dolphin-memory-engine](https://github.com/aldelaro5/dolphin-memory-engine)  
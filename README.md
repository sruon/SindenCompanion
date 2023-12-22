# Sinden Companion

## Getting started
- Download the most recent release from the release page
- Unzip the content in the same folder as Lightgun.exe
- Start SindenCompanion.exe
- You may need to add the binary to your antivirus/Microsoft Defender exception list as the included memory reader and DLL injector are identified as threats

## Features
- Executable name and/or Window title recoil profile switching
- Dynamic recoil profile switching based on memory values
- Start Lightgun.exe automatically
- Start automatically at boot

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
    # Setting a title will match the window title
    # Setting both will && the match
        title: "My Game"
    # Optional : Set a memory reader to read the game's memory and change profile dynamically
    memscan:
        # Pointer paths to use. First element maps to Player 1, second to Player 2.
        paths: 
          - mygame.exe+0x0018AC00,0x364,0x4
        # Type of value to read (byte, short, int, uint)
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


Launching the application and switching between Notepad and Lightgun.exe should produce recoil events.

You may try with Assault Cube v1.3.0.2 to test the memory scan feature.

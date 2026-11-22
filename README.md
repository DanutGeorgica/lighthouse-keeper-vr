# Lighthouse Keeper (VR)

## Documentation
- [State of the art - Homework 4](https://docs.google.com/document/d/1EIpNnr4yg7cyYmR681d2eWMJL6vHJYoU/edit?usp=sharing&ouid=112303915405704568626&rtpof=true&sd=true)

- [Main Modules - Homework 5](https://docs.google.com/document/d/12SGNewXyobNGTXdxlC5tE6bC3SOVWns8PJDPm7ZQel4/edit?usp=sharing)

## Week 5 unity scene additions
- setup base project with xr
- added an island (from https://sandherostudio.itch.io/low-poly-island)
- generated lighthouse using AI (python blender script)
- added the following assets from internet:
  - table light - https://poly.pizza/m/uJDWrSJGVH
  - desk - https://poly.pizza/m/YJyJam67hJ
  - lantern - https://poly.pizza/m/h1HBWwsIGk
  - motorised spotlight - https://poly.pizza/m/BLq0yBWHce
  - clipboard - https://poly.pizza/m/8H5SDxwMnEA
  - radio - https://poly.pizza/m/ShNRjTDU3i
  - generator - https://poly.pizza/m/K58RQ63qR5
  - power box - https://poly.pizza/m/WFVjj4vnGg
  - binoculars - https://poly.pizza/m/PLmfHOiB08
  - compass - https://poly.pizza/m/LlnxQPETHh
  - marker buoy - https://3dassets.dev/assets/lighthouse-and-rocky-coast-marker-buoy-197873b5
  - foghorn - https://3dassets.dev/assets/lighthouse-and-rocky-coast-foghorn-on-mounting-4abd54cd
  - barometer - https://3dassets.dev/assets/abandoned-weather-station-and-storm-shelter-aneroid-ba-b151829e
  - thermometer - https://3dassets.dev/assets/abandoned-weather-station-and-storm-shelter-maximum-mi-373297fd
  - fuel jerrycan - https://polyfork.dev/asset/fuel-jerrycan-6f1d85

all of these were put on the main scene for graybox testing purposes.

## Week 6 additions

video: https://www.youtube.com/watch?v=zrrIB1fh8L4

- objects grayboxing was mostly done last week
- added skybox
- added ocean shader
- added rain particles
- remade some of the models (can be found in assets -> blender for the .blend files, or assets -> models -> interactables for the .fbx), mostly ai generated with some manual edits:
  - lantern
  - compass
  - power box
  - lever
  - button
  - radio with a few buttons on it

- added interactions for the models:
  - for the lantern: can grab by the handle and have the rest of it spin around as the player moves
  - for the compass: can be grabbed, always points in a certain configurable direction
  - for the power box: can grab the panel handle and open/close it
  - for the lever: grabbable handle, can be smoothly moved up/down
  - for the button: smooth button that uses poke
  - for the radio:
    - switch that uses physical hitbox of the controllers to be hit up/down (kind of clunky atleast without a vr headset), the controllers have sphere colliders that are set on a specific layer to interact with the switch
    - button that can be smoothly poked (same as the button mentioned before)
    - knob switch that can be grabbed and set on 7 levels based on controller rotation

## Week 7-8 additions

- visual scene additions:
  - new ocean (for the sake of allowing stuff to float) (https://assetstore.unity.com/packages/vfx/shaders/shoreline-stylized-water-for-urp-mobile-desktop-432064)
  - modified fog and lighting (they're volumetric now) (https://github.com/CristianQiu/Unity-URP-Volumetric-Light)

- modified interactables:
  - added new key switch interactable on the new main operations console
  - modified door hinges constraints to break less often (had no idea velocity limits had been a thing)
  - added events for all interactables (so they're actually usable and not just for show now)

- # added BOAT:
  - # very essential. it floats.
  - currently has a predetermined route
  - is able to float at the correct water position
  - is able to tilt nicely along the waves
  - has a nice volumetric light in the back

- added fresnel lens (the big lens in the lighthouse that light up outside):
  - has a nice volumetric cone beam
  - added basic rotations (in different speeds) and on/off function (from the control panel)

- ocean additions:
  - of course the new ocean (slightly less waves for now)
  - added obstacles (currently don't actually do anything):
    - debris, rocks, shipwreck, navigation buoy(also has a nice light)

- lighthouse additions:
  - new lights (bulkhead side lights and 1 ceiling light in the main room)
  - cabinet, control panel, the radio ui, and a few other models to grab

- added a central game script to handle and connect the main lighthouse systems:
  - beacon power and rotation speed
  - interior lights
  - foghorn
  - radio power, frequency, messages and responses

- added functionality to the lighthouse beacon:
  - can be turned on/off using the physical key switch
  - rotation speed can be adjusted using a 7-position knob
  - beacon lights and rotation are controlled by the same script

- added functionality to the interior lights:
  - controlled through the physical lever
  - lights start turned on when the scene starts

- added foghorn functionality:
  - connected the physical button to the foghorn through the game manager
  - pressing the button plays the foghorn audio using an AudioSource

- expanded the radio system:
  - added a radio power switch with an indicator light
  - added a frequency knob with 7 possible positions
  - added a world-space UI for displaying incoming transmissions and response options
  - radio messages are stored in a queue and displayed one at a time
  - messages can be queued even when the radio is turned off, and are displayed once it is powered on
  - added 3 response options for each transmission, which can be selected through the UI
  - added a physical transmit button for submitting the selected response
  - the player must tune to the correct frequency before a response can be submitted
  - added frequency-based text distortion: when tuned incorrectly, random letters in the incoming message and response options are replaced with incorrect characters, the distortion becomes more noticeable the further the frequency is from the required one, and disappears when tuned correctly the actual response options are kept unchanged, so selecting and submitting an answer still uses the original text

- added a basic generator refueling system (for the "repairs" and maintenance part of the project):
  - jerrycan can be grabbed and tilted upside down to pour fuel
  - added a particle system to simulate liquid fuel coming out of the jerrycan
  - fuel is only transferred when the jerrycan's pouring area overlaps the generator's fuel filling area
  - generator tracks the amount of fuel received and has a maximum capacity

- added the base models for future repair updates (made the powerbox door not break as often, added screwdriver and adjustable wrench, the generator and jerrycan)

- fixed the binoculars (now use rendertexture), zoom view uses a RenderTexture displayed through two quads with a zoomed Shader Graph material, seems kind of inconsistent with lighting but it is what it is

- added many models required by everythong mentioned before (ai generated with slight manual modifications in blender): generator, jerrycan, workstation_table, boat, fresnel_lens, key_switch, cabinet, control_panel, fishing_boat, navigation_buoy, rocks_cluster, shipwreck, debris, bulkhead_light, ceiling_light, foghorn, screwdriver, wrench
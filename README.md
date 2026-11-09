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
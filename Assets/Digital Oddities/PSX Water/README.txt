**************************************
*		PSX Water	     *
*         Created by Soundy777       *   
*            README FILE             *
**************************************

Requirements
------------
PSX Water currently works only with Universal Rendering Pipeline (v7.1.8 or later)
Make sure you have Universal RP package imported in the project before using this asset.


Demo Scenes
-----------
Check out the demo scene for a few examples of how to use this water in the context of a game level.
Note: make sure you have Universal RP 7.5 or later installed from Package Manager and also a URP pipeline asset assigned to Graphics Settings.


Usage
---------------------------
Begin by dragging one of the prefabs into your scene. This prefab will contain two mesh planes inside of a container gameObject. To simplify the process of applying materials I've included a simple util script which will apply the materials to the planes for you - feel free to remove this if you don't need/want it.
Next you'll want to go into the water materials and spend some time playing around with the various values to achieve different effects.

For the vegetation its important to begin by matching their material wave properties with that of the water they're going to be on. Place them slight above (0.01f) the water plane's y value. You may have to fiddle with the various material parameters to get them to sit nicely on the surface (some jank is authentic to the psx style, so don't overshoot for perfection)

For the buoyancy its again a case of matching the component's wave properties to that of the water. You will also need to create and position empty child objects to specify where forces should be applied to keep the object afloat. Experiment and have fun with it.


Support
-------
Hit me up if you find something broken or if you need a hand with anything. I love chatting to people but sometimes I do get locked into my own projects, but I usually respond within a couple hours. 

* Support-Discord: https://discord.gg/wvqMPAZw5D
* Email: 77soundy77@gmail.com

License
-------
All code is MIT Licensed, and all assets are CC BY 4.0. You must credit me if you use or modify them
To give me Credit please link to the itch.io page for the asset: https://soundy777.itch.io/psx-water-shader-unity-urp
and credit me as the Author: Soundy777
Thanks in Advance


Future updates
--------------
I'll try to keep this asset up to date as best as I can, just let me know what else to add & I'll try to drop it in. 


Version history
---------------
v1.0 March / 2025
First release

v2.0 April / 2025
Added: Vertex Painting Support
Added: Intersection FX
Added: Foliage Shader
Added: Buoyancy Script
Added: Simple Rotator Script
Fixed a bunch of issues & rigged up the demo a bit more with a few assets thrown in
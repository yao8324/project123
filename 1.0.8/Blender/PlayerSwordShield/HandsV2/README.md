# Oathbound gauntlets V2

Original procedural closed-grip hand/forearm meshes; no legacy MageHand geometry. The two OBJ files are editable static shapes in Blender (OBJ does not include skin weights). Keep the stated Unity coordinates: +Y blade, -Z toward the forearm. Meshes are in metres and the weapon grip is the origin.

Each hand has four curled fingers, one opposing thumb, a continuous palm/wrist/forearm volume, fitted steel plates, gold trim and leather seams. Separate left/right vertex sets use positive transforms and outward winding. Unity prefabs have a two-bone SkinnedMeshRenderer: Hand at the grip origin and Elbow at (+/-0.20,-0.48,-0.52). Palm/fingers follow Hand rigidly; the forearm gradually blends to Elbow so the sleeve can stay connected off-screen during wrist rotation. There is no per-finger skeleton.

Reproduction source: Assets/Scripts/Visuals/Editor/OathboundHandsBuilder.cs. BuildPrefabs() regenerates these exports and Unity mesh/prefab/material assets.

# MeshEdit
### Dissolve Unity Meshes and perform per-vertex edits in the Editor!   
  
#### Getting Started:
After placing the MeshEdit folder into *%YourProjectName%/Assets*, you can open MeshEdit by navigating to ***Window->MeshEdit*** or by pressing ***Ctrl+Shift+E***.  
**Editing is perfectly safe**, as all work is done to a copy of the original object and can be undone. MeshEdit settings are saved to your EditorPrefs for persistence.  
> If ***Edit on open*** is active, whenever and however the window is opened, any selected GameObjects will be immediately dissolved for editing. Likewise, if ***Save on window close*** is active, whenever and however the window is closed, changes will be saved back to the meshes being edited.  
  
#### Known issues:
***(As of v0.1)***
* "Instantiating material/mesh due to calling renderer.material/MeshFilter.mesh during edit mode. This will leak materials/meshes. Please use renderer.material/MeshFilter.sharedMesh instead."
  * *No known solutions yet, but the errors are the result of the specific way I've chosen to go about modifying the meshes.*
  
#### Planned features:
***(As of v0.1)***
* Sphere brush for selecting/modifying vertices.
  * *Choose to paint on X, Y, or Z axis. Brush will be resizable. Value per click/tick will be adjustable. Will have toggle for uniform movement or falloff based on distance to brush center.*
* Generate mesh
  * *Generate a plane mesh for anything between 3 vertices to any power-of-two. Rapid production of natural features like cliff walls, fields, etc.*

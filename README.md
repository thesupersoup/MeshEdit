# MeshEdit
### Dissolve Unity Meshes and perform per-vertex edits in the Editor!   
  
#### Getting Started:
After placing the MeshEdit folder into *%YourProjectName%/Assets*, you can open MeshEdit by navigating to ***Window->MeshEdit*** or by pressing ***Ctrl+Shift+E***.  
  
**Editing is perfectly safe**, as all work is done to a copy of the original object and can be undone. MeshEdit settings are saved to your EditorPrefs for persistence.  
> If ***Edit on open*** is active, whenever and however the window is opened, any selected GameObjects will be immediately dissolved for editing. Likewise, if ***Save on window close*** is active, whenever and however the window is closed, changes will be saved back to the meshes being edited.  
  
#### Known issues:
***(As of v0.2)***
* Occasionally exiting the MeshEdit window while editing will leave editable meshes visible and source meshes disabled
  * No data is lost, it's just inconvenient
  
#### Planned features:
***(As of v0.2)***
* Sphere brush for selecting/modifying vertices.
  * *Choose to paint on X, Y, or Z axis. Brush will be resizable. Value per click/tick will be adjustable. Will have toggle for uniform movement or falloff based on distance to brush center.*
* Generate mesh
  * *Generate a plane mesh for anything between 3 vertices to any power-of-two. Rapid production of natural features like cliff walls, fields, etc.*

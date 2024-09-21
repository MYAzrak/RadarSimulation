# RadarSimulation

CMP Senior Project - Simulation of maritime radar detection

### Clone Repo

1. Add project from disk.
2. Choose `RadarSimulation -> RadarProject`.
3. Use version `2022.3.40f1`.

### For Procedural Land Generation

#### Noise and Color Maps

1. Create an empty GameObject named **"Map Generator"** and apply `MapGenerator.cs` and `MapDisplay.cs` scripts to it.
2. Create a **Plane** 3D GameObject, and remove the Mesh Collider for now.
3. Apply the Plane to the **Map Generator** object's **Map Display (Script)'s Texture Renderer**.
4. Apply the material from `Assets/Materials/Land` to the Plane.
5. Click on the **Map Generator** in Unity and choose the desired width, height, and scale.

#### Mesh

1. Create a new empty GameObject named **"Mesh"**.
2. Add a **Mesh Filter** and **Mesh Renderer** to the Mesh object (via the Component menu).
3. Assign the Mesh Material from `Assets/Materials` to the Mesh object.
4. Drag the Mesh object to the **Map Generator**'s **Map Display**'s Mesh Filter & Mesh Renderer fields.
5. In the **Map Generator** object, select the **Mesh** option under the Draw Mode dropdown.

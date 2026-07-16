GLB → OBJ + Textures (converted by TACO in browser)

Files:
  model.obj   - geometry (vertices / UV / normals / faces)
  model.mtl   - materials
  textures/   - all extracted textures

PBR texture mapping (extensions to standard OBJ MTL):
  map_Kd       baseColor (standard)
  map_Bump     normal map (standard)
  map_Ke       emissive (PBR extension)
  map_Pm       metallic   (PBR extension)
  map_Pr       roughness  (PBR extension)
  map_Ao       ambient occlusion (extension; commented in MTL)

Notes:
  - World transforms have been baked into vertices; OBJ has no scene graph.
  - UV V axis flipped (1 - V) to match OBJ convention (origin bottom-left).
  - Skeletal animation / morph targets / cameras / lights are NOT exported.
  - Normals are re-normalized after world transform to handle non-uniform scaling.

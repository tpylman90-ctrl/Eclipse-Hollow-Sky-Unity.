# Eclipse short-run environment kit

The library is original procedural Blender work and contains no third-party
mesh or texture dependencies. It is built for the existing short skyway run.

Run from the Unity repository root with Blender 4.x:

```sh
blender --background --factory-startup --python Blender/EclipseWorld/generate_library.py
```

The script creates Unity Resources assets under `Assets/Resources/EclipseWorld`:

- `RoadSegment`: one detailed 39 m roadway module with layered decking, edge
  lighting, underside ribs, and center markings.
- `StandardTower`: a compact shared tower mesh. Runtime placement varies its
  height, width, depth, and rotation; the facade atlas carries the small-scale
  window and panel detail for distant views.
- `HeroTowerA` and `HeroTowerB`: near-route architectural landmarks with
  modeled floor bands, corner spars, decks, entrances, and distinct crowns.
- `Eclipse_FacadeAtlas.png`: original 256 px tiled color atlas with lit windows,
  panel seams, color variation, and warm office lights.

The Unity runtime places eight hero landmarks and gives each a detail-to-atlas
LOD. Distant buildings use the shared standard mesh. The road and all static
scenery are combined for rendering. Keep the source script and generated OBJ,
MTL, and atlas together so the library stays reproducible.

The Unity build script restores the compact base64 atlas source as a PNG before
model import. This keeps the project diff text-only while allowing Unity's OBJ
importer to bind the same baked color atlas used by Blender.

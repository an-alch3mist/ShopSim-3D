# LOG.md created, perform LOG.SaveLog(str, format) to append text here:

```sceneGameObject-hierarchy
=== Component Abbreviations ===
asrc = AudioSource
alstn = AudioListener
cam = Camera
================================

./player/(scale:1.0 | asrc, CharacterController, FirstPersonController, FirstPersonGrabber)
└ playerCam (scale:1.0 | alstn, cam, UniversalAdditionalCameraData)

```

```sceneGameObject-hierarchy
=== Component Abbreviations ===
dmc = MeshFilter | MeshRenderer
bc = BoxCollider
mc = MeshCollider
asrc = AudioSource
alstn = AudioListener
cam = Camera
================================

./Level/(scale:1.0 | no components)
├ Plane (100 x 100) (scale:1.0 | dmc, mc, ProBuilderMesh, ProBuilderShape)
├ cubeModel x1 (scale:1.0 | no components)
│ └ Cube (scale:1.0 | dmc, bc, ProBuilderMesh, ProBuilderShape)
├ cubeModel x0.5 (scale:0.5 | no components)
│ └ Cube (scale:1.0 | dmc, bc, ProBuilderMesh, ProBuilderShape)
├ cubeModel x0.1 (scale:(0.1,0.2,0.1) | no components)
│ └ Cube (scale:1.0 | dmc, bc, ProBuilderMesh, ProBuilderShape)
├ shelfModel (scale:1.0 | no components)
│ ├ lowerBoard (scale:1.0 | dmc, mc, ProBuilderMesh, ProBuilderShape)
│ ├ lowerShelf (y=0.2) (scale:1.0 | dmc, mc, ProBuilderMesh, ProBuilderShape)
│ ├ upperBoard (scale:1.0 | dmc, mc, ProBuilderMesh, ProBuilderShape)
│ └ upperShelf (y=1.0) (scale:1.0 | dmc, mc, ProBuilderMesh, ProBuilderShape)
├ npcModel 0.5 x 1.8 (scale:1.0 | no components)
│ ├ bodyCube (scale:(0.5,1.8,0.5) | dmc, bc, ProBuilderMesh, ProBuilderShape)
│ └ headCube (scale:(0.5,0.2,0.5) | dmc, bc, ProBuilderMesh, ProBuilderShape)
└ player (scale:1.0 | asrc, CharacterController, FirstPersonController, FirstPersonGrabber)
  └ playerCam (scale:1.0 | alstn, cam, UniversalAdditionalCameraData)

```


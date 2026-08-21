# Owner Direction

Treat this as the primary product focus until I replace or remove it. Each scheduled run should implement one scoped step toward this direction.

## Current focus: map design and play experience

The current map feels too small, linear, and uneventful. A player can complete it quickly without needing to make many interesting decisions or overcome much challenge.

For the next several development runs, prioritize expanding and improving the playable map. The goal is not simply to make the level larger; it should become more engaging to navigate, replay, and master. As we build out the level to be larger we should do it in a modular way. Creating new rooms in separate scenes or prefabs makes sense to me. And additive loading (if in scenes) makes sense. Tile prefabs also makes sense. So use your discression.

As the map size grows, we will need to adjust the camera so it follows the drown.

### Desired player experience

- Create a clearer sense of progression from the starting area to the final objective.
- Introduce distinct areas with recognizable landmarks, silhouettes, lighting, and gameplay purposes.
- Add meaningful route choices, including safer routes, dangerous shortcuts, and optional areas containing useful rewards.
- Improve encounter pacing so traversal alternates between anticipation, navigation, resource decisions, combat pressure, and brief recovery.
- Use existing mechanics—Signal, powered territory, dead zones, salvage, enemies, gates, towers, and shortcuts—to create challenge before inventing additional systems.
- Make positioning and navigation matter. Enemy placement, environmental hazards, sightlines, cover, and restricted spaces should create tactical situations.
- Ensure the critical route remains understandable without relying entirely on HUD instructions.
- Extend playtime through meaningful content and decisions rather than empty corridors, excessive walking, or repetitive encounters.

### Level-authoring requirements

Prefer authoring the map through Unity scenes, prefabs, and serialized configuration rather than constructing level geometry and content through runtime code.

- Build reusable modular environment prefabs for floors, walls, doors, hazards, landmarks, encounter spaces, and decorative elements.
- Place environment geometry, objectives, enemies, pickups, lighting, and navigation cues directly in the scene wherever practical.
- Use code for reusable behavior and runtime state, not for defining the map layout.
- Gradually migrate existing runtime-generated map content into scene-authored objects without breaking the current playable loop.
- Avoid a risky all-at-once rewrite. Each run should leave the game playable and should convert or improve one coherent section of the map.
- Keep tuning data in ScriptableObjects or serialized prefab/scene fields when it may need adjustment during playtesting.
- Prefer authored meshes and materials over primitive placeholder geometry for important landmarks and frequently seen structures.

### Priorities

1. Establish a strong overall level layout and critical path.
2. Create a compelling opening area that teaches movement, Signal, and powered territory quickly.
3. Add the first meaningful route choice and risk/reward decision.
4. Build distinct encounter spaces around existing enemy behaviors.
5. Improve landmarks, environmental storytelling, navigation cues, and visual variety.
6. Add optional spaces, shortcuts, rewards, and replay value.
7. Polish lighting, decoration, transitions, and environmental atmosphere.

### Acceptance signals

The map is improving when:

- a first-time player understands where they are trying to go;
- the first minute includes a meaningful action or decision;
- completing the level requires engagement with several core mechanics;
- different areas are recognizable from screenshots without seeing the HUD;
- at least one route choice changes risk, resource cost, or reward;
- enemies and hazards create deliberate encounters rather than incidental contact;
- the level cannot be completed immediately by simply moving directly toward the objective;
- added playtime comes from decisions and escalating situations, not padding; and
- most map content can be inspected and adjusted visually in the Unity Editor.

### Avoid for now

- Expanding the map with large empty spaces.
- Adding unrelated mechanics before existing mechanics are used effectively.
- Procedurally or programmatically generating the primary level layout.
- Moving the entire map out of runtime code in one broad, destabilizing rewrite.
- Spending multiple runs on invisible architecture without delivering a playable map improvement.
- Replacing the established DEAD SIGNAL concept or core loop.


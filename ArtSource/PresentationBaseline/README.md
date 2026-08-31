# Presentation Run P01 comparison sources

These are unchanged reference captures selected during Presentation Run P01. They are documentation evidence, not Unity runtime assets and must not be imported under `Assets`.

| File | Original local source | State |
| --- | --- | --- |
| `P01-Central-Tower-Available-1600x900.png` | `Logs/PresentationPlaytest-20260829/01-Central-Tower-Available.png` | Central activation available |
| `P01-Spine-Powered-Gate-Open-1600x900.png` | `Logs/PresentationPlaytest-20260829/05-Spine-Tower-Powered-Gate-Open.png` | Spine powered, return gate open |
| `P01-Security-Trial-Cleared-1600x900.png` | `Logs/PresentationPlaytest-20260829/11-Lockdown-Cleared-Doors-Open.png` | Room B cleared, doors open |
| `P01-Dock-Uplink-Locked-1616x939.png` | `Logs/Run109-Final-Full/Full-16.png` | Legacy Dock return, uplink locked |
| `P02-Central-Tower-Hero-Finish-1600x900.png` | `Logs/Run206-CentralHeroCapture.xml` | Central activation available after the P02 hero finish |
| `P03-Cargo-Annex-Hero-Finish-1600x900.png` | `Logs/Run207-CargoHeroLockedFinal.xml` | Cargo Annex locked, withdrawal-side view after the P03 finish |
| `P04-Coolant-Reclamation-Hero-Finish-1600x900.png` | `Logs/Run208-CoolantHeroFinal.xml` | Coolant line stable, centered baffle-threading view after the P04 finish |
| `P05-Relay-Fork-Finish-1600x900.png` | `Logs/P05-Focused-Final.xml` | Relay Fork routed, with paired routing banks and forked copper floor feeds |
| `P05-Transfer-Vault-Finish-1600x900.png` | `Logs/P05-Focused-Final.xml` | Transfer Vault available, with the assembler bed, route threshold, and linear copper feeds |
| `P06-Warden-Bay-Hero-Finish-1600x900.png` | `Logs/Run210-P06-Focused.xml` | Warden deployed inside the refined red containment mount and shield lanes |
| `P06-Sapper-Cradle-Hero-Finish-1600x900.png` | `Logs/Run210-P06-Focused.xml` | Sapper deployed from the refined magenta siphon socket and ceramic cradle |
| `P07-Relay-Foundry-Hero-Finish-1600x900.png` | `Logs/Run211-P07-Focused-Final2.xml` | Relay powered, with refinished turbine/tower materials and state-driven induction inlays |
| `P08-Cooling-Gantry-Hero-Finish-1600x900.png` | `Logs/Run212-P08-Capture-Final.xml` | Relay payload stabilized, with the refinished exchanger, processing bed, pipe feeds, vent banks, and two return approaches |

Keep each source unchanged. Owning presentation runs should add clean post-change captures rather than overwrite these files. The Dock source includes development-window chrome and is suitable only for composition/state comparison; P14 must replace it with a clean 1600×900 post-change frame. The P03 raw-camera frame proves the Cargo materials and mesh render, but its extreme north-edge pose also exposes the existing objective-beacon shape and camera-edge void; it is not a human hierarchy verdict. The P04 frame proves the finish and stable state render while retaining the pre-existing Central powered-territory arc at the far left; it is likewise not a human hierarchy verdict. The P05 pair proves the shared surface language renders while preserving different forked-routing and linear-assembly silhouettes; existing black camera-edge void and neighboring cyan territory remain visible, so these frames are not a human hierarchy verdict. The P06 pair proves the two finishes and owning threats render, but the automated poses retain large existing objective and powered-territory overlays; use them as asset-binding evidence rather than a final contrast or comfort verdict. The P07 frame proves the two authored finish meshes, focal-machinery material separation, and powered state binding render together; the broad cyan powered-territory overlay and camera-edge void remain review debt for the dedicated lighting/resolution runs, so it is not a final hierarchy verdict. The P08 frame proves the distinct cold stabilization surface family, retained lifecycle coils, low processing bed, pipe/vent silhouettes, and two approach openings render together; neighboring Foundry cyan and the lower camera-edge void remain dedicated lighting/resolution debt, so this is not a human hierarchy verdict.

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

Keep each source unchanged. Owning presentation runs should add clean post-change captures rather than overwrite these files. The Dock source includes development-window chrome and is suitable only for composition/state comparison; P14 must replace it with a clean 1600×900 post-change frame. The P03 raw-camera frame proves the Cargo materials and mesh render, but its extreme north-edge pose also exposes the existing objective-beacon shape and camera-edge void; it is not a human hierarchy verdict. The P04 frame proves the finish and stable state render while retaining the pre-existing Central powered-territory arc at the far left; it is likewise not a human hierarchy verdict.

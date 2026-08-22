using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadSignal.Tests
{
    public sealed class BootstrapSmokeTests
    {
        [UnityTest]
        public IEnumerator SignalBolt_AuthoredCoverBlocksClosedGateButOpenDoorwayStaysClear()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var gamepad = InputSystem.AddDevice<Gamepad>();
            try
            {
                var game = Object.FindFirstObjectByType<DeadSignalGame>();
                var feedback = Object.FindFirstObjectByType<CombatFeedbackController>();
                var player = game.transform.Find("Maintenance Drone");
                player.position = new Vector3(2.5f, 0f, 0.4f);

                InputSystem.QueueStateEvent(gamepad,
                    new GamepadState { rightStick = Vector2.right }.WithButton(GamepadButton.RightShoulder));
                yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState { rightStick = Vector2.right });
                yield return new WaitForSeconds(0.18f);

                Assert.That(game.ActiveSignalBoltCount, Is.Zero,
                    "The closed authored shortcut gate should consume the swept projectile.");
                Assert.That(feedback.transform.Find("Bulkhead Signal Impact"), Is.Not.Null,
                    "Blocked fire should produce the generated cyan cover-impact confirmation.");

                yield return new WaitForSeconds(0.2f);
                player.position = new Vector3(-0.6f, 0f, 0.4f);
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.West));
                yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                yield return null;
                yield return new WaitForSecondsRealtime(0.08f);
                player.position = new Vector3(3f, 0f, 0.4f);
                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.West));
                yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                yield return null;
                InputSystem.QueueStateEvent(gamepad,
                    new GamepadState { rightStick = Vector2.right }.WithButton(GamepadButton.RightShoulder));
                yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState { rightStick = Vector2.right });
                yield return new WaitForSeconds(0.18f);

                Assert.That(game.ActiveSignalBoltCount, Is.EqualTo(1),
                    "Retracting the shortcut gate should let the same shot continue through its authored doorway.");
                Assert.That(game.transform.Cast<Transform>().Single(child => child.name == "Signal Bolt").position.x,
                    Is.GreaterThan(4.4f));
            }
            finally
            {
                InputSystem.RemoveDevice(gamepad);
            }
        }

        [UnityTest]
        public IEnumerator SceneLoad_BootstrapsCompleteRuntimeWithoutErrors()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            yield return null;

            var game = Object.FindFirstObjectByType<DeadSignalGame>();
            Assert.That(game, Is.Not.Null, "Runtime bootstrap did not create the game controller.");
            var signalSpine = GameObject.Find("Opening Signal Spine");
            var signalSpineTexture = Resources.Load<Texture2D>("Environment/SignalSpineInlay");
            var signalSpineMaterial = Resources.Load<Material>("Materials/SignalSpineInlay");
            Assert.That(signalSpine, Is.Not.Null, "The opening route should be authored directly in SampleScene.");
            Assert.That(signalSpine.transform.childCount, Is.EqualTo(5));
            Assert.That(signalSpine.GetComponentsInChildren<Collider>().Length, Is.Zero,
                "Navigation inlays must remain presentation-only and never block the opening route.");
            Assert.That(signalSpine.GetComponentsInChildren<Renderer>().Length, Is.EqualTo(5));
            Assert.That(signalSpineTexture, Is.Not.Null);
            Assert.That(signalSpineMaterial, Is.Not.Null);
            Assert.That(signalSpineMaterial.mainTexture, Is.EqualTo(signalSpineTexture));
            var towerPosition = new Vector3(-0.6f, 0f, 0.4f);
            var routeDistances = signalSpine.transform.Cast<Transform>()
                .Select(inlay => Vector3.Distance(inlay.position, towerPosition)).ToArray();
            Assert.That(routeDistances.Zip(routeDistances.Skip(1), (current, next) => next < current).All(value => value),
                Is.True, "Each authored inlay should advance continuously from extraction toward the tower.");
            Assert.That(signalSpine.transform.Cast<Transform>().All(inlay =>
                    Mathf.Abs(Mathf.DeltaAngle(inlay.eulerAngles.y, 125f)) < 0.1f),
                Is.True, "Every generated inlay must retain the visually verified tower-facing yaw.");
            Assert.That(Object.FindFirstObjectByType<DeadSignalHud>(), Is.Not.Null,
                "Runtime bootstrap should compose a dedicated HUD presenter.");
            Assert.That(Object.FindFirstObjectByType<ObjectiveBeaconHud>(), Is.Not.Null,
                "Runtime bootstrap should compose a dedicated objective beacon presenter.");
            var maintenanceDrone = game.transform.Find("Maintenance Drone");
            Assert.That(maintenanceDrone, Is.Not.Null);
            Assert.That(game.HasPlayerDroneAssets, Is.True,
                "The player prefab and original maintenance-drone texture should load from Resources.");
            Assert.That(game.PlayerDronePartCount, Is.EqualTo(4));
            Assert.That(maintenanceDrone.GetComponentsInChildren<Renderer>().Length, Is.EqualTo(4));
            Assert.That(Resources.Load<GameObject>("Actors/MaintenanceDroneModel"), Is.Not.Null,
                "The authored Blender model should load from Resources.");
            var authoredDronePrefab = Resources.Load<GameObject>("Actors/MaintenanceDroneAssembly");
            var authoredHullMaterial = Resources.Load<Material>("Materials/MaintenanceDroneHull");
            Assert.That(authoredHullMaterial, Is.Not.Null);
            Assert.That(authoredHullMaterial.mainTexture, Is.EqualTo(Resources.Load<Texture2D>("Actors/MaintenanceDroneHullAlbedo")),
                "The authored hull material should persistently map the generated albedo outside Play Mode.");
            Assert.That(authoredDronePrefab.transform.Find("Drone Chassis").GetComponent<Renderer>().sharedMaterial,
                Is.EqualTo(authoredHullMaterial), "Prefab Mode should display the mapped hull material.");
            Assert.That(authoredDronePrefab.transform.Find("Drone Signal Ring").GetComponent<Renderer>().sharedMaterial,
                Is.EqualTo(Resources.Load<Material>("Materials/MaintenanceDroneSignal")));
            Assert.That(authoredDronePrefab.transform.Find("Drone Core").GetComponent<Renderer>().sharedMaterial,
                Is.EqualTo(Resources.Load<Material>("Materials/MaintenanceDroneCore")));
            Assert.That(authoredDronePrefab.transform.Find("Drone Tool").GetComponent<Renderer>().sharedMaterial,
                Is.EqualTo(Resources.Load<Material>("Materials/MaintenanceDroneTool")));
            Assert.That(maintenanceDrone.GetComponentsInChildren<Collider>().Length, Is.Zero,
                "The authored player drone should remain presentation-only so deterministic movement stays authoritative.");
            Assert.That(maintenanceDrone.Find("Drone Chassis").GetComponent<Renderer>().sharedMaterial.mainTexture,
                Is.Not.Null, "The authored drone chassis should render the original ceramic Signal texture.");
            var playerMeshes = maintenanceDrone.GetComponentsInChildren<MeshFilter>().Select(filter => filter.sharedMesh).ToArray();
            Assert.That(playerMeshes.Length, Is.EqualTo(4));
            Assert.That(playerMeshes.All(mesh => mesh != null && mesh.vertexCount >= 24), Is.True,
                "Every player part should use purpose-built geometry rather than a placeholder primitive.");
            Assert.That(playerMeshes.All(mesh => mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0)), Is.True,
                "Every player mesh should retain complete authored UV coordinates.");
            Assert.That(maintenanceDrone.Find("Drone Tool").localPosition, Is.EqualTo(new Vector3(0f, 0.3f, 0.68f)),
                "The authored tool must preserve the projectile origin and aiming silhouette.");
            Assert.That(game.HasSignalBoltAssets, Is.True,
                "The authored Signal bolt prefab should be ready before the player fires.");
            var authoredBoltPrefab = Resources.Load<GameObject>("Projectiles/SignalBoltAssembly");
            var authoredBoltModel = Resources.Load<GameObject>("Projectiles/SignalBoltModel");
            var boltAlbedo = Resources.Load<Texture2D>("Projectiles/SignalBoltAlbedo");
            var boltShellMaterial = Resources.Load<Material>("Materials/SignalBoltShell");
            Assert.That(authoredBoltPrefab, Is.Not.Null);
            Assert.That(authoredBoltModel, Is.Not.Null);
            Assert.That(boltAlbedo, Is.Not.Null);
            Assert.That(boltShellMaterial.mainTexture, Is.EqualTo(boltAlbedo),
                "The projectile shell material should persistently map the generated albedo outside Play Mode.");
            Assert.That(authoredBoltPrefab.transform.Find("Bolt Shell").GetComponent<Renderer>().sharedMaterial,
                Is.EqualTo(boltShellMaterial));
            Assert.That(authoredBoltPrefab.transform.Find("Bolt Energy").GetComponent<Renderer>().sharedMaterial,
                Is.EqualTo(Resources.Load<Material>("Materials/SignalBoltEnergy")));
            var boltMeshes = authoredBoltPrefab.GetComponentsInChildren<MeshFilter>().Select(filter => filter.sharedMesh).ToArray();
            Assert.That(boltMeshes.Length, Is.EqualTo(2));
            Assert.That(boltMeshes.All(mesh => mesh != null && mesh.vertexCount >= 24), Is.True,
                "Both Signal bolt parts should use purpose-built geometry rather than a placeholder cube.");
            Assert.That(boltMeshes.All(mesh => mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0)), Is.True,
                "Both Signal bolt meshes should retain complete authored UV coordinates.");
            Assert.That(authoredBoltPrefab.GetComponentsInChildren<Collider>().Length, Is.Zero,
                "The authored projectile should remain presentation-only so deterministic hit rules stay authoritative.");
            var boltTrailTexture = Resources.Load<Texture2D>("Projectiles/SignalBoltTrail");
            var boltTrailMaterial = Resources.Load<Material>("Materials/SignalBoltTrail");
            var boltTrailTuning = Resources.Load<SignalBoltPresentationTuning>("Tuning/SignalBoltPresentationTuning");
            var boltTrail = authoredBoltPrefab.GetComponent<TrailRenderer>();
            Assert.That(boltTrailTexture, Is.Not.Null);
            Assert.That(boltTrailMaterial, Is.Not.Null);
            Assert.That(boltTrailTuning, Is.Not.Null);
            Assert.That(boltTrail, Is.Not.Null, "The authored projectile prefab should persist its directional afterimage.");
            Assert.That(boltTrail.sharedMaterial, Is.EqualTo(boltTrailMaterial));
            Assert.That(boltTrailMaterial.mainTexture, Is.EqualTo(boltTrailTexture));
            Assert.That(boltTrail.time, Is.EqualTo(boltTrailTuning.TrailDuration));
            Assert.That(boltTrail.startWidth, Is.EqualTo(boltTrailTuning.StartingWidth));
            Assert.That(boltTrail.endWidth, Is.EqualTo(boltTrailTuning.EndingWidth));
            Assert.That(boltTrail.textureMode, Is.EqualTo(LineTextureMode.Stretch));
            Assert.That(boltTrail.shadowCastingMode, Is.EqualTo(UnityEngine.Rendering.ShadowCastingMode.Off));
            var bulkheadImpactTexture = Resources.Load<Texture2D>("Projectiles/SignalBoltBulkheadImpact");
            var bulkheadImpactMaterial = Resources.Load<Material>("Materials/SignalBoltBulkheadImpact");
            Assert.That(bulkheadImpactTexture, Is.Not.Null);
            Assert.That(bulkheadImpactMaterial, Is.Not.Null);
            Assert.That(bulkheadImpactMaterial.mainTexture, Is.EqualTo(bulkheadImpactTexture));
            Assert.That(game.HasSignalBoltBulkheadImpact, Is.True);
            var securityWarden = game.transform.Find("Security Warden");
            var authoredWardenPrefab = Resources.Load<GameObject>("Actors/SecurityWardenAssembly");
            Assert.That(authoredWardenPrefab, Is.Not.Null,
                "The authored Security Warden prefab should load from Resources.");
            Assert.That(Resources.Load<GameObject>("Actors/SecurityWardenModel"), Is.Not.Null,
                "The authored Blender Security Warden model should load from Resources.");
            var wardenArmorTexture = Resources.Load<Texture2D>("Actors/SecurityWardenArmorAlbedo");
            var wardenArmorMaterial = Resources.Load<Material>("Materials/SecurityWardenArmor");
            Assert.That(wardenArmorTexture, Is.Not.Null,
                "The original Security Warden armor texture should load from Resources.");
            Assert.That(wardenArmorMaterial, Is.Not.Null);
            Assert.That(wardenArmorMaterial.mainTexture, Is.EqualTo(wardenArmorTexture),
                "The authored Warden armor material should persistently map the generated albedo outside Play Mode.");
            Assert.That(authoredWardenPrefab.transform.Find("Warden Chassis").GetComponent<Renderer>().sharedMaterial,
                Is.EqualTo(wardenArmorMaterial), "Prefab Mode should display the mapped Warden armor material.");
            Assert.That(authoredWardenPrefab.transform.Find("Warden Eye").GetComponent<Renderer>().sharedMaterial,
                Is.EqualTo(Resources.Load<Material>("Materials/SecurityWardenEye")));
            Assert.That(authoredWardenPrefab.transform.Find("Warden Crown").GetComponent<Renderer>().sharedMaterial,
                Is.EqualTo(Resources.Load<Material>("Materials/SecurityWardenCrown")));
            Assert.That(securityWarden, Is.Not.Null, "Dormant security should exist before tower activation.");
            Assert.That(securityWarden.GetComponentsInChildren<Renderer>(true).Length, Is.EqualTo(3));
            var wardenMeshes = securityWarden.GetComponentsInChildren<MeshFilter>(true).Select(filter => filter.sharedMesh).ToArray();
            Assert.That(wardenMeshes.Length, Is.EqualTo(3));
            Assert.That(wardenMeshes.All(mesh => mesh != null && mesh.vertexCount >= 24), Is.True,
                "Every Warden part should use purpose-built geometry rather than a placeholder primitive.");
            Assert.That(wardenMeshes.All(mesh => mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0)), Is.True,
                "Every Warden mesh should retain complete authored UV coordinates.");
            Assert.That(securityWarden.GetComponentsInChildren<Collider>(true).Length, Is.Zero,
                "The authored Warden should remain presentation-only so deterministic threat collision stays authoritative.");
            Assert.That(securityWarden.Find("Warden Chassis").localPosition, Is.EqualTo(new Vector3(0f, 0.38f, 0f)));
            Assert.That(securityWarden.Find("Warden Eye").localPosition, Is.EqualTo(new Vector3(0f, 0.48f, -0.59f)));
            Assert.That(securityWarden.Find("Warden Crown").localPosition, Is.EqualTo(new Vector3(0f, 0.76f, 0f)));
            Assert.That(securityWarden.Find("Warden Chassis").GetComponent<Renderer>().sharedMaterial.mainTexture,
                Is.Not.Null, "The Warden chassis should render the original armored security texture.");
            Assert.That(securityWarden.gameObject.activeSelf, Is.False,
                "The authored Warden should remain dormant until tower activation.");
            var wardenWarning = game.transform.Find("Warden Strike Warning");
            Assert.That(wardenWarning, Is.Not.Null);
            Assert.That(wardenWarning.GetComponent<SpriteRenderer>().sprite, Is.Not.Null,
                "The Warden proximity warning should render the authored floor glyph.");
            Assert.That(game.HasWardenWarningTexture, Is.True);
            Assert.That(game.IsWardenWarningVisible, Is.False,
                "The strike warning should remain hidden while the Warden is dormant.");
            Assert.That(Resources.Load<WardenThreatTelegraphTuning>("Tuning/WardenThreatTelegraphTuning"), Is.Not.Null);
            var towerJunction = GameObject.Find("Tower Approach Junction");
            Assert.That(towerJunction, Is.Not.Null,
                "The tower approach should be placed as scene-authored prefab content rather than runtime layout code.");
            var authoredObstacles = towerJunction.GetComponentsInChildren<AuthoredMapObstacle>();
            Assert.That(authoredObstacles.Length, Is.EqualTo(3));
            Assert.That(game.AuthoredMapObstacleCount, Is.EqualTo(25),
                "Every authored junction, salvage area, departure channel, and threat-bay obstacle should participate " +
                "in movement resolution.");
            Assert.That(authoredObstacles.Sum(obstacle => obstacle.GetComponentsInChildren<Renderer>().Length), Is.EqualTo(6));
            Assert.That(authoredObstacles.Sum(obstacle => obstacle.GetComponentsInChildren<Collider>().Length), Is.Zero,
                "The non-physics movement controller should use authored obstacle bounds without duplicate physics colliders.");
            Assert.That(
                authoredObstacles.SelectMany(obstacle => obstacle.GetComponentsInChildren<Renderer>())
                    .Any(renderer => renderer.sharedMaterial.mainTexture != null),
                Is.True,
                "The tower junction should display its original coolant-manifold panel texture.");
            var salvageAnnex = GameObject.Find("Northeast Salvage Annex");
            Assert.That(salvageAnnex, Is.Not.Null,
                "The optional northeast cache should be enclosed by scene-authored prefab content.");
            Assert.That(salvageAnnex.transform.position, Is.EqualTo(new Vector3(9.7f, 0f, 6.3f)),
                "The annex should surround the established cache position without moving the objective.");
            var annexObstacles = salvageAnnex.GetComponentsInChildren<AuthoredMapObstacle>();
            Assert.That(annexObstacles.Length, Is.EqualTo(3));
            Assert.That(annexObstacles.Sum(obstacle => obstacle.GetComponentsInChildren<Renderer>().Length), Is.EqualTo(9));
            Assert.That(annexObstacles.Sum(obstacle => obstacle.GetComponentsInChildren<Collider>().Length), Is.Zero,
                "The annex should use serialized movement bounds without duplicate physics colliders.");
            var northCargoBarrier = salvageAnnex.transform.Find("North Cargo Barrier");
            Assert.That(northCargoBarrier.localPosition, Is.EqualTo(new Vector3(0.75f, 0f, 1.55f)));
            Assert.That(northCargoBarrier.localScale, Is.EqualTo(new Vector3(0.65f, 1f, 1f)),
                "The annex north wall must preserve the widened west entrance shared with the Warden bay.");
            var annexTexture = Resources.Load<Texture2D>("Environment/SalvageAnnexAlbedo");
            var annexArmor = Resources.Load<Material>("Materials/SalvageAnnexArmor");
            Assert.That(annexTexture, Is.Not.Null);
            Assert.That(annexArmor.mainTexture, Is.EqualTo(annexTexture),
                "The annex armor should persistently map its original cargo-panel albedo.");
            Assert.That(
                annexObstacles.SelectMany(obstacle => obstacle.GetComponentsInChildren<Renderer>())
                    .Count(renderer => renderer.sharedMaterial == annexArmor),
                Is.EqualTo(3));
            var eastVault = GameObject.Find("Optional East Salvage Vault");
            Assert.That(eastVault, Is.Not.Null,
                "The fourth cache should occupy a scene-authored optional room beyond the original east boundary.");
            Assert.That(eastVault.transform.position, Is.EqualTo(new Vector3(16.7f, 0f, 0f)));
            Assert.That(eastVault.transform.eulerAngles.y, Is.EqualTo(180f).Within(0.01f),
                "The imported vault must face its split doorway toward the original arena.");
            var eastVaultObstacles = eastVault.GetComponentsInChildren<AuthoredMapObstacle>();
            var eastVaultSocket = eastVault.GetComponentInChildren<AuthoredSalvageSocket>();
            Assert.That(eastVaultObstacles.Length, Is.EqualTo(6));
            Assert.That(eastVaultObstacles.All(obstacle =>
                    obstacle.RightAxis.sqrMagnitude > 0.99f && obstacle.ForwardAxis.sqrMagnitude > 0.99f), Is.True,
                "Imported render transforms must not own navigation bounds because FBX basis rotation can make an axis vertical.");
            Assert.That(eastVaultObstacles.All(obstacle =>
                    obstacle.GetComponent<Renderer>() == null && obstacle.transform.localRotation == Quaternion.identity), Is.True,
                "East-vault collision authoring should remain on identity-oriented, presentation-free transforms.");
            Assert.That(eastVaultSocket, Is.Not.Null);
            Assert.That(Vector3.Distance(eastVaultSocket.Position, new Vector3(18.7f, 0f, 0f)), Is.LessThan(0.001f));
            Assert.That(game.AuthoredSalvageSocketCount, Is.EqualTo(1));
            Assert.That(eastVault.GetComponentsInChildren<Collider>().Length, Is.Zero,
                "The optional room should use serialized object-aligned blockers without duplicate physics colliders.");
            var eastVaultMeshes = eastVault.GetComponentsInChildren<MeshFilter>()
                .Select(filter => filter.sharedMesh)
                .ToArray();
            Assert.That(eastVaultMeshes.Length, Is.EqualTo(8));
            Assert.That(eastVaultMeshes.All(mesh => mesh != null && mesh.vertexCount >= 24), Is.True,
                "Every east-vault part should use authored beveled geometry instead of a primitive placeholder.");
            Assert.That(eastVaultMeshes.All(mesh =>
                mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0)), Is.True,
                "Every east-vault mesh should retain authored UV coordinates.");
            var eastVaultTexture = Resources.Load<Texture2D>("Environment/EastSalvageVaultAlbedo");
            var eastVaultArmor = Resources.Load<Material>("Materials/EastVaultArmor");
            Assert.That(eastVaultTexture, Is.Not.Null);
            Assert.That(eastVaultArmor.mainTexture, Is.EqualTo(eastVaultTexture));
            Assert.That(eastVault.transform.Find("Vault North Wall").GetComponent<Renderer>().sharedMaterial,
                Is.EqualTo(eastVaultArmor));
            var eastVaultRouteWaypoints = new[]
            {
                new Vector3(12.5f, 0f, 0f),
                new Vector3(14.6f, 0f, 0f),
                new Vector3(15.5f, 0f, 2f),
                new Vector3(18.7f, 0f, 2f),
                new Vector3(18.7f, 0f, 0f)
            };
            var eastVaultRouteSamples = eastVaultRouteWaypoints
                .SelectMany((start, index) => index == eastVaultRouteWaypoints.Length - 1
                    ? Enumerable.Empty<Vector3>()
                    : Enumerable.Range(0, 11).Select(step =>
                        Vector3.Lerp(start, eastVaultRouteWaypoints[index + 1], step / 10f)))
                .Append(eastVaultRouteWaypoints[^1])
                .ToArray();
            var completeEastRouteObstacles = Object.FindObjectsByType<AuthoredMapObstacle>(FindObjectsSortMode.None);
            var blockedEastVaultSample = eastVaultRouteSamples.FirstOrDefault(sample =>
                completeEastRouteObstacles.Any(obstacle => obstacle.OverlapsCircle(sample, 0.48f)));
            var blockingEastVaultObstacle = completeEastRouteObstacles.FirstOrDefault(obstacle =>
                obstacle.OverlapsCircle(blockedEastVaultSample, 0.48f));
            Assert.That(blockingEastVaultObstacle, Is.Null,
                $"A continuous player-radius-clear route must pass through the east doorway, around the splitter, " +
                $"and reach the optional cache. Sample {blockedEastVaultSample} overlaps " +
                $"{blockingEastVaultObstacle?.name ?? "no named obstacle"}; center " +
                $"{blockingEastVaultObstacle?.Center}, half-size {blockingEastVaultObstacle?.ScaledHalfSize}, " +
                $"right {blockingEastVaultObstacle?.RightAxis}, forward {blockingEastVaultObstacle?.ForwardAxis}.");
            var departureChannel = GameObject.Find("Extraction Departure Channel");
            Assert.That(departureChannel, Is.Not.Null,
                "The opening route should be framed by scene-authored departure-channel content.");
            Assert.That(departureChannel.transform.position, Is.EqualTo(new Vector3(-7.2f, 0f, -4.2f)));
            Assert.That(departureChannel.transform.eulerAngles.y, Is.EqualTo(325f).Within(0.01f),
                "The channel should align with the extraction-to-tower route.");
            var departureObstacles = departureChannel.GetComponentsInChildren<AuthoredMapObstacle>();
            Assert.That(departureObstacles.Length, Is.EqualTo(2));
            Assert.That(departureObstacles.Sum(obstacle => obstacle.GetComponentsInChildren<Renderer>().Length), Is.EqualTo(6));
            Assert.That(departureObstacles.Sum(obstacle => obstacle.GetComponentsInChildren<Collider>().Length), Is.Zero);
            var departureTexture = Resources.Load<Texture2D>("Environment/DepartureCapacitorAlbedo");
            var departureArmor = Resources.Load<Material>("Materials/DepartureCapacitorArmor");
            Assert.That(departureTexture, Is.Not.Null);
            Assert.That(departureArmor.mainTexture, Is.EqualTo(departureTexture));
            var coolantGauntlet = GameObject.Find("Southeast Coolant Gauntlet");
            Assert.That(coolantGauntlet, Is.Not.Null,
                "The southeast cache should sit inside a scene-authored coolant reclamation lane.");
            Assert.That(coolantGauntlet.transform.position, Is.EqualTo(new Vector3(10.4f, 0f, -6.4f)),
                "The gauntlet should surround the established cache without moving the objective.");
            var coolantObstacles = coolantGauntlet.GetComponentsInChildren<AuthoredMapObstacle>();
            Assert.That(coolantObstacles.Length, Is.EqualTo(2));
            Assert.That(coolantObstacles.Sum(obstacle => obstacle.GetComponentsInChildren<Renderer>().Length), Is.EqualTo(8));
            Assert.That(coolantObstacles.Sum(obstacle => obstacle.GetComponentsInChildren<Collider>().Length), Is.Zero,
                "The coolant baffles should use serialized movement bounds without duplicate physics colliders.");
            var coolantMeshes = coolantObstacles
                .SelectMany(obstacle => obstacle.GetComponentsInChildren<MeshFilter>())
                .Select(filter => filter.sharedMesh)
                .ToArray();
            Assert.That(coolantMeshes.Length, Is.EqualTo(8));
            Assert.That(coolantMeshes.All(mesh => mesh != null && mesh.vertexCount >= 24), Is.True,
                "Every coolant-baffle part should use purpose-built geometry rather than a placeholder primitive.");
            Assert.That(
                coolantMeshes.All(mesh => mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0)),
                Is.True,
                "Every coolant-baffle mesh should retain authored UV coordinates.");
            Assert.That(coolantObstacles.Select(obstacle => obstacle.transform.localPosition), Is.EquivalentTo(new[]
            {
                new Vector3(-1f, 0f, 1.35f),
                new Vector3(1f, 0f, -1.35f)
            }), "The staggered baffles should preserve the authored salvage corridor.");
            var coolantTexture = Resources.Load<Texture2D>("Environment/CoolantGauntletAlbedo");
            var coolantArmor = Resources.Load<Material>("Materials/CoolantBaffleArmor");
            Assert.That(coolantTexture, Is.Not.Null);
            Assert.That(coolantArmor.mainTexture, Is.EqualTo(coolantTexture),
                "The coolant baffles should persistently map their original reclamation-yard albedo.");
            var relayFork = GameObject.Find("Northwest Relay Fork");
            Assert.That(relayFork, Is.Not.Null,
                "The northwest cache should sit beyond a scene-authored relay route fork.");
            Assert.That(relayFork.transform.position, Is.EqualTo(new Vector3(-5.8f, 0f, 7.2f)),
                "The relay fork should preserve the established northwest cache coordinate.");
            var relayObstacles = relayFork.GetComponentsInChildren<AuthoredMapObstacle>();
            Assert.That(relayObstacles.Length, Is.EqualTo(2));
            Assert.That(relayObstacles.Sum(obstacle => obstacle.GetComponentsInChildren<Renderer>().Length), Is.EqualTo(8));
            Assert.That(relayObstacles.Sum(obstacle => obstacle.GetComponentsInChildren<Collider>().Length), Is.Zero,
                "The relay banks should use serialized movement bounds without duplicate physics colliders.");
            var relayMeshes = relayObstacles
                .SelectMany(obstacle => obstacle.GetComponentsInChildren<MeshFilter>())
                .Select(filter => filter.sharedMesh)
                .ToArray();
            Assert.That(relayMeshes.Length, Is.EqualTo(8));
            Assert.That(relayMeshes.All(mesh => mesh != null && mesh.vertexCount >= 24), Is.True,
                "Every relay-bank part should use purpose-built geometry rather than a placeholder primitive.");
            Assert.That(
                relayMeshes.All(mesh => mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0)),
                Is.True,
                "Every relay-bank mesh should retain authored UV coordinates.");
            Assert.That(relayObstacles.Select(obstacle => obstacle.transform.localPosition), Is.EquivalentTo(new[]
            {
                new Vector3(-2.1f, 0f, -0.55f),
                new Vector3(2.1f, 0f, -0.55f)
            }), "The relay banks should preserve the authored central throat and outside routes.");
            Assert.That(relayFork.transform.Find("West Relay Bank").localEulerAngles.y, Is.EqualTo(22f).Within(0.01f));
            Assert.That(relayFork.transform.Find("East Relay Bank").localEulerAngles.y, Is.EqualTo(338f).Within(0.01f),
                "The relay banks should angle outward into a readable route fork.");
            var relayTexture = Resources.Load<Texture2D>("Environment/RelayForkAlbedo");
            var relayArmor = Resources.Load<Material>("Materials/RelayBankArmor");
            Assert.That(relayTexture, Is.Not.Null);
            Assert.That(relayArmor.mainTexture, Is.EqualTo(relayTexture),
                "The relay banks should persistently map their original signal-yard albedo.");
            var wardenBay = GameObject.Find("Security Warden Staging Bay");
            Assert.That(wardenBay, Is.Not.Null,
                "The dormant Warden should be foreshadowed by a scene-authored security bay.");
            Assert.That(wardenBay.transform.position, Is.EqualTo(new Vector3(6.8f, 0f, 4.7f)),
                "The bay should preserve the established Warden spawn coordinate.");
            var bayObstacles = wardenBay.GetComponentsInChildren<AuthoredMapObstacle>();
            Assert.That(bayObstacles.Length, Is.EqualTo(3));
            Assert.That(bayObstacles.Sum(obstacle => obstacle.GetComponentsInChildren<Renderer>().Length), Is.EqualTo(9));
            Assert.That(bayObstacles.Sum(obstacle => obstacle.GetComponentsInChildren<Collider>().Length), Is.Zero,
                "The security shields should use serialized movement bounds without duplicate physics colliders.");
            var bayMeshes = bayObstacles
                .SelectMany(obstacle => obstacle.GetComponentsInChildren<MeshFilter>())
                .Select(filter => filter.sharedMesh)
                .ToArray();
            Assert.That(bayMeshes.Length, Is.EqualTo(9));
            Assert.That(bayMeshes.All(mesh => mesh != null && mesh.vertexCount >= 24), Is.True,
                "Every security-shield part should use purpose-built geometry rather than a placeholder primitive.");
            Assert.That(
                bayMeshes.All(mesh => mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0)),
                Is.True,
                "Every security-shield mesh should retain authored UV coordinates.");
            Assert.That(bayObstacles.All(obstacle => !obstacle.OverlapsCircle(securityWarden.position, 0.48f)), Is.True,
                "The staging bay must not trap or overlap the dormant Warden at activation.");
            Assert.That(bayObstacles.All(obstacle => !obstacle.OverlapsCircle(new Vector3(5.1f, 0f, 4.7f), 0.48f)), Is.True,
                "The west-facing bay mouth must preserve a traversable Warden exit toward the tower.");
            var northShield = wardenBay.transform.Find("North Security Shield");
            Assert.That(northShield.localPosition, Is.EqualTo(new Vector3(-0.2f, 0f, 1.15f)));
            Assert.That(northShield.localScale, Is.EqualTo(new Vector3(0.45f, 1f, 1f)),
                "The north shield must leave a generous turning area into the neighboring salvage annex.");
            Assert.That(wardenBay.transform.Find("East Security Shield").localScale,
                Is.EqualTo(new Vector3(0.72f, 1f, 1f)),
                "The east shield must not extend into the annex entrance corridor.");
            var annexRouteWaypoints = new[]
            {
                new Vector3(5f, 0f, 6.9f),
                new Vector3(6.2f, 0f, 6.9f),
                new Vector3(7.5f, 0f, 7.15f),
                new Vector3(8.2f, 0f, 7f),
                new Vector3(8.45f, 0f, 6.7f),
                new Vector3(8.75f, 0f, 6.55f),
                new Vector3(9.2f, 0f, 6.4f),
                new Vector3(9.7f, 0f, 6.3f)
            };
            var annexRouteSamples = annexRouteWaypoints
                .SelectMany((start, index) => index == annexRouteWaypoints.Length - 1
                    ? Enumerable.Empty<Vector3>()
                    : Enumerable.Range(0, 11).Select(step =>
                        Vector3.Lerp(start, annexRouteWaypoints[index + 1], step / 10f)))
                .Append(annexRouteWaypoints[^1])
                .ToArray();
            var combinedNortheastObstacles = bayObstacles.Concat(annexObstacles).ToArray();
            Assert.That(annexRouteSamples.All(sample =>
                    combinedNortheastObstacles.All(obstacle => !obstacle.OverlapsCircle(sample, 0.48f))), Is.True,
                "A continuous player-radius-clear route must connect the main arena to the northeast salvage cache " +
                "when the Warden bay and annex obstacles are evaluated together.");
            var bypassMarkers = new[]
            {
                wardenBay.transform.Find("North Bypass Entry Marker"),
                wardenBay.transform.Find("North Bypass Exit Marker")
            };
            Assert.That(bypassMarkers.All(marker => marker != null), Is.True,
                "Two authored floor arrows should make the northern bypass readable before the player commits.");
            var routeMaterial = Resources.Load<Material>("Materials/DepartureThresholdBeacons");
            Assert.That(bypassMarkers.SelectMany(marker => marker.GetComponentsInChildren<Renderer>())
                .All(renderer => renderer.sharedMaterial == routeMaterial), Is.True,
                "Bay route arrows should use the established cyan navigation language.");
            Assert.That(bypassMarkers.Sum(marker => marker.GetComponentsInChildren<Collider>().Length), Is.Zero,
                "Route markers must remain presentation-only and never obstruct their marked path.");
            var bypassMarkerMeshes = bypassMarkers
                .SelectMany(marker => marker.GetComponentsInChildren<MeshFilter>())
                .Select(filter => filter.sharedMesh)
                .ToArray();
            Assert.That(bypassMarkerMeshes.Length, Is.EqualTo(2));
            Assert.That(bypassMarkerMeshes.All(mesh =>
                    mesh.bounds.size.x > 1.8f && mesh.bounds.size.z > 0.8f && mesh.bounds.size.y < 0.2f), Is.True,
                "Route chevrons must import as broad top-facing floor geometry rather than edge-on arrows.");
            var bayTexture = Resources.Load<Texture2D>("Environment/WardenBayAlbedo");
            var bayArmor = Resources.Load<Material>("Materials/SecurityShieldArmor");
            Assert.That(bayTexture, Is.Not.Null);
            Assert.That(bayArmor.mainTexture, Is.EqualTo(bayTexture),
                "The security shields should persistently map their original containment-bay albedo.");
            var signalSapper = game.transform.Find("Signal Sapper");
            var sapperCradle = GameObject.Find("Signal Sapper Service Cradle");
            Assert.That(sapperCradle, Is.Not.Null,
                "The dormant Sapper should be foreshadowed by a scene-authored service cradle.");
            Assert.That(sapperCradle.transform.position, Is.EqualTo(new Vector3(-10.8f, 0f, 5.7f)),
                "The cradle should preserve the established Sapper spawn coordinate.");
            var cradleObstacles = sapperCradle.GetComponentsInChildren<AuthoredMapObstacle>();
            Assert.That(cradleObstacles.Length, Is.EqualTo(2));
            Assert.That(cradleObstacles.Sum(obstacle => obstacle.GetComponentsInChildren<Renderer>().Length), Is.EqualTo(6));
            Assert.That(cradleObstacles.Sum(obstacle => obstacle.GetComponentsInChildren<Collider>().Length), Is.Zero,
                "The siphon pylons should use serialized movement bounds without duplicate physics colliders.");
            Assert.That(sapperCradle.transform.Find("North Siphon Pylon").localPosition,
                Is.EqualTo(new Vector3(0f, 0f, 1.3f)));
            Assert.That(sapperCradle.transform.Find("West Siphon Pylon").localPosition,
                Is.EqualTo(new Vector3(-1.3f, 0f, 0f)));
            Assert.That(sapperCradle.transform.Find("West Siphon Pylon").localEulerAngles.y,
                Is.EqualTo(90f).Within(0.01f), "The two pylons should form an L-shaped cradle open toward the tower.");
            var cradleMeshes = cradleObstacles
                .SelectMany(obstacle => obstacle.GetComponentsInChildren<MeshFilter>())
                .Select(filter => filter.sharedMesh)
                .ToArray();
            Assert.That(cradleMeshes.Length, Is.EqualTo(6));
            Assert.That(cradleMeshes.All(mesh => mesh != null && mesh.vertexCount >= 24), Is.True,
                "Every siphon-pylon part should use purpose-built geometry rather than placeholder primitives.");
            Assert.That(cradleMeshes.All(mesh =>
                    mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0)), Is.True,
                "Every siphon-pylon mesh should retain authored UV coordinates.");
            var cradleTexture = Resources.Load<Texture2D>("Environment/SapperCradleAlbedo");
            var cradleArmor = Resources.Load<Material>("Materials/SapperCradleArmor");
            Assert.That(cradleTexture, Is.Not.Null);
            Assert.That(cradleArmor.mainTexture, Is.EqualTo(cradleTexture),
                "The siphon pylons should persistently map their original black-violet cradle albedo.");
            Assert.That(cradleObstacles.All(obstacle => !obstacle.OverlapsCircle(signalSapper.position, 0.54f)), Is.True,
                "The service cradle must not overlap or trap the dormant Sapper.");
            var sapperEmergenceSamples = Enumerable.Range(0, 31)
                .Select(step => Vector3.Lerp(signalSapper.position, new Vector3(-8.3f, 0f, 3.5f), step / 30f))
                .ToArray();
            var completeAuthoredLayout = Object.FindObjectsByType<AuthoredMapObstacle>(FindObjectsSortMode.None);
            Assert.That(sapperEmergenceSamples.All(sample =>
                    completeAuthoredLayout.All(obstacle => !obstacle.OverlapsCircle(sample, 0.54f))), Is.True,
                "A continuous Sapper-radius-clear emergence route must leave the cradle toward the tower.");
            var authoredSapperPrefab = Resources.Load<GameObject>("Actors/SignalSapperAssembly");
            var sapperArmorTexture = Resources.Load<Texture2D>("Actors/SignalSapperArmorAlbedo");
            var sapperArmorMaterial = Resources.Load<Material>("Materials/SignalSapperArmor");
            Assert.That(authoredSapperPrefab, Is.Not.Null,
                "The authored Signal Sapper prefab should load from Resources.");
            Assert.That(Resources.Load<GameObject>("Actors/SignalSapperModel"), Is.Not.Null,
                "The authored Blender Signal Sapper model should load from Resources.");
            Assert.That(sapperArmorTexture, Is.Not.Null);
            Assert.That(sapperArmorMaterial, Is.Not.Null);
            Assert.That(sapperArmorMaterial.mainTexture, Is.EqualTo(sapperArmorTexture),
                "The Sapper armor material should persistently map the generated albedo outside Play Mode.");
            Assert.That(authoredSapperPrefab.transform.Find("Sapper Chassis").GetComponent<Renderer>().sharedMaterial,
                Is.EqualTo(sapperArmorMaterial), "Prefab Mode should display the mapped Sapper armor material.");
            Assert.That(authoredSapperPrefab.transform.Find("Sapper Fork Left").GetComponent<Renderer>().sharedMaterial,
                Is.EqualTo(Resources.Load<Material>("Materials/SignalSapperFork")));
            Assert.That(authoredSapperPrefab.transform.Find("Sapper Fork Right").GetComponent<Renderer>().sharedMaterial,
                Is.EqualTo(Resources.Load<Material>("Materials/SignalSapperFork")));
            Assert.That(authoredSapperPrefab.transform.Find("Sapper Drain Core").GetComponent<Renderer>().sharedMaterial,
                Is.EqualTo(Resources.Load<Material>("Materials/SignalSapperCore")));
            Assert.That(signalSapper, Is.Not.Null, "Dormant sapper should exist before tower activation.");
            Assert.That(game.HasSignalSapperAssets, Is.True);
            Assert.That(game.SignalSapperPartCount, Is.EqualTo(4));
            Assert.That(signalSapper.GetComponentsInChildren<Renderer>(true).Length, Is.EqualTo(4));
            var sapperMeshes = signalSapper.GetComponentsInChildren<MeshFilter>(true).Select(filter => filter.sharedMesh).ToArray();
            Assert.That(sapperMeshes.Length, Is.EqualTo(4));
            Assert.That(sapperMeshes.All(mesh => mesh != null && mesh.vertexCount >= 24), Is.True,
                "Every Sapper part should use purpose-built geometry rather than a placeholder primitive.");
            Assert.That(sapperMeshes.All(mesh => mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0)), Is.True,
                "Every Sapper mesh should retain complete authored UV coordinates.");
            Assert.That(signalSapper.GetComponentsInChildren<Collider>(true).Length, Is.Zero,
                "The authored Sapper should remain presentation-only so deterministic threat collision stays authoritative.");
            Assert.That(signalSapper.Find("Sapper Chassis").localPosition, Is.EqualTo(new Vector3(0f, 0.32f, 0f)));
            Assert.That(signalSapper.Find("Sapper Fork Left").localPosition, Is.EqualTo(new Vector3(-0.43f, 0.28f, 0.28f)));
            Assert.That(signalSapper.Find("Sapper Fork Right").localPosition, Is.EqualTo(new Vector3(0.43f, 0.28f, 0.28f)));
            Assert.That(signalSapper.Find("Sapper Drain Core").localPosition, Is.EqualTo(new Vector3(0f, 0.55f, -0.12f)));
            Assert.That(signalSapper.Find("Sapper Chassis").GetComponent<Renderer>().sharedMaterial.mainTexture, Is.Not.Null,
                "The Sapper chassis should render the original parasitic armor texture.");
            Assert.That(signalSapper.gameObject.activeSelf, Is.False);
            var telegraphRoot = game.transform.Find("Sapper Drain Telegraph");
            Assert.That(telegraphRoot, Is.Not.Null, "The Sapper telegraph should be constructed with the runtime arena.");
            var telegraph = telegraphRoot.GetComponent<SignalSapperTelegraph>();
            Assert.That(telegraphRoot.gameObject.activeSelf, Is.False, "The Sapper telegraph should remain hidden while dormant.");
            Assert.That(telegraph.IsVisible, Is.False);
            Assert.That(telegraph.HasPulseTexture, Is.True,
                "The Sapper telegraph should load its original drain-glyph texture.");
            Assert.That(telegraph.HasTetherTexture, Is.True,
                "The Sapper telegraph should load its original directional tether texture.");
            Assert.That(Resources.Load<SignalSapperTelegraphTuning>("Tuning/SignalSapperTelegraphTuning"), Is.Not.Null,
                "The Sapper telegraph should load designer-facing presentation tuning.");
            var sapperTether = telegraphRoot.Find("Sapper Target Tether").GetComponent<LineRenderer>();
            Assert.That(sapperTether.textureMode, Is.EqualTo(LineTextureMode.Tile));
            Assert.That(sapperTether.sharedMaterial.mainTexture, Is.EqualTo(Resources.Load<Texture2D>("VFX/SapperTetherFlow")),
                "The targeting tether should render the authored repeating energy-flow texture.");
            var sapperPulseFlash = telegraphRoot.Find("Sapper Pulse Flash");
            Assert.That(sapperPulseFlash, Is.Not.Null);
            Assert.That(sapperPulseFlash.GetComponent<SpriteRenderer>(), Is.Not.Null,
                "The expanding drain pulse should render the authored transparent glyph instead of a primitive cylinder.");
            Assert.That(game.transform.Find("Tower Power Territory"), Is.Not.Null);
            var extractionPad = game.transform.Find("Extraction Pad Assembly");
            Assert.That(extractionPad, Is.Not.Null, "The start and finish objective should load from the authored extraction-pad prefab.");
            Assert.That(game.HasExtractionPadAssets, Is.True,
                "The extraction-pad prefab and original docking texture should load from Resources.");
            Assert.That(game.ExtractionPadPartCount, Is.EqualTo(4));
            Assert.That(extractionPad.GetComponentsInChildren<Renderer>().Length, Is.EqualTo(4));
            Assert.That(extractionPad.GetComponentsInChildren<Collider>().Length, Is.Zero,
                "The authored extraction pad should remain presentation-only so existing interaction rules stay authoritative.");
            Assert.That(extractionPad.Find("Extraction Plinth").GetComponent<Renderer>().sharedMaterial.mainTexture, Is.Not.Null,
                "The authored extraction housing should render the original docking texture.");
            Assert.That(extractionPad.Find("Extraction Beacon"), Is.Not.Null);
            var shortcut = game.transform.Find("Shortcut Gate Assembly");
            Assert.That(shortcut, Is.Not.Null, "The optional route choice should load from the authored shortcut prefab.");
            Assert.That(game.HasShortcutGateAssets, Is.True,
                "The shortcut prefab and original powered-lock texture should load from Resources.");
            Assert.That(game.ShortcutGatePartCount, Is.EqualTo(6));
            Assert.That(shortcut.GetComponentsInChildren<Renderer>().Length, Is.EqualTo(6));
            Assert.That(shortcut.GetComponentsInChildren<Collider>().Length, Is.Zero,
                "The authored shortcut should remain presentation-only so movement rules stay authoritative.");
            Assert.That(shortcut.Find("Signal Shortcut Gate").GetComponent<Renderer>().sharedMaterial.mainTexture, Is.Not.Null,
                "The authored gate should render the original powered-lock texture.");
            var signalRouting = game.transform.Find("Tower Signal Lines");
            Assert.That(signalRouting, Is.Not.Null, "The powered network should load from the authored routing prefab.");
            Assert.That(game.HasSignalRoutingAssets, Is.True,
                "The routing prefab and original circuit texture should load from Resources.");
            Assert.That(game.SignalRoutingPartCount, Is.EqualTo(3));
            Assert.That(signalRouting.GetComponentsInChildren<Renderer>(true).Length, Is.EqualTo(3));
            Assert.That(signalRouting.GetComponentsInChildren<Collider>(true).Length, Is.Zero,
                "The authored routing should remain presentation-only so gameplay rules stay authoritative.");
            Assert.That(
                signalRouting.Find("Signal Trunk West").GetComponent<Renderer>().sharedMaterial.mainTexture,
                Is.Not.Null,
                "The authored network trunks should render the original circuit texture.");
            Assert.That(signalRouting.gameObject.activeSelf, Is.False,
                "The authored network routing should remain hidden while the tower is dormant.");
            var maintenanceDeck = game.transform.Find("Maintenance Deck Modules");
            Assert.That(maintenanceDeck, Is.Not.Null, "The arena should be assembled from authored maintenance-deck modules.");
            Assert.That(game.MaintenanceDeckModuleCount, Is.EqualTo(35));
            Assert.That(maintenanceDeck.childCount, Is.EqualTo(35));
            Assert.That(game.HasMaintenanceDeckAssets, Is.True,
                "The authored deck prefab and original plating texture should load from Resources.");
            Assert.That(maintenanceDeck.GetChild(0).GetComponent<Renderer>().sharedMaterial.mainTexture, Is.Not.Null,
                "Every deck module should render the original plating texture.");
            var roomShell = game.transform.Find("Maintenance Room Shell");
            Assert.That(roomShell, Is.Not.Null, "The arena perimeter should load from the authored room-shell prefab.");
            Assert.That(game.HasMaintenanceRoomShellAssets, Is.True,
                "The room-shell prefab and original bulkhead texture should load from Resources.");
            Assert.That(game.RoomShellBulkheadCount, Is.EqualTo(5));
            Assert.That(game.MachineSocketCount, Is.EqualTo(6));
            Assert.That(roomShell.Find("Bulkheads/East Bulkhead"), Is.Null,
                "The former solid east wall must not visually seal the optional room route.");
            Assert.That(roomShell.Find("Bulkheads/East Bulkhead North"), Is.Not.Null);
            Assert.That(roomShell.Find("Bulkheads/East Bulkhead South"), Is.Not.Null);
            Assert.That(roomShell.GetComponentsInChildren<AuthoredMapObstacle>().Length, Is.EqualTo(2),
                "The widened arena must not allow movement through either visible side of the east doorway.");
            Assert.That(roomShell.Find("Bulkheads").GetChild(0).GetComponent<Renderer>().sharedMaterial.mainTexture, Is.Not.Null,
                "Every authored bulkhead should render the original wall texture.");
            var stationMachines = game.transform.Find("Station Machines");
            Assert.That(stationMachines, Is.Not.Null, "The room sockets should be populated from the authored station-machine prefab.");
            Assert.That(game.HasStationMachineAssets, Is.True,
                "The station-machine prefab and original console texture should load from Resources.");
            Assert.That(game.StationMachineInstanceCount, Is.EqualTo(6));
            Assert.That(game.StationMachinePartCount, Is.EqualTo(12));
            Assert.That(stationMachines.childCount, Is.EqualTo(6));
            Assert.That(stationMachines.GetComponentsInChildren<Renderer>().Length, Is.EqualTo(12));
            Assert.That(stationMachines.GetComponentsInChildren<Collider>().Length, Is.Zero,
                "The authored machines should remain presentation-only so room navigation stays unchanged.");
            Assert.That(
                stationMachines.GetChild(0).Find("Machine Housing").GetComponent<Renderer>().sharedMaterial.mainTexture,
                Is.Not.Null,
                "Every authored machine housing should render the original console texture.");
            Assert.That(
                stationMachines.GetChild(0).Find("Machine Status").GetComponent<Renderer>().sharedMaterial,
                Is.Not.EqualTo(stationMachines.GetChild(1).Find("Machine Status").GetComponent<Renderer>().sharedMaterial),
                "Adjacent station machines should retain alternating red/cyan status strips.");
            var salvageCaches = game.transform.Cast<Transform>().Where(child => child.name == "Salvage Cache").ToArray();
            Assert.That(game.HasSalvageCacheAssets, Is.True,
                "The salvage-cache prefab and original containment texture should load from Resources.");
            Assert.That(game.SalvageCacheInstanceCount, Is.EqualTo(RunModel.SalvageRequired + 1));
            Assert.That(game.SalvageCachePartCount, Is.EqualTo((RunModel.SalvageRequired + 1) * 2));
            Assert.That(salvageCaches.Length, Is.EqualTo(RunModel.SalvageRequired + 1));
            Assert.That(salvageCaches.Sum(cache => cache.GetComponentsInChildren<Renderer>().Length),
                Is.EqualTo((RunModel.SalvageRequired + 1) * 2));
            Assert.That(game.HasSalvagePresentationTuning, Is.True);
            Assert.That(Resources.Load<SalvagePresentationTuning>("Tuning/SalvagePresentationTuning"), Is.Not.Null);
            Assert.That(salvageCaches.Sum(cache => cache.GetComponentsInChildren<Collider>().Length), Is.Zero,
                "The authored salvage caches should remain presentation-only so collection rules stay authoritative.");
            Assert.That(salvageCaches[0].Find("Salvage Case").GetComponent<Renderer>().sharedMaterial.mainTexture,
                Is.Not.Null, "Every authored salvage case should render the original containment texture.");
            var signalTower = game.transform.Find("Signal Tower Assembly");
            Assert.That(signalTower, Is.Not.Null, "The central objective should load from the authored Signal-tower prefab.");
            Assert.That(game.HasSignalTowerAssets, Is.True,
                "The Signal-tower prefab and original housing texture should load from Resources.");
            Assert.That(game.SignalTowerPartCount, Is.EqualTo(3));
            Assert.That(signalTower.GetComponentsInChildren<Renderer>().Length, Is.EqualTo(3));
            Assert.That(signalTower.GetComponentsInChildren<Collider>().Length, Is.Zero,
                "The authored tower should remain presentation-only so existing interaction rules stay authoritative.");
            Assert.That(signalTower.Find("Tower Base").GetComponent<Renderer>().sharedMaterial.mainTexture, Is.Not.Null,
                "The authored tower housing should render the original control-panel texture.");
            Assert.That(Camera.main != null || Object.FindFirstObjectByType<Camera>() != null, Is.True);
            Assert.That(game.HasPlayerCameraTuning, Is.True,
                "The authored tactical-camera tuning should load from Resources.");
            Assert.That(game.IsPlayerCameraFollowing, Is.True,
                "The player camera should be configured on its independent follow rig.");
            Assert.That(Object.FindFirstObjectByType<AudioListener>(), Is.Not.Null,
                "The runtime camera should provide the listener required by the synthesized soundscape.");
            Assert.That(game.HasPauseInsignia, Is.True, "The generated pause-menu insignia should load from Resources.");
            Assert.That(game.HasCameraComfortIcon, Is.True, "The generated Steady Camera icon should load from Resources.");
            Assert.That(game.HasReducedFlashesIcon, Is.True, "The generated Reduced Flashes icon should load from Resources.");
            Assert.That(game.HasHighContrastIcon, Is.True, "The generated High Contrast icon should load from Resources.");
            Assert.That(game.HasObjectiveBeaconIcon, Is.True, "The generated objective beacon icon should load from Resources.");
            Assert.That(game.HasInputLinkIcon, Is.True, "The generated input-link icon should load from Resources.");
            Assert.That(game.HasAudioLinkIcon, Is.True, "The generated audio-link icon should load from Resources.");
            Assert.That(game.HasBindingMatrixIcon, Is.True,
                "The generated control-routing icon should load from Resources.");
            Assert.That(game.HasBindingConflictIcon, Is.True,
                "The generated binding-conflict icon should load from Resources.");
            Assert.That(game.FireKeyboardBinding, Is.Not.Empty);
            Assert.That(game.InteractKeyboardBinding, Is.Not.Empty);
            Assert.That(game.HasGeneratedAudio, Is.True, "The runtime audio service should synthesize ambience and cue clips.");
            Assert.That(Object.FindFirstObjectByType<SignalDustController>(), Is.Not.Null,
                "Reflex composition should provide a dedicated ambient Signal-dust presenter.");
            Assert.That(game.HasSignalDustTexture, Is.True, "The original Signal-dust texture should load from Resources.");
            Assert.That(game.HasLowSignalWarningTexture, Is.True,
                "The low-Signal presenter should load its original warning vignette from Resources.");
            Assert.That(Object.FindFirstObjectByType<TowerActivationSweepController>(), Is.Not.Null,
                "Reflex composition should provide a dedicated tower-activation presenter.");
            Assert.That(game.HasTowerActivationSweepTexture, Is.True,
                "The tower-activation presenter should load its original circuit-ring texture from Resources.");
            Assert.That(game.IsTowerActivationSweepPlaying, Is.False,
                "The activation sweep should remain hidden while the tower is dormant.");
            Assert.That(game.LowSignalWarningIntensity, Is.Zero,
                "The emergency vignette should stay hidden while the starting Signal reserve is safe.");
            Assert.That(game.SignalDustMaximumParticles, Is.EqualTo(56), "Ambient particles should stay within their fixed budget.");
            Assert.That(game.IsSignalDustPowered, Is.True, "The extraction dock should begin with powered Signal dust.");
            Assert.That(game.transform.Find("Adaptive Signal Dust Field"), Is.Not.Null);
            Assert.That(game.ActiveInputPromptDevice, Is.EqualTo(InputPromptDevice.KeyboardMouse),
                "A fresh run should begin with keyboard-and-mouse guidance until controller input is received.");
            Assert.That(game.CurrentObjectiveBeaconPhase, Is.EqualTo(ObjectiveBeaconPhase.Tower));
            Assert.That(game.CurrentObjectiveBeaconTarget, Is.EqualTo(new Vector3(-0.6f, 0f, 0.4f)));
            var combatFeedback = Object.FindFirstObjectByType<CombatFeedbackController>();
            Assert.That(combatFeedback, Is.Not.Null, "Reflex composition should provide the combat-feedback controller.");
            Assert.That(combatFeedback.HasImpactTexture, Is.True, "The generated impact texture should load from Resources.");
            var audio = Object.FindFirstObjectByType<DeadSignalAudio>();
            Assert.That(audio, Is.Not.Null, "Reflex composition should provide the adaptive audio controller.");
            Assert.That(audio.HasGeneratedClips, Is.True);

            var gamepad = InputSystem.AddDevice<Gamepad>();
            var player = game.transform.Find("Maintenance Drone");
            var startingPosition = player.position;
            var hadCameraImpulsePreference = PlayerPrefs.HasKey("DeadSignal.CameraImpulseEnabled");
            var initialCameraImpulse = game.IsCameraImpulseEnabled;
            var hadReducedFlashesPreference = PlayerPrefs.HasKey("DeadSignal.ReducedFlashesEnabled");
            var initialReducedFlashes = game.IsReducedFlashesEnabled;
            var hadHighContrastPreference = PlayerPrefs.HasKey("DeadSignal.HighContrastEnabled");
            var initialHighContrast = game.IsHighContrastEnabled;
            var hadAudioPreference = PlayerPrefs.HasKey("DeadSignal.AudioEnabled");
            var initialAudio = game.IsAudioEnabled;
            try
            {
                var signalBeforePause = game.CurrentSignal;
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.Start));
                yield return null;

                Assert.That(game.IsPaused, Is.True, "Gamepad Menu should pause a running game.");
                Assert.That(game.ActiveInputPromptDevice, Is.EqualTo(InputPromptDevice.Gamepad),
                    "A meaningful controller action should immediately switch every adaptive prompt to gamepad guidance.");
                Assert.That(Time.timeScale, Is.Zero);
                yield return new WaitForSecondsRealtime(0.1f);
                Assert.That(game.CurrentSignal, Is.EqualTo(signalBeforePause), "Signal must not drain while paused.");

                var signalDust = game.transform.Find("Adaptive Signal Dust Field").GetComponent<ParticleSystem>();
                Assert.That(signalDust.isPaused, Is.True, "Time-scale pause should also freeze ambient Signal dust.");

                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.DpadLeft));
                yield return null;
                Assert.That(game.IsAudioEnabled, Is.EqualTo(!initialAudio),
                    "Gamepad d-pad left should toggle Signal Audio while paused.");
                Assert.That(audio.AudioEnabled, Is.EqualTo(game.IsAudioEnabled),
                    "The Reflex-composed audio service should share the persisted presentation setting.");
                Assert.That(PlayerPrefs.GetInt("DeadSignal.AudioEnabled", -1), Is.EqualTo(game.IsAudioEnabled ? 1 : 0),
                    "The audio choice should persist for future runs.");

                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                yield return null;
                game.ToggleAudio();
                Assert.That(game.IsAudioEnabled, Is.EqualTo(initialAudio),
                    "The pause option should restore its prior audio state.");
                if (!game.IsAudioEnabled)
                {
                    game.ToggleAudio();
                }

                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.North));
                yield return null;
                Assert.That(game.IsCameraImpulseEnabled, Is.EqualTo(!initialCameraImpulse),
                    "Gamepad north should toggle the Steady Camera option while paused.");
                Assert.That(combatFeedback.CameraImpulseEnabled, Is.EqualTo(game.IsCameraImpulseEnabled),
                    "Reflex-composed combat feedback should share the comfort setting.");
                Assert.That(PlayerPrefs.GetInt("DeadSignal.CameraImpulseEnabled", -1),
                    Is.EqualTo(game.IsCameraImpulseEnabled ? 1 : 0), "The comfort choice should persist for future runs.");

                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                yield return null;
                game.ToggleCameraImpulse();
                Assert.That(game.IsCameraImpulseEnabled, Is.EqualTo(initialCameraImpulse),
                    "The pause option should restore its prior camera-impulse state.");

                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.DpadDown));
                yield return null;
                Assert.That(game.IsReducedFlashesEnabled, Is.EqualTo(!initialReducedFlashes),
                    "Gamepad d-pad down should toggle Reduced Flashes while paused.");
                Assert.That(combatFeedback.ReducedFlashesEnabled, Is.EqualTo(game.IsReducedFlashesEnabled),
                    "Reflex-composed combat feedback should share the reduced-flashes setting.");
                Assert.That(PlayerPrefs.GetInt("DeadSignal.ReducedFlashesEnabled", -1),
                    Is.EqualTo(game.IsReducedFlashesEnabled ? 1 : 0), "The reduced-flashes choice should persist for future runs.");

                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                yield return null;
                game.ToggleReducedFlashes();
                Assert.That(game.IsReducedFlashesEnabled, Is.EqualTo(initialReducedFlashes),
                    "The pause option should restore its prior reduced-flashes state.");

                var salvageCase = game.transform.Find("Salvage Cache/Salvage Case");
                Assert.That(salvageCase, Is.Not.Null);
                var initialSalvageColor = salvageCase.GetComponent<Renderer>().sharedMaterial.color;
                var machineHousing = stationMachines.GetChild(0).Find("Machine Housing").GetComponent<Renderer>();
                var initialMachineHousingColor = machineHousing.sharedMaterial.color;
                var playerHousing = player.Find("Drone Chassis").GetComponent<Renderer>();
                var initialPlayerHousingColor = playerHousing.sharedMaterial.color;
                var wardenHousing = securityWarden.Find("Warden Chassis").GetComponent<Renderer>();
                var initialWardenHousingColor = wardenHousing.sharedMaterial.color;
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.DpadUp));
                yield return null;
                Assert.That(game.IsHighContrastEnabled, Is.EqualTo(!initialHighContrast),
                    "Gamepad d-pad up should toggle High Contrast while paused.");
                Assert.That(PlayerPrefs.GetInt("DeadSignal.HighContrastEnabled", -1),
                    Is.EqualTo(game.IsHighContrastEnabled ? 1 : 0), "The high-contrast choice should persist for future runs.");
                Assert.That(salvageCase.GetComponent<Renderer>().sharedMaterial.color, Is.Not.EqualTo(initialSalvageColor),
                    "High Contrast should immediately remap shared world materials while paused.");
                Assert.That(machineHousing.sharedMaterial.color, Is.Not.EqualTo(initialMachineHousingColor),
                    "High Contrast should immediately remap the authored machine housing material.");
                Assert.That(playerHousing.sharedMaterial.color, Is.Not.EqualTo(initialPlayerHousingColor),
                    "High Contrast should immediately remap the authored player housing material.");
                Assert.That(wardenHousing.sharedMaterial.color, Is.Not.EqualTo(initialWardenHousingColor),
                    "High Contrast should immediately remap the authored Warden housing material.");

                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                yield return null;
                game.ToggleHighContrast();
                Assert.That(game.IsHighContrastEnabled, Is.EqualTo(initialHighContrast),
                    "The pause option should restore its prior high-contrast state.");

                if (!game.IsHighContrastEnabled)
                {
                    game.ToggleHighContrast();
                }

                Assert.That(game.IsHighContrastEnabled, Is.True,
                    "High Contrast should remain active for restart-persistence validation.");

                if (!game.IsReducedFlashesEnabled)
                {
                    game.ToggleReducedFlashes();
                }

                Assert.That(game.IsReducedFlashesEnabled, Is.True,
                    "Reduced Flashes should be active for presentation validation.");

                if (game.IsCameraImpulseEnabled)
                {
                    game.ToggleCameraImpulse();
                }

                Assert.That(game.IsCameraImpulseEnabled, Is.False,
                    "The camera-impulse suppression path should be active for feedback validation.");

                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.Start));
                yield return null;
                Assert.That(game.IsPaused, Is.False, "Gamepad Menu should resume a paused game.");
                Assert.That(Time.timeScale, Is.EqualTo(1f));
                Assert.That(signalDust.isPlaying, Is.True, "Ambient Signal dust should resume with the run.");

                player.position = new Vector3(7.5f, 0f, -4f);
                yield return null;
                Assert.That(game.IsSignalDustPowered, Is.False, "Dead zones should switch the ambient field to its sparse state.");
                var deadDustRate = game.SignalDustEmissionRate;
                player.position = startingPosition;
                yield return null;
                Assert.That(game.IsSignalDustPowered, Is.True);
                Assert.That(game.SignalDustEmissionRate, Is.GreaterThan(deadDustRate),
                    "Powered territory should carry a visibly denser Signal-dust field.");

                var gameCamera = game.GetComponentInChildren<Camera>();
                var cameraRestPosition = gameCamera.transform.position;
                combatFeedback.PlaySignalImpact(player.position + Vector3.up * 0.5f, false);
                Assert.That(combatFeedback.ActiveImpactCount, Is.EqualTo(1));
                var impactBurst = combatFeedback.transform.Find("Combat Impact Burst");
                Assert.That(impactBurst, Is.Not.Null);
                Assert.That(impactBurst.GetComponent<SpriteRenderer>().sprite, Is.Not.Null);
                Assert.That(impactBurst.GetComponent<SpriteRenderer>().color.a, Is.LessThanOrEqualTo(0.3f),
                    "Reduced Flashes should cap combat-burst opacity without removing the hit confirmation.");
                var cameraFacingDirection = -game.GetComponentInChildren<Camera>().transform.forward;
                Assert.That(Vector3.Dot(impactBurst.forward, cameraFacingDirection), Is.GreaterThan(0.99f),
                    "The impact sprite should face the overhead camera.");
                Assert.That(combatFeedback.IsHitStopped, Is.True, "A combat impact should begin a brief hit-stop.");
                Assert.That(Time.timeScale, Is.Zero);
                yield return new WaitForSecondsRealtime(0.08f);
                Assert.That(combatFeedback.IsHitStopped, Is.False, "Hit-stop should end using real time.");
                Assert.That(Time.timeScale, Is.EqualTo(1f));
                Assert.That(gameCamera.transform.position, Is.EqualTo(cameraRestPosition),
                    "Steady Camera should suppress camera impulse without removing hit-stop or impact art.");
                Assert.That(combatFeedback.ActiveImpactCount, Is.EqualTo(1),
                    "The burst should remain long enough to read after hit-stop ends.");
                yield return new WaitForSeconds(0.3f);
                Assert.That(combatFeedback.ActiveImpactCount, Is.Zero, "Finished impact bursts should clean themselves up.");

                combatFeedback.PlayEnvironmentImpact(player.position + Vector3.up * 0.4f);
                var bulkheadImpact = combatFeedback.transform.Find("Bulkhead Signal Impact");
                Assert.That(bulkheadImpact, Is.Not.Null);
                Assert.That(bulkheadImpact.GetComponent<SpriteRenderer>().sharedMaterial,
                    Is.EqualTo(Resources.Load<Material>("Materials/SignalBoltBulkheadImpact")));
                Assert.That(combatFeedback.IsHitStopped, Is.False,
                    "Cover impacts should confirm blocked shots without interrupting player control.");
                yield return new WaitForSeconds(0.3f);
                Assert.That(combatFeedback.ActiveImpactCount, Is.Zero,
                    "The generated bulkhead-impact flash should clean itself up.");

                InputSystem.QueueStateEvent(gamepad, new GamepadState
                {
                    leftStick = Vector2.right,
                    rightStick = Vector2.up
                });
                yield return null;

                Assert.That(player.position.x, Is.GreaterThan(startingPosition.x), "Left stick should move the drone.");
                Assert.That(Vector3.Dot(player.forward, Vector3.forward), Is.GreaterThan(0.9f), "Right stick should aim the drone.");

                player.position = new Vector3(-0.6f, 0f, 0.4f);
                var cuesBeforeTower = audio.PlayedCueCount;
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.West));
                yield return null;

                Assert.That(game.transform.Find("Security Warden").gameObject.activeSelf, Is.True,
                    "Gamepad west button should activate the nearby tower and awaken security.");
                Assert.That(signalRouting.gameObject.activeSelf, Is.True,
                    "Tower activation should reveal the authored Signal-routing assembly.");
                Assert.That(game.IsTowerActivationSweepPlaying, Is.True,
                    "Tower activation should launch one visible network-expansion sweep.");
                Assert.That(game.transform.Find("Tower Network Activation Sweep"), Is.Not.Null);
                Assert.That(game.TowerActivationSweepAlpha, Is.LessThanOrEqualTo(0.28f),
                    "Reduced Flashes should cap the activation sweep without removing it.");
                var initialSweepDiameter = game.TowerActivationSweepDiameter;
                yield return new WaitForSeconds(0.12f);
                Assert.That(game.TowerActivationSweepDiameter, Is.GreaterThan(initialSweepDiameter),
                    "The activation sweep should expand from the tower toward the powered boundary.");
                Assert.That(game.TowerActivationSweepAlpha, Is.GreaterThan(0f).And.LessThanOrEqualTo(0.28f),
                    "Reduced Flashes should preserve a visible activation cue within its opacity cap.");
                Assert.That(game.TowerActivationSweepDiameter, Is.LessThanOrEqualTo(game.TowerActivationSweepMaximumDiameter));
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.Start));
                yield return null;
                Assert.That(game.IsPaused, Is.True);
                var pausedSweepDiameter = game.TowerActivationSweepDiameter;
                yield return new WaitForSecondsRealtime(0.08f);
                Assert.That(game.TowerActivationSweepDiameter, Is.EqualTo(pausedSweepDiameter),
                    "Pause should freeze the activation sweep with the rest of the run.");
                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.Start));
                yield return null;
                Assert.That(game.IsPaused, Is.False);
                Assert.That(audio.PlayedCueCount, Is.GreaterThan(cuesBeforeTower),
                    "Tower activation should produce an audible state-change cue when audio is enabled.");
                Assert.That(audio.PoweredVolume, Is.GreaterThan(0f),
                    "The powered network layer should remain active after the tower comes online.");
                securityWarden.position = player.position + Vector3.right * 2f;
                yield return null;
                Assert.That(game.IsWardenWarningVisible, Is.True,
                    "The authored strike warning should appear before the Warden reaches contact range.");
                Assert.That(game.IsWardenWarningMotionSuppressed, Is.True,
                    "Reduced Flashes should retain the warning while suppressing its rotation and scale pulse.");
                securityWarden.position = new Vector3(6.8f, 0f, 4.7f);
                yield return null;
                var sapper = game.transform.Find("Signal Sapper");
                Assert.That(sapper.gameObject.activeSelf, Is.True,
                    "Tower activation should awaken the Signal Sapper.");
                Assert.That(telegraphRoot.gameObject.activeSelf, Is.True,
                    "Tower activation should reveal the Sapper-to-tower telegraph.");
                Assert.That(telegraph.IsVisible, Is.True);
                Assert.That(telegraph.IsLatched, Is.False);
                Assert.That(telegraphRoot.Find("Sapper Target Tether"), Is.Not.Null);
                var tetherOffsetBeforeAnimation = telegraph.TetherTextureOffset;
                yield return null;
                Assert.That(telegraph.TetherTextureOffset, Is.LessThan(tetherOffsetBeforeAnimation),
                    "The tether texture should animate from the Sapper toward its tower target.");

                Assert.That(game.CurrentObjectiveBeaconPhase, Is.EqualTo(ObjectiveBeaconPhase.Salvage),
                    "Tower activation should advance guidance to the nearest live salvage cache.");
                var salvageTarget = game.CurrentObjectiveBeaconTarget;
                Assert.That(new Vector2(salvageTarget.x, salvageTarget.z), Is.EqualTo(new Vector2(-5.8f, 7.2f)),
                    "The beacon should select the closest remaining cache from the tower.");

                var signalBeforePulse = game.CurrentSignal;
                sapper.position = new Vector3(-0.6f, 0f, 0.4f);
                yield return null;

                Assert.That(game.IsSapperLatched, Is.True, "The sapper should latch onto the powered tower.");
                Assert.That(telegraph.IsLatched, Is.True, "The telegraph should switch to its countdown presentation when latched.");
                Assert.That(sapper.Find("Sapper Drain Core").localScale.y, Is.GreaterThan(0.5f),
                    "The drain pulse should preserve the authored core thickness instead of applying primitive-only scale values.");
                var initialCountdown = telegraph.DisplayedCountdown;
                Assert.That(initialCountdown, Is.GreaterThan(1f));
                yield return new WaitForSeconds(0.2f);
                Assert.That(telegraph.DisplayedCountdown, Is.LessThan(initialCountdown),
                    "The displayed drain countdown should decrease with the gameplay pulse timer.");

                var pulseTimeout = 2f;
                while (game.CurrentSignal > signalBeforePulse - RunModel.SapperPulseCost && pulseTimeout > 0f)
                {
                    pulseTimeout -= Time.deltaTime;
                    yield return null;
                }

                Assert.That(game.CurrentSignal, Is.LessThanOrEqualTo(signalBeforePulse - RunModel.SapperPulseCost),
                    "A latched sapper should pulse-drain Signal until destroyed.");
                Assert.That(telegraph.PulseFlashVisible, Is.False,
                    "Reduced Flashes should suppress the expanding floor flash while preserving the countdown.");

                yield return new WaitForSecondsRealtime(0.08f);
                player.position = new Vector3(2.5f, 0f, 0.4f);
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.West));
                yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                yield return null;
                Assert.That(game.transform.Find("Shortcut Gate Assembly/Signal Shortcut Gate").gameObject.activeSelf, Is.False,
                    "The powered shortcut should retract before validating the unobstructed combat lane.");

                sapper.position = new Vector3(5f, 0f, 0.4f);
                InputSystem.QueueStateEvent(gamepad,
                    new GamepadState { rightStick = Vector2.right }.WithButton(GamepadButton.RightShoulder));
                yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState { rightStick = Vector2.right });
                var boltSpawnTimeout = 0.4f;
                while (!game.LastSignalBoltUsedAuthoredPrefab && boltSpawnTimeout > 0f)
                {
                    boltSpawnTimeout -= Time.unscaledDeltaTime;
                    yield return null;
                }

                Assert.That(game.LastSignalBoltUsedAuthoredPrefab, Is.True,
                    "Firing should instantiate the authored Signal bolt before deterministic hit processing.");
                yield return new WaitForSeconds(0.22f);
                Assert.That(game.LastSignalBoltBlockedByEnvironment, Is.False,
                    "The open shortcut doorway should leave the combat line unobstructed.");
                Assert.That(game.SapperHealth, Is.EqualTo(1f),
                    "The first unobstructed Signal bolt should damage the Sapper exactly once.");
                InputSystem.QueueStateEvent(gamepad,
                    new GamepadState { rightStick = Vector2.right }.WithButton(GamepadButton.RightShoulder));
                yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState { rightStick = Vector2.right });
                yield return new WaitForSeconds(0.22f);

                Assert.That(sapper.gameObject.activeSelf, Is.False,
                    "Two Signal bolts should purge the two-health Sapper.");
                Assert.That(telegraphRoot.gameObject.activeSelf, Is.False,
                    "Purging the Sapper should immediately hide every telegraph element.");
                Assert.That(telegraph.IsVisible, Is.False);

                var cachesToSecure = game.transform.Cast<Transform>()
                    .Where(child => child.name == "Salvage Cache" && child.gameObject.activeSelf)
                    .Take(RunModel.SalvageRequired)
                    .ToArray();
                foreach (var child in cachesToSecure)
                {
                    player.position = child.position;
                    yield return null;
                }

                yield return null;
                Assert.That(game.CurrentObjectiveBeaconPhase, Is.EqualTo(ObjectiveBeaconPhase.Extraction),
                    "Securing any three of four caches should advance guidance to extraction.");
                Assert.That(game.transform.Cast<Transform>().Count(child =>
                        child.name == "Salvage Cache" && child.gameObject.activeSelf), Is.EqualTo(1),
                    "One optional cache should remain available after the extraction requirement is met.");
                Assert.That(game.CurrentObjectiveBeaconTarget, Is.EqualTo(new Vector3(-9.2f, 0f, -5.6f)));

                player.position = new Vector3(3f, 0f, 0.4f);
                InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.right });
                for (int frame = 0; frame < 30; frame++)
                {
                    yield return null;
                }

                Assert.That(player.position.x, Is.GreaterThan(4.35f),
                    "The retracted shortcut gate should allow the drone through the bulkhead.");

                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                yield return null;
                player.position = new Vector3(-9.2f, 0f, -5.6f);
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.West));
                yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                yield return null;

                var completedRunInstanceId = game.GetInstanceID();
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South));
                yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                yield return null;

                var restartedGame = Object.FindFirstObjectByType<DeadSignalGame>();
                Assert.That(restartedGame, Is.Not.Null,
                    "Restarting a completed run should bootstrap a fresh playable runtime after the scene reloads.");
                Assert.That(restartedGame.GetInstanceID(), Is.Not.EqualTo(completedRunInstanceId));
                Assert.That(restartedGame.transform.Find("Maintenance Drone"), Is.Not.Null);
                Assert.That(restartedGame.IsHighContrastEnabled, Is.True,
                    "A restarted run should apply the persisted high-contrast palette during construction.");
                Assert.That(restartedGame.IsAudioEnabled, Is.True,
                    "A restarted run should apply the persisted Signal Audio choice during construction.");
            }
            finally
            {
                Time.timeScale = 1f;
                if (hadCameraImpulsePreference)
                {
                    PlayerPrefs.SetInt("DeadSignal.CameraImpulseEnabled", initialCameraImpulse ? 1 : 0);
                }
                else
                {
                    PlayerPrefs.DeleteKey("DeadSignal.CameraImpulseEnabled");
                }

                if (hadReducedFlashesPreference)
                {
                    PlayerPrefs.SetInt("DeadSignal.ReducedFlashesEnabled", initialReducedFlashes ? 1 : 0);
                }
                else
                {
                    PlayerPrefs.DeleteKey("DeadSignal.ReducedFlashesEnabled");
                }

                if (hadHighContrastPreference)
                {
                    PlayerPrefs.SetInt("DeadSignal.HighContrastEnabled", initialHighContrast ? 1 : 0);
                }
                else
                {
                    PlayerPrefs.DeleteKey("DeadSignal.HighContrastEnabled");
                }

                if (hadAudioPreference)
                {
                    PlayerPrefs.SetInt("DeadSignal.AudioEnabled", initialAudio ? 1 : 0);
                }
                else
                {
                    PlayerPrefs.DeleteKey("DeadSignal.AudioEnabled");
                }

                PlayerPrefs.Save();
                InputSystem.RemoveDevice(gamepad);
            }
        }
    }
}

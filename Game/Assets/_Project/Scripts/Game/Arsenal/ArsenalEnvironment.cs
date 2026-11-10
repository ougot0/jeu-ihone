using System.Collections.Generic;
using Arsenal.Game.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Arsenal.Game.ArsenalScene
{
    /// <summary>
    /// Construit l'armurerie 3D provisoire (sol béton, murs à panneaux, mur de présentation perforé,
    /// piédestal central, établis, éclairage cinématographique, sonde de réflexion).
    /// Toute la géométrie est remplaçable par un décor modélisé (voir docs/03-assets.md, "ENV-01").
    /// </summary>
    public class ArsenalEnvironment
    {
        public Transform Root { get; private set; }
        public Vector3 DisplayPoint { get; private set; }
        public Vector3 CameraPoint { get; private set; }
        public List<Transform> WallMounts { get; } = new List<Transform>();

        public static readonly Color Accent = new Color(0.95f, 0.62f, 0.2f);

        private ReflectionProbe _probe;

        public static ArsenalEnvironment Build(Transform parent, int quality)
        {
            var env = new ArsenalEnvironment();
            env.Root = new GameObject("Environment").transform;
            env.Root.SetParent(parent, false);
            env.BuildRoom();
            env.BuildFurniture();
            env.BuildLights(quality);
            env.BuildAtmosphere();
            return env;
        }

        private GameObject Box(string name, Material m, Vector3 pos, Vector3 scale, Transform parent = null, Vector3? euler = null)
        {
            var go = PrimitiveMeshes.Create(name, PrimitiveType.Cube, m, parent ? parent : Root);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            return go;
        }

        private GameObject Cylinder(string name, Material m, Vector3 pos, float diameter, float height)
        {
            var go = PrimitiveMeshes.Create(name, PrimitiveType.Cylinder, m, Root);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(diameter, height * 0.5f, diameter);
            return go;
        }

        private void BuildRoom()
        {
            var floor = MaterialLibrary.Create("Env_Floor", new Color(0.32f, 0.32f, 0.33f), 0f, 0.62f, ProceduralTextures.Concrete, new Vector2(4f, 4f));
            var wall = MaterialLibrary.Create("Env_Wall", new Color(0.2f, 0.21f, 0.22f), 0.1f, 0.35f, ProceduralTextures.WallPanel, new Vector2(4f, 1.5f));
            var ceiling = MaterialLibrary.Create("Env_Ceiling", new Color(0.07f, 0.07f, 0.075f), 0f, 0.2f);
            var peg = MaterialLibrary.Create("Env_Pegboard", new Color(0.16f, 0.165f, 0.17f), 0.3f, 0.4f, ProceduralTextures.Pegboard, new Vector2(18f, 7f));
            var trim = MaterialLibrary.Create("Env_Trim", new Color(0.05f, 0.05f, 0.055f), 0.8f, 0.6f);

            Box("Floor", floor, new Vector3(0f, -0.05f, 0f), new Vector3(12f, 0.1f, 12f));
            Box("Ceiling", ceiling, new Vector3(0f, 3.65f, 0f), new Vector3(12f, 0.1f, 12f));
            Box("Wall_Back", wall, new Vector3(0f, 1.8f, 4.05f), new Vector3(12f, 3.6f, 0.1f));
            Box("Wall_Front", wall, new Vector3(0f, 1.8f, -4.05f), new Vector3(12f, 3.6f, 0.1f));
            Box("Wall_Left", wall, new Vector3(-5.05f, 1.8f, 0f), new Vector3(0.1f, 3.6f, 8f));
            Box("Wall_Right", wall, new Vector3(5.05f, 1.8f, 0f), new Vector3(0.1f, 3.6f, 8f));
            Box("Pegboard", peg, new Vector3(0f, 1.85f, 3.98f), new Vector3(6.6f, 2.5f, 0.04f));
            Box("Pegboard_FrameTop", trim, new Vector3(0f, 3.12f, 3.96f), new Vector3(6.8f, 0.05f, 0.06f));
            Box("Pegboard_FrameBottom", trim, new Vector3(0f, 0.58f, 3.96f), new Vector3(6.8f, 0.05f, 0.06f));
            Box("Baseboard_Back", trim, new Vector3(0f, 0.06f, 3.98f), new Vector3(12f, 0.12f, 0.04f));

            // Emplacements d'exposition sur le mur perforé (2 rangées x 3).
            for (int row = 0; row < 2; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    var mount = new GameObject($"WallMount_{row}_{col}").transform;
                    mount.SetParent(Root, false);
                    mount.localPosition = new Vector3(-2.1f + col * 2.1f, 2.55f - row * 1.05f, 3.88f);
                    WallMounts.Add(mount);
                    Box("Hook_L", trim, new Vector3(-0.25f, -0.12f, 0.04f), new Vector3(0.02f, 0.02f, 0.12f), mount);
                    Box("Hook_R", trim, new Vector3(0.25f, -0.12f, 0.04f), new Vector3(0.02f, 0.02f, 0.12f), mount);
                }
            }
        }

        private void BuildFurniture()
        {
            var steel = MaterialLibrary.Create("Env_Steel", new Color(0.12f, 0.125f, 0.13f), 0.85f, 0.55f, ProceduralTextures.BrushedMetal, new Vector2(2f, 2f));
            var top = MaterialLibrary.Create("Env_PedestalTop", new Color(0.03f, 0.03f, 0.032f), 0.2f, 0.85f);
            var ring = MaterialLibrary.CreateEmissive("Env_AccentRing", new Color(0.2f, 0.12f, 0.04f), Accent * 2.2f);
            var wood = MaterialLibrary.Create("Env_Workbench", new Color(0.3f, 0.18f, 0.1f), 0f, 0.45f, ProceduralTextures.WoodGrain, new Vector2(3f, 1f));
            var caseMat = MaterialLibrary.Create("Env_Case", new Color(0.06f, 0.065f, 0.07f), 0.1f, 0.4f, ProceduralTextures.FineNoise, new Vector2(2f, 2f));
            var olive = MaterialLibrary.Create("Env_Crate", new Color(0.2f, 0.23f, 0.15f), 0.05f, 0.3f, ProceduralTextures.FineNoise, new Vector2(2f, 2f));

            // Piédestal central
            Cylinder("Pedestal_Base", steel, new Vector3(0f, 0.45f, 0.4f), 0.75f, 0.9f);
            Cylinder("Pedestal_Ring", ring, new Vector3(0f, 0.905f, 0.4f), 0.8f, 0.02f);
            Cylinder("Pedestal_Top", top, new Vector3(0f, 0.93f, 0.4f), 0.78f, 0.04f);
            DisplayPoint = new Vector3(0f, 1.22f, 0.4f);
            CameraPoint = new Vector3(0f, 1.38f, -0.9f);

            // Etablis latéraux
            Box("Workbench_L", wood, new Vector3(-4.2f, 0.9f, 1.5f), new Vector3(1.2f, 0.06f, 3.2f));
            Box("Workbench_L_Legs", steel, new Vector3(-4.2f, 0.45f, 1.5f), new Vector3(1.1f, 0.85f, 0.05f));
            Box("Workbench_R", wood, new Vector3(4.2f, 0.9f, 1.5f), new Vector3(1.2f, 0.06f, 3.2f));
            Box("Workbench_R_Legs", steel, new Vector3(4.2f, 0.45f, 1.5f), new Vector3(1.1f, 0.85f, 0.05f));

            // Mallettes et caisses
            Box("Case_1", caseMat, new Vector3(-4.2f, 1.0f, 0.6f), new Vector3(0.9f, 0.14f, 0.38f), null, new Vector3(0f, 8f, 0f));
            Box("Case_2", caseMat, new Vector3(-4.15f, 1.14f, 0.62f), new Vector3(0.8f, 0.12f, 0.34f), null, new Vector3(0f, -4f, 0f));
            Box("Crate_1", olive, new Vector3(4.3f, 1.08f, 2.4f), new Vector3(0.6f, 0.3f, 0.4f));
            Box("Crate_2", olive, new Vector3(-3.6f, 0.25f, 3.3f), new Vector3(0.8f, 0.5f, 0.6f), null, new Vector3(0f, 12f, 0f));
            Box("Crate_3", olive, new Vector3(-3.55f, 0.72f, 3.3f), new Vector3(0.6f, 0.44f, 0.5f), null, new Vector3(0f, -6f, 0f));
            Box("Locker_R", steel, new Vector3(4.6f, 1.1f, -0.8f), new Vector3(0.6f, 2.2f, 1.6f));
        }

        private void BuildLights(int quality)
        {
            var stripMat = MaterialLibrary.CreateEmissive("Env_LightStrip", Color.white, new Color(3.2f, 3.1f, 2.9f));
            Box("CeilingStrip_1", stripMat, new Vector3(-1.5f, 3.58f, 0.5f), new Vector3(0.12f, 0.03f, 4f));
            Box("CeilingStrip_2", stripMat, new Vector3(1.5f, 3.58f, 0.5f), new Vector3(0.12f, 0.03f, 4f));

            // Lumière principale (clé) au-dessus du piédestal, ombres douces.
            var key = CreateLight("Light_Key", LightType.Spot, new Vector3(0.6f, 3.3f, -0.4f), DisplayPoint, new Color(1f, 0.95f, 0.88f), 9f, 6f);
            key.spotAngle = 38f;
            key.innerSpotAngle = 18f;
            key.shadows = quality >= 1 ? LightShadows.Soft : LightShadows.Hard;
            key.shadowStrength = 0.85f;

            // Contre-jour froid pour détacher la silhouette.
            var rim = CreateLight("Light_Rim", LightType.Spot, new Vector3(-1.6f, 2.6f, 2.4f), DisplayPoint, new Color(0.55f, 0.7f, 1f), 7f, 6f);
            rim.spotAngle = 40f;

            // Remplissage chaud, accent du piédestal, lavage du mur.
            CreateLight("Light_Fill", LightType.Point, new Vector3(1.4f, 1.6f, -1.2f), Vector3.zero, new Color(1f, 0.8f, 0.6f), 0.9f, 5f);
            CreateLight("Light_Accent", LightType.Point, new Vector3(0f, 0.95f, 0.4f), Vector3.zero, Accent, 0.6f, 1.6f);
            var wash = CreateLight("Light_WallWash", LightType.Spot, new Vector3(0f, 3.4f, 2.2f), new Vector3(0f, 1.6f, 4f), new Color(1f, 0.92f, 0.82f), 5f, 6f);
            wash.spotAngle = 95f;
            wash.innerSpotAngle = 40f;
        }

        private Light CreateLight(string name, LightType type, Vector3 position, Vector3 lookAt, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(Root, false);
            go.transform.localPosition = position;
            if (type != LightType.Point) go.transform.LookAt(lookAt);
            var l = go.AddComponent<Light>();
            l.type = type;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
            return l;
        }

        private void BuildAtmosphere()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.16f, 0.17f, 0.19f);
            RenderSettings.ambientEquatorColor = new Color(0.1f, 0.095f, 0.09f);
            RenderSettings.ambientGroundColor = new Color(0.04f, 0.04f, 0.04f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.05f, 0.05f, 0.055f);
            RenderSettings.fogDensity = 0.025f;

            var probeGo = new GameObject("ReflectionProbe");
            probeGo.transform.SetParent(Root, false);
            probeGo.transform.localPosition = new Vector3(0f, 1.6f, 0.3f);
            _probe = probeGo.AddComponent<ReflectionProbe>();
            _probe.mode = ReflectionProbeMode.Realtime;
            _probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            _probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            _probe.resolution = 128;
            _probe.size = new Vector3(12f, 3.8f, 8.2f);
            _probe.boxProjection = true;
            _probe.hdr = true;
            _probe.clearFlags = ReflectionProbeClearFlags.SolidColor;
            _probe.backgroundColor = new Color(0.05f, 0.05f, 0.055f);
        }

        /// <summary>Recalcule les reflets (à appeler une fois le décor et les armes en place).</summary>
        public void RefreshReflections()
        {
            if (_probe != null) _probe.RenderProbe();
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace StackIsland
{
    /// <summary>
    /// Builds the first walled beach yard in the bright tycoon style: warm tiled floor, thick coral walls with
    /// gaps, a blue-caged crab pen, and grass, round trees, palms, sand and sea around it.
    /// Also answers simple routing questions (go through the pen gate).
    /// </summary>
    public class Yard : MonoBehaviour
    {
        // yard bounds (world XZ)
        public const float MinX = -13f, MaxX = 13f, MinZ = -17f, MaxZ = 17f;
        // crab pen (top-left corner of the yard)
        public const float PenMinX = -12.6f, PenMaxX = -1.2f, PenMinZ = 4.2f, PenMaxZ = 16.6f;
        public static readonly Vector3 PenGate = new Vector3(-4.5f, 0, 4.2f);
        // customer gates in the south wall
        public const float EntryGateX = 10.2f, ExitGateX = 0.6f;

        public static readonly Color Floor = new Color(0.97f, 0.74f, 0.6f);
        public static readonly Color FloorTile = new Color(0.98f, 0.66f, 0.42f);
        public static readonly Color PenFloor = new Color(0.98f, 0.86f, 0.62f);
        public static readonly Color Sand = new Color(0.98f, 0.88f, 0.62f);
        public static readonly Color Grass = new Color(0.42f, 0.82f, 0.3f);

        public static bool InPen(Vector3 p) => p.x > PenMinX && p.x < PenMaxX && p.z > PenMinZ && p.z < PenMaxZ;

        /// <summary>Waypoints from a to b (b included). Only the pen needs a detour, through its gate.</summary>
        public static void Route(Vector3 a, Vector3 b, List<Vector3> pts)
        {
            pts.Clear();
            bool ia = InPen(a), ib = InPen(b);
            if (ia != ib)
            {
                var inside = PenGate + new Vector3(0, 0, 1.2f);
                var outside = PenGate + new Vector3(0, 0, -1.2f);
                pts.Add(ia ? inside : outside);
                pts.Add(ia ? outside : inside);
            }
            pts.Add(b);
        }

        public void Build()
        {
            BuildGround();
            BuildWalls();
            BuildSurroundings();
        }

        void BuildGround()
        {
            var sea = Visuals.Primitive(PrimitiveType.Plane, transform, new Vector3(0, -0.35f, 0), new Vector3(40, 1, 40), Mats.Lit(new Color(0.2f, 0.62f, 0.9f)), "sea");
            sea.GetComponent<Renderer>().receiveShadows = false;
            ProcArt.MeshObject("sand", transform, ProcArt.RoundedSlab(84, 90, 24, 0.3f), Mats.Lit(Sand), new Vector3(0, -0.32f, 2));
            ProcArt.MeshObject("grass", transform, ProcArt.RoundedSlab(50, 58, 12, 0.3f), Mats.Lit(Grass), new Vector3(0, -0.3f, 0)).GetComponent<Renderer>().receiveShadows = true;
            ProcArt.MeshObject("floor", transform, ProcArt.RoundedSlab(MaxX - MinX, MaxZ - MinZ, 0.6f, 0.05f), Mats.Lit(Floor), Vector3.zero).GetComponent<Renderer>().receiveShadows = true;
            // scattered darker tiles, like a sun-baked tiled floor
            var rng = new System.Random(3);
            var tile = Mats.Lit(FloorTile);
            for (int i = 0; i < 70; i++)
            {
                var p = new Vector3(Mathf.Round(Mathf.Lerp(MinX + 1, MaxX - 1, (float)rng.NextDouble()) / 0.7f) * 0.7f, 0.055f,
                                    Mathf.Round(Mathf.Lerp(MinZ + 1, MaxZ - 1, (float)rng.NextDouble()) / 0.7f) * 0.7f);
                if (InPen(p)) continue;
                ProcArt.MeshObject("tile", transform, ProcArt.RoundedRect(0.62f, 0.62f, 0.08f), tile, p).GetComponent<Renderer>().receiveShadows = true;
                if (rng.NextDouble() < 0.5)
                    ProcArt.MeshObject("tile", transform, ProcArt.RoundedRect(0.62f, 0.62f, 0.08f), tile, p + new Vector3(0.7f, 0, rng.NextDouble() < 0.5 ? 0.7f : 0)).GetComponent<Renderer>().receiveShadows = true;
            }
            var pen = ProcArt.MeshObject("penFloor", transform, ProcArt.RoundedSlab(PenMaxX - PenMinX, PenMaxZ - PenMinZ, 0.5f, 0.07f), Mats.Lit(PenFloor),
                new Vector3((PenMinX + PenMaxX) * 0.5f, 0, (PenMinZ + PenMaxZ) * 0.5f));
            pen.GetComponent<Renderer>().receiveShadows = true;
            // the floor needs a collider so the player stands on something solid
            var col = new GameObject("floorCollider").AddComponent<BoxCollider>();
            col.transform.SetParent(transform, false);
            col.size = new Vector3(120, 1, 120);
            col.center = new Vector3(0, -0.5f, 0);
        }

        void BuildWalls()
        {
            // outer wall, with two customer gates in the south side and a closed gate north (next yard)
            Wall(new Vector3(MinX, 0, MinZ), new Vector3(MinX, 0, MaxZ));
            Wall(new Vector3(MaxX, 0, MinZ), new Vector3(MaxX, 0, MaxZ));
            Wall(new Vector3(MinX, 0, MaxZ), new Vector3(-2.2f, 0, MaxZ));
            Wall(new Vector3(2.2f, 0, MaxZ), new Vector3(MaxX, 0, MaxZ));
            Wall(new Vector3(MinX, 0, MinZ), new Vector3(ExitGateX - 1.4f, 0, MinZ));
            Wall(new Vector3(ExitGateX + 1.4f, 0, MinZ), new Vector3(EntryGateX - 1.4f, 0, MinZ));
            Wall(new Vector3(EntryGateX + 1.4f, 0, MinZ), new Vector3(MaxX, 0, MinZ));
            // north gate stays shut for now (next yard)
            ProcArt.CageFence(transform, new Vector3(-2.2f, 0, MaxZ), new Vector3(2.2f, 0, MaxZ), 1.2f);
            Blocker(new Vector3(0, 0, MaxZ), new Vector3(4.4f, 3, 0.8f));

            // crab pen: blue cage on the east and south sides (with a gate gap)
            Cage(new Vector3(PenMaxX, 0, PenMinZ), new Vector3(PenMaxX, 0, MaxZ - 0.4f));
            Cage(new Vector3(MinX + 0.4f, 0, PenMinZ), new Vector3(PenGate.x - 1.3f, 0, PenMinZ));
            Cage(new Vector3(PenGate.x + 1.3f, 0, PenMinZ), new Vector3(PenMaxX, 0, PenMinZ));
        }

        /// <summary>A straight run of thick low wall with one collider.</summary>
        void Wall(Vector3 a, Vector3 b)
        {
            if ((b - a).magnitude < 0.1f) return;
            ProcArt.LowWall(transform, a, b);
            Blocker((a + b) * 0.5f, new Vector3(0.7f, 3f, (b - a).magnitude), Quaternion.LookRotation((b - a).normalized));
        }

        void Cage(Vector3 a, Vector3 b)
        {
            if ((b - a).magnitude < 0.1f) return;
            ProcArt.CageFence(transform, a, b);
            Blocker((a + b) * 0.5f, new Vector3(0.3f, 3f, (b - a).magnitude), Quaternion.LookRotation((b - a).normalized));
        }

        void Blocker(Vector3 center, Vector3 size, Quaternion? rot = null)
        {
            var go = new GameObject("wallCollider");
            go.transform.SetParent(transform, false);
            go.transform.position = center + Vector3.up * size.y * 0.5f;
            go.transform.rotation = rot ?? Quaternion.identity;
            go.AddComponent<BoxCollider>().size = size;
        }

        void BuildSurroundings()
        {
            var rng = new System.Random(11);
            Color[] greens = { new Color(0.36f, 0.78f, 0.25f), new Color(0.3f, 0.7f, 0.22f), new Color(0.48f, 0.85f, 0.3f) };
            string[] palms = { "nature-kit/tree_palmTall", "nature-kit/tree_palmBend", "pirate-kit/palm-detailed-straight", "nature-kit/tree_palmDetailedTall" };
            int placed = 0, guard = 0;
            while (placed < 120 && guard++ < 3000)
            {
                float x = Mathf.Lerp(-26, 26, (float)rng.NextDouble());
                float z = Mathf.Lerp(-30, 30, (float)rng.NextDouble());
                bool nearYard = x > MinX - 2.4f && x < MaxX + 2.4f && z > MinZ - 2.4f && z < MaxZ + 2.4f;
                if (nearYard) continue;
                if (z < MinZ && (Mathf.Abs(x - EntryGateX) < 2.6f || Mathf.Abs(x - ExitGateX) < 2.6f)) continue;
                if (z > MaxZ && Mathf.Abs(x) < 3f) continue;
                bool inGrass = Mathf.Abs(x) < 23 && Mathf.Abs(z) < 27;
                if (inGrass)
                    ProcArt.BlobTree(transform, new Vector3(x, 0, z), 2.6f + (float)rng.NextDouble() * 1.8f, greens[rng.Next(greens.Length)]);
                else
                {
                    var tree = Visuals.Spawn(palms[rng.Next(palms.Length)], transform, 4f + (float)rng.NextDouble() * 2f, Color.white, true);
                    tree.transform.position = new Vector3(x, -0.05f, z);
                    tree.transform.rotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0);
                    Visuals.SetShadows(tree, true);
                }
                placed++;
            }
            foreach (float gx in new[] { EntryGateX, ExitGateX })
            {
                var path = ProcArt.MeshObject("path", transform, ProcArt.RoundedSlab(2.6f, 12f, 1f, 0.05f), Mats.Lit(new Color(0.96f, 0.78f, 0.6f)), new Vector3(gx, -0.26f, MinZ - 6f));
                path.GetComponent<Renderer>().receiveShadows = true;
            }
        }
    }
}

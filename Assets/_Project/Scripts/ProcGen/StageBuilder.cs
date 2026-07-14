using System.Collections.Generic;
using Chris.PachiRogue.Physics;
using UnityEngine;

namespace Chris.PachiRogue.ProcGen
{
    /// <summary>
    /// Scene side of procgen: instantiates a <see cref="StagePlan"/> into
    /// graybox geometry — pegs/bumpers/hazards from chunk layouts, side
    /// walls, a launch ledge area at the top, multiplier cups and a kill
    /// zone at the bottom. Uses an art prefab per chunk when one is assigned.
    /// </summary>
    public sealed class StageBuilder : MonoBehaviour
    {
        public const float StageHalfWidth = 4.5f;
        public const float LaunchY = 1.5f;

        private static readonly Color PegColor = new Color(0.55f, 0.78f, 1f);
        private static readonly Color BumperColor = new Color(1f, 0.85f, 0.35f);
        private static readonly Color HazardColor = new Color(1f, 0.35f, 0.4f);
        private static readonly Color CupColor = new Color(0.65f, 1f, 0.65f);
        private static readonly Color WallColor = new Color(0.35f, 0.32f, 0.5f);

        private readonly List<GameObject> _spawned = new List<GameObject>();

        public float BottomY { get; private set; }

        /// <summary>Y below which a launch resolves (under the cups).</summary>
        public float KillY => BottomY - 2f;

        public void Build(StagePlan plan, IReadOnlyDictionary<string, ChunkSO> chunkAssets)
        {
            Clear();

            if (plan.ConstraintsRelaxed)
            {
                Debug.LogWarning("StageAssembler relaxed constraints for this stage (see PLAN.md Phase 3).");
            }

            float y = 0f;
            foreach (ChunkPlacement placement in plan.Placements)
            {
                ChunkSO chunkAsset = chunkAssets[placement.ChunkId];
                ChunkData chunk = chunkAsset.ToData();

                if (chunkAsset.ArtPrefab != null)
                {
                    GameObject art = Instantiate(chunkAsset.ArtPrefab, transform);
                    art.transform.localPosition = new Vector3(placement.JitterX, y, 0f);
                    if (placement.Mirror)
                    {
                        art.transform.localScale = new Vector3(-1f, 1f, 1f);
                    }

                    _spawned.Add(art);
                }
                else
                {
                    BuildGrayboxChunk(chunk, placement, y);
                }

                y -= chunk.Height;
            }

            BottomY = y - 1.5f;
            BuildWalls(topY: LaunchY + 1f, bottomY: KillY);
            BuildCups(BottomY);
            BuildKillZone(KillY);
        }

        public void Clear()
        {
            foreach (GameObject spawned in _spawned)
            {
                if (spawned != null)
                {
                    Destroy(spawned);
                }
            }

            _spawned.Clear();
        }

        private void BuildGrayboxChunk(ChunkData chunk, ChunkPlacement placement, float topY)
        {
            float sign = placement.Mirror ? -1f : 1f;

            foreach (Vector2 local in chunk.Pegs)
            {
                GameObject peg = SpawnCircle($"Peg", PegColor, 0.18f,
                    new Vector2(local.x * sign + placement.JitterX, topY + local.y));
                peg.AddComponent<Peg>();
            }

            foreach (Vector2 local in chunk.Bumpers)
            {
                GameObject bumper = SpawnCircle("Bumper", BumperColor, 0.3f,
                    new Vector2(local.x * sign + placement.JitterX, topY + local.y));
                bumper.AddComponent<Bumper>();
            }

            foreach (Vector2 local in chunk.Hazards)
            {
                GameObject hazard = SpawnCircle("Hazard", HazardColor, 0.24f,
                    new Vector2(local.x * sign + placement.JitterX, topY + local.y));
                hazard.AddComponent<Hazard>();
            }
        }

        private void BuildWalls(float topY, float bottomY)
        {
            float height = topY - bottomY;
            float centerY = (topY + bottomY) * 0.5f;
            SpawnBox("Wall L", WallColor, new Vector2(0.4f, height), new Vector2(-StageHalfWidth - 0.2f, centerY), isTrigger: false);
            SpawnBox("Wall R", WallColor, new Vector2(0.4f, height), new Vector2(StageHalfWidth + 0.2f, centerY), isTrigger: false);
        }

        private void BuildCups(float y)
        {
            var cups = new (float x, float multiplier)[] { (-3f, 2f), (0f, 5f), (3f, 2f) };
            foreach ((float x, float multiplier) in cups)
            {
                GameObject cup = SpawnBox("Cup", CupColor, new Vector2(1.2f, 0.8f), new Vector2(x, y), isTrigger: true);
                cup.AddComponent<Cup>().multiplier = multiplier;
            }
        }

        private void BuildKillZone(float y)
        {
            GameObject zone = SpawnBox("KillZone", new Color(0f, 0f, 0f, 0f),
                new Vector2(StageHalfWidth * 2f + 2f, 0.5f), new Vector2(0f, y), isTrigger: true);
            zone.AddComponent<KillZone>();
        }

        private GameObject SpawnCircle(string label, Color color, float radius, Vector2 position)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(position.x, position.y, 0f);

            var collider = go.AddComponent<CircleCollider2D>();
            collider.radius = radius;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = GrayboxSprites.Circle();
            renderer.color = color;
            go.transform.localScale = Vector3.one * (radius * 2f);

            _spawned.Add(go);
            return go;
        }

        private GameObject SpawnBox(string label, Color color, Vector2 size, Vector2 position, bool isTrigger)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(position.x, position.y, 0f);

            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            collider.isTrigger = isTrigger;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = GrayboxSprites.Square();
            renderer.color = color;

            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            _spawned.Add(go);
            return go;
        }
    }
}

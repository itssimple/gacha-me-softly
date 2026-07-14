using System.Collections.Generic;
using UnityEngine;

namespace Chris.PachiRogue.ProcGen
{
    /// <summary>
    /// An authored stage chunk: metadata for the assembler plus a graybox
    /// feature layout (peg/bumper/hazard positions in local space, x within
    /// ±<see cref="ChunkData.HalfWidth"/>, y in [0, -Height]). An optional
    /// art prefab can replace the graybox layout later without touching the
    /// assembler (see NOTES.md).
    /// </summary>
    [CreateAssetMenu(menuName = "PachiRogue/Chunk", fileName = "Chunk_")]
    public sealed class ChunkSO : ScriptableObject
    {
        [SerializeField] private string id;

        [Tooltip("0 = easy (Meadow), 1 = medium (Onsen), 2 = hard (Sky).")]
        [SerializeField] private int difficultyBand;

        [SerializeField] private ConnectorProfile topConnector = ConnectorProfile.Open;
        [SerializeField] private ConnectorProfile bottomConnector = ConnectorProfile.Open;

        [Tooltip("Score obtainable in this chunk; validated against the recipe's budget range.")]
        [SerializeField] private int rewardBudget;

        [SerializeField] private bool allowMirror = true;

        [SerializeField] private float height = 5f;

        [SerializeField] private List<Vector2> pegs = new List<Vector2>();
        [SerializeField] private List<Vector2> bumpers = new List<Vector2>();
        [SerializeField] private List<Vector2> hazards = new List<Vector2>();

        [Tooltip("Optional themed art prefab; graybox layout is used when null.")]
        [SerializeField] private GameObject artPrefab;

        public GameObject ArtPrefab => artPrefab;

        public ChunkData ToData()
        {
            return new ChunkData
            {
                Id = id,
                DifficultyBand = difficultyBand,
                Top = topConnector,
                Bottom = bottomConnector,
                RewardBudget = rewardBudget,
                AllowMirror = allowMirror,
                Height = height,
                HazardCount = hazards.Count,
                Pegs = pegs,
                Bumpers = bumpers,
                Hazards = hazards
            };
        }
    }

    /// <summary>Pure snapshot of a chunk for the assembler/validator.</summary>
    public sealed class ChunkData
    {
        public const float HalfWidth = 4f;

        public string Id;
        public int DifficultyBand;
        public ConnectorProfile Top;
        public ConnectorProfile Bottom;
        public int RewardBudget;
        public bool AllowMirror;
        public float Height;
        public int HazardCount;
        public IReadOnlyList<Vector2> Pegs;
        public IReadOnlyList<Vector2> Bumpers;
        public IReadOnlyList<Vector2> Hazards;
    }
}

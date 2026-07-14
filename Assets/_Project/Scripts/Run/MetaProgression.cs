using System;
using Chris.PachiRogue.Core;

namespace Chris.PachiRogue.Run
{
    /// <summary>
    /// Mutating facade over the meta-progression fields of a
    /// <see cref="SaveModelV1"/> (star-shards, befriended NPCs, passive
    /// ability levels). Pure C# — the caller persists the model through
    /// <c>ISaveService</c> after mutating.
    /// </summary>
    public sealed class MetaProgression : IMetaProgressionView
    {
        private readonly SaveModelV1 _model;

        public MetaProgression(SaveModelV1 model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));

            // Repair parallel-list drift from a hand-edited or truncated save.
            while (_model.passiveAbilityLevels.Count < _model.passiveAbilityIds.Count)
            {
                _model.passiveAbilityLevels.Add(0);
            }

            while (_model.passiveAbilityLevels.Count > _model.passiveAbilityIds.Count)
            {
                _model.passiveAbilityLevels.RemoveAt(_model.passiveAbilityLevels.Count - 1);
            }
        }

        public long Shards => _model.metaCurrency;

        public void AwardShards(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            _model.metaCurrency += amount;
        }

        public bool TrySpendShards(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            if (_model.metaCurrency < amount)
            {
                return false;
            }

            _model.metaCurrency -= amount;
            return true;
        }

        public bool HasMet(string npcId)
        {
            return _model.metNpcIds.Contains(npcId);
        }

        /// <summary>Records a new friend. Idempotent.</summary>
        public void MeetNpc(string npcId)
        {
            if (string.IsNullOrEmpty(npcId))
            {
                throw new ArgumentException("NPC id must be non-empty.", nameof(npcId));
            }

            if (!_model.metNpcIds.Contains(npcId))
            {
                _model.metNpcIds.Add(npcId);
            }
        }

        public int GetAbilityLevel(string abilityId)
        {
            int index = _model.passiveAbilityIds.IndexOf(abilityId);
            return index < 0 ? 0 : _model.passiveAbilityLevels[index];
        }

        public void SetAbilityLevel(string abilityId, int level)
        {
            if (string.IsNullOrEmpty(abilityId))
            {
                throw new ArgumentException("Ability id must be non-empty.", nameof(abilityId));
            }

            if (level < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            int index = _model.passiveAbilityIds.IndexOf(abilityId);
            if (index < 0)
            {
                _model.passiveAbilityIds.Add(abilityId);
                _model.passiveAbilityLevels.Add(level);
            }
            else
            {
                _model.passiveAbilityLevels[index] = level;
            }
        }
    }
}

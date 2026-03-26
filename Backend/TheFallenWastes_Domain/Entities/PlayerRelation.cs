using System;
using TheFallenWastes_Domain.Enums;

namespace TheFallenWastes_Domain.Entities
{
    /// <summary>
    /// Personal relation from one player to another.
    /// Each player can independently mark others as Friend or Enemy.
    /// No entry = Neutral. Alliance membership = Ally (checked separately).
    /// </summary>
    public class PlayerRelation
    {
        public Guid Id { get; private set; }
        public Guid PlayerId { get; private set; }        // The player who sets the relation
        public Guid TargetPlayerId { get; private set; }   // The player being marked
        public RelationType Type { get; private set; }

        private PlayerRelation() { }

        public PlayerRelation(Guid playerId, Guid targetPlayerId, RelationType type)
        {
            if (playerId == Guid.Empty)
                throw new ArgumentException("PlayerId cannot be empty.", nameof(playerId));
            if (targetPlayerId == Guid.Empty)
                throw new ArgumentException("TargetPlayerId cannot be empty.", nameof(targetPlayerId));
            if (playerId == targetPlayerId)
                throw new ArgumentException("Cannot set a relation with yourself.");

            Id = Guid.NewGuid();
            PlayerId = playerId;
            TargetPlayerId = targetPlayerId;
            Type = type;
        }

        public void ChangeType(RelationType newType)
        {
            Type = newType;
        }
    }
}
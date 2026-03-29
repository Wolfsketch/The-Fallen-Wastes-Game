using System;
using System.Collections.Generic;
using System.Net.Mail;

namespace TheFallenWastes_Domain.Entities
{
    public class Player
    {
        public Guid Id { get; private set; }
        public string Username { get; private set; }
        public string Email { get; private set; }

        public DateTime CreatedAtUtc { get; private set; }
        public bool IsActive { get; private set; }

        public int Score { get; private set; }
        public int AttackScore { get; private set; }
        public int DefenseScore { get; private set; }
        public int TriumphPoints { get; private set; }

        public int WarScore => AttackScore + DefenseScore;

        /// Conquest level derived from TriumphPoints. Level L costs (3/2)*(L-1)*L total TP.
        /// L=1:0 | L=2:3 | L=3:9 | L=4:18 | L=5:30
        public int ConquestLevel => ComputeConquestLevel(TriumphPoints);

        /// How many settlements this player may own (= ConquestLevel).
        public int MaxSettlements => ConquestLevel;

        /// Total TP needed to reach the next conquest level.
        public int TriumphPointsForNextLevel => (int)(1.5 * ConquestLevel * (ConquestLevel + 1));

        public bool CommanderActive { get; private set; }
        public DateTime? CommanderExpiresUtc { get; private set; }

        public bool QuartermasterActive { get; private set; }
        public DateTime? QuartermasterExpiresUtc { get; private set; }

        public bool TechPriestActive { get; private set; }
        public DateTime? TechPriestExpiresUtc { get; private set; }

        public bool WarlordActive { get; private set; }
        public DateTime? WarlordExpiresUtc { get; private set; }

        public bool ScoutMasterActive { get; private set; }
        public DateTime? ScoutMasterExpiresUtc { get; private set; }

        public ICollection<Settlement> Settlements { get; private set; }

        public Guid? AllianceId { get; private set; }

        public int DataVersion { get; private set; }

        private Player()
        {
            Username = string.Empty;
            Email = string.Empty;
            Settlements = new List<Settlement>();
            DataVersion = 1;
        }

        public Player(string username, string email)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be empty.", nameof(username));

            if (username.Length < 5)
                throw new ArgumentException("Username must be at least 5 characters long.", nameof(username));

            ValidateEmail(email);

            Id = Guid.NewGuid();
            Username = username;
            Email = email;
            CreatedAtUtc = DateTime.UtcNow;
            IsActive = true;

            Score = 0;
            AttackScore = 0;
            DefenseScore = 0;

            CommanderActive = false;
            CommanderExpiresUtc = null;

            QuartermasterActive = false;
            QuartermasterExpiresUtc = null;

            TechPriestActive = false;
            TechPriestExpiresUtc = null;

            WarlordActive = false;
            WarlordExpiresUtc = null;

            ScoutMasterActive = false;
            ScoutMasterExpiresUtc = null;

            Settlements = new List<Settlement>();
            DataVersion = 1;
        }

        // ALLIANCE

        public void JoinAlliance(Guid allianceId)
        {
            if (allianceId == Guid.Empty)
                throw new ArgumentException("AllianceId cannot be empty.", nameof(allianceId));
            AllianceId = allianceId;
        }

        public void LeaveAlliance()
        {
            AllianceId = null;
        }

        // COMMANDER

        public void ActivateCommander(TimeSpan duration)
        {
            CommanderActive = true;
            CommanderExpiresUtc = DateTime.UtcNow.Add(duration);
        }

        public void ActivateCommanderPermanent()
        {
            CommanderActive = true;
            CommanderExpiresUtc = DateTime.MaxValue;
        }

        public void DeactivateCommander()
        {
            CommanderActive = false;
            CommanderExpiresUtc = null;
        }

        public bool IsCommanderCurrentlyActive()
        {
            if (CommanderActive && CommanderExpiresUtc.HasValue && CommanderExpiresUtc.Value > DateTime.UtcNow)
                return true;

            if (CommanderActive && CommanderExpiresUtc.HasValue && CommanderExpiresUtc.Value <= DateTime.UtcNow)
            {
                CommanderActive = false;
                CommanderExpiresUtc = null;
            }

            return false;
        }

        public int GetBuildQueueLimit()
        {
            const int baseSlots = 2;
            const int commanderBonus = 5;
            return IsCommanderCurrentlyActive() ? baseSlots + commanderBonus : baseSlots;
        }

        public int GetResearchQueueLimit()
        {
            const int baseSlots = 1;
            const int commanderBonus = 2;
            return IsCommanderCurrentlyActive() ? baseSlots + commanderBonus : baseSlots;
        }

        public int GetTrainingQueueLimit()
        {
            const int baseSlots = 2;
            const int commanderBonus = 3;
            return IsCommanderCurrentlyActive() ? baseSlots + commanderBonus : baseSlots;
        }

        // QUARTERMASTER

        public void ActivateQuartermaster(TimeSpan duration)
        {
            QuartermasterActive = true;
            QuartermasterExpiresUtc = DateTime.UtcNow.Add(duration);
        }

        public void DeactivateQuartermaster()
        {
            QuartermasterActive = false;
            QuartermasterExpiresUtc = null;
        }

        public bool IsQuartermasterCurrentlyActive()
        {
            if (QuartermasterActive && QuartermasterExpiresUtc.HasValue && QuartermasterExpiresUtc.Value > DateTime.UtcNow)
                return true;

            if (QuartermasterActive && QuartermasterExpiresUtc.HasValue && QuartermasterExpiresUtc.Value <= DateTime.UtcNow)
            {
                QuartermasterActive = false;
                QuartermasterExpiresUtc = null;
            }

            return false;
        }

        // TECH PRIEST

        public void ActivateTechPriest(TimeSpan duration)
        {
            TechPriestActive = true;
            TechPriestExpiresUtc = DateTime.UtcNow.Add(duration);
        }

        public void DeactivateTechPriest()
        {
            TechPriestActive = false;
            TechPriestExpiresUtc = null;
        }

        public bool IsTechPriestCurrentlyActive()
        {
            if (TechPriestActive && TechPriestExpiresUtc.HasValue && TechPriestExpiresUtc.Value > DateTime.UtcNow)
                return true;

            if (TechPriestActive && TechPriestExpiresUtc.HasValue && TechPriestExpiresUtc.Value <= DateTime.UtcNow)
            {
                TechPriestActive = false;
                TechPriestExpiresUtc = null;
            }

            return false;
        }

        // WARLORD

        public void ActivateWarlord(TimeSpan duration)
        {
            WarlordActive = true;
            WarlordExpiresUtc = DateTime.UtcNow.Add(duration);
        }

        public void DeactivateWarlord()
        {
            WarlordActive = false;
            WarlordExpiresUtc = null;
        }

        public bool IsWarlordCurrentlyActive()
        {
            if (WarlordActive && WarlordExpiresUtc.HasValue && WarlordExpiresUtc.Value > DateTime.UtcNow)
                return true;

            if (WarlordActive && WarlordExpiresUtc.HasValue && WarlordExpiresUtc.Value <= DateTime.UtcNow)
            {
                WarlordActive = false;
                WarlordExpiresUtc = null;
            }

            return false;
        }

        // SCOUT MASTER

        public void ActivateScoutMaster(TimeSpan duration)
        {
            ScoutMasterActive = true;
            ScoutMasterExpiresUtc = DateTime.UtcNow.Add(duration);
        }

        public void DeactivateScoutMaster()
        {
            ScoutMasterActive = false;
            ScoutMasterExpiresUtc = null;
        }

        public bool IsScoutMasterCurrentlyActive()
        {
            if (ScoutMasterActive && ScoutMasterExpiresUtc.HasValue && ScoutMasterExpiresUtc.Value > DateTime.UtcNow)
                return true;

            if (ScoutMasterActive && ScoutMasterExpiresUtc.HasValue && ScoutMasterExpiresUtc.Value <= DateTime.UtcNow)
            {
                ScoutMasterActive = false;
                ScoutMasterExpiresUtc = null;
            }

            return false;
        }

        // EXISTING METHODS

        public void ChangeUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be empty.", nameof(username));

            if (username.Length < 5)
                throw new ArgumentException("Username must be at least 5 characters long.", nameof(username));

            Username = username;
        }

        public void ChangeEmail(string email)
        {
            ValidateEmail(email);
            Email = email;
        }

        public void Deactivate() => IsActive = false;
        public void Activate() => IsActive = true;

        public void SetScore(int score)
        {
            if (score < 0)
                throw new ArgumentException("Score cannot be negative.", nameof(score));

            Score = score;
        }

        public void SetAttackScore(int attackScore)
        {
            if (attackScore < 0)
                throw new ArgumentException("Attack score cannot be negative.", nameof(attackScore));

            AttackScore = attackScore;
        }

        public void SetDefenseScore(int defenseScore)
        {
            if (defenseScore < 0)
                throw new ArgumentException("Defense score cannot be negative.", nameof(defenseScore));

            DefenseScore = defenseScore;
        }

        public void SetMilitaryScores(int attackScore, int defenseScore)
        {
            if (attackScore < 0)
                throw new ArgumentException("Attack score cannot be negative.", nameof(attackScore));

            if (defenseScore < 0)
                throw new ArgumentException("Defense score cannot be negative.", nameof(defenseScore));

            AttackScore = attackScore;
            DefenseScore = defenseScore;
        }

        public void AddTriumphPoints(int amount)
        {
            if (amount > 0) TriumphPoints += amount;
        }

        public void AddSettlement(Settlement settlement)
        {
            if (settlement == null)
                throw new ArgumentNullException(nameof(settlement));

            Settlements.Add(settlement);
        }

        private static int ComputeConquestLevel(int tp)
        {
            // totalTP(L) = (3/2)*(L-1)*L => L = (1 + sqrt(1 + 8*tp/3)) / 2
            if (tp <= 0) return 1;
            double L = (1.0 + Math.Sqrt(1.0 + 8.0 * tp / 3.0)) / 2.0;
            return Math.Max(1, (int)Math.Floor(L));
        }

        public void SetDataVersion(int version)
        {
            if (version < 1)
                throw new ArgumentException("DataVersion cannot be lower than 1.", nameof(version));

            DataVersion = version;
        }

        private static void ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty.", nameof(email));

            try
            {
                var mailAddress = new MailAddress(email);
                if (mailAddress.Address != email)
                    throw new ArgumentException("Email format is invalid.", nameof(email));
            }
            catch
            {
                throw new ArgumentException("Email format is invalid.", nameof(email));
            }
        }
    }
}
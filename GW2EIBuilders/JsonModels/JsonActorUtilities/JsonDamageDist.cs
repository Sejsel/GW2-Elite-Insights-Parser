﻿using System;
using System.Collections.Generic;
using System.Linq;
using GW2EIEvtcParser;
using GW2EIEvtcParser.EIData;
using GW2EIEvtcParser.ParsedData;

namespace GW2EIBuilders.JsonModels
{
    /// <summary>
    /// Class corresponding a damage distribution
    /// </summary>
    public class JsonDamageDist
    {
        /// <summary>
        /// Total damage done
        /// </summary>
        public int TotalDamage { get; }
        /// <summary>
        /// Minimum damage done
        /// </summary>
        public int Min { get; }
        /// <summary>
        /// Maximum damage done
        /// </summary>
        public int Max { get; }
        /// <summary>
        /// Number of hits
        /// </summary>
        public int Hits { get; }
        /// <summary>
        /// Number of connected hits
        /// </summary>
        public int ConnectedHits { get; }
        /// <summary>
        /// Number of crits
        /// </summary>
        public int Crit { get; }
        /// <summary>
        /// Number of glances
        /// </summary>
        public int Glance { get; }
        /// <summary>
        /// Number of flanks
        /// </summary>
        public int Flank { get; }
        /// <summary>
        /// Number of times the hit missed due to blindness
        /// </summary>
        public int Missed { get; }
        /// <summary>
        /// Number of times the hit was invulned
        /// </summary>
        public int Invulned { get; }
        /// <summary>
        /// Number of times the hit nterrupted
        /// </summary>
        public int Interrupted { get; }
        /// <summary>
        /// Number of times the hit was evaded
        /// </summary>
        public int Evaded { get; }
        /// <summary>
        /// Number of times the hit was blocked
        /// </summary>
        public int Blocked { get; }
        /// <summary>
        /// Damage done against barrier, not necessarily included in total damage
        /// </summary>
        public int ShieldDamage { get; }
        /// <summary>
        /// Critical damage
        /// </summary>
        public int CritDamage { get; }
        /// <summary>
        /// ID of the damaging skill
        /// </summary>
        /// <seealso cref="JsonLog.SkillMap"/>
        /// <seealso cref="JsonLog.BuffMap"/>
        public long Id { get; }
        /// <summary>
        /// True if indirect damage \n
        /// If true, the id is a buff
        /// </summary>
        public bool IndirectDamage { get; }

        protected JsonDamageDist(long id, List<AbstractDamageEvent> list, ParsedEvtcLog log, Dictionary<string, JsonLog.SkillDesc> skillDesc, Dictionary<string, JsonLog.BuffDesc> buffDesc)
        {
            IndirectDamage = list.Exists(x => x is NonDirectDamageEvent);
            if (IndirectDamage)
            {
                if (!buffDesc.ContainsKey("b" + id))
                {
                    if (log.Buffs.BuffsByIds.TryGetValue(id, out Buff buff))
                    {
                        buffDesc["b" + id] = new JsonLog.BuffDesc(buff, log);
                    }
                    else
                    {
                        SkillItem skill = list.First().Skill;
                        var auxBoon = new Buff(skill.Name, id, skill.Icon);
                        buffDesc["b" + id] = new JsonLog.BuffDesc(auxBoon, log);
                    }
                }
            }
            else
            {
                if (!skillDesc.ContainsKey("s" + id))
                {
                    SkillItem skill = list.First().Skill;
                    skillDesc["s" + id] = new JsonLog.SkillDesc(skill, log.LogData.GW2Build);
                }
            }
            Hits = list.Count;
            Id = id;
            Min = int.MaxValue;
            Max = int.MinValue;
            foreach (AbstractDamageEvent dmgEvt in list)
            {
                TotalDamage += dmgEvt.Damage;
                Min = Math.Min(Min, dmgEvt.Damage);
                Max = Math.Max(Max, dmgEvt.Damage);
                if (!IndirectDamage)
                {
                    if (dmgEvt.HasHit)
                    {
                        Flank += dmgEvt.IsFlanking ? 1 : 0;
                        Glance += dmgEvt.HasGlanced ? 1 : 0;
                        Crit += dmgEvt.HasCrit ? 1 : 0;
                        CritDamage += dmgEvt.HasCrit ? dmgEvt.Damage : 0;
                        Interrupted += dmgEvt.HasInterrupted ? 1 : 0;
                    }
                    Missed += dmgEvt.IsBlind ? 1 : 0;
                    Evaded += dmgEvt.IsEvaded ? 1 : 0;
                    Blocked += dmgEvt.IsBlocked ? 1 : 0;
                }
                ConnectedHits += dmgEvt.HasHit ? 1 : 0;
                Invulned += dmgEvt.IsAbsorbed ? 1 : 0;
                ShieldDamage += dmgEvt.ShieldDamage;
            }
        }

        internal static List<JsonDamageDist> BuildJsonDamageDistList(Dictionary<long, List<AbstractDamageEvent>> dlsByID, ParsedEvtcLog log, Dictionary<string, JsonLog.SkillDesc> skillDesc, Dictionary<string, JsonLog.BuffDesc> buffDesc)
        {
            var res = new List<JsonDamageDist>();
            foreach (KeyValuePair<long, List<AbstractDamageEvent>> pair in dlsByID)
            {
                res.Add(new JsonDamageDist(pair.Key, pair.Value, log, skillDesc, buffDesc));
            }
            return res;
        }

    }
}

﻿using LuckParser.Parser;
using LuckParser.Models.ParseModels;
using System;
using System.Collections.Generic;
using System.Linq;
using static LuckParser.Parser.ParseEnum.TrashIDS;
using System.Drawing;

namespace LuckParser.Models.Logic
{
    public class MursaatOverseer : RaidLogic
    {
        public MursaatOverseer(ushort triggerID) : base(triggerID)
        {
            MechanicList.AddRange(new List<Mechanic>()
            {
            new Mechanic(37677, "Soldier's Aura", Mechanic.MechType.SkillOnPlayer, new MechanicPlotlySetting("circle-open","rgb(255,0,0)"), "Jade","Jade Soldier's Aura hit", "Jade Aura",0),
            new Mechanic(37788, "Jade Explosion", Mechanic.MechType.SkillOnPlayer, new MechanicPlotlySetting("circle","rgb(255,0,0)"), "Jade Expl","Jade Soldier's Death Explosion", "Jade Explosion",0),
            //new Mechanic(37779, "Claim", Mechanic.MechType.PlayerBoon, ParseEnum.BossIDS.MursaatOverseer, new MechanicPlotlySetting("square","rgb(255,200,0)"), "Claim",0), //Buff remove only
            //new Mechanic(37697, "Dispel", Mechanic.MechType.PlayerBoon, ParseEnum.BossIDS.MursaatOverseer, new MechanicPlotlySetting("circle","rgb(255,200,0)"), "Dispel",0), //Buff remove only
            //new Mechanic(37813, "Protect", Mechanic.MechType.PlayerBoon, ParseEnum.BossIDS.MursaatOverseer, new MechanicPlotlySetting("circle","rgb(0,255,255)"), "Protect",0), //Buff remove only
            new Mechanic(757, "Invulnerability", Mechanic.MechType.PlayerBoon, new MechanicPlotlySetting("circle-open","rgb(0,255,255)"), "Protect","Protected by the Protect Shield","Protect Shield",0,(condition=> condition.CombatItem.Value == 1000)),
            new Mechanic(38155, "Mursaat Overseer's Shield", Mechanic.MechType.EnemyBoon, new MechanicPlotlySetting("circle-open","rgb(255,200,0)"), "Shield","Jade Soldier Shield", "Soldier Shield",0),
            new Mechanic(38155, "Mursaat Overseer's Shield", Mechanic.MechType.EnemyBoonStrip, new MechanicPlotlySetting("square-open","rgb(255,200,0)"), "Dispel","Dispelled Jade Soldier Shield", "Dispel",0),
            //new Mechanic(38184, "Enemy Tile", Mechanic.MechType.SkillOnPlayer, ParseEnum.BossIDS.MursaatOverseer, new MechanicPlotlySetting("square-open","rgb(255,200,0)"), "Floor","Enemy Tile damage", "Tile dmg",0) //Fixed damage (3500), not trackable
            });
            Extension = "mo";
            IconUrl = "https://wiki.guildwars2.com/images/c/c8/Mini_Mursaat_Overseer.png";
        }

        protected override CombatReplayMap GetCombatMapInternal()
        {
            return new CombatReplayMap("https://i.imgur.com/lT1FW2r.png",
                            (889, 889),
                            (1360, 2701, 3911, 5258),
                            (-27648, -9216, 27648, 12288),
                            (11774, 4480, 14078, 5376));
        }

        protected override List<ParseEnum.TrashIDS> GetTrashMobsIDS()
        {
            return new List<ParseEnum.TrashIDS>
            {
                Jade
            };
        }


        public override List<PhaseData> GetPhases(ParsedLog log, bool requirePhases)
        {
            long fightDuration = log.FightData.FightDuration;
            List<PhaseData> phases = GetInitialPhase(log);
            Target mainTarget = Targets.Find(x => x.ID == (ushort)ParseEnum.TargetIDS.MursaatOverseer);
            if (mainTarget == null)
            {
                throw new InvalidOperationException("Main target of the fight not found");
            }
            phases[0].Targets.Add(mainTarget);
            if (!requirePhases)
            {
                return phases;
            }
            List<int> limit = new List<int>()
            {
                75,
                50,
                25,
                0
            };
            long start = 0;
            int i = 0;
            for (i = 0; i < limit.Count; i++)
            {
                Point pt = mainTarget.HealthOverTime.FirstOrDefault(x => x.Y/100.0 <= limit[i]);
                if (pt == null)
                {
                    break;
                }
                PhaseData phase = new PhaseData(start, Math.Min(pt.X, fightDuration))
                {
                    Name = (25 + limit[i]) + "% - " + limit[i] + "%"
                };
                phase.Targets.Add(mainTarget);
                phases.Add(phase);
                start = pt.X;
            }
            if (i < 4)
            {
                PhaseData lastPhase = new PhaseData(start, fightDuration)
                {
                    Name = (25 + limit[i]) + "% -" + limit[i] + "%"
                };
                lastPhase.Targets.Add(mainTarget);
                phases.Add(lastPhase);
            }
            return phases;
        }


        public override void ComputeAdditionalThrashMobData(Mob mob, ParsedLog log)
        {
            CombatReplay replay = mob.CombatReplay;
            List<CastLog> cls = mob.GetCastLogs(log, 0, log.FightData.FightDuration);
            switch (mob.ID)
            {
                case (ushort)Jade:
                    List<CombatItem> shield = GetFilteredList(log, 38155, mob, true);
                    int shieldStart = 0;
                    int shieldRadius = 100;
                    foreach (CombatItem c in shield)
                    {
                        if (c.IsBuffRemove == ParseEnum.BuffRemove.None)
                        {
                            shieldStart = (int)(log.FightData.ToFightSpace(c.Time));
                        }
                        else
                        {
                            int shieldEnd = (int)(log.FightData.ToFightSpace(c.Time));
                            replay.Actors.Add(new CircleActor(true, 0, shieldRadius, (shieldStart, shieldEnd), "rgba(255, 200, 0, 0.3)", new AgentConnector(mob)));
                        }
                    }
                    List<CastLog> explosion = cls.Where(x => x.SkillId == 37788).ToList();
                    foreach (CastLog c in explosion)
                    {
                        int start = (int)c.Time;
                        int precast = 1350;
                        int duration = 100;
                        int radius = 1200;
                        replay.Actors.Add(new CircleActor(true, 0, radius, (start, start + precast + duration), "rgba(255, 0, 0, 0.05)", new AgentConnector(mob)));
                        replay.Actors.Add(new CircleActor(true, 0, radius, (start + precast -10, start + precast + duration), "rgba(255, 0, 0, 0.25)", new AgentConnector(mob)));
                    }
                    break;
                default:
                    throw new InvalidOperationException("Unknown ID in ComputeAdditionalData");
            }
        }


        public override void ComputeAdditionalTargetData(Target target, ParsedLog log)
        {
            CombatReplay replay = target.CombatReplay;
            List<CastLog> cls = target.GetCastLogs(log, 0, log.FightData.FightDuration);
            switch (target.ID)
            {
                case (ushort)ParseEnum.TargetIDS.MursaatOverseer:
                    break;
                default:
                    throw new InvalidOperationException("Unknown ID in ComputeAdditionalData");
            }
        }

        public override int IsCM(ParsedLog log)
        {
            Target target = Targets.Find(x => x.ID == (ushort)ParseEnum.TargetIDS.MursaatOverseer);
            if (target == null)
            {
                throw new InvalidOperationException("Target for CM detection not found");
            }
            OverrideMaxHealths(log);
            return (target.Health > 25e6) ? 1 : 0;
        }

        public override void ComputeAdditionalPlayerData(Player p, ParsedLog log)
        {
        }
    }
}

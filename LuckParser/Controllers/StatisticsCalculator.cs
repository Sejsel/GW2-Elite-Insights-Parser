﻿using System;
using System.Collections.Generic;
using System.Linq;
using LuckParser.Models.DataModels;
using LuckParser.Models.ParseModels;

namespace LuckParser.Controllers
{
    /// <summary>
    /// Calculates statistical information from a log
    /// </summary>
    public class StatisticsCalculator
    {
        public class Switches
        {
            public bool CalculateDPS = false;
            public bool CalculateStats = false;
            public bool CalculateDefense = false;
            public bool CalculateSupport = false;
            public bool CalculateBoons = false;
            public bool CalculateConditions = false;
            public bool CalculateCombatReplay = false;
            public bool CalculateMechanics = false;
        }

        private readonly SettingsContainer _settings;

        private Statistics _statistics;

        private ParsedLog _log;

        public StatisticsCalculator(SettingsContainer settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Calculate a statistic from a log
        /// </summary>
        /// <param name="log"></param>
        /// <param name="switches"></param>
        /// <returns></returns>
        public Statistics CalculateStatistics(ParsedLog log, Switches switches)
        {
            _statistics = new Statistics();

            _log = log;

            _statistics.Phases = log.GetBoss().GetPhases(log, _settings.ParsePhases);
            if (switches.CalculateCombatReplay && _settings.ParseCombatReplay)
            {
                foreach (Player p in log.GetPlayerList())
                {
                    p.InitCombatReplay(log, _settings.PollingRate, false, true);
                }
                log.GetBoss().InitCombatReplay(log, _settings.PollingRate, false, true);
            }
            if (switches.CalculateDPS) CalculateDPS();
            if (switches.CalculateStats) CalculateStats();
            if (switches.CalculateDefense) CalculateDefenses();
            if (switches.CalculateSupport) CalculateSupport();
            if (switches.CalculateBoons)
            {
                SetPresentBoons();
                CalculateBoons();
            } 
                      
            if (switches.CalculateConditions) CalculateConditions();
            if (switches.CalculateMechanics)
            {
                log.GetBoss().AddMechanics(log);
                foreach (Player p in log.GetPlayerList())
                {
                    p.AddMechanics(log);
                }
                log.GetMechanicData().ComputePresentMechanics(log, _statistics.Phases);
            }

            return _statistics;
        }

        private Statistics.FinalDPS GetFinalDPS(AbstractPlayer player, int phaseIndex, bool checkRedirection)
        {
            Statistics.FinalDPS final = new Statistics.FinalDPS();

            PhaseData phase = _statistics.Phases[phaseIndex];

            double phaseDuration = (phase.GetDuration()) / 1000.0;

            double damage;
            double dps = 0.0;

            ////////// ALL
            //DPS
            damage = player.GetDamageLogs(0, _log, phase.GetStart(),
                    phase.GetEnd())
                .Sum(x => x.GetDamage());
            if (phaseDuration > 0)
            {
                dps = damage / phaseDuration;
            }
            final.AllDps = (int)dps;
            final.AllDamage = (int)damage;
            //Condi DPS
            damage = player.GetDamageLogs(0, _log, phase.GetStart(),
                    phase.GetEnd())
                .Where(x => x.IsCondi() > 0).Sum(x => x.GetDamage());
            if (phaseDuration > 0)
            {
                dps = damage / phaseDuration;
            }
            final.AllCondiDps = (int)dps;
            final.AllCondiDamage = (int)damage;
            //Power DPS
            damage = final.AllDamage - final.AllCondiDamage;
            if (phaseDuration > 0)
            {
                dps = damage / phaseDuration;
            }
            final.AllPowerDps = (int)dps;
            final.AllPowerDamage = (int)damage;
            final.PlayerPowerDamage = player.GetJustPlayerDamageLogs(0, _log,
                phase.GetStart(), phase.GetEnd()).Where(x => x.IsCondi() == 0).Sum(x => x.GetDamage());
            /////////// BOSS
            //DPS
            if (checkRedirection && phase.GetRedirection().Count > 0)
            {
                damage = player.GetDamageLogs(phase.GetRedirection(), _log,
                    phase.GetStart(), phase.GetEnd()).Sum(x => x.GetDamage());
            } else
            {
                damage = player.GetDamageLogs(_log.GetBossData().GetInstid(), _log,
                    phase.GetStart(), phase.GetEnd()).Sum(x => x.GetDamage());
            }
            if (phaseDuration > 0)
            {
                dps = damage / phaseDuration;
            }
            final.BossDps = (int)dps;
            final.BossDamage = (int)damage;
            //Condi DPS
            if (checkRedirection && phase.GetRedirection().Count > 0)
            {
                damage = player.GetDamageLogs(phase.GetRedirection(), _log,
                    phase.GetStart(), phase.GetEnd()).Where(x => x.IsCondi() > 0).Sum(x => x.GetDamage());
            }
            else
            {
                damage = player.GetDamageLogs(_log.GetBossData().GetInstid(), _log,
                    phase.GetStart(), phase.GetEnd()).Where(x => x.IsCondi() > 0).Sum(x => x.GetDamage());
            }
            if (phaseDuration > 0)
            {
                dps = damage / phaseDuration;
            }
            final.BossCondiDps = (int)dps;
            final.BossCondiDamage = (int)damage;
            //Power DPS
            damage = final.BossDamage - final.BossCondiDamage;
            if (phaseDuration > 0)
            {
                dps = damage / phaseDuration;
            }
            final.BossPowerDps = (int)dps;
            final.BossPowerDamage = (int)damage;
            if (checkRedirection && phase.GetRedirection().Count > 0)
            {
                final.PlayerBossPowerDamage = player.GetJustPlayerDamageLogs(phase.GetRedirection(), _log,
                    phase.GetStart(), phase.GetEnd()).Where(x => x.IsCondi() == 0).Sum(x => x.GetDamage());
            }
            else
            {
                final.PlayerBossPowerDamage = player.GetJustPlayerDamageLogs(_log.GetBossData().GetInstid(), _log,
                    phase.GetStart(), phase.GetEnd()).Where(x => x.IsCondi() == 0).Sum(x => x.GetDamage());
            }

            return final;
        }

        private void CalculateDPS()
        {
            foreach (Player player in _log.GetPlayerList())
            {
                Statistics.FinalDPS[] phaseDps = new Statistics.FinalDPS[_statistics.Phases.Count];
                for (int phaseIndex = 0; phaseIndex <_statistics.Phases.Count; phaseIndex++)
                {
                    phaseDps[phaseIndex] = GetFinalDPS(player,phaseIndex, true);
                }

                _statistics.Dps[player] = phaseDps;
            }

            Statistics.FinalDPS[] phaseBossDps = new Statistics.FinalDPS[_statistics.Phases.Count];
            for (int phaseIndex = 0; phaseIndex <_statistics.Phases.Count; phaseIndex++)
            {
                phaseBossDps[phaseIndex] = GetFinalDPS(_log.GetBoss(), phaseIndex, false);
            }

            _statistics.BossDps = phaseBossDps;
        }

        private void CalculateStats()
        {
            foreach (Player player in _log.GetPlayerList())
            {
                Statistics.FinalStats[] phaseStats = new Statistics.FinalStats[_statistics.Phases.Count];
                for (int phaseIndex = 0; phaseIndex <_statistics.Phases.Count; phaseIndex++)
                {
                    Statistics.FinalStats final = new Statistics.FinalStats();

                    PhaseData phase = _statistics.Phases[phaseIndex];
                    long start = phase.GetStart() + _log.GetBossData().GetFirstAware();
                    long end = phase.GetEnd() + _log.GetBossData().GetFirstAware();

                    List<DamageLog> damageLogs  = player.GetJustPlayerDamageLogs(0, _log, phase.GetStart(), phase.GetEnd());
                    List<CastLog> castLogs = player.GetCastLogs(_log, phase.GetStart(), phase.GetEnd());

                    int instid = player.GetInstid();

                    final.PowerLoopCount = 0;
                    final.CritablePowerLoopCount = 0;
                    final.CriticalRate = 0;
                    final.CriticalDmg = 0;
                    final.ScholarRate = 0;
                    final.ScholarDmg = 0;
                    final.MovingRate = 0;
                    final.MovingDamage = 0;
                    final.FlankingRate = 0;
                    final.GlanceRate = 0;
                    final.Missed = 0;
                    final.Interupts = 0;
                    final.Invulned = 0;
                    final.Wasted = 0;
                    final.TimeWasted = 0;
                    final.Saved = 0;
                    final.TimeSaved = 0;
                    final.StackDist = 0;
                    
                    final.PowerLoopCountBoss = 0;
                    final.CritablePowerLoopCountBoss = 0;
                    final.CriticalRateBoss = 0;
                    final.CriticalDmgBoss = 0;
                    final.ScholarRateBoss = 0;
                    final.ScholarDmgBoss = 0;
                    final.MovingRateBoss = 0;
                    final.MovingDamageBoss = 0;
                    final.FlankingRateBoss = 0;
                    final.GlanceRateBoss = 0;
                    final.MissedBoss = 0;
                    final.InteruptsBoss = 0;
                    final.InvulnedBoss = 0;

                    // Add non critable sigil/rune procs here
                    HashSet<long> nonCritable = new HashSet<long>
                    {
                        9292
                    };
                    HashSet<long> idsToCheck = new HashSet<long>();
                    if (phase.GetRedirection().Count > 0)
                    {
                        foreach (AgentItem a in phase.GetRedirection())
                        {
                            idsToCheck.Add(a.GetInstid());
                        }
                    } else
                    {
                        idsToCheck.Add(_log.GetBossData().GetInstid());
                    }
                    foreach (DamageLog dl in damageLogs)
                    {
                        if (dl.IsCondi() == 0)
                        {

                            if (idsToCheck.Contains(dl.GetDstInstidt()))
                            {
                                if (idsToCheck.Count > 1)
                                {
                                    AgentItem target = phase.GetRedirection().Find(x => x.GetInstid() == dl.GetDstInstidt());
                                    if (dl.GetTime() < target.GetFirstAware() - _log.GetBossData().GetFirstAware() || dl.GetTime() > target.GetLastAware() - _log.GetBossData().GetFirstAware())
                                    {
                                        continue;
                                    }
                                }
                                if (dl.GetResult() == ParseEnum.Result.Crit)
                                {
                                    final.CriticalRateBoss++;
                                    final.CriticalDmgBoss += dl.GetDamage();
                                }

                                if (dl.IsNinety() > 0)
                                {
                                    final.ScholarRateBoss++;
                                    final.ScholarDmgBoss += (int)(dl.GetDamage() / 11.0); //regular+10% damage
                                }

                                if (dl.IsMoving() > 0)
                                {
                                    final.MovingRateBoss++;
                                    final.MovingDamageBoss += (int)(dl.GetDamage() / 21.0);
                                }
                                
                                final.FlankingRateBoss += dl.IsFlanking();

                                if (dl.GetResult() == ParseEnum.Result.Glance)
                                {
                                    final.GlanceRateBoss++;
                                }

                                if (dl.GetResult() == ParseEnum.Result.Blind)
                                {
                                    final.MissedBoss++;
                                }

                                if (dl.GetResult() == ParseEnum.Result.Interrupt)
                                {
                                    final.InteruptsBoss++;
                                }

                                if (dl.GetResult() == ParseEnum.Result.Absorb)
                                {
                                    final.InvulnedBoss++;
                                }
                                final.PowerLoopCountBoss++;
                                if (!nonCritable.Contains(dl.GetID()))
                                {
                                    final.CritablePowerLoopCountBoss++;
                                }
                            }

                            if (dl.GetResult() == ParseEnum.Result.Crit)
                            {
                                final.CriticalRate++;
                                final.CriticalDmg += dl.GetDamage();
                            }

                            if (dl.IsNinety() > 0)
                            {
                                final.ScholarRate++;
                                final.ScholarDmg += (int)(dl.GetDamage() / 11.0); //regular+10% damage
                            }

                            if (dl.IsMoving() > 0)
                            {
                                final.MovingRate++;
                                final.MovingDamage += (int)(dl.GetDamage() / 21.0);
                            }
                            
                            final.FlankingRate += dl.IsFlanking();

                            if (dl.GetResult() == ParseEnum.Result.Glance)
                            {
                                final.GlanceRate++;
                            }

                            if (dl.GetResult() == ParseEnum.Result.Blind)
                            {
                                final.Missed++;
                            }

                            if (dl.GetResult() == ParseEnum.Result.Interrupt)
                            {
                                final.Interupts++;
                            }

                            if (dl.GetResult() == ParseEnum.Result.Absorb)
                            {
                                final.Invulned++;
                            }
                            final.PowerLoopCount++;
                            if (!nonCritable.Contains(dl.GetID()))
                            {
                                final.CritablePowerLoopCount++;
                            }
                        }
                    }
                    foreach (CastLog cl in castLogs)
                    {
                        if (cl.EndActivation() == ParseEnum.Activation.CancelCancel)
                        {
                            final.Wasted++;
                            final.TimeWasted += cl.GetActDur();
                        }
                        if (cl.EndActivation() == ParseEnum.Activation.CancelFire)
                        {
                            final.Saved++;
                            if (cl.GetActDur() < cl.GetExpDur())
                            {
                                final.TimeSaved += cl.GetExpDur() - cl.GetActDur();
                            }
                        }
                    }

                    final.TimeSaved = final.TimeSaved / 1000f;
                    final.TimeWasted = final.TimeWasted / 1000f;
                    
                    final.PowerLoopCount = final.PowerLoopCount == 0 ? 1 : final.PowerLoopCount;
                    
                    final.PowerLoopCountBoss = final.PowerLoopCountBoss == 0 ? 1 : final.PowerLoopCountBoss;

                    // Counts
                    CombatData combatData = _log.GetCombatData();
                    final.SwapCount = combatData.GetStates(instid, ParseEnum.StateChange.WeaponSwap, start, end).Count;
                    final.DownCount = combatData.GetStates(instid, ParseEnum.StateChange.ChangeDown, start, end).Count;
                    final.DodgeCount = combatData.GetSkillCount(instid, SkillItem.DodgeId, start, end) + combatData.GetBuffCount(instid, 40408, start, end);//dodge = 65001 mirage cloak =40408

                    //Stack Distance
                    if (_settings.ParseCombatReplay && _log.GetBossData().GetBossBehavior().CanCombatReplay)
                    {
                        if (_statistics.StackCenterPositions == null)
                        {
                            _statistics.StackCenterPositions = new List<Point3D>();
                            List<List<Point3D>> GroupsPosList = new List<List<Point3D>>();
                            foreach (Player p in _log.GetPlayerList())
                            {
                                List<Point3D> list = p.GetCombatReplay().GetActivePositions();  
                                if (list.Count > 1)
                                {
                                    GroupsPosList.Add(list);
                                }
                            }                       
                            for (int time = 0; time < GroupsPosList[0].Count; time++)
                            {
                                float x = 0;
                                float y = 0;
                                float z = 0;
                                int activePlayers = GroupsPosList.Count;
                                foreach (List<Point3D> points in GroupsPosList)
                                {
                                    Point3D point = points[time];
                                    if (point != null)
                                    {
                                        x += point.X;
                                        y += point.Y;
                                        z += point.Z;
                                    }
                                    else
                                    {
                                        activePlayers--;
                                    }
                                   
                                }
                                x = x /activePlayers;
                                y = y / activePlayers;
                                z = z / activePlayers;
                                _statistics.StackCenterPositions.Add(new Point3D(x, y, z, _settings.PollingRate * time));
                            }
                        }
                        List<Point3D> positions = player.GetCombatReplay().GetPositions().Where(x => x.Time >= phase.GetStart() && x.Time <= phase.GetEnd()).ToList();
                        int offset = player.GetCombatReplay().GetPositions().Count(x => x.Time < phase.GetStart());
                        if (positions.Count > 1)
                        {
                            List<float> distances = new List<float>();
                            for (int time = 0; time < positions.Count; time++)
                            {

                                float deltaX = positions[time].X - _statistics.StackCenterPositions[time + offset].X;
                                float deltaY = positions[time].Y - _statistics.StackCenterPositions[time + offset].Y;
                                //float deltaZ = positions[time].Z - Statistics.StackCenterPositions[time].Z;


                                distances.Add((float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY));
                            }
                            final.StackDist = distances.Sum() / distances.Count;
                        }
                        else
                        {
                            final.StackDist = -1;
                        }
                    }
                    // R.I.P
                    List<CombatItem> dead = combatData.GetStates(instid, ParseEnum.StateChange.ChangeDead, start, end);
                    final.Died = 0.0;
                    if (dead.Count > 0)
                    {
                        final.Died = dead.Last().Time - start;
                    }

                    List<CombatItem> disconnect = combatData.GetStates(instid, ParseEnum.StateChange.Despawn, start, end);
                    final.Dcd = 0.0;
                    if (disconnect.Count > 0)
                    {
                        final.Dcd = disconnect.Last().Time - start;
                    }

                    phaseStats[phaseIndex] = final;
                }
                _statistics.Stats[player] = phaseStats;
            }
        }

        private void CalculateDefenses()
        {
            foreach (Player player in _log.GetPlayerList())
            {
                Statistics.FinalDefenses[] phaseDefense = new Statistics.FinalDefenses[_statistics.Phases.Count];
                for (int phaseIndex = 0; phaseIndex <_statistics.Phases.Count; phaseIndex++)
                {
                    Statistics.FinalDefenses final = new Statistics.FinalDefenses();

                    PhaseData phase =_statistics.Phases[phaseIndex];

                    List<DamageLog> damageLogs = player.GetDamageTakenLogs(_log, phase.GetStart(), phase.GetEnd());
                    //List<DamageLog> healingLogs = player.getHealingReceivedLogs(log, phase.getStart(), phase.getEnd());
                 
                    final.DamageTaken = damageLogs.Sum(x => (long)x.GetDamage());
                    //final.allHealReceived = healingLogs.Sum(x => x.getDamage());
                    final.BlockedCount = damageLogs.Count(x => x.GetResult() == ParseEnum.Result.Block);
                    final.InvulnedCount = 0;
                    final.DamageInvulned = 0;
                    final.EvadedCount = damageLogs.Count(x => x.GetResult() == ParseEnum.Result.Evade);
                    final.DamageBarrier = damageLogs.Sum(x => x.IsShields() == 1 ? x.GetDamage() : 0);
                    foreach (DamageLog log in damageLogs.Where(x => x.GetResult() == ParseEnum.Result.Absorb))
                    {
                        final.InvulnedCount++;
                        final.DamageInvulned += log.GetDamage();
                    }

                    phaseDefense[phaseIndex] = final;
                }
                _statistics.Defenses[player] = phaseDefense;
            }
        }

       
        private void CalculateSupport()
        {
            foreach (Player player in _log.GetPlayerList())
            {
                Statistics.FinalSupport[] phaseSupport = new Statistics.FinalSupport[_statistics.Phases.Count];
                for (int phaseIndex = 0; phaseIndex <_statistics.Phases.Count; phaseIndex++)
                {
                    Statistics.FinalSupport final = new Statistics.FinalSupport();

                    PhaseData phase =_statistics.Phases[phaseIndex];

                    int[] resArray = player.GetReses(_log, phase.GetStart(), phase.GetEnd());
                    int[] cleanseArray = player.GetCleanses(_log, phase.GetStart(), phase.GetEnd());
                    //List<DamageLog> healingLogs = player.getHealingLogs(log, phase.getStart(), phase.getEnd());
                    //final.allHeal = healingLogs.Sum(x => x.getDamage());
                    final.Resurrects = resArray[0];
                    final.ResurrectTime = resArray[1]/1000f;
                    final.CondiCleanse = cleanseArray[0];
                    final.CondiCleanseTime = cleanseArray[1]/1000f;

                    phaseSupport[phaseIndex] = final;
                }
                _statistics.Support[player] = phaseSupport;
            }
        }

        private Dictionary<long, Statistics.FinalBoonUptime>[] GetBoonsForPlayers(List<Player> playerList, Player player)
        {
            Dictionary<long, Statistics.FinalBoonUptime>[] uptimesByPhase =
                new Dictionary<long, Statistics.FinalBoonUptime>[_statistics.Phases.Count];

            for (int phaseIndex = 0; phaseIndex < _statistics.Phases.Count; phaseIndex++)
            {
                PhaseData phase = _statistics.Phases[phaseIndex];
                long fightDuration = phase.GetEnd() - phase.GetStart();

                Dictionary<Player, BoonDistribution> boonDistributions = new Dictionary<Player, BoonDistribution>();
                foreach (Player p in playerList)
                {
                    boonDistributions[p] = p.GetBoonDistribution(_log, _statistics.Phases, phaseIndex);
                }

                Dictionary<long, Statistics.FinalBoonUptime> final =
                    new Dictionary<long, Statistics.FinalBoonUptime>();

                foreach (Boon boon in player.GetBoonToTrack())
                {
                    long totalGeneration = 0;
                    long totalOverstack = 0;

                    foreach (BoonDistribution boons in boonDistributions.Values)
                    {
                        if (boons.ContainsKey(boon.GetID()))
                        {
                            totalGeneration += boons.GetGeneration(boon.GetID(), player.GetInstid());
                            totalOverstack += boons.GetOverstack(boon.GetID(), player.GetInstid());
                        }
                    }

                    Statistics.FinalBoonUptime uptime = new Statistics.FinalBoonUptime();

                    if (boon.GetBoonType() == Boon.BoonType.Duration)
                    {
                        uptime.Generation = Math.Round(100.0f * totalGeneration / fightDuration / playerList.Count, 1);
                        uptime.Overstack = Math.Round(100.0f * (totalOverstack + totalGeneration) / fightDuration / playerList.Count, 1);
                    }
                    else if (boon.GetBoonType() == Boon.BoonType.Intensity)
                    {
                        uptime.Generation = Math.Round((double) totalGeneration / fightDuration / playerList.Count, 1);
                        uptime.Overstack = Math.Round((double) (totalOverstack + totalGeneration) / fightDuration / playerList.Count, 1);
                    }

                    final[boon.GetID()] = uptime;
                }

                uptimesByPhase[phaseIndex] = final;
            }

            return uptimesByPhase;
        }

        private void CalculateBoons()
        {
            foreach (Player player in _log.GetPlayerList())
            {
                // Boons applied to self
                Dictionary<long, Statistics.FinalBoonUptime>[] selfUptimesByPhase = new Dictionary<long, Statistics.FinalBoonUptime>[_statistics.Phases.Count];
                for (int phaseIndex = 0; phaseIndex <_statistics.Phases.Count; phaseIndex++)
                {
                    Dictionary<long, Statistics.FinalBoonUptime> final = new Dictionary<long, Statistics.FinalBoonUptime>();

                    PhaseData phase =_statistics.Phases[phaseIndex];

                    BoonDistribution selfBoons = player.GetBoonDistribution(_log,_statistics.Phases, phaseIndex);

                    long fightDuration = phase.GetEnd() - phase.GetStart();
                    foreach (Boon boon in player.GetBoonToTrack())
                    {
                        Statistics.FinalBoonUptime uptime = new Statistics.FinalBoonUptime
                        {
                            Uptime = 0,
                            Generation = 0,
                            Overstack = 0
                        };
                        if (selfBoons.ContainsKey(boon.GetID()))
                        {
                            long generation = selfBoons.GetGeneration(boon.GetID(), player.GetInstid());
                            if (boon.GetBoonType() == Boon.BoonType.Duration)
                            {
                                uptime.Uptime = Math.Round(100.0 * selfBoons.GetUptime(boon.GetID()) / fightDuration, 1);
                                uptime.Generation = Math.Round(100.0f * generation / fightDuration, 1);
                                uptime.Overstack = Math.Round(100.0f * (selfBoons.GetOverstack(boon.GetID(), player.GetInstid()) + generation) / fightDuration, 1);
                            }
                            else if (boon.GetBoonType() == Boon.BoonType.Intensity)
                            {
                                uptime.Uptime = Math.Round((double)selfBoons.GetUptime(boon.GetID()) / fightDuration, 1);
                                uptime.Generation = Math.Round((double)generation / fightDuration, 1);
                                uptime.Overstack = Math.Round((double)(selfBoons.GetOverstack(boon.GetID(), player.GetInstid()) + generation) / fightDuration, 1);
                            }
                        }
                        final[boon.GetID()] = uptime;
                    }

                    selfUptimesByPhase[phaseIndex] = final;
                }
                _statistics.SelfBoons[player] = selfUptimesByPhase;

                // Boons applied to player's group
                var otherPlayersInGroup = _log.GetPlayerList()
                    .Where(p => p.GetGroup() == player.GetGroup() && player.GetInstid() != p.GetInstid())
                    .ToList();
                _statistics.GroupBoons[player] = GetBoonsForPlayers(otherPlayersInGroup, player);

                // Boons applied to other groups
                var offGroupPlayers = _log.GetPlayerList().Where(p => p.GetGroup() != player.GetGroup()).ToList();
                _statistics.OffGroupBoons[player] = GetBoonsForPlayers(offGroupPlayers, player);

                // Boons applied to squad
                var otherPlayers = _log.GetPlayerList().Where(p => p.GetInstid() != player.GetInstid()).ToList();
                _statistics.SquadBoons[player] = GetBoonsForPlayers(otherPlayers, player);
            }
        }

        private void CalculateConditions()
        {
            _statistics.BossConditions = new Dictionary<long, Statistics.FinalBossBoon>[_statistics.Phases.Count];
            for (int phaseIndex = 0; phaseIndex <_statistics.Phases.Count; phaseIndex++)
            {
                BoonDistribution boonDistribution = _log.GetBoss().GetBoonDistribution(_log,_statistics.Phases, phaseIndex);
                Dictionary<long, Statistics.FinalBossBoon> rates = new Dictionary<long, Statistics.FinalBossBoon>();

                PhaseData phase =_statistics.Phases[phaseIndex];
                long fightDuration = phase.GetDuration();

                foreach (Boon boon in _log.GetBoss().GetBoonToTrack())
                {
                    Statistics.FinalBossBoon condition = new Statistics.FinalBossBoon(_log.GetPlayerList());
                    rates[boon.GetID()] = condition;
                    if (boonDistribution.ContainsKey(boon.GetID()))
                    {
                        if (boon.GetBoonType() == Boon.BoonType.Duration)
                        {
                            condition.Uptime = Math.Round(100.0 * boonDistribution.GetUptime(boon.GetID()) / fightDuration, 1);
                            foreach(Player p in _log.GetPlayerList())
                            {
                                long gen = boonDistribution.GetGeneration(boon.GetID(), p.GetInstid());
                                condition.Generated[p] = Math.Round(100.0 * gen / fightDuration, 1);
                                condition.Overstacked[p] = Math.Round(100.0 * (boonDistribution.GetOverstack(boon.GetID(), p.GetInstid()) + gen) / fightDuration, 1);
                            }
                        }
                        else if (boon.GetBoonType() == Boon.BoonType.Intensity)
                        {
                            condition.Uptime = Math.Round((double) boonDistribution.GetUptime(boon.GetID()) / fightDuration, 1);
                            foreach (Player p in _log.GetPlayerList())
                            {
                                long gen = boonDistribution.GetGeneration(boon.GetID(), p.GetInstid());
                                condition.Generated[p] = Math.Round((double) gen / fightDuration, 1);
                                condition.Overstacked[p] = Math.Round((double)(boonDistribution.GetOverstack(boon.GetID(), p.GetInstid())+ gen) / fightDuration, 1);
                            }
                        }

                        rates[boon.GetID()] = condition;
                    }
                }

                _statistics.BossConditions[phaseIndex] = rates;
            }
        }
        /// <summary>
        /// Checks the combat data and gets buffs that were present during the fight
        /// </summary>
        private void SetPresentBoons()
        {
            List<CombatItem> combatList = _log.GetCombatData();
            var skillIDs = new HashSet<long>(combatList.Select(x => x.SkillID));
            if (_settings.PlayerBoonsUniversal)
            {
                // Main boons
                foreach (Boon boon in Boon.GetBoonList())
                {
                    if (skillIDs.Contains(boon.GetID()))
                    {
                        _statistics.PresentBoons.Add(boon);
                    }
                }
                // Main Conditions
                foreach (Boon boon in Boon.GetCondiBoonList())
                {
                    if (skillIDs.Contains(boon.GetID()))
                    {
                        _statistics.PresentConditions.Add(boon);
                    }
                }
            }

            if (_settings.PlayerBoonsImpProf)
            {
                // Important class specific boons
                foreach (Boon boon in Boon.GetOffensiveTableList())
                {
                    if (skillIDs.Contains(boon.GetID()))
                    {
                        _statistics.PresentOffbuffs.Add(boon);
                    }
                }

                foreach (Boon boon in Boon.GetDefensiveTableList())
                {
                    if (skillIDs.Contains(boon.GetID()))
                    {
                        _statistics.PresentDefbuffs.Add(boon);
                    }
                }
            }

            var players = _log.GetPlayerList();
            Dictionary<ushort, List<Boon>> presentPersonalBuffs = new Dictionary<ushort, List<Boon>>();
            if (_settings.PlayerBoonsAllProf)
            {
                var playersById = new Dictionary<ushort, Player>();
                foreach (var player in players)
                {
                    presentPersonalBuffs[player.GetInstid()] = new List<Boon>();
                    playersById.Add(player.GetInstid(), player);
                }
                // All class specific boons
                var remainingBoons = Boon.GetRemainingBuffsList();

                var classSpecificBoonsById = new Dictionary<long, Boon>();
                foreach (var boon in remainingBoons)
                {
                    if (boon.GetID() == -1) continue;

                    classSpecificBoonsById.Add(boon.GetID(), boon);
                }

                foreach (var item in combatList)
                {
                    if (playersById.TryGetValue(item.DstInstid, out Player player))
                    {
                        if (classSpecificBoonsById.TryGetValue(item.SkillID, out Boon boon))
                        {
                            presentPersonalBuffs[player.GetInstid()].Add(boon);
                            classSpecificBoonsById.Remove(item.SkillID);
                        }
                    }
                }
            }
            foreach (Player player in players)
            {
                List<List<Boon>> boonListToTrack = new List<List<Boon>>
                {
                    _statistics.PresentBoons,
                    _statistics.PresentOffbuffs,
                    _statistics.PresentDefbuffs,
                };
                if(_settings.PlayerBoonsAllProf)
                {
                    boonListToTrack.Add(presentPersonalBuffs[player.GetInstid()]);
                }
                player.SetBoonToTrack(boonListToTrack);
            }
            // boss boons
            _log.GetBoss().SetBoonToTrack(new List<List<Boon>> {_statistics.PresentBoons, _statistics.PresentConditions, Boon.GetBossBoonList()});
        }
    }
}


using System;
using System.Collections.Generic;
using System.Text;
using DNWS.Werewolf;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace DNWS.Werewolf
{
    partial class WerewolfGame
    {
        public const string ROLE_SEER = "Seer";
        public const string ROLE_AURA_SEER = "Aura Seer";
        public const string ROLE_PRIEST = "Priest";
        public const string ROLE_DOCTOR = "Doctor";
        public const string ROLE_WEREWOLF = "Werewolf";
        public const string ROLE_WEREWOLF_SHAMAN = "Werewolf Sharman";
        public const string ROLE_ALPHA_WEREWOLF = "Alpha Werewolf";
        public const string ROLE_WEREWOLF_SEER = "Werewolf Seer";
        public const string ROLE_MEDIUM = "Medium";
        public const string ROLE_BODYGUARD = "Bodyguard";
        public const string ROLE_JAILER = "Jailer";
        public const string ROLE_FOOL = "Fool";
        public const string ROLE_HEAD_HUNTER = "Head Hunter";
        public const string ROLE_SERIAL_KILLER = "Serial Killer";
        public const string ROLE_GUNNER = "Gunner";
        public const string ACTION_DAY_VOTE = "Day Vote";
        public const string ACTION_HOLYWATER = "Throw holy-water";
        public const string ACTION_SHOOT = "Shoot";
        public const string ACTION_JAIL = "Jail";
        public const string ACTION_ENCHANT = "Enchant";
        public const string ACTION_NIGHT_VOTE = "Night Vote";
        public const string ACTION_GUARD = "Guard";
        public const string ACTION_HEAL = "Heal";
        public const string ACTION_KILL = "Kill";
        public const string ACTION_REVEAL = "Reveal";
        public const string ACTION_AURA = "Aura";
        public const string ACTION_REVIVE = "Revive";
        public static string[] ROLE_LIST = {
            ROLE_ALPHA_WEREWOLF,
            ROLE_AURA_SEER,
            ROLE_BODYGUARD,
            ROLE_DOCTOR,
            ROLE_FOOL,
            ROLE_GUNNER,
            ROLE_HEAD_HUNTER,
            ROLE_JAILER,
            ROLE_MEDIUM,
            ROLE_PRIEST,
            ROLE_SEER,
            ROLE_SERIAL_KILLER,
            ROLE_WEREWOLF,
            ROLE_WEREWOLF_SEER,
            ROLE_WEREWOLF_SHAMAN
        };
        private void InitActions()
        {
            if (_db == null || _roleInitialized == false) {
                return;
            }
            if (_db.Actions.Count() == 0) {

                Role seer = GetRoleByName(ROLE_SEER);
                Role auraseer = GetRoleByName(ROLE_AURA_SEER);
                Role priest = GetRoleByName(ROLE_PRIEST);
                Role doctor = GetRoleByName(ROLE_DOCTOR);
                Role werewolf = GetRoleByName(ROLE_WEREWOLF);
                Role werewolfshaman = GetRoleByName(ROLE_WEREWOLF_SHAMAN);
                Role alphawerewolf = GetRoleByName(ROLE_ALPHA_WEREWOLF);
                Role werewolfseer = GetRoleByName(ROLE_WEREWOLF_SEER);
                Role medium = GetRoleByName(ROLE_MEDIUM);
                Role bodyguard = GetRoleByName(ROLE_BODYGUARD);
                Role jailer = GetRoleByName(ROLE_JAILER);
                Role fool = GetRoleByName(ROLE_FOOL);
                Role headhunter = GetRoleByName(ROLE_HEAD_HUNTER);
                Role serialkiller = GetRoleByName(ROLE_SERIAL_KILLER);
                Role gunner = GetRoleByName(ROLE_GUNNER);

                Action dayVote = new Action{
                    Id = 1,
                    Name = ACTION_DAY_VOTE,
                    Description = "Vote to burn a player in a day time",
                };
                _db.Actions.Add(dayVote);
                _db.ActionRoles.Add(new ActionRole{Action = dayVote, Role = seer});
                _db.ActionRoles.Add(new ActionRole{Action = dayVote, Role = auraseer});
                _db.ActionRoles.Add(new ActionRole{Action = dayVote, Role = priest});
                _db.ActionRoles.Add(new ActionRole{Action = dayVote, Role = doctor});
                _db.ActionRoles.Add(new ActionRole{Action = dayVote, Role = werewolf});
                _db.ActionRoles.Add(new ActionRole{Action = dayVote, Role = werewolfshaman});
                _db.ActionRoles.Add(new ActionRole{Action = dayVote, Role = alphawerewolf});
                _db.ActionRoles.Add(new ActionRole{Action = dayVote, Role = werewolfseer});
                _db.ActionRoles.Add(new ActionRole{Action = dayVote, Role = medium});
                _db.ActionRoles.Add(new ActionRole{Action = dayVote, Role = bodyguard});
                _db.ActionRoles.Add(new ActionRole{Action = dayVote, Role = jailer});
                _db.ActionRoles.Add(new ActionRole{Action = dayVote, Role = fool});
                _db.ActionRoles.Add(new ActionRole{Action = dayVote, Role = headhunter});
                _db.ActionRoles.Add(new ActionRole{Action = dayVote, Role = serialkiller});
                _db.ActionRoles.Add(new ActionRole{Action = dayVote, Role = gunner});

                Action holywater = new Action{
                    Id = 2,
                    Name = ACTION_HOLYWATER,
                    Description = "Throw holy water to kill a werewolf",
                };
                _db.Actions.Add(holywater);
                _db.ActionRoles.Add(new ActionRole{Action = holywater, Role = priest});

                Action shoot = new Action{
                    Id = 3,
                    Name = ACTION_SHOOT,
                    Description = "Shoot to kill a player",
                };
                _db.Actions.Add(shoot);
                _db.ActionRoles.Add(new ActionRole{Action = shoot, Role = gunner});

                Action jail = new Action{
                    Id = 4,
                    Name = ACTION_JAIL,
                    Description = "Jail a player at night",
                };
                _db.Actions.Add(jail);
                _db.ActionRoles.Add(new ActionRole{Action = jail, Role = jailer});

                Action enchant = new Action{
                    Id = 5,
                    Name = ACTION_ENCHANT,
                    Description = "Enchant a player at night",
                };
                _db.Actions.Add(enchant);
                _db.ActionRoles.Add(new ActionRole{Action = enchant, Role = werewolfshaman});

                Action nightVote = new Action{
                    Id = 6,
                    Name = ACTION_NIGHT_VOTE,
                    Description = "Werewolf vote to kill a player in a night time",
                };
                _db.Actions.Add(nightVote);
                _db.ActionRoles.Add(new ActionRole{Action = nightVote, Role = werewolf});
                _db.ActionRoles.Add(new ActionRole{Action = nightVote, Role = werewolfshaman});
                _db.ActionRoles.Add(new ActionRole{Action = nightVote, Role = alphawerewolf});
                _db.ActionRoles.Add(new ActionRole{Action = nightVote, Role = werewolfseer});

                Action guard = new Action{
                    Id = 7,
                    Name = ACTION_GUARD,
                    Description = "Protect a player",
                };
                _db.Actions.Add(guard);
                _db.ActionRoles.Add(new ActionRole{Action = guard, Role = bodyguard});

                Action heal = new Action{
                    Id = 8,
                    Name = ACTION_HEAL,
                    Description = "Heal a player",
                };
                _db.Actions.Add(heal);
                _db.ActionRoles.Add(new ActionRole{Action = heal, Role = doctor});

                Action kill = new Action{
                    Id = 9,
                    Name = ACTION_KILL,
                    Description = "Kill a player",
                };
                _db.Actions.Add(kill);
                _db.ActionRoles.Add(new ActionRole{Action = kill, Role = serialkiller});

                Action reveal = new Action{
                    Id = 10,
                    Name = ACTION_REVEAL,
                    Description = "Reveal a player's role",
                };
                _db.Actions.Add(reveal);
                _db.ActionRoles.Add(new ActionRole{Action = reveal, Role = seer});
                _db.ActionRoles.Add(new ActionRole{Action = reveal, Role = werewolfseer});

                Action aura = new Action{
                    Id = 11,
                    Name = ACTION_AURA,
                    Description = "See a player's aura",
                };
                _db.Actions.Add(aura);
                _db.ActionRoles.Add(new ActionRole{Action = aura, Role = auraseer});

                Action revive = new Action{
                    Id = 12,
                    Name = ACTION_REVIVE,
                    Description = "Revive a dead player",
                };
                _db.Actions.Add(revive);
                _db.ActionRoles.Add(new ActionRole{Action = revive, Role = medium});

                _db.SaveChanges();
            }
            _actionInitialized = true;
        }
        private void InitRoles()
        {
            if (_db == null) {
              return;
            }
            if ( _db.Roles.Count() == 0) {
                _db.Roles.Add(new Role
                {
                    Id = 1,
                    Name = ROLE_SEER,
                    Description = "Every night can uncover a role of a player they choose.",
                    Type = Role.TypeEnum.VillagerEnum
                });
                _db.Roles.Add(new Role
                {
                    Id = 2,
                    Name = ROLE_AURA_SEER,
                    Description = "Every night can uncover the aura of a player they choose.",
                    Type = Role.TypeEnum.VillagerEnum
                });
                _db.Roles.Add(new Role
                {
                    Id = 3,
                    Name = ROLE_PRIEST,
                    Description = "Can throw \"Holy water\" on a player they pick during the day once each game. If they hit a werewolf, the werewolf dies; otherwise, the priest dies instead.",
                    Type = Role.TypeEnum.VillagerEnum
                });
                _db.Roles.Add(new Role
                {
                    Id = 4,
                    Name = ROLE_DOCTOR,
                    Description = "Every night can protect a player from dying that night.",
                    Type = Role.TypeEnum.VillagerEnum
                });
                _db.Roles.Add(new Role
                {
                    Id = 5,
                    Name = ROLE_WEREWOLF,
                    Description = "Votes every night with other werewolves who to kill.",
                    Type = Role.TypeEnum.WolfEnum
                });
                _db.Roles.Add(new Role
                {
                    Id = 6,
                    Name = ROLE_WEREWOLF_SHAMAN,
                    Description = "Can enchant a player every day. Overnight, that player will appear as a werewolf to seers and aura seers.",
                    Type = Role.TypeEnum.WolfEnum
                });
                _db.Roles.Add(new Role
                {
                    Id = 7,
                    Name = ROLE_ALPHA_WEREWOLF,
                    Description = "Same as the Werewolf role, but if the werewolves can't decide who to kill (draw while voting), the alpha's vote is worth double.",
                    Type = Role.TypeEnum.WolfEnum
                });
                _db.Roles.Add(new Role
                {
                    Id = 8,
                    Name = ROLE_WEREWOLF_SEER,
                    Description = "Can see the role of a player once per night.",
                    Type = Role.TypeEnum.WolfEnum
                });
                _db.Roles.Add(new Role
                {
                    Id = 9,
                    Name = ROLE_MEDIUM,
                    Description = "Can talk to the dead players and revive one of them once each game.",
                    Type = Role.TypeEnum.VillagerEnum
                });
                _db.Roles.Add(new Role
                {
                    Id = 10,
                    Name = ROLE_BODYGUARD,
                    Description = "Can protect a player he picks. If that player is attacked that night, the bodyguard gets attacked instead. The bodyguard survives the first attack, but dies on the second one.",
                    Type = Role.TypeEnum.VillagerEnum
                });
                _db.Roles.Add(new Role
                {
                    Id = 11,
                    Name = ROLE_JAILER,
                    Description = "Can put any player in jail during the day. The night after, that player can't use their role ability and the jailer will be able to talk to their prisoner anonymously. Once every game, he can kill the prisoner.",
                    Type = Role.TypeEnum.VillagerEnum
                });
                _db.Roles.Add(new Role
                {
                    Id = 12,
                    Name = ROLE_FOOL,
                    Description = "If the fool gets killed by getting lynched, he wins.",
                    Type = Role.TypeEnum.NeutralEnum
                });
                _db.Roles.Add(new Role
                {
                    Id = 13,
                    Name = ROLE_HEAD_HUNTER,
                    Description = "If the headhunter's target gets lynched, he wins",
                    Type = Role.TypeEnum.NeutralEnum
                });
                _db.Roles.Add(new Role
                {
                    Id = 14,
                    Name = ROLE_SERIAL_KILLER,
                    Description = "Can kill a player he chooses every night. If he is the last player alive, he wins. That said, werewolves can't kill the serial killer.",
                    Type = Role.TypeEnum.NeutralEnum
                });
                _db.Roles.Add(new Role
                {
                    Id = 15,
                    Name = ROLE_GUNNER,
                    Description = "You have two bullets which you can use to kill somebody. The shots are very loud so that your role will be revealed after the first shot.",
                    Type = Role.TypeEnum.NeutralEnum
                });
                _db.SaveChanges();
            }
            _roleInitialized = true;
        }
    }
}

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
        private void InitActions()
        {
            if (_db == null || _roleInitialized == false) {
                return;
            }
            if (_db.Actions.Count() == 0) {

                Role seer = GetRoleByName("Seer");
                Role auraseer = GetRoleByName("Aura Seer");
                Role priest = GetRoleByName("Priest");
                Role doctor = GetRoleByName("Doctor");
                Role werewolf = GetRoleByName("Werewolf");
                Role werewolfshaman = GetRoleByName("Werewolf Shaman");
                Role alphawerewolf = GetRoleByName("Alpha Werewolf");
                Role werewolfseer = GetRoleByName("Werewolf Seer");
                Role medium = GetRoleByName("Medium");
                Role bodyguard = GetRoleByName("Bodyguard");
                Role jailer = GetRoleByName("Jailer");
                Role fool = GetRoleByName("Fool");
                Role headhunter = GetRoleByName("Head Hunter");
                Role serialkiller = GetRoleByName("Serial Killer");
                Role gunner = GetRoleByName("Gunner");

                Action dayVote = new Action{
                    Id = 1,
                    Name = "Day Vote",
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
                    Name = "Throw holy-water",
                    Description = "Throw holy water to kill a werewolf",
                };
                _db.Actions.Add(holywater);
                _db.ActionRoles.Add(new ActionRole{Action = holywater, Role = priest});

                Action shoot = new Action{
                    Id = 3,
                    Name = "Shoot",
                    Description = "Shoot to kill a player",
                };
                _db.Actions.Add(shoot);
                _db.ActionRoles.Add(new ActionRole{Action = shoot, Role = gunner});

                Action jail = new Action{
                    Id = 4,
                    Name = "Jail",
                    Description = "Jail a player at night",
                };
                _db.Actions.Add(jail);
                _db.ActionRoles.Add(new ActionRole{Action = jail, Role = jailer});

                Action enchant = new Action{
                    Id = 5,
                    Name = "Enchant",
                    Description = "Enchant a player at night",
                };
                _db.Actions.Add(enchant);
                _db.ActionRoles.Add(new ActionRole{Action = enchant, Role = werewolfshaman});

                Action nightVote = new Action{
                    Id = 6,
                    Name = "Night Vote",
                    Description = "Werewolf vote to kill a player in a night time",
                };
                _db.Actions.Add(nightVote);
                _db.ActionRoles.Add(new ActionRole{Action = nightVote, Role = werewolf});
                _db.ActionRoles.Add(new ActionRole{Action = nightVote, Role = werewolfshaman});
                _db.ActionRoles.Add(new ActionRole{Action = nightVote, Role = alphawerewolf});
                _db.ActionRoles.Add(new ActionRole{Action = nightVote, Role = werewolfseer});

                Action guard = new Action{
                    Id = 7,
                    Name = "Guard",
                    Description = "Protect a player",
                };
                _db.Actions.Add(guard);
                _db.ActionRoles.Add(new ActionRole{Action = guard, Role = bodyguard});

                Action heal = new Action{
                    Id = 8,
                    Name = "Heal",
                    Description = "Heal a player",
                };
                _db.Actions.Add(heal);
                _db.ActionRoles.Add(new ActionRole{Action = guard, Role = doctor});

                Action kill = new Action{
                    Id = 9,
                    Name = "Kill",
                    Description = "Kill a player",
                };
                _db.Actions.Add(kill);
                _db.ActionRoles.Add(new ActionRole{Action = kill, Role = serialkiller});

                Action reveal = new Action{
                    Id = 10,
                    Name = "Reveal",
                    Description = "Reveal a player's role",
                };
                _db.Actions.Add(reveal);
                _db.ActionRoles.Add(new ActionRole{Action = reveal, Role = seer});
                _db.ActionRoles.Add(new ActionRole{Action = reveal, Role = werewolfseer});

                Action aura = new Action{
                    Id = 11,
                    Name = "Aura",
                    Description = "See a player's aura",
                };
                _db.Actions.Add(aura);
                _db.ActionRoles.Add(new ActionRole{Action = aura, Role = auraseer});

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
                    Name = "Seer",
                    Description = "Every night can uncover a role of a player they choose.",
                    Type = Role.TypeEnum.VillagerEnum
                });
                _db.Roles.Add(new Role
                {
                    Id = 2,
                    Name = "Aura Seer",
                    Description = "Every night can uncover the aura of a player they choose.",
                    Type = Role.TypeEnum.VillagerEnum
                });
                _db.Roles.Add(new Role
                {
                    Id = 3,
                    Name = "Priest",
                    Description = "Can throw \"Holy water\" on a player they pick during the day once each game. If they hit a werewolf, the werewolf dies; otherwise, the priest dies instead.",
                    Type = Role.TypeEnum.VillagerEnum
                });
                _db.Roles.Add(new Role
                {
                    Id = 4,
                    Name = "Doctor",
                    Description = "Every night can protect a player from dying that night.",
                    Type = Role.TypeEnum.VillagerEnum
                });
                _db.Roles.Add(new Role
                {
                    Id = 5,
                    Name = "Werewolf",
                    Description = "Votes every night with other werewolves who to kill.",
                    Type = Role.TypeEnum.WolfEnum
                });
                _db.Roles.Add(new Role
                {
                    Id = 6,
                    Name = "Werewolf Shaman",
                    Description = "Can enchant a player every day. Overnight, that player will appear as a werewolf to seers and aura seers.",
                    Type = Role.TypeEnum.WolfEnum
                });
                _db.Roles.Add(new Role
                {
                    Id = 7,
                    Name = "Alpha Werewolf",
                    Description = "Same as the Werewolf role, but if the werewolves can't decide who to kill (draw while voting), the alpha's vote is worth double.",
                    Type = Role.TypeEnum.WolfEnum
                });
                _db.Roles.Add(new Role
                {
                    Id = 8,
                    Name = "Werewolf Seer",
                    Description = "Can see the role of a player once per night.",
                    Type = Role.TypeEnum.WolfEnum
                });
                _db.Roles.Add(new Role
                {
                    Id = 9,
                    Name = "Medium",
                    Description = "Can talk to the dead players and revive one of them once each game.",
                    Type = Role.TypeEnum.VillagerEnum
                });
                _db.Roles.Add(new Role
                {
                    Id = 10,
                    Name = "Bodyguard",
                    Description = "Can protect a player he picks. If that player is attacked that night, the bodyguard gets attacked instead. The bodyguard survives the first attack, but dies on the second one.",
                    Type = Role.TypeEnum.VillagerEnum
                });
                _db.Roles.Add(new Role
                {
                    Id = 11,
                    Name = "Jailer",
                    Description = "Can put any player in jail during the day. The night after, that player can't use their role ability and the jailer will be able to talk to their prisoner anonymously. Once every game, he can kill the prisoner.",
                    Type = Role.TypeEnum.VillagerEnum
                });
                _db.Roles.Add(new Role
                {
                    Id = 12,
                    Name = "Fool",
                    Description = "If the fool gets killed by getting lynched, he wins.",
                    Type = Role.TypeEnum.NeutralEnum
                });
                _db.Roles.Add(new Role
                {
                    Id = 13,
                    Name = "Head Hunter",
                    Description = "If the headhunter's target gets lynched, he wins",
                    Type = Role.TypeEnum.NeutralEnum
                });
                _db.Roles.Add(new Role
                {
                    Id = 14,
                    Name = "Serial Killer",
                    Description = "Can kill a player he chooses every night. If he is the last player alive, he wins. That said, werewolves can't kill the serial killer.",
                    Type = Role.TypeEnum.NeutralEnum
                });
                _db.Roles.Add(new Role
                {
                    Id = 15,
                    Name = "Gunner",
                    Description = "You have two bullets which you can use to kill somebody. The shots are very loud so that your role will be revealed after the first shot.",
                    Type = Role.TypeEnum.NeutralEnum
                });
                _db.SaveChanges();
            }
            _roleInitialized = true;
        }
    }
}
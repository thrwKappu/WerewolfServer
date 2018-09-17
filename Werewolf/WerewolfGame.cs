
using System;
using System.Collections.Generic;
using System.Text;
using DNWS.Werewolf;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace DNWS.Werewolf
{
    class WerewolfGame
    {
        private static WerewolfGame _instance = null;
        private static bool _dbinitialized = false;
        WerewolfContext _db = null;
        
        public static WerewolfGame GetInstance()
        {
            if (_instance == null) {
                _instance = new WerewolfGame();
            }
            return _instance;
        }

        private WerewolfGame()
        {
            try {
                if (_db == null) {
                    _db = new WerewolfContext();
                }
                //FIXME, should move to database init part
                if(!_dbinitialized) {
                    InitRoles();
                }
            } catch (Exception ex) {
                Console.Out.WriteLine(ex.ToString());
            }
        }
        private void InitRoles()
        {
            if (_db == null) {
              return;
            }
            if( _db.Roles.Count() == 0) {
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
                    Name = "Werewolf shaman",
                    Description = "Can enchant a player every day. Overnight, that player will appear as a werewolf to seers and aura seers.",
                    Type = Role.TypeEnum.WolfEnum
                });
                _db.Roles.Add(new Role
                {
                    Id = 7,
                    Name = "Alpha werewolf",
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
                    Name = "Head hunter",
                    Description = "If the headhunter's target gets lynched, he wins",
                    Type = Role.TypeEnum.NeutralEnum
                });
                _db.Roles.Add(new Role
                {
                    Id = 14,
                    Name = "Serial killer",
                    Description = "Can kill a player he chooses every night. If he is the last player alive, he wins. That said, werewolves can't kill the serial killer.",
                    Type = Role.TypeEnum.NeutralEnum
                });
                _db.SaveChanges();
            }
            _dbinitialized = true;
        }

    }
}
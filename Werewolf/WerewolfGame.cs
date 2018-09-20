
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
        private static WerewolfGame _instance = null;
        private static bool _roleInitialized = false;
        private static bool _actionInitialized = false;
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
                if (!_roleInitialized) {
                    InitRoles();
                }
                if (!_actionInitialized) {
                    InitActions();
                }
            } catch (Exception ex) {
                Console.Out.WriteLine(ex.ToString());
            }
        }

        private static T DeepClone<T>(T obj)
        {
            if (obj == null)
            {
                return default(T);
            }
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Position = 0;
                return (T)formatter.Deserialize(ms);
            }
        }
        public List<Player> GetPlayers()
        {
            return DeepClone<List<Player>>(_db.Players.ToList());
        }
        public Player GetPlayer(string id)
        {
            long lid = Int64.Parse(id);
            return DeepClone<Player>(_db.Players.Where(player => player.Id == lid).Single());
        }
        public Player GetPlayerByName(string name)
        {
            return DeepClone<Player>(_db.Players.Where(player => player.Name.ToLower() == name.ToLower()).Single());
        }
        public Boolean IsPlayerExists(string name)
        {
            if(_db.Players.Where(player => player.Name.ToLower() == name.ToLower()).Count() > 0) {
                return true;
            }
            return false;
        }
        public Player GetPlayerBySession(string session)
        {
            return DeepClone<Player>(_db.Players.Where(player => player.Session == session).Single());
        }
        public List<Player> GetPlayerByGame(string gameid)
        {
            long gid = Int64.Parse(gameid);
            return DeepClone<List<Player>>(_db.Games.Where(game => game.Id == gid).Include(game => game.Players).Single().Players.ToList());
        }
        public void AddPlayer(Player player)
        {
            _db.Players.Add(player);
            _db.SaveChanges();
        }
        public void UpdatePlayer(Player player)
        {
            _db.Players.Update(player);
            _db.SaveChanges();
        }
        public void DeletePlayer(string id)
        {
            long lid = Int64.Parse(id);
            Player player = _db.Players.Where(p => p.Id == lid).FirstOrDefault();
            if (player != null) {
                _db.Players.Remove(player);
            } else {
                throw new Exception();
            }
            _db.SaveChanges();
        }
        public Player PostPlayerLogin(Player player)
        {
            return null;
        }
        public List<Action> GetActions()
        {
            return DeepClone<List<Action>>(_db.Actions.Include(ar => ar.ActionRoles).ThenInclude(r => r.Role).ToList());
        }

        public Action GetAction(string id)
        {
            // Throws exception
            long lid = Int64.Parse(id);
            return DeepClone<Action>(_db.Actions.Where(action => action.Id == lid).Include(ar => ar.ActionRoles).ThenInclude(r => r.Role).Single());
        }
        private Action GetActionByName(string name)
        {
            return DeepClone<Action>(_db.Actions.Where(action => action.Name == name).Single());
        }
        public List<Action> GetActionByRoleId(string id)
        {
            long lid = Int64.Parse(id);
            return DeepClone<List<Action>>(_db.Actions.Where(action => action.Roles.Any(role => role.Id == lid)).ToList());
        }

        public List<Role> GetRoles()
        {
            return DeepClone<List<Role>>(_db.Roles.Include(ar => ar.ActionRoles).ThenInclude(a => a.Action).ToList());
        }
        public Role GetRole(string id)
        {
            // Throws exception
            long lid = Int64.Parse(id);
            return DeepClone<Role>(_db.Roles.Where(role => role.Id == lid).Include(ar => ar.ActionRoles).ThenInclude(a => a.Action).Single());
        }
        private Role GetRoleByName(string name)
        {
            return DeepClone<Role>(_db.Roles.Where(role => role.Name == name).First());
        }
    }
}
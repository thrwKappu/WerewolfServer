
using System;
using System.Collections.Generic;
using System.Text;
using DNWS.Werewolf;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace DNWS.Werewolf
{
    public class WerewolfEvent
    {
        public const int GAME_CREATED = 1;
        public const int PLAYER_JOIN = 2;
        public const int GAME_STARTED = 3;
        public const int GAME_DELETED = 4;
        private int _event;
        private long _gameid;
        public int Event
        {
            get { return _event; }
        }
        public long GameId
        {
            get { return _gameid; }
        }
        public WerewolfEvent(int e, long g)
        {
            _event = e;
            _gameid = g;
        }
    }
    public partial class WerewolfGame : IObservable<WerewolfEvent>
    {
        public const string OUTCOME_TARGET_DEAD = "Target Dead";
        public const string OUTCOME_PLAYER_DEAD = "Player Dead";
        public const string OUTCOME_OTHER_DEAD = "Other Player Dead";
        public const string OUTCOME_NOTHING = "Nothing Happended";
        public const string OUTCOME_PERFORMED = "Action Performed";
        public const string OUTCOME_ENCHANTED = "Enchanted";
        public const string OUTCOME_JAILED = "Jailed";
        public const string OUTCOME_REVEALED = "Revealed";
        public const string OUTCOME_AURA_UNKOWN = "Unknown";
        public const string OUTCOME_AURA_WEREWOLF = "Werewolf";
        public const string OUTCOME_AURA_VILLAGER = "Villager";
        public const string OUTCOME_NOT_VALID = "Not Valid Action";
        public const string OUTCOME_TARGET_NOT_ALIVED = "Target Is Not Alived";
        internal class Unsubscriber<BaggageInfo> : IDisposable
        {
            private List<IObserver<BaggageInfo>> _observers;
            private IObserver<BaggageInfo> _observer;

            internal Unsubscriber(List<IObserver<BaggageInfo>> observers, IObserver<BaggageInfo> observer)
            {
                this._observers = observers;
                this._observer = observer;
            }

            public void Dispose()
            {
                if (_observers.Contains(_observer))
                    _observers.Remove(_observer);
            }
        }
        private static bool _roleInitialized = false;
        private static bool _actionInitialized = false;
        private static Int64 _currentGameId = 0;
        public static int MAX_PLAYERS = 2;
        public static int GAME_COUNTDOWN_PERIOD = 5;
        public static int GAME_DAY_PERIOD = 10;
        public static int GAME_NIGHT_PERIOD = 10;
        private static WerewolfContext _db;

        private static List<IObserver<WerewolfEvent>> observers = null;

        public Int64 CurrentGameId
        {
            get
            {
                return _currentGameId;
            }
            set
            {
                _currentGameId = value;
            }
        }
        public WerewolfGame(WerewolfContext db)
        {
            // DI
            _db = db;
            try
            {
                if (!_roleInitialized)
                {
                    InitRoles();
                }
                if (!_actionInitialized)
                {
                    InitActions();
                }
                //FIXME, should move to database init part
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.ToString());
            }
            if (observers == null)
            {
                observers = new List<IObserver<WerewolfEvent>>();
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
        public List<Game> GetGames()
        {
            return DeepClone<List<Game>>(_db.Games.Include(game => game.Players).ToList());
        }
        public Game GetGame(string id)
        {
            long lid = Int64.Parse(id);
            return DeepClone<Game>(_db.Games.Include(game => game.Players).ThenInclude(player => player.Role).Where(game => game.GameId == lid).ToList()[0]);
        }
        public void SetGameDay(string id, int day)
        {
            long lid = Int64.Parse(id);
            Game g = _db.Games.Where(game => game.GameId == lid).ToList()[0];
            g.Day = day;
            _db.SaveChanges();
        }
        public void SetGamePeriod(string id, Game.PeriodEnum p)
        {
            long lid = Int64.Parse(id);
            Game g = _db.Games.Where(game => game.GameId == lid).ToList()[0];
            g.Period = p;
            _db.SaveChanges();
        }
        public void SetGameOutcome(string id, Game.OutcomeEnum o)
        {
            long lid = Int64.Parse(id);
            Game g = _db.Games.Where(game => game.GameId == lid).ToList()[0];
            g.Outcome = o;
            _db.SaveChanges();
        }
        public void SetGameStatus(string id, Game.StatusEnum s)
        {
            long lid = Int64.Parse(id);
            Game g = _db.Games.Where(game => game.GameId == lid).ToList()[0];
            g.Status = s;
            _db.SaveChanges();
        }
        public void StartGame(string id)
        {
            long lid = Int64.Parse(id);
            Game game = _db.Games.Where(g => g.GameId == lid).ToList()[0];
            if (game != null)
            {
                game.Status = Game.StatusEnum.PlayingEnum;
                game.ResetDayVoteList();
                game.ResetNightVoteList();
            }
            else
            {
                throw new Exception();
            }
            _db.SaveChanges();
            NotifyObserver(WerewolfEvent.GAME_STARTED, game.GameId);
        }

        public void DeleteGame(string id)
        {
            long lid = Int64.Parse(id);
            Game game = _db.Games.Where(g => g.GameId == lid).ToList()[0];
            if (game != null)
            {
                _db.Games.Remove(game);
            }
            else
            {
                throw new Exception();
            }
            _db.SaveChanges();
            NotifyObserver(WerewolfEvent.GAME_DELETED, game.GameId);
        }
        public Game CreateGame()
        {
            Game game = new Game();
            game.Hash = Guid.NewGuid().ToString();
            game.Status = Game.StatusEnum.WaitingEnum;
            //game.Players = new List<Player>();
            game.Period = Game.PeriodEnum.ProcessingEnum;
            _db.Games.Add(game);
            _db.SaveChanges();
            NotifyObserver(WerewolfEvent.GAME_CREATED, game.GameId);
            return DeepClone<Game>(_db.Games.OrderBy(g => g.GameId).Last());
        }
        public Game JoinGame(Game g, Player p)
        {
            Game game = _db.Games.Where(_g => _g.GameId == g.GameId).Include(_game => _game.Players).ToList()[0];
            if (game.Status != Game.StatusEnum.WaitingEnum)
            {
                throw new Exception("Game is already ended or running");
            }
            List<Player> players = game.Players.ToList();
            if (players.Count >= MAX_PLAYERS)
            {
                throw new Exception("Game is fulled already");
            }
            Player player = _db.Players.Where(_p => _p.Id == p.Id).ToList()[0];
            if (players.Contains(player))
            {
                throw new Exception("User in game already");
            }
            //player.Game = game.GameId.ToString();
            player.Status = Player.StatusEnum.AliveEnum;
            players.Add(player);
            game.Players = players;
            _db.Games.Update(game);
            _db.Players.Update(player);
            _db.SaveChanges();
            NotifyObserver(WerewolfEvent.PLAYER_JOIN, game.GameId);
            return DeepClone<Game>(game);
        }
        public Game LeaveGame(Game g, Player p)
        {
            Game game = _db.Games.Where(_g => _g.GameId == g.GameId).ToList()[0];
            List<Player> players = game.Players.ToList();
            Player player = _db.Players.Where(_p => _p.Id == p.Id).ToList()[0];
            players.Remove(player);
            player.Status = Player.StatusEnum.NotInGameEnum;
            game.Players = players;
            _db.Games.Update(game);
            _db.Players.Update(player);
            _db.SaveChanges();
            return DeepClone<Game>(game);
        }
        public List<Player> GetPlayers()
        {
            return DeepClone<List<Player>>(_db.Players.ToList());
        }
        public Player GetPlayer(string id)
        {
            long lid = Int64.Parse(id);
            return DeepClone<Player>(_db.Players.Where(player => player.Id == lid).Include(player => player.Role).ToList()[0]);
        }
        public bool IsPlayerDead(string id)
        {
            var deadReasonArray = new[]
            {
                Player.StatusEnum.VoteDeadEnum,
                Player.StatusEnum.ShotDeadEnum,
                Player.StatusEnum.JailDeadEnum,
                Player.StatusEnum.HolyDeadEnum,
                Player.StatusEnum.KillDeadEnum,
            };
            long lid = Int64.Parse(id);
            Player player = _db.Players.Where(_p => _p.Id == lid).ToList()[0];
            foreach (Player.StatusEnum reason in deadReasonArray)
            {
                if (player.Status == reason)
                {
                    return true;
                }
            }
            return false;
        }
        public Player GetPlayerByName(string name)
        {
            return DeepClone<Player>(_db.Players.Where(player => player.Name.ToUpper() == name.ToUpper()).ToList()[0]);
        }
        public Boolean IsPlayerExists(string name)
        {
            if (_db.Players.Where(player => player.Name.ToUpper() == name.ToUpper()).Count() > 0)
            {
                return true;
            }
            return false;
        }
        public Player GetPlayerBySession(string session)
        {
            //return DeepClone<Player>(_db.Players.Where(player => player.Session == session).Include(player => player.Game).ThenInclude(game => game.Players).ThenInclude(p => p.Role).ThenInclude(r => r.ActionRoles).ToList()[0]);
            List<Player> players = _db.Players.Where(player => player.Session == session).Include(player => player.Game).ThenInclude(game => game.Players).ThenInclude(p => p.Role).ThenInclude(r => r.ActionRoles).ToList();
            if (players.Count > 0)
            {
                return DeepClone<Player>(players[0]);
            }
            throw new Exception();
        }
        public List<Player> GetPlayerByGame(string gameid)
        {
            long gid = Int64.Parse(gameid);
            Game _game = _db.Games.Where(game => game.GameId == gid).Include(game => game.Players).ToList()[0];
            return DeepClone<List<Player>>(_game.Players.ToList());
        }
        public void AddPlayer(Player player)
        {
            _db.Players.Add(player);
            _db.SaveChanges();
        }
        public void UpdatePlayer(Player player)
        {
            Player p = _db.Players.Where(pr => pr.Id == player.Id).ToList()[0];
            if (p == null)
            {
                throw new Exception("User not found");
            }
            if (p.Name != player.Name)
            {
                throw new Exception("Not allow to change name");
            }
            if (p != null)
            {
                p.Password = player.Password;
                p.Session = player.Session;
                if (player.Role != null)
                {
                    p.Role = _db.Roles.Where(r => r.Id == player.Role.Id).ToList()[0];
                }
                _db.Players.Update(p);
                _db.SaveChanges();
            }
            else
            {
                throw new Exception("Player not found");
            }
        }
        public void DeletePlayer(string id)
        {
            long lid = Int64.Parse(id);
            Player player = _db.Players.Where(p => p.Id == lid).ToList()[0];
            if (player != null)
            {
                _db.Players.Remove(player);
            }
            else
            {
                throw new Exception();
            }
            _db.SaveChanges();
        }
        public void SetPlayerStatus(string id, Player.StatusEnum status)
        {
            long lid = Int64.Parse(id);
            Player player = _db.Players.Where(p => p.Id == lid).ToList()[0];
            player.Status = status;
            _db.SaveChanges();
        }
        public List<Action> GetActions()
        {
            return DeepClone<List<Action>>(_db.Actions.Include(ar => ar.ActionRoles).ThenInclude(r => r.Role).ToList());
        }
        public Action GetAction(string id)
        {
            // Throws exception
            long lid = Int64.Parse(id);
            return DeepClone<Action>(_db.Actions.Where(action => action.Id == lid).Include(ar => ar.ActionRoles).ThenInclude(r => r.Role).ToList()[0]);
        }
        private Action GetActionByName(string name)
        {
            return DeepClone<Action>(_db.Actions.Where(action => action.Name == name).ToList()[0]);
        }
        public List<Action> GetActionByRoleId(string id)
        {
            long lid = Int64.Parse(id);
            //return DeepClone<List<Action>>(_db.Actions.Where(action => action.Roles.Any(role => role.RoleId == lid)).ToList());
            return DeepClone<List<Action>>(_db.Roles.Where(r => r.Id == lid).ToList()[0].Actions.ToList());
        }
        public List<Role> GetRoles()
        {
            return DeepClone<List<Role>>(_db.Roles.Include(ar => ar.ActionRoles).ThenInclude(a => a.Action).ToList());
        }
        public Role GetRole(string id)
        {
            // Throws exception
            long lid = Int64.Parse(id);
            return DeepClone<Role>(_db.Roles.Where(role => role.Id == lid).Include(ar => ar.ActionRoles).ThenInclude(a => a.Action).ToList()[0]);
        }
        public Role GetRoleByName(string name)
        {
            return DeepClone<Role>(_db.Roles.Where(role => role.Name == name).ToList()[0]);
        }
        public string PostAction(string sessionID, string actionID, string targetID)
        {
            Game game;
            Player player;
            Player target;
            DNWS.Werewolf.Action action;
            Role role;

            try
            {
                player = GetPlayerBySession(sessionID);
            }
            catch (Exception)
            {
                throw new Exception("Player not found.");
            }
            if (player.Status != Player.StatusEnum.AliveEnum)
            {
                throw new Exception("Player is not alived.");
            }
            try
            {
                action = GetAction(actionID);
            }
            catch (Exception)
            {
                throw new Exception("Action not found.");
            }
            try
            {
                target = GetPlayer(targetID);
            }
            catch (Exception)
            {
                throw new Exception("Target not found.");
            }
            if (target.Id == player.Id)
            {
                throw new Exception("You can't perform on yourself.");
            }
            game = GetGame(player.Game.GameId.ToString());
            if (game == null)
            {
                throw new Exception("Player is not in a game.");
            }
            if (game.Period == Game.PeriodEnum.ProcessingEnum)
            {
                throw new Exception("Please wait for processing.");
            }
            role = GetRole(player.Role.Id.ToString());

            if (role == null)
            {
                throw new Exception("Player does not have any role.");
            }
            List<int> action_ids = role.ActionRoles.Select(ar => ar.Action.Id).ToList();
            //if (!role.Actions.Contains(action))
            if (!action_ids.Contains(action.Id))
            {
                throw new Exception("Player's role does not have this action.");
            }
            if (game.Status != Game.StatusEnum.PlayingEnum)
            {
                throw new Exception("Game is not playable.");
            }
            if (game.Period == Game.PeriodEnum.DayEnum)
            {
                if (target.Status != Player.StatusEnum.AliveEnum)
                {
                    return OUTCOME_TARGET_NOT_ALIVED;
                }
                if (action.Name == WerewolfGame.ACTION_DAY_VOTE)
                {
                    if (game.DayVoteList.ContainsKey(player) && game.DayVoteList[player] == target)
                    {
                        game.DayVoteList.Remove(player);
                    }
                    else
                    {
                        game.DayVoteList[player] = target;
                    }
                    return OUTCOME_PERFORMED;
                }
                else if (action.Name == WerewolfGame.ACTION_ENCHANT)
                {
                    if (game.Enchanted == target)
                    {
                        game.Enchanted = null;
                    }
                    else
                    {
                        game.Enchanted = target;
                    }
                    return OUTCOME_PERFORMED;
                }
                else if (action.Name == WerewolfGame.ACTION_JAIL)
                {
                    if (game.Jailed == target)
                    {
                        game.Jailed = null;
                    }
                    else
                    {
                        game.Jailed = target;
                    }
                    return OUTCOME_PERFORMED;
                }
                else if (action.Name == WerewolfGame.ACTION_SHOOT)
                {
                    SetPlayerStatus(target.Id.ToString(), Player.StatusEnum.ShotDeadEnum);
                    return OUTCOME_TARGET_DEAD;
                }
                else if (action.Name == WerewolfGame.ACTION_HOLYWATER)
                {
                    if ((new[] {WerewolfGame.ROLE_WEREWOLF,
                                WerewolfGame.ROLE_ALPHA_WEREWOLF,
                                WerewolfGame.ROLE_WEREWOLF_SEER,
                                WerewolfGame.ROLE_WEREWOLF_SHAMAN}).Contains(target.Role.Name))
                    {
                        SetPlayerStatus(target.Id.ToString(), Player.StatusEnum.HolyDeadEnum);
                        return OUTCOME_TARGET_DEAD;
                    }
                    else
                    {
                        SetPlayerStatus(player.Id.ToString(), Player.StatusEnum.HolyDeadEnum);
                        return OUTCOME_PLAYER_DEAD;
                    }
                }
            }
            else if (game.Period == Game.PeriodEnum.NightEnum)
            {
                if (game.Jailed != null && player.Id == game.Jailed.Id)
                {
                    return OUTCOME_PERFORMED;
                }
                if (action.Name == WerewolfGame.ACTION_REVIVE)
                {
                    if (IsPlayerDead(target.Id.ToString()))
                    {
                        throw new Exception("Target is not dead.");
                    }
                    if (game.ReviveByMedium == target)
                    {
                        game.ReviveByMedium = null;
                    }
                    else
                    {
                        game.ReviveByMedium = target;
                    }
                    return OUTCOME_PERFORMED;
                }
                if (target.Status != Player.StatusEnum.AliveEnum)
                {
                    throw new Exception("Target is not alived.");
                }
                if (action.Name == WerewolfGame.ACTION_NIGHT_VOTE)
                {
                    if (game.NightVoteList.ContainsKey(player) && game.NightVoteList[player] == target)
                    {
                        game.NightVoteList.Remove(player);
                    }
                    else
                    {
                        game.NightVoteList[player] = target;
                    }
                    return OUTCOME_PERFORMED;
                }
                else if (action.Name == WerewolfGame.ACTION_GUARD)
                {
                    if (game.ProtectedByBodyguard == target)
                    {
                        game.ProtectedByBodyguard = null;
                    }
                    else
                    {
                        game.ProtectedByBodyguard = target;
                    }
                    return OUTCOME_PERFORMED;
                }
                else if (action.Name == WerewolfGame.ACTION_HEAL)
                {
                    if (game.HealedByDoctor == target)
                    {
                        game.HealedByDoctor = null;
                    }
                    else
                    {
                        game.HealedByDoctor = target;
                    }
                    return OUTCOME_PERFORMED;
                }
                else if (action.Name == WerewolfGame.ACTION_KILL)
                {
                    if (game.KillBySerialKiller == target)
                    {
                        game.KillBySerialKiller = null;
                    }
                    else
                    {
                        game.KillBySerialKiller = target;
                    }
                    return OUTCOME_PERFORMED;
                }
                else if (action.Name == WerewolfGame.ACTION_REVEAL)
                {
                    if (target == game.Enchanted)
                    {
                        return OUTCOME_ENCHANTED;
                    }
                    return OUTCOME_REVEALED;
                }
                else if (action.Name == WerewolfGame.ACTION_AURA)
                {
                    if (new[] {
                        WerewolfGame.ROLE_GUNNER,
                        WerewolfGame.ROLE_JAILER,
                        WerewolfGame.ROLE_MEDIUM,
                        WerewolfGame.ROLE_ALPHA_WEREWOLF,
                        WerewolfGame.ROLE_HEAD_HUNTER,
                        WerewolfGame.ROLE_SERIAL_KILLER
                    }.Contains(target.Name))
                    {
                        return OUTCOME_AURA_UNKOWN;
                    }
                    else if (new[] {
                        WerewolfGame.ROLE_WEREWOLF,
                        WerewolfGame.ROLE_WEREWOLF_SEER,
                        WerewolfGame.ROLE_WEREWOLF_SHAMAN
                    }.Contains(target.Name))
                    {
                        return OUTCOME_AURA_WEREWOLF;
                    }
                    return OUTCOME_AURA_VILLAGER;
                }
            }
            return OUTCOME_NOT_VALID;
        }
        public IDisposable Subscribe(IObserver<WerewolfEvent> observer)
        {
            if (!observers.Contains(observer))
            {
                observers.Add(observer);
            }
            return new Unsubscriber<WerewolfEvent>(observers, observer);
        }
        public void NotifyObserver(int EventType, long GameId)
        {
            WerewolfEvent we = new WerewolfEvent(EventType, GameId);
            foreach (IObserver<WerewolfEvent> observer in observers)
            {
                observer.OnNext(we);
            }
        }
    }
}
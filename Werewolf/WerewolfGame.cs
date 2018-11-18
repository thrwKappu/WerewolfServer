
using System;
using System.Collections.Generic;
using System.Text;
using DNWS.Werewolf;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using OutcomeEnum = DNWS.Werewolf.Action.OutcomeEnum;

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
    public partial class WerewolfGame : IObservable<WerewolfEvent>, IDisposable
    {
        /* 
        public const string OUTCOME_TARGET_DEAD = "TargetDead";
        public const string OUTCOME_PLAYER_DEAD = "PlayerDead";
        public const string OUTCOME_OTHER_DEAD = "OtherPlayerDead";
        public const string OUTCOME_NOTHING = "NothingHappended";
        public const string OUTCOME_PERFORMED = "ActionPerformed";
        public const string OUTCOME_ENCHANTED = "Enchanted";
        public const string OUTCOME_JAILED = "Jailed";
        public const string OUTCOME_REVEALED = "Revealed";
        public const string OUTCOME_AURA_UNKOWN = "Unknown";
        public const string OUTCOME_AURA_WEREWOLF = "Werewolf";
        public const string OUTCOME_AURA_VILLAGER = "Villager";
        public const string OUTCOME_NOT_VALID = "NotValidAction";
        public const string OUTCOME_TARGET_NOT_ALIVED = "TargetIsNotAlived";
        */
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
        public static int DEFAULT_MAX_PLAYERS = 2;
        private int _max_players = DEFAULT_MAX_PLAYERS;
        public int max_players {  get => _max_players; set => _max_players = value;}
        public static int GAME_COUNTDOWN_PERIOD = 5;
        public static int GAME_DAY_PERIOD = 30;
        public static int GAME_NIGHT_PERIOD = 30;
        public static int GAME_MAX_DAY = 10;

        private ChatMessageManager chatManager;
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
        public WerewolfGame()
        {
            string max = null;
            if ((max = System.Environment.GetEnvironmentVariable("WEREWOLF_MAX_PLAYER")) != null)
            {
                try
                {
                    max_players = int.Parse(max);
                }
                catch
                {
                    Console.WriteLine("Invalid WEREWOLF_MAX_PLAYER variable, fall back to default value");
                }
            }
            using (WerewolfContext _db = new WerewolfContext())
            {
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
            }
            if (observers == null)
            {
                observers = new List<IObserver<WerewolfEvent>>();
            }
            chatManager = new ChatMessageManager();
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

        private bool IsWerewolf(string role)
        {
            return ((new[] {WerewolfGame.ROLE_WEREWOLF,
                        WerewolfGame.ROLE_ALPHA_WEREWOLF,
                        WerewolfGame.ROLE_WEREWOLF_SEER,
                        WerewolfGame.ROLE_WEREWOLF_SHAMAN}).Contains(role));
        }
        public List<Game> GetGames()
        {
            using (WerewolfContext _db = new WerewolfContext())
            {
                return DeepClone<List<Game>>(_db.Games.Include(game => game.Players).ToList());
            }
        }
        public Game GetGame(string id)
        {
            long lid = Int64.Parse(id);
            using (WerewolfContext _db = new WerewolfContext())
            {
                return DeepClone<Game>(_db.Games.Include(game => game.Players).ThenInclude(player => player.Role).Where(game => game.Id == lid).ToList()[0]);
            }
        }
        public void SetGameDay(string id, int day)
        {
            long lid = Int64.Parse(id);
            using (WerewolfContext _db = new WerewolfContext())
            {
                Game game = _db.Games.Where(g => g.Id == lid).ToList()[0];
                game.Day = day;
                _db.Games.Update(game);
                _db.SaveChanges();
            }
        }
        public void SetGamePeriod(string id, Game.PeriodEnum p)
        {
            long lid = Int64.Parse(id);
            using (WerewolfContext _db = new WerewolfContext())
            {
                Game game = _db.Games.Where(g => g.Id == lid).ToList()[0];
                game.Period = p;
                _db.Games.Update(game);
                _db.SaveChanges();
            }
        }
        public void SetGameOutcome(string id, Game.OutcomeEnum o)
        {
            long lid = Int64.Parse(id);
            using (WerewolfContext _db = new WerewolfContext())
            {
                Game game = _db.Games.Where(g => g.Id == lid).ToList()[0];
                game.Outcome = o;
                _db.Games.Update(game);
                _db.SaveChanges();
            }
        }
        public void SetGameStatus(string id, Game.StatusEnum s)
        {
            long lid = Int64.Parse(id);
            using (WerewolfContext _db = new WerewolfContext())
            {
                Game game = _db.Games.Where(g => g.Id == lid).ToList()[0];
                game.Status = s;
                _db.Games.Update(game);
                _db.SaveChanges();
            }
        }
        public void StartGame(string id)
        {
            Game game = null;
            using (WerewolfContext _db = new WerewolfContext())
            {
                long lid = Int64.Parse(id);
                game = _db.Games.Where(g => g.Id == lid).ToList()[0];
                if (game != null)
                {
                    game.Status = Game.StatusEnum.PlayingEnum;
                    game.ResetDayVoteList();
                    game.ResetNightVoteList();
                }
                else
                {
                    throw new GameNotFoundWerewolfException();
                }
                _db.Games.Update(game);
                _db.SaveChanges();
            }
            if (game != null)
            {
                NotifyObserver(WerewolfEvent.GAME_STARTED, game.Id);
            }
        }

        public void DeleteGame(string id)
        {
            long lid = Int64.Parse(id);
            Game game = null;
            using (WerewolfContext _db = new WerewolfContext())
            {
                game = _db.Games.Where(g => g.Id == lid).ToList()[0];
                if (game != null)
                {
                    _db.Games.Remove(game);
                }
                else
                {
                    throw new GameNotFoundWerewolfException();
                }
                _db.SaveChanges();
            } 
            if (game != null)
            {
                NotifyObserver(WerewolfEvent.GAME_DELETED, game.Id);
            }
        }
        public Game CreateGame()
        {
            Game game = new Game();
            game.Hash = Guid.NewGuid().ToString();
            game.Status = Game.StatusEnum.WaitingEnum;
            //game.Players = new List<Player>();
            game.Period = Game.PeriodEnum.ProcessingEnum;
            using (WerewolfContext _db = new WerewolfContext())
            {
                _db.Games.Add(game);
                _db.SaveChanges();
                NotifyObserver(WerewolfEvent.GAME_CREATED, game.Id);
                return DeepClone<Game>(_db.Games.OrderBy(g => g.Id).Last());
            }
        }
        public Game JoinGame(Game g, Player p)
        {
            using (WerewolfContext _db = new WerewolfContext())
            {
                Game game = _db.Games.Where(_g => _g.Id == g.Id).Include(_game => _game.Players).ToList()[0];
                if (game.Status != Game.StatusEnum.WaitingEnum)
                {
                    throw new GameNotPlayableWerewolfException("Game is already ended or running");
                }
                List<Player> players = game.Players.ToList();
                if (players.Count >= max_players)
                {
                    throw new GameNotPlayableWerewolfException("Game is fulled already");
                }
                Player player = _db.Players.Where(_p => _p.Id == p.Id).ToList()[0];
                if (players.Contains(player))
                {
                    throw new PlayerInGameAlreadyWerewolfException("User in game already");
                }
                //player.Game = game.GameId.ToString();
                player.Status = Player.StatusEnum.AliveEnum;
                players.Add(player);
                game.Players = players;
                _db.Games.Update(game);
                _db.Players.Update(player);
                _db.SaveChanges();
                NotifyObserver(WerewolfEvent.PLAYER_JOIN, game.Id);
                return DeepClone<Game>(game);
            }
        }
        public Game LeaveGame(Game g, Player p)
        {
            using (WerewolfContext _db = new WerewolfContext())
            {
                Game game = _db.Games.Where(_g => _g.Id == g.Id).ToList()[0];
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
        }
        public List<Player> GetPlayers()
        {
            using (WerewolfContext _db = new WerewolfContext())
            {
                return DeepClone<List<Player>>(_db.Players.ToList());
            }
        }
        public Player GetPlayer(string id)
        {
            long lid = Int64.Parse(id);
            using (WerewolfContext _db = new WerewolfContext())
            {
                return DeepClone<Player>(_db.Players.Where(player => player.Id == lid).Include(player => player.Role).ToList()[0]);
            }
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
            using (WerewolfContext _db = new WerewolfContext())
            {
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
        }
        public Player GetPlayerByName(string name)
        {
            using (WerewolfContext _db = new WerewolfContext())
            {
                return DeepClone<Player>(_db.Players.Where(player => player.Name.ToUpper() == name.ToUpper()).ToList()[0]);
            }
        }
        public Boolean IsPlayerExists(string name)
        {
            using (WerewolfContext _db = new WerewolfContext())
            {
                if (_db.Players.Where(player => player.Name.ToUpper() == name.ToUpper()).Count() > 0)
                {
                    return true;
                }
                return false;
            }
        }
        public Player GetPlayerBySession(string session)
        {
            List<Player> players = null;
            using (WerewolfContext _db = new WerewolfContext())
            {
                //return DeepClone<Player>(_db.Players.Where(player => player.Session == session).Include(player => player.Game).ThenInclude(game => game.Players).ThenInclude(p => p.Role).ThenInclude(r => r.ActionRoles).ToList()[0]);
                try
                {
                    players = _db.Players.Where(player => player.Session == session).Include(player => player.Game).ThenInclude(game => game.Players).ThenInclude(p => p.Role).ThenInclude(r => r.ActionRoles).ToList();
                }
                catch (Exception)
                {
                    throw new PlayerNotFoundWerewolfException();
                }
            }
            if (players != null && players.Count > 0)
            {
                return DeepClone<Player>(players[0]);
            }
            return null;
        }
        public List<Player> GetPlayerByGame(string gameid)
        {
            long gid = Int64.Parse(gameid);
            using (WerewolfContext _db = new WerewolfContext())
            {
                Game _game = _db.Games.Where(game => game.Id == gid).Include(game => game.Players).ToList()[0];
                return DeepClone<List<Player>>(_game.Players.ToList());
            }
        }
        public void AddPlayer(Player player)
        {
            using (WerewolfContext _db = new WerewolfContext())
            {
                _db.Players.Add(player);
                _db.SaveChanges();
            }
        }
        public void UpdatePlayer(Player player)
        {
            using (WerewolfContext _db = new WerewolfContext())
            {
                Player p = _db.Players.Where(pr => pr.Id == player.Id).ToList()[0];
                if (p == null)
                {
                    throw new PlayerNotFoundWerewolfException("User not found");
                }
                if (p.Name != player.Name)
                {
                    throw new Exception("Not allow to change name");
                }
                p.Password = player.Password;
                p.Session = player.Session;
                p.GameId = player.GameId;
                if (p.GameId == null)
                {
                    p.Game = null;
                }
                else
                {
                    p.Game = _db.Games.Where(g => g.Id == player.GameId).ToList()[0];
                }
                if (player.Role != null)
                {
                    p.Role = _db.Roles.Where(r => r.Id == player.Role.Id).ToList()[0];
                }
                _db.Players.Update(p);
                _db.SaveChanges();
            }
        }
        public void DeletePlayer(string id)
        {
            long lid = Int64.Parse(id);
            using (WerewolfContext _db = new WerewolfContext())
            {
                Player player = _db.Players.Where(p => p.Id == lid).ToList()[0];
                if (player != null)
                {
                    _db.Players.Remove(player);
                }
                else
                {
                    throw new PlayerNotFoundWerewolfException();
                }
                _db.SaveChanges();
            }
        }
        public void SetPlayerStatus(string id, Player.StatusEnum status)
        {
            long lid = Int64.Parse(id);
            using (WerewolfContext _db = new WerewolfContext())
            {
                Player player = _db.Players.Where(p => p.Id == lid).ToList()[0];
                player.Status = status;
                _db.Players.Update(player);
                _db.SaveChanges();
            }
        }
        public List<Action> GetActions()
        {
            using (WerewolfContext _db = new WerewolfContext())
            {
                return DeepClone<List<Action>>(_db.Actions.Include(ar => ar.ActionRoles).ThenInclude(r => r.Role).ToList());
            }
        }
        public Action GetAction(string id)
        {
            // Throws exception
            long lid = Int64.Parse(id);
            using (WerewolfContext _db = new WerewolfContext())
            {
                return DeepClone<Action>(_db.Actions.Where(action => action.Id == lid).Include(ar => ar.ActionRoles).ThenInclude(r => r.Role).ToList()[0]);
            }
        }
        private Action GetActionByName(string name)
        {
            using (WerewolfContext _db = new WerewolfContext())
            {
                return DeepClone<Action>(_db.Actions.Where(action => action.Name == name).ToList()[0]);
            }
        }
        public List<Action> GetActionByRoleId(string id)
        {
            long lid = Int64.Parse(id);
            //return DeepClone<List<Action>>(_db.Actions.Where(action => action.Roles.Any(role => role.RoleId == lid)).ToList());
            using (WerewolfContext _db = new WerewolfContext())
            {
                Role role = _db.Roles.Where(r => r.Id == lid).Include(ar => ar.ActionRoles).ThenInclude(a => a.Action).ToList()[0];
                List<Action> actions = new List<Action>();
                foreach (ActionRole ar in role.ActionRoles)
                {
                    // ar.Action.ActionRoles = null;
                    // ar.Action.Roles = null;
                    actions.Add(ar.Action);
                }
                return actions;
            }
            //return DeepClone<List<Action>>(role.Actions.ToList());
            //return DeepClone<List<Action>>(_db.Roles.Where(r => r.Id == lid).Include(ar => ar.ActionRoles).ThenInclude(a => a.Action).ToList()[0].Actions.ToList());
        }
        public List<Role> GetRoles()
        {
            using (WerewolfContext _db = new WerewolfContext())
            {
                return DeepClone<List<Role>>(_db.Roles.Include(ar => ar.ActionRoles).ThenInclude(a => a.Action).ToList());
            }
        }
        public Role GetRole(string id)
        {
            // Throws exception
            long lid = Int64.Parse(id);
            using (WerewolfContext _db = new WerewolfContext())
            {
                return DeepClone<Role>(_db.Roles.Where(role => role.Id == lid).Include(ar => ar.ActionRoles).ThenInclude(a => a.Action).ToList()[0]);
            }
        }

        public Role GetRoleByName(string name)
        {
            using (WerewolfContext _db = new WerewolfContext())
            {
                return DeepClone<Role>(_db.Roles.Where(role => role.Name == name).ToList()[0]);
            }
        }

        public List<ChatMessage> GetMessages(string sessionID, string lastID)
        {
            Player player;
            Game game;
            try
            {
                player = GetPlayerBySession(sessionID);
            }
            catch (Exception)
            {
                throw new PlayerNotFoundWerewolfException("Player not found.");
            }
            if (player.Status != Player.StatusEnum.AliveEnum)
            {
                throw new PlayerIsNotAliveWerewolfException("Player is not alived.");
            }
            game = GetGame(player.Game.Id.ToString());
            if (game == null)
            {
                throw new PlayerIsNotInGameWerewolfException("Player is not in a game.");
            }
            if (game.Period == Game.PeriodEnum.ProcessingEnum)
            {
                throw new PlayerIsNotAllowToChatWerewolfException("Please wait for processing.");
            }
            if (game.Period == Game.PeriodEnum.DayEnum && player.Status == Player.StatusEnum.AliveEnum)
            {
                return chatManager.GetSince(game.Id, ChatMessage.ChannelEnum.VillageEnum, lastID);
            }
            else
            {
                if (player.Status != Player.StatusEnum.AliveEnum || player.Role.Name == WerewolfGame.ROLE_MEDIUM)
                {
                    return chatManager.GetSince(game.Id, ChatMessage.ChannelEnum.DeadEnum, lastID);
                }
                else if (IsWerewolf(player.Role.Name))
                {
                    return chatManager.GetSince(game.Id, ChatMessage.ChannelEnum.WolfEnum, lastID);
                }
                else if (player.Role.Name == WerewolfGame.ROLE_JAILER || (game.Jailed != null && game.Jailed.Id == player.Id))
                {
                    return chatManager.GetSince(game.Id, ChatMessage.ChannelEnum.JailEnum, lastID);
                }
                throw new PlayerIsNotAllowToChatWerewolfException("You're not allow to talk now");
            }
        }
        public void PostMessage(string sessionID, ChatMessage message)
        {
            Player player;
            Game game;
            try
            {
                player = GetPlayerBySession(sessionID);
            }
            catch (Exception)
            {
                throw new PlayerNotFoundWerewolfException("Player not found.");
            }
            if (player.Status != Player.StatusEnum.AliveEnum)
            {
                throw new PlayerIsNotAliveWerewolfException("Player is not alived.");
            }
            game = GetGame(player.Game.Id.ToString());
            if (game == null)
            {
                throw new PlayerIsNotInGameWerewolfException("Player is not in a game.");
            }
            if (game.Period == Game.PeriodEnum.ProcessingEnum)
            {
                throw new PlayerIsNotAllowToChatWerewolfException("Please wait for processing.");
            }
            message.GameId = game.Id;
            message.PlayerId = (long)player.Id;
            if (game.Period == Game.PeriodEnum.DayEnum)
            {
                message.Channel = ChatMessage.ChannelEnum.VillageEnum;
                chatManager.Add(message);
            }
            else
            {
                if (player.Status != Player.StatusEnum.AliveEnum || player.Role.Name == WerewolfGame.ROLE_MEDIUM)
                {
                    message.Channel = ChatMessage.ChannelEnum.DeadEnum;
                    chatManager.Add(message);
                }
                else if (IsWerewolf(player.Role.Name))
                {
                    message.Channel = ChatMessage.ChannelEnum.WolfEnum;
                    chatManager.Add(message);
                }
                else if (player.Role.Name == WerewolfGame.ROLE_JAILER || (game.Jailed != null && game.Jailed.Id == player.Id))
                {
                    message.Channel = ChatMessage.ChannelEnum.JailEnum;
                    chatManager.Add(message);
                }
                else
                {
                    throw new PlayerIsNotAllowToChatWerewolfException("You're not allow to chat now.");
                }
            }
        }
        public OutcomeEnum PostAction(string sessionID, string actionID, string targetID)
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
                throw new PlayerNotFoundWerewolfException("Player not found.");
            }
            if (player.Status != Player.StatusEnum.AliveEnum)
            {
                throw new PlayerIsNotAliveWerewolfException("Player is not alived.");
            }
            try
            {
                action = GetAction(actionID);
            }
            catch (Exception)
            {
                throw new ActionNotFoundWerewolfException("Action not found.");
            }
            try
            {
                target = GetPlayer(targetID);
            }
            catch (Exception)
            {
                throw new TargetNotFoundWerewolfException("Target not found.");
            }
            if (target.Id == player.Id)
            {
                throw new CantPerformOnYourselfWerewolfException("You can't perform on yourself.");
            }
            game = GetGame(player.Game.Id.ToString());
            if (game == null)
            {
                throw new PlayerIsNotInGameWerewolfException("Player is not in a game.");
            }
            if (game.Period == Game.PeriodEnum.ProcessingEnum)
            {
                throw new ProcessingPeriodWerewolfException("Please wait for processing.");
            }
            role = GetRole(player.Role.Id.ToString());

            if (role == null)
            {
                throw new PlayerNotFoundWerewolfException("Player does not have any role.");
            }
            List<int> action_ids = role.ActionRoles.Select(ar => ar.Action.Id).ToList();
            //if (!role.Actions.Contains(action))
            if (!action_ids.Contains(action.Id))
            {
                throw new ActionNotFoundWerewolfException("Player's role does not have this action.");
            }
            if (game.Status != Game.StatusEnum.PlayingEnum)
            {
                throw new GameNotPlayableWerewolfException("Game is not playable.");
            }
            if (game.Period == Game.PeriodEnum.DayEnum)
            {
                if (target.Status != Player.StatusEnum.AliveEnum)
                {
                    return OutcomeEnum.TargetIsNotAlivedEnum;
                }
                if (action.Name == WerewolfGame.ACTION_DAY_VOTE)
                {
                    if (game.DayVoteList.ContainsKey(player.Id) && game.DayVoteList[player.Id] == target.Id)
                    {
                        game.DayVoteList.Remove(player.Id);
                    }
                    else
                    {
                        game.DayVoteList[player.Id] = target.Id;
                    }
                    using (WerewolfContext _db = new WerewolfContext())
                    {
                        _db.Games.Update(game);
                        _db.SaveChanges();
                    }
                    return OutcomeEnum.ActionPerformedEnum;
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
                    return OutcomeEnum.ActionPerformedEnum;
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
                    return OutcomeEnum.ActionPerformedEnum;
                }
                else if (action.Name == WerewolfGame.ACTION_SHOOT && target.Status == Player.StatusEnum.AliveEnum)
                {
                    SetPlayerStatus(target.Id.ToString(), Player.StatusEnum.ShotDeadEnum);
                    return OutcomeEnum.TargetDeadEnum;
                }
                else if (action.Name == WerewolfGame.ACTION_HOLYWATER)
                {
                    if (IsWerewolf(target.Role.Name))
                    {
                        SetPlayerStatus(target.Id.ToString(), Player.StatusEnum.HolyDeadEnum);
                        return OutcomeEnum.TargetDeadEnum;
                    }
                    else
                    {
                        SetPlayerStatus(player.Id.ToString(), Player.StatusEnum.HolyDeadEnum);
                        return OutcomeEnum.PlayerDeadEnum;
                    }
                }
            }
            else if (game.Period == Game.PeriodEnum.NightEnum)
            {
                if (game.Jailed != null && player.Id == game.Jailed.Id)
                {
                    return OutcomeEnum.ActionPerformedEnum;
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
                    return OutcomeEnum.ActionPerformedEnum;
                }
                if (target.Status != Player.StatusEnum.AliveEnum)
                {
                    throw new Exception("Target is not alived.");
                }
                if (action.Name == WerewolfGame.ACTION_NIGHT_VOTE)
                {
                    if (game.NightVoteList.ContainsKey(player.Id) && game.NightVoteList[player.Id] == target.Id)
                    {
                        game.NightVoteList.Remove(player.Id);
                    }
                    else
                    {
                        game.NightVoteList[player.Id] = target.Id;
                    }
                    using (WerewolfContext _db = new WerewolfContext())
                    {
                        _db.Games.Update(game);
                        _db.SaveChanges();
                    }
                    return OutcomeEnum.ActionPerformedEnum;
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
                    using (WerewolfContext _db = new WerewolfContext())
                    {
                        _db.Games.Update(game);
                        _db.SaveChanges();
                    }
                    return OutcomeEnum.ActionPerformedEnum;
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
                    using (WerewolfContext _db = new WerewolfContext())
                    {
                        _db.Games.Update(game);
                        _db.SaveChanges();
                    }
                    return OutcomeEnum.ActionPerformedEnum;
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
                    using (WerewolfContext _db = new WerewolfContext())
                    {
                        _db.Games.Update(game);
                        _db.SaveChanges();
                    }
                    return OutcomeEnum.ActionPerformedEnum;
                }
                else if (action.Name == WerewolfGame.ACTION_REVEAL)
                {
                    if (target == game.Enchanted)
                    {
                        return OutcomeEnum.EnchantedEnum;
                    }
                    return OutcomeEnum.RevealedEnum;
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
                        return OutcomeEnum.UnknownEnum;
                    }
                    else if (new[] {
                        WerewolfGame.ROLE_WEREWOLF,
                        WerewolfGame.ROLE_WEREWOLF_SEER,
                        WerewolfGame.ROLE_WEREWOLF_SHAMAN
                    }.Contains(target.Name))
                    {
                        return OutcomeEnum.WerewolfEnum;
                    }
                    return OutcomeEnum.VillagerEnum;

                }
            }
            return OutcomeEnum.NotValidActionEnum;
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
        public void ResetGameState(string id)
        {
            long lid = long.Parse(id);
            using (WerewolfContext _db = new WerewolfContext())
            {
                Game game = _db.Games.Where(g => g.Id == lid).ToList()[0];
                game.KillBySerialKiller = null;
                game.HealedByDoctor = null;
                game.Jailed = null;
                game.ProtectedByBodyguard = null;
                game.ResetNightVoteList();
                game.ResetDayVoteList();
                _db.Games.Update(game);
                _db.SaveChanges();
            }
        }

        public void Dispose()
        {
        }
    }
}
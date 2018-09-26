
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
    public class WerewolfManager : IObserver<WerewolfEvent>
    {
        public class GameManager
        {
            private string gameId;
            private int timeCounter;
            private WerewolfGame werewolf;
            public GameManager(Game game)
            {
                Game _game = game;
                werewolf = WerewolfGame.GetInstance();
                gameId = game.GameId.ToString();
                werewolf.SetGameDay(gameId, 0);
                werewolf.SetGamePeriod(gameId, Game.PeriodEnum.NightEnum);
                timeCounter = 0;
                // set player role;
                List<Player> players = _game.Players.ToList();
                int playerCount = players.Count;
                List<string> roles = WerewolfGame.ROLE_LIST.ToList();
                int roleCount = roles.Count;
                Random rand = new Random();
                int pos;
                foreach(Player player in _game.Players)
                {
                    pos = rand.Next(roleCount);
                    player.Role = werewolf.GetRoleByName(roles[pos]);
                    werewolf.UpdatePlayer(player);
                    Console.WriteLine("Assign role {0} to player {1}", roles[pos], player.Name);
                    roles.RemoveAt(pos);
                    roleCount--;
                }
            }
            public void OnTimedEvent(object stateInfo)
            {
                Game _game = WerewolfGame.GetInstance().GetGame(gameId);
                if (_game.Status == Game.StatusEnum.PlayingEnum)
                {
                    if (_game.Period == Game.PeriodEnum.ProcessingEnum)
                    {
                        werewolf.SetGamePeriod(gameId, Game.PeriodEnum.NightEnum);
                    }
                    Console.WriteLine("Game[{0}]: OnTimedEvent", _game.GameId);
                    lock (this)
                    {
                        timeCounter++;
                        if (_game.Period == Game.PeriodEnum.NightEnum && timeCounter >= WerewolfGame.GAME_NIGHT_PERIOD)
                        {
                            werewolf.SetGamePeriod(gameId, Game.PeriodEnum.ProcessingEnum);
                            // End of the night, revive a player
                            if (_game.ReviveByMedium != null)
                            {
                                _game.ReviveByMedium.Status = Player.StatusEnum.AliveEnum;
                            }
                            _game.ReviveByMedium = null;
                            // check who will be killed by wolf.
                            Dictionary<Player, int> voteCount = new Dictionary<Player, int>();
                            foreach (KeyValuePair<Player, Player> entry in _game.NightVoteList)
                            {
                                if (voteCount.ContainsKey(entry.Value))
                                {
                                    voteCount[entry.Value]++;
                                }
                                else
                                {
                                    voteCount[entry.Value] = 1;
                                }
                            }
                            Player maxVote = voteCount.FirstOrDefault(x => x.Value == voteCount.Values.Max()).Key;
                            if (maxVote != null && maxVote == _game.ProtectedByBodyguard)
                            {
                                _game.BodyguardHit--;
                                if (_game.BodyguardHit == 0)
                                {
                                    Player bodyguard = _game.Players.Where(p => p.Role.Name == WerewolfGame.ROLE_BODYGUARD).Single();
                                    bodyguard.Status = Player.StatusEnum.DeadEnum;
                                }
                            }
                            else if (maxVote != null && maxVote != _game.HealedByDoctor)
                            {
                                maxVote.Status = Player.StatusEnum.DeadEnum;
                            }
                            _game.ResetNightVoteList();
                            // Serial killer's victim
                            Player victim = _game.KillBySerialKiller;
                            if (victim == _game.ProtectedByBodyguard && _game.BodyguardHit > 0)
                            {
                                _game.BodyguardHit--;
                                if (_game.BodyguardHit == 0)
                                {
                                    Player bodyguard = _game.Players.Where(p => p.Role.Name == WerewolfGame.ROLE_BODYGUARD).Single();
                                    bodyguard.Status = Player.StatusEnum.DeadEnum;
                                }
                            }
                            else if (victim != _game.HealedByDoctor)
                            {
                                _game.KillBySerialKiller.Status = Player.StatusEnum.DeadEnum;
                            }
                            _game.KillBySerialKiller = null;
                            timeCounter = 0;
                            werewolf.SetGamePeriod(gameId, Game.PeriodEnum.DayEnum);
                        }
                        else if (_game.Period == Game.PeriodEnum.DayEnum && timeCounter >= WerewolfGame.GAME_DAY_PERIOD)
                        {
                            werewolf.SetGamePeriod(gameId, Game.PeriodEnum.ProcessingEnum);
                            // End of the day, check who will be killed by villager.
                            Dictionary<Player, int> voteCount = new Dictionary<Player, int>();
                            foreach (KeyValuePair<Player, Player> entry in _game.DayVoteList)
                            {
                                if (voteCount.ContainsKey(entry.Value))
                                {
                                    voteCount[entry.Value]++;
                                }
                                else
                                {
                                    voteCount[entry.Value] = 1;
                                }
                            }
                            Player maxVote = voteCount.FirstOrDefault(x => x.Value == voteCount.Values.Max()).Key;
                            if(maxVote != null) {
                                maxVote.Status = Player.StatusEnum.DeadEnum;
                            }
                            _game.ResetDayVoteList();

                            timeCounter = 0;
                            werewolf.SetGameDay(gameId, (int)_game.Day + 1);
                            werewolf.SetGamePeriod(gameId, Game.PeriodEnum.NightEnum);
                        }
                        if (_game.Period == Game.PeriodEnum.NightEnum)
                        {
                            Console.WriteLine("Game[{0}]: Night time of day {1} @ {2}", _game.GameId, _game.Day, timeCounter);
                        }
                        else
                        {
                            Console.WriteLine("Game[{0}]: Day time of day {1} @ {2}", _game.GameId, _game.Day, timeCounter);
                        }
                    }
                }
            }
        }

        private System.Timers.Timer _timer;
        private List<Game> gameList;
        private List<Timer> timerList;
        private WerewolfGame werewolf;
        public WerewolfManager()
        {
            gameList = new List<Game>();
            timerList = new List<Timer>();
            werewolf = WerewolfGame.GetInstance();
        }
        public void Start()
        {
            werewolf.Subscribe(this);
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(WerewolfEvent we)
        {
            Game game = werewolf.GetGame(we.GameId.ToString());
            switch (we.Event)
            {
                case WerewolfEvent.GAME_CREATED:
                    if (!gameList.Contains(game))
                    {
                        gameList.Add(game);
                    }
                    break;
                case WerewolfEvent.PLAYER_JOIN:
                    if (game.Players.Count >= WerewolfGame.MAX_PLAYERS)
                    {
                        Console.WriteLine("Start game {0}", game.GameId);
                        werewolf.StartGame(game.GameId.ToString());
                    }
                    break;
                case WerewolfEvent.GAME_STARTED:
                    GameManager gm = new GameManager(game);
                    AutoResetEvent autoEvent = new AutoResetEvent(false);
                    Console.WriteLine("Game {0} started, waiting for 5 seconds count down", game.GameId);
                    Timer timer = new Timer(gm.OnTimedEvent, autoEvent, WerewolfGame.GAME_COUNTDOWN_PERIOD * 1000, 1000);
                    timerList.Add(timer);
                    break;
            }
        }


    }
}
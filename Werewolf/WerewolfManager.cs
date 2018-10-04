
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
            public GameManager(WerewolfGame werewolf, Game game)
            {
                this.werewolf = werewolf;
                Game _game = werewolf.GetGame(game.GameId.ToString());
                gameId = game.GameId.ToString();
                werewolf.SetGameDay(gameId, 0);
                werewolf.SetGamePeriod(gameId, Game.PeriodEnum.NightEnum);
                timeCounter = 0;
                // set player role;
                List<Player> players = _game.Players.ToList();
                int playerCount = players.Count;
                List<string> roles = null;
                Random rand = new Random();
                if (playerCount == 16)
                {
                    // 50% chances to be Fool game or Head hunter game
                    if (rand.Next(2)%2 == 0) {
                        roles = WerewolfGame.ROLE_LIST_16_FOOL.ToList();
                    } else {
                        roles = WerewolfGame.ROLE_LIST_16_HEAD_HUNTER.ToList();
                    }
                }
                else if (playerCount == 15)
                {
                    if (rand.Next(2)%2 == 0) {
                        roles = WerewolfGame.ROLE_LIST_15_FOOL.ToList();
                    } else {
                        roles = WerewolfGame.ROLE_LIST_15_HEAD_HUNTER.ToList();
                    }
                }
                else if (playerCount == 14)
                {
                    if (rand.Next(2)%2 == 0) {
                        roles = WerewolfGame.ROLE_LIST_14_FOOL.ToList();
                    } else {
                        roles = WerewolfGame.ROLE_LIST_14_HEAD_HUNTER.ToList();
                    }
                }
                else if (playerCount == 2) // for testing purpose
                {
                    roles = WerewolfGame.ROLE_LIST_2.ToList();
                }
                int roleCount = roles.Count;
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
                //TODO random head-hunter target, must be villager team
            }
            private Game.OutcomeEnum CheckWinner()
            {
                lock(this)
                {
                    Game _game = werewolf.GetGame(gameId);
                    List<Player> survivers = _game.Players.Where(x => x.Status == Player.StatusEnum.AliveEnum).ToList();
                    if (survivers.Count == 1)
                    {
                        if (survivers[0].Role.Name == WerewolfGame.ROLE_SERIAL_KILLER)
                        {
                            werewolf.SetGameStatus(_game.GameId.ToString(), Game.StatusEnum.EndedEnum);
                            return Game.OutcomeEnum.SerialKillerWin;
                        }
                        else if (survivers[0].Role.Name == WerewolfGame.ROLE_FOOL)
                        {
                            werewolf.SetGameStatus(_game.GameId.ToString(), Game.StatusEnum.EndedEnum);
                            return Game.OutcomeEnum.FoolWin;
                        }
                    }
                    int countWerewolfTeam = survivers.Where(x => x.Role.Type == Role.TypeEnum.WolfEnum).Count();
                    int countVillagerTeam = survivers.Where(x => x.Role.Type == Role.TypeEnum.VillagerEnum).Count();
                    // Headhunt 
                    if (_game.TargetByHeadHunter != null && werewolf.GetPlayer(_game.TargetByHeadHunter.Id.ToString()).Status == Player.StatusEnum.DeadEnum)
                    {
                        countVillagerTeam = countVillagerTeam += survivers.Where(x => x.Role.Name == WerewolfGame.ROLE_HEAD_HUNTER).Count();
                    }
                    if (countWerewolfTeam > countVillagerTeam)
                    {
                        werewolf.SetGameStatus(_game.GameId.ToString(), Game.StatusEnum.EndedEnum);
                        return Game.OutcomeEnum.WerewolfWin;
                    }
                    else if (countWerewolfTeam == 0)
                    {
                        werewolf.SetGameStatus(_game.GameId.ToString(), Game.StatusEnum.EndedEnum);
                        return Game.OutcomeEnum.VillagerWin;
                    }
                    return Game.OutcomeEnum.NoWin;

                }
            }
            public void OnTimedEvent(object stateInfo)
            {
                lock (this)
                {
                    Game _game = werewolf.GetGame(gameId);
                    if (_game.Status == Game.StatusEnum.EndedEnum)
                    {
                        Timer t = (Timer)stateInfo;
                        Console.WriteLine("Ending game {0}.", _game.GameId);
                        t.Dispose();
                        return;
                    }
                    werewolf.SetGameOutcome(gameId, Game.OutcomeEnum.NoWin);
                    if (_game.Status == Game.StatusEnum.PlayingEnum)
                    {
                        if (_game.Period == Game.PeriodEnum.ProcessingEnum)
                        {
                            werewolf.SetGamePeriod(gameId, Game.PeriodEnum.NightEnum);
                        }
                        Console.WriteLine("Game[{0}]: OnTimedEvent", _game.GameId);
                        timeCounter++;
                        if (_game.Period == Game.PeriodEnum.NightEnum && timeCounter >= WerewolfGame.GAME_NIGHT_PERIOD)
                        {
                            werewolf.SetGamePeriod(gameId, Game.PeriodEnum.ProcessingEnum);
                            // End of the night, revive a player
                            if (_game.ReviveByMedium != null)
                            {
                                werewolf.SetPlayerStatus(_game.ReviveByMedium.Id.ToString(), Player.StatusEnum.AliveEnum);
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
                            if (maxVote != null && maxVote.Id == _game.ProtectedByBodyguard.Id)
                            {
                                _game.BodyguardHit--;
                                if (_game.BodyguardHit == 0)
                                {
                                    Player bodyguard = _game.Players.Where(p => p.Role.Name == WerewolfGame.ROLE_BODYGUARD).Single();
                                    werewolf.SetPlayerStatus(bodyguard.Id.ToString(), Player.StatusEnum.DeadEnum);
                                }
                            }
                            else if (maxVote != null && maxVote != _game.HealedByDoctor)
                            {
                                werewolf.SetPlayerStatus(maxVote.Id.ToString(), Player.StatusEnum.DeadEnum);
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
                                    werewolf.SetPlayerStatus(bodyguard.Id.ToString(), Player.StatusEnum.DeadEnum);
                                }
                            }
                            else if (victim != _game.HealedByDoctor)
                            {
                                werewolf.SetPlayerStatus(_game.KillBySerialKiller.Id.ToString(), Player.StatusEnum.DeadEnum);
                            }
                            _game.KillBySerialKiller = null;
                            timeCounter = 0;
                            werewolf.SetGameOutcome(gameId, CheckWinner());
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
                            if (maxVote != null)
                            {
                                werewolf.SetPlayerStatus(maxVote.Id.ToString(), Player.StatusEnum.DeadEnum);
                                if (maxVote.Role.Name == WerewolfGame.ROLE_FOOL)
                                {
                                    //TODO Fool win, stop game 
                                    _game.Outcome = Game.OutcomeEnum.FoolWin;
                                }
                                if (_game.TargetByHeadHunter.Id == maxVote.Id)
                                {
                                    _game.Outcome = Game.OutcomeEnum.HeadHunterWin;
                                }
                            }
                            _game.ResetDayVoteList();

                            if (CheckWinner() == Game.OutcomeEnum.WerewolfWin)
                            {
                                //TODO Werewolf win, 
                            }
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

        private List<Game> gameList;
        private List<Timer> timerList;
        private WerewolfGame werewolf;
        public WerewolfManager()
        {
            gameList = new List<Game>();
            timerList = new List<Timer>();
            werewolf = new WerewolfGame(new WerewolfContext());
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
                    GameManager gm = new GameManager(new WerewolfGame(new WerewolfContext()), game);
                    AutoResetEvent autoEvent = new AutoResetEvent(false);
                    Console.WriteLine("Game {0} started, waiting for 5 seconds count down", game.GameId);
                    Timer timer = new Timer(new TimerCallback(gm.OnTimedEvent));
                    timer.Change(WerewolfGame.GAME_COUNTDOWN_PERIOD * 1000, 1000);
                    timerList.Add(timer);
                    break;
            }
        }


    }
}
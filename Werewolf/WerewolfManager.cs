
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
            public GameManager(string gameID)
            {
                Game _game = null;
                WerewolfGame werewolf = new WerewolfGame();
                try
                {
                    _game = werewolf.GetGame(gameID);
                    gameId = gameID;
                    werewolf.SetGameStatus(gameId, Game.StatusEnum.WaitingEnum);
                    werewolf.SetGamePeriod(gameId, Game.PeriodEnum.ProcessingEnum);
                    werewolf.SetGameDay(gameId, 0);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return;
                }
                if (_game == null)
                {
                    return;
                }
                timeCounter = 0;
                // set player role;
                List<Player> players = _game.Players.ToList();
                int playerCount = players.Count;
                List<string> roles = null;
                Random rand = new Random();
                if (playerCount == 16)
                {
                    // 50% chances to be Fool game or Head hunter game
                    if (rand.Next(2) % 2 == 0)
                    {
                        roles = WerewolfGame.ROLE_LIST_16_FOOL.ToList();
                    }
                    else
                    {
                        roles = WerewolfGame.ROLE_LIST_16_HEAD_HUNTER.ToList();
                    }
                }
                else if (playerCount == 15)
                {
                    if (rand.Next(2) % 2 == 0)
                    {
                        roles = WerewolfGame.ROLE_LIST_15_FOOL.ToList();
                    }
                    else
                    {
                        roles = WerewolfGame.ROLE_LIST_15_HEAD_HUNTER.ToList();
                    }
                }
                else if (playerCount == 14)
                {
                    if (rand.Next(2) % 2 == 0)
                    {
                        roles = WerewolfGame.ROLE_LIST_14_FOOL.ToList();
                    }
                    else
                    {
                        roles = WerewolfGame.ROLE_LIST_14_HEAD_HUNTER.ToList();
                    }
                }
                else if (playerCount == 2) // for testing purpose
                {
                    roles = WerewolfGame.ROLE_LIST_2.ToList();
                }
                int roleCount = roles.Count;
                int pos;
                foreach (Player player in _game.Players)
                {
                    pos = rand.Next(roleCount);
                    player.Role = werewolf.GetRoleByName(roles[pos]);
                    werewolf.UpdatePlayer(player);
                    Console.WriteLine("Assign role {0} to player {1}", roles[pos], player.Name);
                    roles.RemoveAt(pos);
                    roleCount--;
                }
                //TODO random head-hunter target, must be villager team
                werewolf.SetGamePeriod(gameId, Game.PeriodEnum.NightEnum);
                werewolf.SetGameStatus(gameId, Game.StatusEnum.PlayingEnum);
            }
            private Game.OutcomeEnum CheckWinner()
            {
                WerewolfGame werewolf = new WerewolfGame();
                Game _game = werewolf.GetGame(gameId);
                List<Player> survivers = _game.Players.Where(x => x.Status == Player.StatusEnum.AliveEnum).ToList();
                if (survivers.Count == 1)
                {
                    if (survivers[0].Role.Name == WerewolfGame.ROLE_SERIAL_KILLER)
                    {
                        werewolf.SetGameStatus(_game.Id.ToString(), Game.StatusEnum.EndedEnum);
                        return Game.OutcomeEnum.SerialKillerWin;
                    }
                    else if (survivers[0].Role.Name == WerewolfGame.ROLE_FOOL)
                    {
                        werewolf.SetGameStatus(_game.Id.ToString(), Game.StatusEnum.EndedEnum);
                        return Game.OutcomeEnum.FoolWin;
                    }
                }
                int countWerewolfTeam = survivers.Where(x => x.Role.Type == Role.TypeEnum.WolfEnum).Count();
                int countVillagerTeam = survivers.Where(x => x.Role.Type == Role.TypeEnum.VillagerEnum).Count();
                // Headhunt 
                if (_game.TargetByHeadHunter != null && werewolf.IsPlayerDead(_game.TargetByHeadHunter.Id.ToString()))
                {
                    countVillagerTeam = countVillagerTeam += survivers.Where(x => x.Role.Name == WerewolfGame.ROLE_HEAD_HUNTER).Count();
                }
                if (countWerewolfTeam > countVillagerTeam)
                {
                    werewolf.SetGameStatus(_game.Id.ToString(), Game.StatusEnum.EndedEnum);
                    return Game.OutcomeEnum.WerewolfWin;
                }
                else if (countWerewolfTeam == 0)
                {
                    werewolf.SetGameStatus(_game.Id.ToString(), Game.StatusEnum.EndedEnum);
                    return Game.OutcomeEnum.VillagerWin;
                }
                return Game.OutcomeEnum.NoWin;
            }
            public void OnTimedEvent(object stateInfo)
            {
                WerewolfGame werewolf = new WerewolfGame();
                Game _game = werewolf.GetGame(gameId);
                if (_game.Status == Game.StatusEnum.EndedEnum)
                {
                    Timer t = (Timer)stateInfo;
                    Console.WriteLine("Ending game {0}.", _game.Id);
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
                    Console.WriteLine("Game[{0}]: OnTimedEvent", _game.Id);
                    // Check shooting
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
                        Dictionary<long?, int> voteCount = new Dictionary<long?, int>();
                        foreach (KeyValuePair<long?, long?> entry in _game.NightVoteList)
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
                        long? maxVote = voteCount.FirstOrDefault(x => x.Value == voteCount.Values.Max()).Key;
                        if (maxVote != null && _game.ProtectedByBodyguard != null && maxVote == _game.ProtectedByBodyguard.Id)
                        {
                            _game.BodyguardHit--;
                            if (_game.BodyguardHit == 0)
                            {
                                Player bodyguard = _game.Players.Where(p => p.Role.Name == WerewolfGame.ROLE_BODYGUARD).Single();
                                werewolf.SetPlayerStatus(bodyguard.Id.ToString(), Player.StatusEnum.VoteDeadEnum);
                            }
                        }
                        else if (maxVote != null && !(_game.HealedByDoctor != null && maxVote == _game.HealedByDoctor.Id))
                        {
                            werewolf.SetPlayerStatus(maxVote.ToString(), Player.StatusEnum.VoteDeadEnum);
                        }
                        _game.ResetNightVoteList();
                        // Serial killer's victim
                        Player victim = _game.KillBySerialKiller;
                        if (victim !=null && victim == _game.ProtectedByBodyguard && _game.BodyguardHit > 0)
                        {
                            _game.BodyguardHit--;
                            if (_game.BodyguardHit == 0)
                            {
                                Player bodyguard = _game.Players.Where(p => p.Role.Name == WerewolfGame.ROLE_BODYGUARD).Single();
                                werewolf.SetPlayerStatus(bodyguard.Id.ToString(), Player.StatusEnum.KillDeadEnum);
                            }
                        }
                        else if (victim != null && victim != _game.HealedByDoctor)
                        {
                            werewolf.SetPlayerStatus(_game.KillBySerialKiller.Id.ToString(), Player.StatusEnum.KillDeadEnum);
                        }
                        werewolf.ResetGameState(_game.Id.ToString());
                        timeCounter = 0;
                        werewolf.SetGameOutcome(gameId, CheckWinner());
                        werewolf.SetGamePeriod(gameId, Game.PeriodEnum.DayEnum);
                    }
                    else if (_game.Period == Game.PeriodEnum.DayEnum && timeCounter >= WerewolfGame.GAME_DAY_PERIOD)
                    {
                        werewolf.SetGamePeriod(gameId, Game.PeriodEnum.ProcessingEnum);
                        // End of the day, check who will be killed by villager.
                        Dictionary<long?, int> voteCount = new Dictionary<long?, int>();
                        foreach (KeyValuePair<long?, long?> entry in _game.DayVoteList)
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
                        long? maxVote = voteCount.FirstOrDefault(x => x.Value == voteCount.Values.Max()).Key;
                        if (maxVote != null)
                        {
                            werewolf.SetPlayerStatus(maxVote.ToString(), Player.StatusEnum.VoteDeadEnum);
                            Player maxVotePlayer = _game.Players.Where(p => p.Id == maxVote).Single();
                            if (maxVotePlayer.Role.Name == WerewolfGame.ROLE_FOOL)
                            {
                                _game.Outcome = Game.OutcomeEnum.FoolWin;
                            }
                            if (_game.TargetByHeadHunter != null && _game.TargetByHeadHunter.Id == maxVote)
                            {
                                _game.Outcome = Game.OutcomeEnum.HeadHunterWin;
                            }
                        }
                        _game.ResetDayVoteList();
                        timeCounter = 0;
                        werewolf.ResetGameState(_game.Id.ToString());
                        werewolf.SetGameOutcome(gameId, CheckWinner());
                        werewolf.SetGameDay(gameId, (int)_game.Day + 1);
                        werewolf.SetGamePeriod(gameId, Game.PeriodEnum.NightEnum);
                    }
                    if (_game.Period == Game.PeriodEnum.NightEnum)
                    {
                        Console.WriteLine("Game[{0}]: Night time of day {1} @ {2}", _game.Id, _game.Day, timeCounter);
                    }
                    else
                    {
                        Console.WriteLine("Game[{0}]: Day time of day {1} @ {2}", _game.Id, _game.Day, timeCounter);
                    }
                }
                if (_game.Day > WerewolfGame.GAME_MAX_DAY)
                {
                    Timer t = (Timer)stateInfo;
                    Console.WriteLine("We've played for {0} days without winner, so ending game {0}.", WerewolfGame.GAME_MAX_DAY, _game.Id);
                    werewolf.SetGameOutcome(gameId, Game.OutcomeEnum.NoWin);
                    werewolf.SetGameStatus(gameId, Game.StatusEnum.EndedEnum);
                    t.Dispose();
                    return;
                }
            }
        }

        private List<Game> gameList;
        private List<Timer> timerList;
        public WerewolfManager()
        {
            gameList = new List<Game>();
            timerList = new List<Timer>();
        }
        public void Start()
        {
            WerewolfGame werewolf = new WerewolfGame();
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
            WerewolfGame werewolf = new WerewolfGame();
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
                    if (game.Players.Count >= werewolf.max_players)
                    {
                        Console.WriteLine("Start game {0}", game.Id);
                        try
                        {
                            werewolf.StartGame(game.Id.ToString());
                        }
                        catch (GameNotFoundWerewolfException ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                    break;
                case WerewolfEvent.GAME_STARTED:
                    GameManager gm = new GameManager(game.Id.ToString());
                    AutoResetEvent autoEvent = new AutoResetEvent(false);
                    Console.WriteLine("Game {0} started, waiting for 5 seconds count down", game.Id);
                    Timer timer = new Timer(new TimerCallback(gm.OnTimedEvent));
                    timer.Change(WerewolfGame.GAME_COUNTDOWN_PERIOD * 1000, 1000);
                    timerList.Add(timer);
                    break;
            }
        }


    }
}
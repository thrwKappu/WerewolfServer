using System;
using System.Collections.Generic;
using System.Text;
using DNWS.Werewolf;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Linq;
using Action = DNWS.Werewolf.Action;
using OutcomeEnum = DNWS.Werewolf.Action.OutcomeEnum;

namespace DNWS
{
    class WerewolfPlugin : IPlugin
    {
        public const string REQUEST_PLAYER = "PLAYER";
        public const string REQUEST_ROLE = "ROLE";
        public const string REQUEST_GAME = "GAME";
        public const string REQUEST_ACTION = "ACTION";
        public const string REQUEST_CHAT = "CHAT";
        public const string HTTP_GET = "GET";
        public const string HTTP_POST = "POST";
        public const string HTTP_PUT = "PUT";
        public const string HTTP_DELETE = "DELETE";
        
        public WerewolfPlugin()
        {
        }

        public void PreProcessing(HTTPRequest request)
        {
            throw new NotImplementedException();
        }

        protected HTTPResponse WerewolfProcess(HTTPRequest httpRequest,string[] requests, string method)
        {
            HTTPResponse response = new HTTPResponse(200);
            WerewolfGame werewolf = new WerewolfGame();
            string path = requests[0].ToUpper();
            string action = method.ToUpper();
            int request_length = requests.Length;

            response.Type = "application/json";

            if (path == REQUEST_PLAYER)
            {
                if (action == HTTP_GET)
                {
                    if (request_length == 1) //player/
                    {
                        try
                        {
                            List<Player> players = werewolf.GetPlayers().OrderBy(p => p.Id).ToList();
                            foreach (Player player in players)
                            {
                                player.Role = null;
                                player.Session = "";
                                player.Password = "";
                                player.Game = null;
                            }
                            response.SetBodyJson(players);
                            return response;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                            response.Status = 400;
                            return response;
                        }
                    }
                    else if (request_length == 2) //player/{id}
                    {
                        string id = requests[1];
                        try
                        {
                            Player player = werewolf.GetPlayer(id);
                            player.Role = null;
                            player.Session = "";
                            player.Password = "";
                            player.Game = null;
                            response.SetBodyJson(player);
                            return response;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                            response.Status = 404;
                            return response;
                        }
                    }
                    else if (request_length == 3) //player/logout/{sessionID}
                    {
                        if (requests[1].ToUpper() == "LOGOUT")
                        {
                            try
                            {
                                string session = requests[2];
                                Player player = werewolf.GetPlayerBySession(session);
                                player.Session = "";
                                werewolf.UpdatePlayer(player);
                                response.Status = 200;
                                return response;
                            }
                            catch (PlayerNotFoundWerewolfException ex)
                            {
                                Console.WriteLine(ex.ToString());
                                response.Status = 404;
                                return response;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                                response.Status = 400;
                                return response;
                            }
                        }
                        else if (requests[1].ToUpper() == "FINDBYGAME") //player/findByGame/{id}
                        {
                            string gameid = requests[2];
                            try
                            {
                                List<Player> players = werewolf.GetPlayerByGame(gameid).OrderBy(p => p.Id).ToList();
                                foreach (Player player in players)
                                {
                                    player.Session = "";
                                    player.Password = "";
                                }
                                response.SetBodyJson(players);
                                return response;

                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                                response.Status = 404;
                                return response;
                            }
                        }
                    }
                }
                else if (action == HTTP_POST)
                {
                    if (request_length == 1) //player/
                    {
                        try
                        {
                            Player player = JsonConvert.DeserializeObject<Player>(httpRequest.Body);
                            if (werewolf.IsPlayerExists(player.Name))
                            {
                                response.Status = 403;
                                return response;
                            }
                            player.Status = Player.StatusEnum.NotInGameEnum;
                            werewolf.AddPlayer(player);
                            player = werewolf.GetPlayerByName(player.Name);
                            player.Session = "";
                            player.Password = "";
                            response.SetBodyJson(player);
                            response.Status = 201;
                            return response;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                            response.Status = 400;
                            return response;
                        }
                    }
                    else if (request_length == 2)
                    {
                        if (requests[1].ToUpper() == "LOGIN")
                        {
                            try
                            {
                                Player p = JsonConvert.DeserializeObject<Player>(httpRequest.Body);
                                if (p == null)
                                {
                                    response.Status = 400;
                                    return response;
                                }
                                try
                                {
                                    Player player = werewolf.GetPlayerByName(p.Name);
                                    if (player.Password != p.Password || player.Name != p.Name)
                                    {
                                        response.SetBodyString("{\"description\":\"User not found or password is incorrect.\"}");
                                        response.Status = 404;
                                        return response;

                                    }
                                    player.Session = Guid.NewGuid().ToString();
                                    player.Game = null;
                                    player.GameId = null;
                                    werewolf.UpdatePlayer(player);
                                    player.Password = "";
                                    response.SetBodyJson(player);
                                    response.Status = 201;
                                    return response;
                                }
                                catch (PlayerNotFoundWerewolfException)
                                {
                                    response.SetBodyString("{\"description\":\"User not found or password is incorrect.\"}");
                                    response.Status = 404;
                                    return response;
                                }
                                catch (Exception)
                                {
                                    response.SetBodyString("{\"description\":\"Invalid data.\"}");
                                    response.Status = 400;
                                    return response;
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                                response.Status = 400;
                                return response;
                            }
                        }
                    }

                }
                else if (action == HTTP_PUT)
                {
                    try
                    {
                        Player player = JsonConvert.DeserializeObject<Player>(httpRequest.Body);
                        werewolf.UpdatePlayer(player);
                        response.Status = 200;
                        return response;
                    }
                    catch (PlayerNotFoundWerewolfException ex)
                    {
                        Console.WriteLine(ex.ToString());
                        response.Status = 404;
                        return response;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        response.Status = 400;
                        return response;
                    }
                }
                else if (action == HTTP_DELETE)
                {
                    if (request_length == 2) //player/{id}
                    {
                        string id = requests[1];
                        try
                        {
                            werewolf.DeletePlayer(id);
                            response.Status = 200;
                            return response;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                            response.Status = 404;
                            return response;
                        }
                    }
                }
            }
            else if (path == REQUEST_GAME)
            {
                if (action == HTTP_GET)
                {
                    if (request_length == 1) //game
                    {
                        List<DNWS.Werewolf.Game> games;
                        try
                        {
                            games = werewolf.GetGames().OrderBy(a => a.Id).ToList();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                            response.Status = 400;
                            return response;
                        }
                        foreach (DNWS.Werewolf.Game g in games)
                        {
                            foreach (Player player in g.Players)
                            {
                                if (werewolf.IsPlayerDead(player.Id.ToString()))
                                {
                                    Player p = werewolf.GetPlayer(player.Id.ToString());
                                    player.Role = p.Role;
                                }
                                else
                                {
                                    player.Role = null;
                                }
                                player.Password = "";
                                player.Session = "";
                                player.Game = null;
                            }
                        }
                        try
                        {
                            response.SetBodyJson(games);
                            return response;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                            response.Status = 400;
                            return response;
                        }
                    }
                    else if (request_length == 2) //game/{id}
                    {
                        string id = requests[1];
                        Game g;
                        try
                        {
                            g = werewolf.GetGame(id);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                            response.Status = 404;
                            return response;
                        }
                        List<Player> players = new List<Player>();
                        foreach (Player player in g.Players)
                        {
                            player.Role = null;
                            player.Password = "";
                            player.Session = "";
                            player.Game = null;
                        }
                        try
                        {
                            response.SetBodyJson(g);
                            return response;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                            response.Status = 400;
                            return response;
                        }
                    }
                    else if (request_length == 3) //game/session/{sessionID}
                    {
                        if (requests[1].ToUpper() == "SESSION")
                        {
                            Game game;
                            Player player;
                            string sessionID = requests[2];
                            try
                            {
                                player = werewolf.GetPlayerBySession(sessionID);
                            }
                            catch (Exception ex)
                            {
                                // Player not found
                                Console.WriteLine(ex.ToString());
                                response.Status = 400;
                                return response;
                            }
                            try {
                                if (player == null)
                                {
                                    response.Status = 400;
                                    return response;
                                }
                                game = player.Game;
                                if (game == null)
                                {
                                    response.Status = 404;
                                    return response;
                                }
                                foreach (Player p in game.Players)
                                {
                                    if (p.Id == player.Id)
                                    {
                                        if (p.Role != null) 
                                        {
                                            p.Role.ActionRoles = null;
                                        }
                                    }
                                    else if (werewolf.IsPlayerDead(p.Id.ToString()))
                                    {
                                        if (p.Role != null) 
                                        {
                                            p.Role.ActionRoles = null;
                                        }
                                    }
                                    else
                                    {
                                        p.Role = null;
                                    }
                                    p.Password = "";
                                    p.Session = "";
                                    p.Game = null;
                                }
                                response.Status = 200;
                                response.SetBodyJson(game);
                                return response;
                            } catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                                response.Status = 400;
                                return response;
                            }
                        }
                    }
                }
                else if (action == HTTP_POST)
                {
                    if (request_length == 3) //game/session/{sessionID}
                    {
                        if (requests[1].ToUpper() == "SESSION")
                        {
                            Game game;
                            Player player;
                            string sessionID = requests[2];
                            try
                            {
                                player = werewolf.GetPlayerBySession(sessionID);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                                response.Status = 400;
                                return response;
                            }
                            if (player == null)
                            {
                                response.Status = 404;
                                return response;
                            }
                            try
                            {
                                lock (werewolf)
                                {
                                    // No game, create one
                                    if (werewolf.CurrentGameId == 0)
                                    {
                                        game = werewolf.CreateGame();
                                        werewolf.CurrentGameId = (long)game.Id;
                                    }
                                    else
                                    {
                                        // Check game seat, if full, create new
                                        game = werewolf.GetGame(werewolf.CurrentGameId.ToString());
                                        if (game.Players.Count >= werewolf.max_players || game.Status == Game.StatusEnum.EndedEnum)
                                        {
                                            game = werewolf.CreateGame();
                                            werewolf.CurrentGameId = (long)game.Id;
                                        }
                                    }
                                    game = werewolf.JoinGame(game, player);
                                    List<Player> players = game.Players.ToList();
                                    foreach (Player p in players)
                                    {
                                        p.Role = null;
                                        p.Password = "";
                                        p.Session = "";
                                        p.Game = null;
                                    }
                                }
                                response.Status = 201;
                                response.SetBodyJson(game);
                                return response;
                            }
                            catch (GameNotPlayableWerewolfException ex)
                            {
                                Console.WriteLine(ex.ToString());
                                response.Status = 403;
                                return response;
                            }
                            catch (PlayerInGameAlreadyWerewolfException ex)
                            {
                                Console.WriteLine(ex.ToString());
                                response.Status = 404;
                                return response;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                                response.Status = 400;
                                return response;
                            }
                        }
                    }
                    else if (request_length == 5)
                    { //game/action/{sessionID}/{actionID}/{targetID}'
                        if (requests[1].ToUpper() == "ACTION")
                        {
                            string sessionID = requests[2];
                            string actionID = requests[3];
                            string targetID = requests[4];
                            try
                            {
                                Action act = new Action();
                                Action.OutcomeEnum outcome = werewolf.PostAction(sessionID, actionID, targetID);
                                if (outcome == OutcomeEnum.RevealedEnum)
                                {
                                    Player target = werewolf.GetPlayer(targetID);
                                    act.Outcome = OutcomeEnum.RevealedEnum;
                                    act.Target = target.Role.Name;
                                    //response.SetBodyString("{\"outcome\":\"revealed\",\"role\":\"{" + target.Role.Name + "}\"}");
                                }
                                else if (outcome ==  OutcomeEnum.EnchantedEnum)
                                {
                                    act.Outcome = OutcomeEnum.RevealedEnum;
                                    act.Target = WerewolfGame.ROLE_ALPHA_WEREWOLF;
                                    //response.SetBodyString("{\"outcome\":\"revealed\",\"role\":\"{" + WerewolfGame.ROLE_ALPHA_WEREWOLF + "}\"}");
                                }
                                else if (outcome == OutcomeEnum.TargetDeadEnum)
                                {
                                    act.Outcome = OutcomeEnum.TargetDeadEnum;
                                    act.Target = targetID;
                                }
                                else
                                {
                                    act.Outcome = outcome;
                                    response.SetBodyString("{\"outcome\":\"" + outcome + "\"}");
                                }
                                response.SetBodyJson(act);
                                response.Status = 201;
                                return response;
                            }
                            catch (PlayerIsNotAliveWerewolfException ex)
                            {
                                Console.WriteLine(ex.ToString());
                                response.Status = 403;
                                return response;
                            }
                            catch (PlayerNotFoundWerewolfException ex)
                            {
                                Console.WriteLine(ex.ToString());
                                response.Status = 404;
                                return response;
                            }
                            catch (ActionNotFoundWerewolfException ex)
                            {
                                Console.WriteLine(ex.ToString());
                                response.Status = 405;
                                return response;
                            }
                            catch (TargetNotFoundWerewolfException ex)
                            {
                                Console.WriteLine(ex.ToString());
                                response.Status = 406;
                                return response;
                            }
                            catch (CantPerformOnYourselfWerewolfException ex)
                            {
                                Console.WriteLine(ex.ToString());
                                response.Status = 407;
                                return response;
                            }
                            catch (PlayerIsNotInGameWerewolfException ex)
                            {
                                Console.WriteLine(ex.ToString());
                                response.Status = 408;
                                return response;
                            }
                            catch (ProcessingPeriodWerewolfException ex)
                            {
                                Console.WriteLine(ex.ToString());
                                response.Status = 409;
                                return response;
                            }
                            catch (GameNotPlayableWerewolfException ex)
                            {
                                Console.WriteLine(ex.ToString());
                                response.Status = 410;
                                return response;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                                response.SetBodyString("{\"description\":\"" + ex.ToString() + "\"}");
                                response.Status = 400;
                                return response;
                            }
                        }
                    }
                }
                else if (action == HTTP_DELETE)
                {
                    if (request_length == 2) //game/{id}
                    {
                        string id = requests[1];
                        Game g;
                        try
                        {
                            g = werewolf.GetGame(id);
                            werewolf.DeleteGame(id);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                            response.Status = 400;
                            return response;
                        }
                        response.Status = 200;
                        return response;
                    }
                    else if (request_length == 3) //game/session/{sessionID}
                    {
                        if (requests[1].ToUpper() == "SESSION")
                        {
                            Game game;
                            string sessionID = requests[2];
                            Player player;
                            try
                            {
                                player = werewolf.GetPlayerBySession(sessionID);
                            }
                            catch (PlayerNotFoundWerewolfException ex)
                            {
                                //Player not found
                                Console.WriteLine(ex.ToString());
                                response.Status = 404;
                                return response;
                            }
                            if (player.Game == null)
                            {
                                response.Status = 404;
                                return response;
                            }
                            try
                            {
                                lock (werewolf)
                                {
                                    game = werewolf.GetGame(player.Game.Id.ToString());
                                    game = werewolf.LeaveGame(game, player);
                                }
                                response.Status = 200;
                                response.SetBodyJson(game);
                                return response;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                                response.Status = 400;
                                return response;
                            }
                        }
                    }
                }
            }
            else if (path == REQUEST_ROLE)
            {
                if (action == HTTP_GET)
                {
                    if (request_length == 1) //role/
                    {
                        List<Role> roles;
                        try
                        {
                            roles = werewolf.GetRoles().OrderBy(r => r.Id).ToList();
                        }
                        catch (Exception ex)
                        {
                            response.SetBodyString(ex.ToString());
                            response.Status = 400;
                            return response;
                        }
                        //Flatten action list
                        foreach (Role role in roles)
                        {
                            List<DNWS.Werewolf.Action> actions = new List<DNWS.Werewolf.Action>();
                            foreach (ActionRole ar in role.ActionRoles)
                            {
                                ar.Action.ActionRoles = null;
                                actions.Add(ar.Action);
                            }
                            role.Actions = actions.OrderBy(a => a.Id).ToList();
                            role.ActionRoles = null;
                        }
                        try
                        {
                            response.Status = 200;
                            response.SetBodyJson(roles);
                            return response;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                            response.Status = 400;
                            return response;
                        }
                    }
                    else if (request_length == 2) //role/{id}
                    {
                        string id = requests[1];
                        Role role;
                        try
                        {
                            role = werewolf.GetRole(id);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                            response.Status = 400;
                            return response;
                        }
                        List<DNWS.Werewolf.Action> actions = new List<DNWS.Werewolf.Action>();
                        foreach (ActionRole ar in role.ActionRoles)
                        {
                            ar.Action.ActionRoles = null;
                            actions.Add(ar.Action);
                        }
                        role.Actions = actions.OrderBy(a => a.Id).ToList();
                        role.ActionRoles = null;
                        try
                        {
                            response.Status = 200;
                            response.SetBodyJson(role);
                            return response;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                            response.Status = 400;
                            return response;
                        }
                    }
                }
            }
            else if (path == REQUEST_ACTION)
            {
                if (action == HTTP_GET)
                {
                    if (request_length == 1)
                    {
                        List<DNWS.Werewolf.Action> actions;
                        try
                        {
                            actions = werewolf.GetActions().OrderBy(a => a.Id).ToList();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                            response.Status = 400;
                            return response;
                        }
                        //Flatten action list
                        foreach (DNWS.Werewolf.Action act in actions)
                        {
                            List<Role> roles = new List<Role>();
                            foreach (ActionRole ar in act.ActionRoles)
                            {
                                ar.Role.ActionRoles = null;
                                roles.Add(ar.Role);
                            }
                            act.Roles = roles.OrderBy(r => r.Id).ToList();
                            act.ActionRoles = null;
                        }
                        try
                        {
                            response.SetBodyJson(actions);
                            return response;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                            response.Status = 400;
                            return response;
                        }
                    }
                    else if (request_length == 2)
                    {
                        string id = requests[1];
                        DNWS.Werewolf.Action act;
                        try
                        {
                            act = werewolf.GetAction(id);
                        }
                        catch (Exception ex)
                        {
                            response.SetBodyString(ex.ToString());
                            response.Status = 500;
                            return response;
                        }
                        List<Role> roles = new List<Role>();
                        foreach (ActionRole ar in act.ActionRoles)
                        {
                            ar.Role.ActionRoles = null;
                            roles.Add(ar.Role);
                        }
                        act.Roles = roles.OrderBy(r => r.Id).ToList();
                        act.ActionRoles = null;
                        try
                        {
                            response.Status = 200;
                            response.SetBodyJson(act);
                            return response;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                            response.Status = 400;
                            return response;
                        }
                    }
                    else if (request_length == 3)
                    {
                        if (requests[1].ToUpper() == "FINDBYROLE")
                        {
                            string roleid = requests[2];
                            List<DNWS.Werewolf.Action> actions;
                            try
                            {
                                actions = werewolf.GetActionByRoleId(roleid).OrderBy(a => a.Id).ToList();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                                response.Status = 400;
                                return response;
                            }
                            //Flatten action list
                            foreach (DNWS.Werewolf.Action act in actions)
                            {
                                List<Role> roles = new List<Role>();
                                foreach (ActionRole ar in act.ActionRoles)
                                {
                                    ar.Action.ActionRoles = null;
                                    ar.Role.ActionRoles = null;
                                    roles.Add(ar.Role);
                                }
                                act.Roles = roles.OrderBy(r => r.Id).ToList();
                                act.ActionRoles = null;
                            }
                            try
                            {
                                response.SetBodyJson(actions);
                                return response;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                                response.Status = 400;
                                return response;
                            }
                        }
                    }
                }
            }
            else if (path == REQUEST_CHAT)
            {
                if (action == HTTP_GET)
                {
                    if (request_length == 3)
                    {
                        string sessionID = requests[1];
                        string lastID = requests[2];
                        try
                        {
                            List<ChatMessage> messages = werewolf.GetMessages(sessionID, lastID).OrderBy(m => m.Id).ToList();
                            response.SetBodyJson(messages);
                            return response;
                        }
                        catch (PlayerNotFoundWerewolfException ex)
                        {
                            Console.WriteLine(ex.ToString());
                            response.Status = 404;
                            return response;
                        }
                        catch (PlayerIsNotAliveWerewolfException ex)
                        {
                            Console.WriteLine(ex.ToString());
                            response.Status = 403;
                            return response;
                        }
                        catch (PlayerIsNotInGameWerewolfException ex)
                        {
                            Console.WriteLine(ex.ToString());
                            response.Status = 405;
                            return response;
                        }
                        catch (PlayerIsNotAllowToChatWerewolfException ex)
                        {
                            Console.WriteLine(ex.ToString());
                            response.Status = 406;
                            return response;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                            response.Status = 400;
                            return response;
                        }
                    }

                }
                else if (action == HTTP_POST)
                {
                    if (request_length == 2)
                    {
                        string sessionID = requests[1];
                        try
                        {
                            ChatMessage message = JsonConvert.DeserializeObject<ChatMessage>(httpRequest.Body);
                            werewolf.PostMessage(sessionID, message);
                            response.Status = 201;
                            return response;
                        }
                        catch (PlayerNotFoundWerewolfException ex)
                        {
                            Console.WriteLine(ex.ToString());
                            response.Status = 404;
                            return response;
                        }
                        catch (PlayerIsNotAliveWerewolfException ex)
                        {
                            Console.WriteLine(ex.ToString());
                            response.Status = 403;
                            return response;
                        }
                        catch (PlayerIsNotInGameWerewolfException ex)
                        {
                            Console.WriteLine(ex.ToString());
                            response.Status = 405;
                            return response;
                        }
                        catch (PlayerIsNotAllowToChatWerewolfException ex)
                        {
                            Console.WriteLine(ex.ToString());
                            response.Status = 406;
                            return response;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                            response.Status = 400;
                            return response;
                        }
                    }
                    
                }
            }
            response.Status = 400;
            return response;
        }
        public HTTPResponse GetResponse(HTTPRequest request)
        {
            HTTPResponse response = null;
            StringBuilder sb = new StringBuilder();
            String[] path = Regex.Split(request.Url, "/");
            path = path.Skip(2).ToArray();
            if (path.Length > 0 && path[path.Length - 1] == "") {
                path = path.Take(path.Length - 1).ToArray();
            }
            if(path.Length > 0) {
                response = WerewolfProcess(request, path, request.Method);
            } else {
                response = new HTTPResponse(400);
            }
            return response;
        }

        public HTTPResponse PostProcessing(HTTPResponse response)
        {
            throw new NotImplementedException();
        }
    }
}
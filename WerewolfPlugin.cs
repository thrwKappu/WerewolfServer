using System;
using System.Collections.Generic;
using System.Text;
using DNWS.Werewolf;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Linq;

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
            WerewolfGame werewolf = WerewolfGame.GetInstance();
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
                    else if (request_length == 3) //player/logout/{id}
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
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                                response.Status = 404;
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
                                try
                                {
                                    Player player = werewolf.GetPlayerByName(p.Name);
                                    if (player.Password != p.Password || player.Name != p.Name)
                                    {
                                        response.SetBodyString("{\"error\":\"User not found or password is incorrect.\"}");
                                        response.Status = 404;
                                        return response;

                                    }
                                    player.Session = Guid.NewGuid().ToString();
                                    werewolf.UpdatePlayer(player);
                                    player.Password = "";
                                    response.SetBodyJson(player);
                                    response.Status = 201;
                                    return response;
                                }
                                catch (Exception ex)
                                {
                                    response.SetBodyString("{\"error\":\"User not found or password is incorrect.\"}");
                                    response.Status = 404;
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
                            games = werewolf.GetGames().OrderBy(a => a.GameId).ToList();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                            response.Status = 400;
                            return response;
                        }
                        //Flatten action list
                        foreach (DNWS.Werewolf.Game g in games)
                        {
                            // List<Player> players = new List<Player>();
                            foreach (Player player in g.Players)
                            {
                                player.Role = null;
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
                            response.Status = 400;
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
                                response.Status = 404;
                                return response;
                            }
                            try {
                                game = player.Game;
                                foreach(Player p in game.Players)
                                {
                                    if (p.Id != player.Id)
                                    {
                                        p.Role = null;
                                    } else {
                                        p.Role.ActionRoles = null;
                                    }
                                    p.Password = "";
                                    p.Session = "";
                                    p.Game = null;
                                }
                                response.Status = 201;
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
                                // Player not found
                                Console.WriteLine(ex.ToString());
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
                                        werewolf.CurrentGameId = (long)game.GameId;
                                    }
                                    else
                                    {
                                        // Check game seat, if full, create new
                                        game = werewolf.GetGame(werewolf.CurrentGameId.ToString());
                                        if (game.Players.Count >= WerewolfGame.MAX_PLAYERS || game.Status == Game.StatusEnum.EndedEnum)
                                        {
                                            game = werewolf.CreateGame();
                                            werewolf.CurrentGameId = (long)game.GameId;
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
                                string outcome = werewolf.PostAction(sessionID, actionID, targetID);
                                if (outcome == WerewolfGame.OUTCOME_REVEALED)
                                {
                                    Player target = werewolf.GetPlayer(targetID);
                                    response.SetBodyString("{\"outcome\":\"revealed\",\"role\":\"{" + target.Role.Name + "}\"}");
                                }
                                else if (outcome == WerewolfGame.OUTCOME_ENCHANTED)
                                {
                                    response.SetBodyString("{\"outcome\":\"revealed\",\"role\":\"{" + WerewolfGame.ROLE_ALPHA_WEREWOLF + "}\"}");
                                }
                                else
                                {
                                    response.SetBodyString("{\"outcome\":\"" + outcome + "\"}");
                                }
                                response.Status = 201;
                                return response;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                                response.SetBodyString("{\"error\":\"" + ex.ToString() + "\"}");
                                response.Status = 404;
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
                            response.Status = 404;
                            return response;
                        }
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
                            catch (Exception ex)
                            {
                                //Player not found
                                Console.WriteLine(ex.ToString());
                                response.Status = 404;
                                return response;
                            }
                            if (player.Game == null)
                            {
                                response.Status = 403;
                                return response;
                            }
                            try
                            {
                                lock (werewolf)
                                {
                                    game = werewolf.GetGame(player.Game.GameId.ToString());
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
                            response.Status = 500;
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
                            response.SetBodyJson(roles);
                            return response;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                            response.Status = 500;
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
                    else if (request_length == 3 && requests[1].ToUpper() == "FINDBYROLE")
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
                                roles.Add(ar.Role);
                            }
                            act.Roles = roles.OrderBy(r => r.Id).ToList();
                            act.ActionRoles = null;
                        }
                        try {
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
            else if (path == REQUEST_CHAT)
            {
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
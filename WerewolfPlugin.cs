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
        WerewolfGame game = null;
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
            game = WerewolfGame.GetInstance();
        }

        public void PreProcessing(HTTPRequest request)
        {
        }

        protected HTTPResponse WerewolfProcess(HTTPRequest httpRequest,string[] requests, string method)
        {
            HTTPResponse response = new HTTPResponse(200);
            WerewolfGame game = WerewolfGame.GetInstance();
            string path = requests[0].ToUpper();
            string action = method.ToUpper();

            response.Type = "application/json";
            string json = "";

            if (path == REQUEST_PLAYER)
            {
                if (action == HTTP_GET)
                {
                    if (requests.Length == 1) //player/
                    {
                        try
                        {
                            response.SetBodyJson(game.GetPlayers().OrderBy(p => p.Id).ToList());
                            return response;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                            response.Status = 400;
                            return response;
                        }
                    }
                    else if (requests.Length == 2) //player/{id}
                    {
                        string id = requests[1];
                        try
                        {
                            response.SetBodyJson(game.GetPlayer(id));
                            return response;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                            response.Status = 404;
                            return response;
                        }
                    }
                    else if (requests.Length == 3) //player/logout/{id}
                    {
                        if (requests[1].ToUpper() == "LOGOUT")
                        {
                            try
                            {
                                string session = requests[2];
                                Player player = game.GetPlayerBySession(session);
                                player.Session = "";
                                game.UpdatePlayer(player);
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
                                response.SetBodyJson(game.GetPlayerByGame(gameid));
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
                    if (requests.Length == 1) //player/
                    {
                        try
                        {
                            Player player = JsonConvert.DeserializeObject<Player>(httpRequest.Body);
                            if (game.IsPlayerExists(player.Name)) {
                                response.Status = 403;
                                return response;
                            }
                            player.Status = Player.StatusEnum.NotInGameEnum;
                            game.AddPlayer(player);
                            response.SetBodyJson(game.GetPlayerByName(player.Name));
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
                    else if (requests.Length == 2)
                    {
                        if (requests[1].ToUpper() == "LOGIN")
                        {
                            try
                            {
                                Player p = JsonConvert.DeserializeObject<Player>(httpRequest.Body);
                                try
                                {
                                    Player player = game.GetPlayer(p.Id.ToString());
                                    if (player.Password != p.Password || player.Name != p.Name)
                                    {
                                        response.SetBodyString("User not found or password is incorrect.");
                                        response.Status = 404;
                                        return response;

                                    }
                                    player.Session = Guid.NewGuid().ToString();
                                    game.UpdatePlayer(player);
                                    response.SetBodyJson(player);
                                    response.Status = 201;
                                    return response;
                                }
                                catch (Exception ex)
                                {
                                    response.SetBodyString("User not found or password is incorrect.");
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
                        game.UpdatePlayer(player);
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
                    if (requests.Length == 2) //player/{id}
                    {
                        string id = requests[1];
                        try
                        {
                            game.DeletePlayer(id);
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
            }
            else if (path == REQUEST_ROLE)
            {
                if (action == HTTP_GET)
                {
                    if (requests.Length == 1) //role/
                    {
                        List<Role> roles;
                        try
                        {
                            roles = game.GetRoles().OrderBy(r => r.Id).ToList();
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
                    else if (requests.Length == 2) //role/{id}
                    {
                        string id = requests[1];
                        Role role;
                        try
                        {
                            role = game.GetRole(id);
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
                    if (requests.Length == 1)
                    {
                        List<DNWS.Werewolf.Action> actions;
                        try
                        {
                            actions = game.GetActions().OrderBy(a => a.Id).ToList();
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
                    else if (requests.Length == 2)
                    {
                        string id = requests[1];
                        DNWS.Werewolf.Action act;
                        try
                        {
                            act = game.GetAction(id);
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
                    else if (requests.Length == 3 && requests[1].ToUpper() == "FINDBYROLE")
                    {
                        string roleid = requests[2];
                        List<DNWS.Werewolf.Action> actions;
                        try
                        {
                            actions = game.GetActionByRoleId(roleid).OrderBy(a => a.Id).ToList();
                        }
                        catch (Exception ex)
                        {
                            response.SetBodyString(ex.ToString());
                            response.Status = 500;
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
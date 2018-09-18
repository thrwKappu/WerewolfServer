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

        protected HTTPResponse WerewolfProcess(string[] requests, string method)
        {
            HTTPResponse response = new HTTPResponse(200);
            WerewolfGame game = WerewolfGame.GetInstance();
            string path = requests[0].ToUpper();
            string action = method.ToUpper();

            response.Type = "application/json";
            string json = "";

            if (path == REQUEST_PLAYER)
            {
                return new HTTPResponse(400);
            }
            else if (path == REQUEST_GAME)
            {
                return new HTTPResponse(400);
            }
            else if (path == REQUEST_ROLE)
            {
                if (action == HTTP_GET)
                {
                    if (requests.Length == 1) //role/
                    {
                        List<Role> roles = game.GetRoles().OrderBy(r => r.Id).ToList();
                        //Flatten action list
                        foreach(Role role in roles) {
                            List<DNWS.Werewolf.Action> actions = new List<DNWS.Werewolf.Action>();
                            foreach(ActionRole ar in role.ActionRoles) {
                                ar.Action.ActionRoles = null;
                                actions.Add(ar.Action);
                            }
                            role.Actions = actions.OrderBy(a => a.Id).ToList();
                            role.ActionRoles = null;
                        }
                        try {
                            json = JsonConvert.SerializeObject(roles,
                                                            Newtonsoft.Json.Formatting.None,
                                                            new JsonSerializerSettings {
                                                                NullValueHandling = NullValueHandling.Ignore
                                                            });
                            roles = null;
                        } catch (Exception ex) {
                            response.SetBodyString(ex.ToString());
                            response.Status = 500;
                            return response;
                        }
                    }
                    else if (requests.Length == 2) //role/{id}
                    {
                        string id = requests[1];
                        Role role = game.GetRole(id);
                        List<DNWS.Werewolf.Action> actions = new List<DNWS.Werewolf.Action>();
                        foreach(ActionRole ar in role.ActionRoles) {
                            ar.Action.ActionRoles = null;
                            actions.Add(ar.Action);
                        }
                        role.Actions = actions.OrderBy(a => a.Id).ToList();
                        role.ActionRoles = null;
                        try {
                            json = JsonConvert.SerializeObject(role,
                                                            Newtonsoft.Json.Formatting.None,
                                                            new JsonSerializerSettings {
                                                                NullValueHandling = NullValueHandling.Ignore
                                                            });
                            role = null;
                        } catch (Exception ex) {
                            response.SetBodyString(ex.ToString());
                            response.Status = 500;
                            return response;
                        }
                    }
                } else {
                    return new HTTPResponse(400);
                }
            }
            else if (path == REQUEST_ACTION)
            {
                if (action == HTTP_GET)
                {
                    if (requests.Length == 1)
                    {
                        List<DNWS.Werewolf.Action> actions = game.GetActions().OrderBy(a => a.Id).ToList();
                        //Flatten action list
                        foreach(DNWS.Werewolf.Action act in actions) {
                            List<Role> roles = new List<Role>();
                            foreach(ActionRole ar in act.ActionRoles) {
                                ar.Role.ActionRoles = null;
                                roles.Add(ar.Role);
                            }
                            act.Roles = roles.OrderBy(r => r.Id).ToList();
                            act.ActionRoles = null;
                        }
                        try {
                            json = JsonConvert.SerializeObject(actions,
                                                            Newtonsoft.Json.Formatting.None,
                                                            new JsonSerializerSettings {
                                                                NullValueHandling = NullValueHandling.Ignore
                                                            });
                            actions = null;
                        } catch (Exception ex) {
                            response.SetBodyString(ex.ToString());
                            response.Status = 500;
                            return response;
                        }
                    }
                    else if (requests.Length == 2)
                    {
                        string id = requests[1];
                        DNWS.Werewolf.Action act = game.GetAction(id);
                        List<Role> roles = new List<Role>();
                        foreach(ActionRole ar in act.ActionRoles) {
                            ar.Role.ActionRoles = null;
                            roles.Add(ar.Role);
                        }
                        act.Roles = roles.OrderBy(r => r.Id).ToList();
                        act.ActionRoles = null;
                        try {
                            json = JsonConvert.SerializeObject(act,
                                                            Newtonsoft.Json.Formatting.None,
                                                            new JsonSerializerSettings {
                                                                NullValueHandling = NullValueHandling.Ignore
                                                            });
                            act = null;
                        } catch (Exception ex) {
                            response.SetBodyString(ex.ToString());
                            response.Status = 500;
                            return response;
                        }
                    }
                    else if (requests.Length == 3 && requests[1].ToUpper() == "FINDBYROLE")
                    {
                        string roleid = requests[2];
                        List<DNWS.Werewolf.Action> actions = game.GetActionByRoleId(roleid).OrderBy(a => a.Id).ToList();
                        //Flatten action list
                        foreach(DNWS.Werewolf.Action act in actions) {
                            List<Role> roles = new List<Role>();
                            foreach(ActionRole ar in act.ActionRoles) {
                                ar.Action.ActionRoles = null;
                                roles.Add(ar.Role);
                            }
                            act.Roles = roles.OrderBy(r => r.Id).ToList();
                            act.ActionRoles = null;
                        }
                        json = JsonConvert.SerializeObject(actions,
                                                           Newtonsoft.Json.Formatting.None,
                                                           new JsonSerializerSettings {
                                                               NullValueHandling = NullValueHandling.Ignore
                                                           });
                    }
                } else {
                    return new HTTPResponse(400);
                }
            }
            else if (path == REQUEST_CHAT)
            {
                return new HTTPResponse(400);
            }
            response.SetBodyString(json);
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
                response = WerewolfProcess(path, request.Method);
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
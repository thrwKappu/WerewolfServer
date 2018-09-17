using System;
using System.Collections.Generic;
using System.Text;
using DNWS.Werewolf;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

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

        protected HTTPResponse WerewolfProcess(string[] requests)
        {
            HTTPResponse response = new HTTPResponse(200);
            WerewolfGame game = WerewolfGame.GetInstance();

            if (requests[0] == "player")


            return response;
        }
        public HTTPResponse GetResponse(HTTPRequest request)
        {
            HTTPResponse response = null;
            StringBuilder sb = new StringBuilder();
            String[] path = Regex.Split(request.Url, "/");
            if(path.Length > 2) {
                String action = path[2];
                String[] parameters = new String[path.Length - 2];
                Array.Copy(path, 3, parameters, 0, path.Length - 2);
                response = WerewolfProcess(path);
            } else {
                response = new HTTPResponse(400);
            }
            //response.body = Encoding.UTF8.GetBytes(sb.ToString());
            return response;
        }

        public HTTPResponse PostProcessing(HTTPResponse response)
        {
            throw new NotImplementedException();
        }
    }
}
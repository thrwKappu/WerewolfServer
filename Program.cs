using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using Microsoft.Extensions.Configuration;
using System.Threading;
using DNWS.Werewolf;

namespace DNWS
{
    // Main class
    public class Program
    {
        static public IConfigurationRoot Configuration { get; set; }

        // Log to console
        public void Log(String msg)
        {
            Console.WriteLine(msg);
        }

        // Start the server, Singleton here
        public void Start()
        {
            // Start server
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("config.json");
            Configuration = builder.Build();
            // FIXME: refactor this into separate plugin system
            WerewolfManager wm = new WerewolfManager();
            wm.Start();
            DotNetWebServer ws = DotNetWebServer.GetInstance(this);
            ws.Start();
        }

        static void Main(string[] args)
        {
            Program p = new Program();
            p.Start();
        }
    }

    /// <summary>
    /// HTTP processor will process each http request
    /// </summary>

    public class HTTPProcessor
    {
        protected class PluginInfo
        {
            protected string _path;
            protected string _type;
            protected bool _preprocessing;
            protected bool _postprocessing;
            protected IPlugin _reference;

            public string path
            {
                get { return _path;}
                set {_path = value;}
            }
            public string type
            {
                get { return _type;}
                set {_type = value;}
            }
            public bool preprocessing
            {
                get { return _preprocessing;}
                set {_preprocessing = value;}
            }
            public bool postprocessing
            {
                get { return _postprocessing;}
                set {_postprocessing = value;}
            }
            public IPlugin reference
            {
                get { return _reference;}
                set {_reference = value;}
            }
        }
        // Get config from config manager, e.g., document root and port
        protected string ROOT = Program.Configuration["DocumentRoot"];
        protected HttpListenerContext _context;
        protected Program _parent;
        protected Dictionary<string, PluginInfo> plugins;

        /// <summary>
        /// Constructor, set the client socket and parent ref, also init stat hash
        /// </summary>
        /// <param name="client">Client socket</param>
        /// <param name="parent">Parent ref</param>
        public HTTPProcessor(HttpListenerContext context, Program parent)
        {
            _context = context;
            _parent = parent;
            plugins = new Dictionary<string, PluginInfo>();
            // load plugins
            var sections = Program.Configuration.GetSection("Plugins").GetChildren();
            foreach(ConfigurationSection section in sections) {
                PluginInfo pi = new PluginInfo();
                pi.path = section["Path"];
                pi.type = section["Class"];
                pi.preprocessing = section["Preprocessing"].ToLower().Equals("true");
                pi.postprocessing = section["Postprocessing"].ToLower().Equals("true");
                pi.reference = (IPlugin) Activator.CreateInstance(Type.GetType(pi.type));
                plugins[section["Path"]] = pi;
            }
        }

        /// <summary>
        /// Get a file from local harddisk based on path
        /// </summary>
        /// <param name="path">Absolute path to the file</param>
        /// <returns></returns>
        protected HTTPResponse getFile(String path)
        {
            HTTPResponse response = null;

            // Guess the content type from file extension
            string fileType = "text/html";
            if (path.ToLower().EndsWith("jpg") || path.ToLower().EndsWith("jpeg"))
            {
                fileType = "image/jpeg";
            }
            if (path.ToLower().EndsWith("png"))
            {
                fileType = "image/png";
            }

            // Try to read the file, if not found then 404, otherwise, 500.
            try
            {
                response = new HTTPResponse(200);
                response.Type = fileType;
                response.Body = System.IO.File.ReadAllBytes(path);
            }
            catch (FileNotFoundException ex)
            {
                response = new HTTPResponse(404);
                response.Body = Encoding.UTF8.GetBytes("<h1>404 Not found</h1>" + ex.Message);
            }
            catch (Exception ex)
            {
                response = new HTTPResponse(500);
                response.Body = Encoding.UTF8.GetBytes("<h1>500 Internal Server Error</h1>" + ex.Message);
            }
            return response;

        }

        /// <summary>
        /// Get a request from client, process it, then return response to client
        /// </summary>
        public void Process()
        {
            HttpListenerRequest req = _context.Request;
            HttpListenerResponse res = _context.Response;
            HTTPRequest request = new HTTPRequest(req);
            HTTPResponse response = new HTTPResponse(200);
            request.addProperty("RemoteEndPoint", req.RemoteEndPoint.ToString());

            if(request.Status != 200) {
                response = new HTTPResponse(request.Status);
            }
            else
            {
                bool processed = false;
                // pre processing
                foreach(KeyValuePair<string, PluginInfo> plugininfo in plugins) {
                    if(plugininfo.Value.preprocessing) {
                        plugininfo.Value.reference.PreProcessing(request);
                    }
                }
                // plugins
                foreach(KeyValuePair<string, PluginInfo> plugininfo in plugins) {
                    if(request.Path.ToLower().Equals(plugininfo.Key.ToLower())) {
                        response = plugininfo.Value.reference.GetResponse(request);
                        processed = true;
                    }
                }
                // local file
                if(!processed) {
                    if (request.Filename.Equals(""))
                    {
                        response = getFile(ROOT + "/index.html");
                    }
                    else
                    {
                        response = getFile(ROOT + "/" + request.Filename);
                    }
                }
                // post processing pipe
                foreach(KeyValuePair<string, PluginInfo> plugininfo in plugins) {
                    if(plugininfo.Value.postprocessing) {
                        response = plugininfo.Value.reference.PostProcessing(response);
                    }
                }
            }
            res.StatusCode = response.Status;
            foreach (string key in response.Headers.Keys)
            {
                res.AddHeader(key, response.Headers[key]);
            }
            if(response.Body != null) {
                byte[] buffer = response.Body;
                res.ContentLength64 = buffer.Length;
                System.IO.Stream output = res.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();
            }
            res.Close();
        }
    }

    public class TaskInfo
    {
        private HTTPProcessor _hp;
        public HTTPProcessor hp 
        { 
            get {return _hp;}
            set {_hp = value;}
        }
        public TaskInfo(HTTPProcessor hp)
        {
            this.hp = hp;
        }
    }

    /// <summary>
    /// Main server class, open the socket and wait for client
    /// </summary>
    public class DotNetWebServer
    {
        protected int _maxThread;
        protected String _threadModel;
        protected Program _parent;
        protected HttpListener listener;
        private static DotNetWebServer _instance = null;
        protected int id;

        private DotNetWebServer(Program parent)
        {
            _parent = parent;
            id = 0;
        }

        /// <summary>
        /// Singleton here
        /// </summary>
        /// <param name="parent">parent ref</param>
        /// <returns></returns>
        public static DotNetWebServer GetInstance(Program parent)
        {
            if (_instance == null)
            {
                _instance = new DotNetWebServer(parent);
            }
            return _instance;
        }

        public void ThreadProc(Object stateinfo)
        {
            TaskInfo ti = stateinfo as TaskInfo;
            ti.hp.Process();
        }

        /// <summary>
        /// Server starting point
        /// </summary>
        public void Start()
        {
            // Start the listener
            try
            {
                if (!HttpListener.IsSupported)
                {
                    Console.WriteLine("Http Listener is not supported");
                    return;
                }
                string[] prefixes = { "http://*:" + Program.Configuration["Port"] + "/" };
                listener = new HttpListener();
                foreach (string s in prefixes)
                {
                    listener.Prefixes.Add(s);
                }
                listener.Start();
                _parent.Log("Server started");

            }
            catch (Exception ex)
            {
                _parent.Log(ex.ToString());
                return;
            }
            // Run the processing loop
            while (true)
            {
                try
                {
                    // Wait for client
                    HttpListenerContext context = listener.GetContext();
                    // Get one, show some info
                    _parent.Log("Client accepted:" + context.Request.RemoteEndPoint.ToString());
                    HTTPProcessor hp = new HTTPProcessor(context, _parent);
                    Thread thread = new Thread(new ThreadStart(hp.Process));
                    id++;
                    thread.Start();
                }
                catch (Exception ex)
                {
                    _parent.Log("Server starting error: " + ex.Message + "\n" + ex.StackTrace);
                }
            }
        }
    }
}

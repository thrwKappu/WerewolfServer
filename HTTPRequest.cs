using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace DNWS
{
  public class HTTPRequest
    {
        protected String _url;
        protected String _filename;
        protected String _path;
        protected static Dictionary<String, String> _propertyListDictionary = null;
        protected static Dictionary<String, String> _requestListDictionary = null;

        protected String _body;

        protected int _status;

        protected String _method;

        public const string CONTENT_TYPE_APP_X_HTTP_FORM_URLENCODED = "application/x-www-form-urlencoded";
        public const string CONTENT_TYPE_APP_JSON = "application/json";

        public String Url
        {
            get { return _url; }
        }

        public String Filename
        {
            get { return _filename; }
        }
        public String Path
        {
            get { return _path; }
        }

        public String Body
        {
            get { return _body; }
        }

        public int Status
        {
            get { return _status; }
        }

        public String Method
        {
            get { return _method; }
        }
        public HTTPRequest(HttpListenerRequest req)
        {
            _propertyListDictionary = new Dictionary<String, String>();
            _method = req.HttpMethod;
            if (!(new [] {"get", "post", "put", "delete"}).Contains(_method.ToLower())) {
                _status = 501;
                return;
            }
            _status = 200;
            //_url = req.Url.ToString();
            _url = req.Url.AbsolutePath;
            String[] urls = Regex.Split(_url, "/");
            _filename = urls[urls.Length - 1];
            _path = urls[1];
            String[] parts = Regex.Split(_filename, "[?]");
            if (parts.Length > 1 && parts[1].Contains('&'))
            {
                //Ref: http://stackoverflow.com/a/4982122
                _requestListDictionary = parts[1].Split('&').Select(x => x.Split('=')).ToDictionary(x => x[0].ToLower(), x => x[1]);
            }
            else
            {
                _requestListDictionary = new Dictionary<String, String>();
                if (parts.Length > 1)
                {
                    String[] requestParts = Regex.Split(parts[1], "[=]");
                    if (requestParts.Length > 1)
                    {
                        _requestListDictionary.Add(requestParts[0], requestParts[1]);
                    }
                }
            }

            foreach (string key in req.Headers)
            {
                addProperty(key, req.Headers.GetValues(key)[0]);
            }
            if (req.HasEntityBody)
            {
                System.IO.Stream rs = req.InputStream;
                System.Text.Encoding encoding = req.ContentEncoding;
                System.IO.StreamReader reader = new System.IO.StreamReader(rs, encoding);
                string body = reader.ReadToEnd();
                rs.Close();
                reader.Close();
                if (req.ContentType.ToLower() == CONTENT_TYPE_APP_X_HTTP_FORM_URLENCODED)
                {
                    string[] lines = Regex.Split(body, "\r\n");
                    foreach (string line in lines)
                    {
                        String[] pair = Regex.Split(line, ":"); //FIXME
                        pair = pair.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                        if (pair[0].Length > 1)
                        { //FIXME, this is a quick hack
                            Dictionary<String, String> _bodys = pair[0].Split('&').Select(x => x.Split('=')).ToDictionary(x => x[0].ToLower(), x => x[1]);
                            _requestListDictionary = _requestListDictionary.Concat(_bodys).ToDictionary(x => x.Key, x => x.Value);
                        }
                    }
                }
                else if (req.ContentType.ToLower() == CONTENT_TYPE_APP_JSON)
                {
                    _body = body;
                }
            }
        }
        public String getPropertyByKey(String key)
        {
            if (_propertyListDictionary.ContainsKey(key.ToLower()))
            {
                return _propertyListDictionary[key.ToLower()];
            }
            else
            {
                return null;
            }
        }
        public String getRequestByKey(String key)
        {
            if (_requestListDictionary.ContainsKey(key.ToLower()))
            {
                return _requestListDictionary[key.ToLower()];
            }
            else
            {
                return null;
            }
        }

        public void addProperty(String key, String value)
        {
            _propertyListDictionary[key.ToLower()] = value;
        }
        public void addRequest(String key, String value)
        {
            _requestListDictionary[key.ToLower()] = value;
        }
    }
}
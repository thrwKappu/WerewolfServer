using System;
using System.Collections.Generic;
using System.Linq;
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
        public HTTPRequest(String request)
        {
            _propertyListDictionary = new Dictionary<String, String>();
            String[] lines = Regex.Split(request, "\\n");

            if (lines.Length == 0)
            {
                _status = 500;
                return;
            }

            String[] statusLine = Regex.Split(lines[0], "\\s");
            if (statusLine.Length != 4)
            { // too short something is wrong
                _status = 401;
                return;
            }
            if ((new [] {"get", "post", "put", "delete"}).Contains(statusLine[0].ToLower())) {
                _method = statusLine[0].ToUpper();
            } else {
                _status = 501;
                return;
            }
            _status = 200;

            _url = statusLine[1];
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

            if (lines.Length == 1) return;
            string contentType = null;
            StringBuilder bodyBuilder = new StringBuilder();
            bool isHeader = true;
            for (int i = 1; i != lines.Length; i++)
            {
                if (isHeader)
                {
                    String[] pair = Regex.Split(lines[i], ":"); //FIXME
                    pair = pair.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                    if (pair.Length == 0) continue;
                    if (pair.Length == 1)
                    {
                        isHeader = false;
                        continue;
                    }
                    else
                    { // Length == 2, GET url request
                        if(pair.Length > 2) {
                            pair[1] = String.Join(":", pair, 1, pair.Length - 1);
                        }
                        pair[1] = pair[1].Replace("\r\n", "").Replace("\r", "").Replace("\n", "");
                        addProperty(pair[0].Trim(), pair[1].Trim());
                        //FIXME: Another quick hack, this rely on HTTP specification, so it should always work, hopefully
                        if (pair[0].Trim().ToLower() == "content-type") {
                            contentType = pair[1].Trim().ToLower();
                        }
                    }
                }
                else
                {
                    if (lines[i] == "") {
                        continue;
                    }
                    if (contentType == CONTENT_TYPE_APP_X_HTTP_FORM_URLENCODED)
                    { // handle post body, but we should skip json body
                        String[] pair = Regex.Split(lines[i], ":"); //FIXME
                        pair = pair.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                        if (pair[0].Length > 1)
                        { //FIXME, this is a quick hack
                            Dictionary<String, String> _bodys = pair[0].Split('&').Select(x => x.Split('=')).ToDictionary(x => x[0].ToLower(), x => x[1]);
                            _requestListDictionary = _requestListDictionary.Concat(_bodys).ToDictionary(x => x.Key, x => x.Value);
                        }
                    }
                    else if (contentType == CONTENT_TYPE_APP_JSON)
                    {
                        bodyBuilder.Append(lines[i]);
                    }

                }
            }
            if (contentType == "application/json") {
              _body = bodyBuilder.ToString();
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
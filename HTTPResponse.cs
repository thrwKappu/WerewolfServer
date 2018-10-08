using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DNWS
{
    // RFC1945
    public class HTTPResponse
    {
        protected int _status = 404;
        public int Status
        {
            get { return _status; }
            set { _status = value; }
        }

        protected byte[] _body;
        public byte[] Body
        {
            get { return _body; }
            set { _body = value; }
        }

        public void SetBodyString(string str)
        {
            Body = Encoding.UTF8.GetBytes(str);
        }

        public void SetBodyJson(Object obj)
        {
            string json = JsonConvert.SerializeObject(obj,
                                Newtonsoft.Json.Formatting.None,
                                new JsonSerializerSettings
                                {
                                    NullValueHandling = NullValueHandling.Ignore
                                });
            obj = null;
            SetBodyString(json);
        }


        protected string _type = "text/html";
        public string Type
        {
            get { return _type; }
            set { _type = value; }
        }

        public Dictionary<string, string> Headers
        {
            get
            {
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers["Content-Type"] = Type;
                headers["Connection"] = "close";
                headers["Server"] = "DNWS 1.0 (Werewolf)";
                return headers;
            }
        }
        public String Header
        {
            get
            {
                StringBuilder headerResponse = new StringBuilder("HTTP/1.0 ");
                switch (_status)
                {
                    case 200:
                        headerResponse.Append("200 OK");
                        break;
                    case 201:
                        headerResponse.Append("201 Created");
                        break;
                    case 400:
                        headerResponse.Append("400 Bad Request");
                        break;
                    case 403:
                        headerResponse.Append("403 Forbidden");
                        break;
                    case 404:
                        headerResponse.Append("404 Not Found");
                        break;
                    case 500:
                        headerResponse.Append("500 Internal Server Error");
                        break;
                    case 501:
                    default:
                        headerResponse.Append("501 Not Implemented");
                        break;

                }

                headerResponse.Append("\r\n");
                headerResponse.Append("Content-Type: ").Append(Type).Append("\r\n");
                headerResponse.Append("Connection: close\r\n");
                headerResponse.Append("Server: DNWS 1.0\r\n");
                headerResponse.Append("\r\n");
                return headerResponse.ToString();
            }
        }

        public HTTPResponse(int status)
        {
            _status = status;
        }

    }
}

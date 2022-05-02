using Core.Api.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Core.Api.Utils
{
    public class Utility
    {
        public string GetRequest(string url)
        {
            HttpWebRequest webRequest = WebRequest.Create(url) as HttpWebRequest;

            if (webRequest == null)
                throw new ArgumentNullException("request is not a http request");

            webRequest.Method = "GET";

            try
            {
                HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();

                if (webResponse.StatusCode == HttpStatusCode.OK)
                {
                    using (var receiveStream = webResponse.GetResponseStream())
                    {
                        using (var readStream = new StreamReader(receiveStream, new UTF8Encoding()))
                        {
                            var data = readStream.ReadToEnd();

                            var _response = JsonConvert.DeserializeObject<GenericModel>(data);

                            return _response.Nonce;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return null;
        }
    }
}

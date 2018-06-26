using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BulkDownloadWeb
{
    /// <summary>
    /// Used for caching and retrieving access token
    /// </summary>
    public class AccessTokenInfo
    {
        public string AccessToken { get; set; }
        public DateTime Expires { get; set; }

        public AccessTokenInfo() { }

        public AccessTokenInfo(string accessToken, DateTime expires)
        {
            this.AccessToken = accessToken;
            this.Expires = expires;
        }
    }
}
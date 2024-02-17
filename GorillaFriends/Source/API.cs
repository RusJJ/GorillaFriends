using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

// This is an API for more enchanced friends management
namespace GorillaFriends
{
    enum eAPIRequest : byte
    {
        LOGIN = 0,

        MAX_REQUESTS
    };
    internal class API
    {
        internal static string apiBase = "https://example.com";
        internal static string token = string.Empty;
        internal static HttpClient client = new HttpClient();

        public static bool Login(string username, string passhash)
        {
            if(token != string.Empty) return false;

            Dictionary<string, string> query = new Dictionary<string, string>();
            query.Add("username", username);
            query.Add("password", passhash);
            DoQuery(eAPIRequest.LOGIN, query, LoginAnswer, false);

            return true;
        }

        internal static void LoginAnswer(string result)
        {

        }
        public static void Logout()
        {
            token = string.Empty;
        }
        public static string GetHashFromPass(string pass)
        {
            return string.Empty;
        }
        async internal static void DoQuery(eAPIRequest request, Dictionary<string, string> query, Action<string> listener, bool needsToken = true)
        {
            if (query == null) return;

            int queries = query.Count;

            if (queries > 0)
            {
                string readyQuery = apiBase + "?action=" + ((int)request) + "&";
                if (needsToken) readyQuery += "token=" + token + "&";

                Array keys = query.Keys.ToArray();
                Array values = query.Values.ToArray();
                for (int i = query.Count - 1; i >= 0; i--)
                {
                    readyQuery += keys.GetValue(i) + "=" + values.GetValue(i);
                    if (i != 0) readyQuery += "&";
                }
                query.Clear();

                string result = await client.GetStringAsync(readyQuery);
                if(listener != null) listener(result);
            }
            return;
        }
    }
}

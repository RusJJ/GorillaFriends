using System.IO;
using System.Net.Http;

namespace GorillaFriends
{
    class WebVerified
    {
        public const string m_szURL = "https://raw.githubusercontent.com/RusJJ/GorillaFriends/file_sources/gorillas.verified";
        async public static void LoadListOfVerified()
        {
            HttpClient client = new HttpClient();
            string result = await client.GetStringAsync(m_szURL);
            using (StringReader reader = new StringReader(result))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    Main.m_listVerifiedUserIds.Add(line);
                }
            }
        }
    }
}

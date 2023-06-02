using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using xNetStandard;

namespace vk_comment_bot
{
    class GetNewsFeed
    {
        public HttpRequest HttpRequest { get; set; } = new();

        public dynamic? GetNews(string q, int count, string startFrom, string token)
        {
            HttpRequest.EnableEncodingContent = false;
            HttpRequest.AddHeader("Authorization", $"Bearer {token}");

            var answer =
                HttpRequest.Get(
                    $"https://api.vk.com/method/newsfeed.search?q={q}&extended=1&count={count}&start_from={startFrom}&access_token={token}&v=5.131");

            return JsonConvert.DeserializeObject<dynamic>(answer.ToString());
        }
    }
}

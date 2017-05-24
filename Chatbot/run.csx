using System.Net;
using System.Linq;
using Newtonsoft.Json;

static Random rnd = new Random();

static string DetermineSubreddit(dynamic data, ChatPlatform platform) {
    if (platform == ChatPlatform.HipChat) {
        var message = data?.item?.message?.message?.ToString();

        var map = new Dictionary<string, string> {
            {"/horse", "horses"},
            {"/pupper", "rarepuppers"},
            {"/floof", "floof"},
            {"/snek", "snek"},
            {"/dog", "dogs"},
            {"/doggif", "doggifs"},
            {"/up", "catsstandingup"},
            {"/down", "catssittingdown"},
            {"/cat", "cats"},
            {"/loaf", "catloaf"},
            {"/businesscat", "catsinbusinessattire"},
            {"/birb", "birbs"},
            {"/weather", "catloaf"},
            {"/test", "catloaf"}
        };

        string subreddit = null;
        if (map.TryGetValue(message, out subreddit)) {
            return subreddit;
        }
    }

    return null;
}

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    // Get request body
    dynamic data = await req.Content.ReadAsAsync<object>();

    var platform = DetermineChatPlatform(data);
    string subreddit = DetermineSubreddit(data, platform);
    dynamic post = await GetPost(subreddit);

    var url = post?.data?.url;
    var title = post?.data?.title;

    if (platform == ChatPlatform.HipChat) {
        return req.CreateResponse(HttpStatusCode.OK, new {
            message = $"{title} {url}",
            message_format = "text"
        });
    } else {
        return req.CreateResponse(HttpStatusCode.BadRequest, "Unknown platform");
    }
}

static async Task<dynamic> GetPost(string subredditName) {
    using (var client = new HttpClient()) {
        try {
            client.BaseAddress = new Uri("https://reddit.com");
            var response = await client.GetAsync($"/r/{subredditName}.json?t=day&limit=100");
            response.EnsureSuccessStatusCode();
            var stringResponse = await response.Content.ReadAsStringAsync();
            dynamic jsonResponse = JsonConvert.DeserializeObject(stringResponse);

            IEnumerable<dynamic> posts = jsonResponse?.data?.children;

            var randomPost = posts
                .Where(p => new[]{
                    ".jpg", ".jpeg", ".gif", ".png"
                    }.Select(e => ((string)p?.data?.url).EndsWith(e)).Any(r => r == true))
                .OrderBy(a => Guid.NewGuid())
                .FirstOrDefault();

            return randomPost;
        }
        catch (HttpRequestException e)
        {
            return null;
        }
    }
}

static ChatPlatform DetermineChatPlatform(dynamic data) {
    if (data?["event"] == "room_message") {
        return ChatPlatform.HipChat;
    } else {
        return ChatPlatform.Unknown;
    }
}

enum ChatPlatform {
    Unknown,
    HipChat,
    Slack
}

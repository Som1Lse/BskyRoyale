using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine.Networking;
using Web.Helpers;
using Web.Twitter.API;
using Web.Twitter.DataStructures;


namespace BskyRoyale {
    [Serializable]
    public class BskyProfile {
        public string did;
        public string handle;
        public string displayName;
        public string description;
        public int followersCount;
        public int followsCount;
        public int postsCount;
    }

    [Serializable]
    public class BskySubject {
        public string did;
        public string handle;
        public string displayName;
        public string description;
    }

    [Serializable]
    public class BskyFollows {
        public BskySubject subject;
        public BskySubject[] follows;
    }

    [Serializable]
    public class BskyPostRecord {
        public string createdAt;
        public string text;
    }

    [Serializable]
    public class BskyPost {
        public string uri;
        public string did;
        public BskySubject author;
        public BskyPostRecord record;
    }

    [Serializable]
    public class BskFeed {
        public BskyPost post;
    }

    [Serializable]
    public class BskyAuthorFeed {
        public string cursor;
        public BskFeed[] feed;
    }

    public static class BskyPatch {
        public static bool Enabled {
            get; set;
        }

        static IList<string> idToDid = new List<string>();
        static IDictionary<string, int> didToId = new Dictionary<string, int>();

        static async Task<string> HttpRequestAsync(string url, IDictionary<string, string> args) {
            var query = args.Join(x => $"{x.Key}={UnityWebRequest.EscapeURL(x.Value)}", "&");
            if (query != "") {
                url += $"?{query}";
            }

            var request = UnityWebRequest.Get(url);
            await request.SendWebRequest();

            if (request.error != null) {
                return string.Empty;
            }

            return request.downloadHandler.text;
        }

        static async Task<UserProfile> GetUserProfileByUsername(
            string username
        ) {
            if (!username.Contains(".")) {
                username += ".bsky.social";
            }

            // https://developer.x.com/en/docs/x-api/v1/accounts-and-users/follow-search-get-users/api-reference/get-users-show
            // https://docs.bsky.app/docs/api/app-bsky-actor-get-profile
            var json = await HttpRequestAsync(
                "https://public.api.bsky.app/xrpc/app.bsky.actor.getProfile",
                new Dictionary<string, string>{
                    { "actor", username },
                }
            );

            var profile = Json.Parse<BskyProfile>(json);

            return new UserProfile {
                @protected = profile.followsCount <= 0,
            };
        }

        static async Task<UserProfile[]> GetUsersFollowingByUsername(
            string username
        ) {
            if (!username.Contains(".")) {
                username += ".bsky.social";
            }

            // https://developer.x.com/en/docs/x-api/v1/accounts-and-users/follow-search-get-users/api-reference/get-friends-list
            // https://docs.bsky.app/docs/api/app-bsky-graph-get-follows
            var json = await HttpRequestAsync(
                "https://public.api.bsky.app/xrpc/app.bsky.graph.getFollows",
                new Dictionary<string, string>{
                    { "actor", username },
                    { "limit", "100" },
                }
            );

            var follows = Json.Parse<BskyFollows>(json);

            var result = new List<UserProfile>();

            foreach (var follow in follows.follows) {
                var did = follow.did;
                int id;
                if (!didToId.TryGetValue(did, out id)) {
                    id = idToDid.Count;
                    idToDid.Add(did);
                    didToId[did] = id;
                }

                var user = new UserProfile {
                    @protected = false,
                    id = id,
                };

                result.Add(user);
            }

            return result.ToArray();
        }

        static async Task<Tweet[]> GetLatestTweetsFromUserByUserId(
            string userID, int maximumTweetsToGet, bool includeRetweets
        ) {
            // https://developer.x.com/en/docs/x-api/v1/tweets/timelines/api-reference/get-statuses-user_timeline
            // https://docs.bsky.app/docs/api/app-bsky-feed-get-author-feed
            var json = await HttpRequestAsync(
                "https://public.api.bsky.app/xrpc/app.bsky.feed.getAuthorFeed",
                new Dictionary<string, string>{
                    { "actor", idToDid[int.Parse(userID)] },
                    { "limit", maximumTweetsToGet.ToString() },
                }
            );

            var feed = Json.Parse<BskyAuthorFeed>(json);

            var result = new List<Tweet>();
            foreach (var post in feed.feed) {
                result.Add(new Tweet {
                    full_text = post.post.record.text,
                });
            }

            return result.ToArray();
        }

        [HarmonyPatch(typeof(WebHelper), "GetTwitterApiAccessToken")]
        [HarmonyPrefix]
        public static bool GetTwitterApiAccessToken_Patch(ref WebAccessToken __result) {
            if (!Enabled) {
                return true;
            }

            __result = new WebAccessToken();
            __result.token_type = "";
            __result.access_token = "";

            return false;
        }

        [HarmonyPatch(typeof(TwitterRestApiHelper), "GetUserProfileByUsername")]
        [HarmonyPrefix]
        public static bool GetUserProfileByUsername_Patch(object[] __args, ref Task<UserProfile> __result) {
            if (!Enabled) {
                return true;
            }

            __result = GetUserProfileByUsername((string)__args[0]);

            return false;
        }
        [HarmonyPatch(typeof(TwitterRestApiHelper), "GetUsersFollowingByUsername")]
        [HarmonyPrefix]
        public static bool GetUsersFollowingByUsername_Patch(object[] __args, ref Task<UserProfile[]> __result) {
            if (!Enabled) {
                return true;
            }

            __result = GetUsersFollowingByUsername((string)__args[0]);

            return false;
        }
        [HarmonyPatch(typeof(TwitterRestApiHelper), "GetLatestTweetsFromUserByUserId")]
        [HarmonyPrefix]
        public static bool GetLatestTweetsFromUserByUserId_Patch(object[] __args, ref Task<Tweet[]> __result) {
            if (!Enabled) {
                return true;
            }

            __result = GetLatestTweetsFromUserByUserId((string)__args[0], (int)__args[2], (bool)__args[3]);

            return false;
        }
    }
}

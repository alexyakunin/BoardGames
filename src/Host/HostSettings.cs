using System;
using System.Text;
using Stl.DependencyInjection;

namespace BoardGames.Host
{
    [Settings("BoardGames")]
    public class HostSettings
    {
        // Fusion
        public string PublisherId { get; set; } = "p";

        // DBs
        public string UsePostgreSql { get; set; } = "";

        // Sign-in
        public string MicrosoftClientId { get; set; } = "6839dbf7-d1d3-4eb2-a7e1-ce8d48f34d00";
        public string MicrosoftClientSecret { get; set; } =
            Encoding.UTF8.GetString(Convert.FromBase64String(
                "REFYeH4yNTNfcVNWX2h0WkVoc1V6NHIueDN+LWRxUTA2Zw=="));
        public string GitHubClientId { get; set; } = "7a38bc415f7e1200fee2";
        public string GitHubClientSecret { get; set; } =
            Encoding.UTF8.GetString(Convert.FromBase64String(
                "OGNkMTAzM2JmZjljOTk3ODc5MjhjNTNmMmE3Y2Q1NWU0ZmNlNjU0OA=="));
    }
}

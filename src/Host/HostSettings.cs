using System;
using System.Text;
using Stl.DependencyInjection;

namespace BoardGames.Host
{
    [RegisterSettings("BoardGames")]
    public class HostSettings
    {
        // Web
        public bool AssumeHttps { get; set; } = false;
        public bool UseHttpsRedirection { get; set; } = false;
        public bool UseForwardedHeaders { get; set; } = true;

        // Fusion
        public string PublisherId { get; set; } = "p";

        // DBs
        public string UsePostgreSql { get; set; } =
            "Server=localhost;Database=board_games_dev;Port=5432;User Id=postgres;Password=Fusion.0.to.1";
        public bool UseSqlite { get; set; } = false;

        // Sign-in
        public string MicrosoftClientId { get; set; } = "6839dbf7-d1d3-4eb2-a7e1-ce8d48f34d00";
        public string MicrosoftClientSecret { get; set; } =
            Encoding.UTF8.GetString(Convert.FromBase64String(
                "REFYeH4yNTNfcVNWX2h0WkVoc1V6NHIueDN+LWRxUTA2Zw=="));
        public string GitHubClientId { get; set; } = "7a38bc415f7e1200fee2";
        public string GitHubClientSecret { get; set; } =
            Encoding.UTF8.GetString(Convert.FromBase64String(
                "OGNkMTAzM2JmZjljOTk3ODc5MjhjNTNmMmE3Y2Q1NWU0ZmNlNjU0OA=="));

        public string DataProtectionCert { get; set; } = @"
MIIGIQIBAzCCBecGCSqGSIb3DQEHAaCCBdgEggXUMIIF0DCCAs8GCSqGSIb3DQEHBqCCAsAwggK8
AgEAMIICtQYJKoZIhvcNAQcBMBwGCiqGSIb3DQEMAQYwDgQI+OuKRBTOqxcCAggAgIICiJ4xXxTK
3CJQLOgjXWQR8SzKXRjGXCMrkTEkdKEM58c39c+h3p0CCITUbRl7oF6anHstJMXV08JzraDnsuAS
3KanOrlJT6UKBxHQix6y+NiAsBiEPmtSNbM+Xelt3rCl23LoUcyk7xkkKJzsn7bNMAt51Df0ykV2
V0XAJ/zzxbkJC6xySwm+h/ZC72YM0CzkFzzkEiIY09z7kBwEYuBpOFf17WVsaN+k6aW4TZZdQOvJ
+ovstfFMsqNxO57vLPAC9Brw1hwpCIoJ4B+RxXaNlUHsQV8GgjXZmpO1HbS4sXH8cZcaj5wDRudq
szoCnSbI7nDEobcnGotFv/1XRI8PHTSw3e+wIw0Js46G5rVP/pr2sLrUgiu0Pp20bh0dmJYhaM3p
DGSTSHm4sJ5bph+qunVr1FEd2kT78HttG+SibURIr+nryZWXfBwQWSXfK4Fctg8EYOxih1Kzaab2
xwycJklQuy7+MiorEad6eMhE5wZHgw0UrYd3PNXDBVzBnfQwC4EVYG7gcbwuiO5BwM4QFik0GM5k
5JrjtQr1yOgSHE+DJI1lUcmP3HUmAGSE+oh/TRiaqaL95+KBbsrbqe3q3qaAvzjVO4et5hzH4kB2
rGE0TwJC67hYJEkgW0PhCZtSovZrqPaCPfArcwp/lYa7NSq4mPLzYe0ccJ9pNdr0krH4qXkphy2G
4VrgRN7Q10xDWG0QpraNY1K7b8OT+jqIYqY06IZHkC/dgIcGev52uH05QWINXPdQ3QsZu2cd+guP
75k/T3SuXHjLcEfeM4DtiniMZVXthf7U3pHoOqmESypawfpDApAE5qndkz7yAJb17Z9nvm/7WLYI
ajsmZJsLhSMa9kjqXNiXCjCCAvkGCSqGSIb3DQEHAaCCAuoEggLmMIIC4jCCAt4GCyqGSIb3DQEM
CgECoIICpjCCAqIwHAYKKoZIhvcNAQwBAzAOBAiv/eSGH8t4FQICCAAEggKAgQL+/LDMM+9JaOII
YC2HHjFQs8ch+rFTiLDycya72IIQO3MjTIJ8pa/nf++H+X73u1aSVPsXcOZD5q5jII9fqTnzpZFh
ZdMyjeiOSdpsvSSPDHEOkUUBpAUsvQTKBuUeS7ORnO0qOjhG8huTNvqykEHjSv4lTuYNh8hyV0EO
cjg2pfBWWzEl0IGKy3MF15Vw2/2o0sIEVbeUws2IrFjz9aJC3FAtd7nryRk+F58XCp0RCxi1Si5J
IGH8fCDG+9/vpoTqRzVSlLxLv6r+fplBn4mcdR6cq4fHf+bu2eIbqoqXcwTyJsmIJ821ftesdW6J
RuSY62F4W6Q3bPHYU5XeIk10h0dgsVvhg4riPfujvTwnfRh1XwOowYL3xk+5yG2GEXEEhBXQuYLg
FK90EskeyZ2b0ezvFBOLHErz5SuciOgBD/Pr5q/WC8rVyzzes6qzpbMm87oPQqFvhcnrJMCOXVSj
vRXcRgEU5yxdh9T3ypX+/MPS1Chps+8PdXFBWba9pMzCc+ZjePgHlE+aGUFsqFpeBd5QRDBVb+Dv
vWlbzWvCeSCn0Lr3Bq6BLvvHlUOScoUJfq8Znn6lUpwqCQDayyTwto2itjANGD1qGDSy/hJmFAWD
pn4D7+gProuEWRk2KTNDIi490bRXKMckM1M2zpnwmZl1pHvYkNuVTcvHa42C5o0XQgrYlvX6ut+R
9vm0ABx97KqXPivaujkfuE56FcrzgmcJ1QSH1Dkz9pbsb6y9W8v0buFWICu90Cmrfw0+vTJXLn1q
1PmhVpKOPNQyHcdjYkZvfTLXXtqTN0UFZ4dWfJ0lDr37E2d+I1J1thvG+tWjok+M50Ud3ID7mjyP
nDElMCMGCSqGSIb3DQEJFTEWBBQqWz7PcWzeyndOTsePRCu2C/tnVjAxMCEwCQYFKw4DAhoFAAQU
lRWnDw0vHMN9GV8EuMUc5z/2d7IECMAuCHMOgFiwAgIIAA==
";
    }
}

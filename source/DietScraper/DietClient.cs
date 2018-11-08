namespace DietScraper
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    public class DietClient
    {
        private readonly Downloader downloader;

        public static Regex PlayerResultRegex = new Regex(
      "<a class=\"profile-img-wrap\" href=\"/player-profile/(?<id>\\d" +
      "+)\">.*?<h2 class=\"user-name\">(?<name>.*?)</h2>.*?Start We" +
      "ight:</span> (?<start_weight>.*?)</span>.*?Goal Weight:</spa" +
      "n> (?<goal_weight>.*?)</span>.*?Win Odds:</span> (?<odds>.*?" +
      ")</span>",
    RegexOptions.Singleline
    | RegexOptions.CultureInvariant
    | RegexOptions.Compiled
    );

        private static Regex GameInviteOnly = new Regex(@"<div id=""game-access-type"" class=""data-item inline-block"">\s+<span class=""value"">Invite-Only</span>");
        private static Regex GameNewRegex = new Regex(@"""status"":""new""");
        private static Regex GameInProgressRegex = new Regex(@"""status"":""started""");
        private static Regex GameEndedRegex = new Regex(@"<p class=""btn-descriptive-text"">The game has ended.</p>");
        private static Regex GameHiddenRegex = new Regex(@"<b>The organizer has chosen to hide <br />the activity in this game.</b>");
        private static Regex GameCancelledRegex = new Regex(@"<div class=""ct-container"">\s+This game has been canceled.\s+<div class=""clear""></div>\s+</div>");
        private static Regex GameNotFoundRegex = new Regex(@"<div class=""ct-container"">\s+Game not found\s+<div class=""clear""></div>\s+</div>");

        private static Regex GameBetRegex = new Regex(
            "<div id=\"game-bet-amount\" class=\"stats-item\">\\s+<div class=\"stats-image\"></div>\\s+<div class=\"stats-data\">\\s+<span class=\"value\">\\$(?<bet_amount>\\d+)</span>");

        public static Regex GamePotRegex = new Regex(
            "<div id=\"game-pot\" class=\"stats-item\">\\s+<div class=\"s" +
            "tats-image\"></div>\\s+<div class=\"stats-data\">\\s+<span c" +
            "lass=\"value\">\\$(?<pot>[\\d,]+)</span>",
            RegexOptions.CultureInvariant | RegexOptions.Compiled );

        public static Regex GameTitleRegex = new Regex(
            @"<meta property=""og:title"" content=""(?<name>.*?)""/>"
            );
        public static Regex GameStartRegex = new Regex(
            @"<meta property=""dietbet:start_date"" content=""(?<start_date>.*?)""/>"
            );
        public static Regex GameEndRegex = new Regex(
            @"<meta property=""dietbet:end_date"" content=""(?<end_date>.*?)""/>"
            );

        public static Regex GameTotalPlayersRegex = new Regex(
            @"<div id=""game-total-players"" class=""stats-item"">\s+<div class=""stats-image""></div>\s+<div class=""stats-data"">\s+<span class=""value"">(?<total_players>[\d,]+)</span>"
            );

        public DietClient(Downloader downloader)
        {
            if (downloader == null)
            {
                throw new ArgumentNullException("downloader");
            }

            this.downloader = downloader;
        }

        public void GetPublicGames()
        {

        }

        public List<GamePlayer> GetPlayers(int gameId, int page)
        {
            string url = string.Format("http://www.dietbetter.com/api/GetPlayers.php?page={0}&gameId={1}&renderHtml=1", page, gameId);

            string content = this.downloader.DownloadString(url);

            List<GamePlayer> players = new List<GamePlayer>();

            foreach (Match match in PlayerResultRegex.Matches(content))
            {
                GamePlayer player = new GamePlayer();

                // We don't use name, oh well.
                string name = match.Groups["name"].Value;
                string startWeight = match.Groups["start_weight"].Value.Replace(" lbs", "");
                string goalWeight = match.Groups["goal_weight"].Value.Replace(" lbs", "");
                string playerId = match.Groups["id"].Value;
                string odds = match.Groups["odds"].Value.Replace("%", "");
                
                if (odds != "N/A")
                {
                    player.Odds = double.Parse(odds);
                }

                if (startWeight != "xxx" && startWeight != "N/A")
                {
                    player.StartWeight = double.Parse(startWeight);
                }

                if (goalWeight != "xxx" && startWeight != "N/A")
                {
                    player.GoalWeight = double.Parse(goalWeight);
                }

                player.PlayerId = int.Parse(playerId);

                players.Add(player);
            }

            return players;
        }

        public Game GetGame(int id)
        {
            string url = string.Format("http://www.dietbetter.com/games/{0}", id);

            string content = this.downloader.DownloadString(url);

            Game game = new Game();

            if (GameNotFoundRegex.IsMatch(content))
            {
                return null;
            }

            if (GameCancelledRegex.IsMatch(content))
            {
                game.Status = GameStatus.Cancelled;
                return game;
            }

            if (GameHiddenRegex.IsMatch(content))
            {
                game.Status = GameStatus.Hidden;
                return game;
            }

            if (GameInviteOnly.IsMatch(content))
            {
                game.Status = GameStatus.InviteOnly;
                return game;
            }

            if (GameEndedRegex.IsMatch(content))
            {
                game.Status = GameStatus.Ended;
            }
            else if (GameInProgressRegex.IsMatch(content))
            {
                game.Status = GameStatus.Started;
            }
            else if (GameNewRegex.IsMatch(content))
            {
                game.Status = GameStatus.New;
            }

            string name = GameTitleRegex.Match(content).Groups["name"].Value;
            string bet = GameBetRegex.Match(content).Groups["bet_amount"].Value;
            string pot = GamePotRegex.Match(content).Groups["pot"].Value;
            string startDate = GameStartRegex.Match(content).Groups["start_date"].Value;
            string endDate = GameEndRegex.Match(content).Groups["end_date"].Value;
            string players = GameTotalPlayersRegex.Match(content).Groups["total_players"].Value;

            game.Bet = double.Parse(bet);
            game.Pot = double.Parse(pot, NumberStyles.AllowThousands);
            game.StartDate = DateTime.Parse(startDate);
            game.EndDate = DateTime.Parse(endDate);
            game.Players = int.Parse(players, NumberStyles.AllowThousands);
            game.Name = name;
            game.Id = id;

            return game;
        }
    }
}

namespace ConsoleUI
{
    using DietScraper;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Serialization;

    public class Program
    {
        public const string UrlFileName = "urls.xml";

        public const string BetAmount = "<div id=\"game-bet-amount\" class=\"stats-item\">\\s+<div class=\"stats-image\"></div>\\s+<div class=\"stats-data\">\\s+<span class=\"value\">\\$(?<bet_amound>\\d+)</span>";

        public static void Main(string[] args)
        {
            string path = Path.Combine(Environment.CurrentDirectory, "Cache");

            Downloader downloader = new Downloader(path);
            DietClient client = new DietClient(downloader);

            List<Game> games = new List<Game>();

            ManualResetEvent man = new ManualResetEvent(false);
            int threads = 27100;
            for (int id = 27100; id >= 1; id--)
            {                                
                ThreadPool.QueueUserWorkItem(o =>                
                    {
                        int i = (int)o;

                        Console.WriteLine("Downloading game {0}...", i);
                        Game game = client.GetGame(i);

                        if (game != null)
                        {
                            if (game.Status == GameStatus.Ended || game.Status == GameStatus.Started || game.Status == GameStatus.New)
                            {
                                Console.WriteLine("Name: {0}", game.Name);
                                Console.WriteLine("Bet: {0}", game.Bet);
                                Console.WriteLine("Pot: {0}", game.Pot);
                                Console.WriteLine("Players: {0}", game.Players);
                                Console.WriteLine("Start Date: {0}", game.StartDate);
                                Console.WriteLine("End Date: {0}", game.EndDate);
                                Console.WriteLine("Status: {0}", game.Status);
                            }
                            else if (game.Status == GameStatus.Cancelled)
                            {
                                Console.WriteLine("Game was cancelled.");
                            }
                            else if (game.Status == GameStatus.Hidden)
                            {
                                Console.WriteLine("Game is hidden.");
                            }
                            else if (game.Status == GameStatus.InviteOnly)
                            {
                                Console.WriteLine("Game is invite only.");
                            }
                            else
                            {
                                Console.WriteLine("Game status is unknown.");
                            }

                            lock (games)
                            {
                                games.Add(game);
                            }
                        }
                        else
                        {
                            Console.WriteLine("The game was not found.");
                        }

                        if (Interlocked.Decrement(ref threads) == 0)
                        {
                            man.Set();
                        }
                    }, id);
            }

            man.WaitOne();



            using (StreamWriter writer = new StreamWriter("outcome.csv"))
            {
                writer.WriteLine("Id,Bet,Pot,Total,Won,Lost,Percent,Payout,Margin");
                var validGames = games.Where(p => p.Status == GameStatus.Ended && p.Pot > 0);
                foreach (Game game in validGames)
                {
                    int page = 0;

                    if (game.Players == 0)
                    {
                        Console.WriteLine("The game has not players.");
                        continue;
                    }

                    List<GamePlayer> allPlayers = new List<GamePlayer>();

                    List<GamePlayer> players;
                    do
                    {
                        players = client.GetPlayers(game.Id, page);

                        foreach (var player in players)
                        {
                            if (!allPlayers.Any(p => p.PlayerId == player.PlayerId))
                            {
                                allPlayers.Add(player);
                            }
                        }

                        page++;
                    } while (players.Count > 0);

                    if (allPlayers.Count != game.Players)
                    {
                        Console.WriteLine("The number of players is {0}, but {1} were found.", game.Players, allPlayers.Count);
                        continue;
                    }
                    
                    int won = allPlayers.Count(p => p.Odds == 100);
                    int lost = game.Players - won;
                    double percent = won / (double)game.Players;
                    double payout = game.Pot;                     

                    if (won > 0)
                    {
                        payout = game.Pot / (double)won;
                    }

                    double margin = (payout - game.Bet) / game.Bet;
                    

                    Console.WriteLine("Game {0} Bet: {5:C} Winners: {1}/{2} ({3:P1}) Payout: {4:C} ({6:P0})", game.Id, won, allPlayers.Count, percent, payout, game.Bet, margin);
                    writer.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7},{8}",
                        game.Id, game.Bet, game.Pot, game.Players, won, lost, percent, payout, margin);
                }
            }
        }

        public static void GetGames()
        {
            //string url = "http://www.dietbetter.com/games/";
            //string content = downloader.DownloadString(url);
            //var matches = Regex.Matches(content, "<a  class=\"game-title.*?\" title=\"(?<title>.*?)\" href=\"http://www\\.dietbetter\\.com/games/(?<id>\\d+)\">.*?</a>");
        }

        public static List<string> LoadIds(string path)
        {
            List<string> urls;
            string fileName = Path.Combine(path, UrlFileName);

            if (File.Exists(fileName))
            {
                using (FileStream stream = File.OpenRead(fileName))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<string>));
                    urls = (List<string>)serializer.Deserialize(stream);
                }
            }
            else
            {
                urls =  new List<string>();
            }

            return urls;
        }

        public static void SaveIds(string path, List<string> urls)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<string>));

            string fileName = Path.Combine(path, UrlFileName);
            FileStream stream = File.OpenWrite(fileName);

            serializer.Serialize(stream, urls);
        }
    }
}

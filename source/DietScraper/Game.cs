using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DietScraper
{
    public class Game
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public double Bet { get; set; }
        public int Players { get; set;}
        public double Pot { get; set; }
        public GameStatus Status { get; set; }
    }

    public enum GameStatus
    {
        Unknown,
        New,
        Started,
        Ended,
        Cancelled,
        Hidden,
        InviteOnly,
    }
}

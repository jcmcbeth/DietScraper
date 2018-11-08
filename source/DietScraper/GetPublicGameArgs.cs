namespace DietScraper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class GetPublicGameArgs
    {
        public int From { get; set; }
        public int To { get; set; }
    }

    public enum GameFilter
    {
        StartingSoon,
        JustStarting,
        InTheFuture,
        RecentlyEnded
    }
}

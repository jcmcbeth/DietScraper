namespace DietScraper
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.Serialization;

    public class Downloader
    {
        private const string ManifestFileName = "Cache.xml";
        private readonly string cacheDirectory;
        private Dictionary<string, CachedPage> pages;
        private bool loaded = false;
        private int topId = 0;        

        public Downloader(string cacheDirectory)
        {
            this.cacheDirectory = cacheDirectory;
            this.pages = new Dictionary<string, CachedPage>();
            this.loaded = false;
        }

        public string DownloadString(string url)
        {
            this.LazyLoad();

            string content = this.GetCachedPage(url);

            if (content == null)
            {
                using (WebClient client = new WebClient())
                {
                    content = client.DownloadString(url);
                }

                CachedPage page = new CachedPage();
                page.Id = GetNextId();
                page.FileName = string.Format("{0}.htm", page.Id);
                page.Status = CacheStatus.Cached;
                page.Url = url;
                page.Date = DateTime.Now;

                lock (this.pages)
                {
                    pages.Add(url, page);
                }

                this.CachePage(page, content);
                this.SaveCache();
            }            

            return content;
        }

        /// <summary>
        /// Gets the next valid ID to use for file names.
        /// </summary>
        /// <returns></returns>
        private int GetNextId()
        {
            lock (pages)
            {
                return ++topId;
            }
        }        

        /// <summary>
        /// Loads the cache from the cache manifest file.
        /// </summary>
        private void LoadCache()
        {            
            string manifestFile = Path.Combine(this.cacheDirectory, Downloader.ManifestFileName);

            if (File.Exists(manifestFile))
            {
                List<CachedPage> pageList;
                using (Stream stream = File.OpenRead(manifestFile))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<CachedPage>));
                    pageList = (List<CachedPage>)serializer.Deserialize(stream);
                }

                foreach (CachedPage page in pageList)
                {
                    this.pages.Add(page.Url, page);
                }

                this.topId = pageList.Max(p => p.Id);
            }

            this.loaded = true;
        }

        /// <summary>
        /// Persist the cache manifest to file.
        /// </summary>
        private void SaveCache()
        {
            string manifestFile = Path.Combine(this.cacheDirectory, Downloader.ManifestFileName);

            lock (this.pages)
            {

                if (File.Exists(manifestFile))
                {
                    bool valid = false;
                    try
                    {
                        //new XmlDocument().LoadXml(manifestFile);
                        XDocument.Load(manifestFile);
                        valid = true;
                    }
                    catch (XmlException)
                    {

                    }

                    if (valid)
                    {
                        File.Copy(manifestFile, manifestFile + ".bak", true);
                    }
                }

                if (!Directory.Exists(this.cacheDirectory))
                {
                    Directory.CreateDirectory(this.cacheDirectory);
                }

                using (Stream stream = File.OpenWrite(manifestFile))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<CachedPage>));
                    serializer.Serialize(stream, this.pages.Values.ToList());
                }
            }
        }

        /// <summary>
        /// Checks if the manifest has been loaded and loads it if neccesary.
        /// </summary>
        private void LazyLoad()
        {
            lock (this.pages)
            {
                if (!this.loaded)
                {
                    this.LoadCache();
                }
            }
        }

        private string GetCachedPage(string url)
        {
            lock (this.pages)
            {
                if (this.pages.ContainsKey(url))
                {
                    CachedPage page = this.pages[url];

                    string cachedFileName = Path.Combine(this.cacheDirectory, page.FileName);

                    return File.ReadAllText(cachedFileName);
                }
            }

            return null;
        }

        private void CachePage(CachedPage page, string content)
        {
            if (!Directory.Exists(this.cacheDirectory))
            {
                Directory.CreateDirectory(this.cacheDirectory);
            }

            string cachedFileName = Path.Combine(this.cacheDirectory, page.FileName);
            File.WriteAllText(cachedFileName, content);
        }
    }

    public class CachedPage
    {
        /// <summary>
        /// Gets and sets the unique identifier.
        /// </summary>
        public int Id
        {
            get;
            set;
        }
        /// <summary>
        /// Gets and sets the URL used to download the cached page.
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// Gets and sets the date the page was last cached.
        /// </summary>
        public DateTime Date { get; set; }
        /// <summary>
        /// Gets and sets the file name of the file where the page is cached.
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// Gets and sets the current cache status of a page.
        /// </summary>
        public CacheStatus Status { get; set; }
    }

    /// <summary>
    /// Status of a cached page.
    /// </summary>
    public enum CacheStatus
    {
        NotCached,
        Cached,
        Downloading,
    }
}

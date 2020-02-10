using Nito.AsyncEx;
using System;
using HtmlAgilityPack;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;

namespace pokemon
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Quantas Paginas deseja armazenar?");
            var num = Console.ReadLine(); 
            int uniq = 0;
            while (uniq < 1) {
                Console.WriteLine("Arquivo unico (1-SIM 2-NAO)");
                uniq = Convert.ToInt32(Console.ReadLine());
                if (uniq > 2)
                {
                    Console.WriteLine("Opção inválida");
                    uniq = 0;
                } 
            }
            Links urls = GetHtmlCards(Convert.ToInt32(num));
            GetHtmlCardsData(urls, Convert.ToInt32(num), uniq);
        }
        public static Links GetHtmlCards(int num)
        { 

            var webClient = new System.Net.WebClient();
            List<string> links = new List<string>();
            Dictionary<string, string> images = new Dictionary<string, string>();
            for (int i = 1; i <= num; i++)
            {
                string pagina = webClient.DownloadString("https://www.pokemon.com/us/pokemon-tcg/pokemon-cards/" + i + "?cardName=&cardText=&evolvesFrom=&simpleSubmit=&format=unlimited&hitPointsMin=0&hitPointsMax=300&retreatCostMin=0&retreatCostMax=5");
                HtmlDocument htmlDocument = new HtmlAgilityPack.HtmlDocument();
                htmlDocument.LoadHtml(pagina);
                HtmlNodeCollection linodes = htmlDocument.DocumentNode.SelectNodes("//ul[@class='cards-grid clear']/li");
                Parallel.ForEach(linodes,
                    node=>
                    {
                        var nodea = node.Element("a");
                        var img = nodea.ChildNodes[1].ChildNodes[1];
                        var t = nodea.Attributes["href"].Value;
                        var linktxt = Base64Encode(img.Attributes["src"].Value);
                        if (!images.ContainsKey(t))
                        {
                            images[t] = linktxt;
                        }
                        links.Add(t);
                    });
            }

            return new Links
            {
                links = links,
                images = images
            };
        }


        public static void GetHtmlCardsData(Links links, int numpaginas, int uniq)
        {
            List<Card> cardsList = new List<Card>();
            List<List<string>> cardsListmulti = new List<List<string>>();
            Parallel.ForEach(links.links,
                link => {
                    var webClient = new System.Net.WebClient();
                    string pagina = webClient.DownloadString("https://www.pokemon.com" + link);
                    HtmlDocument htmlDocument = new HtmlAgilityPack.HtmlDocument();
                    htmlDocument.LoadHtml(pagina);
                    Parallel.ForEach(htmlDocument.DocumentNode.SelectNodes("//div[@class='color-block color-block-gray']"),
                    node =>
                    {
                        var t = node.ChildNodes["h1"];
                        if (t != null && link != null)
                        { 
                                Card c = new Card();
                                c.Nome = t.InnerText;
                                t = htmlDocument.DocumentNode.SelectSingleNode("//div[@class='stats-footer']/span");
                                c.Numerãção = t.InnerText;
                                t = htmlDocument.DocumentNode.SelectSingleNode("//div[@class='stats-footer']/h3");
                                c.Expansao = t.InnerText;
                                c.Url = "https://www.pokemon.com" + link;
                                c.Image = links.images.FirstOrDefault(x=>x.Key == link).Value;
                                cardsList.Add(c); 
                        }
                    });
                });
            if (uniq == 2)
            {
                for (var i = 0; i < numpaginas; i++)
                {
                    var c = 1;
                    string path = @"C:\temp\cards" + i + ".json";
                    FileInfo fi = new FileInfo(path);
                    string json = string.Empty;
                    foreach (var card in cardsList)
                    {
                        if (c <= 12)
                        {
                            json += JsonConvert.SerializeObject(card) + ",";
                            c++;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (!fi.Exists && !string.IsNullOrEmpty(json))
                    {
                        using (StreamWriter file = fi.CreateText())
                        {
                            json = json.Remove(json.Length - 1);
                            file.WriteLine("[" + json + "]");
                            file.WriteLine();
                            file.Close();
                        }
                    }
                }
            }
            else
            {
                string path = @"C:\temp\cards.json";
                FileInfo fi = new FileInfo(path);
                string json = string.Empty;
                foreach (var card in cardsList)
                {
                    json += JsonConvert.SerializeObject(card) + ",";
                }

                if (!fi.Exists && !string.IsNullOrEmpty(json))
                {
                    using (StreamWriter file = fi.CreateText())
                    {
                        json = json.Remove(json.Length - 1);
                        file.WriteLine("[" + json + "]");
                        file.WriteLine();
                        file.Close();
                    }
                }

            }

        }

        public class Card
        {
            public string Nome;
            public string Numerãção;
            public string Expansao;
            public string Url;
            public string Image;

            public Card() { }



        }
        public class Links
        {
            public List<string> links;
            public IDictionary<string,string> images; 

            public Links() { }



        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}

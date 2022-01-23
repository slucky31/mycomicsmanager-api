using HtmlAgilityPack;
using System;
using MyComicsManagerApi.Utils;
using Serilog;

namespace MyComicsManagerApi.DataParser
{
    public class HtmlDataParser
    {
        private static ILogger Log => Serilog.Log.ForContext<HtmlDataParser>();
        
        private HtmlWeb Web { get; set; }

        protected HtmlDocument Doc { get; set; }

        public HtmlDataParser()
        {
            Web = new HtmlWeb();
        }

        public void LoadDocument(string url)
        {
            Log.Here().Information("Load information from {Url}", url.Replace(Environment.NewLine, ""));
            Doc = Web.Load(url);
        }

        public HtmlNode ExtractSingleNode(string htmlPath)
        {
            return Doc.DocumentNode.SelectSingleNode(htmlPath);
        }

        public string ExtractTextValue(string htmlPath)
        {
            var selectedNode = Doc.DocumentNode.SelectSingleNode(htmlPath);
            if (selectedNode != null)
            {
                return selectedNode.InnerText.Trim();
            }
            return null;
        }

        public string ExtractTextValueAndSplitOnSeparator(string htmlPath, string separator, int id)
        {
            var extractedText = ExtractTextValue(htmlPath);
            string splitExtractedText = extractedText;
            if (!String.IsNullOrEmpty(extractedText) && extractedText.Contains(separator))
            {
                splitExtractedText = extractedText.Split(separator)[id].Trim();
            }
            return splitExtractedText;
        }

        public string ExtractAttributValue(string htmlPath, string attribut)
        {
            var selectedNode = Doc.DocumentNode.SelectSingleNode(htmlPath);
            if (selectedNode != null)
            {
                return selectedNode.Attributes[attribut].Value.Trim();
            }
            return null;
        }
    }
}
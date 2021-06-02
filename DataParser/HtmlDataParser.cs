using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using Serilog;
using HtmlAgilityPack;


namespace MyComicsManagerApi.DataParser
{
    public class HtmlDataParser
    {
        private HtmlWeb Web { get; set; }

        private HtmlDocument Doc { get; set; }

        public HtmlDataParser()
        {
            Web = new HtmlWeb();
        }

        public void LoadDocument(string url)
        {
            Doc = Web.Load(url);
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

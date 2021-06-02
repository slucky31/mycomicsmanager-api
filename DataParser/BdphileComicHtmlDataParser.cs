using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MyComicsManagerApi.DataParser
{
    public class BdphileComicHtmlDataParser : ComicHtmlDataParser
    {
        private const string BDPHILE_URL = "https://www.bdphile.info/search/album/?q=";

        private string FicheURL {get; set;}

        private bool IsOneShot { get; set; } = false;

        protected override string ExtractDateParution()
        {
            // TODO : A convertir dans un format exploitable
            return ExtractTextValue("/html/body/div[1]/section[1]/div/div[2]/div/div/dl/dd[4]");
        }

        protected override string ExtractDessinateur()
        {
            return ExtractTextValue("/html/body/div[1]/section[1]/div/div[2]/div/div/dl/dd[2]/a");            
        }

        protected override string ExtractEditeur()
        {
            return ExtractTextValue("/html/body/div[1]/section[1]/div/div[2]/div/div/dl/dd[3]/a");
        }

        protected override string ExtractISBN()
        {
            return ExtractTextValue("/html/body/div[1]/section[1]/div/div[2]/div/div/dl/dd[8]");
        }

        protected override string ExtractNote()
        {
            return ExtractTextValueAndSplitOnSeparator("/html/body/div[1]/section[1]/div/div[3]/div/div[1]","/",0);            
        }

        protected override string ExtractOneShot()
        {
            return IsOneShot.ToString();
        }

        protected override string ExtractScenariste()
        {
            return ExtractTextValue("/html/body/div[1]/section[1]/div/div[2]/div/div/dl/dd[1]/a");
        }

        protected override string ExtractSerie()
        {
            if (IsOneShot)
            {
                return "One shot";
            }
            else
            {
                return ExtractTextValue("/html/body/div[1]/section[1]/div/section/h1/a");
            }            
        }

        protected override string ExtractSerieStatus()
        {
            // TODO : Statut de la série
            return "";
        }

        protected override string ExtractSerieUrl()
        {
            if (IsOneShot)
            {
                return FicheURL;
            }
            else
            {
                return ExtractAttributValue("/html/body/div[1]/section[1]/div/section/h1/a", "href");
            }
        }

        protected override string ExtractTitre()
        {
            if (IsOneShot)
            {
                return ExtractTextValue("/html/body/div[1]/section[1]/div/section/h1/a");
            }
            else
            {
                return ExtractTextValueAndSplitOnSeparator("/html/body/div[1]/section[1]/div/section/h2",":",1);
            }
        }

        protected override string ExtractTome()
        {
            if (IsOneShot)
            {
                return "1";
            }
            else
            {
                var tome = ExtractTextValueAndSplitOnSeparator("/html/body/div[1]/section[1]/div/section/h2", ":", 0);
                
                // Suppression de tous les caractères sauf les chiffres de 0 à 9
                Regex regexObj = new Regex(@"[^\d]");
                return regexObj.Replace(tome, "");
            }
        }

        protected override string ExtractURL()
        {
            return FicheURL;
        }

        protected override void Search(string isbn)
        {
            // Recherche sur BDPhile
            // https://www.bdphile.info/search/album/?q=9782365772013
            LoadDocument(BDPHILE_URL + isbn);

            // Récupération de l'URL de la fiche du comic
            FicheURL = ExtractAttributValue("/html/body/div[1]/section[2]/div/div[2]/a[1]", "href");

            // Récupération de la page liée à l'ISBN recherché
            LoadDocument(FicheURL);

            IsOneShot = "(one-shot)".Equals(ExtractTextValue("/html/body/div[1]/section[1]/div/section/h1/span[1]"));

        }
    }
}

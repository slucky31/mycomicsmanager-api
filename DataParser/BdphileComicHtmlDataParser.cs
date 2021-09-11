using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MyComicsManagerApi.DataParser
{
    public class BdphileComicHtmlDataParser : ComicHtmlDataParser
    {
        private const string BDPHILE_URL = "https://www.bdphile.info/search/album/?q=";

        private string FicheURL { get; set; }

        private bool IsOneShot { get; set; }

        private Dictionary<string, string> ExtractedInfo { get; set; }

        public BdphileComicHtmlDataParser()
        {
            ExtractedInfo = new Dictionary<string, string>();
            IsOneShot = false;
        }

        protected override string ExtractColoriste()
        {
            return ExtractedInfo.GetValueOrDefault("Couleurs", "").Trim();
        }

        protected override string ExtractDateParution()
        {
            // TODO : A convertir dans un format exploitable
            return ExtractedInfo.GetValueOrDefault("Date de publication", "");
        }

        protected override string ExtractDessinateur()
        {
            return ExtractedInfo.GetValueOrDefault("Dessin", "").Trim();
        }

        protected override string ExtractEditeur()
        {
            return ExtractedInfo.GetValueOrDefault("Éditeur", "").Trim();
        }

        protected override string ExtractISBN()
        {
            return ExtractedInfo.GetValueOrDefault("EAN", "").Trim();
        }

        protected override string ExtractNote()
        {
            return ExtractTextValueAndSplitOnSeparator("/html/body/div[1]/section[1]/div/div[3]/div/div[1]", "/", 0);
        }

        protected override string ExtractOneShot()
        {
            return IsOneShot.ToString();
        }

        protected override string ExtractScenariste()
        {
            return ExtractedInfo.GetValueOrDefault("Scénario", "").Trim();
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
                return ExtractTextValue("/html/body/div[1]/section[1]/div/section/h1/text()");
            }
            else
            {
                return ExtractTextValueAndSplitOnSeparator("/html/body/div[1]/section[1]/div/section/h2", ":", 1);
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

        protected void extractDataTable()
        {
            ExtractedInfo.Clear();

            var selectedNode = ExtractSingleNode("/html/body/div[1]/section[1]/div/div[2]/div/div/dl");

            // Recherche de toutes les balises <dt>
            // Pour sélectionner dans le noeud courant ; uiliser .// sinon avec // on repart au début du document
            // https://stackoverflow.com/questions/10583926/html-agility-pack-selectnodes-from-a-node
            var dtNodes = selectedNode.SelectNodes(".//dt");

            // Recherche de toutes les balises <dd>
            // Pour sélectionner dans le noeud courant ; uiliser .// sinon avec // on repart au début du document
            // https://stackoverflow.com/questions/10583926/html-agility-pack-selectnodes-from-a-node
            var ddNodes = selectedNode.SelectNodes(".//dd");

            // On stocke le tout dans un dictionnaire
            for (int i = 0; i < dtNodes.Count; i++)
            {
                ExtractedInfo.Add(dtNodes[i].InnerText, ddNodes[i].InnerText);
            }
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

            // Récupération du tableau contenant les informations (les éléments sans valeurs ne sont pas affichés)
            extractDataTable();
        }
    }
}
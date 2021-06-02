using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyComicsManagerApi.ComicDataParser
{
    public enum ComicDataEnum
    {
        TITRE,
        SERIE,
        SERIE_URL,
        SCENRAISTE,
        DESSINATEUR,
        TOME,
        DATEPARUTION,
        ISBN,
        URL,
        EDITEUR,
        NOTE,
        FILE,
        ONESHOT,
        VIGNETTE
    }
    
    public abstract class ComicDataParser : HtmlDataParser
    {
  
        private Dictionary<ComicDataEnum, string> ExtractedData { get; }

        public ComicDataParser()
        {
            ExtractedData = new Dictionary<ComicDataEnum, string>();
        }

        public Dictionary<ComicDataEnum, string> Parse(string isbn)
        {
            Search(isbn);
            // TODO Exception ISBN Not Found !

            ExtractedData.Clear();

            ExtractedData.Add(ComicDataEnum.TITRE, ExtractTitre());
            ExtractedData.Add(ComicDataEnum.SERIE, ExtractSerie());
            ExtractedData.Add(ComicDataEnum.SERIE_URL, ExtractSerieUrl());
            ExtractedData.Add(ComicDataEnum.SCENRAISTE, ExtractScenariste());
            ExtractedData.Add(ComicDataEnum.DESSINATEUR, ExtractDessinateur());
            ExtractedData.Add(ComicDataEnum.TOME, ExtractTome());
            ExtractedData.Add(ComicDataEnum.DATEPARUTION, ExtractDateParution());
            ExtractedData.Add(ComicDataEnum.ISBN, ExtractISBN());
            ExtractedData.Add(ComicDataEnum.URL, ExtractURL());
            ExtractedData.Add(ComicDataEnum.EDITEUR, ExtractEditeur());
            ExtractedData.Add(ComicDataEnum.NOTE, ExtractNote());
            ExtractedData.Add(ComicDataEnum.ONESHOT, ExtractOneShot());

            return ExtractedData;
        }

        protected abstract string ExtractOneShot();

        protected abstract void Search(string isbn);

        protected abstract string ExtractTitre();

        protected abstract string ExtractSerie();

        protected abstract string ExtractSerieUrl();

        protected abstract string ExtractScenariste();

        protected abstract string ExtractDessinateur();

        protected abstract string ExtractTome();

        protected abstract string ExtractDateParution();

        protected abstract string ExtractISBN();

        protected abstract string ExtractURL();

        protected abstract string ExtractEditeur();

        protected abstract string ExtractNote();

        protected abstract string ExtractSerieStatus();
    }
}

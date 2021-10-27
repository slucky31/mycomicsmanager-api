using MyComicsManagerApi.Models;
using MongoDB.Driver;
using System.Collections.Generic;
using Serilog;
using System.IO;
using System;
using System.Globalization;
using MyComicsManagerApi.DataParser;
using MyComicsManagerApi.Utils;

namespace MyComicsManagerApi.Services
{
    public class ComicService
    {
        private readonly IMongoCollection<Comic> _comics;
        private readonly LibraryService _libraryService;
        private readonly ComicFileService _comicFileService;

        public ComicService(IDatabaseSettings settings, LibraryService libraryService, ComicFileService comicFileService)
        {
            Log.Debug("settings = {Settings}", settings);
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);
            _comics = database.GetCollection<Comic>(settings.ComicsCollectionName);
            _libraryService = libraryService;
            _comicFileService = comicFileService;
        }

        public List<Comic> Get() =>
            _comics.Find(comic => true).ToList();

        public Comic Get(string id) =>
            _comics.Find<Comic>(comic => comic.Id == id).FirstOrDefault();

        public Comic Create(Comic comic)
        {
            // TODO : Faire une vérification sur les champs qui vont être utilisés plus tard : EbookName, EbookPath
            // TODO : EbookPath ne devrait jamais être null,  car un comic ne peut exister sans fichier !
            // TODO : Vérifier comment est créer un comic la première fois pour s'assurer que le EbookPath n'est pas null
            
            // Conversion du fichier en CBZ et mise à jour du path car le nom du fichier peut avoir changer
            _comicFileService.ConvertComicFileToCbz(comic, comic.EbookPath);
            comic.EbookPath = Path.GetDirectoryName(comic.EbookPath) + Path.DirectorySeparatorChar + comic.EbookName;
            
            // Déplacement du fichier vers la racine de la librairie sélectionnée
            var destination = _libraryService.GetLibraryPath(comic.LibraryId, LibraryService.PathType.ABSOLUTE_PATH) +
                              comic.EbookName;
            try
            {
                
                // Gestion du cas où le fichier uploadé existe déjà dans la lib
                while (File.Exists(destination))
                {
                    Log.Warning("Le fichier {File} existe déjà", destination);
                    comic.Title = Path.GetFileNameWithoutExtension(destination) + "-Rename";
                    comic.EbookName = comic.Title + Path.GetExtension(destination);
                    Log.Warning("Il va être renommé en {FileName}", comic.EbookName);
                    destination = _libraryService.GetLibraryPath(comic.LibraryId, LibraryService.PathType.ABSOLUTE_PATH) +
                                  comic.EbookName;
                }

                File.Move(comic.EbookPath, destination);
            }
            catch (Exception e)
            {                
                Log.Error(e,"Erreur lors du déplacement du fichier");
                Log.Error("Origin = {Origin}", comic.EbookPath);
                Log.Error("Destination = {Destination}", destination);
                return null;
            }
            comic.EbookPath = comic.EbookName;
            
            // Récupération des données du fichier ComicInfo.xml si il existe
            if (_comicFileService.HasComicInfoInComicFile(comic))
            {
                comic = _comicFileService.ExtractDataFromComicInfo(comic);
                UpdateDirectoryAndFileName(comic);
            }

            // Calcul du nombre d'images dans le fichier CBZ
            _comicFileService.SetNumberOfImagesInCbz(comic);
            
            // Insertion en base de données
            _comics.InsertOne(comic);
 
            // Extraction de l'image de couverture après enregristrement car nommé avec l'id du comic       
            _comicFileService.SetAndExtractCoverImage(comic);
            Update(comic.Id, comic);
            
            // Création du fichier ComicInfo.xml
            _comicFileService.AddComicInfoInComicFile(comic);

            return comic;
        }

        public void Update(string id, Comic comic)
        {
            UpdateDirectoryAndFileName(comic);

            // Mise à jour en base de données
            _comics.ReplaceOne(c => c.Id == id, comic);
            
            // Mise à jour du fichier ComicInfo.xml
            _comicFileService.AddComicInfoInComicFile(comic);
        }

        private void UpdateDirectoryAndFileName(Comic comic)
        {
            // Mise à jour du nom du fichier
            if (!string.IsNullOrEmpty(comic.Serie) && comic.Volume > 0)
            {
                // Calcul de l'origine
                var origin = _comicFileService.GetComicEbookPath(comic, LibraryService.PathType.ABSOLUTE_PATH);

                // Mise à jour du nom du fichier pour le calcul de la destination
                comic.EbookName = comic.Serie.ToPascalCase() + "_T" + comic.Volume.ToString("000") + ".cbz";
                var libraryPath = _libraryService.GetLibraryPath(comic.LibraryId, LibraryService.PathType.ABSOLUTE_PATH);
                var comicEbookPath = Path.GetDirectoryName(comic.EbookPath) + Path.DirectorySeparatorChar + comic.EbookName;

                // Renommage du fichier
                File.Move(origin, libraryPath + comicEbookPath);

                // Mise à jour du chemin relatif avec le nouveau nom du fichier 
                comic.EbookPath = comicEbookPath;
            }

            // Mise à jour de l'arborescence du fichier
            if (!string.IsNullOrEmpty(comic.Serie))
            {
                var origin = _comicFileService.GetComicEbookPath(comic, LibraryService.PathType.ABSOLUTE_PATH);
                var libraryPath = _libraryService.GetLibraryPath(comic.LibraryId, LibraryService.PathType.ABSOLUTE_PATH);
                var eBookPath = comic.Serie.ToPascalCase() + Path.DirectorySeparatorChar;

                // Création du répertoire de destination
                Directory.CreateDirectory(libraryPath + eBookPath);

                // Déplacement du fichier
                File.Move(origin, libraryPath + eBookPath + comic.EbookName);
                comic.EbookPath = eBookPath + comic.EbookName;
            }
        }

        public void Remove(Comic comic)
        {
            // Suppression du fichier
            Comic c = _comics.Find<Comic>(c => (c.Id == comic.Id)).FirstOrDefault();
            if (c != null) {    
                
                // Suppression du fichier
                if (File.Exists(_comicFileService.GetComicEbookPath(comic, LibraryService.PathType.ABSOLUTE_PATH))) {
                    File.Delete(_comicFileService.GetComicEbookPath(comic, LibraryService.PathType.ABSOLUTE_PATH));
                }

                // Suppression de l'image de couverture
                if (File.Exists(c.CoverPath))
                {
                    File.Delete(c.CoverPath);
                }
                //TODO : Gestion des exceptions
            }

            // Suppression de la référence en base de données
            _comics.DeleteOne(c => c.Id == comic.Id);
        }

        public void RemoveAllComicsFromLibrary(string libId)
        {            
            List<Comic> comics = _comics.Find<Comic>(c => (c.LibraryId == libId)).ToList();
            foreach(Comic c in comics) {
                Remove(c);
            }
        }

        public void SearchComicInfoAndUpdate(Comic comic)
        {
            if (!String.IsNullOrEmpty(comic.ISBN))
            {
                var parser = new BdphileComicHtmlDataParser();
                var results = parser.Parse(comic.ISBN);

                if (results.Count > 0)
                {

                    // TODO : si la clé n'existe pas, on a un plantage ! Il faudrait gérer cela plus proprement !
                    
                    comic.Editor = results[ComicDataEnum.EDITEUR];
                    comic.ISBN = results[ComicDataEnum.ISBN];
                    comic.Penciller = results[ComicDataEnum.DESSINATEUR];
                    comic.Serie = results[ComicDataEnum.SERIE];
                    comic.Title = results[ComicDataEnum.TITRE];
                    comic.Writer = results[ComicDataEnum.SCENARISTE];
                    comic.FicheUrl = results[ComicDataEnum.URL];
                    comic.Colorist = results[ComicDataEnum.COLORISTE];
                    comic.LanguageISO = results[ComicDataEnum.LANGAGE];
                    var frCulture = new CultureInfo("fr-FR");
                    
                    DateTime dateValue;
                    DateTimeStyles dateTimeStyles = DateTimeStyles.AssumeUniversal;
                    if (DateTime.TryParseExact(results[ComicDataEnum.DATE_PARUTION], "dd MMMM yyyy", frCulture,
                        dateTimeStyles, out dateValue))
                    {
                        comic.Published = dateValue;
                    }
                    else
                    {
                        Log.Warning("Une erreur est apparue lors de l'analyse de la date de publication : {datePublication}", results[ComicDataEnum.DATE_PARUTION]);
                    }

                    if (int.TryParse(results[ComicDataEnum.TOME], out var intValue))
                    {
                        comic.Volume = intValue;
                    }
                    else
                    {
                        Log.Warning("Une erreur est apparue lors de l'analyse du volume : {Tome}", results[ComicDataEnum.TOME]);
                    }

                    const NumberStyles style = NumberStyles.AllowDecimalPoint;
                    if (double.TryParse(results[ComicDataEnum.NOTE], style, CultureInfo.InvariantCulture, out var doubleValue))
                    {
                        comic.Review = doubleValue;
                    }
                    else
                    {
                        Log.Warning("Une erreur est apparue lors de l'analyse de la note : {Note}", results[ComicDataEnum.NOTE]);
                        comic.Review = -1;
                    }
                    
                    if (double.TryParse(results[ComicDataEnum.PRIX].Split('€')[0], style, CultureInfo.InvariantCulture, out doubleValue))
                    {
                        comic.Price = doubleValue;
                    }
                    else
                    {
                        Log.Warning("Une erreur est apparue lors de l'analyse du prix : {Prix}", results[ComicDataEnum.PRIX]);
                        comic.Review = -1;
                    }
                    
                    Update(comic.Id, comic);
                }
                // TODO : throw Exception pour remonter côté WS ?
            }           
        }

        

    }
}
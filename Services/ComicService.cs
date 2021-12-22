using MyComicsManagerApi.Models;
using MongoDB.Driver;
using System.Collections.Generic;
using Serilog;
using System.IO;
using System;
using System.Globalization;
using System.Linq;
using MyComicsManagerApi.DataParser;
using MyComicsManagerApi.Exceptions;
using MyComicsManagerApi.Utils;

namespace MyComicsManagerApi.Services
{
    public class ComicService
    {
        private readonly IMongoCollection<Comic> _comics;
        private readonly LibraryService _libraryService;
        private readonly ComicFileService _comicFileService;
        const int MAX_COMICS_PER_REQUEST = 100;

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
        
        public List<Comic> GetOrderByLastAddedLimitBy(int limit) =>
            _comics.Find(comic => true).SortByDescending(comic => comic.Added).Limit(limit < MAX_COMICS_PER_REQUEST ? limit : MAX_COMICS_PER_REQUEST).ToList();
        
        public List<Comic> GetWithoutIsbnLimitBy(int limit) =>
            _comics.Find(comic => string.IsNullOrEmpty(comic.Isbn)).SortBy(comic => comic.Added).Limit(limit < MAX_COMICS_PER_REQUEST ? limit : MAX_COMICS_PER_REQUEST).ToList();

        public List<Comic> GetRandomLimitBy(int limit)
        {
            var list = _comics.Find(comic => true).ToList();
            return list.OrderBy(arg => Guid.NewGuid()).Take(limit < MAX_COMICS_PER_REQUEST ? limit : MAX_COMICS_PER_REQUEST).ToList();
        }
            

        public Comic Get(string id) =>
            _comics.Find(comic => comic.Id == id).FirstOrDefault();
        
        public Comic Create(Comic comic)
        {
            // Note du développeur : 
            // EbookPath est en absolu au début du traitement pour localiser le fichier dans le répertoire d'upload

            if (comic.EbookName == null || comic.EbookPath == null)
            {
                Log.Error("Une des valeurs suivantes est null et ne devrait pas l'être");
                Log.Error("EbookName : {Value}", comic.EbookName);
                Log.Error("EbookPath : {Value}", comic.EbookPath);
                return null;
            }

            // Conversion du fichier en CBZ et mise à jour du path car le nom du fichier peut avoir changer
            _comicFileService.ConvertComicFileToCbz(comic);

            // Déplacement du fichier vers la racine de la librairie sélectionnée
            var destination = _libraryService.GetLibraryPath(comic.LibraryId, LibraryService.PathType.ABSOLUTE_PATH) +
                              comic.EbookName;
            
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
            
            try
            {
                MoveComic(comic.EbookPath, destination);
            }
            catch (Exception)
            {
                return null;
            }

            // A partir de ce point, EbookPath doit être le chemin relatif par rapport à la librairie
            comic.EbookPath = comic.EbookName;
            
            // Récupération des données du fichier ComicInfo.xml si il existe
            if (_comicFileService.HasComicInfoInComicFile(comic))
            {
                comic = _comicFileService.ExtractDataFromComicInfo(comic);
                try
                {
                    UpdateDirectoryAndFileName(comic);
                }
                catch (Exception)
                {
                    return null;
                }
            }

            // Calcul du nombre d'images dans le fichier CBZ
            _comicFileService.SetNumberOfImagesInCbz(comic);
            
            // Insertion en base de données
            comic.Added = DateTime.Now;
            comic.Edited = comic.Added;
            _comics.InsertOne(comic);
 
            // Extraction de l'image de couverture après enregistrement car nommé avec l'id du comic       
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
            comic.Edited = DateTime.Now;
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

                // Renommage du fichier (si le fichier existe déjà, on ne fait rien, car il est déjà présent !)
                MoveComic(origin, libraryPath + comicEbookPath);
                
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

                // Déplacement du fichier (si le fichier existe déjà, on ne fait rien, car il est déjà présent !)
                MoveComic(origin, libraryPath + eBookPath + comic.EbookName);
                comic.EbookPath = eBookPath + comic.EbookName;
            }
        }

        public void Remove(Comic comic)
        {
            // Suppression du fichier
            Comic comicToDelete = _comics.Find(c => (c.Id == comic.Id)).FirstOrDefault();
            if (comicToDelete != null) {    
                
                // Suppression du fichier
                if (File.Exists(_comicFileService.GetComicEbookPath(comic, LibraryService.PathType.ABSOLUTE_PATH))) {
                    File.Delete(_comicFileService.GetComicEbookPath(comic, LibraryService.PathType.ABSOLUTE_PATH));
                }

                // Suppression de l'image de couverture
                if (File.Exists(comicToDelete.CoverPath))
                {
                    File.Delete(comicToDelete.CoverPath);
                }
                //TODO : Gestion des exceptions
            }

            // Suppression de la référence en base de données
            _comics.DeleteOne(c => c.Id == comic.Id);
        }

        public void RemoveAllComicsFromLibrary(string libId)
        {            
            List<Comic> comics = _comics.Find(c => (c.LibraryId == libId)).ToList();
            foreach(Comic c in comics) {
                Remove(c);
            }
        }

        public Comic SearchComicInfoAndUpdate(Comic comic)
        {
            if (string.IsNullOrEmpty(comic.Isbn))
            {
                return null;
            }

            var parser = new BdphileComicHtmlDataParser();
            var results = parser.Parse(comic.Isbn);

            if (results.Count == 0)
            {
                return null;
            }

            comic.Editor = results[ComicDataEnum.EDITEUR];
            comic.Isbn = results[ComicDataEnum.ISBN];
            comic.Penciller = results[ComicDataEnum.DESSINATEUR];
            comic.Serie = results[ComicDataEnum.SERIE];
            comic.Title = results[ComicDataEnum.TITRE];
            comic.Writer = results[ComicDataEnum.SCENARISTE];
            comic.FicheUrl = results[ComicDataEnum.URL];
            comic.Colorist = results[ComicDataEnum.COLORISTE];
            comic.LanguageIso = results[ComicDataEnum.LANGAGE];
            var frCulture = new CultureInfo("fr-FR");

            const DateTimeStyles dateTimeStyles = DateTimeStyles.AssumeUniversal;
            if (DateTime.TryParseExact(results[ComicDataEnum.DATE_PARUTION], "dd MMMM yyyy", frCulture,
                    dateTimeStyles, out var dateValue))
            {
                comic.Published = dateValue;
            }
            else
            {
                Log.Warning(
                    "Une erreur est apparue lors de l'analyse de la date de publication : {DatePublication}",
                    results[ComicDataEnum.DATE_PARUTION]);
            }

            if (int.TryParse(results[ComicDataEnum.TOME], out var intValue))
            {
                comic.Volume = intValue;
            }
            else
            {
                Log.Warning("Une erreur est apparue lors de l'analyse du volume : {Tome}",
                    results[ComicDataEnum.TOME]);
            }

            const NumberStyles style = NumberStyles.AllowDecimalPoint;
            if (double.TryParse(results[ComicDataEnum.NOTE], style, CultureInfo.InvariantCulture,
                    out var doubleValue))
            {
                comic.Review = doubleValue;
            }
            else
            {
                Log.Warning("Une erreur est apparue lors de l'analyse de la note : {Note}",
                    results[ComicDataEnum.NOTE]);
                comic.Review = -1;
            }

            if (double.TryParse(results[ComicDataEnum.PRIX].Split('€')[0], style, CultureInfo.InvariantCulture,
                    out doubleValue))
            {
                comic.Price = doubleValue;
            }
            else
            {
                Log.Warning("Une erreur est apparue lors de l'analyse du prix : {Prix}",
                    results[ComicDataEnum.PRIX]);
                comic.Review = -1;
            }

            Update(comic.Id, comic);
            return comic;
        }
        
        private void MoveComic(string origin, string destination)
        {
            try
            {
                File.Move(origin, destination);
            }
            catch (Exception e)
            {                
                Log.Error(e,"Erreur lors du déplacement du fichier {Origin} vers {Destination}" ,origin, destination);
                
                // Création du répertoire de destination
                var errorPath = _libraryService.GetLibrairiesDirRootPath() + "errors";
                Directory.CreateDirectory(errorPath);
                
                errorPath += Path.DirectorySeparatorChar + Path.GetFileName(destination);
                while (File.Exists(errorPath))
                {
                    Log.Warning("Le fichier {File} existe déjà", destination);
                    string fileName = Path.GetFileNameWithoutExtension(destination) + "-Duplicate";
                    fileName += Path.GetExtension(destination);
                    Log.Warning("Il va être renommé en {FileName}", fileName);
                    errorPath = _libraryService.GetLibrairiesDirRootPath() + "errors" + Path.DirectorySeparatorChar + fileName;
                }
                File.Move(origin, errorPath);
                Log.Error(e,"Le fichier {Origin} a été déplacé dans {Destination}" ,origin, errorPath);
                throw new ComicIoException("Erreur lors du déplacement du fichier. Consulter le répertoire errors.", e);
            }
        }

        

    }
}
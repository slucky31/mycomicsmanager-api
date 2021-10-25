using MyComicsManagerApi.Models;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using System.IO;
using System.Threading.Tasks;
using System;
using MyComicsManagerApi.DataParser;

namespace MyComicsManagerApi.Services
{
    public class ComicService
    {
        private readonly IMongoCollection<Comic> _comics;
        private readonly LibraryService _libraryService;
        private readonly ComicFileService _comicFileService;

        public ComicService(IDatabaseSettings settings, LibraryService libraryService, ComicFileService comicFileService)
        {
            Log.Debug("settings = {@settings}", settings);
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
            
            // Récupération du fichier dans le répertoire d'Upload
            var origin = Path.GetFullPath(_libraryService.GetFileUploadDirRootPath() + comic.EbookName);
            
            // Déplacement du fichier vers la librairie sélectionné
            var destination = _comicFileService.GetComicEbookPath(comic, LibraryService.PathType.FULL_PATH);
            try
            {
                
                // Gestion du cas où le fichier uploadé existe déjà dans la lib
                while (File.Exists(destination))
                {
                    Log.Warning("Le fichier {File} existe déjà", destination);
                    comic.Title = Path.GetFileNameWithoutExtension(destination) + "-Rename";
                    comic.EbookName = comic.Title + Path.GetExtension(destination);
                    Log.Warning("Il va être renommé en {FileName}", comic.EbookName);
                    destination = _comicFileService.GetComicEbookPath(comic, LibraryService.PathType.FULL_PATH);
                }

                File.Move(origin, destination);
                
                // Conversion du fichier en CBZ
                _comicFileService.ConvertComicFileToCbz(comic);
                
            }
            catch (Exception e)
            {                
                Log.Error("Erreur lors du dépalcement du fichier : {0}", e.Message);
                Log.Error("Origin = {@origin}", origin);
                Log.Error("Destination = {@destination}", destination);
                return null;
            }

            // Calcul du nombre d'images dans le fichier CBZ
            _comicFileService.SetNumberOfImagesInCbz(comic);
            
            // Insertion en base de données
            _comics.InsertOne(comic);
 
            // Extraction de l'image de couverture après enregristrement car nommé avec l'id du comic       
            _comicFileService.SetAndExtractCoverImage(comic);
            var filter = Builders<Comic>.Filter.Eq(c => c.Id,comic.Id);
            var update = Builders<Comic>.Update.Set(c => c.CoverPath, comic.CoverPath);            
            this.UpdateField(filter, update);

            return comic;
        }

        public void Update(string id, Comic comic)
        {
            _comics.ReplaceOne(comic => comic.Id == id, comic);
        }

        public void UpdateField(FilterDefinition<Comic> filter, UpdateDefinition<Comic> update)
        {
            var options = new UpdateOptions { IsUpsert = true };
            _comics.UpdateOne(filter, update, options);
        }

        public void Remove(Comic comic)
        {
            // Suppression du fichier
            Comic c = _comics.Find<Comic>(c => (c.Id == comic.Id)).FirstOrDefault();
            if (c != null) {    
                
                // Suppression du fichier
                if (File.Exists(_comicFileService.GetComicEbookPath(comic, LibraryService.PathType.FULL_PATH))) {
                    File.Delete(_comicFileService.GetComicEbookPath(comic, LibraryService.PathType.FULL_PATH));
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
                comic.Editor = results[ComicDataEnum.EDITEUR];
                comic.ISBN = results[ComicDataEnum.ISBN];
                comic.Penciller = results[ComicDataEnum.DESSINATEUR];
                // comic.Published = results[ComicDataEnum.DATE_PARUTION]; // TODO : Conversion de date
                comic.Review = int.Parse(results[ComicDataEnum.NOTE].Split(".")[0]); // TODO : Exception ?
                comic.Serie = results[ComicDataEnum.SERIE];
                comic.Title = results[ComicDataEnum.TITRE];
                comic.Volume = results[ComicDataEnum.TOME];
                comic.Writer = results[ComicDataEnum.SCENARISTE];
                comic.FicheUrl = results[ComicDataEnum.URL];
                comic.Colorist = results[ComicDataEnum.COLORISTE];
                /* TODO : Liste des champs restants à gérer
                - Category               
                - LanguageISO
                - PageCount
                - Price */
                Update(comic.Id, comic);
            }           
        }

        

    }
}
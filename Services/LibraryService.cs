using MyComicsManagerApi.Models;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using System.IO;

namespace MyComicsManagerApi.Services
{
    public class LibraryService
    {
        private readonly IMongoCollection<Library> _libraries;
        private readonly ILibrairiesSettings _libSettings;
        private readonly char[] charsToTrim = {'/', '\\'};

        public enum PathType
        {
            RELATIVE_PATH,
            ABSOLUTE_PATH
        }

        public LibraryService(IDatabaseSettings dbSettings, ILibrairiesSettings libSettings)
        {
            Log.Debug("settings = {@settings}", dbSettings);
            var client = new MongoClient(dbSettings.ConnectionString);
            var database = client.GetDatabase(dbSettings.DatabaseName);
            _libraries = database.GetCollection<Library>(dbSettings.LibrariesCollectionName);
            _libSettings = libSettings;
        }

        public List<Library> Get() =>
            _libraries.Find(library => true).ToList();

        public Library Get(string id) =>
            _libraries.Find<Library>(library => library.Id == id).FirstOrDefault();

        public Library Create(Library libraryIn)
        {
            // Création de la librairie dans MangoDB
            _libraries.InsertOne(libraryIn);

            // Création de la librairie dans libs
            Directory.CreateDirectory(GetLibraryPath(libraryIn.Id, PathType.ABSOLUTE_PATH));
            
            // Création du répertoire d'upload si il n'est pas déjà créé
            GetFileUploadDirRootPath();

            return libraryIn;
        }

        public void Update(string id, Library libraryIn) {
            _libraries.ReplaceOne(library => library.Id == id, libraryIn);
        }

        public void Remove(Library libraryIn)
        {
            
            // Suppression des fichiers et du répértoire dans libs
            string libPath = GetLibraryPath(libraryIn.Id, PathType.ABSOLUTE_PATH);
            if (Directory.Exists(libPath))
            {
                Directory.Delete(libPath, true);
            }

            // Suppression de la référence en base de données
            _libraries.DeleteOne(library => library.Id == libraryIn.Id);
        }

        public string GetLibraryPath(string id, PathType type)
        {    
            Library lib = this.Get(id);
            if (lib == null)
            {
                return null;
            } else {
                string path = "";
                if (type == PathType.ABSOLUTE_PATH)
                {
                    path = GetLibrairiesDirRootPath();
                }
                return path + lib.RelPath.TrimEnd(charsToTrim) + Path.DirectorySeparatorChar; 
            }
        }

        public string GetLibrairiesDirRootPath() {

            Directory.CreateDirectory(_libSettings.LibrairiesDirRootPath.TrimEnd(charsToTrim));
            return _libSettings.LibrairiesDirRootPath.TrimEnd(charsToTrim) + Path.DirectorySeparatorChar;
        }

        public string GetFileUploadDirRootPath() {

            Directory.CreateDirectory(_libSettings.FileUploadDirRootPath.TrimEnd(charsToTrim));
            return _libSettings.FileUploadDirRootPath.TrimEnd(charsToTrim) + Path.DirectorySeparatorChar;
        }

        public string GetCoversDirRootPath()
        {
            Directory.CreateDirectory(_libSettings.CoversDirRootPath.TrimEnd(charsToTrim));
            return _libSettings.CoversDirRootPath.TrimEnd(charsToTrim) + Path.DirectorySeparatorChar;
        }

    }
}
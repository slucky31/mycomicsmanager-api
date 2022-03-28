using MyComicsManagerApi.Models;
using MongoDB.Driver;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using MyComicsManagerApi.Utils;
using Serilog;

namespace MyComicsManagerApi.Services
{
    public class LibraryService
    {
        private static ILogger Log => Serilog.Log.ForContext<LibraryService>();
        
        private readonly IMongoCollection<Library> _libraries;
        private readonly ILibrairiesSettings _libSettings;
        private readonly char[] _charsToTrim = {'/', '\\'};

        public enum PathType
        {
            RELATIVE_PATH,
            ABSOLUTE_PATH
        }

        public LibraryService(IDatabaseSettings dbSettings, ILibrairiesSettings libSettings)
        {
            Log.Here().Debug("settings = {@Settings}", dbSettings);
            var client = new MongoClient(dbSettings.ConnectionString);
            var database = client.GetDatabase(dbSettings.DatabaseName);
            _libraries = database.GetCollection<Library>(dbSettings.LibrariesCollectionName);
            _libSettings = libSettings;
        }

        public List<Library> Get() =>
            _libraries.Find(library => true).ToList();

        public Library Get(string id) =>
            _libraries.Find(library => library.Id == id).FirstOrDefault();

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
            
            // Suppression des fichiers et du répertoire dans libs
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
                return path + lib.RelPath.TrimEnd(_charsToTrim) + Path.DirectorySeparatorChar; 
            }
        }

        public string GetLibrairiesDirRootPath() {

            Directory.CreateDirectory(_libSettings.LibrairiesDirRootPath.TrimEnd(_charsToTrim));
            return _libSettings.LibrairiesDirRootPath.TrimEnd(_charsToTrim) + Path.DirectorySeparatorChar;
        }

        public string GetFileUploadDirRootPath() {

            Directory.CreateDirectory(_libSettings.FileUploadDirRootPath.TrimEnd(_charsToTrim));
            return _libSettings.FileUploadDirRootPath.TrimEnd(_charsToTrim) + Path.DirectorySeparatorChar;
        }

        public string GetCoversDirRootPath()
        {
            Directory.CreateDirectory(_libSettings.CoversDirRootPath.TrimEnd(_charsToTrim));
            return _libSettings.CoversDirRootPath.TrimEnd(_charsToTrim) + Path.DirectorySeparatorChar;
        }
        
        public List<FileInfo> GetUploadedFiles()
        {
            var fileUploadDirRootPath = GetFileUploadDirRootPath();
            string[] extensions = { "*.cbr", "*.cbz", "*.pdf", "*.zip", "*.rar" };
            
            Log.Information("Recherche des fichiers dans {Path}", fileUploadDirRootPath);
            
            // Création du répertoire si il n'existe pas
            Directory.CreateDirectory(fileUploadDirRootPath);

            // Lister les fichiers
            var directory = new DirectoryInfo(fileUploadDirRootPath);        
            return extensions.AsParallel().SelectMany(searchPattern  => directory.EnumerateFiles(searchPattern, SearchOption.AllDirectories)).ToList();
        }

    }
}
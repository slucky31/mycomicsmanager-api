﻿using MyComicsManagerApi.ComputerVision;
using MyComicsManagerApi.Models;
using Serilog;
using SharpCompress.Common;
using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace MyComicsManagerApi.Services
{
    public class ComicFileService
    {
        private readonly LibraryService _libraryService;
        private readonly ComputerVisionService _computerVisionService;

        public ComicFileService(LibraryService libraryService, ComputerVisionService computerVisionService)
        {
            _libraryService = libraryService;
            _computerVisionService = computerVisionService;
        }

 
        public void SetAndExtractCoverImage(Comic comic)
        {
            // Normalizes the path.
            string extractPath = Path.GetFullPath(_libraryService.GetCoversDirRootPath());

            // Update comic with file
            comic.CoverPath = Path.GetFileName(ExtractImageFromCbz(comic, extractPath, 0));
        }

        public List<string> ExtractFirstImages(Comic comic, int nbImagesToExtract)
        {
            // Normalizes the path.
            string extractPath = Path.GetFullPath(_libraryService.GetCoversDirRootPath() + "/isbn/");

            List<string> firstImages = new List<string>();
            for (int i=0;i<nbImagesToExtract;i++)
            {
                string fileName = Path.GetFileName(ExtractImageFromCbz(comic, extractPath, i));
                firstImages.Add(fileName);
            }
            return firstImages;
        }

        public List<string> ExtractLastImages(Comic comic, int nbImagesToExtract)
        {
            // Normalizes the path.
            string extractPath = Path.GetFullPath(_libraryService.GetCoversDirRootPath() + "/isbn/");

            List<string> lastImages = new List<string>();

            if (comic.PageCount == 0)
            {
                SetNumberOfImagesInCbz(comic);
            }

            for (int i = comic.PageCount - nbImagesToExtract; i < comic.PageCount; i++)
            {
                string fileName = Path.GetFileName(ExtractImageFromCbz(comic, extractPath, i));
                lastImages.Add(fileName);
            }

            return lastImages;
        }

        // https://docs.microsoft.com/fr-fr/dotnet/standard/io/how-to-compress-and-extract-files
        private string ExtractImageFromCbz(Comic comic, string extractPath, int imageIndex)
        {
            string zipPath = comic.EbookPath;                       
            Log.Information("Les fichiers seront extraits dans {path}", extractPath);

            // Ensures that the last character on the extraction path
            // is the directory separator char.
            // Without this, a malicious zip file could try to traverse outside of the expected
            // extraction path.
            if (!extractPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            { 
                extractPath += Path.DirectorySeparatorChar;
            }

            string destinationPath = "";
            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {

                if (imageIndex < 0 || imageIndex >= archive.Entries.Count)
                {
                    throw new ArgumentOutOfRangeException("imageIndex", "imageIndex (" + imageIndex + ") doit être compris entre 0 et " + archive.Entries.Count + ".");
                }
                
                var images = archive.Entries.Where(s => s.FullName.EndsWith(".jpg") || s.FullName.EndsWith(".png") || s.FullName.EndsWith(".gif") || s.FullName.EndsWith(".webp"));
                ZipArchiveEntry entry = images.ElementAt(imageIndex);

                if (null != entry)
                {
                    Log.Information("Fichier à extraire {FileName}", entry.FullName);
                    destinationPath = Path.GetFullPath(Path.Combine(extractPath, comic.Id + "-" + imageIndex + Path.GetExtension(entry.FullName)));
                    Log.Information("Destination {destination}", destinationPath);

                    if (destinationPath.StartsWith(extractPath, StringComparison.Ordinal))
                    {
                        if (File.Exists(destinationPath))
                        {
                            File.Delete(destinationPath);
                            // TODO : Supprimer toutes les images dans le cache !!!
                        }
                        entry.ExtractToFile(destinationPath);
                        // TODO : Créer une image plus petite
                    }
                }
            }

            return destinationPath;
        }

        public void ConvertComicFileToCbz(Comic comic)
        {
            string tempDir = CreateTempDirectory();

            // Extartion des images du PDF
            string extension = Path.GetExtension(comic.EbookPath);
            switch (extension)
            {
                case ".cbz":
                    Log.Information("ExtractImagesFromCbz");
                    ExtractImagesFromCbz(comic, tempDir);
                    break;

                case ".pdf":
                    Log.Information("ExtractImagesFromPdf");
                    ExtractImagesFromPdf(comic, tempDir);
                    break;

                case ".cbr":
                    Log.Information("ExtractImagesFromCbr");
                    ExtractImagesFromCbr(comic, tempDir);
                    break;

                default:
                    // TODO : Faudrait faire qqch la, non ?
                    Log.Error("L'extension de ce fichier n'est pas pris en compte : {Extension}", extension);
                    return;
            }

            // Création de l'archive à partir du répertoire
            // https://khalidabuhakmeh.com/create-a-zip-file-with-dotnet-5
            // https://stackoverflow.com/questions/163162/can-you-call-directory-getfiles-with-multiple-filters
            string cbzPath = Path.GetFullPath(Path.Combine(_libraryService.GetFileUploadDirRootPath(), Path.ChangeExtension(comic.EbookPath, ".cbz")));
            Log.Information("CbzPath = {0}", cbzPath);

            var images = Directory.GetFiles(tempDir, "*.*", SearchOption.AllDirectories)
                .Where(s => s.EndsWith(".jpg") || s.EndsWith(".png") || s.EndsWith(".gif") || s.EndsWith(".webp") || s.EndsWith(".xml"));
            if (File.Exists(cbzPath))
            {
                File.Delete(cbzPath);
            }
            using var archive = ZipFile.Open(cbzPath, ZipArchiveMode.Create);
            foreach (var image in images)
            {
                var entry = archive.CreateEntryFromFile(image,Path.GetFileName(image),CompressionLevel.Optimal);
                Log.Information($"{entry.FullName} was compressed.");
            }


            // Suppression du dossier temporaire et du fichier PDF
            try
            {
                Directory.Delete(tempDir, true);
                File.Delete(comic.EbookPath);
            }
            catch (Exception e)
            {
                Log.Error("La suppression du répertoire temporaire a échoué : {0}", e.Message);
            }

            // Mise à jour de l'objet Comic avec le nouveau fichier CBZ
            comic.EbookPath = Path.ChangeExtension(comic.EbookPath, ".cbz");
            comic.EbookName = Path.GetFileName(cbzPath);
        }

        private static void ExtractImagesFromCbz(Comic comic, string tempDir)
        {
            ZipFile.ExtractToDirectory(comic.EbookPath, tempDir, overwriteFiles: true);
        }

        private static void ExtractImagesFromPdf(Comic comic, string tempDir)
        {
            PdfDocument document = PdfDocument.Open(comic.EbookPath);
            foreach (Page page in document.GetPages())
            {
                foreach (var image in page.GetImages())
                {
                    if (!image.TryGetBytes(out _))
                    {
                        IReadOnlyList<byte> b = image.RawBytes;
                        string imageName = Path.Combine(tempDir, "P" + page.Number.ToString("D5") + ".jpg");
                        File.WriteAllBytes(imageName, b.ToArray());
                        Log.Information("Image with {b} bytes on page {page}. Location: {image}.", b.Count, page.Number, imageName);
                    }
                }
            }
        }

        public void ExtractImagesFromCbr(Comic comic, string tempDir)
        {
            
            using (Stream stream = File.OpenRead(comic.EbookPath))
            using (var reader = ReaderFactory.Open(stream))
            {
                while (reader.MoveToNextEntry())
                {
                    if (!reader.Entry.IsDirectory)
                    {
                        Log.Information("Entry : {0}", reader.Entry.Key);
                        reader.WriteEntryToDirectory(tempDir, new ExtractionOptions
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }
                }
            }
        }

        public void SetNumberOfImagesInCbz(Comic comic)
        {
            string zipPath = comic.EbookPath;            
            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {                                            
                var images = archive.Entries.Where(s => s.FullName.EndsWith(".jpg") || s.FullName.EndsWith(".png") || s.FullName.EndsWith(".gif") || s.FullName.EndsWith(".webp"));
                comic.PageCount = images.Count();
            }

        }

        public async Task<List<string>> ExtractISBNFromCbz(Comic comic, int imageIndex)
        {
            string tempDir = CreateTempDirectory();
            Log.Information("tempDir : {0}", tempDir);

            string impagePath = ExtractImageFromCbz(comic, tempDir, imageIndex);
            Log.Information("impagePath : {0}", impagePath);

            string extractedText = await _computerVisionService.ReadTextFromLocalImage(impagePath);
            Log.Information("extractedText : {0}", extractedText);

            // https://regexlib.com/Search.aspx?k=isbn - Author : Churk
            string isbnPattern = "(ISBN[-]*(1[03])*[ ]*(: ){0,1})*(([0-9Xx][- ]*){13}|([0-9Xx][- ]*){10})";
            Regex rgx = new Regex(isbnPattern);

            var isbnList = new List<string>();
            foreach (Match match in rgx.Matches(extractedText))
            {
                isbnList.Add(match.Value);
            }    
            return isbnList;        
        }

        private static string CreateTempDirectory()
        {
            // Création d'un répertoire temporaire pour stocker les images
            string tempDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            Directory.CreateDirectory(tempDir);
            Log.Information("Créaction du répertoire temporaire : {tempDir}", tempDir);

            if (!tempDir.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            { 
                tempDir += Path.DirectorySeparatorChar;
            }

            return tempDir;
        }
    }
}
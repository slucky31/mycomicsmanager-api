using MyComicsManagerApi.ComputerVision;
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
using System.Xml.Serialization;
using UglyToad.PdfPig;

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
            Directory.CreateDirectory(extractPath);

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
            var extractPath = Path.GetFullPath(_libraryService.GetCoversDirRootPath() + "/isbn/");
            Directory.CreateDirectory(extractPath);

            var lastImages = new List<string>();
            if (comic.PageCount == 0)
            {
                SetNumberOfImagesInCbz(comic);
            }

            for (var i = comic.PageCount - nbImagesToExtract; i < comic.PageCount; i++)
            {
                var fileName = Path.GetFileName(ExtractImageFromCbz(comic, extractPath, i));
                lastImages.Add(fileName);
            }

            return lastImages;
        }

        // https://docs.microsoft.com/fr-fr/dotnet/standard/io/how-to-compress-and-extract-files
        private string ExtractImageFromCbz(Comic comic, string extractPath, int imageIndex)
        {
            var zipPath = GetComicEbookPath(comic, LibraryService.PathType.ABSOLUTE_PATH);                       
            Log.Information("Les fichiers seront extraits dans {Path}", extractPath);

            // Ensures that the last character on the extraction path
            // is the directory separator char.
            // Without this, a malicious zip file could try to traverse outside of the expected
            // extraction path.
            if (!extractPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            { 
                extractPath += Path.DirectorySeparatorChar;
            }

            using var archive = ZipFile.OpenRead(zipPath);
            if (imageIndex < 0 || imageIndex >= archive.Entries.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(imageIndex), "imageIndex (" + imageIndex + ") doit être compris entre 0 et " + archive.Entries.Count + ".");
            }
                
            var images = archive.Entries.Where(s => s.FullName.EndsWith(".jpg") || s.FullName.EndsWith(".png") || s.FullName.EndsWith(".gif") || s.FullName.EndsWith(".webp")).OrderBy(s => s.FullName);
            ZipArchiveEntry entry = images.ElementAt(imageIndex);
            Log.Information("Fichier à extraire {FileName}", entry.FullName);
            var destinationPath = Path.GetFullPath(Path.Combine(extractPath, comic.Id + "-" + imageIndex + Path.GetExtension(entry.FullName)));
            Log.Information("Destination {Destination}", destinationPath);

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

            return destinationPath;
        }

        public void ConvertComicFileToCbz(Comic comic)
        {
            var tempDir = CreateTempDirectory();

            // Extraction des images du PDF
            var extension = Path.GetExtension(comic.EbookPath);
            switch (extension)
            {
                case ".cbz":
                    Log.Information("ExtractImagesFromCbz");
                    ExtractImagesFromCbz(comic.EbookPath, tempDir);
                    break;

                case ".pdf":
                    Log.Information("ExtractImagesFromPdf");
                    ExtractImagesFromPdf(comic.EbookPath, tempDir);
                    break;

                case ".cbr":
                    Log.Information("ExtractImagesFromCbr");
                    ExtractImagesFromCbr(comic.EbookPath, tempDir);
                    break;

                default:
                    // TODO : Faudrait faire qqch la, non ?
                    Log.Error("L'extension de ce fichier n'est pas pris en compte : {Extension}", extension);
                    return;
            }
            
            // Suppression du fichier origine
            if (File.Exists(comic.EbookPath))
            {
                if (comic.EbookPath != null)
                {
                    File.Delete(comic.EbookPath);
                }
            }

            comic.EbookPath = Path.ChangeExtension(comic.EbookPath, ".cbz");
            Log.Information("comic.EbookPath = {Path}", comic.EbookPath);

            // Création de l'archive à partir du répertoire
            // https://khalidabuhakmeh.com/create-a-zip-file-with-dotnet-5
            // https://stackoverflow.com/questions/163162/can-you-call-directory-getfiles-with-multiple-filters
            
            var images = Directory.GetFiles(tempDir, "*.*", SearchOption.AllDirectories)
                .Where(s => s.EndsWith(".jpg") || s.EndsWith(".png") || s.EndsWith(".gif") || s.EndsWith(".webp") || s.EndsWith(".xml"));

            if (comic.EbookPath != null)
            {
                using var archive = ZipFile.Open(comic.EbookPath, ZipArchiveMode.Create);
                foreach (var image in images)
                {
                    var entry = archive.CreateEntryFromFile(image,Path.GetFileName(image),CompressionLevel.Optimal);
                    Log.Information("{FullName} was compressed", entry.FullName);
                }
            }

            // Suppression du dossier temporaire
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch (Exception e)
            {
                Log.Error(e,"La suppression du répertoire temporaire a échoué");
            }

            // Mise à jour de l'objet Comic avec le nouveau fichier CBZ et le nouveau chemin
            comic.EbookName = Path.GetFileName(comic.EbookPath);
        }

        private void ExtractImagesFromCbz(string comicEbookPath, string tempDir)
        {
            ZipFile.ExtractToDirectory(comicEbookPath, tempDir, overwriteFiles: true);
        }

        private void ExtractImagesFromPdf(string comicEbookPath, string tempDir)
        {
            var document = PdfDocument.Open(comicEbookPath);
            foreach (var page in document.GetPages())
            {
                foreach (var image in page.GetImages())
                {
                    if (!image.TryGetBytes(out _))
                    {
                        IReadOnlyList<byte> b = image.RawBytes;
                        string imageName = Path.Combine(tempDir, "P" + page.Number.ToString("D5") + ".jpg");
                        File.WriteAllBytes(imageName, b.ToArray());
                        Log.Information("Image with {Size} bytes on page {Page}. Location: {Image}", b.Count, page.Number, imageName);
                    }
                }
            }
        }

        private static void ExtractImagesFromCbr(string comicEbookPath, string tempDir)
        {
            using Stream stream = File.OpenRead(comicEbookPath);
            using var reader = ReaderFactory.Open(stream);
            while (reader.MoveToNextEntry())
            {
                // au cas où : https://docs.microsoft.com/fr-fr/dotnet/csharp/language-reference/keywords/continue
                if (reader.Entry.IsDirectory) continue;
                
                Log.Information("Key : {Key}", reader.Entry.Key);
                
                reader.WriteEntryToDirectory(tempDir, new ExtractionOptions
                {
                    ExtractFullPath = true,
                    Overwrite = true
                });
            }
        }

        public void SetNumberOfImagesInCbz(Comic comic)
        {
            var zipPath = GetComicEbookPath(comic, LibraryService.PathType.ABSOLUTE_PATH);
            using var archive = ZipFile.OpenRead(zipPath);
            var images = archive.Entries.Where(s => s.FullName.EndsWith(".jpg") || s.FullName.EndsWith(".png") || s.FullName.EndsWith(".gif") || s.FullName.EndsWith(".webp"));
            comic.PageCount = images.Count();
        }

        public async Task<List<string>> ExtractIsbnFromCbz(Comic comic, int imageIndex)
        {
            var tempDir = CreateTempDirectory();
            Log.Information("tempDir : {Dir}", tempDir);

            var imagePath = ExtractImageFromCbz(comic, tempDir, imageIndex);
            Log.Information("imagePath : {Path}", imagePath);

            var extractedText = await _computerVisionService.ReadTextFromLocalImage(imagePath);
            Log.Information("extractedText : {Text}", extractedText);

            // https://regexlib.com/Search.aspx?k=isbn - Author : Churk
            const string isbnPattern = "(ISBN[-]*(1[03])*[ ]*(: ){0,1})*(([0-9Xx][- ]*){13}|([0-9Xx][- ]*){10})";
            var rgx = new Regex(isbnPattern);

            var isbnList = new List<string>();
            foreach (Match match in rgx.Matches(extractedText))
            {
                isbnList.Add(match.Value);
            }    
            return isbnList;        
        }
        
        public async Task<string> ExtractTitleFromCbz(Comic comic)
        {
            var tempDir = CreateTempDirectory();
            Log.Information("tempDir : {Dir}", tempDir);

            // Extraction des infos de la page de couverture
            var imagePath = ExtractImageFromCbz(comic, tempDir, 0);
            Log.Information("imagePath : {Path}", imagePath);

            var extractedText = await _computerVisionService.ReadTextFromLocalImage(imagePath);
            Log.Information("extractedText : {Text}", extractedText);

            return extractedText;
        }

        public bool HasComicInfoInComicFile(Comic comic)
        {
            var comicEbookPath = GetComicEbookPath(comic, LibraryService.PathType.ABSOLUTE_PATH);
            
            using var zipToOpen = new FileStream(comicEbookPath, FileMode.Open);
            using var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update);
            var entry = archive.GetEntry("ComicInfo.xml");
            return (entry != null);
        }

        public void AddComicInfoInComicFile(Comic comic)
        {
            var comicInfo = new ComicInfo
            {
                Title = comic.Title,
                Series = comic.Serie,
                Writer = comic.Writer,
                Penciller = comic.Penciller,
                Colorist = comic.Colorist,
                Editor = comic.Editor,
                PageCount = comic.PageCount,
                LanguageISO = comic.LanguageIso,
                ISBN = comic.Isbn,
                Web = comic.FicheUrl,
                Price = comic.Price,
                Published = comic.Published,
                Tags = comic.Category,
                Review = comic.Review,
                Volume = comic.Volume
                
            };

            var comicEbookPath = GetComicEbookPath(comic, LibraryService.PathType.ABSOLUTE_PATH);

            using var zipToOpen = new FileStream(comicEbookPath, FileMode.Open);
            using var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update);
            
            // Suppression du fichier ComicInfo.xml si il exsite
            var entry = archive.GetEntry("ComicInfo.xml");
            entry?.Delete();
            
            // Ajout du fichier ComicInfo.xml dans l'archive
            var comicInfoEntry = archive.CreateEntry("ComicInfo.xml");
            using var writer = new StreamWriter(comicInfoEntry.Open());
            var mySerializer = new XmlSerializer(typeof(ComicInfo));
            mySerializer.Serialize(writer, comicInfo);
            writer.Close();
        }
        
        public Comic ExtractDataFromComicInfo(Comic comic)
        {
            var comicEbookPath = GetComicEbookPath(comic, LibraryService.PathType.ABSOLUTE_PATH);
            
            using var zipToOpen = new FileStream(comicEbookPath, FileMode.Open);
            using var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update);
            
            // Vérification de la présence du fichier ComicInfo.xml
            var entry = archive.GetEntry("ComicInfo.xml");
            if (entry == null) return comic;
            
            // Construction de l'objet ComicInfo à partir de l'XML
            using var reader = new StreamReader(entry.Open());
            var serializer = new XmlSerializer(typeof(ComicInfo));
            var comicInfo = (ComicInfo) serializer.Deserialize(reader);

            // Récupération des informations
            if (comicInfo == null) return comic;
            comic.Title = comicInfo.Title;
            comic.Serie = comicInfo.Series;
            comic.Writer = comicInfo.Writer;
            comic.Penciller = comicInfo.Penciller;
            comic.Colorist = comicInfo.Colorist;
            comic.Editor = comicInfo.Editor;
            comic.LanguageIso = comicInfo.LanguageISO;
            comic.Isbn = comicInfo.ISBN;
            comic.FicheUrl = comicInfo.Web;
            comic.Price = comicInfo.Price;
            comic.Published = comicInfo.Published;
            comic.Category = comicInfo.Tags;
            comic.Review = comicInfo.Review;
            comic.Volume = comicInfo.Volume;

            return comic;
        }

        private static string CreateTempDirectory()
        {
            // Création d'un répertoire temporaire pour stocker les images
            var tempDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            Directory.CreateDirectory(tempDir);
            Log.Information("Création du répertoire temporaire : {Dir}", tempDir);

            if (!tempDir.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            { 
                tempDir += Path.DirectorySeparatorChar;
            }

            return tempDir;
        }
        
        public string GetComicEbookPath(Comic comic, LibraryService.PathType pathType)
        {
            return _libraryService.GetLibraryPath(comic.LibraryId, pathType) + comic.EbookPath;
        }
    }
}
using MyComicsManagerApi.Models;
using Serilog;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;
using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace MyComicsManagerApi.Services
{
    public class ComicFileService
    {

        private readonly LibraryService _libraryService;

        public ComicFileService(LibraryService libraryService)
        {
            _libraryService = libraryService;
        }


        //
        // https://docs.microsoft.com/fr-fr/dotnet/standard/io/how-to-compress-and-extract-files
        //
        public void SetAndExtractCoverImage(Comic comic)
        {
            string zipPath = comic.EbookPath;

            // Normalizes the path.
            string extractPath = Path.GetFullPath(_libraryService.GetCoversDirRootPath());
            Log.Information("Les fichiers seront extraits dans {path}", extractPath);

            // Ensures that the last character on the extraction path
            // is the directory separator char.
            // Without this, a malicious zip file could try to traverse outside of the expected
            // extraction path.
            if (!extractPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                extractPath += Path.DirectorySeparatorChar;

            string destinationPath = "";

            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                ZipArchiveEntry entry = archive.Entries.First();
                if (null != entry)
                {
                    Log.Information("Fichier à extraire {FileName}", entry.FullName);
                    destinationPath = Path.GetFullPath(Path.Combine(extractPath, comic.Id + ".jpg"));
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

            // Update comic with file
            comic.CoverPath = destinationPath;

        }

        public void ConvertComicFileToCbz(Comic comic)
        {
            string tempDir = CreateTempDirectory();

            // Extartion des images du PDF

            switch (Path.GetExtension(comic.EbookPath))
            {
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
                    Log.Error("L'extension de ce fichier n'est pas pris en compte.");
                    return;
            }

            // Création de l'archive à partir du répertoire
            string cbzPath = Path.GetFullPath(Path.Combine(_libraryService.GetFileUploadDirRootPath(), Path.ChangeExtension(comic.EbookPath, ".cbz")));
            Log.Information("CbzPath = {0}", cbzPath);
            ZipFile.CreateFromDirectory(tempDir, cbzPath);

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
            comic.EbookPath = cbzPath;
            comic.EbookName = Path.GetFileName(cbzPath);

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

            //using var archive = RarArchive.Open(comic.EbookPath);
            using (Stream stream = File.OpenRead(comic.EbookPath))
            using (var reader = ReaderFactory.Open(stream))
            {
                while (reader.MoveToNextEntry())
                {
                    if (!reader.Entry.IsDirectory)
                    {
                        Log.Information("Entry : {0}", reader.Entry.Key);
                        reader.WriteEntryToDirectory(tempDir, new ExtractionOptions()
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }
                }
            }
        }

        private static string CreateTempDirectory()
        {
            // Création d'un répertoire temporaire pour stocker les images
            string tempDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            Directory.CreateDirectory(tempDir);
            Log.Information("Créaction du répertoire temporaire : {tempDir}", tempDir);

            if (!tempDir.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                tempDir += Path.DirectorySeparatorChar;

            return tempDir;
        }
    }
}

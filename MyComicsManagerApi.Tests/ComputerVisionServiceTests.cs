using FluentAssertions;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using MyComicsManagerApi.ComputerVision;
using MyComicsManagerApi.Models;
using System.Threading.Tasks;
using Xunit;

namespace MyComicsManagerApiTests
{
    public class ComputerVisionServiceTests
    {
        private ComputerVisionService service { get; set; }

        private const string READ_TEXT_URL_IMAGE = "https://intelligentkioskstore.blob.core.windows.net/visionapi/suggestedphotos/3.png";

        // https://livre.ciclic.fr/sites/default/files/isbn.jpg
        // https://wikimedi.ca/wiki/Fichier:ISBNetEAN.jpg
        // https://helpcenter-io.s3.amazonaws.com/uploads/fantasticbook/3of3CUrBJChWdstpToJObHbrQZaINNpCSZQOOlDj-file-aGOoKmVcMU.png
        // https://secure.sogides.com/public/produits/9782/894/316/qu_9782894316467.jpg
        // https://secure.sogides.com/public/produits/9782/764/026/qu_9782764026540.jpg


    }
}

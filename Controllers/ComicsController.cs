using Microsoft.AspNetCore.Mvc;
using MyComicsManagerApi.Models;
using MyComicsManagerApi.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hangfire;

namespace MyComicsManagerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComicsController : ControllerBase
    {
        private readonly ComicService _comicService;
        // TODO : ne devrait pas être utilisé mais encapsulé dans le service ComicService
        private readonly ComicFileService _comicFileService;

        public ComicsController(ComicService comicService, ComicFileService comicFileService)
        {
            _comicService = comicService;
            _comicFileService = comicFileService;
        }

        [HttpGet]
        public ActionResult<List<Comic>> Get() =>
            _comicService.Get();
        
        [HttpGet("random/limit/{limit:int}")]
        public ActionResult<List<Comic>> GetRandomLimitBy(int limit) =>
            _comicService.GetRandomLimitBy(limit);
        
        [HttpGet("withoutIsbn/limit/{limit:int}")]
        public ActionResult<List<Comic>> ListComicsWithoutIsbnLimitBy(int limit) =>
            _comicService.GetWithoutIsbnLimitBy(limit);
        
        [HttpGet("orderBy/lastAdded/limit/{limit:int}")]
        public ActionResult<List<Comic>> ListComicsOrderByLastAddedLimitBy(int limit) =>
            _comicService.GetOrderByLastAddedLimitBy(limit);

        [HttpGet("{id:length(24)}", Name = "GetComic")]
        public ActionResult<Comic> Get(string id)
        {
            var comic = _comicService.Get(id);

            if (comic == null)
            {
                return NotFound();
            }

            return comic;
        }

        [HttpPost]
        public ActionResult<Comic> Create(Comic comic)
        {
            var createdComic = _comicService.Create(comic);

            if (createdComic == null)
            {
                return NotFound();
            }
            else
            {
                return CreatedAtRoute("GetComic", new { id = comic.Id.ToString() }, comic);
            }
        }

        [HttpPut("{id:length(24)}")]
        public IActionResult Update(string id, Comic comicIn)
        {
            var comic = _comicService.Get(id);

            if (comic == null)
            {
                return NotFound();
            }

            _comicService.Update(id, comicIn);

            return NoContent();
        }

        [HttpGet("searchcomicinfo/{id:length(24)}")]
        public ActionResult<Comic> SearchComicInfo(string id)
        {
            var comic = _comicService.Get(id);
            if (comic == null)
            {
                return NotFound();
            }

            comic = _comicService.SearchComicInfoAndUpdate(comic);
            if (comic == null)
            {
                return NotFound();
            }

            return _comicService.Get(id);
        }

        [HttpGet("extractcover/{id:length(24)}")]
        public ActionResult<Comic> SetAndExtractCoverImage(string id)
        {
            var comic = _comicService.Get(id);

            if (comic == null)
            {
                return NotFound();
            }

            _comicFileService.SetAndExtractCoverImage(comic);
            _comicService.Update(id, comic);

            return _comicService.Get(id);
        }

        [HttpGet("extractisbn/{id:length(24)}&{indexImage:int}")]
        public ActionResult<List<string>> ExtractISBN(string id, int indexImage)
        {
            var comic = _comicService.Get(id);

            if (comic == null)
            {
                return NotFound();
            }

            // TODO : check index image < nb images

            // Evitement de l'utilisation de await / async
            // https://visualstudiomagazine.com/Blogs/Tool-Tracker/2019/10/calling-methods-async.aspx
            Task<List<string>> task = _comicFileService.ExtractIsbnFromCbz(comic, indexImage);
            var isbnList = task.Result;

            return isbnList;
        }
        
        [HttpGet("extractTitle/{id:length(24)}")]
        public ActionResult<string> ExtractTitle(string id)
        {
            var comic = _comicService.Get(id);

            if (comic == null)
            {
                return NotFound();
            }
            
            var task = _comicFileService.ExtractTitleFromCbz(comic);
            return task.Result;
        }

        [HttpGet("extractimages/{id:length(24)}&{nbImagesToExtract:int}&{first:bool}")]
        public ActionResult<List<string>> ExtractImages(string id, int nbImagesToExtract, bool first)
        {
            var comic = _comicService.Get(id);

            if (comic == null)
            {
                return NotFound();
            }

            // TODO : check index image < nb images

            if (first)
            {
                return _comicFileService.ExtractFirstImages(comic, nbImagesToExtract);
            } else
            {
                return _comicFileService.ExtractLastImages(comic, nbImagesToExtract);
            }
            
        }
        
        [HttpPost("{id:length(24)}/convertImagesToWebP")]
        public ActionResult<Comic> ConvertImagesToWebP(string id)
        {
            var comic = _comicService.Get(id);
            if (comic == null)
            {
                return NotFound();
            }
            
            var jobId = BackgroundJob.Enqueue(() => _comicService.ConvertImagesToWebP(comic));

            return Ok();
            
        }


        [HttpDelete("{id:length(24)}")]
        public IActionResult Delete(string id)
        {
            var comic = _comicService.Get(id);

            if (comic == null)
            {
                return NotFound();
            }

            _comicService.Remove(comic);

            return NoContent();
        }

        [HttpDelete("deleteallcomicsfromlib/{id:length(24)}")]
        public IActionResult DeleteAllComicsFromLib(string id)
        {
            _comicService.RemoveAllComicsFromLibrary(id);

            return NoContent();
        }
    }
}
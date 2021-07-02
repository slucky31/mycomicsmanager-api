using MyComicsManagerApi.Models;
using MyComicsManagerApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace MyComicsManagerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComicsController : ControllerBase
    {
        private readonly ComicService _comicService;
        private readonly ComicFileService _comicFileService;

        public ComicsController(ComicService comicService, ComicFileService comicFileService)
        {
            _comicService = comicService;
            _comicFileService = comicFileService;
        }

        [HttpGet]
        public ActionResult<List<Comic>> Get() =>
            _comicService.Get();

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

            _comicService.SearchComicInfoAndUpdate(comic);

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
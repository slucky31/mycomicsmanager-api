using MyComicsManagerApi.Models;
using MyComicsManagerApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;


namespace MyComicsManagerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LibrariesController : ControllerBase
    {
        private readonly LibraryService _libraryService;

        private readonly ILogger<LibrariesController> _logger;

        public LibrariesController(ILogger<LibrariesController> logger, LibraryService libraryService)
        {
            _libraryService = libraryService;
            _logger = logger;
        }

        [HttpGet]
        public ActionResult<List<Library>> Get() =>
            _libraryService.Get();

        [HttpGet("{id:length(24)}", Name = "GetLibrary")]
        public ActionResult<Library> Get(string id)
        {
            var library = _libraryService.Get(id);

            if (library == null)
            {
                return NotFound();
            }

            return library;
        }

        [HttpPost]
        public ActionResult<Library> Create(Library library)
        {   
            _libraryService.Create(library);
            
            return CreatedAtRoute("GetLibrary", new { id = library.Id.ToString() }, library);
        }

        [HttpPut("{id:length(24)}")]
        public IActionResult Update(string id, Library libraryIn)
        {
            var library = _libraryService.Get(id);

            if (library == null)
            {
                return NotFound();
            }

            _libraryService.Update(id, libraryIn);

            return NoContent();
        }

        [HttpDelete("{id:length(24)}")]
        public IActionResult Delete(string id)
        {
            var library = _libraryService.Get(id);

            if (library == null)
            {
                return NotFound();
            }

            _libraryService.Remove(library);

            return NoContent();
        }
    }
}
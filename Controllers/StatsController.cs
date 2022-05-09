using MyComicsManagerApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace MyComicsManagerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatsController : ControllerBase
    {
        private readonly ComicService _comicService;
        
        public StatsController(ComicService comicService)
        {
            _comicService = comicService;
        }
        
        [HttpGet("comics")]
        public ActionResult<long> GetNbComics()
        {
            return _comicService.CountComics();
        }
        
        [HttpGet("comicsWithoutSeries")]
        public ActionResult<long> GetNbComicsWithoutSeries()
        {
            return _comicService.CountComicsWithoutSerie();
        }
        
        [HttpGet("comicsWithoutIsbn")]
        public ActionResult<long> GetNbComicsWithoutIsbn()
        {
            return _comicService.CountComicsWithoutIsbn();
        }
        
        [HttpGet("comicsRead")]
        public ActionResult<long> GetNbComicsRead()
        {
            return _comicService.CountComicsRead();
        }
        
        [HttpGet("comicsUnRead")]
        public ActionResult<long> GetNbComicsUnRead()
        {
            return _comicService.CountComicsUnRead();
        }
        
        
    }
}
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using MyComicsManagerApi.Models;


namespace MyComicsManagerApi.Services;

public class GoogleBooksApiDataService
{
    private readonly HttpClient _httpClient;
    private readonly string _requestUri;

    public GoogleBooksApiDataService(HttpClient httpClient)
    {
        this._httpClient = httpClient;
        _requestUri = "https://www.googleapis.com/books/v1";
    }

    public async Task<BookInformation> GetBookInformation(string isbn)
    {
        var cleanIsbn = isbn.Replace("-", "").Replace(" ", "");
        var response = await _httpClient.GetStringAsync($"{_requestUri}/volumes?q=isbn:{cleanIsbn}");
        var bookInformation = JsonSerializer.Deserialize<BookInformation>(response);
        return bookInformation;
    }
}
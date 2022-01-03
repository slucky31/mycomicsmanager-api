using Xunit;
using FluentAssertions;
using MyComicsManagerApi.DataParser;

namespace MyComicsManagerApiTests
{
    public class HtmlDataParserTests
    {
        
        private HtmlDataParser Parser { get; set; }
        private const string TitleXPath = "/html/body/div[5]/div[1]/div/div/section/div/div/h1";


        [Fact]
        public void ExtractTextValue()
        {
            Parser = new HtmlDataParser();
            Parser.LoadDocument("https://opensource.org/licenses/MS-PL");
            var title = Parser.ExtractTextValue(TitleXPath);
            title.Should().Be("Microsoft Public License (MS-PL)");

        }

        [Fact]
        public void ExtractTextValueAndSplitOnSeparator()
        {
            Parser = new HtmlDataParser();
            Parser.LoadDocument("https://opensource.org/licenses/MS-PL");
            var title = Parser.ExtractTextValueAndSplitOnSeparator(TitleXPath,"(",0);
            title.Should().Be("Microsoft Public License");

        }

        [Fact]
        public void ExtractAttributeValue()
        {
            Parser = new HtmlDataParser();
            Parser.LoadDocument("https://opensource.org/licenses/MS-PL");
            var title = Parser.ExtractAttributValue(TitleXPath, "class");
            title.Should().Be("page-title");

        }

    }
}

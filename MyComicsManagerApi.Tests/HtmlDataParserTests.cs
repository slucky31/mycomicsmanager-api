using System;
using Xunit;
using FluentAssertions;
using MyComicsManagerApi.DataParser;
using System.Threading.Tasks;

namespace MyComicsManagerApiTests
{
    public class HtmlDataParserTests
    {
        
        private HtmlDataParser parser { get; set; }
        
        
        [Fact]
        public void ExtractTextValue()
        {
            parser = new HtmlDataParser();
            parser.LoadDocument("https://opensource.org/licenses/MS-PL");
            var title = parser.ExtractTextValue("/html/body/div[6]/div/div/div/section/div/div/h1");
            title.Should().Be("Microsoft Public License (MS-PL)");

        }

        [Fact]
        public void ExtractTextValueAndSplitOnSeparator()
        {
            parser = new HtmlDataParser();
            parser.LoadDocument("https://opensource.org/licenses/MS-PL");
            var title = parser.ExtractTextValueAndSplitOnSeparator("/html/body/div[6]/div/div/div/section/div/div/h1","(",0);
            title.Should().Be("Microsoft Public License");

        }

        [Fact]
        public void ExtractAttributValue()
        {
            parser = new HtmlDataParser();
            parser.LoadDocument("https://opensource.org/licenses/MS-PL");
            var title = parser.ExtractAttributValue("/html/body/div[6]/div/div/div/section/div/div/h1", "class");
            title.Should().Be("page-title");

        }

    }
}

using FluentAssertions;
using MyComicsManagerApi.Utils;
using Xunit;

namespace MyComicsManagerApiTests
{
    public class StringExtensionTest
    {
        [Fact]
        public void ToPascalCaseTest()
        {
            var myString = "wAr aNd pEaCe";
            myString.ToPascalCase().Should().Be("WarAndPeace");
            
            myString = "wAr + aNd pEaCe";
            myString.ToPascalCase().Should().Be("WarAndPeace");
            
            myString = "Les_Géants___";
            myString.ToPascalCase().Should().Be("LesGeants");
        }
        
        [Fact]
        public void ToCamlCaseTest()
        {
            var myString = "wAr aNd pEaCe";
            myString.ToCamlCase().Should().Be("warAndPeace");
            
            myString = "wAr + aNd pEaCe";
            myString.ToCamlCase().Should().Be("warAndPeace");
            
            myString = "Les_Géants___";
            myString.ToCamlCase().Should().Be("lesGeants");
        }
        
        [Fact]
        public void RemoveDiacriticsTest()
        {
            const string myString = "éèàîôùûê";
            myString.RemoveDiacritics().Should().Be("eeaiouue");
        }
        
        
    }
}
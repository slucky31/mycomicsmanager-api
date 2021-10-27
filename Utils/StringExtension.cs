using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MyComicsManagerApi.Utils
{
    public static class StringExtension
    {
        public static string ToPascalCase(this string str)
        {
            if (string.IsNullOrEmpty(str) || str.Length <= 1)
            {
                return str;
            }
            
            // Ajout d'une majuscule à chaque début de mot
            var txtInfo = new CultureInfo("en-us", false).TextInfo;
            var stripStr = txtInfo.ToTitleCase(str);
                
            // Conservation uniquement des lettres et des chiffres
            // Source : https://stackoverflow.com/questions/7316258/how-to-get-only-letters-from-a-string-in-c/7316298
            stripStr = new string(stripStr.Where(c => Char.IsLetter(c) || Char.IsDigit(c)).ToArray());
                
            // Suppression des accents
            // Source : https://stackoverflow.com/questions/249087/how-do-i-remove-diacritics-accents-from-a-string-in-net
            return stripStr.RemoveDiacritics();
        }
        
        public static string ToCamlCase(this string str)
        {
            if (string.IsNullOrEmpty(str) || str.Length <= 1)
            {
                return str;
            }
            var caml = str.ToPascalCase();
            return char.ToLowerInvariant(caml[0]) + caml[1..];
        }
        
        public static string RemoveDiacritics(this string str) 
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }
            
            var normalizedString = str.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);

        }
    }
    
    
}
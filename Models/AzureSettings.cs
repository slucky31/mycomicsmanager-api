using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyComicsManagerApi.Models
{
    public class AzureSettings : IAzureSettings
    {
        public string Endpoint { get; set; }
        public string Key { get; set; }
    }

    public interface IAzureSettings
    {
        public string Endpoint { get; set; }
        public string Key { get; set; }
    }
}

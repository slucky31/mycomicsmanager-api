namespace MyComicsManagerApi.Models
{
    public class LibrairiesSettings : ILibrairiesSettings
    {
        public string LibrairiesDirRootPath { get; set; }
        public string FileUploadDirRootPath { get; set; }
        public string CoversDirRootPath { get; set; }

    }

    public interface ILibrairiesSettings
    {
        string LibrairiesDirRootPath { get; set; }
        string FileUploadDirRootPath { get; set; }
        string CoversDirRootPath { get; set; }

    }
}
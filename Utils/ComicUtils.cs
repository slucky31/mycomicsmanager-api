using System;
using System.IO;
using MyComicsManagerApi.Models;
using Serilog;

namespace MyComicsManagerApi.Utils
{
    public class ComicUtils
    {
        public static void MoveComic(string origin, string destination)
        {
            try
            {
                File.Move(origin, destination);
            }
            catch (Exception e)
            {                
                Log.Error(e,"Erreur lors du déplacement du fichier");
                Log.Error("Origin = {Origin}", origin);
                Log.Error("Destination = {Destination}", destination);
                throw new Exception("Erreur lors du déplacement du fichier",e);
            }
        }
    }
}
using System;

namespace MyComicsManagerApi.Exceptions
{
    public class ComicIoException : Exception
    {
        
        public ComicIoException()
        {
        }

        public ComicIoException(string message)
            : base(message)
        {
        }

        public ComicIoException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
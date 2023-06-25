using System.Runtime.Serialization;

namespace ToysRuParser.Exceptions
{
    [Serializable]
    internal class ProductNotFountException : Exception
    {
        public ProductNotFountException()
        {
        }

        public ProductNotFountException(string? message) : base(message)
        {
        }

        public ProductNotFountException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected ProductNotFountException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
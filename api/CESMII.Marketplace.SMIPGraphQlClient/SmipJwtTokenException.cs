using System.Runtime.Serialization;
namespace CESMII.Marketplace.SmipGraphQlClient
{
    [Serializable]
    public class SmipJwtTokenException : Exception
    {
        public SmipJwtTokenException()
        {
        }

        public SmipJwtTokenException(string? message) : base(message)
        {
        }

        public SmipJwtTokenException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected SmipJwtTokenException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}

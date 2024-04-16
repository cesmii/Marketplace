using System.Runtime.Serialization;
using GraphQL;

namespace CESMII.Marketplace.SmipGraphQlClient
{
    [Serializable]
    public class SmipGrapQlException : Exception
    {
        public SmipGrapQlException()
        {
        }

        public SmipGrapQlException(string? message) : base(message)
        {
        }

        public SmipGrapQlException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected SmipGrapQlException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public SmipGrapQlException(string? message, List<GraphQLError> errors) : base(message)
        {
            //TBD - consider processing errors collection to nicely formatted output 
        }

        public SmipGrapQlException(string? message, Exception? innerException, List<GraphQLError> errors) : base(message, innerException)
        {
            //TBD - consider processing errors collection to nicely formatted output 
        }

    }
}

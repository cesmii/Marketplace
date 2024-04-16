namespace CESMII.Marketplace.SmipGraphQlClient.Models
{
    internal class SmipAuthenticationChallengeResponseModel
    {
        public SmipAuthenticationRequestModel authenticationRequest { get; set; }
    }

    internal class SmipAuthenticationRequestModel
    {
        public SmipAuthenticationJwtRequestModel jwtRequest { get; set; }
    }

    internal class SmipAuthenticationJwtRequestModel
    {
        public string Challenge { get; set; }

        public string Message { get; set; }
    }

    internal class SmipAuthenticationTokenResponseModel
    {
        public SmipAuthenticationTokenModel authenticationValidation { get; set; }
    }

    internal class SmipAuthenticationTokenModel
    {
        public string jwtClaim { get; set; }
    }
}

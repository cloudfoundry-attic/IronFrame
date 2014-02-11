namespace IronFoundry.Warden.Protocol
{
    public partial class ErrorResponse : Response
    {
        public override Message.Type ResponseType
        {
            get { return Warden.Protocol.Message.Type.Error; }
        }
    }
}

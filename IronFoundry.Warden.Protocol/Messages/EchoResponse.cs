namespace IronFoundry.Warden.Protocol
{
    public partial class EchoResponse : Response
    {
        public override Message.Type ResponseType
        {
            get { return Warden.Protocol.Message.Type.Echo; }
        }
    }
}

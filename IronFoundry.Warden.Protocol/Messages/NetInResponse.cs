namespace IronFoundry.Warden.Protocol
{
    public partial class NetInResponse : Response
    {
        public override Message.Type ResponseType
        {
            get { return Message.Type.NetIn; }
        }
    }
}

namespace IronFoundry.Warden.Protocol
{
    public partial class NetOutResponse : Response
    {
        public override Message.Type ResponseType
        {
            get { return Message.Type.NetOut; }
        }
    }
}

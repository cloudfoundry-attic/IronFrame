namespace IronFoundry.Warden.Protocol
{
    public partial class PingResponse : Response
    {
        public override Message.Type ResponseType
        {
            get { return Message.Type.Ping; }
        }
    }
}

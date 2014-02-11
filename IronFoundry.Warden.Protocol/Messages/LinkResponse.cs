namespace IronFoundry.Warden.Protocol
{
    public partial class LinkResponse : Response
    {
        public override Message.Type ResponseType
        {
            get { return Message.Type.Link; }
        }
    }
}

namespace IronFoundry.Warden.Protocol
{
    public partial class ListResponse : Response
    {
        public override Message.Type ResponseType
        {
            get { return Message.Type.List; }
        }
    }
}

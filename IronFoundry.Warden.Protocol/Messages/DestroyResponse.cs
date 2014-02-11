namespace IronFoundry.Warden.Protocol
{
    public partial class DestroyResponse : Response
    {
        public override Message.Type ResponseType
        {
            get { return Message.Type.Destroy; }
        }
    }
}

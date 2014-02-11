namespace IronFoundry.Warden.Protocol
{
    public partial class RunResponse : Response
    {
        public override Message.Type ResponseType
        {
            get { return Message.Type.Run; }
        }
    }
}

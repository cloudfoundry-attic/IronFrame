namespace IronFoundry.Warden.Protocol
{
    public partial class StopResponse : Response
    {
        public override Message.Type ResponseType
        {
            get { return Message.Type.Stop; }
        }
    }
}

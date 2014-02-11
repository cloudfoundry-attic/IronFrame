namespace IronFoundry.Warden.Protocol
{
    public partial class CreateResponse : Response
    {
        public override Message.Type ResponseType
        {
            get { return Message.Type.Create; }
        }
    }
}

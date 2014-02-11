namespace IronFoundry.Warden.Protocol
{
    public partial class CopyInResponse : Response
    {
        public override Message.Type ResponseType
        {
            get { return Message.Type.CopyIn; }
        }
    }
}

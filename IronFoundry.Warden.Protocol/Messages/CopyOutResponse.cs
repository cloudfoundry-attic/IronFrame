namespace IronFoundry.Warden.Protocol
{
    public partial class CopyOutResponse : Response
    {
        public override Message.Type ResponseType
        {
            get { return Message.Type.CopyOut; }
        }
    }
}

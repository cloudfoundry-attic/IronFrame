namespace IronFoundry.Warden.Protocol
{
    public partial class LimitMemoryResponse : Response
    {
        public override Message.Type ResponseType
        {
            get { return Message.Type.LimitMemory; }
        }
    }
}

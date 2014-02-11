namespace IronFoundry.Warden.Protocol
{
    public partial class LimitDiskResponse : Response
    {
        public override Message.Type ResponseType
        {
            get { return Message.Type.LimitDisk; }
        }
    }
}

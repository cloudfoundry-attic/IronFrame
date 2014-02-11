namespace IronFoundry.Warden.Protocol
{
    public partial class LimitBandwidthResponse : Response
    {
        public override Message.Type ResponseType
        {
            get { return Message.Type.LimitBandwidth; }
        }
    }
}

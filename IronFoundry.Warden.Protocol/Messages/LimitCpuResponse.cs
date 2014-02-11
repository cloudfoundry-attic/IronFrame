namespace IronFoundry.Warden.Protocol
{
    public partial class LimitCpuResponse : Response
    {
        public override Message.Type ResponseType
        {
            get { return Message.Type.LimitCpu; }
        }
    }
}
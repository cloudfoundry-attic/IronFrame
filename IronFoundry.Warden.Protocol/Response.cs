namespace IronFoundry.Warden.Protocol
{
    public abstract class Response
    {
        public abstract Message.Type ResponseType { get; }
    }
}

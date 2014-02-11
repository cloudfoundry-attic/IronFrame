namespace IronFoundry.Warden.Protocol
{
    public abstract class Request
    {
        public override string ToString()
        {
            return this.GetType().ToString();
        }
    }
}

namespace IronFoundry.Container.Utilities
{
    using System;

    internal sealed class LocalPrincipalData
    {
        private readonly string userName;
        private readonly string password;

        public LocalPrincipalData(string userName, string password)
        {
            if (String.IsNullOrWhiteSpace(userName))
            {
                throw new ArgumentNullException("userName");
            }
            this.userName = userName;

            if (String.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentNullException("password");
            }
            this.password = password;
        }

        public string UserName
        {
            get { return userName; }
        }

        public string Password
        {
            get { return password; }
        }
    }
}

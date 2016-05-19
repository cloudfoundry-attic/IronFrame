using System;
using System.Security.Cryptography.X509Certificates;

namespace ConsoleApplication1
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                // test the cert
                string dataString =
                    "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";
                string password = "password";

                byte[] rawData = Convert.FromBase64String(dataString);

                var cert = new X509Certificate2(rawData, password, X509KeyStorageFlags.UserKeySet);
                Console.WriteLine("SUCCESS");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message + ex.StackTrace);
                Console.WriteLine("FAILURE");
            }
        }
    }
}
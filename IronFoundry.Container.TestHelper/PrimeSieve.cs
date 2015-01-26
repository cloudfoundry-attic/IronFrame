using System.Collections.Generic;

namespace IronFoundry.Warden.TestHelper
{
    //Sieve of Eratosthenes from http://blogs.msdn.com/b/brada/archive/2005/04/17/409081.aspx
    public class PrimeSieve : IEnumerable<int>
    {
        Numbers[] values;
        public PrimeSieve(int max)
        {
            values = new Numbers[max];
        }
        public void ComputePrimes()
        {         
            values[0] = Numbers.Prime; //not really... but, it helps
            values[1] = Numbers.Prime;
 
            //Loop through each unset value
            for (int outer = 2; outer != -1; outer = FirstUnset(outer))
            {
                //The next unset value must be prime
                values[outer] = Numbers.Prime;
                //mark out all multiples of this prime as being Composite
                for (int inner = outer*2; inner < values.Length; inner += outer)
                {
                    values[inner] = Numbers.Composite;
                }
            }
        }

        int FirstUnset(int last)
        {
            for (int i = last; i < values.Length; i++)
            {
                if (values[i] == Numbers.Unset) return i;
            }
            return -1;
        }
 
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<int> GetEnumerator()
        {
            for (int i = 2; i < values.Length; i++)
            {
                if (values[i] == Numbers.Prime) yield return i;
            }
        }
    }
    public enum Numbers
    {
        Unset,
        Prime,
        Composite,
    }
}
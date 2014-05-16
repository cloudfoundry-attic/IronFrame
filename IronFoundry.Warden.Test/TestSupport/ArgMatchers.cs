using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NSubstitute;

public static class ArgMatchers
{
    public static IEnumerable<T> IsSequence<T>(params T[] values)
    {
        return Arg.Is<IEnumerable<T>>(v => CompareSequences(v, values));
    }

    public static T IsSame<T>(T value)
    {
        return Arg.Is<T>(v => Object.ReferenceEquals(v, value));
    }

    //
    // Helper Methods
    //

    static bool CompareSequences(IEnumerable lhs, IEnumerable rhs)
    {
        var lhsEnumerator = lhs.GetEnumerator();
        var rhsEnumerator = rhs.GetEnumerator();

        bool lhsHasValue = lhsEnumerator.MoveNext();
        bool rhsHasValue = rhsEnumerator.MoveNext();

        while (lhsHasValue && rhsHasValue)
        {
            if (!Object.Equals(lhsEnumerator.Current, rhsEnumerator.Current))
                return false;

            lhsHasValue = lhsEnumerator.MoveNext();
            rhsHasValue = rhsEnumerator.MoveNext();
        }

        return !lhsHasValue && !rhsHasValue;
    }
}

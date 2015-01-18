using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronFoundry.Container
{
    public static class Guard
    {
        public static void Equals<T>(T first, T second, string message)
            where T : IEquatable<T>
        {
            if (!first.Equals(second))
                throw new InvalidOperationException(message);
        }

        public static void InRange<T>(T min, T max, T value, string parameter)
            where T : IComparable
        {
            Guard.InRange(min, max, value, parameter, string.Format("{0} must be between {1} and {2}", parameter, min, max));
        }

        public static void InRange<T>(T min, T max, T value, string parameter, string message)
            where T : IComparable
        {
            if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
                throw new ArgumentOutOfRangeException(parameter, message);
        }

        public static void EqualTo<T>(T expected, T value, string parameter, string message)
            where T : IComparable
        {
            if (value.CompareTo(expected) != 0)
                throw new ArgumentOutOfRangeException(parameter, message);
        }

        public static T IsType<T>(object value, string parameter, bool allowNull = false) where T : class
        {
            if (allowNull && value == null)
                return null;

            Guard.NotNull(value, parameter);

            T casted = value as T;
            if (casted == null)
                throw new ArgumentException(String.Format("{0} was not of type {1} (was {2})", parameter, typeof(T).FullName, value.GetType().FullName), parameter);

            return casted;
        }

        public static void NotNull<T>(T value, string parameter) where T : class
        {
            Guard.NotNull(value, parameter, string.Format("{0} is null", parameter));
        }

        public static void NotNull<T>(T value, string parameter, string message) where T : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameter, message);
            }
        }

        public static void NotNullOrEmpty(string value, string parameter)
        {
            if (String.IsNullOrEmpty(value))
            {
                if (value == null)
                {
                    throw new ArgumentNullException(parameter, string.Format("{0} is null", parameter));
                }
                else
                {
                    throw new ArgumentException(string.Format("{0} is empty", parameter), parameter);
                }
            }
        }

        public static void NotEmpty(Guid value, string parameter)
        {
            if (value != Guid.Empty) return;
            throw new ArgumentException(string.Format("{0} is empty", parameter), parameter);
        }

        public static void NotNullOrEmpty<T>(IEnumerable<T> value, string parameter)
        {
            Guard.NotNullOrEmpty(value, parameter, string.Format("{0} is null or empty", parameter));
        }

        public static void NotNullOrEmpty<T>(IEnumerable<T> value, string parameter, string message)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameter, message);
            }

            if (!value.Any())
            {
                throw new ArgumentException(message, parameter);
            }
        }

        public static void NotNullOrWhiteSpace(string value, string parameter)
        {
            NotNullOrWhiteSpace(value, parameter, string.Format("{0} is null or whitespace.", parameter));
        }

        public static void NotNullOrWhiteSpace(string value, string parameter, string message)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                if (value == null)
                {
                    throw new ArgumentNullException(parameter, message);
                }
                else
                {
                    throw new ArgumentException(message, parameter);
                }
            }
        }

        public static void True(bool check, string parameter)
        {
            Guard.True(check, parameter, string.Format("{0} is not true", parameter));
        }

        public static void True(bool check, string parameter, string message)
        {
            if (!check)
                throw new ArgumentException(message, parameter);
        }

        public static void CheckEnumValue<TEnum>(object enumValue)
        {
            Type enumType = typeof(TEnum);
            if (!Enum.IsDefined(enumType, enumValue))
            {
                throw new ArgumentException(String.Format("Invalid value '{0}' provided for enumeration '{1}'", enumValue, enumType.Name));
            }
        }
    }
}

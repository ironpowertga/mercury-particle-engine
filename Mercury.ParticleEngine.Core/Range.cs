﻿namespace Mercury.ParticleEngine {
    using System;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Represents a closed interval of values.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Range : IEquatable<Range>, IFormattable {
        /// <summary>
        /// Defines a template for a regex which can be used to validate a string representation
        /// of an interval. The template contains tokens which should be replaced with culture
        /// specific symbols.
        /// </summary>
        private const string RegexTemplate = @"\[([\$(PositiveSign)\$(NegativeSign)]?[0-9]+)\$(GroupSeparator)([\$(PositiveSign)\$(NegativeSign)]?[0-9]+)\]";

        /// <summary>
        /// Gets a regex pattern which can be used to validate a string representation of an interval
        /// in the specified culture.
        /// </summary>
        /// <param name="provider">The culture in which the interval is represented.</param>
        /// <returns>A regex pattern which can be used to validate the interval representation.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// Thrown if the value passed to the <paramref name="provider"/> parameter is <c>null</c>.
        /// </exception>
        private static string GetFormatPattern(IFormatProvider provider) {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            var numberFormat = NumberFormatInfo.GetInstance(provider);

            return RegexTemplate.Replace("$(PositiveSign)",     numberFormat.PositiveSign)
                                .Replace("$(NegativeSign)",     numberFormat.NegativeSign)
                                .Replace("$(GroupSeparator)",   numberFormat.NumberGroupSeparator);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Range"/> struct.
        /// </summary>
        /// <param name="x">The left boundary value.</param>
        /// <param name="y">The right boundary value.</param>
        /// <exception cref="T:System.ArgumentException">
        /// Thrown if either of the values passed to the <paramref name="x"/> or <paramref name="y"/>
        /// parameters are not finite. That is, positive infinity, negative infinity, or NaN.
        /// </exception>
        public Range(int x, int y) {
            X = Math.Min(x, y);
            Y = Math.Max(x, y);

            Diameter = Math.Abs(X - Y);
            Radius   = Diameter / 2;
            Centre   = X + Radius;
        }

        /// <summary>
        /// Creates a new interval which is a union of two separate intervals, representing a range
        /// that encompasses both of them.
        /// </summary>
        /// <param name="x">The first interval.</param>
        /// <param name="y">The second interval.</param>
        /// <returns>An interval that encompasses both input intervals.</returns>
        /// <example>
        ///     <code lang="C#">
        ///     <![CDATA[[
        ///     var x = new Interval(0f, 10f);
        ///     var y = new Interval(-10f, 0f);
        ///     
        ///     var union = Interval.Union(x, y);
        ///     
        ///     // union.X == -10f;
        ///     // union.Y == 10f;
        ///     ]]>
        ///     </code>
        /// </example>
        static public Range Union(Range x, Range y) => new Range(Math.Min(x.X, y.X), Math.Max(x.Y, y.Y));

        /// <summary>
        /// Gets or sets the inclusive minimum value in the interval.
        /// </summary>
        public readonly int X;

        /// <summary>
        /// Gets or sets the inclusive maximum value in the interval.
        /// </summary>
        public readonly int Y;

        /// <summary>
        /// Gets the diameter (size) of the interval.
        /// </summary>
        public readonly int Diameter;

        /// <summary>
        /// Gets the centre of the interval.
        /// </summary>
        public readonly int Centre;

        /// <summary>
        /// Gets or sets the radius of the interval.
        /// </summary>
        public readonly int Radius;

        /// <summary>
        /// Gets a value indicating whether or not the interval is degenerate. A degenerate interval
        /// is one which contains only a float distinct boundary (X == Y, Diameter == 0).
        /// </summary>
        public bool IsDegenerate => X.Equals(Y);

        /// <summary>
        /// Gets a value indicating whether or not the interval is proper. A proper interval is one
        /// which is neither empty or degenerate.
        /// </summary>
        public bool IsProper => !X.Equals(Y);

        /// <summary>
        /// Gets the interior of the interval. The interior is the largest proper interval contained
        /// within this interval.
        /// </summary>
        public Range Interior {
            get
            {
                var x = X + 1;
                var y = Y - 1;

                return new Range(x, y);
            }
        }

        /// <summary>
        /// Gets the closure of the interval. The closure is the smallest proper interval which
        /// contains this interval.
        /// </summary>
        public Range Closure {
            get
            {
                var x = X - 1;
                var y = Y + 1;

                return new Range(x, y);
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not the specified value is contained within the
        /// closed interval.
        /// </summary>
        /// <param name="value">The floating point value.</param>
        /// <returns><c>true</c> if the specified value is contained within the closed interval;
        /// else <c>false</c>.</returns>
        public bool Contains(int value) => value >= X && value <= Y;

        /// <summary>
        /// Creates a new interval by parsing an ISO 31-11 string representation of a closed interval.
        /// </summary>
        /// <param name="value">Input string value.</param>
        /// <returns>A new interval value.</returns>
        /// <exception cref="FormatException">Thrown if the input String is not in a valid ISO 31-11
        /// closed interval format, or if the numbers represented within the closed interval could
        /// not be parsed.</exception>
        /// <remarks>
        /// Example of a well formed ISO 31-11 closed interval: <i>"[0,1]"</i>. Open intervals are
        /// not supported.
        /// </remarks>
        /// <exception cref="T:System.FormatException">
        /// Thrown if the value passed to the <paramref name="value"/> parameter is not in the
        /// correct format for an ISO 31-11 closed interval, or if the numbers represented within
        /// the closed interval could not be parsed.
        /// </exception>
        public static Range Parse(String value) => Parse(value, CultureInfo.InvariantCulture);

        /// <summary>
        /// Creates a new interval by parsing an ISO 31-11 string representation of an interval.
        /// </summary>
        /// <param name="value">Input string value.</param>
        /// <param name="format">The format provider.</param>
        /// <remarks>
        /// Example of a well formed ISO 31-11 interval: <i>"[0,1]"</i>.
        /// </remarks>
        /// <exception cref="T:System.FormatException">
        /// Thrown if the value passed to the <paramref name="value"/> parameter is not in the
        /// correct format for an ISO 31-11 interval, or if the numbers represented within the
        /// closed interval could not be parsed.
        /// </exception>
        public static Range Parse(string value, IFormatProvider format) {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (format == null)
                throw new ArgumentNullException(nameof(format));

            var regex = new Regex(GetFormatPattern(format));

            if (regex.IsMatch(value)) {
                var match = regex.Match(value);

                var group1 = match.Groups[1].Value;
                var group2 = match.Groups[2].Value;

                // No error handling required on boundary parsing, regex has already validated the
                // format of the boundary values...
                var x = Int32.Parse(group1, NumberStyles.Integer, format);
                var y = Int32.Parse(group2, NumberStyles.Integer, format);

                return new Range(x, y);
            }

            throw new FormatException("value is not in the correct format for an ISO 31-11 closed interval in ℝ form.");
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        ///     <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj) {
            if (obj != null)
                if (obj is Range)
                    return Equals((Range)obj);

            return false;
        }

        /// <summary>
        /// Determines whether the specified <see cref="RangeF"/> is equal to this instance.
        /// </summary>
        /// <param name="value">The <see cref="RangeF"/> to compare with this instance.</param>
        /// <returns>
        ///     <c>true</c> if the specified <see cref="RangeF"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(Range value) => X.Equals(value.X) &&
                                           Y.Equals(value.Y);

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode();

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString() => ToString("G", CultureInfo.InvariantCulture);

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <param name="formatProvider">The format provider.</param>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public string ToString(IFormatProvider formatProvider) => ToString("G", formatProvider);

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="formatProvider">The format provider.</param>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public string ToString(string format, IFormatProvider formatProvider) {
            var numberFormat = NumberFormatInfo.GetInstance(formatProvider);

            var minimum = X.ToString(format, numberFormat);
            var maximum = Y.ToString(format, numberFormat);
            
            var seperator = numberFormat.NumberGroupSeparator;

            return String.Format(formatProvider, "[{0}{1}{2}]", minimum, seperator, maximum);
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="x">The lvalue.</param>
        /// <param name="y">The rvalue.</param>
        /// <returns>
        ///     <c>true</c> if the lvalue <see cref="RangeF"/> is equal to the rvalue; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator ==(Range x, Range y) => x.Equals(y);

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="x">The lvalue.</param>
        /// <param name="y">The rvalue.</param>
        /// <returns>
        ///     <c>true</c> if the lvalue <see cref="RangeF"/> is not equal to the rvalue; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator !=(Range x, Range y) => !x.Equals(y);

        static public implicit operator Range(int value) => new Range(value, value);
    }
}
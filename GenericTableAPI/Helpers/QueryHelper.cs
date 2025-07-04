﻿using System.Text;
using System.Text.RegularExpressions;

namespace GenericTableAPI.Helpers
{
    public static class QueryHelper
    {
        private static readonly Regex IdentifierRegex
            = new(@"^([A-Za-z_][A-Za-z0-9_]*)(\.[A-Za-z_][A-Za-z0-9_]*)*$", RegexOptions.Compiled);

        public static void ValidateIdentifier(string? name)
        {
            if (string.IsNullOrEmpty(name))
                return;
            if (!IdentifierRegex.IsMatch(name))
                    throw new ArgumentException($"Invalid SQL identifier: '{name}'", nameof(name));
        }

        /// <summary>
        /// Concatenate any number of segments (strings or other objects) into one SQL string.
        /// </summary>
        public static string Build(params object?[] segments)
        {
            var sb = new StringBuilder();
            foreach (var seg in segments)
            {
                if (seg == null)
                    continue;

                // If it’s already a string, just append it;
                // otherwise call ToString().
                sb.Append(seg is string s ? s : seg.ToString());
            }
            return sb.ToString();
        }
    }
}

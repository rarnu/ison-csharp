// Ison.cs - C# parser and serializer for ISON (Interchange Simple Object Notation)
// Ported from ison-go

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Ison
{
    /// <summary>
    /// Current version of the Ison package
    /// </summary>
    public static class VersionInfo
    {
        public const string Version = "1.0.1";
    }

    /// <summary>
    /// Represents the type of an ISON value
    /// </summary>
    public enum ValueType
    {
        Null,
        Bool,
        Int,
        Float,
        String,
        Reference
    }

    /// <summary>
    /// Represents an ISON reference (e.g., :1, :user:42, :OWNS:5)
    /// </summary>
    public struct Reference : IEquatable<Reference>
    {
        public string ID { get; set; }
        public string Namespace { get; set; }
        public string Relationship { get; set; }

        public Reference(string id, string ns = "", string relationship = "")
        {
            ID = id;
            Namespace = ns;
            Relationship = relationship;
        }

        /// <summary>
        /// Converts the reference back to ISON format
        /// </summary>
        public string ToIson()
        {
            if (!string.IsNullOrEmpty(Relationship))
            {
                return $":{Relationship}:{ID}";
            }
            if (!string.IsNullOrEmpty(Namespace))
            {
                return $":{Namespace}:{ID}";
            }
            return $":{ID}";
        }

        /// <summary>
        /// Returns true if this is a relationship reference (uppercase namespace)
        /// </summary>
        public bool IsRelationship()
        {
            return !string.IsNullOrEmpty(Relationship);
        }

        /// <summary>
        /// Returns the namespace or relationship name
        /// </summary>
        public string GetNamespace()
        {
            if (!string.IsNullOrEmpty(Relationship))
            {
                return Relationship;
            }
            return Namespace;
        }

        public override string ToString()
        {
            return ToIson();
        }

        public bool Equals(Reference other)
        {
            return ID == other.ID && 
                   Namespace == other.Namespace && 
                   Relationship == other.Relationship;
        }

        public override bool Equals(object? obj)
        {
            return obj is Reference other && Equals(other);
        }

        public override int GetHashCode()
        {
            // NETSTANDARD2_0 lacks HashCode.Combine, so fall back to manual hashing
#if NETSTANDARD2_0
            unchecked
            {
                var hash = ID?.GetHashCode() ?? 0;
                hash = (hash * 397) ^ (Namespace?.GetHashCode() ?? 0);
                hash = (hash * 397) ^ (Relationship?.GetHashCode() ?? 0);
                return hash;
            }
#else
            return HashCode.Combine(ID, Namespace, Relationship);
#endif
        }

        public static bool operator ==(Reference left, Reference right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Reference left, Reference right)
        {
            return !(left == right);
        }
    }
}

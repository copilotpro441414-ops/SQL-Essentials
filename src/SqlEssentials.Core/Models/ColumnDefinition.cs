using System;

namespace SqlEssentials.Core.Models
{
    public sealed class ColumnDefinition : IColumnDefinition
    {
        public ColumnDefinition(
            string name,
            string dataType,
            bool isNullable,
            bool isPrimaryKey,
            bool isForeignKey,
            bool isIdentity,
            bool isComputed,
            int? maxLength,
            int? precision,
            int? scale)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
            IsNullable = isNullable;
            IsPrimaryKey = isPrimaryKey;
            IsForeignKey = isForeignKey;
            IsIdentity = isIdentity;
            IsComputed = isComputed;
            MaxLength = maxLength;
            Precision = precision;
            Scale = scale;
        }

        public string Name { get; }
        public string DataType { get; }
        public bool IsNullable { get; }
        public bool IsPrimaryKey { get; }
        public bool IsForeignKey { get; }
        public bool IsIdentity { get; }
        public bool IsComputed { get; }
        public int? MaxLength { get; }
        public int? Precision { get; }
        public int? Scale { get; }
    }
}

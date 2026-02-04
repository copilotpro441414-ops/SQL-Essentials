using System;

namespace SqlEssentials.Core.Models
{
    public sealed class ParameterDefinition : IParameterDefinition
    {
        public ParameterDefinition(string name, string dataType, ParameterDirection direction, string defaultValue)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
            Direction = direction;
            DefaultValue = defaultValue;
        }

        public string Name { get; }
        public string DataType { get; }
        public ParameterDirection Direction { get; }
        public string DefaultValue { get; }
        public bool IsOutput => Direction == ParameterDirection.Output || Direction == ParameterDirection.InputOutput;
        public bool HasDefault => !string.IsNullOrEmpty(DefaultValue);
    }

    public enum ParameterDirection
    {
        Input,
        Output,
        InputOutput
    }
}

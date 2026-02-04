using System;
using System.Collections.Generic;

namespace SqlEssentials.Core.Models
{
    public sealed class FunctionDefinition : IFunctionDefinition
    {
        private readonly IReadOnlyList<ParameterDefinition> _parameters;

        public FunctionDefinition(
            string schemaName,
            string functionName,
            FunctionType type,
            string returnType,
            IReadOnlyList<ParameterDefinition> parameters)
        {
            SchemaName = string.IsNullOrWhiteSpace(schemaName) ? "dbo" : schemaName;
            FunctionName = functionName ?? throw new ArgumentNullException(nameof(functionName));
            Type = type;
            ReturnType = returnType ?? throw new ArgumentNullException(nameof(returnType));
            _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            FullyQualifiedName = $"[{SchemaName}].[{FunctionName}]";
        }

        public string SchemaName { get; }
        public string FunctionName { get; }
        public string FullyQualifiedName { get; }
        public FunctionType Type { get; }
        public string ReturnType { get; }
        public bool IsTableValued => Type == FunctionType.TableValued || Type == FunctionType.InlineTableValued;
        public IReadOnlyList<ParameterDefinition> Parameters => _parameters;

        IReadOnlyList<IParameterDefinition> IFunctionDefinition.Parameters => _parameters;
    }

    public enum FunctionType
    {
        Scalar,
        TableValued,
        InlineTableValued
    }
}

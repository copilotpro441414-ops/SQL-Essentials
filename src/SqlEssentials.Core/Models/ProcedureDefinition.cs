using System;
using System.Collections.Generic;

namespace SqlEssentials.Core.Models
{
    public sealed class ProcedureDefinition : IProcedureDefinition
    {
        private readonly IReadOnlyList<ParameterDefinition> _parameters;

        public ProcedureDefinition(string schemaName, string procedureName, IReadOnlyList<ParameterDefinition> parameters)
        {
            SchemaName = string.IsNullOrWhiteSpace(schemaName) ? "dbo" : schemaName;
            ProcedureName = procedureName ?? throw new ArgumentNullException(nameof(procedureName));
            _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            FullyQualifiedName = $"[{SchemaName}].[{ProcedureName}]";
        }

        public string SchemaName { get; }
        public string ProcedureName { get; }
        public string FullyQualifiedName { get; }
        public IReadOnlyList<ParameterDefinition> Parameters => _parameters;

        IReadOnlyList<IParameterDefinition> IProcedureDefinition.Parameters => _parameters;
    }
}

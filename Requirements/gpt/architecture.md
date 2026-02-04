# Architecture overview diagram

Below is a high-level architecture intended to be implementable as a Visual Studio–style extension (menus/tool windows/completion providers), with a core engine that remains testable outside SSMS. 



## Why ScriptDOM + SMO are strong building blocks: 
Microsoft’s ScriptDOM produces a T-SQL AST usable for formatting and analysis, and SMO is designed for programmatic access to SQL Server objects and management operations. 
25

## Data flow for completions and analysis
This flow is designed to meet the latency constraints implied by modern completion UX and to support as-you-type analysis without blocking the UI.

``` rust
Keystroke / caret move
   -> (debounce 30–80ms)
       -> read current buffer snapshot
           -> incremental parse attempt
              -> if parse succeeds:
                   update AST + symbol table
                   request metadata if needed (async)
              -> if parse fails/partial:
                   fallback lexical context rules
           -> compute completion candidates (context rules)
           -> score candidates (type weight + prefix + MRU)
           -> return completion list to UI (async)
   -> analyzer pass (light rules only; async)
       -> diagnostics published to Error List panel
This directly aligns with documented behaviors like ranked suggestions tied to prior selections (MRU) and clause completion guidance (JOIN/GROUP BY assistance). 

```

### Core algorithms and pseudocode
Completion ranking (deterministic, explainable)
Inspired by SQL Prompt’s stated ranking factors. 
3
```
score(candidate, context, typedPrefix, userHistory):
    s = 0
    s += typeWeight(candidate.type, context)          // e.g., tables after FROM
    s += prefixSimilarity(candidate.text, typedPrefix) // e.g., startswith bonus
    s += mruBoost(userHistory, candidate.key)          // recency/frequency
    s += contextBoost(candidate, context)              // e.g., FK-join relevance
    return s
```

### JOIN clause generation (FK-first, heuristic fallback)
Directly aligned with dbForge’s documented FK- and column-name-based prompting. 
5


```
suggestJoin(leftTable, rightTable, metadata):
    fkPaths = metadata.findForeignKeysBetween(leftTable, rightTable)
    if fkPaths not empty:
        return buildJoinOnForeignKey(fkPaths.best)
    else:
        // fallback: compare column names & types
        candidates = []
        for colL in leftTable.columns:
            for colR in rightTable.columns:
                if similarName(colL.name, colR.name) and compatibleType(colL, colR):
                    candidates.add(colL = colR)
        return pickBestPredicateSet(candidates)
```

### Rename refactoring (scope-aware symbol rewrite)
Matches the “auto-correction of references” concept, which requires binding occurrences to declarations. 
9


```
renameSymbol(document, symbolId, newName):
    ast, symbols = parseAndBind(document)
    occurrences = symbols.findOccurrences(symbolId)
    edits = []
    for occ in occurrences:
        edits.add(replaceRange(occ.span, newName))
    return applyEditsAtomically(document, edits) // single undo unit
```

### Rule-based analyzer profiles
dbForge’s analyzer profiles are explicitly configurable and stored externally (XML), which provides a practical model: stable rule IDs + profile-driven enablement/severity. 

```
runAnalyzer(document, profile):
    ast, symbols = parseAndBind(document)
    diagnostics = []
    for rule in profile.enabledRules:
        diagnostics += rule.evaluate(ast, symbols, metadataCache)
    return diagnostics
```

## SSMS/Visual Studio extension development references
Because SSMS is increasingly tied to the Visual Studio shell, the Visual Studio extensibility ecosystem provides the best-documented reference model for menus/commands/tool windows and IntelliSense-style extension points. The Visual Studio SDK documentation explicitly includes extending IntelliSense and adding UI elements via packages/commands. 
35

Additionally, Visual Studio documents multiple extensibility models (VSSDK vs Community Toolkit vs VisualStudio.Extensibility), including runtime considerations and version targeting tradeoffs—useful when planning compatibility across evolving SSMS shells. 
40

### Technology stack recommendations
- Language/runtime: C# with the Visual Studio extension model selected based on SSMS version targets; SSMS 21’s Visual Studio 2022 foundation suggests alignment with newer extensibility patterns while maintaining a compatibility layer strategy. 
41
- Parsing/AST: Microsoft ScriptDOM (AST, syntax checking, formatting and analysis use cases). 
25
- Metadata access: SMO (for schema discovery/scriptability) with caching and async refresh. 
42
- Optional deployment tooling: DacFx for project/deployment lifecycle scenarios (future expansion), especially if the analyzer later integrates with database projects. 
43
namespace CastleEscape.Contracts.Patterns;

/// <summary>A class that takes part in a pattern, found by reflection on <c>[DesignPattern]</c>.</summary>
/// <param name="Type">Class or interface name.</param>
/// <param name="Role">Role in the pattern (e.g. ConcreteDecorator).</param>
/// <param name="File">Source file, relative to the repository.</param>
public sealed record PatternParticipantDto(string Type, string Role, string File);

/// <summary>One P1 design pattern as applied in this game.</summary>
/// <param name="Key">URL key, e.g. "abstract-factory".</param>
/// <param name="Name">Pattern name.</param>
/// <param name="Category">Creational, Structural or Behavioral.</param>
/// <param name="Owner">Student responsible for the pattern.</param>
/// <param name="Problem">The problem it solves in this game.</param>
/// <param name="Requirement">The course's pattern-specific requirement.</param>
/// <param name="HowMet">How the requirement is met here.</param>
/// <param name="Doc">Path to the pattern's document in the repository.</param>
/// <param name="Demo">Demo endpoint.</param>
/// <param name="Participants">Classes marked with [DesignPattern] for this pattern.</param>
public sealed record PatternDto(
    string Key,
    string Name,
    string Category,
    string Owner,
    string Problem,
    string Requirement,
    string HowMet,
    string Doc,
    string Demo,
    PatternParticipantDto[] Participants);

/// <summary>Result of running a pattern demo: a readable trace plus structured evidence.</summary>
/// <param name="Pattern">Pattern name.</param>
/// <param name="Summary">One-line conclusion.</param>
/// <param name="Trace">What happened, step by step (the same lines the console demo prints).</param>
/// <param name="Evidence">Values that prove the requirement (identities, counts, addresses...).</param>
public sealed record PatternDemoResponse(
    string Pattern,
    string Summary,
    string[] Trace,
    Dictionary<string, object?> Evidence);

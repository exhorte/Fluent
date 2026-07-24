using System.Globalization;
using Fluent.Core.Dictionary;
using Fluent.Rewrite.Dictionary;

namespace Fluent.Rewrite.Tests;

public sealed class DictionaryExchangeTests
{
    [Fact]
    public void Export_then_import_round_trips_on_an_empty_dictionary()
    {
        PersonalDictionaryEntry[] source =
        [
            new("bonjur", "bonjour"),
            new("cceptation", "acceptation")
        ];

        string exported = DictionaryExchange.Export(source);
        DictionaryImportPlan plan = DictionaryExchange.Plan(
            exported,
            [],
            DictionaryConflictPolicy.SkipExisting);

        Assert.True(plan.Parsed);
        Assert.Null(plan.ParseError);
        Assert.Equal(2, plan.AddedCount);
        Assert.Equal(2, plan.EntriesToUpsert.Count);
        Assert.Contains(new PersonalDictionaryEntry("bonjur", "bonjour"), plan.EntriesToUpsert);
        Assert.Contains(new PersonalDictionaryEntry("cceptation", "acceptation"), plan.EntriesToUpsert);
    }

    [Fact]
    public void Skip_existing_keeps_the_local_entry()
    {
        PersonalDictionaryEntry[] existing = [new("bonjur", "bonjour")];
        string incoming = DictionaryExchange.Export([new("bonjur", "BONJOUR corrigé")]);

        DictionaryImportPlan plan = DictionaryExchange.Plan(
            incoming,
            existing,
            DictionaryConflictPolicy.SkipExisting);

        Assert.Equal(1, plan.SkippedConflictCount);
        Assert.Empty(plan.EntriesToUpsert);
    }

    [Fact]
    public void Overwrite_existing_updates_the_entry()
    {
        PersonalDictionaryEntry[] existing = [new("bonjur", "bonjour")];
        string incoming = DictionaryExchange.Export([new("bonjur", "bonjour corrigé")]);

        DictionaryImportPlan plan = DictionaryExchange.Plan(
            incoming,
            existing,
            DictionaryConflictPolicy.OverwriteExisting);

        Assert.Equal(1, plan.UpdatedCount);
        PersonalDictionaryEntry updated = Assert.Single(plan.EntriesToUpsert);
        Assert.Equal("bonjour corrigé", updated.Replacement);
    }

    [Fact]
    public void Invalid_entries_are_rejected_by_the_existing_validation()
    {
        // Empty replacement, and spoken form equal to replacement: both invalid.
        const string content =
            """
            [
              { "spokenForm": "motvalide", "replacement": "" },
              { "spokenForm": "identique", "replacement": "identique" }
            ]
            """;

        DictionaryImportPlan plan = DictionaryExchange.Plan(
            content,
            [],
            DictionaryConflictPolicy.SkipExisting);

        Assert.True(plan.Parsed);
        Assert.Equal(2, plan.RejectedCount);
        Assert.Empty(plan.EntriesToUpsert);
    }

    [Fact]
    public void Duplicate_spoken_forms_in_the_file_keep_only_the_first()
    {
        const string content =
            """
            [
              { "spokenForm": "mot", "replacement": "premier" },
              { "spokenForm": "MOT", "replacement": "second" }
            ]
            """;

        DictionaryImportPlan plan = DictionaryExchange.Plan(
            content,
            [],
            DictionaryConflictPolicy.SkipExisting);

        Assert.Equal(1, plan.AddedCount);
        Assert.Equal(1, plan.SkippedDuplicateCount);
        PersonalDictionaryEntry added = Assert.Single(plan.EntriesToUpsert);
        Assert.Equal("premier", added.Replacement);
    }

    [Fact]
    public void Import_is_bounded_to_the_dictionary_capacity()
    {
        PersonalDictionaryEntry[] existing = Enumerable.Range(0, PersonalDictionaryLimits.MaximumEntryCount)
            .Select(index => new PersonalDictionaryEntry(
                $"mot{index.ToString("D3", CultureInfo.InvariantCulture)}",
                $"terme{index.ToString("D3", CultureInfo.InvariantCulture)}"))
            .ToArray();

        string incoming = DictionaryExchange.Export([new("nouveau", "remplacement")]);

        DictionaryImportPlan plan = DictionaryExchange.Plan(
            incoming,
            existing,
            DictionaryConflictPolicy.SkipExisting);

        Assert.Equal(1, plan.SkippedCapacityCount);
        Assert.Empty(plan.EntriesToUpsert);
    }

    [Fact]
    public void Invalid_json_produces_an_unparsed_plan()
    {
        DictionaryImportPlan plan = DictionaryExchange.Plan(
            "{ this is not a dictionary",
            [],
            DictionaryConflictPolicy.SkipExisting);

        Assert.False(plan.Parsed);
        Assert.NotNull(plan.ParseError);
        Assert.Empty(plan.EntriesToUpsert);
    }

    [Fact]
    public void Hostile_content_is_preserved_as_data_not_executed()
    {
        const string hostileReplacement = "a'); DROP TABLE personal_dictionary; --";
        string incoming = DictionaryExchange.Export([new("injection", hostileReplacement)]);

        DictionaryImportPlan plan = DictionaryExchange.Plan(
            incoming,
            [],
            DictionaryConflictPolicy.SkipExisting);

        Assert.Equal(1, plan.AddedCount);
        PersonalDictionaryEntry added = Assert.Single(plan.EntriesToUpsert);
        Assert.Equal(hostileReplacement, added.Replacement);
    }
}

namespace Motif.Sketch.Specs;

public sealed class Files
{
    [Fact]
    public async Task Save_then_open_gives_an_identical_file()
    {
        var h = await Harness.WithExample();
        h.Tasks.AddElement(ElementType.SecondaryActor);
        await h.Tasks.SaveAs();
        var saved = h.Projects.Writes[^1].Json;

        h.Projects.NextOpen = h.Projects.FileOf("again.motifsketch.json", saved);
        (await h.Tasks.Open()).Succeeded.Should().BeTrue();
        await h.Tasks.SaveAs();

        h.Projects.Writes[^1].Json.Should().Be(saved);
    }

    [Fact]
    public async Task The_file_is_indented_camelCase_json_and_the_sketch_is_absent_until_started()
    {
        var h = await Harness.Started();
        await h.Tasks.SaveAs();

        var json = h.Projects.Writes[^1].Json;
        json.Should().StartWith("{\n  \"schemaVersion\": 1,\n  \"title\": \"Untitled\",\n  \"contextDiagram\": {");
        json.Should().Contain("\"type\": \"system\"").And.NotContain("\"sketch\"");
    }

    [Fact]
    public async Task In_save_in_place_browsers_Save_writes_back_to_the_same_file()
    {
        var h = await Harness.Started();
        h.Projects.NextOpen = h.Projects.FileOf("shop.motifsketch.json", Example.Json());
        await h.Tasks.Open();

        h.Tasks.AddElement(ElementType.Storage);
        (await h.Tasks.Save()).Succeeded.Should().BeTrue();

        var write = h.Projects.Writes.Should().ContainSingle().Which;
        (write.Name, write.Downloaded).Should().Be(("shop.motifsketch.json", false));
        h.Queries.FileName.Should().Be("shop.motifsketch.json");
        h.Queries.HasUnsavedChanges.Should().BeFalse();
    }

    [Fact]
    public async Task Save_as_picks_a_new_file_and_later_saves_go_there()
    {
        var h = await Harness.Started();
        h.Projects.NextSaveAsName = "mine.motifsketch.json";

        await h.Tasks.SaveAs();
        h.Tasks.AddElement(ElementType.Storage);
        await h.Tasks.Save();

        h.Projects.Writes.Select(w => w.Name).Should().Equal("mine.motifsketch.json", "mine.motifsketch.json");
        h.Projects.Writes.Select(w => w.Handle).Distinct().Should().ContainSingle();
    }

    [Fact]
    public async Task In_other_browsers_Save_downloads_a_copy()
    {
        var h = await Harness.Started();
        h.Projects.CanSaveInPlace = false;
        h.Tasks.RenameProject("Shop");

        await h.Tasks.Save();
        await h.Tasks.Save();

        h.Projects.Writes.Should().HaveCount(2).And.OnlyContain(w => w.Downloaded && w.Name == "Shop.motifsketch.json");
        h.Queries.HasUnsavedChanges.Should().BeFalse();
    }

    [Fact]
    public async Task A_cancelled_save_leaves_the_work_unsaved()
    {
        var h = await Harness.Started();
        h.Tasks.AddElement(ElementType.Storage);
        h.Projects.NextSaveAsName = null;

        (await h.Tasks.Save()).Cancelled.Should().BeTrue();

        h.Queries.HasUnsavedChanges.Should().BeTrue();
    }

    [Fact]
    public async Task A_failed_save_says_why()
    {
        var h = await Harness.Started();
        h.Projects.Fail = true;

        var outcome = await h.Tasks.Save();

        outcome.Succeeded.Should().BeFalse();
        outcome.Problems.Should().ContainSingle().Which.Should().Contain("disk is full");
    }

    [Fact]
    public async Task Edits_mark_unsaved_changes_and_New_starts_clean()
    {
        var h = await Harness.Started();
        h.Queries.HasUnsavedChanges.Should().BeFalse();

        h.Tasks.AddElement(ElementType.Storage);
        h.Queries.HasUnsavedChanges.Should().BeTrue();

        await h.Tasks.NewProject();
        h.Queries.Should().BeEquivalentTo(new { HasUnsavedChanges = false, CanUndo = false, FileName = (string?)null });
    }

    [Fact]
    public async Task A_hand_written_file_without_positions_is_laid_out()
    {
        var h = await Harness.Started();
        h.Projects.NextOpen = h.Projects.FileOf("hand.motifsketch.json", """
            { "schemaVersion": 1, "title": "Hand", "contextDiagram": {
              "elements": [ { "id": "s", "type": "system", "name": "Sys" }, { "id": "a", "type": "primaryActor", "name": "Person" } ],
              "relationships": [ { "id": "r", "from": "a", "to": "s", "label": "Uses" } ] } }
            """);

        (await h.Tasks.Open()).Succeeded.Should().BeTrue();

        h.Element("Person").Box.Right.Should().BeLessThan(h.System.Box.X);
    }

    [Theory]
    [InlineData("not json at all", "can't be read")]
    [InlineData("""{ "schemaVersion": 2, "contextDiagram": { "elements": [] } }""", "schema version 2")]
    [InlineData("""{ "schemaVersion": 1, "contextDiagram": { "elements": [ { "id": "s", "type": "planet", "name": "X" } ] } }""", "unknown type 'planet'")]
    [InlineData("""{ "schemaVersion": 1, "contextDiagram": { "elements": [] } }""", "exactly one system")]
    [InlineData("""{ "schemaVersion": 1, "contextDiagram": { "elements": [ { "id": "s", "type": "system", "name": "A" }, { "id": "s", "type": "storage", "name": "B" } ], "relationships": [ { "id": "r", "from": "s", "to": "s" } ] } }""", "used more than once")]
    [InlineData("""{ "schemaVersion": 1, "contextDiagram": { "elements": [ { "id": "s", "type": "system", "name": "A" } ], "relationships": [ { "id": "r", "from": "s", "to": "ghost" } ] } }""", "isn't an element")]
    [InlineData("""{ "schemaVersion": 1, "contextDiagram": { "elements": [ { "id": "s", "type": "system", "name": "A" }, { "id": "b", "type": "storage", "name": "B" } ], "relationships": [] } }""", "exactly one relationship")]
    [InlineData("""{ "schemaVersion": 1, "contextDiagram": { "elements": [ { "id": "s", "type": "system", "name": "A" }, { "id": "b", "type": "storage", "name": "B" }, { "id": "c", "type": "storage", "name": "C" } ], "relationships": [ { "id": "r1", "from": "s", "to": "b" }, { "id": "r2", "from": "b", "to": "c" } ] } }""", "doesn't have the system at one end")]
    [InlineData("""{ "schemaVersion": 1, "contextDiagram": { "elements": [ { "id": "s", "type": "system", "name": "A" } ] }, "sketch": { "ports": [] } }""", "no User port")]
    [InlineData("""{ "schemaVersion": 1, "contextDiagram": { "elements": [ { "id": "s", "type": "system", "name": "A" } ] }, "sketch": { "ports": [ { "id": "u", "name": "User", "side": "driving", "isUser": true }, { "id": "p", "name": "P", "side": "driven", "adapters": [ { "id": "x", "name": "Specs", "kind": "checks" } ] } ] } }""", "driving ports only")]
    [InlineData("""{ "schemaVersion": 1, "contextDiagram": { "elements": [ { "id": "s", "type": "system", "name": "A" } ] }, "sketch": { "ports": [ { "id": "u", "name": "User", "side": "driving", "isUser": true, "adapters": [ { "id": "x", "name": "X", "kind": "real", "sources": ["nope"] } ] } ] } }""", "isn't an element of the context diagram")]
    [InlineData("""{ "schemaVersion": 1, "contextDiagram": { "elements": [ { "id": "s", "type": "system", "name": "A" }, { "id": "a", "type": "primaryActor", "name": "Ann" } ], "relationships": [ { "id": "r", "from": "a", "to": "s" } ] }, "sketch": { "ports": [ { "id": "u", "name": "User", "side": "driving", "isUser": true, "adapters": [ { "id": "x", "name": "X", "kind": "real", "sources": ["a"] }, { "id": "y", "name": "Y", "kind": "real", "sources": ["a"] } ] } ] } }""", "more than one adapter")]
    public async Task An_invalid_file_is_reported_and_leaves_the_current_work_untouched(string text, string problem)
    {
        var h = await Harness.WithExample();
        var before = h.Queries.ProjectJsonText;
        h.Projects.NextOpen = h.Projects.FileOf("bad.motifsketch.json", text);

        var outcome = await h.Tasks.Open();

        outcome.Succeeded.Should().BeFalse();
        outcome.Problems.Should().Contain(p => p.Contains(problem));
        h.Queries.ProjectJsonText.Should().Be(before);
    }

    [Fact]
    public async Task Every_problem_in_a_file_is_listed()
    {
        var h = await Harness.Started();
        h.Projects.NextOpen = h.Projects.FileOf("bad.motifsketch.json",
            """{ "schemaVersion": 1, "contextDiagram": { "elements": [ { "id": "b", "type": "storage", "name": "" } ] } }""");

        var outcome = await h.Tasks.Open();

        outcome.Problems.Should().HaveCountGreaterThanOrEqualTo(3);
    }
}

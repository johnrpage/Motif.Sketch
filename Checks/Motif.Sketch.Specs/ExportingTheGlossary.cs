namespace Motif.Sketch.Specs;

/// <summary>Export glossary: starting entries from the sketch, merged by Sketch id without touching what people wrote.</summary>
public sealed class ExportingTheGlossary
{
    private static async Task<Harness> Sketched()
    {
        var h = await Harness.Started();
        var dev = h.Tasks.AddElement(ElementType.PrimaryActor);
        h.Tasks.RenameElement(dev, "Developer");
        h.Tasks.DescribeElement(dev, "writes the code");
        var mail = h.Tasks.AddElement(ElementType.ExternalSystem);
        h.Tasks.RenameElement(mail, "Mail");
        var db = h.Tasks.AddElement(ElementType.Storage);
        h.Tasks.RenameElement(db, "Orders");
        h.Tasks.StartSketch();
        h.Tasks.PlaceOnNewPort(mail, Side.Driven);
        h.Tasks.RenamePort(h.Port("Mail").Id, "Mail");
        return h;
    }

    private static string Id(Harness h, string name) => h.Element(name).Id;

    [Fact]
    public async Task A_new_glossary_has_a_proposed_entry_for_each_actor_external_system_storage_and_port()
    {
        var h = await Sketched();

        var outcome = await h.Tasks.ExportGlossary(intoExisting: false);

        outcome.Should().BeEquivalentTo(new { Succeeded = true, FileName = "glossary.md" });
        outcome.Added.Should().Equal("Developer", "Mail", "Orders", "User");
        var md = h.Glossaries.Writes.Single().Markdown;
        md.Should().StartWith("# Glossary\n");
        md.Should().Contain($"## Developer\n\nWrites the code.\n\n- **Kind:** actor\n- **Status:** proposed\n- **Sketch:** `{Id(h, "Developer")}`\n");
        md.Should().Contain($"## Orders\n\n- **Kind:** storage\n- **Status:** proposed\n- **Sketch:** `{Id(h, "Orders")}`\n");
        md.Should().NotContain("## My System", "the system itself isn't one of the kinds Export glossary writes");
    }

    [Fact]
    public async Task An_element_and_the_port_that_reaches_it_share_one_entry_with_two_kinds()
    {
        var h = await Sketched();

        await h.Tasks.ExportGlossary(intoExisting: false);

        h.Glossaries.Writes.Single().Markdown.Should().Contain(
            $"## Mail\n\n- **Kind:** external system, port\n- **Status:** proposed\n- **Sketch:** `{Id(h, "Mail")}` (element), `{h.Port("Mail").Id}` (port)\n");
    }

    [Fact]
    public async Task Merging_adds_only_the_missing_terms_in_alphabetical_place_and_keeps_every_line_people_wrote()
    {
        var h = await Sketched();
        var existing =
            "# Glossary\r\n\r\nOur words.\r\n\r\n" +
            "## Customer\r\n\r\nSomeone who orders.\r\n\r\n- **Kind:** domain concept\r\n\r\n" +
            $"## Dev\r\n\r\nThe person formerly known as Developer.\r\n\r\n- **Kind:** actor\r\n- **Sketch:** `{Id(h, "Developer")}`\r\n\r\n" +
            "## Zebra\r\n\r\nLast.\r\n";
        h.Glossaries.NextOpen = h.Glossaries.FileOf(existing);

        var outcome = await h.Tasks.ExportGlossary(intoExisting: true);

        outcome.Added.Should().Equal("Mail", "Orders", "User");
        outcome.AlreadyThere.Should().Be(1, "Developer was renamed Dev in the glossary, and its Sketch id finds it");
        var write = h.Glossaries.Writes.Single();
        write.Handle.Should().Be(h.Glossaries.NextOpen.Handle, "it's written back to the file it came from");
        var md = write.Markdown;
        md.Should().NotContain("## Developer");
        md.Replace("\r\n", "").Should().NotContain("\n", "the glossary's own line endings are kept");
        Lines(md).Should().ContainInOrder(Lines(existing), "nothing people wrote is changed or moved");
        Headings(md).Should().Equal("Customer", "Dev", "Mail", "Orders", "User", "Zebra");
    }

    [Fact]
    public async Task A_term_with_the_same_name_counts_as_already_there()
    {
        var h = await Sketched();
        h.Glossaries.NextOpen = h.Glossaries.FileOf("# Glossary\n\n## orders\n\nWhat people buy.\n");

        var outcome = await h.Tasks.ExportGlossary(intoExisting: true);

        outcome.Added.Should().NotContain("Orders");
        Headings(h.Glossaries.Writes.Single().Markdown).Should().Equal("Developer", "Mail", "orders", "User");
    }

    [Fact]
    public async Task Terms_after_the_last_heading_go_at_the_end()
    {
        var h = await Sketched();
        h.Glossaries.NextOpen = h.Glossaries.FileOf("# Glossary\n\n## Account\n\nA login.\n");

        await h.Tasks.ExportGlossary(intoExisting: true);

        var md = h.Glossaries.Writes.Single().Markdown;
        md.Should().StartWith("# Glossary\n\n## Account\n\nA login.\n\n## Developer\n");
        md.Should().EndWith("- **Sketch:** `" + h.Port("User").Id + "`\n");
        md.Should().NotContain("\n\n\n");
    }

    [Fact]
    public async Task Headings_inside_a_code_fence_arent_terms()
    {
        var h = await Sketched();
        h.Glossaries.NextOpen = h.Glossaries.FileOf("# Glossary\n\n```markdown\n## Zzz\n```\n\n## Account\n\nA login.\n");

        await h.Tasks.ExportGlossary(intoExisting: true);

        // "Zzz" is example text in the fence: nothing is put in before it, so the fence stays whole.
        Headings(h.Glossaries.Writes.Single().Markdown).Should().Equal("Zzz", "Account", "Developer", "Mail", "Orders", "User");
    }

    [Fact]
    public async Task A_glossary_that_already_has_every_term_isnt_written()
    {
        var h = await Sketched();
        await h.Tasks.ExportGlossary(intoExisting: false);
        h.Glossaries.NextOpen = h.Glossaries.FileOf(h.Glossaries.Writes.Single().Markdown);

        var outcome = await h.Tasks.ExportGlossary(intoExisting: true);

        outcome.Should().BeEquivalentTo(new { Succeeded = true, AlreadyThere = 4 });
        outcome.Added.Should().BeEmpty();
        h.Glossaries.Writes.Should().ContainSingle("the second export changed nothing, so wrote nothing");
    }

    [Fact]
    public async Task Motif_Sketchs_own_glossary_already_has_every_term_in_its_sketch()
    {
        var h = await Harness.Started("#example");
        h.Glossaries.NextOpen = h.Glossaries.FileOf(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Golden", "glossary.md")));

        var outcome = await h.Tasks.ExportGlossary(intoExisting: true);

        outcome.Should().BeEquivalentTo(new { Succeeded = true });
        outcome.Added.Should().BeEmpty();
        h.Glossaries.Writes.Should().BeEmpty();
    }

    [Fact]
    public async Task Read_only_views_can_export_the_glossary_too()
    {
        var h = await Harness.Started("#example");

        var outcome = await h.Tasks.ExportGlossary(intoExisting: false);

        outcome.Succeeded.Should().BeTrue();
        h.Autosave.Stores.Should().Be(0);
    }

    [Fact]
    public async Task Download_only_browsers_get_the_merged_glossary_as_a_download()
    {
        var h = await Sketched();
        h.Glossaries.CanSaveInPlace = false;
        h.Glossaries.NextOpen = h.Glossaries.FileOf("# Glossary\n", "terms.md");

        await h.Tasks.ExportGlossary(intoExisting: true);

        h.Glossaries.Writes.Single().Should().Match<(string Markdown, string Name, string? Handle, bool Downloaded)>(
            w => w.Downloaded && w.Name == "terms.md");
    }

    [Fact]
    public async Task Cancelling_the_picker_writes_nothing()
    {
        var h = await Sketched();
        h.Glossaries.NextOpen = null;

        var outcome = await h.Tasks.ExportGlossary(intoExisting: true);

        outcome.Cancelled.Should().BeTrue();
        h.Glossaries.Writes.Should().BeEmpty();
    }

    [Fact]
    public async Task A_glossary_that_cant_be_opened_says_so()
    {
        var h = await Sketched();
        h.Glossaries.Fail = true;

        var outcome = await h.Tasks.ExportGlossary(intoExisting: true);

        outcome.Problem.Should().Contain("couldn't be opened");
    }

    private static string[] Lines(string md) => md.Replace("\r\n", "\n").Split('\n');

    private static IEnumerable<string> Headings(string md) =>
        Lines(md).Where(l => l.StartsWith("## ", StringComparison.Ordinal)).Select(l => l[3..]);
}

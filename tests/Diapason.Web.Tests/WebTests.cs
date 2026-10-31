using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Resources;
using Diapason.Domain;
using Diapason.Workbooks;
using Diapason.Workbooks.Tests;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Diapason.Web.Tests;

// Spec 001, criteria 10 and 12 to 14.
public class WebTests
{
    private static WebApplication Build(string folder, int port = 5188) =>
        DiapasonApp.Build([$"--Diapason:DataFolder={folder}", $"--Diapason:Port={port}", "--environment=Production"]);

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    [Fact]
    public async Task The_app_answers_on_loopback_and_not_on_the_network_addresses_of_the_machine()
    {
        using var temp = new TempFolder();
        var port = FreePort();
        await using var app = Build(temp.Path, port);

        await app.StartAsync(CancellationToken.None);
        try
        {
            Assert.True(await Connects(IPAddress.Loopback, port));
            var network = (await Dns.GetHostAddressesAsync(Dns.GetHostName()))
                .Where(a => a.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a));
            foreach (var address in network)
                Assert.False(await Connects(address, port), address.ToString());
        }
        finally
        {
            await app.StopAsync(CancellationToken.None);
        }
    }

    private static async Task<bool> Connects(IPAddress address, int port)
    {
        using var client = new TcpClient(address.AddressFamily);
        try
        {
            await client.ConnectAsync(address, port).WaitAsync(TimeSpan.FromSeconds(2));
            return true;
        }
        catch (Exception e) when (e is SocketException or TimeoutException)
        {
            return false;
        }
    }

    [Fact]
    public void The_environment_variable_overrides_the_data_folder()
    {
        using var temp = new TempFolder();
        var app = DiapasonApp.Build([$"--Diapason:DataFolder=elsewhere", $"--{DiapasonOptions.DataFolderVariable}={temp.Path}"]);

        Assert.Equal(temp.Path, app.Services.GetRequiredService<IOptions<DiapasonOptions>>().Value.DataFolder);
    }

    [Fact]
    public void Reload_reads_the_folder_again()
    {
        using var temp = new TempFolder();
        var state = Build(temp.Path).Services.GetRequiredService<DataState>();
        var changed = 0;
        state.Changed += () => changed++;

        Assert.Equal(IssueCode.RegistryMissing, Assert.Single(state.Current.Issues).Code);
        Generators.WriteSampleData(temp.Path);
        Assert.Null(state.Current.Overview);

        var reloaded = state.Reload();

        Assert.Equal(2, reloaded.Overview!.Engagements.Count);
        Assert.Same(reloaded, state.Current);
        Assert.Equal(1, changed);
    }

    [Fact]
    public void Italian_is_the_default_and_English_the_other_language()
    {
        var options = Build(".").Services.GetRequiredService<IOptions<Microsoft.AspNetCore.Builder.RequestLocalizationOptions>>().Value;

        Assert.Equal("it", options.DefaultRequestCulture.UICulture.Name);
        Assert.Equal(["it", "en"], options.SupportedUICultures!.Select(c => c.Name));
    }

    [Fact]
    public void Both_languages_have_every_text_and_every_issue_has_a_message()
    {
        var resources = new ResourceManager("Diapason.Web.Resources.Texts", typeof(Texts).Assembly);
        var english = Keys(resources.GetResourceSet(CultureInfo.InvariantCulture, true, false)!);
        var italian = Keys(resources.GetResourceSet(new CultureInfo("it"), true, false)!);

        Assert.Equal(english, italian);
        Assert.Empty(Enum.GetNames<IssueCode>().Select(c => $"Issue_{c}").Except(english));
    }

    [Fact]
    public void Numbers_are_in_the_Italian_format()
    {
        Assert.Equal("3.840 €", Show.Money(3_840m));
        Assert.Equal("20,0 %", Show.Share(0.2m));
        Assert.Equal("1,25", Show.Ratio(1.25m));
        Assert.Equal("–", Show.Ratio(null));
    }

    private static SortedSet<string> Keys(ResourceSet set) =>
        new(set.Cast<System.Collections.DictionaryEntry>().Select(e => (string)e.Key));
}

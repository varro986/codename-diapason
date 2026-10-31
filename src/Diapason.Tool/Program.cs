using Diapason.Workbooks;

// Usage: Diapason.Tool templates <folder>    empty registry and plan, in the layout of specs/data-contract.md
//        Diapason.Tool sample-data <folder>  synthetic registry and plans, for tests and development
// Neither ever overwrites a file: if one already exists, nothing is written.

const string Usage = "Usage: Diapason.Tool templates|sample-data <folder>";

if (args is not [var command, var folder])
{
    Console.Error.WriteLine(Usage);
    return 2;
}

Func<string, IReadOnlyList<string>>? write = command switch
{
    "templates" => Generators.WriteTemplates,
    "sample-data" => Generators.WriteSampleData,
    _ => null,
};
if (write is null)
{
    Console.Error.WriteLine($"Unknown command '{command}'. {Usage}");
    return 2;
}

try
{
    var written = write(folder);
    foreach (var path in written) Console.WriteLine($"written: {path}");
    Console.WriteLine($"{command}: {written.Count} files written to {Path.GetFullPath(folder)}");
    return 0;
}
catch (IOException e)
{
    Console.Error.WriteLine($"{command}: {e.Message}. Choose an empty folder or move those files first.");
    return 1;
}

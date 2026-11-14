using ModularPipelines;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Options;

namespace Initialization.Microsoft.Extensions.DependencyInjection.Pipeline.Modules;

public class RunUnitTestsModule : Module<List<CommandResult>>
{
    protected override async Task<List<CommandResult>> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var results = new List<CommandResult>();
        var git = await context.Tools.Git.Information.GetRequiredInfoAsync(cancellationToken);

        foreach (var unitTestProjectFile in git.Root
                     .GetFiles(file => file.Path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                                       && file.Path.Contains("UnitTests", StringComparison.OrdinalIgnoreCase)))
        {
            results.Add(await context.Tools.DotNet.TestVstestAsync(new DotNetTestVstestOptions
            {
                ProjectSolution = unitTestProjectFile.Path,
            }, new CommandExecutionOptions
            {
                Logging = new CommandLoggingOptions
                {
                    Verbosity = CommandLogVerbosity.InputOnly,
                    ShowStandardError = true,
                },
            }, cancellationToken));
        }

        return results;
    }
}

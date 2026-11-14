using ModularPipelines;
using ModularPipelines.Context;

namespace Initialization.Microsoft.Extensions.DependencyInjection.Pipeline.Modules;

public class PackageFilesRemovalModule : Module<None>
{
    protected override async Task<None> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var git = await context.Tools.Git.Information.GetRequiredInfoAsync(cancellationToken);
        var packageFiles = git.Root.GetFiles(path => path.Extension is ".nupkg");

        foreach (var packageFile in packageFiles)
        {
            packageFile.Delete();
        }

        return None.Value;
    }
}

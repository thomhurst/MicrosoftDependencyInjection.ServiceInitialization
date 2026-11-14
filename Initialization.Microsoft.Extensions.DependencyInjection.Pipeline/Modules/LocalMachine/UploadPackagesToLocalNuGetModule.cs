using Microsoft.Extensions.Logging;
using ModularPipelines;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;

namespace Initialization.Microsoft.Extensions.DependencyInjection.Pipeline.Modules.LocalMachine;

[DependsOn<AddLocalNugetSourceModule>]
[DependsOn<PackagePathsParserModule>]
[DependsOn<CreateLocalNugetFolderModule>]
public class UploadPackagesToLocalNuGetModule : Module<CommandResult[]>
{
    protected override async Task OnBeforeExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var packagePaths = await context.GetModule<PackagePathsParserModule>();
        foreach (var packagePath in packagePaths.Value)
        {
            context.Logger.LogInformation("[Local Directory] Uploading {File}", packagePath);
        }

        await base.OnBeforeExecuteAsync(context, cancellationToken);
    }

    protected override async Task<CommandResult[]> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var localRepoLocation = await context.GetModule<CreateLocalNugetFolderModule>();
        var packagePaths = await context.GetModule<PackagePathsParserModule>();
        var results = new List<CommandResult>();
        foreach (var file in packagePaths.Value)
        {
            results.Add(await context.Tools.DotNet
                .NuGet
                .PushAsync(new DotNetNuGetPushOptions([file.Path])
                {
                    Source = localRepoLocation.Value.Path
                }, cancellationToken: cancellationToken));
        }

        return results.ToArray();
    }
}

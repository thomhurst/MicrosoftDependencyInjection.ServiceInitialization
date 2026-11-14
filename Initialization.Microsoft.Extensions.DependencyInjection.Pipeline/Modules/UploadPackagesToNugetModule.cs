using Initialization.Microsoft.Extensions.DependencyInjection.Pipeline.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModularPipelines;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Options;

namespace Initialization.Microsoft.Extensions.DependencyInjection.Pipeline.Modules;

[DependsOn<RunUnitTestsModule>]
[DependsOn<PackagePathsParserModule>]
public class UploadPackagesToNugetModule : Module<CommandResult[]>
{
    private readonly IOptions<NuGetSettings> _options;

    public UploadPackagesToNugetModule(IOptions<NuGetSettings> options)
    {
        _options = options;
    }

    protected override async Task OnBeforeExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var packagePaths = await context.GetModule<PackagePathsParserModule>();

        foreach (var packagePath in packagePaths.Value)
        {
            context.Logger.LogInformation("Uploading {File}", packagePath);
        }

        await base.OnBeforeExecuteAsync(context, cancellationToken);
    }

    protected override void Configure(ModuleConfigurationBuilder module) => module
        .WithSkipWhen(async (context, cancellationToken) =>
        {
            var gitVersionInfo = await context.Tools.Git.Versioning.GetVersioningInformationAsync(cancellationToken);

            if (gitVersionInfo.BranchName != "main")
            {
                return true;
            }

            var publishPackages =
                context.Environment.Variables.Get("PUBLISH_PACKAGES")!;

            if (!bool.TryParse(publishPackages, out var shouldPublishPackages) || !shouldPublishPackages)
            {
                return true;
            }

            return false;
        });

    protected override async Task<CommandResult[]> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(_options.Value.ApiKey);

        var gitVersionInformation = await context.Tools.Git.Versioning.GetVersioningInformationAsync(cancellationToken);

        if (gitVersionInformation.BranchName != "main")
        {
            return [];
        }

        var packagePaths = await context.GetModule<PackagePathsParserModule>();

        var results = new List<CommandResult>();
        foreach (var file in packagePaths.Value)
        {
            results.Add(await context.Tools.DotNet
                .NuGet
                .PushAsync(new DotNetNuGetPushOptions([file.Path])
                {
                    Source = "https://api.nuget.org/v3/index.json",
                    ApiKey = _options.Value.ApiKey!
                }, cancellationToken: cancellationToken));
        }

        return results.ToArray();
    }
}

#addin "nuget:?package=Cake.Git&version=0.19.0"
#addin "nuget:?package=Cake.ReSharperReports&version=0.10.0"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var configuration = Argument("configuration", "Debug");
var revision = EnvironmentVariable("BUILD_NUMBER") ?? Argument("revision", "9999");
var target = Argument("target", "Default");


//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define git commit id
var commitId = "SNAPSHOT";

// Define product name and version
var product = "Viveport";
var companyName = "HTC";
var version = "1.7.1";
var semanticVersion = string.Format("{0}.{1}", version, revision);
var ciVersion = string.Format("{0}.{1}", version, "0");

// Define copyright
var copyright = string.Format("Copyright © 2016 - {0}", DateTime.Now.Year);

// Define timestamp for signing
var lastSignTimestamp = DateTime.Now;
var signIntervalInMilli = 1000 * 5;

// Define path
var solutionFile = File(string.Format("./source/{0}.sln", product));

// Define directories.
var distDir = Directory("./dist");
var tempDir = Directory("./temp");
var generatedDir = Directory("./source/generated");
var packagesDir = Directory("./source/packages");
var nugetDir = distDir + Directory(configuration) + Directory("nuget");
var homeDir = Directory(EnvironmentVariable("USERPROFILE") ?? EnvironmentVariable("HOME"));
var reportReSharperDupFinder = distDir + Directory(configuration) + Directory("report/ReSharper/DupFinder");
var reportReSharperInspectCode = distDir + Directory(configuration) + Directory("report/ReSharper/InspectCode");

// Define signing key, password and timestamp server
var signKeyEnc00 = EnvironmentVariable("SIGNKEYENC00");
var signKeyEnc01 = EnvironmentVariable("SIGNKEYENC01");
var signKeyEnc02 = EnvironmentVariable("SIGNKEYENC02");
var signKeyEnc03 = EnvironmentVariable("SIGNKEYENC03");
var signKeyEnc04 = EnvironmentVariable("SIGNKEYENC04");
var signKeyEnc05 = EnvironmentVariable("SIGNKEYENC05");
var signKeyEnc06 = EnvironmentVariable("SIGNKEYENC06");
var signKeyEnc07 = EnvironmentVariable("SIGNKEYENC07");
var signKeyEnc = EnvironmentVariable("SIGNKEYENC") ?? signKeyEnc00 + signKeyEnc01 + signKeyEnc02 + signKeyEnc03 + signKeyEnc04 + signKeyEnc05 + signKeyEnc06 + signKeyEnc07 ?? "NOTSET";
var signPass = EnvironmentVariable("SIGNPASS") ?? "NOTSET";
var signSha1Uri = new Uri("http://timestamp.digicert.com");
var signSha256Uri = new Uri("http://timestamp.digicert.com");

// Define nuget push source and key
var nugetApiKey = EnvironmentVariable("NUGET_PUSH_TOKEN") ?? EnvironmentVariable("NUGET_APIKEY") ?? "NOTSET";
var nugetSource = EnvironmentVariable("NUGET_PUSH_PATH") ?? EnvironmentVariable("NUGET_SOURCE") ?? "NOTSET";


//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Fetch-Git-Commit-ID")
    .ContinueOnError()
    .Does(() =>
{
    var lastCommit = GitLogTip(MakeAbsolute(Directory(".")));
    commitId = lastCommit.Sha;
});

Task("Display-Config")
    .IsDependentOn("Fetch-Git-Commit-ID")
    .Does(() =>
{
    Information("Build target: {0}", target);
    Information("Build configuration: {0}", configuration);
    Information("Build commitId: {0}", commitId);
    if ("Release".Equals(configuration))
    {
        Information("Build version: {0}", semanticVersion);
    }
    else
    {
        Information("Build version: {0}-CI{1}", ciVersion, revision);
    }
});

Task("Clean-Workspace")
    .IsDependentOn("Display-Config")
    .Does(() =>
{
    CleanDirectory(distDir);
    CleanDirectory(tempDir);
    CleanDirectory(generatedDir);
    CleanDirectory(packagesDir);
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean-Workspace")
    .Does(() =>
{
    NuGetRestore(string.Format("./source/{0}.sln", product));
});

Task("Generate-AssemblyInfo")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    CreateDirectory(generatedDir);
    var file = "./source/generated/SharedAssemblyInfo.cs";
    var assemblyVersion = semanticVersion;
    if (!"Release".Equals(configuration))
    {
        assemblyVersion = ciVersion;
    }
    CreateAssemblyInfo(
            file,
            new AssemblyInfoSettings
            {
                    Company = companyName,
                    Copyright = copyright,
                    Product = string.Format("{0} : {1}", product, commitId),
                    Version = version,
                    FileVersion = assemblyVersion,
                    InformationalVersion = assemblyVersion
            }
    );
});

Task("Build-Assemblies")
    .IsDependentOn("Generate-AssemblyInfo")
    .Does(() =>
{
    var settings = new DotNetCoreBuildSettings
    {
            Configuration = configuration
    };
    DotNetCoreBuild("./source/", settings);
});

Task("Run-DupFinder")
    .IsDependentOn("Build-Assemblies")
    .Does(() =>
{
    if(IsRunningOnWindows())
    {
        DupFinder(
                string.Format("./source/{0}.sln", product),
                new DupFinderSettings()
                {
                        ShowStats = true,
                        ShowText = true,
                        OutputFile = new FilePath(reportReSharperDupFinder.ToString() + "/" + product + ".xml"),
                        ThrowExceptionOnFindingDuplicates = false
                }
        );
        ReSharperReports(
                new FilePath(reportReSharperDupFinder.ToString() + "/" + product + ".xml"),
                new FilePath(reportReSharperDupFinder.ToString() + "/" + product + ".html")
        );
    }
});

Task("Run-InspectCode")
    .IsDependentOn("Run-DupFinder")
    .Does(() =>
{
    if(IsRunningOnWindows())
    {
        InspectCode(
                string.Format("./source/{0}.sln", product),
                new InspectCodeSettings()
                {
                        SolutionWideAnalysis = true,
                        OutputFile = new FilePath(reportReSharperInspectCode.ToString() + "/" + product + ".xml"),
                        ThrowExceptionOnFindingViolations = false
                }
        );
        ReSharperReports(
                new FilePath(reportReSharperInspectCode.ToString() + "/" + product + ".xml"),
                new FilePath(reportReSharperInspectCode.ToString() + "/" + product + ".html")
        );
    }
});

Task("Sign-Assemblies")
    .WithCriteria(() => "Release".Equals(configuration) && !"NOTSET".Equals(signPass) && !"NOTSET".Equals(signKeyEnc))
    .IsDependentOn("Run-InspectCode")
    .Does(() =>
{
    var currentSignTimestamp = DateTime.Now;
    Information("Last timestamp:    " + lastSignTimestamp);
    Information("Current timestamp: " + currentSignTimestamp);
    var totalTimeInMilli = (DateTime.Now - lastSignTimestamp).TotalMilliseconds;

    var signKey = "./temp/key.pfx";
    System.IO.File.WriteAllBytes(signKey, Convert.FromBase64String(signKeyEnc));

    var file = string.Format("./temp/{0}/{1}/bin/net45/{1}.dll", configuration, product);

    if (totalTimeInMilli < signIntervalInMilli)
    {
        System.Threading.Thread.Sleep(signIntervalInMilli - (int)totalTimeInMilli);
    }
    Sign(
            file,
            new SignToolSignSettings
            {
                    TimeStampUri = signSha1Uri,
                    CertPath = signKey,
                    Password = signPass
            }
    );
    lastSignTimestamp = DateTime.Now;

    System.Threading.Thread.Sleep(signIntervalInMilli);
    Sign(
            file,
            new SignToolSignSettings
            {
                    AppendSignature = true,
                    TimeStampUri = signSha256Uri,
                    DigestAlgorithm = SignToolDigestAlgorithm.Sha256,
                    TimeStampDigestAlgorithm = SignToolDigestAlgorithm.Sha256,
                    CertPath = signKey,
                    Password = signPass
            }
    );
    lastSignTimestamp = DateTime.Now;

    file = string.Format("./temp/{0}/{1}/bin/netstandard2.0/{1}.dll", configuration, product);

    if (totalTimeInMilli < signIntervalInMilli)
    {
        System.Threading.Thread.Sleep(signIntervalInMilli - (int)totalTimeInMilli);
    }
    Sign(
            file,
            new SignToolSignSettings
            {
                    TimeStampUri = signSha1Uri,
                    CertPath = signKey,
                    Password = signPass
            }
    );
    lastSignTimestamp = DateTime.Now;

    System.Threading.Thread.Sleep(signIntervalInMilli);
    Sign(
            file,
            new SignToolSignSettings
            {
                    AppendSignature = true,
                    TimeStampUri = signSha256Uri,
                    DigestAlgorithm = SignToolDigestAlgorithm.Sha256,
                    TimeStampDigestAlgorithm = SignToolDigestAlgorithm.Sha256,
                    CertPath = signKey,
                    Password = signPass
            }
    );
    lastSignTimestamp = DateTime.Now;
});

Task("Build-NuGet-Package")
    .IsDependentOn("Sign-Assemblies")
    .Does(() =>
{
    CreateDirectory(nugetDir);
    var nugetPackVersion = semanticVersion;
    if (!"Release".Equals(configuration))
    {
        nugetPackVersion = string.Format("{0}-CI{1}", ciVersion, revision);
    }
    Information("Pack version: {0}", nugetPackVersion);
    var settings = new DotNetCorePackSettings
    {
            Configuration = configuration,
            OutputDirectory = nugetDir,
            NoBuild = true,
            ArgumentCustomization = (args) =>
            {
                    return args.Append("/p:Version={0}", nugetPackVersion);
            }
    };

    DotNetCorePack("./source/" + product + "/", settings);
});

Task("Publish-NuGet-Package")
    .WithCriteria(() => "Release".Equals(configuration) && !"NOTSET".Equals(nugetApiKey) && !"NOTSET".Equals(nugetSource))
    .IsDependentOn("Build-NuGet-Package")
    .Does(() =>
{
    var nugetPushVersion = semanticVersion;
    if (!"Release".Equals(configuration))
    {
        nugetPushVersion = string.Format("{0}-CI{1}", ciVersion, revision);
    }
    Information("Publish version: {0}", nugetPushVersion);
    var package = string.Format("./dist/{0}/nuget/{1}.{2}.nupkg", configuration, product, nugetPushVersion);
    NuGetPush(
            package,
            new NuGetPushSettings
            {
                    Source = nugetSource,
                    ApiKey = nugetApiKey
            }
    );
});


//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Build-NuGet-Package");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);

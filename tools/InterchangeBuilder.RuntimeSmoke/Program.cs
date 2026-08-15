using System;
using System.IO;
using System.Reflection;

namespace InterchangeBuilder.RuntimeSmoke;

internal static class Program
{
    private static string _artifactDirectory = string.Empty;
    private static string _managedDirectory = string.Empty;

    private static int Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("Usage: InterchangeBuilder.RuntimeSmoke <artifact-directory> <Cities2 managed-directory>");
            return 2;
        }

        _artifactDirectory = Path.GetFullPath(args[0]);
        _managedDirectory = Path.GetFullPath(args[1]);
        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

        try
        {
            Assembly.LoadFrom(Path.Combine(_artifactDirectory, "0Harmony.dll"));
            Assembly.LoadFrom(Path.Combine(_artifactDirectory, "InterchangeBuilder.LaneConnections.Core.dll"));
            Assembly.LoadFrom(Path.Combine(_artifactDirectory, "InterchangeBuilder.dll"));
            Assembly upgrade = Assembly.LoadFrom(Path.Combine(_artifactDirectory, "InterchangeBuilder.LaneConnections.dll"));
            Type bootstrap = upgrade.GetType("InterchangeBuilder.LaneConnections.Bootstrap", throwOnError: true)
                ?? throw new TypeLoadException("InterchangeBuilder.LaneConnections.Bootstrap");
            MethodInfo validateTargets = bootstrap.GetMethod("ValidateTargets", BindingFlags.Public | BindingFlags.Static)
                ?? throw new MissingMethodException(bootstrap.FullName, "ValidateTargets");
            string result = validateTargets.Invoke(null, null) as string
                ?? throw new InvalidOperationException("Target validation did not return a result.");

            Console.WriteLine("Runtime metadata smoke test passed: " + result + ".");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 5;
        }
    }

    private static Assembly? ResolveAssembly(object? sender, ResolveEventArgs args)
    {
        string fileName = new AssemblyName(args.Name).Name + ".dll";
        string artifactCandidate = Path.Combine(_artifactDirectory, fileName);
        if (File.Exists(artifactCandidate))
        {
            return Assembly.LoadFrom(artifactCandidate);
        }

        string managedCandidate = Path.Combine(_managedDirectory, fileName);
        return File.Exists(managedCandidate) ? Assembly.LoadFrom(managedCandidate) : null;
    }
}

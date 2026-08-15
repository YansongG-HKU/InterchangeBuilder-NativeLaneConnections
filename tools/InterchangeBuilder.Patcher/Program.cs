using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

if (args.Length != 3)
{
    Console.Error.WriteLine("Usage: InterchangeBuilder.Patcher <original.dll> <upgrade.dll> <output-directory>");
    return 2;
}

string originalPath = Path.GetFullPath(args[0]);
string upgradePath = Path.GetFullPath(args[1]);
string outputDirectory = Path.GetFullPath(args[2]);
if (!File.Exists(originalPath) || !File.Exists(upgradePath))
{
    Console.Error.WriteLine("Both the original mod assembly and upgrade assembly must exist.");
    return 3;
}

Directory.CreateDirectory(outputDirectory);
var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(Path.GetDirectoryName(originalPath)!);
resolver.AddSearchDirectory(Path.GetDirectoryName(upgradePath)!);

bool readSymbols = File.Exists(Path.ChangeExtension(originalPath, ".pdb"));
var readerParameters = new ReaderParameters
{
    AssemblyResolver = resolver,
    ReadSymbols = readSymbols,
    SymbolReaderProvider = readSymbols ? new PortablePdbReaderProvider() : null
};

using AssemblyDefinition target = AssemblyDefinition.ReadAssembly(originalPath, readerParameters);
using AssemblyDefinition upgrade = AssemblyDefinition.ReadAssembly(upgradePath);
TypeDefinition bootstrap = upgrade.MainModule.Types.Single(type =>
    type.FullName == "InterchangeBuilder.LaneConnections.Bootstrap");
MethodReference install = target.MainModule.ImportReference(
    bootstrap.Methods.Single(method => method.Name == "Install"));
MethodReference uninstall = target.MainModule.ImportReference(
    bootstrap.Methods.Single(method => method.Name == "Uninstall"));

TypeDefinition mod = target.MainModule.Types.Single(type => type.FullName == "InterchangeBuilder.Mod");
MethodDefinition onLoad = mod.Methods.Single(method => method.Name == "OnLoad");
MethodDefinition onDispose = mod.Methods.Single(method => method.Name == "OnDispose");
InjectAtStart(onLoad, install, loadUpdateSystem: true);
InjectAtStart(onDispose, uninstall, loadUpdateSystem: false);

string outputAssembly = Path.Combine(outputDirectory, Path.GetFileName(originalPath));
var writerParameters = new WriterParameters
{
    WriteSymbols = readSymbols,
    SymbolWriterProvider = readSymbols ? new PortablePdbWriterProvider() : null
};
target.Write(outputAssembly, writerParameters);
Console.WriteLine(outputAssembly);
return 0;

static void InjectAtStart(MethodDefinition method, MethodReference call, bool loadUpdateSystem)
{
    if (method.Body.Instructions.Any(instruction =>
            instruction.OpCode == OpCodes.Call &&
            instruction.Operand is MethodReference existing &&
            existing.FullName == call.FullName))
    {
        return;
    }

    ILProcessor processor = method.Body.GetILProcessor();
    Instruction first = method.Body.Instructions[0];
    Instruction callInstruction = processor.Create(OpCodes.Call, call);
    processor.InsertBefore(first, callInstruction);
    if (loadUpdateSystem)
    {
        processor.InsertBefore(callInstruction, processor.Create(OpCodes.Ldarg_1));
    }
}

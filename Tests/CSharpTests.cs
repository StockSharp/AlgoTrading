namespace StockSharp.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.Compilation;
using Ecng.Reflection;
using Ecng.UnitTesting;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.BusinessEntities;
using StockSharp.Algo.Compilation;
using StockSharp.Algo.Strategies;

[TestClass]
public partial class CSharpTests : BaseTestClass
{
	/// <summary>
	/// Run a strategy that is compiled into this assembly. Used by the hand-written tests, which
	/// need the concrete type to reach the strategy's own parameters.
	/// </summary>
	public static Task RunStrategy<T>(CancellationToken cancellationToken, Action<T, Security> extra = null, TimeSpan? replayDuration = null, TimeSpan? postTradeHorizon = null, bool requireTrades = true)
		where T : Strategy
		=> AsmInit.RunStrategy(TypeHelper.CreateInstance<T>(typeof(T)), cancellationToken, extra, postTradeHorizon, replayDuration, requireTrades);

	/// <summary>
	/// Run a strategy straight from its source file. Keeping the examples out of this compilation
	/// is what makes the build cheap, and the Python half has always worked this way.
	/// </summary>
	public static async Task RunStrategy(string filePath, CancellationToken cancellationToken, Action<Strategy, Security> extra = null, TimeSpan? replayDuration = null, TimeSpan? postTradeHorizon = null, bool requireTrades = true)
	{
		var strategyPath = Path.Combine("../../../../API/", filePath);

		using var code = new CodeInfo
		{
			Name = Path.GetFileNameWithoutExtension(strategyPath),
			Text = File.ReadAllText(strategyPath),
			Language = FileExts.CSharp,
		};

		ResolveLocalReferences(code);

		var errors = await code.CompileAsync(t => t.IsRequiredType<Strategy>(), code.Name, cancellationToken);

		foreach (var err in errors.ErrorsOnly())
			throw new InvalidOperationException($"{filePath}: {err}");

		var strategy = code.ObjectType.CreateInstance<Strategy>();

		await AsmInit.RunStrategy(strategy, cancellationToken, extra, postTradeHorizon, replayDuration, requireTrades);
	}

	/// <summary>
	/// The default references name assemblies without a path, and the ones this repository needs
	/// sit next to the test binaries rather than in the runtime directory.
	/// </summary>
	private static void ResolveLocalReferences(CodeInfo code)
	{
		foreach (var reference in code.AssemblyReferences)
		{
			if (Path.IsPathRooted(reference.FileName))
				continue;

			var localPath = Path.Combine(AppContext.BaseDirectory, reference.FileName);

			if (File.Exists(localPath))
				reference.FileName = localPath;
		}
	}

	public static string RowName(MethodInfo method, object[] data)
		=> StrategyInventory.RowName(method, data);

	public static IEnumerable<object[]> Shard00() => StrategyInventory.Shard(".cs", 0);
	public static IEnumerable<object[]> Shard01() => StrategyInventory.Shard(".cs", 1);
	public static IEnumerable<object[]> Shard02() => StrategyInventory.Shard(".cs", 2);
	public static IEnumerable<object[]> Shard03() => StrategyInventory.Shard(".cs", 3);
	public static IEnumerable<object[]> Shard04() => StrategyInventory.Shard(".cs", 4);
	public static IEnumerable<object[]> Shard05() => StrategyInventory.Shard(".cs", 5);
	public static IEnumerable<object[]> Shard06() => StrategyInventory.Shard(".cs", 6);
	public static IEnumerable<object[]> Shard07() => StrategyInventory.Shard(".cs", 7);

	[TestMethod, TestCategory("Shard00")]
	[DynamicData(nameof(Shard00), DynamicDataDisplayName = nameof(RowName))]
	public Task Strategies00(string path, string name) => RunStrategy(path, CancellationToken);

	[TestMethod, TestCategory("Shard01")]
	[DynamicData(nameof(Shard01), DynamicDataDisplayName = nameof(RowName))]
	public Task Strategies01(string path, string name) => RunStrategy(path, CancellationToken);

	[TestMethod, TestCategory("Shard02")]
	[DynamicData(nameof(Shard02), DynamicDataDisplayName = nameof(RowName))]
	public Task Strategies02(string path, string name) => RunStrategy(path, CancellationToken);

	[TestMethod, TestCategory("Shard03")]
	[DynamicData(nameof(Shard03), DynamicDataDisplayName = nameof(RowName))]
	public Task Strategies03(string path, string name) => RunStrategy(path, CancellationToken);

	[TestMethod, TestCategory("Shard04")]
	[DynamicData(nameof(Shard04), DynamicDataDisplayName = nameof(RowName))]
	public Task Strategies04(string path, string name) => RunStrategy(path, CancellationToken);

	[TestMethod, TestCategory("Shard05")]
	[DynamicData(nameof(Shard05), DynamicDataDisplayName = nameof(RowName))]
	public Task Strategies05(string path, string name) => RunStrategy(path, CancellationToken);

	[TestMethod, TestCategory("Shard06")]
	[DynamicData(nameof(Shard06), DynamicDataDisplayName = nameof(RowName))]
	public Task Strategies06(string path, string name) => RunStrategy(path, CancellationToken);

	[TestMethod, TestCategory("Shard07")]
	[DynamicData(nameof(Shard07), DynamicDataDisplayName = nameof(RowName))]
	public Task Strategies07(string path, string name) => RunStrategy(path, CancellationToken);

	/// <summary>
	/// Run one example on demand, for digging into a failure. It carries no shard, so no CI job
	/// selects it; pass the path it should run as a run parameter:
	/// dotnet test --filter "FullyQualifiedName~CSharpTests.Debug" -- TestRunParameters.Parameter(name="strategy",value="0001-0100/0002_NDay_Breakout/CS/NdayBreakoutStrategy.cs")
	/// </summary>
	[TestMethod, TestCategory("Manual")]
	public async Task Debug()
	{
		var path = TestContext.Properties.TryGetValue("strategy", out var value) ? value as string : null;

		if (path.IsEmpty())
			Inconclusive("Pass the example to run as TestRunParameters.Parameter(name=\"strategy\", value=\"<path under API/>\").");

		await RunStrategy(path, CancellationToken);
	}
}

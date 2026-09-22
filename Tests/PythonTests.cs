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

using StockSharp.Algo.Compilation;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;

[TestClass]
public partial class PythonTests : BaseTestClass
{
	public static async Task RunStrategy(string filePath, CancellationToken cancellationToken, Action<Strategy, Security> extra = null, TimeSpan? replayDuration = null, TimeSpan? postTradeHorizon = null, bool requireTrades = true)
	{
		var strategyPath = Path.Combine("../../../../API/", filePath);

		var code = new CodeInfo
		{
			Name = Path.GetFileNameWithoutExtension(strategyPath),
			Text = File.ReadAllText(strategyPath),
			Language = FileExts.Python,
		};

		var errors = await code.CompileAsync(t => t.IsRequiredType<Strategy>(), code.Name, cancellationToken);

		foreach (var err in errors.ErrorsOnly())
			throw new InvalidOperationException(err.ToString());

		var strategy = code.ObjectType.CreateInstance<Strategy>();

		await AsmInit.RunStrategy(strategy, cancellationToken, extra, postTradeHorizon, replayDuration, requireTrades);
	}

	public static string RowName(MethodInfo method, object[] data)
		=> StrategyInventory.RowName(method, data);

	public static IEnumerable<object[]> Shard00() => StrategyInventory.Shard(".py", 0);
	public static IEnumerable<object[]> Shard01() => StrategyInventory.Shard(".py", 1);
	public static IEnumerable<object[]> Shard02() => StrategyInventory.Shard(".py", 2);
	public static IEnumerable<object[]> Shard03() => StrategyInventory.Shard(".py", 3);
	public static IEnumerable<object[]> Shard04() => StrategyInventory.Shard(".py", 4);
	public static IEnumerable<object[]> Shard05() => StrategyInventory.Shard(".py", 5);
	public static IEnumerable<object[]> Shard06() => StrategyInventory.Shard(".py", 6);
	public static IEnumerable<object[]> Shard07() => StrategyInventory.Shard(".py", 7);

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
	/// dotnet test --filter "FullyQualifiedName~PythonTests.Debug" -- TestRunParameters.Parameter(name="strategy",value="0001-0100/0002_NDay_Breakout/PY/nday_breakout_strategy.py")
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

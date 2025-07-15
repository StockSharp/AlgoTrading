namespace StockSharp.Tests;

using System.IO;

using Ecng.Compilation;
using Ecng.Reflection;

using StockSharp.Algo.Compilation;

[TestClass]
public class PythonTests
{
	public static async Task RunStrategy(string filePath)
	{
		var strategyPath = Path.Combine("../../../../API/", filePath);

		var code = new CodeInfo
		{
			Name = Path.GetFileNameWithoutExtension(strategyPath),
			Text = File.ReadAllText(strategyPath),
			Language = FileExts.Python,
		};

		var errors = await code.CompileAsync(t => t.IsRequiredType<Strategy>(), code.Name, default);

		foreach (var err in errors.ErrorsOnly())
			throw new InvalidOperationException(err.ToString());

		var strategy = code.ObjectType.CreateInstance<Strategy>();

		await AsmInit.RunStrategy(strategy);
	}

	[TestMethod]
	public Task MaCrossoverStrategyTest()
		=> RunStrategy("0001_MA_CrossOver/ma_crossover_strategy.py");
}
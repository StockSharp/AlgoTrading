namespace StockSharp.Tests;

using System;
using System.IO;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.Compilation;
using Ecng.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Algo.Compilation;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;

[TestClass]
public partial class PythonTests
{
	public static async Task RunStrategy(string filePath, Action<Strategy, Security> extra = null)
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

		await AsmInit.RunStrategy(strategy, extra);
	}
}

namespace StockSharp.Tests;

using System;
using System.Linq;

using Ecng.UnitTesting;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class StrategyInventoryTests : BaseTestClass
{
	[TestMethod]
	[TestCategory("Inventory")]
	[TestCategory("Shard00")]
	public void BothLanguagesAndAllShardsUseTheSameSnapshot()
	{
		var csharp = StrategyInventory.Enumerate(".cs").ToArray();
		var python = StrategyInventory.Enumerate(".py").ToArray();
		IsTrue(csharp.Length > 3000, "The snapshot must include the whole API catalogue, not just the selected test filter.");
		AreEqual(csharp.Length, python.Length);
		IsTrue(csharp.Select(example => example.Key).SequenceEqual(python.Select(example => example.Key)), "Both languages must cover identical folder keys.");
		AreEqual(csharp.Length, csharp.Select(example => example.Key).Distinct(StringComparer.Ordinal).Count());
		IsTrue(csharp.Select(example => example.Id).SequenceEqual(csharp.Select(example => example.Id).OrderBy(id => id)));

		foreach (var extension in new[] { ".cs", ".py" })
		{
			var expected = StrategyInventory.Enumerate(extension).Where(example => !StrategyInventory.Overrides.Contains(example.Key)).ToArray();
			var rows = Enumerable.Range(0, StrategyInventory.ShardCount)
				.SelectMany(shard => StrategyInventory.Shard(extension, shard))
				.ToArray();
			AreEqual(expected.Length, rows.Length, "Shard discovery must neither lose nor duplicate snapshot entries.");
			IsTrue(expected.Select(example => example.RelativePath).OrderBy(path => path, StringComparer.Ordinal)
				.SequenceEqual(rows.Select(row => (string)row[0]).OrderBy(path => path, StringComparer.Ordinal)));
		}
	}
}

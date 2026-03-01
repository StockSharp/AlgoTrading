namespace StockSharp.Tests;

using System;
using System.Threading.Tasks;

using Ecng.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.BusinessEntities;
using StockSharp.Algo.Strategies;
using StockSharp.Samples.Strategies;

[TestClass]
public partial class CSharpTests
{
	public static Task RunStrategy<T>(Action<T, Security> extra = null)
		where T : Strategy
		=> AsmInit.RunStrategy(TypeHelper.CreateInstance<T>(typeof(T)), extra);
}

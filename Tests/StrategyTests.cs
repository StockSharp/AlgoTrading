namespace StockSharp.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.UnitTesting;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Tests written once for both versions of an example: <see cref="CSharpTests"/> runs every test declared here
/// against the C# source of the example and <see cref="PythonTests"/> against the Python one.
/// </summary>
public abstract partial class StrategyTests : BaseTestClass
{
	/// <summary>
	/// Extension of the example sources this class runs.
	/// </summary>
	protected abstract string Extension { get; }

	/// <summary>
	/// Whether this class runs the Python versions of the examples.
	/// </summary>
	protected bool IsPython => Extension == ".py";

	/// <summary>
	/// Run an example from its source file over the packaged history.
	/// </summary>
	/// <param name="filePath">Path of the source file under API/.</param>
	/// <param name="setup">Prepares the strategy before it starts; the second argument is the other packaged instrument.</param>
	/// <param name="replayDuration">How much of the packaged history to replay; all of it when not set.</param>
	/// <param name="postTradeHorizon">How long the replay continues after the last trade; the harness default when not set.</param>
	/// <returns>The run.</returns>
	protected abstract Task RunFile(string filePath, Action<Strategy, Security> setup, TimeSpan? replayDuration, TimeSpan? postTradeHorizon);

	/// <summary>
	/// Run the example with the folder key <paramref name="key"/>, such as <c>0068_CCI_Divergence</c>, in this class's language.
	/// </summary>
	/// <param name="key">Folder key of the example.</param>
	/// <param name="setup">Prepares the strategy before it starts; the second argument is the other packaged instrument.</param>
	/// <param name="replayDuration">How much of the packaged history to replay; all of it when not set.</param>
	/// <param name="postTradeHorizon">How long the replay continues after the last trade; the harness default when not set.</param>
	/// <returns>The run.</returns>
	protected Task Replay(string key, Action<Strategy, Security> setup, TimeSpan? replayDuration = null, TimeSpan? postTradeHorizon = null)
		=> RunFile(StrategyInventory.GetFile(key, Extension), setup, replayDuration, postTradeHorizon);

	/// <summary>
	/// Set a parameter of an example, converting the value to the type the example declared the parameter with:
	/// the Python versions declare fractional parameters as floats, the C# versions as decimals.
	/// </summary>
	/// <param name="strategy">The example.</param>
	/// <param name="name">Parameter name.</param>
	/// <param name="value">New value.</param>
	protected static void SetParam(Strategy strategy, string name, object value)
	{
		if (!strategy.Parameters.TryGetValue(name, out var param))
			throw new InvalidOperationException($"Parameter '{name}' not found. Available: {string.Join(", ", strategy.Parameters.CachedKeys)}");

		param.Value = value is IConvertible && param.Value is IConvertible current && current.GetType() != value.GetType()
			? value.To(current.GetType())
			: value;
	}

	/// <summary>
	/// Run one example on demand, for digging into a failure. It carries no shard, so no CI job selects it; pass the
	/// source file of the language to run as a run parameter:
	/// dotnet test --filter "FullyQualifiedName~CSharpTests.Debug" -- TestRunParameters.Parameter(name="strategy",value="0001-0100/0002_NDay_Breakout/CS/NdayBreakoutStrategy.cs")
	/// dotnet test --filter "FullyQualifiedName~PythonTests.Debug" -- TestRunParameters.Parameter(name="strategy",value="0001-0100/0002_NDay_Breakout/PY/nday_breakout_strategy.py")
	/// </summary>
	/// <returns>The run.</returns>
	[TestMethod, TestCategory("Manual")]
	public async Task Debug()
	{
		var path = TestContext.Properties.TryGetValue("strategy", out var value) ? value as string : null;

		if (path.IsEmpty())
			Inconclusive("Pass the example to run as TestRunParameters.Parameter(name=\"strategy\", value=\"<path under API/>\").");

		await RunFile(path, null, null, null);
	}

	/// <summary>
	/// Check that the published percent stop of an example is real native protection: set tight, it has to flatten a
	/// position before the next finished signal candle.
	/// </summary>
	/// <param name="key">Folder key of the example.</param>
	/// <param name="duration">How much history to replay; a week when not set.</param>
	/// <param name="useSecondarySecurity">Run on the other packaged instrument.</param>
	/// <param name="setup">Additional preparation of the strategy.</param>
	/// <param name="expectedFrame">Published candle time frame; five minutes when not set.</param>
	/// <param name="expectedStopPercent">Published stop percent.</param>
	/// <returns>The check.</returns>
	protected async Task CheckPercentStopBetweenBars(string key, TimeSpan? duration = null, bool useSecondarySecurity = false, Action<Strategy, Security> setup = null, TimeSpan? expectedFrame = null, decimal expectedStopPercent = 2m)
	{
		DateTime? entryTime = null;
		var intrabarStops = 0;
		var entries = 0;
		var finishedBars = 0;
		var entryBar = 0;
		var stopOrders = new HashSet<Order>();
		var nativeStop = typeof(Strategy).GetField("_stopLoss", BindingFlags.Instance | BindingFlags.NonPublic);
		await Replay(key, (strategy, secondary) =>
		{
			if (useSecondarySecurity) strategy.Security = secondary;
			setup?.Invoke(strategy, secondary);
			IsTrue(strategy.Parameters.TryGetValue("StopLossPercent", out var stop), "The published percent stop must actually exist.");
			AreEqual(expectedStopPercent, Convert.ToDecimal(stop.Value));
			AreEqual((expectedFrame ?? TimeSpan.FromMinutes(5)).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "StopLossPercent", 0.00001m);
			strategy.CandleReceived += (_, candle) =>
			{
				// An external stream can finish the same time slot after the primary one.
				// Count actual primary signal bars, not callbacks from another security.
				if (candle.State == CandleStates.Finished && candle.SecurityId == strategy.Security.Id.ToSecurityId()) finishedBars++;
			};
			strategy.OrderRegistering += order =>
			{
				if (strategy.Position == 0m)
				{
					entries++;
					entryTime = strategy.CurrentTime;
					entryBar = finishedBars;
					var unit = (Unit)nativeStop.GetValue(strategy);
					IsNotNull(unit, "The parameter must enable actual native protection, not remain unused metadata.");
					AreEqual(UnitTypes.Percent, unit.Type);
					AreEqual(0.00001m, unit.Value);
				}
				else if (entryTime is not null && finishedBars == entryBar && strategy.CurrentTime < entryTime.Value.AddMinutes(4))
				{
					// Exclude the next bar's close even if an entry acknowledgement has a small timestamp delay.
					stopOrders.Add(order);
					AreEqual(Math.Abs(strategy.Position), order.Volume);
					AreEqual(strategy.Position > 0m ? Sides.Sell : Sides.Buy, order.Side);
				}
			};
			strategy.Trades.TradeAdded += trade =>
			{
				if (stopOrders.Contains(trade.Order) && strategy.Position == 0m)
				{
					AreEqual(entryBar, finishedBars, "The stop must actually fill before another finished signal bar.");
					IsTrue(strategy.CurrentTime < entryTime.Value.AddMinutes(4));
					intrabarStops++;
				}
			};
		}, duration ?? TimeSpan.FromDays(7));
		IsTrue(entries > 0 && intrabarStops > 0,
			"The real archive must contain an actual tight-stop flatten fill before another finished 5m candle can produce a neutral or opposite signal.");
	}

	/// <summary>
	/// Check that an example's ATR stop freezes the independently calculated Wilder ATR distance at every entry and
	/// that changing the multiplier changes real executions.
	/// </summary>
	/// <param name="key">Folder key of the example.</param>
	/// <param name="entryComment">Comment of the example's entry orders.</param>
	/// <param name="optionalAtr">The ATR stop is switched off by default and enabled through UseAtrStop.</param>
	/// <param name="signalSetup">Additional preparation of the strategy.</param>
	/// <param name="stopParameter">Name of the ATR multiplier parameter.</param>
	/// <param name="atrPeriodParameter">Name of the ATR period parameter.</param>
	/// <returns>The check.</returns>
	protected async Task CheckAtrDistancesAndRiskParameterChangeRealExecutions(string key, string entryComment, bool optionalAtr = false, Action<Strategy> signalSetup = null, string stopParameter = "AtrMultiplier", string atrPeriodParameter = "AtrPeriod")
	{
		async Task<OrderTraceRecorder> Run(decimal multiplier)
		{
			var trace = new OrderTraceRecorder();
			var count = 0;
			var atr = 0m;
			decimal? previousClose = null;
			var distances = new Dictionary<Order, decimal>();
			var checkedFills = 0;
			DateTime? entryFillTime = null;
			var entryBar = 0;
			var intrabarStopFills = 0;
			var stopField = typeof(Strategy).GetField("_stopLoss", BindingFlags.Instance | BindingFlags.NonPublic);
			await Replay(key, (strategy, _) =>
			{
				IsTrue(strategy.Parameters.ContainsKey(stopParameter));
				AreEqual(2m, Convert.ToDecimal(strategy.Parameters[stopParameter].Value));
				AreEqual(14, strategy.Parameters[atrPeriodParameter].Value);
				signalSetup?.Invoke(strategy);
				if (optionalAtr)
				{
					IsTrue(strategy.Parameters.TryGetValue("UseAtrStop", out var enabled));
					AreEqual(false, enabled.Value);
					enabled.Value = true;
				}
				SetParam(strategy, stopParameter, multiplier);
				strategy.CandleReceived += (_, candle) =>
				{
					if (candle.State != CandleStates.Finished) return;
					var tr = previousClose is decimal close
						? Math.Max(candle.HighPrice - candle.LowPrice, Math.Max(Math.Abs(candle.HighPrice - close), Math.Abs(candle.LowPrice - close)))
						: candle.HighPrice - candle.LowPrice;
					count++;
					var length = Math.Min(count, 14);
					atr = (atr * (length - 1) + tr) / length;
					previousClose = candle.ClosePrice;
				};
				strategy.OrderRegistering += order =>
				{
					if (order.Comment == entryComment) distances.Add(order, atr * multiplier);
				};
				strategy.Trades.TradeAdded += trade =>
				{
					if (multiplier > 0m && multiplier < 0.001m && !distances.ContainsKey(trade.Order)
						&& strategy.Position == 0m && entryFillTime is not null && count == entryBar
						&& strategy.CurrentTime < entryFillTime.Value.AddMinutes(4))
						intrabarStopFills++;
					if (multiplier <= 0m || !distances.TryGetValue(trade.Order, out var expected)
						|| strategy.Position == 0m || (strategy.Position > 0m) != (trade.Order.Side == Sides.Buy)) return;
					var stop = (Unit)stopField.GetValue(strategy);
					IsNotNull(stop, "The actual native ATR protection must be active after the entry fill.");
					AreEqual(UnitTypes.Absolute, stop.Type);
					IsTrue(Math.Abs(expected - stop.Value) < 0.00000001m, "Every entry/reversal must freeze its own independently calculated Wilder ATR distance, not a current-price self-comparison or first-entry-only setting.");
					checkedFills++;
					entryFillTime = strategy.CurrentTime;
					entryBar = count;
				};
				trace.Attach(strategy);
			}, TimeSpan.FromDays(31));
			if (multiplier > 0m) IsTrue(checkedFills > 3, $"Need repeated actual ATR entry-distance checks: multiplier={multiplier}, checkedFills={checkedFills}.");
			if (multiplier > 0m && multiplier < 0.001m)
				IsTrue(intrabarStopFills > 0, "A tight native ATR stop must actually flatten before the next finished signal candle, not just change bar-close order traces.");
			return trace;
		}
		var unprotected = await Run(0m);
		var published = await Run(2m);
		var tight = await Run(0.00001m);
		published.AssertDiffersFrom(unprotected, "The published ATR stop must affect actual trading, not remain metadata.");
		tight.AssertDiffersFrom(published, $"Changing {stopParameter} must change real executions.");
	}
}

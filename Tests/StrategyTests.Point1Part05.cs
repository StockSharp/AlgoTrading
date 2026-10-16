namespace StockSharp.Tests;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

using Ecng.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Selected independent real-replay contract checks for Point 1 / Part 05.
/// The local environment lacks the .NET SDK / StockSharp sibling checkout,
/// so these tests still require execution with BOTH language runners.
/// </summary>
public abstract partial class StrategyTests
{
	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_ColorZerolagRvi_DisabledShortEntriesOnlyCloseLongs()
	{
		var buyEntries = 0;
		var longCloses = 0;

		await Replay("1996_Color_Zerolag_RVI", (strategy, _) =>
		{
			foreach (var name in new[] { "BuyOpen", "BuyClose", "SellOpen", "SellClose" })
			{
				IsTrue(strategy.Parameters.TryGetValue(name, out var parameter), $"Missing {name} in {Extension}.");
				AreEqual(true, parameter.Value, $"The documented {name} default must be enabled.");
			}

			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "RviLength", 5);
			// StockSharp RVI signal uses four fixed taps; Length < 4 reads outside its buffer.
			SetParam(strategy, "SignalLength", 4);
			SetParam(strategy, "SellOpen", false);

			strategy.OrderRegistering += order =>
			{
				if (order.Side == Sides.Sell)
				{
					IsTrue(strategy.Position > 0, "SellOpen=false must not create a short entry.");
					AreEqual(strategy.Position, order.Volume, "Closing a long must use its full executed volume.");
					longCloses++;
				}
				else if (order.Side == Sides.Buy && strategy.Position == 0)
				{
					AreEqual(strategy.Volume, order.Volume, "Long entry must use the configured order volume.");
					buyEntries++;
				}
			};
		}, replayDuration: TimeSpan.FromDays(14));

		IsTrue(buyEntries > 0 && longCloses > 0,
			"The archive must demonstrate real entries and exits, not just present parameters.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_RandomTrader_AbsoluteProtectionConfiguredOnEntry()
	{
		var stopField = typeof(Strategy).GetField("_stopLoss", BindingFlags.Instance | BindingFlags.NonPublic);
		IsNotNull(stopField, "StockSharp protection internals changed; update the assertion rather than skipping it.");
		var entries = 0;

		await Replay("2012_Random_Trader", (strategy, _) =>
		{
			AreEqual(10m, Convert.ToDecimal(strategy.Parameters["StopLoss"].Value));
			AreEqual(10m, Convert.ToDecimal(strategy.Parameters["TakeProfit"].Value));
			SetParam(strategy, "Cooldown", 3);

			strategy.OrderRegistering += order =>
			{
				if (strategy.Position != 0)
					return;

				var stop = (Unit)stopField.GetValue(strategy);
				IsNotNull(stop, "The example must configure actual native protection before entry.");
				AreEqual(UnitTypes.Absolute, stop.Type);
				AreEqual(10m, stop.Value);
				AreEqual(strategy.Volume, order.Volume);
				entries++;
			};
		}, replayDuration: TimeSpan.FromDays(7));

		IsTrue(entries > 0, "Require at least one actual entry from the packaged archive.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_ColorJVariation_AbsoluteStopIsLive()
	{
		var stopField = typeof(Strategy).GetField("_stopLoss", BindingFlags.Instance | BindingFlags.NonPublic);
		IsNotNull(stopField, "StockSharp protection internals changed; update the assertion rather than skipping it.");
		var entries = 0;

		await Replay("1990_Color_J_Variation", (strategy, _) =>
		{
			AreEqual(1000m, Convert.ToDecimal(strategy.Parameters["StopLoss"].Value));
			AreEqual(2000m, Convert.ToDecimal(strategy.Parameters["TakeProfit"].Value));
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "JmaPeriod", 3);

			strategy.OrderRegistering += order =>
			{
				if (strategy.Position != 0)
					return;

				var stop = (Unit)stopField.GetValue(strategy);
				IsNotNull(stop, "The documented stop must be active after startup.");
				AreEqual(UnitTypes.Absolute, stop.Type);
				AreEqual(1000m, stop.Value);
				entries++;
			};
		}, replayDuration: TimeSpan.FromDays(7));

		IsTrue(entries > 0, "Require real slope-reversal entries in the packaged archive.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_ExpMovingAverageFn_AbsoluteProtectionAndPositionVolume()
	{
		var stopField = typeof(Strategy).GetField("_stopLoss", BindingFlags.Instance | BindingFlags.NonPublic);
		var takeField = typeof(Strategy).GetField("_takeProfit", BindingFlags.Instance | BindingFlags.NonPublic);
		IsNotNull(stopField, "Do not skip protection if the StockSharp API changes.");
		IsNotNull(takeField, "Do not skip protection if the StockSharp API changes.");
		var entries = 0;

		await Replay("1992_Exp_MovingAverage_FN", (strategy, _) =>
		{
			AreEqual(1000m, Convert.ToDecimal(strategy.Parameters["StopLoss"].Value));
			AreEqual(2000m, Convert.ToDecimal(strategy.Parameters["TakeProfit"].Value));
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "Length", 3);

			strategy.OrderRegistering += order =>
			{
				if (strategy.Position == 0)
				{
					var stop = (Unit)stopField.GetValue(strategy);
					var take = (Unit)takeField.GetValue(strategy);
					IsNotNull(stop, "Native stop must be activated before an entry.");
					IsNotNull(take, "Native take profit must be activated before an entry.");
					AreEqual(UnitTypes.Absolute, stop.Type);
					AreEqual(UnitTypes.Absolute, take.Type);
					AreEqual(1000m, stop.Value);
					AreEqual(2000m, take.Value);
					AreEqual(strategy.Volume, order.Volume);
					entries++;
				}
				else if (order.Side == (strategy.Position > 0m ? Sides.Sell : Sides.Buy))
				{
					IsTrue(order.Volume <= Math.Abs(strategy.Position) + strategy.Volume,
						"No oversized reversing order should be placed.");
				}
			};
		}, replayDuration: TimeSpan.FromDays(7));

		IsTrue(entries > 0, "Archive must produce a real slope-turn entry.");
	}


	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_ColorStochNr_AllFiveModesProduceDistinctTrades()
	{
		var signatures = new HashSet<string>();
		for (var modeIndex = 0; modeIndex < 5; modeIndex++)
		{
			var mode = modeIndex;
			var orders = new List<string>();
			await Replay("2009_Color_Stoch_NR", (strategy, _) =>
			{
				var published = strategy.Parameters["Mode"];
				AreEqual(3, Convert.ToInt32(published.Value), "OscDisposition must be the default in both C# and Python.");
				AreEqual(2m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
				AreEqual(2m, Convert.ToDecimal(strategy.Parameters["TakeProfitPercent"].Value));
				SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
				var selectedMode = published.Value.GetType().IsEnum
					? Enum.ToObject(published.Value.GetType(), mode)
					: (object)mode;
				published.Value = selectedMode;
				strategy.OrderRegistering += order =>
				{
					IsTrue(order.Volume > 0m);
					if (strategy.Position != 0m &&
						order.Side == (strategy.Position > 0m ? Sides.Sell : Sides.Buy))
						IsTrue(order.Volume == Math.Abs(strategy.Position) || order.Volume == strategy.Volume,
							"Every reversal must cover the filled position or open only the configured volume.");
					orders.Add($"{strategy.CurrentTime:O}/{order.Side}/{order.Volume}");
				};
			}, replayDuration: TimeSpan.FromDays(14));
			IsTrue(orders.Count > 0, $"Require actual archive-backed orders from stochastic mode {mode}.");
			signatures.Add(string.Join(";", orders));
		}
		IsTrue(signatures.Count >= 3, "Distinct stochastic signal modes must not silently all run OscDisposition.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_GoCandleBodyReversal_DocumentedDefaultsAndActualOrders()
	{
		var buys = 0;
		var sells = 0;
		await Replay("2220_Go_Candle_Body_Reversal", (strategy, _) =>
		{
			AreEqual(174, Convert.ToInt32(strategy.Parameters["Period"].Value));
			AreEqual(TimeSpan.FromHours(1).TimeFrame(), strategy.Parameters["CandleType"].Value);
			// The archive must verify original defaults separately; the smaller profile is for
			// actual signal/execution checks and MUST NOT be represented as the default.
			SetParam(strategy, "Period", 5);
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m);
				if (order.Side == Sides.Buy)
					buys++;
				else
					sells++;
			};
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(buys > 0 && sells > 0,
			"Both directions of the completed candle-body SMA reversal must place real orders.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_CmRsi_OpeningPriceSignalVolumeAndNativeProtection()
	{
		var entries = 0;
		var actualFills = 0;
		var stopField = typeof(Strategy).GetField("_stopLoss", BindingFlags.Instance | BindingFlags.NonPublic);
		var takeField = typeof(Strategy).GetField("_takeProfit", BindingFlags.Instance | BindingFlags.NonPublic);
		IsNotNull(stopField);
		IsNotNull(takeField);
		await Replay("2320_Cm_RSI", (strategy, _) =>
		{
			AreEqual(14, Convert.ToInt32(strategy.Parameters["RsiPeriod"].Value));
			AreEqual(30m, Convert.ToDecimal(strategy.Parameters["BuyLevel"].Value));
			AreEqual(70m, Convert.ToDecimal(strategy.Parameters["SellLevel"].Value));
			AreEqual(200, Convert.ToInt32(strategy.Parameters["TakeProfit"].Value));
			AreEqual(100, Convert.ToInt32(strategy.Parameters["StopLoss"].Value));
			AreEqual(0.1m, Convert.ToDecimal(strategy.Parameters["OrderVolume"].Value));
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "RsiPeriod", 3);
			SetParam(strategy, "BuyLevel", 45m);
			SetParam(strategy, "SellLevel", 55m);
			SetParam(strategy, "OrderVolume", 1m);
			strategy.OrderRegistering += order =>
			{
				if (strategy.Position != 0m)
					return;
				var stop = (Unit)stopField.GetValue(strategy);
				var take = (Unit)takeField.GetValue(strategy);
				IsNotNull(stop);
				IsNotNull(take);
				AreEqual(UnitTypes.Absolute, stop.Type);
				AreEqual(UnitTypes.Absolute, take.Type);
				AreEqual(100m * strategy.Security.PriceStep, stop.Value);
				AreEqual(200m * strategy.Security.PriceStep, take.Value);
				AreEqual(1m, order.Volume, "Every new entry must use OrderVolume, not Strategy.Volume.");
				entries++;
			};
			strategy.Trades.TradeAdded += _ => actualFills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(entries > 0 && actualFills > 0,
			"The reachable open-price RSI profile must create and fill real orders in the archive.");
	}
	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_VininITrendLrma_BothModesAndFullPositionReversal()
	{
		for (var modeIndex = 0; modeIndex < 2; modeIndex++)
		{
			var selected = modeIndex;
			var requests = 0;
			var fills = 0;
			await Replay("2050_VininI_Trend_LRMA", (strategy, _) =>
			{
				AreEqual(13, Convert.ToInt32(strategy.Parameters["Period"].Value));
				AreEqual(10m, Convert.ToDecimal(strategy.Parameters["UpLevel"].Value));
				AreEqual(-10m, Convert.ToDecimal(strategy.Parameters["DnLevel"].Value));
				AreEqual(0, Convert.ToInt32(strategy.Parameters["Mode"].Value));
				SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
				SetParam(strategy, "Period", 5);
				// A reachable demonstration profile is separate from default ±10% thresholds.
				SetParam(strategy, "UpLevel", 0.1m);
				SetParam(strategy, "DnLevel", -0.1m);
				var setting = strategy.Parameters["Mode"];
				setting.Value = setting.Value.GetType().IsEnum
					? Enum.ToObject(setting.Value.GetType(), selected)
					: (object)selected;
				strategy.OrderRegistering += order =>
				{
					IsTrue(order.Volume > 0m);
					if (strategy.Position != 0m &&
						order.Side == (strategy.Position > 0m ? Sides.Sell : Sides.Buy))
						IsTrue(order.Volume == Math.Abs(strategy.Position) || order.Volume == strategy.Volume,
							"Opposite position must close at executed volume before re-entry.");
					requests++;
				};
				strategy.Trades.TradeAdded += _ => fills++;
			}, replayDuration: TimeSpan.FromDays(14));
			IsTrue(requests > 0 && fills > 0,
				$"The reachability profile for LRMA mode {selected} must place and fill real trades.");
		}
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_FisherOrgSign_DocumentedLevelsAndFilledEntries()
	{
		var entries = 0;
		var fills = 0;
		await Replay("2292_Fisher_Org_Sign", (strategy, _) =>
		{
			AreEqual(7, Convert.ToInt32(strategy.Parameters["Length"].Value));
			AreEqual(1.5m, Convert.ToDecimal(strategy.Parameters["UpLevel"].Value));
			AreEqual(-1.5m, Convert.ToDecimal(strategy.Parameters["DownLevel"].Value));
			SetParam(strategy, "Length", 5);
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			// Reachable explicit profile; do not claim this verifies default thresholds.
			SetParam(strategy, "UpLevel", 0.1m);
			SetParam(strategy, "DownLevel", -0.1m);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m);
				if (strategy.Position != 0m &&
					order.Side == (strategy.Position > 0m ? Sides.Sell : Sides.Buy))
					IsTrue(order.Volume == Math.Abs(strategy.Position) || order.Volume == strategy.Volume,
						"Reverse must close the actual filled volume and open at configured Volume.");
				entries++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(entries > 0 && fills > 0, "Fisher level crosses must result in real filled orders.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_ColorStepXccx_NativeSteppedOscillatorAndActualFills()
	{
		var buys = 0;
		var sells = 0;
		var fills = 0;
		await Replay("1987_Color_Step_Xccx", (strategy, _) =>
		{
			AreEqual(30, Convert.ToInt32(strategy.Parameters["DPeriod"].Value));
			AreEqual(7, Convert.ToInt32(strategy.Parameters["MPeriod"].Value));
			AreEqual(5m, Convert.ToDecimal(strategy.Parameters["StepSizeFast"].Value));
			AreEqual(30m, Convert.ToDecimal(strategy.Parameters["StepSizeSlow"].Value));
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			// Override warm-up for replay reachability; default parameter values are tested above.
			SetParam(strategy, "DPeriod", 5);
			SetParam(strategy, "MPeriod", 3);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m);
				if (strategy.Position != 0m &&
					order.Side == (strategy.Position > 0m ? Sides.Sell : Sides.Buy))
					IsTrue(order.Volume == Math.Abs(strategy.Position) || order.Volume == strategy.Volume,
						"Reversal must close the actual filled position first.");
				if (order.Side == Sides.Buy) buys++;
				else sells++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(buys > 0 && sells > 0 && fills > 0,
			"Both normalized step-line crossing directions must submit and fill real orders.");
	}


	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_MavaXonax_FinishedCrossMustProduceActualExecutions()
	{
		var entries = 0;
		var fills = 0;
		await Replay("2013_Mava_Xonax", (strategy, _) =>
		{
			AreEqual(6, Convert.ToInt32(strategy.Parameters["EmaPeriod"].Value));
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "EmaPeriod", 3); // Reachable replay profile, not the published default.
			SetParam(strategy, "SignalCooldownBars", 1);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m, "MAVA orders must always have positive volume.");
				if (strategy.Position == 0m)
				{
					AreEqual(strategy.Volume, order.Volume,
						"New position must use configured strategy volume.");
					entries++;
				}
				else if (order.Side == (strategy.Position > 0m ? Sides.Sell : Sides.Buy))
				{
					AreEqual(Math.Abs(strategy.Position), order.Volume,
						"Risk exit must close the entire actual filled position.");
				}
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(entries > 0 && fills > 0,
			"MAVA must demonstrate registered entries and real exchange fills, not only indicator signals.");
	}


	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_SuperTake_RealFillsAndStopExitVolume()
	{
		var entries = 0;
		var exits = 0;
		var fills = 0;
		var firstEntryIsLong = false;
		await Replay("1988_SuperTake", (strategy, _) =>
		{
			AreEqual(10m, Convert.ToDecimal(strategy.Parameters["TakeProfit"].Value));
			AreEqual(15m, Convert.ToDecimal(strategy.Parameters["StopLoss"].Value));
			AreEqual(1.8m, Convert.ToDecimal(strategy.Parameters["MartinFactor"].Value));
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			// The original defaults are checked above; the tighter profile makes
			// quote-driven take/stop executions observable in the packaged archive.
			SetParam(strategy, "TakeProfit", 0.1m);
			SetParam(strategy, "StopLoss", 0.1m);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m);
				AreEqual(OrderTypes.Market, order.Type);
				if (strategy.Position == 0m)
				{
					if (entries == 0) firstEntryIsLong = order.Side == Sides.Buy;
					AreEqual(strategy.Volume, order.Volume);
					entries++;
				}
				else if (order.Side == (strategy.Position > 0 ? Sides.Sell : Sides.Buy))
				{
					AreEqual(Math.Abs(strategy.Position), order.Volume,
						"A protective exit must close the entire filled position.");
					exits++;
				}
			};
			strategy.Trades.TradeAdded += trade =>
			{
				IsTrue(trade.Trade.Volume > 0m);
				fills++;
			};
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(firstEntryIsLong, "The first SuperTake entry must be long.");
		IsTrue(entries > 0 && exits > 0 && fills >= 2,
			"The BTC/TON archive must confirm actual entry and stop/take fills.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_ExpCyclePeriod_EhlersSignalPermissionsAndRealFills()
	{
		var stopField = typeof(Strategy).GetField("_stopLoss", BindingFlags.Instance | BindingFlags.NonPublic);
		var takeField = typeof(Strategy).GetField("_takeProfit", BindingFlags.Instance | BindingFlags.NonPublic);
		IsNotNull(stopField);
		IsNotNull(takeField);
		var buys = 0;
		var sells = 0;
		var fills = 0;
		await Replay("2027_Exp_CyclePeriod", (strategy, _) =>
		{
			AreEqual(0.07m, Convert.ToDecimal(strategy.Parameters["Alpha"].Value));
			AreEqual(1, Convert.ToInt32(strategy.Parameters["SignalBar"].Value));
			AreEqual(2000m, Convert.ToDecimal(strategy.Parameters["TakeProfit"].Value));
			AreEqual(1000m, Convert.ToDecimal(strategy.Parameters["StopLoss"].Value));
			foreach (var flag in new[] { "BuyPosOpen", "SellPosOpen", "BuyPosClose", "SellPosClose" })
				AreEqual(true, strategy.Parameters[flag].Value, $"Documented {flag} default.");
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "TakeProfit", 1m);
			SetParam(strategy, "StopLoss", 1m);
			strategy.OrderRegistering += order =>
			{
				var stop = (Unit)stopField.GetValue(strategy);
				var take = (Unit)takeField.GetValue(strategy);
				IsNotNull(stop);
				IsNotNull(take);
				AreEqual(UnitTypes.Absolute, stop.Type);
				AreEqual(UnitTypes.Absolute, take.Type);
				IsTrue(order.Volume > 0m);
				if (order.Side == Sides.Buy) buys++;
				else sells++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(buys > 0 && sells > 0 && fills > 0,
			"Both CyclePeriod directions and actual fills must occur on the packaged archive.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_MfiHistogram_TickProtectionAndActualReversalFills()
	{
		var stopField = typeof(Strategy).GetField("_stopLoss", BindingFlags.Instance | BindingFlags.NonPublic);
		var takeField = typeof(Strategy).GetField("_takeProfit", BindingFlags.Instance | BindingFlags.NonPublic);
		IsNotNull(stopField);
		IsNotNull(takeField);
		var marketOrders = 0;
		var actualFills = 0;
		await Replay("2200_MFI_Histogram", (strategy, _) =>
		{
			AreEqual(1000m, ((Unit)strategy.Parameters["StopLoss"].Value).Value);
			AreEqual(2000m, ((Unit)strategy.Parameters["TakeProfit"].Value).Value);
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "MfiPeriod", 5);
			SetParam(strategy, "HighLevel", 55m);
			SetParam(strategy, "LowLevel", 45m);
			SetParam(strategy, "StopLoss", new Unit(1m, UnitTypes.Absolute));
			SetParam(strategy, "TakeProfit", new Unit(1m, UnitTypes.Absolute));
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m, "Orders cannot have zero or negative volume.");
				var stop = (Unit)stopField.GetValue(strategy);
				var take = (Unit)takeField.GetValue(strategy);
				IsNotNull(stop);
				IsNotNull(take);
				AreEqual(UnitTypes.Absolute, stop.Type);
				AreEqual(UnitTypes.Absolute, take.Type);
				var step = strategy.Security.PriceStep;
				IsTrue(step is > 0m);
				AreEqual(step.Value, stop.Value, "StopLoss=1 tick must mean one instrument price step.");
				AreEqual(step.Value, take.Value, "TakeProfit=1 tick must mean one instrument price step.");
				if (order.Type == OrderTypes.Market)
					marketOrders++;
			};
			strategy.Trades.TradeAdded += _ => actualFills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(marketOrders > 0 && actualFills > 0,
			"A filled market order and price-step-anchored protection must be observed on the BTC/TON archive.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_BreakevenTrailing_OneLongEntryAndFillAnchoredExit()
	{
		var longEntries = 0;
		var actualFills = 0;
		var stopExits = 0;
		await Replay("2135_Breakeven_Trailing_Stop", (strategy, _) =>
		{
			AreEqual(5m, Convert.ToDecimal(strategy.Parameters["BreakevenPlus"].Value));
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["BreakevenStep"].Value));
			AreEqual(3m, Convert.ToDecimal(strategy.Parameters["TrailingPlus"].Value));
			AreEqual(1m, Convert.ToDecimal(strategy.Parameters["TrailingStep"].Value));
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "BreakevenPlus", 1m);
			SetParam(strategy, "BreakevenStep", 0.2m);
			SetParam(strategy, "TrailingPlus", 0.5m);
			SetParam(strategy, "TrailingStep", 0.2m);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m);
				if (order.Side == Sides.Buy)
					longEntries++;
				else if (order.Side == Sides.Sell)
				{
					IsTrue(strategy.Position > 0m, "Stop exit must be against an executed long.");
					AreEqual(strategy.Position, order.Volume, "Exit must close entire actual position.");
					stopExits++;
				}
			};
			strategy.Trades.TradeAdded += _ => actualFills++;
		}, replayDuration: TimeSpan.FromDays(14));
		AreEqual(1, longEntries, "Only the first completed candle may open a long position.");
		IsTrue(actualFills >= 2 && stopExits > 0,
			"Archive must demonstrate actual entry and a filled quote-triggered stop exit.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_BykovTrend_HysteresisAndActualReversalFills()
	{
		var orders = 0;
		var actualFills = 0;
		await Replay("2217_BykovTrend", (strategy, _) =>
		{
			AreEqual(3, Convert.ToInt32(strategy.Parameters["Risk"].Value));
			AreEqual(9, Convert.ToInt32(strategy.Parameters["SSP"].Value));
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "SSP", 5);
			strategy.OrderRegistering += order =>
			{
				AreEqual(OrderTypes.Market, order.Type);
				AreEqual(strategy.Volume + Math.Abs(strategy.Position), order.Volume,
					"An opposite Williams %R extreme must fully reverse the executed position.");
				orders++;
			};
			strategy.Trades.TradeAdded += _ => actualFills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(orders > 0 && actualFills > 0,
			"Archive must exhibit a completed threshold transition and an actual matched trade.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_ForexLine_DoubleWeightedCrossAndRealFills()
	{
		var buys = 0;
		var sells = 0;
		var actualFills = 0;
		await Replay("2196_ForexLine", (strategy, _) =>
		{
			AreEqual(5, Convert.ToInt32(strategy.Parameters["FastLength1"].Value));
			AreEqual(10, Convert.ToInt32(strategy.Parameters["FastLength2"].Value));
			AreEqual(20, Convert.ToInt32(strategy.Parameters["SlowLength1"].Value));
			AreEqual(20, Convert.ToInt32(strategy.Parameters["SlowLength2"].Value));
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "FastLength1", 2);
			SetParam(strategy, "FastLength2", 3);
			SetParam(strategy, "SlowLength1", 4);
			SetParam(strategy, "SlowLength2", 5);
			strategy.OrderRegistering += order =>
			{
				AreEqual(OrderTypes.Market, order.Type);
				AreEqual(strategy.Volume + Math.Abs(strategy.Position), order.Volume,
					"A WMA crossover must reverse the entire filled opposite side.");
				if (order.Side == Sides.Buy) buys++;
				else sells++;
			};
			strategy.Trades.TradeAdded += _ => actualFills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(buys > 0 && sells > 0 && actualFills > 0,
			"BTC/TON history must produce both formed-signal directions and actual matched trades.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_ColorBearsGap_FlagsAndActualPositionCloses()
	{
		var actualFills = 0;
		var longEntries = 0;
		var longCloses = 0;
		await Replay("2157_Color_Bears_Gap", (strategy, _) =>
		{
			foreach (var name in new[] { "BuyOpen", "BuyClose", "SellOpen", "SellClose" })
				AreEqual(true, strategy.Parameters[name].Value, $"Expected documented default for {name}.");
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "Length1", 3);
			SetParam(strategy, "Length2", 2);
			SetParam(strategy, "SellOpen", false);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m);
				if (order.Side == Sides.Buy)
				{
					IsTrue(strategy.Position <= 0m);
					longEntries++;
				}
				else if (order.Side == Sides.Sell)
				{
					IsTrue(strategy.Position > 0m, "SellOpen=false must not create short positions.");
					AreEqual(strategy.Position, order.Volume,
						"Closing the long must use its actual volume, not default Volume.");
					longCloses++;
				}
			};
			strategy.Trades.TradeAdded += _ => actualFills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(longEntries > 0 && longCloses > 0 && actualFills > 1,
			"Both a long fill and a close fill are required on the BTC/TON archive.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_HeikenAshiNoWick_ActualContrarianFillAndPositionSize()
	{
		var registered = 0;
		var fills = 0;
		await Replay("1994_Heiken_Ashi_No_Wick", (strategy, _) =>
		{
			// Keep documented 15-minute default for the primary replay.
			strategy.OrderRegistering += order =>
			{
				AreEqual(OrderTypes.Market, order.Type);
				IsTrue(order.Volume > 0m, "HA no-wick orders require positive volumes.");
				if (strategy.Position != 0m && (strategy.Position > 0m) != (order.Side == Sides.Buy))
					IsTrue(order.Volume >= Math.Abs(strategy.Position),
						"An opposite no-wick candle must at least close the actual position.");
				registered++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(registered > 0 && fills > 0,
			"The BTC/TON archive must demonstrate an actual contrarian no-wick trade and fill.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_ColorZeroLagMa_CloseOnlyModeProducesActualExit()
	{
		var sellClose = 0;
		var fills = 0;
		await Replay("1995_Color_ZeroLag_MA", (strategy, _) =>
		{
			foreach (var flag in new[] { "OpenBuy", "OpenSell", "CloseBuy", "CloseSell" })
				AreEqual(true, strategy.Parameters[flag].Value,
					$"{flag} must default to true as documented.");
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "Length", 5);
			SetParam(strategy, "OpenSell", false);
			strategy.OrderRegistering += order =>
			{
				AreEqual(OrderTypes.Market, order.Type);
				IsTrue(order.Volume > 0m);
				if (order.Side == Sides.Sell)
				{
					IsTrue(strategy.Position > 0m,
						"OpenSell=false must not create a new short position.");
					AreEqual(strategy.Position, order.Volume,
						"The entire executed long must close without a short entry.");
					sellClose++;
				}
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(sellClose > 0 && fills > 1,
			"Archive verification needs a real long entry and fully matched close.");
	}

	[TestMethod]
	[TestCategory("Point1Part05")]
	public async Task Point1Part05_TrailingStopActivation_ManualEntryMustHaveActualProtectiveExit()
	{
		var seeded = false;
		var entryOrders = 0;
		var exitOrders = 0;
		var exitFills = 0;
		await Replay("1989_Trailing_Stop_Activation", (strategy, _) =>
		{
			AreEqual(500m, Convert.ToDecimal(strategy.Parameters["TrailingStop"].Value));
			AreEqual(0m, Convert.ToDecimal(strategy.Parameters["StopLoss"].Value));
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "TrailingStop", 0.1m);
			SetParam(strategy, "StopLoss", 1m);
			strategy.OrderRegistering += order =>
			{
				AreEqual(OrderTypes.Market, order.Type);
				IsTrue(order.Volume > 0m);
				if (order.Side == Sides.Buy)
				{
					IsTrue(seeded, "Exit-only manager must not open any unseeded long position.");
					entryOrders++;
				}
				else
				{
					IsTrue(strategy.Position > 0m, "Protective sell needs a filled long position.");
					AreEqual(strategy.Position, order.Volume, "Protective exit must close all filled volume.");
					exitOrders++;
				}
			};
			strategy.Trades.TradeAdded += trade =>
			{
				if (trade.Order.Side == Sides.Sell)
					exitFills++;
			};
			strategy.CandleReceived += (_, candle) =>
			{
				if (seeded || candle.State != CandleStates.Finished ||
					!strategy.IsFormedAndOnlineAndAllowTrading())
					return;
				seeded = true;
				strategy.BuyMarket(strategy.Volume);
			};
		}, replayDuration: TimeSpan.FromDays(14));

		AreEqual(1, entryOrders, "Exactly one entry must be seeded by the test itself.");
		IsTrue(exitOrders > 0 && exitFills > 0,
			"Manager must close the manually seeded position with an actual filled protective order.");
	}


	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_LacustStopAndBe_FilledEntryAndExecutableQuoteExits()
	{
		var entries = 0;
		var exits = 0;
		var filled = 0;
		await Replay("1993_Lacust_Stop_and_BE", (strategy, _) =>
		{
			AreEqual(400m, Convert.ToDecimal(strategy.Parameters["StopLoss"].Value));
			AreEqual(2000m, Convert.ToDecimal(strategy.Parameters["TakeProfit"].Value));
			AreEqual(300m, Convert.ToDecimal(strategy.Parameters["TrailingStart"].Value));
			AreEqual(200m, Convert.ToDecimal(strategy.Parameters["TrailingStop"].Value));
			AreEqual(250m, Convert.ToDecimal(strategy.Parameters["BreakevenGain"].Value));
			AreEqual(100m, Convert.ToDecimal(strategy.Parameters["Breakeven"].Value));
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "StopLoss", 10m);
			SetParam(strategy, "TakeProfit", 15m);
			SetParam(strategy, "TrailingStart", 8m);
			SetParam(strategy, "TrailingStop", 5m);
			SetParam(strategy, "BreakevenGain", 6m);
			SetParam(strategy, "Breakeven", 1m);
			strategy.OrderRegistering += order =>
			{
				AreEqual(OrderTypes.Market, order.Type);
				IsTrue(order.Volume > 0m);
				if (strategy.Position == 0m)
				{
					AreEqual(strategy.Volume, order.Volume);
					entries++;
				}
				else
				{
					AreEqual(Math.Abs(strategy.Position), order.Volume,
						"Stop/take exit must close the actual filled position, not the default volume.");
					AreEqual(strategy.Position > 0m ? Sides.Sell : Sides.Buy, order.Side);
					IsTrue(order.Comment is "Lacust stop / breakeven / trailing" or "Lacust take profit",
						"Protective exit must be triggered from a fresh executable quote.");
					exits++;
				}
			};
			strategy.Trades.TradeAdded += _ => filled++;
		}, replayDuration: TimeSpan.FromDays(7));
		IsTrue(entries > 0 && exits > 0 && filled >= 2,
			"Packaged BTC/TON must contain actual opening and protective exit fills.");
	}


	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_CenterOfGravityOsma_MultistageOscillatorAndActualReversalFills()
	{
		var orders = 0;
		var fills = 0;
		var stopField = typeof(Strategy).GetField("_stopLoss", BindingFlags.Instance | BindingFlags.NonPublic);
		var takeField = typeof(Strategy).GetField("_takeProfit", BindingFlags.Instance | BindingFlags.NonPublic);
		IsNotNull(stopField);
		IsNotNull(takeField);
		await Replay("1998_Center_Of_Gravity_OSMA", (strategy, _) =>
		{
			foreach (var name in new[] { "Period", "SmoothPeriod1", "SmoothPeriod2",
				"BuyPosOpen", "SellPosOpen", "BuyPosClose", "SellPosClose", "StopLoss", "TakeProfit" })
				IsTrue(strategy.Parameters.ContainsKey(name), $"Missing {name} in {Extension}.");
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "Period", 4);
			SetParam(strategy, "SmoothPeriod1", 3);
			SetParam(strategy, "SmoothPeriod2", 3);
			SetParam(strategy, "StopLoss", 100m);
			SetParam(strategy, "TakeProfit", 200m);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m);
				if (strategy.Position == 0m)
				{
					var stop = (Unit)stopField.GetValue(strategy);
					var take = (Unit)takeField.GetValue(strategy);
					IsNotNull(stop);
					IsNotNull(take);
					AreEqual(UnitTypes.Absolute, stop.Type);
					AreEqual(UnitTypes.Absolute, take.Type);
					AreEqual(100m, stop.Value);
					AreEqual(200m, take.Value);
				}
				else if (order.Side == (strategy.Position > 0m ? Sides.Sell : Sides.Buy))
					IsTrue(order.Volume <= strategy.Volume + Math.Abs(strategy.Position),
						"Reverse or native stop must never exceed executed position plus one entry.");
				orders++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(7));
		IsTrue(orders > 0 && fills > 0,
			"Center-of-Gravity OSMA must generate actual orders and trade fills on the archive.");
	}

}

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


	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_ColorZerolagTrix_FiveWeightedComponentsAndInvertedCrossings()
	{
		var buys = 0;
		var sells = 0;
		var fills = 0;
		await Replay("1997_ColorZerolagTrix", (strategy, _) =>
		{
			AreEqual(TimeSpan.FromHours(4).TimeFrame(), strategy.Parameters["CandleType"].Value);
			foreach (var flag in new[] { "BuyOpen", "BuyClose", "SellOpen", "SellClose" })
				AreEqual(true, strategy.Parameters[flag].Value);
			foreach (var index in new[] { 1, 2, 3, 4, 5 })
			{
				IsTrue(strategy.Parameters.ContainsKey("Period" + index));
				IsTrue(strategy.Parameters.ContainsKey("Factor" + index));
				SetParam(strategy, "Period" + index, index + 2);
			}
			SetParam(strategy, "Smoothing", 3);
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			// Closed shorts are allowed, but new shorts are forbidden in this profile.
			SetParam(strategy, "SellOpen", false);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m);
				if (order.Side == Sides.Buy)
					buys++;
				else
				{
					IsTrue(strategy.Position > 0m, "SellOpen=false must not create shorts.");
					AreEqual(strategy.Position, order.Volume, "Long closes use the actual volume.");
					sells++;
				}
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(buys > 0 && sells > 0 && fills > 1,
			"Both sides of the inverted fast/slow crossing must produce real executions.");
	}


	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_ColorZerolagTrixOsma_FiveComponentsAndActualOscillatorReversals()
	{
		var orders = 0;
		var fills = 0;
		var stopField = typeof(Strategy).GetField("_stopLoss", BindingFlags.Instance | BindingFlags.NonPublic);
		IsNotNull(stopField);
		await Replay("1999_Color_Zerolag_TriX_OSMA", (strategy, _) =>
		{
			foreach (var name in new[] { "Smoothing1", "Smoothing2", "StopLoss", "TakeProfit" })
				IsTrue(strategy.Parameters.ContainsKey(name), $"Missing {name} in {Extension}.");
			for (var i = 1; i <= 5; i++)
			{
				IsTrue(strategy.Parameters.ContainsKey("Factor" + i));
				IsTrue(strategy.Parameters.ContainsKey("Period" + i));
				SetParam(strategy, "Period" + i, i + 2);
			}
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "Smoothing1", 3);
			SetParam(strategy, "Smoothing2", 3);
			SetParam(strategy, "StopLoss", 100m);
			SetParam(strategy, "TakeProfit", 200m);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m);
				if (strategy.Position == 0m)
				{
					var stop = (Unit)stopField.GetValue(strategy);
					IsNotNull(stop);
					AreEqual(UnitTypes.Absolute, stop.Type);
					AreEqual(100m, stop.Value);
				}
				else if (order.Side == (strategy.Position > 0m ? Sides.Sell : Sides.Buy))
					IsTrue(order.Volume <= Math.Abs(strategy.Position) + strategy.Volume);
				orders++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(orders > 0 && fills > 0, "Archive must show actual OSMA reversal orders and fills.");
	}


	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_ColorZerolagRsiOsma_FiveFormedRsiAndLiveReversals()
	{
		var entries = 0;
		var exits = 0;
		var fills = 0;
		await Replay("2001_Color_Zerolag_RSI_OSMA", (strategy, _) =>
		{
			var published = new[] { 8, 21, 34, 55, 89 };
			for (var i = 1; i <= 5; i++)
			{
				AreEqual(published[i - 1], Convert.ToInt32(strategy.Parameters["RsiPeriod" + i].Value));
				IsTrue(strategy.Parameters.ContainsKey("Factor" + i));
				SetParam(strategy, "RsiPeriod" + i, i + 2);
			}
			AreEqual(TimeSpan.FromHours(4).TimeFrame(), strategy.Parameters["CandleType"].Value);
			foreach (var flag in new[] { "AllowBuy", "AllowSell", "CloseLong", "CloseShort" })
				AreEqual(true, strategy.Parameters[flag].Value);
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "Smoothing1", 3);
			SetParam(strategy, "Smoothing2", 3);
			SetParam(strategy, "StopLoss", 0m);
			SetParam(strategy, "TakeProfit", 0m);
			SetParam(strategy, "AllowSell", false);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m);
				if (order.Side == Sides.Buy)
				{
					var expectedBuy = strategy.Position < 0m
						? -strategy.Position + strategy.Volume : strategy.Volume;
					AreEqual(expectedBuy, order.Volume,
						$"Buy order sizing: position={strategy.Position}, side={order.Side}, allowSell={strategy.Parameters["AllowSell"].Value}.");
					entries++;
				}
				else
				{
					IsTrue(strategy.Position > 0m,
						$"AllowSell=false prohibits opening shorts: position={strategy.Position}, volume={order.Volume}, flags={strategy.Parameters["AllowSell"].Value}.");
					AreEqual(strategy.Position, order.Volume,
						$"Sell closing volume should match actual position {strategy.Position}; submitted {order.Volume}; flags={strategy.Parameters["AllowSell"].Value}.");
					exits++;
				}
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(entries > 0 && exits > 0 && fills >= 2,
			"Both actual entries and exits must fill for 5-component RSI OSMA.");
	}


	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_ColorZerolagRsiOsma_NativeProtectionSeparatelyTracksRealOrders()
	{
		var registered = 0;
		var filled = 0;
		var stopField = typeof(Strategy).GetField("_stopLoss", BindingFlags.Instance | BindingFlags.NonPublic);
		var takeField = typeof(Strategy).GetField("_takeProfit", BindingFlags.Instance | BindingFlags.NonPublic);
		IsNotNull(stopField);
		IsNotNull(takeField);
		await Replay("2001_Color_Zerolag_RSI_OSMA", (strategy, _) =>
		{
			for (var i = 1; i <= 5; i++)
				SetParam(strategy, "RsiPeriod" + i, i + 2);
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "Smoothing1", 3);
			SetParam(strategy, "Smoothing2", 3);
			SetParam(strategy, "StopLoss", 100m);
			SetParam(strategy, "TakeProfit", 200m);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m);
				var stop = (Unit)stopField.GetValue(strategy);
				var take = (Unit)takeField.GetValue(strategy);
				IsNotNull(stop);
				IsNotNull(take);
				AreEqual(UnitTypes.Absolute, stop.Type);
				AreEqual(UnitTypes.Absolute, take.Type);
				AreEqual(100m, stop.Value);
				AreEqual(200m, take.Value);
				registered++;
				// Native ActiveProtection may submit a pure close of |Position|.
				// That order must not be mislabeled as a strategy entry/reversal.
			};
			strategy.Trades.TradeAdded += _ => filled++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(registered > 0 && filled > 0, "Protection profile needs actual archive-backed orders and fills.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_WellMartin_AdxFilterAndNativeProtectionWithRealFills()
	{
		var entries = 0;
		var exits = 0;
		var fills = 0;
		var stopField = typeof(Strategy).GetField("_stopLoss", BindingFlags.Instance | BindingFlags.NonPublic);
		IsNotNull(stopField);
		await Replay("2002_WellMartin", (strategy, _) =>
		{
			foreach (var name in new[] { "BollingerPeriod", "BollingerWidth", "AdxPeriod", "AdxLevel",
				"StopLoss", "TakeProfit" })
				IsTrue(strategy.Parameters.ContainsKey(name), $"{Extension} missing {name}.");
			AreEqual(84, Convert.ToInt32(strategy.Parameters["BollingerPeriod"].Value));
			AreEqual(1.8m, Convert.ToDecimal(strategy.Parameters["BollingerWidth"].Value));
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "BollingerPeriod", 5);
			SetParam(strategy, "BollingerWidth", 0.5m);
			SetParam(strategy, "AdxPeriod", 5);
			SetParam(strategy, "AdxLevel", 100m);
			SetParam(strategy, "StopLoss", 100m);
			SetParam(strategy, "TakeProfit", 200m);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m);
				if (strategy.Position == 0m)
				{
					AreEqual(OrderTypes.Market, order.Type, "Signal entries are market orders.");
					AreEqual(strategy.Volume, order.Volume, "Flat entry must use fixed volume.");
					var stop = (Unit)stopField.GetValue(strategy);
					IsNotNull(stop);
					AreEqual(UnitTypes.Absolute, stop.Type);
					AreEqual(100m, stop.Value);
					entries++;
				}
				else
				{
					AreEqual(Math.Abs(strategy.Position), order.Volume,
						"Exit must use the actual filled position, not one nominal lot.");
					AreEqual(strategy.Position > 0m ? Sides.Sell : Sides.Buy, order.Side);
					exits++;
				}
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(entries > 0 && exits > 0 && fills >= 2, "Require actual entry and exit fills.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_ColorZerolagX10Ma_IndependentEntryExitFlagsAndActualFills()
	{
		var buys = 0;
		var closes = 0;
		var fills = 0;
		await Replay("2006_Color_Zerolag_X10_Ma", (strategy, _) =>
		{
			AreEqual(20, Convert.ToInt32(strategy.Parameters["Length"].Value));
			AreEqual(TimeSpan.FromHours(4).TimeFrame(), strategy.Parameters["CandleType"].Value);
			foreach (var flag in new[] { "BuyPosOpen", "SellPosOpen", "BuyPosClose", "SellPosClose" })
				AreEqual(true, strategy.Parameters[flag].Value);
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "Length", 5);
			SetParam(strategy, "SellPosOpen", false);
			strategy.OrderRegistering += order =>
			{
				AreEqual(OrderTypes.Market, order.Type);
				IsTrue(order.Volume > 0m);
				if (order.Side == Sides.Sell)
				{
					IsTrue(strategy.Position > 0m, "SellPosOpen=false forbids a new short position.");
					AreEqual(strategy.Position, order.Volume, "Opposite signal must flatten the full long.");
					closes++;
				}
				else
				{
					AreEqual(strategy.Volume, order.Volume, "A flat long entry uses the configured order volume.");
					buys++;
				}
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(buys > 0 && closes > 0 && fills > 1, "Both entry and exit must actually fill.");
	}


	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_ColorZerolagMomentumOsma_WeightedMomentumAndFullActualExits()
	{
		var buys = 0;
		var sells = 0;
		var fills = 0;
		await Replay("2004_Color_Zerolag_Momentum_OSMA", (strategy, _) =>
		{
			foreach (var flag in new[] { "BuyOpen", "SellOpen", "BuyClose", "SellClose" })
				AreEqual(true, strategy.Parameters[flag].Value);
			for (var i = 1; i <= 5; i++)
			{
				IsTrue(strategy.Parameters.ContainsKey("MomentumPeriod" + i));
				IsTrue(strategy.Parameters.ContainsKey("Factor" + i));
				SetParam(strategy, "MomentumPeriod" + i, i + 2);
			}
			SetParam(strategy, "Smoothing1", 3);
			SetParam(strategy, "Smoothing2", 3);
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "SellOpen", false);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m);
				if (order.Side == Sides.Sell)
				{
					IsTrue(strategy.Position > 0m, "SellOpen=false forbids new shorts.");
					AreEqual(strategy.Position, order.Volume, "Close the exact filled long.");
					sells++;
				}
				else
				{
					AreEqual(strategy.Volume, order.Volume);
					buys++;
				}
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(buys > 0 && sells > 0 && fills > 1,
			"Weighted Momentum OSMA must execute both entry and closing orders.");
	}


	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_ColorJMomentum_JurikSmoothedMomentumAndNativePercentProtection()
	{
		var buys = 0;
		var fills = 0;
		var stopField = typeof(Strategy).GetField("_stopLoss", BindingFlags.Instance | BindingFlags.NonPublic);
		IsNotNull(stopField);
		await Replay("2007_ColorJMomentum", (strategy, _) =>
		{
			AreEqual(8, Convert.ToInt32(strategy.Parameters["MomentumLength"].Value));
			AreEqual(8, Convert.ToInt32(strategy.Parameters["JmaLength"].Value));
			AreEqual(TimeSpan.FromHours(8).TimeFrame(), strategy.Parameters["CandleType"].Value);
			foreach (var name in new[] { "EnableStopLoss", "StopLossPercent", "TakeProfitPercent", "EnableLong", "EnableShort" })
				IsTrue(strategy.Parameters.ContainsKey(name));
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "MomentumLength", 3);
			SetParam(strategy, "JmaLength", 3);
			SetParam(strategy, "EnableShort", false);
			SetParam(strategy, "EnableStopLoss", true);
			SetParam(strategy, "StopLossPercent", 1m);
			SetParam(strategy, "TakeProfitPercent", 2m);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m);
				var unit = (Unit)stopField.GetValue(strategy);
				IsNotNull(unit);
				AreEqual(UnitTypes.Percent, unit.Type);
				AreEqual(1m, unit.Value);
				if (order.Side == Sides.Buy)
					buys++;
				else if (strategy.Position > 0m)
					AreEqual(strategy.Position, order.Volume);
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(buys > 0 && fills > 0, "Smoothed momentum must fill real entries in the archive.");
	}


	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_ColorJLaguerre_JurikSmoothedRsiAndActualExits()
	{
		var buys = 0;
		var sells = 0;
		var fills = 0;
		var stopField = typeof(Strategy).GetField("_stopLoss", BindingFlags.Instance | BindingFlags.NonPublic);
		IsNotNull(stopField);
		await Replay("2008_Color_JLaguerre", (strategy, _) =>
		{
			AreEqual(14, Convert.ToInt32(strategy.Parameters["RsiLength"].Value));
			AreEqual(85m, Convert.ToDecimal(strategy.Parameters["HighLevel"].Value));
			AreEqual(50m, Convert.ToDecimal(strategy.Parameters["MiddleLevel"].Value));
			AreEqual(15m, Convert.ToDecimal(strategy.Parameters["LowLevel"].Value));
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
			AreEqual(TimeSpan.FromHours(1).TimeFrame(), strategy.Parameters["CandleType"].Value);
			IsTrue(strategy.Parameters.ContainsKey("JmaLength"), "Jurik smoothing must be configurable.");
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "RsiLength", 4);
			SetParam(strategy, "JmaLength", 4);
			SetParam(strategy, "HighLevel", 65m);
			SetParam(strategy, "LowLevel", 35m);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m);
				var stop = (Unit)stopField.GetValue(strategy);
				IsNotNull(stop);
				AreEqual(UnitTypes.Percent, stop.Type);
				AreEqual(2m, stop.Value);
				if (order.Side == Sides.Buy) buys++;
				else sells++;
				if (strategy.Position != 0m && order.Side == (strategy.Position > 0m ? Sides.Sell : Sides.Buy))
					IsTrue(order.Volume <= Math.Abs(strategy.Position) + strategy.Volume);
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(buys > 0 && sells > 0 && fills > 1,
			"Archive must contain actual oscillator signals and filled orders in both directions.");
	}


	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_ColorMaRsiTrigger_DocumentedSignTransitionWithActualFills()
	{
		var buys = 0;
		var sells = 0;
		var fills = 0;
		await Replay("2010_ColorMaRsi_Trigger", (strategy, _) =>
		{
			foreach (var (name, value) in new (string name, int value)[]
			{
				("EmaFastLength", 5), ("EmaSlowLength", 10), ("RsiFastLength", 3), ("RsiSlowLength", 13)
			})
				AreEqual(value, Convert.ToInt32(strategy.Parameters[name].Value));
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "EmaFastLength", 3);
			SetParam(strategy, "EmaSlowLength", 7);
			SetParam(strategy, "RsiFastLength", 3);
			SetParam(strategy, "RsiSlowLength", 7);
			strategy.OrderRegistering += order =>
			{
				AreEqual(OrderTypes.Market, order.Type);
				IsTrue(order.Volume > 0m);
				if (order.Side == Sides.Buy) buys++;
				else sells++;
				if (strategy.Position != 0m)
				{
					AreEqual(Math.Abs(strategy.Position) + strategy.Volume, order.Volume,
						"Opposite composite signal must flatten the actual position and enter exactly once.");
				}
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(buys > 0 && sells > 0 && fills > 1,
			"The archive must produce real buys and sells on documented sign transitions.");
	}


	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_EscortTrend_MacdConfirmationAndNativeAbsoluteRiskWithFills()
	{
		var orders = 0;
		var fills = 0;
		var stopField = typeof(Strategy).GetField("_stopLoss", BindingFlags.Instance | BindingFlags.NonPublic);
		var takeField = typeof(Strategy).GetField("_takeProfit", BindingFlags.Instance | BindingFlags.NonPublic);
		IsNotNull(stopField);
		IsNotNull(takeField);
		await Replay("2011_Escort_Trend", (strategy, _) =>
		{
			foreach (var (name, value) in new (string, int)[]
			{
				("FastWmaPeriod", 8), ("SlowWmaPeriod", 18), ("CciPeriod", 14),
				("MacdFastEma", 8), ("MacdSlowEma", 18)
			})
				AreEqual(value, Convert.ToInt32(strategy.Parameters[name].Value));
			AreEqual(100m, Convert.ToDecimal(strategy.Parameters["CciThreshold"].Value));
			AreEqual(200m, Convert.ToDecimal(strategy.Parameters["TakeProfit"].Value));
			AreEqual(55m, Convert.ToDecimal(strategy.Parameters["StopLoss"].Value));
			AreEqual(35m, Convert.ToDecimal(strategy.Parameters["TrailingStop"].Value));
			AreEqual(3m, Convert.ToDecimal(strategy.Parameters["TrailingStep"].Value));
			SetParam(strategy, "FastWmaPeriod", 3);
			SetParam(strategy, "SlowWmaPeriod", 7);
			SetParam(strategy, "CciPeriod", 5);
			SetParam(strategy, "CciThreshold", 10m);
			SetParam(strategy, "MacdFastEma", 3);
			SetParam(strategy, "MacdSlowEma", 8);
			SetParam(strategy, "MacdSignalPeriod", 3);
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "StopLoss", 100m);
			SetParam(strategy, "TakeProfit", 200m);
			SetParam(strategy, "TrailingStop", 0m);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m);
				var stop = (Unit)stopField.GetValue(strategy);
				var take = (Unit)takeField.GetValue(strategy);
				IsNotNull(stop);
				IsNotNull(take);
				AreEqual(UnitTypes.Absolute, stop.Type);
				AreEqual(UnitTypes.Absolute, take.Type);
				AreEqual(100m, stop.Value);
				AreEqual(200m, take.Value);
				orders++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(orders > 0 && fills > 0,
			"MACD/WMA/CCI confluence must place and fill actual orders.");
	}


	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_Batch10_MomentumAma_ExecutesActualFills()
	{
		var orders = 0;
		var fills = 0;
		await Replay("2017_ColorMomentum_Ama", (strategy, _) =>
		{
			AreEqual(8, Convert.ToInt32(strategy.Parameters["MomentumPeriod"].Value), "2017_ColorMomentum_Ama: MomentumPeriod default.");
			AreEqual(9, Convert.ToInt32(strategy.Parameters["AmaPeriod"].Value), "2017_ColorMomentum_Ama: AmaPeriod default.");
			AreEqual(0, Convert.ToInt32(strategy.Parameters["SignalCooldownBars"].Value), "2017_ColorMomentum_Ama: SignalCooldownBars default.");

			AreEqual(TimeSpan.FromHours(1).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "MomentumPeriod", 5);
			SetParam(strategy, "AmaPeriod", 5);
			SetParam(strategy, "FastPeriod", 2);
			SetParam(strategy, "SlowPeriod", 20);
			SetParam(strategy, "SignalBar", 1);
			SetParam(strategy, "SignalCooldownBars", 0);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m, "2017_ColorMomentum_Ama: market order requires a positive volume.");
				if (strategy.Position != 0m && order.Side == (strategy.Position > 0m ? Sides.Sell : Sides.Buy))
					IsTrue(order.Volume <= Math.Abs(strategy.Position) + strategy.Volume,
						"2017_ColorMomentum_Ama: closing volume must not exceed current position plus one entry.");
				orders++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(orders > 0 && fills > 0,
			"2017_ColorMomentum_Ama: require real order registration and an actual filled trade on BTC/TON archive.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_Batch10_Xxdpo_ExecutesActualFills()
	{
		var orders = 0;
		var fills = 0;
		await Replay("2022_Color_XXDPO", (strategy, _) =>
		{
			AreEqual(21, Convert.ToInt32(strategy.Parameters["FirstLength"].Value), "2022_Color_XXDPO: FirstLength default.");
			AreEqual(5, Convert.ToInt32(strategy.Parameters["SecondLength"].Value), "2022_Color_XXDPO: SecondLength default.");
			AreEqual(0, Convert.ToInt32(strategy.Parameters["SignalCooldownBars"].Value), "2022_Color_XXDPO: SignalCooldownBars default.");

			AreEqual(TimeSpan.FromHours(6).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "FirstLength", 8);
			SetParam(strategy, "SecondLength", 3);
			SetParam(strategy, "SignalCooldownBars", 0);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m, "2022_Color_XXDPO: market order requires a positive volume.");
				if (strategy.Position != 0m && order.Side == (strategy.Position > 0m ? Sides.Sell : Sides.Buy))
					IsTrue(order.Volume <= Math.Abs(strategy.Position) + strategy.Volume,
						"2022_Color_XXDPO: closing volume must not exceed current position plus one entry.");
				orders++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(orders > 0 && fills > 0,
			"2022_Color_XXDPO: require real order registration and an actual filled trade on BTC/TON archive.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_Batch10_XdinMA_ExecutesActualFills()
	{
		var orders = 0;
		var fills = 0;
		await Replay("2023_ColorXdinMA", (strategy, _) =>
		{
			AreEqual(10, Convert.ToInt32(strategy.Parameters["MainLength"].Value), "2023_ColorXdinMA: MainLength default.");
			AreEqual(20, Convert.ToInt32(strategy.Parameters["PlusLength"].Value), "2023_ColorXdinMA: PlusLength default.");
			AreEqual(0, Convert.ToInt32(strategy.Parameters["SignalCooldownBars"].Value), "2023_ColorXdinMA: SignalCooldownBars default.");

			AreEqual(TimeSpan.FromHours(6).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "MainLength", 5);
			SetParam(strategy, "PlusLength", 10);
			SetParam(strategy, "SignalCooldownBars", 0);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m, "2023_ColorXdinMA: market order requires a positive volume.");
				if (strategy.Position != 0m && order.Side == (strategy.Position > 0m ? Sides.Sell : Sides.Buy))
					IsTrue(order.Volume <= Math.Abs(strategy.Position) + strategy.Volume,
						"2023_ColorXdinMA: closing volume must not exceed current position plus one entry.");
				orders++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(orders > 0 && fills > 0,
			"2023_ColorXdinMA: require real order registration and an actual filled trade on BTC/TON archive.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_Batch10_CoppockHistogram_ExecutesActualFills()
	{
		var orders = 0;
		var fills = 0;
		await Replay("2025_Coppock_Histogram", (strategy, _) =>
		{
			AreEqual(14, Convert.ToInt32(strategy.Parameters["Roc1Period"].Value), "2025_Coppock_Histogram: Roc1Period default.");
			AreEqual(11, Convert.ToInt32(strategy.Parameters["Roc2Period"].Value), "2025_Coppock_Histogram: Roc2Period default.");
			AreEqual(0, Convert.ToInt32(strategy.Parameters["SignalCooldownBars"].Value), "2025_Coppock_Histogram: SignalCooldownBars default.");

			AreEqual(TimeSpan.FromHours(8).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "Roc1Period", 7);
			SetParam(strategy, "Roc2Period", 5);
			SetParam(strategy, "SmoothPeriod", 3);
			SetParam(strategy, "SignalCooldownBars", 0);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m, "2025_Coppock_Histogram: market order requires a positive volume.");
				if (strategy.Position != 0m && order.Side == (strategy.Position > 0m ? Sides.Sell : Sides.Buy))
					IsTrue(order.Volume <= Math.Abs(strategy.Position) + strategy.Volume,
						"2025_Coppock_Histogram: closing volume must not exceed current position plus one entry.");
				orders++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(orders > 0 && fills > 0,
			"2025_Coppock_Histogram: require real order registration and an actual filled trade on BTC/TON archive.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_Batch10_CronexCci_ExecutesActualFills()
	{
		var orders = 0;
		var fills = 0;
		await Replay("2026_Cronex_CCI", (strategy, _) =>
		{
			AreEqual(25, Convert.ToInt32(strategy.Parameters["CciPeriod"].Value), "2026_Cronex_CCI: CciPeriod default.");
			AreEqual(14, Convert.ToInt32(strategy.Parameters["FastPeriod"].Value), "2026_Cronex_CCI: FastPeriod default.");
			AreEqual(25, Convert.ToInt32(strategy.Parameters["SlowPeriod"].Value), "2026_Cronex_CCI: SlowPeriod default.");
			AreEqual(true, strategy.Parameters["EnableLongEntry"].Value, "2026_Cronex_CCI: EnableLongEntry must be enabled by default.");
			AreEqual(true, strategy.Parameters["EnableShortEntry"].Value, "2026_Cronex_CCI: EnableShortEntry must be enabled by default.");
			AreEqual(true, strategy.Parameters["EnableLongExit"].Value, "2026_Cronex_CCI: EnableLongExit must be enabled by default.");
			AreEqual(true, strategy.Parameters["EnableShortExit"].Value, "2026_Cronex_CCI: EnableShortExit must be enabled by default.");
			AreEqual(TimeSpan.FromHours(4).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "CciPeriod", 6);
			SetParam(strategy, "FastPeriod", 3);
			SetParam(strategy, "SlowPeriod", 8);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m, "2026_Cronex_CCI: market order requires a positive volume.");
				if (strategy.Position != 0m && order.Side == (strategy.Position > 0m ? Sides.Sell : Sides.Buy))
					IsTrue(order.Volume <= Math.Abs(strategy.Position) + strategy.Volume,
						"2026_Cronex_CCI: closing volume must not exceed current position plus one entry.");
				orders++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(orders > 0 && fills > 0,
			"2026_Cronex_CCI: require real order registration and an actual filled trade on BTC/TON archive.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_Batch10_DigVariation_ExecutesActualFills()
	{
		var orders = 0;
		var fills = 0;
		await Replay("2031_DigVariation", (strategy, _) =>
		{
			AreEqual(20, Convert.ToInt32(strategy.Parameters["Period"].Value), "2031_DigVariation: Period default.");
			AreEqual(0, Convert.ToInt32(strategy.Parameters["CooldownPeriod"].Value), "2031_DigVariation: CooldownPeriod default.");
			AreEqual(true, strategy.Parameters["BuyOpen"].Value, "2031_DigVariation: BuyOpen must be enabled by default.");
			AreEqual(true, strategy.Parameters["SellOpen"].Value, "2031_DigVariation: SellOpen must be enabled by default.");
			AreEqual(true, strategy.Parameters["BuyClose"].Value, "2031_DigVariation: BuyClose must be enabled by default.");
			AreEqual(true, strategy.Parameters["SellClose"].Value, "2031_DigVariation: SellClose must be enabled by default.");

			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "Period", 5);
			SetParam(strategy, "CooldownPeriod", 0);
			SetParam(strategy, "StopLoss", 100m);
			SetParam(strategy, "TakeProfit", 200m);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m, "2031_DigVariation: market order requires a positive volume.");
				if (strategy.Position != 0m && order.Side == (strategy.Position > 0m ? Sides.Sell : Sides.Buy))
					IsTrue(order.Volume <= Math.Abs(strategy.Position) + strategy.Volume,
						"2031_DigVariation: closing volume must not exceed current position plus one entry.");
				orders++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(orders > 0 && fills > 0,
			"2031_DigVariation: require real order registration and an actual filled trade on BTC/TON archive.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_Batch10_EmaCrossoverSignal_ExecutesActualFills()
	{
		var orders = 0;
		var fills = 0;
		await Replay("2033_Ema_Crossover_Signal", (strategy, _) =>
		{
			AreEqual(5, Convert.ToInt32(strategy.Parameters["FastPeriod"].Value), "2033_Ema_Crossover_Signal: FastPeriod default.");
			AreEqual(13, Convert.ToInt32(strategy.Parameters["SlowPeriod"].Value), "2033_Ema_Crossover_Signal: SlowPeriod default.");
			AreEqual(true, strategy.Parameters["AllowBuyOpen"].Value, "2033_Ema_Crossover_Signal: AllowBuyOpen must be enabled by default.");
			AreEqual(true, strategy.Parameters["AllowSellOpen"].Value, "2033_Ema_Crossover_Signal: AllowSellOpen must be enabled by default.");
			AreEqual(true, strategy.Parameters["AllowBuyClose"].Value, "2033_Ema_Crossover_Signal: AllowBuyClose must be enabled by default.");
			AreEqual(true, strategy.Parameters["AllowSellClose"].Value, "2033_Ema_Crossover_Signal: AllowSellClose must be enabled by default.");
			AreEqual(TimeSpan.FromHours(4).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "FastPeriod", 3);
			SetParam(strategy, "SlowPeriod", 8);
			SetParam(strategy, "StopLossTicks", 1000m);
			SetParam(strategy, "TakeProfitTicks", 2000m);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m, "2033_Ema_Crossover_Signal: market order requires a positive volume.");
				if (strategy.Position != 0m && order.Side == (strategy.Position > 0m ? Sides.Sell : Sides.Buy))
					IsTrue(order.Volume <= Math.Abs(strategy.Position) + strategy.Volume,
						"2033_Ema_Crossover_Signal: closing volume must not exceed current position plus one entry.");
				orders++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(orders > 0 && fills > 0,
			"2033_Ema_Crossover_Signal: require real order registration and an actual filled trade on BTC/TON archive.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_Batch10_MovingUpMA_ExecutesActualFills()
	{
		var orders = 0;
		var fills = 0;
		await Replay("2038_MovingUp_MA", (strategy, _) =>
		{
			AreEqual(13, Convert.ToInt32(strategy.Parameters["FastLength"].Value), "2038_MovingUp_MA: FastLength default.");
			AreEqual(21, Convert.ToInt32(strategy.Parameters["SlowLength"].Value), "2038_MovingUp_MA: SlowLength default.");
			AreEqual(true, strategy.Parameters["UseStopLoss"].Value, "2038_MovingUp_MA: UseStopLoss must be enabled by default.");
			AreEqual(true, strategy.Parameters["UseTakeProfit"].Value, "2038_MovingUp_MA: UseTakeProfit must be enabled by default.");
			AreEqual(TimeSpan.FromHours(1).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "FastLength", 3);
			SetParam(strategy, "SlowLength", 8);
			SetParam(strategy, "StopLoss", 100m);
			SetParam(strategy, "TakeProfit", 200m);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m, "2038_MovingUp_MA: market order requires a positive volume.");
				if (strategy.Position != 0m && order.Side == (strategy.Position > 0m ? Sides.Sell : Sides.Buy))
					IsTrue(order.Volume <= Math.Abs(strategy.Position) + strategy.Volume,
						"2038_MovingUp_MA: closing volume must not exceed current position plus one entry.");
				orders++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(orders > 0 && fills > 0,
			"2038_MovingUp_MA: require real order registration and an actual filled trade on BTC/TON archive.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_Batch10_Fast2Crossover_ExecutesActualFills()
	{
		var orders = 0;
		var fills = 0;
		await Replay("2039_Fast2_Crossover", (strategy, _) =>
		{
			AreEqual(3, Convert.ToInt32(strategy.Parameters["FastLength"].Value), "2039_Fast2_Crossover: FastLength default.");
			AreEqual(9, Convert.ToInt32(strategy.Parameters["SlowLength"].Value), "2039_Fast2_Crossover: SlowLength default.");

			AreEqual(TimeSpan.FromHours(8).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "FastLength", 3);
			SetParam(strategy, "SlowLength", 6);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m, "2039_Fast2_Crossover: market order requires a positive volume.");
				if (strategy.Position != 0m && order.Side == (strategy.Position > 0m ? Sides.Sell : Sides.Buy))
					IsTrue(order.Volume <= Math.Abs(strategy.Position) + strategy.Volume,
						"2039_Fast2_Crossover: closing volume must not exceed current position plus one entry.");
				orders++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(orders > 0 && fills > 0,
			"2039_Fast2_Crossover: require real order registration and an actual filled trade on BTC/TON archive.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_Batch10_FineTuningMA_ExecutesActualFills()
	{
		var orders = 0;
		var fills = 0;
		await Replay("2043_Fine_Tuning_MA", (strategy, _) =>
		{
			AreEqual(10, Convert.ToInt32(strategy.Parameters["MaLength"].Value), "2043_Fine_Tuning_MA: MaLength default.");

			AreEqual(TimeSpan.FromHours(4).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "MaLength", 5);
			SetParam(strategy, "StopLossPercent", 10m);
			SetParam(strategy, "TakeProfitPercent", 10m);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m, "2043_Fine_Tuning_MA: market order requires a positive volume.");
				if (strategy.Position != 0m && order.Side == (strategy.Position > 0m ? Sides.Sell : Sides.Buy))
					IsTrue(order.Volume <= Math.Abs(strategy.Position) + strategy.Volume,
						"2043_Fine_Tuning_MA: closing volume must not exceed current position plus one entry.");
				orders++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(orders > 0 && fills > 0,
			"2043_Fine_Tuning_MA: require real order registration and an actual filled trade on BTC/TON archive.");
	}


	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_Batch11_CorrectedAverageBreakout_ReplaysCsharpAndPython()
	{
		var orders = 0;
		var buyOrders = 0;
		var fills = 0;
		await Replay("2044_CorrectedAverage_Breakout", (strategy, _) =>
		{
			AreEqual(12m, Convert.ToDecimal(strategy.Parameters["Length"].Value), "2044_CorrectedAverage_Breakout: default Length must match README.");
			AreEqual(300m, Convert.ToDecimal(strategy.Parameters["LevelPoints"].Value), "2044_CorrectedAverage_Breakout: default LevelPoints must match README.");

			AreEqual(true, strategy.Parameters["EnableLong"].Value, "EnableLong must be enabled by default.");
			AreEqual(true, strategy.Parameters["EnableShort"].Value, "EnableShort must be enabled by default.");
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "Length", 5m);
			SetParam(strategy, "LevelPoints", 2m);
			SetParam(strategy, "StopLossPoints", 100m);
			SetParam(strategy, "TakeProfitPoints", 200m);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m, "2044_CorrectedAverage_Breakout: every registered order needs positive volume.");
				if (order.Side == Sides.Buy)
					buyOrders++;
				if (strategy.Position != 0m && order.Side == (strategy.Position > 0m ? Sides.Sell : Sides.Buy))
					IsTrue(order.Volume <= Math.Abs(strategy.Position) + strategy.Volume,
						"2044_CorrectedAverage_Breakout: exit/reverse order must not exceed filled position plus entry.");
				orders++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(orders > 0 && fills > 0,
			"2044_CorrectedAverage_Breakout: require real archive-backed order registration and trade fill.");

	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_Batch11_ForceDiverSign_ReplaysCsharpAndPython()
	{
		var orders = 0;
		var buyOrders = 0;
		var fills = 0;
		await Replay("2045_Force_DiverSign", (strategy, _) =>
		{
			AreEqual(3m, Convert.ToDecimal(strategy.Parameters["Period1"].Value), "2045_Force_DiverSign: default Period1 must match README.");
			AreEqual(7m, Convert.ToDecimal(strategy.Parameters["Period2"].Value), "2045_Force_DiverSign: default Period2 must match README.");


			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "Period1", 2m);
			SetParam(strategy, "Period2", 4m);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m, "2045_Force_DiverSign: every registered order needs positive volume.");
				if (order.Side == Sides.Buy)
					buyOrders++;
				if (strategy.Position != 0m && order.Side == (strategy.Position > 0m ? Sides.Sell : Sides.Buy))
					IsTrue(order.Volume <= Math.Abs(strategy.Position) + strategy.Volume,
						"2045_Force_DiverSign: exit/reverse order must not exceed filled position plus entry.");
				orders++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(orders > 0 && fills > 0,
			"2045_Force_DiverSign: require real archive-backed order registration and trade fill.");

	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_Batch11_ForecastOscillator_ReplaysCsharpAndPython()
	{
		var orders = 0;
		var buyOrders = 0;
		var fills = 0;
		await Replay("2046_Forecast_Oscillator", (strategy, _) =>
		{
			AreEqual(15m, Convert.ToDecimal(strategy.Parameters["Length"].Value), "2046_Forecast_Oscillator: default Length must match README.");
			AreEqual(3m, Convert.ToDecimal(strategy.Parameters["T3Period"].Value), "2046_Forecast_Oscillator: default T3Period must match README.");

			AreEqual(true, strategy.Parameters["BuyOpen"].Value, "BuyOpen must be enabled by default.");
			AreEqual(true, strategy.Parameters["SellOpen"].Value, "SellOpen must be enabled by default.");
			AreEqual(true, strategy.Parameters["BuyClose"].Value, "BuyClose must be enabled by default.");
			AreEqual(true, strategy.Parameters["SellClose"].Value, "SellClose must be enabled by default.");
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "Length", 6m);
			SetParam(strategy, "T3Period", 3m);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m, "2046_Forecast_Oscillator: every registered order needs positive volume.");
				if (order.Side == Sides.Buy)
					buyOrders++;
				if (strategy.Position != 0m && order.Side == (strategy.Position > 0m ? Sides.Sell : Sides.Buy))
					IsTrue(order.Volume <= Math.Abs(strategy.Position) + strategy.Volume,
						"2046_Forecast_Oscillator: exit/reverse order must not exceed filled position plus entry.");
				orders++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(orders > 0 && fills > 0,
			"2046_Forecast_Oscillator: require real archive-backed order registration and trade fill.");

	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_Batch11_VininITrend_ReplaysCsharpAndPython()
	{
		var orders = 0;
		var buyOrders = 0;
		var fills = 0;
		await Replay("2049_VininI_Trend", (strategy, _) =>
		{
			AreEqual(20m, Convert.ToDecimal(strategy.Parameters["Period"].Value), "2049_VininI_Trend: default Period must match README.");
			AreEqual(0m, Convert.ToDecimal(strategy.Parameters["Mode"].Value), "2049_VininI_Trend: default Mode must match README.");


			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "Period", 5m);
			SetParam(strategy, "Mode", 1m);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m, "2049_VininI_Trend: every registered order needs positive volume.");
				if (order.Side == Sides.Buy)
					buyOrders++;
				if (strategy.Position != 0m && order.Side == (strategy.Position > 0m ? Sides.Sell : Sides.Buy))
					IsTrue(order.Volume <= Math.Abs(strategy.Position) + strategy.Volume,
						"2049_VininI_Trend: exit/reverse order must not exceed filled position plus entry.");
				orders++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(orders > 0 && fills > 0,
			"2049_VininI_Trend: require real archive-backed order registration and trade fill.");

	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_Batch11_ColorHmaReversal_ReplaysCsharpAndPython()
	{
		var orders = 0;
		var buyOrders = 0;
		var fills = 0;
		await Replay("2054_Color_HMA_Reversal", (strategy, _) =>
		{
			AreEqual(13m, Convert.ToDecimal(strategy.Parameters["HmaPeriod"].Value), "2054_Color_HMA_Reversal: default HmaPeriod must match README.");

			AreEqual(true, strategy.Parameters["BuyOpen"].Value, "BuyOpen must be enabled by default.");
			AreEqual(true, strategy.Parameters["SellOpen"].Value, "SellOpen must be enabled by default.");
			AreEqual(true, strategy.Parameters["BuyClose"].Value, "BuyClose must be enabled by default.");
			AreEqual(true, strategy.Parameters["SellClose"].Value, "SellClose must be enabled by default.");
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "HmaPeriod", 6m);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m, "2054_Color_HMA_Reversal: every registered order needs positive volume.");
				if (order.Side == Sides.Buy)
					buyOrders++;
				if (strategy.Position != 0m && order.Side == (strategy.Position > 0m ? Sides.Sell : Sides.Buy))
					IsTrue(order.Volume <= Math.Abs(strategy.Position) + strategy.Volume,
						"2054_Color_HMA_Reversal: exit/reverse order must not exceed filled position plus entry.");
				orders++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(orders > 0 && fills > 0,
			"2054_Color_HMA_Reversal: require real archive-backed order registration and trade fill.");

	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_Batch11_Hvr_ReplaysCsharpAndPython()
	{
		var orders = 0;
		var buyOrders = 0;
		var fills = 0;
		await Replay("2055_HVR", (strategy, _) =>
		{
			AreEqual(6m, Convert.ToDecimal(strategy.Parameters["ShortPeriod"].Value), "2055_HVR: default ShortPeriod must match README.");
			AreEqual(100m, Convert.ToDecimal(strategy.Parameters["LongPeriod"].Value), "2055_HVR: default LongPeriod must match README.");
			AreEqual(1m, Convert.ToDecimal(strategy.Parameters["RatioThreshold"].Value));

			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "ShortPeriod", 4m);
			SetParam(strategy, "LongPeriod", 10m);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m, "2055_HVR: every registered order needs positive volume.");
				if (order.Side == Sides.Buy)
					buyOrders++;
				if (strategy.Position != 0m && order.Side == (strategy.Position > 0m ? Sides.Sell : Sides.Buy))
					IsTrue(order.Volume <= Math.Abs(strategy.Position) + strategy.Volume,
						"2055_HVR: exit/reverse order must not exceed filled position plus entry.");
				orders++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(orders > 0 && fills > 0,
			"2055_HVR: require real archive-backed order registration and trade fill.");

	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_Batch11_DecEMA_ReplaysCsharpAndPython()
	{
		var orders = 0;
		var buyOrders = 0;
		var fills = 0;
		await Replay("2056_DecEMA", (strategy, _) =>
		{
			AreEqual(3m, Convert.ToDecimal(strategy.Parameters["EmaPeriod"].Value), "2056_DecEMA: default EmaPeriod must match README.");
			AreEqual(15m, Convert.ToDecimal(strategy.Parameters["Length"].Value), "2056_DecEMA: default Length must match README.");

			AreEqual(true, strategy.Parameters["BuyPosOpen"].Value, "BuyPosOpen must be enabled by default.");
			AreEqual(true, strategy.Parameters["SellPosOpen"].Value, "SellPosOpen must be enabled by default.");
			AreEqual(true, strategy.Parameters["BuyPosClose"].Value, "BuyPosClose must be enabled by default.");
			AreEqual(true, strategy.Parameters["SellPosClose"].Value, "SellPosClose must be enabled by default.");
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "EmaPeriod", 2m);
			SetParam(strategy, "Length", 3m);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m, "2056_DecEMA: every registered order needs positive volume.");
				if (order.Side == Sides.Buy)
					buyOrders++;
				if (strategy.Position != 0m && order.Side == (strategy.Position > 0m ? Sides.Sell : Sides.Buy))
					IsTrue(order.Volume <= Math.Abs(strategy.Position) + strategy.Volume,
						"2056_DecEMA: exit/reverse order must not exceed filled position plus entry.");
				orders++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(orders > 0 && fills > 0,
			"2056_DecEMA: require real archive-backed order registration and trade fill.");

	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_Batch11_BandsPrice_ReplaysCsharpAndPython()
	{
		var orders = 0;
		var buyOrders = 0;
		var fills = 0;
		await Replay("2057_BandsPrice", (strategy, _) =>
		{
			AreEqual(100m, Convert.ToDecimal(strategy.Parameters["BandsPeriod"].Value), "2057_BandsPrice: default BandsPeriod must match README.");
			AreEqual(5m, Convert.ToDecimal(strategy.Parameters["Smooth"].Value), "2057_BandsPrice: default Smooth must match README.");
			AreEqual(25m, Convert.ToDecimal(strategy.Parameters["UpLevel"].Value), "2057_BandsPrice: default UpLevel must match README.");
			AreEqual(-25m, Convert.ToDecimal(strategy.Parameters["DnLevel"].Value), "2057_BandsPrice: default DnLevel must match README.");

			AreEqual(true, strategy.Parameters["BuyOpen"].Value, "BuyOpen must be enabled by default.");
			AreEqual(true, strategy.Parameters["SellOpen"].Value, "SellOpen must be enabled by default.");
			AreEqual(true, strategy.Parameters["BuyClose"].Value, "BuyClose must be enabled by default.");
			AreEqual(true, strategy.Parameters["SellClose"].Value, "SellClose must be enabled by default.");
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "BandsPeriod", 8m);
			SetParam(strategy, "Smooth", 3m);
			SetParam(strategy, "UpLevel", 5m);
			SetParam(strategy, "DnLevel", -5m);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m, "2057_BandsPrice: every registered order needs positive volume.");
				if (order.Side == Sides.Buy)
					buyOrders++;
				if (strategy.Position != 0m && order.Side == (strategy.Position > 0m ? Sides.Sell : Sides.Buy))
					IsTrue(order.Volume <= Math.Abs(strategy.Position) + strategy.Volume,
						"2057_BandsPrice: exit/reverse order must not exceed filled position plus entry.");
				orders++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(orders > 0 && fills > 0,
			"2057_BandsPrice: require real archive-backed order registration and trade fill.");

	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_Batch11_KlPriceReversal_ReplaysCsharpAndPython()
	{
		var orders = 0;
		var buyOrders = 0;
		var fills = 0;
		await Replay("2058_KlPrice_Reversal", (strategy, _) =>
		{
			AreEqual(100m, Convert.ToDecimal(strategy.Parameters["PriceMaLength"].Value), "2058_KlPrice_Reversal: default PriceMaLength must match README.");
			AreEqual(20m, Convert.ToDecimal(strategy.Parameters["AtrLength"].Value), "2058_KlPrice_Reversal: default AtrLength must match README.");

			AreEqual(true, strategy.Parameters["EnableBuy"].Value, "EnableBuy must be enabled by default.");
			AreEqual(true, strategy.Parameters["EnableSell"].Value, "EnableSell must be enabled by default.");
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "PriceMaLength", 6m);
			SetParam(strategy, "AtrLength", 4m);
			SetParam(strategy, "UpLevel", 5m);
			SetParam(strategy, "DownLevel", -5m);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m, "2058_KlPrice_Reversal: every registered order needs positive volume.");
				if (order.Side == Sides.Buy)
					buyOrders++;
				if (strategy.Position != 0m && order.Side == (strategy.Position > 0m ? Sides.Sell : Sides.Buy))
					IsTrue(order.Volume <= Math.Abs(strategy.Position) + strategy.Volume,
						"2058_KlPrice_Reversal: exit/reverse order must not exceed filled position plus entry.");
				orders++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(orders > 0 && fills > 0,
			"2058_KlPrice_Reversal: require real archive-backed order registration and trade fill.");

	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_Batch11_SimpleTrailingStop_ReplaysCsharpAndPython()
	{
		var orders = 0;
		var buyOrders = 0;
		var fills = 0;
		await Replay("2059_Simple_Trailing_Stop", (strategy, _) =>
		{
			AreEqual(100m, Convert.ToDecimal(strategy.Parameters["TrailPoints"].Value), "2059_Simple_Trailing_Stop: default TrailPoints must match README.");


			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "TrailPoints", 1m);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m, "2059_Simple_Trailing_Stop: every registered order needs positive volume.");
				if (order.Side == Sides.Buy)
					buyOrders++;
				if (strategy.Position != 0m && order.Side == (strategy.Position > 0m ? Sides.Sell : Sides.Buy))
					IsTrue(order.Volume <= Math.Abs(strategy.Position) + strategy.Volume,
						"2059_Simple_Trailing_Stop: exit/reverse order must not exceed filled position plus entry.");
				orders++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(orders > 0 && fills > 0,
			"2059_Simple_Trailing_Stop: require real archive-backed order registration and trade fill.");
		AreEqual(1, buyOrders, "One-shot trailing example must submit exactly one long entry.");
	}


	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_Batch12_ITrend_ArchiveReplaysBothLanguages()
	{
		var orders = 0;
		var fills = 0;
		await Replay("2061_I_Trend", (strategy, _) =>
		{
			AreEqual(13m, Convert.ToDecimal(strategy.Parameters["MaPeriod"].Value), "2061_I_Trend: MaPeriod default.");
			AreEqual(20m, Convert.ToDecimal(strategy.Parameters["BbPeriod"].Value), "2061_I_Trend: BbPeriod default.");
			AreEqual(0m, Convert.ToDecimal(strategy.Parameters["PriceType"].Value), "2061_I_Trend: PriceType default.");
			AreEqual(0m, Convert.ToDecimal(strategy.Parameters["BbMode"].Value), "2061_I_Trend: BbMode default.");


			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "MaPeriod", 4m);
			SetParam(strategy, "BbPeriod", 6m);
			SetParam(strategy, "BbDeviation", 1m);
			SetParam(strategy, "PriceType", 0m);
			SetParam(strategy, "BbMode", 0m);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m, "2061_I_Trend: order volume must be positive.");
				orders++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(orders > 0 && fills > 0,
			"2061_I_Trend: real BTC/TON replay must contain actual orders and fills.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_Batch12_AnchoredMomentum_ArchiveReplaysBothLanguages()
	{
		var orders = 0;
		var fills = 0;
		await Replay("2062_Anchored_Momentum_Breakout", (strategy, _) =>
		{
			AreEqual(34m, Convert.ToDecimal(strategy.Parameters["SmaPeriod"].Value), "2062_Anchored_Momentum_Breakout: SmaPeriod default.");
			AreEqual(20m, Convert.ToDecimal(strategy.Parameters["EmaPeriod"].Value), "2062_Anchored_Momentum_Breakout: EmaPeriod default.");
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value), "2062_Anchored_Momentum_Breakout: StopLossPercent default.");
			AreEqual(4m, Convert.ToDecimal(strategy.Parameters["TakeProfitPercent"].Value), "2062_Anchored_Momentum_Breakout: TakeProfitPercent default.");


			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "SmaPeriod", 7m);
			SetParam(strategy, "EmaPeriod", 3m);
			SetParam(strategy, "StopLossPercent", 10m);
			SetParam(strategy, "TakeProfitPercent", 10m);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m, "2062_Anchored_Momentum_Breakout: order volume must be positive.");
				orders++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(orders > 0 && fills > 0,
			"2062_Anchored_Momentum_Breakout: real BTC/TON replay must contain actual orders and fills.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_Batch12_IchimokuOscillator_ArchiveReplaysBothLanguages()
	{
		var orders = 0;
		var fills = 0;
		await Replay("2063_Ichimoku_Oscillator", (strategy, _) =>
		{
			AreEqual(9m, Convert.ToDecimal(strategy.Parameters["TenkanPeriod"].Value), "2063_Ichimoku_Oscillator: TenkanPeriod default.");
			AreEqual(26m, Convert.ToDecimal(strategy.Parameters["KijunPeriod"].Value), "2063_Ichimoku_Oscillator: KijunPeriod default.");
			AreEqual(52m, Convert.ToDecimal(strategy.Parameters["SenkouSpanBPeriod"].Value), "2063_Ichimoku_Oscillator: SenkouSpanBPeriod default.");
			AreEqual(7m, Convert.ToDecimal(strategy.Parameters["SmoothingPeriod"].Value), "2063_Ichimoku_Oscillator: SmoothingPeriod default.");
			AreEqual(true, strategy.Parameters["EnableStopLoss"].Value, "2063_Ichimoku_Oscillator: EnableStopLoss enabled.");

			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "TenkanPeriod", 4m);
			SetParam(strategy, "KijunPeriod", 6m);
			SetParam(strategy, "SenkouSpanBPeriod", 9m);
			SetParam(strategy, "SmoothingPeriod", 3m);
			SetParam(strategy, "StopLossPercent", 10m);
			SetParam(strategy, "TakeProfitPercent", 10m);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m, "2063_Ichimoku_Oscillator: order volume must be positive.");
				orders++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(orders > 0 && fills > 0,
			"2063_Ichimoku_Oscillator: real BTC/TON replay must contain actual orders and fills.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_Batch12_HullTrendOsma_ArchiveReplaysBothLanguages()
	{
		var orders = 0;
		var fills = 0;
		await Replay("2064_Hull_Trend_OSMA", (strategy, _) =>
		{
			AreEqual(20m, Convert.ToDecimal(strategy.Parameters["HullPeriod"].Value), "2064_Hull_Trend_OSMA: HullPeriod default.");
			AreEqual(5m, Convert.ToDecimal(strategy.Parameters["SignalPeriod"].Value), "2064_Hull_Trend_OSMA: SignalPeriod default.");
			AreEqual(3m, Convert.ToDecimal(strategy.Parameters["TakeProfit"].Value), "2064_Hull_Trend_OSMA: TakeProfit default.");
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["StopLoss"].Value), "2064_Hull_Trend_OSMA: StopLoss default.");

			AreEqual(TimeSpan.FromHours(8).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "HullPeriod", 7m);
			SetParam(strategy, "SignalPeriod", 3m);
			SetParam(strategy, "TakeProfit", 200m);
			SetParam(strategy, "StopLoss", 100m);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m, "2064_Hull_Trend_OSMA: order volume must be positive.");
				orders++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(orders > 0 && fills > 0,
			"2064_Hull_Trend_OSMA: real BTC/TON replay must contain actual orders and fills.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_Batch12_ZerolagHLR_ArchiveReplaysBothLanguages()
	{
		var orders = 0;
		var fills = 0;
		await Replay("2065_Color_Zerolag_HLR", (strategy, _) =>
		{
			AreEqual(15m, Convert.ToDecimal(strategy.Parameters["Smoothing"].Value), "2065_Color_Zerolag_HLR: Smoothing default.");
			AreEqual(8m, Convert.ToDecimal(strategy.Parameters["HlrPeriod1"].Value), "2065_Color_Zerolag_HLR: HlrPeriod1 default.");
			AreEqual(21m, Convert.ToDecimal(strategy.Parameters["HlrPeriod2"].Value), "2065_Color_Zerolag_HLR: HlrPeriod2 default.");
			AreEqual(34m, Convert.ToDecimal(strategy.Parameters["HlrPeriod3"].Value), "2065_Color_Zerolag_HLR: HlrPeriod3 default.");
			AreEqual(55m, Convert.ToDecimal(strategy.Parameters["HlrPeriod4"].Value), "2065_Color_Zerolag_HLR: HlrPeriod4 default.");
			AreEqual(89m, Convert.ToDecimal(strategy.Parameters["HlrPeriod5"].Value), "2065_Color_Zerolag_HLR: HlrPeriod5 default.");
			AreEqual(true, strategy.Parameters["BuyPosOpen"].Value, "2065_Color_Zerolag_HLR: BuyPosOpen enabled.");
			AreEqual(true, strategy.Parameters["SellPosOpen"].Value, "2065_Color_Zerolag_HLR: SellPosOpen enabled.");
			AreEqual(true, strategy.Parameters["BuyPosClose"].Value, "2065_Color_Zerolag_HLR: BuyPosClose enabled.");
			AreEqual(true, strategy.Parameters["SellPosClose"].Value, "2065_Color_Zerolag_HLR: SellPosClose enabled.");

			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "Smoothing", 3m);
			SetParam(strategy, "HlrPeriod1", 3m);
			SetParam(strategy, "HlrPeriod2", 4m);
			SetParam(strategy, "HlrPeriod3", 5m);
			SetParam(strategy, "HlrPeriod4", 6m);
			SetParam(strategy, "HlrPeriod5", 7m);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m, "2065_Color_Zerolag_HLR: order volume must be positive.");
				orders++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(orders > 0 && fills > 0,
			"2065_Color_Zerolag_HLR: real BTC/TON replay must contain actual orders and fills.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_Batch12_AmmaTrend_ArchiveReplaysBothLanguages()
	{
		var orders = 0;
		var fills = 0;
		await Replay("2066_AMMA_Trend", (strategy, _) =>
		{
			AreEqual(25m, Convert.ToDecimal(strategy.Parameters["MaPeriod"].Value), "2066_AMMA_Trend: MaPeriod default.");
			AreEqual(true, strategy.Parameters["AllowLongEntry"].Value, "2066_AMMA_Trend: AllowLongEntry enabled.");
			AreEqual(true, strategy.Parameters["AllowShortEntry"].Value, "2066_AMMA_Trend: AllowShortEntry enabled.");
			AreEqual(true, strategy.Parameters["AllowLongExit"].Value, "2066_AMMA_Trend: AllowLongExit enabled.");
			AreEqual(true, strategy.Parameters["AllowShortExit"].Value, "2066_AMMA_Trend: AllowShortExit enabled.");

			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "MaPeriod", 5m);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m, "2066_AMMA_Trend: order volume must be positive.");
				orders++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(orders > 0 && fills > 0,
			"2066_AMMA_Trend: real BTC/TON replay must contain actual orders and fills.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_Batch12_DailyBreakpoint_ArchiveReplaysBothLanguages()
	{
		var orders = 0;
		var fills = 0;
		await Replay("2071_Daily_Breakpoint", (strategy, _) =>
		{
			AreEqual(30m, Convert.ToDecimal(strategy.Parameters["BreakPoint"].Value), "2071_Daily_Breakpoint: BreakPoint default.");
			AreEqual(5m, Convert.ToDecimal(strategy.Parameters["LastBarMin"].Value), "2071_Daily_Breakpoint: LastBarMin default.");
			AreEqual(100m, Convert.ToDecimal(strategy.Parameters["LastBarMax"].Value), "2071_Daily_Breakpoint: LastBarMax default.");
			AreEqual(100m, Convert.ToDecimal(strategy.Parameters["StopLoss"].Value), "2071_Daily_Breakpoint: StopLoss default.");
			AreEqual(200m, Convert.ToDecimal(strategy.Parameters["TakeProfit"].Value), "2071_Daily_Breakpoint: TakeProfit default.");
			AreEqual(0m, Convert.ToDecimal(strategy.Parameters["TrailingStart"].Value), "2071_Daily_Breakpoint: TrailingStart default.");
			AreEqual(0m, Convert.ToDecimal(strategy.Parameters["TrailingStop"].Value), "2071_Daily_Breakpoint: TrailingStop default.");
			AreEqual(1m, Convert.ToDecimal(strategy.Parameters["TrailingStep"].Value), "2071_Daily_Breakpoint: TrailingStep default.");


			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "BreakPoint", 1m);
			SetParam(strategy, "LastBarMin", 0m);
			SetParam(strategy, "LastBarMax", 1000000m);
			SetParam(strategy, "StopLoss", 100m);
			SetParam(strategy, "TakeProfit", 200m);
			SetParam(strategy, "TrailingStop", 0m);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m, "2071_Daily_Breakpoint: order volume must be positive.");
				orders++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(orders > 0 && fills > 0,
			"2071_Daily_Breakpoint: real BTC/TON replay must contain actual orders and fills.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_Batch12_InstantaneousTrendFilter_ArchiveReplaysBothLanguages()
	{
		var orders = 0;
		var fills = 0;
		await Replay("2072_InstantaneousTrendFilter", (strategy, _) =>
		{
			AreEqual(0.07m, Convert.ToDecimal(strategy.Parameters["Alpha"].Value), "2072_InstantaneousTrendFilter: Alpha default.");


			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "Alpha", 0.3m);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m, "2072_InstantaneousTrendFilter: order volume must be positive.");
				orders++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(orders > 0 && fills > 0,
			"2072_InstantaneousTrendFilter: real BTC/TON replay must contain actual orders and fills.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_Batch12_ColorZerolagJjrsx_ArchiveReplaysBothLanguages()
	{
		var orders = 0;
		var fills = 0;
		await Replay("2076_ColorZerolagJJRSX", (strategy, _) =>
		{
			AreEqual(8m, Convert.ToDecimal(strategy.Parameters["FastPeriod"].Value), "2076_ColorZerolagJJRSX: FastPeriod default.");
			AreEqual(21m, Convert.ToDecimal(strategy.Parameters["SlowPeriod"].Value), "2076_ColorZerolagJJRSX: SlowPeriod default.");
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["StopLoss"].Value), "2076_ColorZerolagJJRSX: StopLoss default.");
			AreEqual(3m, Convert.ToDecimal(strategy.Parameters["TakeProfit"].Value), "2076_ColorZerolagJJRSX: TakeProfit default.");
			AreEqual(true, strategy.Parameters["BuyOpen"].Value, "2076_ColorZerolagJJRSX: BuyOpen enabled.");
			AreEqual(true, strategy.Parameters["SellOpen"].Value, "2076_ColorZerolagJJRSX: SellOpen enabled.");
			AreEqual(true, strategy.Parameters["BuyClose"].Value, "2076_ColorZerolagJJRSX: BuyClose enabled.");
			AreEqual(true, strategy.Parameters["SellClose"].Value, "2076_ColorZerolagJJRSX: SellClose enabled.");

			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "FastPeriod", 3m);
			SetParam(strategy, "SlowPeriod", 8m);
			SetParam(strategy, "StopLoss", 100m);
			SetParam(strategy, "TakeProfit", 200m);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m, "2076_ColorZerolagJJRSX: order volume must be positive.");
				orders++;
			};
			strategy.Trades.TradeAdded += _ => fills++;
		}, replayDuration: TimeSpan.FromDays(14));
		IsTrue(orders > 0 && fills > 0,
			"2076_ColorZerolagJJRSX: real BTC/TON replay must contain actual orders and fills.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task Point1Part05_Batch12_VirtualTrailingStop_OnlyClosesExternallySeededPosition()
	{
		var seeded = false;
		var entries = 0;
		var exits = 0;
		var exitFills = 0;
		await Replay("2075_Virtual_Trailing_Stop", (strategy, _) =>
		{
			foreach (var name in new[] { "StopLoss", "TakeProfit", "TrailingStop",
				"TrailingStart", "TrailingStep", "CandleType" })
				IsTrue(strategy.Parameters.ContainsKey(name), "Missing virtual trailing parameter: " + name);
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "StopLoss", 1m);
			SetParam(strategy, "TakeProfit", 1m);
			SetParam(strategy, "TrailingStop", 1m);
			SetParam(strategy, "TrailingStart", 0m);
			SetParam(strategy, "TrailingStep", 1m);
			strategy.OrderRegistering += order =>
			{
				IsTrue(order.Volume > 0m);
				if (order.Side == Sides.Buy)
				{
					IsTrue(seeded, "Virtual manager must never create an unseeded entry.");
					entries++;
				}
				else
				{
					IsTrue(strategy.Position > 0m, "Virtual stop needs an externally filled long.");
					AreEqual(strategy.Position, order.Volume,
						"Virtual stop must close exactly the actual filled position.");
					exits++;
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
		AreEqual(1, entries, "Only the test itself may seed the entry.");
		IsTrue(exits > 0 && exitFills > 0,
			"Virtual manager must register and fill an actual protective sell after the seeded long.");
	}

}

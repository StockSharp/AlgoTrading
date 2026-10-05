namespace StockSharp.Tests;

using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.Logging;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Storages;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Charting;
using StockSharp.Configuration;
using StockSharp.MatchingEngine;
using StockSharp.Messages;

using DisplayAttribute = System.ComponentModel.DataAnnotations.DisplayAttribute;

public abstract partial class StrategyTests
{
	[TestMethod]
	[TestCategory("Shard01")]
	public async Task S0001_MaCrossover()
	{
		var recorder = new OrderTraceRecorder();
		Strategy strategy = null;

		await Replay("0001_MA_CrossOver", (current, _) =>
		{
			strategy = current;
			SetParam(current, "FastLength", 5);
			SetParam(current, "SlowLength", 20);
			SetParam(current, "StopLossPercent", 100m);
			recorder.Attach(current);
		});

		recorder.AssertReverses(strategy.Volume);
	}

	[TestMethod]
	[TestCategory("Shard04")]
	public Task S0020_MomentumPercentage()
		// Select the other real archive instrument; all published strategy parameters stay unchanged.
		=> Replay("0020_Momentum_Percentage", (strategy, secondary) => strategy.Security = secondary);

	[TestMethod]
	[TestCategory("Shard05")]
	public Task S0029_MaDeviation()
		// Select the other real archive instrument; published strategy parameters stay unchanged.
		=> Replay("0029_MA_Deviation", (strategy, secondary) => strategy.Security = secondary);

	[TestMethod]
	[TestCategory("Shard05")]
	public Task S0037_VixTrigger()
		// Explicit two-stream mechanics fixture, not an assertion that TON is actual VIX history.
		=> Replay("0037_VIX_Trigger", (strategy, secondary) => SetParam(strategy, "VixSecurity", secondary));

	[TestMethod]
	[TestCategory("Shard02")]
	public Task S0042_IvSpike()
		// Mechanics fixture, not IV market history: TON closes stand in for the IV readings, at a spike threshold a price series reaches.
		=> Replay("0042_IV_Spike", (strategy, secondary) =>
		{
			SetParam(strategy, "IVSecurity", secondary);
			SetParam(strategy, "IVSpikeThreshold", 1.002m);
		});

	[TestMethod]
	[TestCategory("Shard01")]
	public async Task S0201_VwapWilliamsR()
	{
		var tightStop = new OrderTraceRecorder();
		var wideStop = new OrderTraceRecorder();

		await Replay("0201_VWAP_Williams_R", (strategy, _) =>
		{
			SetParam(strategy, "WilliamsRPeriod", 5);
			SetParam(strategy, "CooldownBars", 1);
			SetParam(strategy, "StopLossPercent", 0.5m);
			tightStop.Attach(strategy);
		}, TimeSpan.FromDays(7));

		await Replay("0201_VWAP_Williams_R", (strategy, _) =>
		{
			SetParam(strategy, "WilliamsRPeriod", 5);
			SetParam(strategy, "CooldownBars", 1);
			SetParam(strategy, "StopLossPercent", 5m);
			wideStop.Attach(strategy);
		}, TimeSpan.FromDays(7));

		tightStop.AssertDiffersFrom(wideStop, "Changing StopLossPercent did not affect submitted orders.");
	}

	[TestMethod]
	[TestCategory("Shard01")]
	public Task S0217_PairsTrading()
		=> Replay("0217_Pairs_Trading", (s, sec2) => SetParam(s, "SecondSecurity", sec2));

	[TestMethod]
	[TestCategory("Shard03")]
	public Task S0219_StatisticalArbitrage()
		=> Replay("0219_Statistical_Arbitrage", (s, sec2) => SetParam(s, "SecondSecurity", sec2));

	/// <summary>
	/// The pair is the whole example, and Beta = 1 is the ratio it documents. Only the presence of
	/// both legs is asserted: how the two sides net out over a month depends on when positions are
	/// closed, and the audit makes no claim about that.
	/// </summary>
	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S0222_CointegrationPairs()
	{
		var recorder = new PairedOrderRecorder();
		Security primary = null;
		Security hedge = null;

		await Replay("0222_Cointegration_Pairs", (strategy, second) =>
		{
			SetParam(strategy, "Asset2", second);
			SetParam(strategy, "Beta", 1m);
			primary = strategy.Security;
			hedge = second;
			recorder.Attach(strategy);
		});

		recorder.AssertTradesBoth(primary, hedge);
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public Task S0230_DeltaNeutralArbitrage()
		=> Replay("0230_Delta_Neutral_Arbitrage", (s, sec2) =>
		{
			SetParam(s, "Asset2Security", sec2);
			SetParam(s, "Asset2Portfolio", s.Portfolio);
		});

	/// <summary>
	/// The README sells this example as a 5-minute strategy whose stops are ATR multiples and whose
	/// HmmHistoryLength is the model's history. The implementation runs hour candles, protects the
	/// position with two fixed 2-percent offsets and never looks past the last ten observations, so
	/// HmmHistoryLength only sizes a buffer. The first replay keeps the acceptance the generated row
	/// gave this example; the assertions state the declared contract and fail until it is met.
	/// </summary>
	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S0320_MacdHiddenMarkovModel()
	{
		DataType declaredCandleType = null;
		string[] parameterIds = null;

		await Replay("0320_MACD_Hidden_Markov_Model", (strategy, _) =>
		{
			IsTrue(strategy.Parameters.TryGetValue("CandleType", out var candleType), "The example has no 'CandleType' parameter.");
			declaredCandleType = (DataType)candleType.Value;
			parameterIds = strategy.Parameters.CachedKeys;
		});

		AreEqual(
			TimeSpan.FromMinutes(5).TimeFrame(), declaredCandleType,
			"README documents CandleType = 5-minute timeframe and an intraday (5m) filter.");

		var hasAtrInput = false;

		foreach (var id in parameterIds)
		{
			if (!id.ContainsIgnoreCase("Atr"))
				continue;

			hasAtrInput = true;
			break;
		}

		IsTrue(hasAtrInput,
			$"README states the stops are ATR multiples the reader adjusts, so an ATR input must exist instead of the two fixed percent offsets handed to StartProtection. Parameters: {string.Join(", ", parameterIds)}.");

		async Task<OrderTraceRecorder> Run(int hmmHistoryLength)
		{
			var recorder = new OrderTraceRecorder();

			await Replay("0320_MACD_Hidden_Markov_Model", (strategy, _) =>
			{
				SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
				SetParam(strategy, "SignalCooldownBars", 1);
				SetParam(strategy, "HmmHistoryLength", hmmHistoryLength);
				recorder.Attach(strategy);
			}, TimeSpan.FromDays(3));

			return recorder;
		}

		var shortHistory = await Run(20);
		var longHistory = await Run(200);

		shortHistory.AssertDiffersFrom(longHistory,
			"HmmHistoryLength is published as the model history and as an optimization range, so its value must change the detected state and the orders that follow from it.");
	}

	/// <summary>
	/// Changing the independently configured ATR period and stop width must affect actual orders.
	/// Keep the default replay as well as compact differential risk fixtures.
	/// </summary>
	[TestMethod]
	[TestCategory("Shard07")]
	public async Task S0343_KeltnerReinforcementLearningSignal()
	{
		// Compact channel settings and a one-bar cooldown make the breakout reachable in the bundled
		// history, so a risk parameter that is actually wired has room to show in the order trace.
		async Task<OrderTraceRecorder> Run(int atrPeriod, decimal stopLossAtr)
		{
			var recorder = new OrderTraceRecorder();

			await Replay("0343_Keltner_Reinforcement_Learning_Signal", (strategy, _) =>
			{
				SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
				SetParam(strategy, "EmaPeriod", 5);
				SetParam(strategy, "AtrMultiplier", 0.2m);
				SetParam(strategy, "CooldownBars", 1);
				SetParam(strategy, "AtrPeriod", atrPeriod);
				SetParam(strategy, "StopLossAtr", stopLossAtr);
				recorder.Attach(strategy);
			}, TimeSpan.FromDays(7));

			return recorder;
		}

		// The defaults, run the way the generated row ran them.
		await Replay("0343_Keltner_Reinforcement_Learning_Signal", null);

		var baseline = await Run(atrPeriod: 14, stopLossAtr: 5m);
		var tightStop = await Run(atrPeriod: 14, stopLossAtr: 0.01m);
		var shortAtr = await Run(atrPeriod: 2, stopLossAtr: 5m);

		// A hundredth-ATR stop and a five-ATR stop must produce different order decisions.
		tightStop.AssertDiffersFrom(baseline, "Changing StopLossAtr did not affect submitted orders: the declared ATR stop never fires.");

		// Hold the EMA period fixed to check the separately configured ATR channel width.
		shortAtr.AssertDiffersFrom(baseline, "Changing AtrPeriod did not affect submitted orders: the declared ATR period is never applied.");
	}

	/// <summary>
	/// The README describes an equal-weight basket of two crypto assets rebalanced weekly. Whatever
	/// the weights end up being, both assets have to be traded; a basket with one leg is not one.
	/// </summary>
	[TestMethod]
	[TestCategory("Shard02")]
	public async Task S0362_CryptoRebalancingPremium()
	{
		var recorder = new PairedOrderRecorder();
		Security primary = null;
		Security secondary = null;

		await Replay("0362_Crypto_Rebalancing_Premium", (strategy, second) =>
		{
			SetParam(strategy, "SecondarySecurityId", second.Id);
			primary = strategy.Security;
			secondary = second;
			recorder.Attach(strategy);
		});

		recorder.AssertTradesBoth(primary, secondary);
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public Task S0365_DispersionTrading()
		=> Replay("0365_Dispersion_Trading", (s, sec2) => SetParam(s, "Constituents", new[] { sec2 }));

	[TestMethod]
	[TestCategory("Shard01")]
	public async Task S0401_SoccerClubsArbitrage()
	{
		var recorder = new PairedOrderRecorder();
		Strategy strategy = null;
		Security secondSecurity = null;

		await Replay("0401_Soccer_Clubs_Arbitrage", (current, second) =>
		{
			strategy = current;
			secondSecurity = second;
			SetParam(current, "Security2Id", second.Id);
			recorder.Attach(current);
		});

		recorder.AssertBalanced(strategy.Security, secondSecurity);
	}

	[TestMethod]
	[TestCategory("Shard02")]
	public async Task S0402_SyntheticLendingRates()
	{
		SyntheticLendingRatesOracle oracle = null;

		await Replay("0402_Synthetic_Lending_Rates", (strategy, second) =>
		{
			// Mechanics fixture: packaged BTC and TON stand in for both rate series and both venue legs.
			oracle = SyntheticLendingRatesOracle.SpreadFixture(strategy.Security, second, strategy.Security, second, minLegTurnover: 0m);
			oracle.Attach(strategy);
		});

		oracle.AssertEveryOrderMatched();
		oracle.AssertBothLegsTraded();
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public Task S0408_VolatilityRiskPremium()
		// Mechanics fixture, not option market history: packaged TON prices stand in for the premiums of a BTC call.
		=> Replay("0408_Volatility_Risk_Premium",
			(strategy, secondary) => SetParam(strategy, "Option", VolatilityRiskPremiumOptionFixture.QuarterlyCall(secondary, strategy.Security)));

	[TestMethod]
	[TestCategory("Shard02")]
	public async Task S0410_WtibrentSpread()
	{
		var model = new WtiBrentSpreadModel();

		// Mechanics fixture: packaged BTC and TON stand in for the WTI and Brent legs, not crude oil history.
		await Replay("0410_WTIBrent_Spread", (strategy, second) =>
		{
			SetParam(strategy, "BrentSecurity", second);
			model.Attach(strategy);
		});

		TestContext.WriteLine(model.ToString());
		model.AssertMatched();
	}

	/// <summary>
	/// The README declares a grid over a predefined price range (UpperLimit 74000, LowerLimit 60000,
	/// GridCount 10). Such a grid is fixed, so this pins both halves of that contract: the declared
	/// parameters themselves, and the fact that no moving-average or ATR setting may move the lines.
	/// </summary>
	[TestMethod]
	[TestCategory("Shard01")]
	public async Task S0425_PredefinedGridIgnoresMovingAverageAndAtrSettings()
	{
		// The inputs of a dynamic grid. A predefined range does not read them, so moving them must
		// leave every line - and therefore every order - where the baseline run put it.
		static void Retune(Strategy strategy)
		{
			if (strategy.Parameters.ContainsKey("MALength"))
				SetParam(strategy, "MALength", 20);

			if (strategy.Parameters.ContainsKey("ATRLength"))
				SetParam(strategy, "ATRLength", 7);

			if (strategy.Parameters.ContainsKey("GridMultiplier"))
				SetParam(strategy, "GridMultiplier", 0.25m);
		}

		var baseline = new OrderTraceRecorder();
		var retuned = new OrderTraceRecorder();

		await Replay("0425_Grid_Bot", (strategy, _) =>
		{
			AreEqual(74000m, strategy.Parameters["UpperLimit"].Value.To<decimal>(), "The declared grid range has UpperLimit 74000.");
			AreEqual(60000m, strategy.Parameters["LowerLimit"].Value.To<decimal>(), "The declared grid range has LowerLimit 60000.");
			AreEqual(10, strategy.Parameters["GridCount"].Value.To<int>(), "The declared grid splits the range into GridCount 10 levels.");
			baseline.Attach(strategy);
		});

		await Replay("0425_Grid_Bot", (strategy, _) =>
		{
			Retune(strategy);
			retuned.Attach(strategy);
		});

		retuned.AssertSameAs(baseline);
	}

	/// <summary>
	/// The README publishes this example as a volatility-adaptive grid with a full risk block, and
	/// lists the defaults a reader is expected to tune: BaseGridSize, MaxPositions, UseVolatilityGrid,
	/// AtrLength, AtrMultiplier, UseTrailingStop, TrailingStopPercent, MaxLossPerDay, TimeBasedExit
	/// and MaxHoldingPeriod. The implementation is an RSI and two moving averages with percent stops,
	/// and exposes none of them.
	/// </summary>
	[TestMethod]
	[TestCategory("Shard02")]
	public async Task S0498_AdvancedAdaptiveGrid()
	{
		string[] parameterIds = null;

		await Replay("0498_Advanced_Adaptive_Grid", (strategy, _) =>
			parameterIds = strategy.Parameters.CachedKeys);

		string[] declared =
		[
			"BaseGridSize",
			"MaxPositions",
			"UseVolatilityGrid",
			"AtrLength",
			"AtrMultiplier",
			"UseTrailingStop",
			"TrailingStopPercent",
			"MaxLossPerDay",
			"TimeBasedExit",
			"MaxHoldingPeriod",
		];
		var missing = declared.Where(name => !parameterIds.Contains(name, StringComparer.Ordinal)).ToArray();

		AreEqual(
			0, missing.Length,
			$"README documents these parameters and their defaults, but the strategy does not expose them: {string.Join(", ", missing)}. Parameters: {string.Join(", ", parameterIds)}.");
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public Task S0526_SpotFuturesArbitrage()
		=> Replay("0526_Spot_Futures_Arbitrage", (s, sec2) =>
		{
			SetParam(s, "Spot", s.Security);
			SetParam(s, "Future", sec2);
		});

	[TestMethod]
	[TestCategory("Shard03")]
	public async Task S0703_QuantumSentimentFluxBehavior()
	{
		var recorder = new OrderTraceRecorder();

		await Replay("0703_Quantum_Sentiment_Flux_Beginners", (strategy, _) => recorder.Attach(strategy));

		recorder.AssertFirstVolume(1m);
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S0806_FootprintBehavior()
	{
		var recorder = new OrderTraceRecorder();

		await Replay("0806_Footprint", (strategy, _) => recorder.Attach(strategy));

		recorder.AssertFirstSide(Sides.Buy);
	}

	private const string NDayBreakout = "0002_NDay_Breakout";

	[TestMethod]
	[TestCategory("Shard00")]
	[DataRow(20, 20, false)]
	[DataRow(10, 30, false)]
	[DataRow(5, 8, true)]
	public async Task S0002_PriorRangeBreakoutsFilteredAndClosedByTheMovingAverage(int lookback, int maPeriod, bool secondary)
	{
		var highs = new Queue<decimal>();
		var lows = new Queue<decimal>();
		var closes = new Queue<decimal>();
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var longEntries = 0;
		var shortEntries = 0;
		var maExits = 0;
		var filteredBreakouts = 0;
		var violations = new List<string>();
		await Replay(NDayBreakout, (strategy, alternateSecurity) =>
		{
			if (secondary) strategy.Security = alternateSecurity;
			AreEqual(20, strategy.Parameters["LookbackPeriod"].Value);
			AreEqual(20, strategy.Parameters["MaPeriod"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
			AreEqual(TimeSpan.FromHours(1).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "LookbackPeriod", lookback);
			SetParam(strategy, "MaPeriod", maPeriod);
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				var close = candle.ClosePrice;
				closes.Enqueue(close);
				if (closes.Count > maPeriod) closes.Dequeue();
				// The range is that of the candles before this one.
				var ready = highs.Count == lookback && closes.Count == maPeriod;
				var rangeHigh = ready ? highs.Max() : 0m;
				var rangeLow = ready ? lows.Min() : 0m;
				highs.Enqueue(candle.HighPrice);
				lows.Enqueue(candle.LowPrice);
				if (highs.Count > lookback) { highs.Dequeue(); lows.Dequeue(); }
				if (!ready) return;
				var ma = closes.Average();
				var position = strategy.Position;
				var breaksUp = candle.HighPrice > rangeHigh;
				var breaksDown = candle.LowPrice < rangeLow;
				if (breaksUp && close > ma && position <= 0m)
				{
					expectedSide = Sides.Buy;
					expectedVolume = strategy.Volume + Math.Abs(position);
					longEntries++;
				}
				else if (breaksDown && close < ma && position >= 0m)
				{
					expectedSide = Sides.Sell;
					expectedVolume = strategy.Volume + Math.Abs(position);
					shortEntries++;
				}
				else if (position > 0m && close < ma)
				{
					expectedSide = Sides.Sell;
					expectedVolume = position;
					maExits++;
				}
				else if (position < 0m && close > ma)
				{
					expectedSide = Sides.Buy;
					expectedVolume = -position;
					maExits++;
				}
				else if ((breaksUp && close <= ma) || (breaksDown && close >= ma))
					filteredBreakouts++;
				if (expectedSide is not null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}, expected {expectedSide} {expectedVolume}. Every order must follow a break of the previous candles' range on the side of the moving average, or close the position when the close crosses back through it.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(longEntries > 0 && shortEntries > 0, "The fixture must break out on both sides.");
		IsTrue(maExits > 0, "The fixture must exercise the moving-average exit.");
		IsTrue(filteredBreakouts > 0, "The fixture must contain breakouts the moving average filters out.");
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public Task S0002_PercentStopWorksBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(NDayBreakout, TimeSpan.FromDays(31), expectedFrame: TimeSpan.FromHours(1));

	private const string AdxTrend = "0003_ADX_Trend";

	[TestMethod]
	[TestCategory("Shard01")]
	[DataRow(14, 50, 2.0, 20, false)]
	[DataRow(7, 20, 0.5, 22, false)]
	[DataRow(10, 30, 1.0, 18, true)]
	public async Task S0003_TrendSideByAdxWithAdxFadeAndFrozenAtrStopExits(int adxPeriod, int maPeriod, double multiplier, int exitThreshold, bool secondary)
	{
		var adx = new AverageDirectionalIndex { Length = adxPeriod };
		var atr = new AverageTrueRange { Length = adxPeriod };
		var sma = new SimpleMovingAverage { Length = maPeriod };
		var k = (decimal)multiplier;
		var stop = 0m;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var entries = new Dictionary<Sides, int> { [Sides.Buy] = 0, [Sides.Sell] = 0 };
		var reversals = 0;
		var fadeExits = 0;
		var stopExits = 0;
		var violations = new List<string>();
		await Replay(AdxTrend, (strategy, alternateSecurity) =>
		{
			if (secondary) strategy.Security = alternateSecurity;
			AreEqual(14, strategy.Parameters["AdxPeriod"].Value);
			AreEqual(50, strategy.Parameters["MaPeriod"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["AtrMultiplier"].Value));
			AreEqual(20, strategy.Parameters["AdxExitThreshold"].Value);
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "AdxPeriod", adxPeriod);
			SetParam(strategy, "MaPeriod", maPeriod);
			SetParam(strategy, "AtrMultiplier", multiplier);
			SetParam(strategy, "AdxExitThreshold", exitThreshold);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				var adxValue = (AverageDirectionalIndexValue)adx.Process(candle);
				var atrValue = atr.Process(candle);
				var maValue = sma.Process(candle);
				if (!adxValue.IsFormed || !atrValue.IsFormed || !maValue.IsFormed || adxValue.MovingAverage is not decimal strength) return;
				var close = candle.ClosePrice;
				var ma = maValue.GetValue<decimal>();
				var offset = k * atrValue.GetValue<decimal>();
				var longSetup = strength > 25m && close > ma;
				var shortSetup = strength > 25m && close < ma;
				var position = strategy.Position;
				void Enter(Sides side, decimal volume)
				{
					expectedSide = side;
					expectedVolume = volume;
					stop = side == Sides.Buy ? close - offset : close + offset;
					entries[side]++;
				}
				void Exit(Sides side, decimal volume)
				{
					expectedSide = side;
					expectedVolume = volume;
					if (strength < exitThreshold) fadeExits++; else stopExits++;
				}
				if (position > 0m)
				{
					if (shortSetup) { Enter(Sides.Sell, strategy.Volume + position); reversals++; }
					else if (strength < exitThreshold || close <= stop) Exit(Sides.Sell, position);
				}
				else if (position < 0m)
				{
					if (longSetup) { Enter(Sides.Buy, strategy.Volume - position); reversals++; }
					else if (strength < exitThreshold || close >= stop) Exit(Sides.Buy, -position);
				}
				else if (longSetup) Enter(Sides.Buy, strategy.Volume);
				else if (shortSetup) Enter(Sides.Sell, strategy.Volume);
				if (expectedSide is not null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}, expected {expectedSide} {expectedVolume}. Every order must follow the side of the moving average while ADX is above 25, or close the position when ADX fades or the close crosses the ATR stop fixed at entry.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(entries[Sides.Buy] > 0 && entries[Sides.Sell] > 0, "The fixture must enter on both sides.");
		IsTrue(fadeExits > 0, "The fixture must close a position because ADX faded.");
		if (multiplier < 1.5) IsTrue(stopExits > 0, "A tight multiplier must exercise the ATR stop.");
		if (!secondary) IsTrue(reversals > 0, "The fixture must reverse on the opposite setup.");
	}

	private const string SarTrend = "0004_Parabolic_SAR_Trend";

	[TestMethod]
	[TestCategory("Shard02")]
	[DataRow(0.02, 0.2, false)]
	[DataRow(0.01, 0.1, true)]
	public async Task S0004_ReversesOnEveryCloseFlipAcrossTheSar(double acceleration, double maximum, bool secondary)
	{
		var sar = new ParabolicSar { Acceleration = (decimal)acceleration, AccelerationMax = (decimal)maximum };
		bool? previousAbove = null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var reversals = 0;
		var violations = new List<string>();
		await Replay(SarTrend, (strategy, alternateSecurity) =>
		{
			if (secondary) strategy.Security = alternateSecurity;
			AreEqual(0.02m, Convert.ToDecimal(strategy.Parameters["AccelerationFactor"].Value));
			AreEqual(0.2m, Convert.ToDecimal(strategy.Parameters["MaxAccelerationFactor"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "AccelerationFactor", acceleration);
			SetParam(strategy, "MaxAccelerationFactor", maximum);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				var value = sar.Process(candle);
				if (value.IsEmpty) return;
				var level = value.GetValue<decimal>();
				if (level <= 0m) return;
				var above = candle.ClosePrice > level;
				var flipped = previousAbove is bool before && before != above;
				previousAbove = above;
				if (!flipped) return;
				var position = strategy.Position;
				if (above && position <= 0m) expectedSide = Sides.Buy;
				else if (!above && position >= 0m) expectedSide = Sides.Sell;
				else return;
				expectedVolume = strategy.Volume + Math.Abs(position);
				if (position != 0m) reversals++;
				expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}, expected {expectedSide} {expectedVolume}. Every order must reverse to the side of the SAR the close has just flipped to.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(reversals > 10, "The fixture must reverse repeatedly.");
	}

	private const string Donchian = "0005_Donchian_Channel";

	[TestMethod]
	[TestCategory("Shard03")]
	[DataRow(20, false)]
	[DataRow(8, true)]
	public async Task S0005_ClosesBeyondThePriorChannelWithMidpointExits(int period, bool secondary)
	{
		var highs = new Queue<decimal>();
		var lows = new Queue<decimal>();
		(decimal Upper, decimal Lower, decimal Middle)? channel = null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var longEntries = 0;
		var shortEntries = 0;
		var midpointExits = 0;
		var violations = new List<string>();
		await Replay(Donchian, (strategy, alternateSecurity) =>
		{
			if (secondary) strategy.Security = alternateSecurity;
			AreEqual(20, strategy.Parameters["ChannelPeriod"].Value);
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "ChannelPeriod", period);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				var prior = channel;
				highs.Enqueue(candle.HighPrice);
				lows.Enqueue(candle.LowPrice);
				if (highs.Count > period) { highs.Dequeue(); lows.Dequeue(); }
				if (highs.Count == period)
				{
					var upper = highs.Max();
					var lower = lows.Min();
					channel = (upper, lower, (upper + lower) / 2m);
				}
				if (prior is not { } band) return;
				var close = candle.ClosePrice;
				var position = strategy.Position;
				if (close > band.Upper && position <= 0m)
				{
					expectedSide = Sides.Buy;
					expectedVolume = strategy.Volume + Math.Abs(position);
					longEntries++;
				}
				else if (close < band.Lower && position >= 0m)
				{
					expectedSide = Sides.Sell;
					expectedVolume = strategy.Volume + Math.Abs(position);
					shortEntries++;
				}
				else if (position > 0m && close <= band.Middle)
				{
					expectedSide = Sides.Sell;
					expectedVolume = position;
					midpointExits++;
				}
				else if (position < 0m && close >= band.Middle)
				{
					expectedSide = Sides.Buy;
					expectedVolume = -position;
					midpointExits++;
				}
				if (expectedSide is not null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}, expected {expectedSide} {expectedVolume}. Every order must follow a close beyond the channel of the previous candles, or close the position when the close returns to its midpoint.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(longEntries > 0 && shortEntries > 0, "The fixture must break out on both sides.");
		IsTrue(midpointExits > 0, "The fixture must exercise the midpoint exit.");
	}

	private const string TripleMa = "0006_Tripple_MA";

	[TestMethod]
	[TestCategory("Shard04")]
	[DataRow(5, 20, 50, false)]
	[DataRow(3, 10, 30, true)]
	public async Task S0006_ShortAverageAboveOrBelowBothWithShortMiddleCrossExits(int shortPeriod, int middlePeriod, int longPeriod, bool secondary)
	{
		var fast = new ExponentialMovingAverage { Length = shortPeriod };
		var middle = new ExponentialMovingAverage { Length = middlePeriod };
		var slow = new ExponentialMovingAverage { Length = longPeriod };
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var entries = new Dictionary<Sides, int> { [Sides.Buy] = 0, [Sides.Sell] = 0 };
		var crossExits = 0;
		var reversals = 0;
		var violations = new List<string>();
		await Replay(TripleMa, (strategy, alternateSecurity) =>
		{
			if (secondary) strategy.Security = alternateSecurity;
			AreEqual(5, strategy.Parameters["ShortMaPeriod"].Value);
			AreEqual(20, strategy.Parameters["MiddleMaPeriod"].Value);
			AreEqual(50, strategy.Parameters["LongMaPeriod"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "ShortMaPeriod", shortPeriod);
			SetParam(strategy, "MiddleMaPeriod", middlePeriod);
			SetParam(strategy, "LongMaPeriod", longPeriod);
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				var f = fast.Process(candle);
				var m = middle.Process(candle);
				var l = slow.Process(candle);
				if (!f.IsFormed || !m.IsFormed || !l.IsFormed) return;
				var s = f.GetValue<decimal>();
				var mid = m.GetValue<decimal>();
				var lng = l.GetValue<decimal>();
				var longSetup = s > mid && s > lng;
				var shortSetup = s < mid && s < lng;
				var position = strategy.Position;
				if (position > 0m && s < mid)
				{
					expectedSide = Sides.Sell;
					expectedVolume = shortSetup ? strategy.Volume + position : position;
					if (shortSetup) { reversals++; entries[Sides.Sell]++; } else crossExits++;
				}
				else if (position < 0m && s > mid)
				{
					expectedSide = Sides.Buy;
					expectedVolume = longSetup ? strategy.Volume - position : -position;
					if (longSetup) { reversals++; entries[Sides.Buy]++; } else crossExits++;
				}
				else if (position == 0m && (longSetup || shortSetup))
				{
					expectedSide = longSetup ? Sides.Buy : Sides.Sell;
					expectedVolume = strategy.Volume;
					entries[expectedSide.Value]++;
				}
				if (expectedSide is not null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}, expected {expectedSide} {expectedVolume}. Every order must open on the short average being above or below both others, or close when it crosses the middle one.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(entries[Sides.Buy] > 0 && entries[Sides.Sell] > 0, "The fixture must enter on both sides.");
		IsTrue(crossExits > 0, "The fixture must close a position on a short/middle cross alone.");
	}

	[TestMethod]
	[TestCategory("Shard04")]
	public Task S0006_PercentStopWorksBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(TripleMa, TimeSpan.FromDays(31));

	private const string KeltnerBreakout = "0007_Keltner_Channel_Breakout";

	[TestMethod]
	[TestCategory("Shard05")]
	[DataRow(20, 14, 2.0, false)]
	[DataRow(10, 7, 1.0, true)]
	public async Task S0007_FreshBandBreakoutsWithCenterAndFrozenAtrStopExits(int emaPeriod, int atrPeriod, double multiplier, bool secondary)
	{
		var ema = new ExponentialMovingAverage { Length = emaPeriod };
		var atr = new AverageTrueRange { Length = atrPeriod };
		var k = (decimal)multiplier;
		decimal? prevClose = null;
		var prevUpper = 0m;
		var prevLower = 0m;
		var stop = 0m;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var entries = new Dictionary<Sides, int> { [Sides.Buy] = 0, [Sides.Sell] = 0 };
		var centerExits = 0;
		var stopExits = 0;
		var violations = new List<string>();
		await Replay(KeltnerBreakout, (strategy, alternateSecurity) =>
		{
			if (secondary) strategy.Security = alternateSecurity;
			AreEqual(20, strategy.Parameters["EmaPeriod"].Value);
			AreEqual(14, strategy.Parameters["AtrPeriod"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["AtrMultiplier"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "EmaPeriod", emaPeriod);
			SetParam(strategy, "AtrPeriod", atrPeriod);
			SetParam(strategy, "AtrMultiplier", multiplier);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				var e = ema.Process(candle);
				var a = atr.Process(candle);
				if (!e.IsFormed || !a.IsFormed) return;
				var center = e.GetValue<decimal>();
				var offset = k * a.GetValue<decimal>();
				var close = candle.ClosePrice;
				var last = prevClose;
				var upper = prevUpper;
				var lower = prevLower;
				prevClose = close;
				prevUpper = center + offset;
				prevLower = center - offset;
				if (last is not decimal lastClose) return;
				var position = strategy.Position;
				if (close > upper && lastClose <= upper && position <= 0m)
				{
					expectedSide = Sides.Buy;
					expectedVolume = strategy.Volume + Math.Abs(position);
					stop = close - offset;
					entries[Sides.Buy]++;
				}
				else if (close < lower && lastClose >= lower && position >= 0m)
				{
					expectedSide = Sides.Sell;
					expectedVolume = strategy.Volume + Math.Abs(position);
					stop = close + offset;
					entries[Sides.Sell]++;
				}
				else if (position > 0m && (close < center || close <= stop))
				{
					expectedSide = Sides.Sell;
					expectedVolume = position;
					if (close <= stop) stopExits++; else centerExits++;
				}
				else if (position < 0m && (close > center || close >= stop))
				{
					expectedSide = Sides.Buy;
					expectedVolume = -position;
					if (close >= stop) stopExits++; else centerExits++;
				}
				if (expectedSide is not null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}, expected {expectedSide} {expectedVolume}. Every order must follow a fresh close beyond the previous EMA ± ATR band, or close the position back through the EMA or at the ATR stop fixed at entry.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(entries[Sides.Buy] > 0 && entries[Sides.Sell] > 0, "The fixture must break out on both sides.");
		IsTrue(centerExits > 0 && stopExits > 0, "The fixture must exercise both the center exit and the stop.");
	}

	private const string HullTrend = "0008_Hull_MA_Trend";

	[TestMethod]
	[TestCategory("Shard06")]
	[DataRow(9, 14, 2.0, false)]
	[DataRow(16, 7, 1.0, true)]
	public async Task S0008_SlopeTurnsWithAtrTrailingStop(int hmaPeriod, int atrPeriod, double multiplier, bool secondary)
	{
		var hma = new HullMovingAverage { Length = hmaPeriod };
		var atr = new AverageTrueRange { Length = atrPeriod };
		var k = (decimal)multiplier;
		decimal? previousHma = null;
		var lastSlope = 0;
		var stop = 0m;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var turns = new Dictionary<Sides, int> { [Sides.Buy] = 0, [Sides.Sell] = 0 };
		var stopExits = 0;
		var trailedStops = 0;
		var violations = new List<string>();
		await Replay(HullTrend, (strategy, alternateSecurity) =>
		{
			if (secondary) strategy.Security = alternateSecurity;
			AreEqual(9, strategy.Parameters["HmaPeriod"].Value);
			AreEqual(14, strategy.Parameters["AtrPeriod"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["AtrMultiplier"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "HmaPeriod", hmaPeriod);
			SetParam(strategy, "AtrPeriod", atrPeriod);
			SetParam(strategy, "AtrMultiplier", multiplier);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				var h = hma.Process(candle);
				var a = atr.Process(candle);
				if (!h.IsFormed || !a.IsFormed) return;
				var value = h.GetValue<decimal>();
				var previous = previousHma;
				previousHma = value;
				if (previous is not decimal prior) return;
				var close = candle.ClosePrice;
				var distance = k * a.GetValue<decimal>();
				var slope = value > prior ? 1 : value < prior ? -1 : 0;
				var position = strategy.Position;
				if (slope != 0 && slope != lastSlope)
				{
					lastSlope = slope;
					if ((slope > 0 && position <= 0m) || (slope < 0 && position >= 0m))
					{
						expectedSide = slope > 0 ? Sides.Buy : Sides.Sell;
						expectedVolume = strategy.Volume + Math.Abs(position);
						stop = slope > 0 ? close - distance : close + distance;
						turns[expectedSide.Value]++;
						expectedOrders++;
						return;
					}
				}
				if (position > 0m)
				{
					if (close <= stop) { expectedSide = Sides.Sell; expectedVolume = position; stopExits++; }
					else if (close - distance > stop) { stop = close - distance; trailedStops++; }
				}
				else if (position < 0m)
				{
					if (close >= stop) { expectedSide = Sides.Buy; expectedVolume = -position; stopExits++; }
					else if (close + distance < stop) { stop = close + distance; trailedStops++; }
				}
				if (expectedSide is not null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}, expected {expectedSide} {expectedVolume}. Every order must follow a turn of the Hull MA slope, or close the position at the stop trailing the close by the ATR distance.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(turns[Sides.Buy] > 0 && turns[Sides.Sell] > 0, "The fixture must turn both ways.");
		IsTrue(stopExits > 0 && trailedStops > 0, "The fixture must trail the stop and stop out.");
	}

	private const string MacdTrend = "0009_MACD_Trend";

	[TestMethod]
	[TestCategory("Shard07")]
	[DataRow(12, 26, 9, false)]
	[DataRow(6, 13, 5, true)]
	public async Task S0009_ReversesOnEveryMacdSignalCross(int fastPeriod, int slowPeriod, int signalPeriod, bool secondary)
	{
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd = { ShortMa = { Length = fastPeriod }, LongMa = { Length = slowPeriod } },
			SignalMa = { Length = signalPeriod },
		};
		bool? previousAbove = null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var reversals = 0;
		var violations = new List<string>();
		await Replay(MacdTrend, (strategy, alternateSecurity) =>
		{
			if (secondary) strategy.Security = alternateSecurity;
			AreEqual(12, strategy.Parameters["FastEmaPeriod"].Value);
			AreEqual(26, strategy.Parameters["SlowEmaPeriod"].Value);
			AreEqual(9, strategy.Parameters["SignalPeriod"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "FastEmaPeriod", fastPeriod);
			SetParam(strategy, "SlowEmaPeriod", slowPeriod);
			SetParam(strategy, "SignalPeriod", signalPeriod);
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				var value = (MovingAverageConvergenceDivergenceSignalValue)macd.Process(candle);
				if (!value.IsFormed || value.Macd is not decimal line || value.Signal is not decimal signal) return;
				var above = line > signal;
				var crossed = previousAbove is bool before && before != above;
				previousAbove = above;
				if (!crossed) return;
				var position = strategy.Position;
				if (above && position <= 0m) expectedSide = Sides.Buy;
				else if (!above && position >= 0m) expectedSide = Sides.Sell;
				else return;
				expectedVolume = strategy.Volume + Math.Abs(position);
				if (position != 0m) reversals++;
				expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}, expected {expectedSide} {expectedVolume}. Every order must reverse to the side of a fresh MACD/signal cross.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(reversals > 10, "The fixture must reverse repeatedly.");
	}

	[TestMethod]
	[TestCategory("Shard07")]
	public Task S0009_PercentStopWorksBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(MacdTrend, TimeSpan.FromDays(31));

	private const string Supertrend = "0010_Super_Trend";

	[TestMethod]
	[TestCategory("Shard00")]
	[DataRow(10, 3.0, false)]
	[DataRow(7, 2.0, true)]
	public async Task S0010_ReversesOnEveryFlipOfTheStandardSupertrend(int period, double multiplier, bool secondary)
	{
		var atr = new AverageTrueRange { Length = period };
		var k = (decimal)multiplier;
		decimal? upperBand = null;
		decimal? lowerBand = null;
		decimal? previousClose = null;
		bool? upTrend = null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var reversals = 0;
		var violations = new List<string>();
		await Replay(Supertrend, (strategy, alternateSecurity) =>
		{
			if (secondary) strategy.Security = alternateSecurity;
			AreEqual(10, strategy.Parameters["Period"].Value);
			AreEqual(3m, Convert.ToDecimal(strategy.Parameters["Multiplier"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "Period", period);
			SetParam(strategy, "Multiplier", multiplier);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				var a = atr.Process(candle);
				if (!a.IsFormed) return;
				// Standard Supertrend: final bands only tighten until the close crosses them.
				var close = candle.ClosePrice;
				var median = (candle.HighPrice + candle.LowPrice) / 2m;
				var basicUpper = median + k * a.GetValue<decimal>();
				var basicLower = median - k * a.GetValue<decimal>();
				var finalUpper = upperBand is not decimal pu || basicUpper < pu || previousClose > pu ? basicUpper : pu;
				var finalLower = lowerBand is not decimal pl || basicLower > pl || previousClose < pl ? basicLower : pl;
				var before = upTrend;
				var now = before is not bool wasUp ? close >= median : wasUp ? close > finalLower : close >= finalUpper;
				upperBand = finalUpper;
				lowerBand = finalLower;
				previousClose = close;
				upTrend = now;
				if (before is not bool previous || previous == now) return;
				var position = strategy.Position;
				if (now && position <= 0m) expectedSide = Sides.Buy;
				else if (!now && position >= 0m) expectedSide = Sides.Sell;
				else return;
				expectedVolume = strategy.Volume + Math.Abs(position);
				if (position != 0m) reversals++;
				expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}, expected {expectedSide} {expectedVolume}. Every order must reverse on a flip of the standard Supertrend.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(reversals > 5, "The fixture must reverse repeatedly.");
	}

	private const string KumoBreakout = "0011_Ichimoku_Kumo_Breakout";

	[TestMethod]
	[TestCategory("Shard01")]
	[DataRow(9, 26, 52, false)]
	[DataRow(5, 13, 26, true)]
	public async Task S0011_CloudBreakoutsConfirmedByTenkanAndHeldUntilThroughTheCloud(int tenkanPeriod, int kijunPeriod, int spanPeriod, bool secondary)
	{
		var ichimoku = new Ichimoku
		{
			Tenkan = { Length = tenkanPeriod },
			Kijun = { Length = kijunPeriod },
			SenkouB = { Length = spanPeriod },
		};
		bool? wasLong = null;
		bool? wasShort = null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var entries = new Dictionary<Sides, int> { [Sides.Buy] = 0, [Sides.Sell] = 0 };
		var cloudExits = 0;
		var violations = new List<string>();
		await Replay(KumoBreakout, (strategy, alternateSecurity) =>
		{
			if (secondary) strategy.Security = alternateSecurity;
			AreEqual(9, strategy.Parameters["TenkanPeriod"].Value);
			AreEqual(26, strategy.Parameters["KijunPeriod"].Value);
			AreEqual(52, strategy.Parameters["SenkouSpanPeriod"].Value);
			AreEqual(TimeSpan.FromMinutes(15).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "TenkanPeriod", tenkanPeriod);
			SetParam(strategy, "KijunPeriod", kijunPeriod);
			SetParam(strategy, "SenkouSpanPeriod", spanPeriod);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				if (ichimoku.Process(candle) is not IIchimokuValue { Tenkan: decimal tenkan, Kijun: decimal kijun, SenkouA: decimal a, SenkouB: decimal b }) return;
				var close = candle.ClosePrice;
				var top = Math.Max(a, b);
				var bottom = Math.Min(a, b);
				var longSetup = close > top && tenkan > kijun;
				var shortSetup = close < bottom && tenkan < kijun;
				var lastLong = wasLong;
				var lastShort = wasShort;
				wasLong = longSetup;
				wasShort = shortSetup;
				if (lastLong is not bool previousLong || lastShort is not bool previousShort) return;
				var position = strategy.Position;
				if (longSetup && !previousLong && position <= 0m)
				{
					expectedSide = Sides.Buy;
					expectedVolume = strategy.Volume + Math.Abs(position);
					entries[Sides.Buy]++;
				}
				else if (shortSetup && !previousShort && position >= 0m)
				{
					expectedSide = Sides.Sell;
					expectedVolume = strategy.Volume + Math.Abs(position);
					entries[Sides.Sell]++;
				}
				else if (position > 0m && close < bottom)
				{
					expectedSide = Sides.Sell;
					expectedVolume = position;
					cloudExits++;
				}
				else if (position < 0m && close > top)
				{
					expectedSide = Sides.Buy;
					expectedVolume = -position;
					cloudExits++;
				}
				if (expectedSide is not null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}, expected {expectedSide} {expectedVolume}. Every order must open when a close beyond the cloud and the Tenkan/Kijun order first agree, or close once the close goes through the cloud.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(entries[Sides.Buy] > 0 && entries[Sides.Sell] > 0, "The fixture must break out of the cloud on both sides.");
		IsTrue(cloudExits > 0, "The fixture must close a position through the cloud.");
	}

	private const string HeikinRun = "0012_Heikin_Ashi_Consecutive";

	[TestMethod]
	[TestCategory("Shard02")]
	[DataRow(3, false)]
	[DataRow(5, true)]
	public async Task S0012_HeikinAshiRunsWithFirstOppositeCandleExits(int required, bool secondary)
	{
		decimal? haOpen = null;
		var haClose = 0m;
		var bullish = 0;
		var bearish = 0;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var entries = new Dictionary<Sides, int> { [Sides.Buy] = 0, [Sides.Sell] = 0 };
		var oppositeExits = 0;
		var violations = new List<string>();
		await Replay(HeikinRun, (strategy, alternateSecurity) =>
		{
			if (secondary) strategy.Security = alternateSecurity;
			AreEqual(3, strategy.Parameters["ConsecutiveCandles"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "ConsecutiveCandles", required);
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				var close = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
				var open = haOpen is decimal previousOpen ? (previousOpen + haClose) / 2m : (candle.OpenPrice + candle.ClosePrice) / 2m;
				haOpen = open;
				haClose = close;
				var up = close > open;
				var down = close < open;
				bullish = up ? bullish + 1 : 0;
				bearish = down ? bearish + 1 : 0;
				var position = strategy.Position;
				if (position > 0m && down)
				{
					expectedSide = Sides.Sell;
					expectedVolume = bearish >= required ? strategy.Volume + position : position;
					oppositeExits++;
				}
				else if (position < 0m && up)
				{
					expectedSide = Sides.Buy;
					expectedVolume = bullish >= required ? strategy.Volume - position : -position;
					oppositeExits++;
				}
				else if (position == 0m && (bullish >= required || bearish >= required))
				{
					expectedSide = bullish >= required ? Sides.Buy : Sides.Sell;
					expectedVolume = strategy.Volume;
					entries[expectedSide.Value]++;
				}
				if (expectedSide is not null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}, expected {expectedSide} {expectedVolume}. Every order must follow a run of same-colored Heikin Ashi candles, or close the position on the first opposite one.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(entries[Sides.Buy] > 0 && entries[Sides.Sell] > 0, "The fixture must enter on both sides.");
		IsTrue(oppositeExits > 0, "The fixture must exit on the first opposite candle.");
	}

	[TestMethod]
	[TestCategory("Shard02")]
	public Task S0012_PercentStopWorksBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(HeikinRun, TimeSpan.FromDays(31));

	private const string GannSwing = "0015_Gann_Swing_Breakout";

	[TestMethod]
	[TestCategory("Shard03")]
	[DataRow(5, 20, false)]
	[DataRow(3, 10, true)]
	public async Task S0015_BreaksOfConfirmedSwingsFilteredByTheAverageAndHeldUntilTheOpposingSwing(int lookback, int maPeriod, bool secondary)
	{
		var sma = new SimpleMovingAverage { Length = maPeriod };
		var window = new List<(decimal High, decimal Low)>();
		decimal? swingHigh = null;
		decimal? swingLow = null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var entries = new Dictionary<Sides, int> { [Sides.Buy] = 0, [Sides.Sell] = 0 };
		var swingExits = 0;
		var violations = new List<string>();
		await Replay(GannSwing, (strategy, alternateSecurity) =>
		{
			if (secondary) strategy.Security = alternateSecurity;
			AreEqual(5, strategy.Parameters["SwingLookback"].Value);
			AreEqual(20, strategy.Parameters["MaPeriod"].Value);
			AreEqual(TimeSpan.FromMinutes(15).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "SwingLookback", lookback);
			SetParam(strategy, "MaPeriod", maPeriod);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				window.Add((candle.HighPrice, candle.LowPrice));
				if (window.Count > 2 * lookback + 1) window.RemoveAt(0);
				if (window.Count == 2 * lookback + 1)
				{
					var pivot = window[lookback];
					var others = window.Where((_, index) => index != lookback).ToArray();
					if (others.All(c => pivot.High > c.High)) swingHigh = pivot.High;
					if (others.All(c => pivot.Low < c.Low)) swingLow = pivot.Low;
				}
				var m = sma.Process(candle);
				if (!m.IsFormed || swingHigh is not decimal high || swingLow is not decimal low) return;
				var ma = m.GetValue<decimal>();
				var close = candle.ClosePrice;
				var position = strategy.Position;
				if (close > high && close > ma && position <= 0m)
				{
					expectedSide = Sides.Buy;
					expectedVolume = strategy.Volume + Math.Abs(position);
					entries[Sides.Buy]++;
				}
				else if (close < low && close < ma && position >= 0m)
				{
					expectedSide = Sides.Sell;
					expectedVolume = strategy.Volume + Math.Abs(position);
					entries[Sides.Sell]++;
				}
				else if (position > 0m && close < low)
				{
					expectedSide = Sides.Sell;
					expectedVolume = position;
					swingExits++;
				}
				else if (position < 0m && close > high)
				{
					expectedSide = Sides.Buy;
					expectedVolume = -position;
					swingExits++;
				}
				if (expectedSide is not null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}, expected {expectedSide} {expectedVolume}. Every order must follow a close beyond the latest confirmed swing on the side of the average, or close the position when the opposing swing is breached.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(entries[Sides.Buy] > 0 && entries[Sides.Sell] > 0, "The fixture must break swings on both sides.");
		IsTrue(swingExits + entries.Values.Sum() > 4, "The fixture must trade repeatedly.");
	}

	private const string Hammer = "0059_Hammer_Candle";

	[TestMethod]
	[TestCategory("Shard04")]
	[DataRow(2.0, false)]
	[DataRow(1.0, true)]
	public async Task S0059_LongOnlyHammersWithStopAtTheLowAndRewardRiskTarget(double ratio, bool secondary)
	{
		var stop = 0m;
		var target = 0m;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var entries = 0;
		var stopExits = 0;
		var targetExits = 0;
		var violations = new List<string>();
		await Replay(Hammer, (strategy, alternateSecurity) =>
		{
			if (secondary) strategy.Security = alternateSecurity;
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["RewardRiskRatio"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "RewardRiskRatio", ratio);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				var close = candle.ClosePrice;
				var position = strategy.Position;
				if (position > 0m)
				{
					if (close <= stop || close >= target)
					{
						expectedSide = Sides.Sell;
						expectedVolume = position;
						if (close <= stop) stopExits++; else targetExits++;
					}
				}
				else if (position == 0m)
				{
					var body = Math.Abs(candle.OpenPrice - close);
					var lower = Math.Min(candle.OpenPrice, close) - candle.LowPrice;
					var upper = candle.HighPrice - Math.Max(candle.OpenPrice, close);
					if (body > 0m && lower >= 2m * body && upper < body / 2m && close > candle.LowPrice)
					{
						expectedSide = Sides.Buy;
						expectedVolume = strategy.Volume;
						stop = candle.LowPrice;
						target = close + (decimal)ratio * (close - candle.LowPrice);
						entries++;
					}
				}
				if (expectedSide is not null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}, expected {expectedSide} {expectedVolume}. Every order must buy a hammer while flat, or close the long at the hammer's low or at the reward/risk target.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(entries > 3, "The fixture must buy several hammers.");
		IsTrue(stopExits > 0 && targetExits > 0, "The fixture must exercise both the stop and the target.");
	}

	private const string ObvDivergence = "0075_OBV_Divergence";

	[TestMethod]
	[TestCategory("Shard05")]
	[DataRow(5, 20, false)]
	[DataRow(3, 10, true)]
	public async Task S0075_NewExtremesUnconfirmedByObvWithMeanReversionExits(int period, int maPeriod, bool secondary)
	{
		var sma = new SimpleMovingAverage { Length = maPeriod };
		var history = new List<(decimal High, decimal Low, decimal Obv)>();
		var obv = 0m;
		decimal? previousClose = null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var entries = new Dictionary<Sides, int> { [Sides.Buy] = 0, [Sides.Sell] = 0 };
		var averageExits = 0;
		var unconfirmedExtremes = 0;
		var violations = new List<string>();
		await Replay(ObvDivergence, (strategy, alternateSecurity) =>
		{
			if (secondary) strategy.Security = alternateSecurity;
			AreEqual(5, strategy.Parameters["DivergencePeriod"].Value);
			AreEqual(20, strategy.Parameters["MAPeriod"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "DivergencePeriod", period);
			SetParam(strategy, "MAPeriod", maPeriod);
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				var close = candle.ClosePrice;
				if (previousClose is decimal last)
					obv += close > last ? candle.TotalVolume : close < last ? -candle.TotalVolume : 0m;
				previousClose = close;
				var previous = history.ToArray();
				history.Add((candle.HighPrice, candle.LowPrice, obv));
				if (history.Count > period) history.RemoveAt(0);
				var m = sma.Process(candle);
				if (previous.Length < period || !m.IsFormed) return;
				var ma = m.GetValue<decimal>();
				var position = strategy.Position;
				if (position > 0m && close > ma) { expectedSide = Sides.Sell; expectedVolume = position; averageExits++; }
				else if (position < 0m && close < ma) { expectedSide = Sides.Buy; expectedVolume = -position; averageExits++; }
				else if (position == 0m)
				{
					var lowest = previous.MinBy(c => c.Low);
					var highest = previous.MaxBy(c => c.High);
					var bullish = candle.LowPrice < lowest.Low && obv > lowest.Obv;
					var bearish = candle.HighPrice > highest.High && obv < highest.Obv;
					if ((candle.LowPrice < lowest.Low && !bullish) || (candle.HighPrice > highest.High && !bearish)) unconfirmedExtremes++;
					if (bullish != bearish)
					{
						expectedSide = bullish ? Sides.Buy : Sides.Sell;
						expectedVolume = strategy.Volume;
						entries[expectedSide.Value]++;
					}
				}
				if (expectedSide is not null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}, expected {expectedSide} {expectedVolume}. Every order must fade a new extreme OBV does not confirm while flat, or close the position when the close crosses back over the average.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(entries[Sides.Buy] > 0 && entries[Sides.Sell] > 0, "The fixture must find divergences on both sides.");
		IsTrue(averageExits > 0, "The fixture must exit at the average.");
		IsTrue(unconfirmedExtremes > 0, "The fixture must contain new extremes that OBV confirms and that are not traded.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public Task S0075_PercentStopWorksBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(ObvDivergence, TimeSpan.FromDays(31));

	private const string FibonacciReversal = "0076_Fibonacci_Retracement_Reversal";

	[TestMethod]
	[TestCategory("Shard06")]
	[DataRow(20, 0.5, false)]
	[DataRow(12, 0.3, true)]
	public async Task S0076_DeepRetracementsInTheSwingDirectionWithMidpointTargets(int period, double bufferPercent, bool secondary)
	{
		var candles = new List<(decimal High, decimal Low)>();
		var target = 0m;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var entries = new Dictionary<Sides, int> { [Sides.Buy] = 0, [Sides.Sell] = 0 };
		var targetExits = 0;
		var violations = new List<string>();
		await Replay(FibonacciReversal, (strategy, alternateSecurity) =>
		{
			if (secondary) strategy.Security = alternateSecurity;
			AreEqual(20, strategy.Parameters["SwingLookbackPeriod"].Value);
			AreEqual(0.5m, Convert.ToDecimal(strategy.Parameters["FibLevelBuffer"].Value));
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "SwingLookbackPeriod", period);
			SetParam(strategy, "FibLevelBuffer", bufferPercent);
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				var swing = candles.ToArray();
				candles.Add((candle.HighPrice, candle.LowPrice));
				if (candles.Count > period) candles.RemoveAt(0);
				if (swing.Length < period) return;
				var close = candle.ClosePrice;
				var position = strategy.Position;
				if (position > 0m && close >= target) { expectedSide = Sides.Sell; expectedVolume = position; targetExits++; }
				else if (position < 0m && close <= target) { expectedSide = Sides.Buy; expectedVolume = -position; targetExits++; }
				else if (position == 0m)
				{
					var high = swing.Max(c => c.High);
					var low = swing.Min(c => c.Low);
					var highIndex = Array.FindIndex(swing, c => c.High == high);
					var lowIndex = Array.FindIndex(swing, c => c.Low == low);
					var range = high - low;
					var middle = low + range / 2m;
					bool Near(decimal level) => Math.Abs(close - level) <= level * (decimal)bufferPercent / 100m;
					if (range > 0m && lowIndex < highIndex && close > candle.OpenPrice && close < middle && (Near(high - range * 0.618m) || Near(high - range * 0.786m)))
						expectedSide = Sides.Buy;
					else if (range > 0m && highIndex < lowIndex && close < candle.OpenPrice && close > middle && (Near(low + range * 0.618m) || Near(low + range * 0.786m)))
						expectedSide = Sides.Sell;
					if (expectedSide is Sides side)
					{
						expectedVolume = strategy.Volume;
						target = middle;
						entries[side]++;
					}
				}
				if (expectedSide is not null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}, expected {expectedSide} {expectedVolume}. Every order must trade a confirming candle at the 61.8% or 78.6% retracement in the swing's direction, or close at the swing's 50% level.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(entries[Sides.Buy] > 0 && entries[Sides.Sell] > 0, "The fixture must trade retracements of rising and falling swings.");
		IsTrue(targetExits > 0, "The fixture must reach the 50% target.");
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public Task S0076_PercentStopWorksBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(FibonacciReversal, TimeSpan.FromDays(31));

	private const string InsideBar = "0077_Inside_Bar_Breakout";

	[TestMethod]
	[TestCategory("Shard07")]
	[DataRow(1.0, false)]
	[DataRow(0.05, true)]
	public async Task S0077_ClosesOutsideTheLatestInsideBarWithPatternStopAndPriorExtremeExits(double stopPercent, bool secondary)
	{
		(decimal High, decimal Low)? previous = null;
		(decimal High, decimal Low)? pattern = null;
		var stop = 0m;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var entries = new Dictionary<Sides, int> { [Sides.Buy] = 0, [Sides.Sell] = 0 };
		var stopExits = 0;
		var extremeExits = 0;
		var violations = new List<string>();
		await Replay(InsideBar, (strategy, alternateSecurity) =>
		{
			if (secondary) strategy.Security = alternateSecurity;
			AreEqual(1m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "StopLossPercent", stopPercent);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				var prior = previous;
				var active = pattern;
				previous = (candle.HighPrice, candle.LowPrice);
				var close = candle.ClosePrice;
				if (active is { } used && (close > used.High || close < used.Low)) pattern = null;
				if (prior is { } p && candle.HighPrice <= p.High && candle.LowPrice >= p.Low) pattern = (candle.HighPrice, candle.LowPrice);
				if (prior is not { } last) return;
				var position = strategy.Position;
				var k = (decimal)stopPercent / 100m;
				if (position > 0m && (close <= stop || close < last.Low))
				{
					expectedSide = Sides.Sell;
					expectedVolume = position;
					if (close <= stop) stopExits++; else extremeExits++;
				}
				else if (position < 0m && (close >= stop || close > last.High))
				{
					expectedSide = Sides.Buy;
					expectedVolume = -position;
					if (close >= stop) stopExits++; else extremeExits++;
				}
				else if (position == 0m && active is { } bar && (close > bar.High || close < bar.Low))
				{
					expectedSide = close > bar.High ? Sides.Buy : Sides.Sell;
					expectedVolume = strategy.Volume;
					stop = expectedSide == Sides.Buy ? bar.Low * (1 - k) : bar.High * (1 + k);
					entries[expectedSide.Value]++;
				}
				if (expectedSide is not null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}, expected {expectedSide} {expectedVolume}. Every order must follow a close outside the latest inside bar while flat, or close the position at the stop beyond the pattern or beyond the previous candle's extreme.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(entries[Sides.Buy] > 0 && entries[Sides.Sell] > 0, "The fixture must break out on both sides.");
		IsTrue(extremeExits > 0, "The fixture must exit beyond the previous candle's extreme.");
		if (stopPercent < 0.5) IsTrue(stopExits > 0, "A tight stop must be reached.");
	}

	private const string OutsideBar = "0078_Outside_Bar_Reversal";

	[TestMethod]
	[TestCategory("Shard00")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task S0078_OutsideBarsAgainstThePriorCandleWithExtremeBreakExits(bool secondary)
	{
		ICandleMessage previous = null;
		var exitLevel = 0m;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var entries = new Dictionary<Sides, int> { [Sides.Buy] = 0, [Sides.Sell] = 0 };
		var extremeExits = 0;
		var sameDirectionOutsideBars = 0;
		var violations = new List<string>();
		await Replay(OutsideBar, (strategy, alternateSecurity) =>
		{
			if (secondary) strategy.Security = alternateSecurity;
			AreEqual(1m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				var prior = previous;
				previous = candle;
				if (prior == null) return;
				var close = candle.ClosePrice;
				var position = strategy.Position;
				if (position > 0m && close < exitLevel) { expectedSide = Sides.Sell; expectedVolume = position; extremeExits++; }
				else if (position < 0m && close > exitLevel) { expectedSide = Sides.Buy; expectedVolume = -position; extremeExits++; }
				else if (position == 0m && candle.HighPrice > prior.HighPrice && candle.LowPrice < prior.LowPrice)
				{
					var bullish = close > candle.OpenPrice;
					var bearish = close < candle.OpenPrice;
					if (bullish && prior.ClosePrice < prior.OpenPrice) { expectedSide = Sides.Buy; exitLevel = candle.LowPrice; }
					else if (bearish && prior.ClosePrice > prior.OpenPrice) { expectedSide = Sides.Sell; exitLevel = candle.HighPrice; }
					else if (bullish || bearish) sameDirectionOutsideBars++;
					if (expectedSide is Sides side) { expectedVolume = strategy.Volume; entries[side]++; }
				}
				if (expectedSide is not null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}, expected {expectedSide} {expectedVolume}. Every order must fade an outside bar closing against the previous candle while flat, or close the position when a close breaks the bar's opposite extreme.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(entries[Sides.Buy] + entries[Sides.Sell] > 0, "The fixture must fade outside bars.");
		if (secondary) IsTrue(entries[Sides.Buy] > 0 && entries[Sides.Sell] > 0, "TON must fade outside bars on both sides.");
		IsTrue(extremeExits > 0, "The fixture must exit through the outside bar's extreme.");
		if (secondary) IsTrue(sameDirectionOutsideBars > 0, "TON must contain outside bars that continue the previous candle and are not traded.");
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public Task S0078_PercentStopWorksBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(OutsideBar, TimeSpan.FromDays(31), expectedStopPercent: 1m);

	private const string TrendlineBounce = "0079_Trendline_Bounce";

	[TestMethod]
	[TestCategory("Shard01")]
	[DataRow(20, 20, 0.5, false)]
	[DataRow(10, 10, 0.2, true)]
	public async Task S0079_BouncesOffSlopingRegressionLinesWithAverageExits(int period, int maPeriod, double thresholdPercent, bool secondary)
	{
		static (decimal Slope, decimal Next) Fit(IReadOnlyList<decimal> values)
		{
			var n = values.Count;
			var meanX = (n - 1) / 2m;
			var meanY = values.Average();
			var covariance = 0m;
			var variance = 0m;
			for (var i = 0; i < n; i++)
			{
				covariance += (i - meanX) * (values[i] - meanY);
				variance += (i - meanX) * (i - meanX);
			}
			var slope = covariance / variance;
			return (slope, meanY + slope * (n - meanX));
		}
		var sma = new SimpleMovingAverage { Length = maPeriod };
		var highs = new List<decimal>();
		var lows = new List<decimal>();
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var entries = new Dictionary<Sides, int> { [Sides.Buy] = 0, [Sides.Sell] = 0 };
		var averageExits = 0;
		var violations = new List<string>();
		await Replay(TrendlineBounce, (strategy, alternateSecurity) =>
		{
			if (secondary) strategy.Security = alternateSecurity;
			AreEqual(20, strategy.Parameters["TrendlinePeriod"].Value);
			AreEqual(20, strategy.Parameters["MAPeriod"].Value);
			AreEqual(0.5m, Convert.ToDecimal(strategy.Parameters["BounceThresholdPercent"].Value));
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "TrendlinePeriod", period);
			SetParam(strategy, "MAPeriod", maPeriod);
			SetParam(strategy, "BounceThresholdPercent", thresholdPercent);
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				var ready = highs.Count == period;
				var support = ready ? Fit(lows) : default;
				var resistance = ready ? Fit(highs) : default;
				highs.Add(candle.HighPrice);
				lows.Add(candle.LowPrice);
				if (highs.Count > period) { highs.RemoveAt(0); lows.RemoveAt(0); }
				var m = sma.Process(candle);
				if (!ready || !m.IsFormed) return;
				var ma = m.GetValue<decimal>();
				var close = candle.ClosePrice;
				var position = strategy.Position;
				var t = (decimal)thresholdPercent / 100m;
				if (position > 0m && close < ma) { expectedSide = Sides.Sell; expectedVolume = position; averageExits++; }
				else if (position < 0m && close > ma) { expectedSide = Sides.Buy; expectedVolume = -position; averageExits++; }
				else if (position == 0m)
				{
					if (support.Slope > 0m && candle.LowPrice <= support.Next * (1 + t) && close > candle.OpenPrice && close > ma) expectedSide = Sides.Buy;
					else if (resistance.Slope < 0m && candle.HighPrice >= resistance.Next * (1 - t) && close < candle.OpenPrice && close < ma) expectedSide = Sides.Sell;
					if (expectedSide is Sides side) { expectedVolume = strategy.Volume; entries[side]++; }
				}
				if (expectedSide is not null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}, expected {expectedSide} {expectedVolume}. Every order must follow a confirmed bounce off a rising support or falling resistance on the side of the average, or close the position when the close crosses the average.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(entries[Sides.Buy] > 0 && entries[Sides.Sell] > 0, "The fixture must bounce off both lines.");
		IsTrue(averageExits > 0, "The fixture must exit on the average.");
	}

	[TestMethod]
	[TestCategory("Shard01")]
	public Task S0079_PercentStopWorksBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(TrendlineBounce, TimeSpan.FromDays(31));

	private const string PivotReversal = "0080_Pivot_Point_Reversal";

	[TestMethod]
	[TestCategory("Shard02")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task S0080_RejectionsOfPriorDayS1AndR1WithPivotExits(bool secondary)
	{
		var days = new Dictionary<DateTime, (decimal High, decimal Low, decimal Close)>();
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var entries = new Dictionary<Sides, int> { [Sides.Buy] = 0, [Sides.Sell] = 0 };
		var pivotExits = 0;
		var violations = new List<string>();
		await Replay(PivotReversal, (strategy, alternateSecurity) =>
		{
			if (secondary) strategy.Security = alternateSecurity;
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				var day = candle.OpenTime.Date;
				days[day] = days.TryGetValue(day, out var d)
					? (Math.Max(d.High, candle.HighPrice), Math.Min(d.Low, candle.LowPrice), candle.ClosePrice)
					: (candle.HighPrice, candle.LowPrice, candle.ClosePrice);
				var earlier = days.Keys.Where(k => k < day).ToArray();
				if (earlier.Length == 0) return;
				var prior = days[earlier.Max()];
				var pivot = (prior.High + prior.Low + prior.Close) / 3m;
				var r1 = 2 * pivot - prior.Low;
				var s1 = 2 * pivot - prior.High;
				var close = candle.ClosePrice;
				var position = strategy.Position;
				if (position > 0m && close >= pivot) { expectedSide = Sides.Sell; expectedVolume = position; pivotExits++; }
				else if (position < 0m && close <= pivot) { expectedSide = Sides.Buy; expectedVolume = -position; pivotExits++; }
				else if (position == 0m)
				{
					if (close > candle.OpenPrice && candle.LowPrice <= s1 && close > s1) expectedSide = Sides.Buy;
					else if (close < candle.OpenPrice && candle.HighPrice >= r1 && close < r1) expectedSide = Sides.Sell;
					if (expectedSide is Sides side) { expectedVolume = strategy.Volume; entries[side]++; }
				}
				if (expectedSide is not null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}, expected {expectedSide} {expectedVolume}. Every order must follow a confirmed rejection of the prior day's S1 or R1 while flat, or close the position at the central pivot.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(entries[Sides.Buy] + entries[Sides.Sell] > 1, "The fixture must reject pivot levels.");
		if (secondary) IsTrue(entries[Sides.Buy] > 0 && entries[Sides.Sell] > 0, "TON must reject both S1 and R1.");
		IsTrue(pivotExits > 0, "The fixture must reach the central pivot.");
	}

	[TestMethod]
	[TestCategory("Shard02")]
	public Task S0080_PercentStopWorksBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(PivotReversal, TimeSpan.FromDays(31));

	private const string VwapBounce = "0081_VWAP_Bounce";

	[TestMethod]
	[TestCategory("Shard03")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task S0081_CandlesClosingAgainstTheDailyVwapReverseThePosition(bool secondary)
	{
		var day = DateTime.MinValue;
		var priceVolume = 0m;
		var volume = 0m;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var entries = new Dictionary<Sides, int> { [Sides.Buy] = 0, [Sides.Sell] = 0 };
		var reversals = 0;
		var violations = new List<string>();
		await Replay(VwapBounce, (strategy, alternateSecurity) =>
		{
			if (secondary) strategy.Security = alternateSecurity;
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				if (candle.OpenTime.Date != day) { day = candle.OpenTime.Date; priceVolume = 0m; volume = 0m; }
				priceVolume += (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m * candle.TotalVolume;
				volume += candle.TotalVolume;
				if (volume <= 0m) return;
				var vwap = priceVolume / volume;
				var close = candle.ClosePrice;
				var position = strategy.Position;
				if (close > candle.OpenPrice && close < vwap && position <= 0m) expectedSide = Sides.Buy;
				else if (close < candle.OpenPrice && close > vwap && position >= 0m) expectedSide = Sides.Sell;
				if (expectedSide is Sides side)
				{
					expectedVolume = strategy.Volume + Math.Abs(position);
					entries[side]++;
					if (position != 0m) reversals++;
					expectedOrders++;
				}
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}, expected {expectedSide} {expectedVolume}. Every order must follow a bullish close below or a bearish close above the daily VWAP, reversing an opposite position.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(entries[Sides.Buy] > 0 && entries[Sides.Sell] > 0 && reversals > 5, "The fixture must reverse repeatedly on both sides.");
	}

	[TestMethod]
	[TestCategory("Shard03")]
	public Task S0081_PercentStopWorksBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(VwapBounce, TimeSpan.FromDays(31));

	private const string VolumeExhaustion = "0082_Volume_Exhaustion";

	[TestMethod]
	[TestCategory("Shard04")]
	[DataRow(20, 2.0, 20, 2.0, false)]
	[DataRow(10, 1.5, 10, 1.0, true)]
	public async Task S0082_VolumeSpikesAgainstTheTrendWithAtrTrailingStop(int volumePeriod, double volumeMultiplier, int maPeriod, double atrMultiplier, bool secondary)
	{
		var sma = new SimpleMovingAverage { Length = maPeriod };
		var atr = new AverageTrueRange { Length = 14 };
		var volumes = new List<decimal>();
		decimal? previousMa = null;
		var stop = 0m;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var entries = new Dictionary<Sides, int> { [Sides.Buy] = 0, [Sides.Sell] = 0 };
		var stopExits = 0;
		var trendSpikes = 0;
		var violations = new List<string>();
		await Replay(VolumeExhaustion, (strategy, alternateSecurity) =>
		{
			if (secondary) strategy.Security = alternateSecurity;
			AreEqual(20, strategy.Parameters["VolumePeriod"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["VolumeMultiplier"].Value));
			AreEqual(20, strategy.Parameters["MAPeriod"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["AtrMultiplier"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "VolumePeriod", volumePeriod);
			SetParam(strategy, "VolumeMultiplier", volumeMultiplier);
			SetParam(strategy, "MAPeriod", maPeriod);
			SetParam(strategy, "AtrMultiplier", atrMultiplier);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				decimal? average = volumes.Count == volumePeriod ? volumes.Average() : null;
				volumes.Add(candle.TotalVolume);
				if (volumes.Count > volumePeriod) volumes.RemoveAt(0);
				var m = sma.Process(candle);
				var a = atr.Process(candle);
				if (!m.IsFormed || !a.IsFormed) return;
				var ma = m.GetValue<decimal>();
				var last = previousMa;
				previousMa = ma;
				if (average is not decimal avg || last is not decimal lastMa) return;
				var close = candle.ClosePrice;
				var distance = (decimal)atrMultiplier * a.GetValue<decimal>();
				var position = strategy.Position;
				if (position > 0m)
				{
					if (close <= stop) { expectedSide = Sides.Sell; expectedVolume = position; stopExits++; }
					else stop = Math.Max(stop, close - distance);
				}
				else if (position < 0m)
				{
					if (close >= stop) { expectedSide = Sides.Buy; expectedVolume = -position; stopExits++; }
					else stop = Math.Min(stop, close + distance);
				}
				else if (candle.TotalVolume > avg * (decimal)volumeMultiplier)
				{
					if (close > candle.OpenPrice && ma < lastMa) { expectedSide = Sides.Buy; stop = close - distance; }
					else if (close < candle.OpenPrice && ma > lastMa) { expectedSide = Sides.Sell; stop = close + distance; }
					else if (close != candle.OpenPrice) trendSpikes++;
					if (expectedSide is Sides side) { expectedVolume = strategy.Volume; entries[side]++; }
				}
				if (expectedSide is not null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}, expected {expectedSide} {expectedVolume}. Every order must fade a volume spike against the moving-average trend while flat, or close the position at the ATR trailing stop.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(entries[Sides.Buy] > 0 && entries[Sides.Sell] > 0, "The fixture must fade spikes on both sides.");
		IsTrue(stopExits > 0, "The fixture must exit at the trailing stop.");
		IsTrue(trendSpikes > 0, "The fixture must contain spikes in the direction of the trend that are not traded.");
	}

	private const string AdxWeakening = "0083_ADX_Weakening";

	[TestMethod]
	[TestCategory("Shard05")]
	[DataRow(14, 20, false)]
	[DataRow(7, 10, true)]
	public async Task S0083_FallingAdxOnTheSideOfTheAverageHeldUntilAdxRises(int adxPeriod, int maPeriod, bool secondary)
	{
		var adx = new AverageDirectionalIndex { Length = adxPeriod };
		var sma = new SimpleMovingAverage { Length = maPeriod };
		decimal? previousAdx = null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var entries = new Dictionary<Sides, int> { [Sides.Buy] = 0, [Sides.Sell] = 0 };
		var risingExits = 0;
		var violations = new List<string>();
		await Replay(AdxWeakening, (strategy, alternateSecurity) =>
		{
			if (secondary) strategy.Security = alternateSecurity;
			AreEqual(14, strategy.Parameters["AdxPeriod"].Value);
			AreEqual(20, strategy.Parameters["MaPeriod"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
			AreEqual(TimeSpan.FromMinutes(15).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "AdxPeriod", adxPeriod);
			SetParam(strategy, "MaPeriod", maPeriod);
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				var m = sma.Process(candle);
				var a = adx.Process(candle);
				if (!m.IsFormed || !a.IsFormed || a is not AverageDirectionalIndexValue { MovingAverage: decimal strength }) return;
				var last = previousAdx;
				previousAdx = strength;
				if (last is not decimal lastStrength) return;
				var close = candle.ClosePrice;
				var ma = m.GetValue<decimal>();
				var position = strategy.Position;
				if (position > 0m && strength > lastStrength) { expectedSide = Sides.Sell; expectedVolume = position; risingExits++; }
				else if (position < 0m && strength > lastStrength) { expectedSide = Sides.Buy; expectedVolume = -position; risingExits++; }
				else if (position == 0m && strength < lastStrength && close != ma)
				{
					expectedSide = close > ma ? Sides.Buy : Sides.Sell;
					expectedVolume = strategy.Volume;
					entries[expectedSide.Value]++;
				}
				if (expectedSide is not null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}, expected {expectedSide} {expectedVolume}. Every order must follow a falling ADX on the side of the average while flat, or close the position when ADX rises.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(entries[Sides.Buy] > 0 && entries[Sides.Sell] > 0, "The fixture must enter on both sides.");
		IsTrue(risingExits > 0, "The fixture must exit when ADX rises.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public Task S0083_PercentStopWorksBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(AdxWeakening, TimeSpan.FromDays(31), expectedFrame: TimeSpan.FromMinutes(15));

	private const string AtrExhaustion = "0084_ATR_Exhaustion";

	[TestMethod]
	[TestCategory("Shard06")]
	[DataRow(14, 20, 1.5, 20, false)]
	[DataRow(7, 10, 1.2, 10, true)]
	public async Task S0084_AtrSpikesWithCandlesAgainstThePriorMove(int atrPeriod, int averagePeriod, double multiplier, int maPeriod, bool secondary)
	{
		var atr = new AverageTrueRange { Length = atrPeriod };
		var sma = new SimpleMovingAverage { Length = maPeriod };
		var atrs = new Queue<decimal>();
		decimal? previousMa = null;
		Sides? expectedSide = null;
		var expectedOrders = 0;
		var actualOrders = 0;
		var entries = new Dictionary<Sides, int> { [Sides.Buy] = 0, [Sides.Sell] = 0 };
		var spikesWithTheMove = 0;
		var nativeStop = typeof(Strategy).GetField("_isStopTrailing", BindingFlags.Instance | BindingFlags.NonPublic);
		var violations = new List<string>();
		await Replay(AtrExhaustion, (strategy, alternateSecurity) =>
		{
			if (secondary) strategy.Security = alternateSecurity;
			AreEqual(14, strategy.Parameters["AtrPeriod"].Value);
			AreEqual(20, strategy.Parameters["AtrAvgPeriod"].Value);
			AreEqual(1.5m, Convert.ToDecimal(strategy.Parameters["AtrMultiplier"].Value));
			AreEqual(20, strategy.Parameters["MaPeriod"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "AtrPeriod", atrPeriod);
			SetParam(strategy, "AtrAvgPeriod", averagePeriod);
			SetParam(strategy, "AtrMultiplier", multiplier);
			SetParam(strategy, "MaPeriod", maPeriod);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				var a = atr.Process(candle);
				var m = sma.Process(candle);
				if (!a.IsFormed) return;
				var value = a.GetValue<decimal>();
				atrs.Enqueue(value);
				if (atrs.Count > averagePeriod) atrs.Dequeue();
				if (!m.IsFormed) return;
				var ma = m.GetValue<decimal>();
				var last = previousMa;
				previousMa = ma;
				if (atrs.Count < averagePeriod || last is not decimal lastMa || strategy.Position != 0m) return;
				if (value <= atrs.Average() * (decimal)multiplier) return;
				var close = candle.ClosePrice;
				if (close > candle.OpenPrice && ma < lastMa) expectedSide = Sides.Buy;
				else if (close < candle.OpenPrice && ma > lastMa) expectedSide = Sides.Sell;
				else if (close != candle.OpenPrice) spikesWithTheMove++;
				if (expectedSide is Sides side) { entries[side]++; expectedOrders++; }
			};
			strategy.OrderRegistering += order =>
			{
				if (strategy.Position != 0m)
				{
					// Only the trailing protection may close a position.
					IsTrue((bool)nativeStop.GetValue(strategy), "The percent stop must trail.");
					return;
				}
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != strategy.Volume || order.Type != OrderTypes.Market)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}, expected {expectedSide}. Every entry must follow an ATR spike above its average with a candle against the prior move of the price average.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(entries[Sides.Buy] + entries[Sides.Sell] > 1, "The fixture must fade ATR spikes.");
		if (secondary) IsTrue(entries[Sides.Buy] > 0 && entries[Sides.Sell] > 0, "TON must fade spikes on both sides.");
		IsTrue(spikesWithTheMove > 0, "The fixture must contain spikes with candles in the direction of the prior move that are not traded.");
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public Task S0084_PercentStopWorksBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(AtrExhaustion, TimeSpan.FromDays(31));

	private const string TenkanKijun = "0085_Ichimoku_Tenkan";

	[TestMethod]
	[TestCategory("Shard07")]
	[DataRow(9, 26, 52, false)]
	[DataRow(5, 13, 26, true)]
	public async Task S0085_TenkanKijunCrossesBeyondTheCloudWithKijunStops(int tenkanPeriod, int kijunPeriod, int spanPeriod, bool secondary)
	{
		var ichimoku = new Ichimoku
		{
			Tenkan = { Length = tenkanPeriod },
			Kijun = { Length = kijunPeriod },
			SenkouB = { Length = spanPeriod },
		};
		bool? tenkanAbove = null;
		var stop = 0m;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var entries = new Dictionary<Sides, int> { [Sides.Buy] = 0, [Sides.Sell] = 0 };
		var crossExits = 0;
		var stopExits = 0;
		var violations = new List<string>();
		await Replay(TenkanKijun, (strategy, alternateSecurity) =>
		{
			if (secondary) strategy.Security = alternateSecurity;
			AreEqual(9, strategy.Parameters["TenkanPeriod"].Value);
			AreEqual(26, strategy.Parameters["KijunPeriod"].Value);
			AreEqual(52, strategy.Parameters["SenkouSpanBPeriod"].Value);
			AreEqual(TimeSpan.FromMinutes(30).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "TenkanPeriod", tenkanPeriod);
			SetParam(strategy, "KijunPeriod", kijunPeriod);
			SetParam(strategy, "SenkouSpanBPeriod", spanPeriod);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				if (ichimoku.Process(candle) is not IIchimokuValue { Tenkan: decimal tenkan, Kijun: decimal kijun, SenkouA: decimal a, SenkouB: decimal b }) return;
				var before = tenkanAbove;
				if (tenkan != kijun) tenkanAbove = tenkan > kijun;
				if (before is not bool wasAbove) return;
				var up = !wasAbove && tenkan > kijun;
				var down = wasAbove && tenkan < kijun;
				var close = candle.ClosePrice;
				var top = Math.Max(a, b);
				var bottom = Math.Min(a, b);
				var position = strategy.Position;
				void Enter(Sides side, decimal volume) { expectedSide = side; expectedVolume = volume; stop = kijun; entries[side]++; }
				if (position > 0m)
				{
					if (down && close < bottom) Enter(Sides.Sell, strategy.Volume + position);
					else if (down || close <= stop) { expectedSide = Sides.Sell; expectedVolume = position; if (down) crossExits++; else stopExits++; }
				}
				else if (position < 0m)
				{
					if (up && close > top) Enter(Sides.Buy, strategy.Volume - position);
					else if (up || close >= stop) { expectedSide = Sides.Buy; expectedVolume = -position; if (up) crossExits++; else stopExits++; }
				}
				else if (up && close > top) Enter(Sides.Buy, strategy.Volume);
				else if (down && close < bottom) Enter(Sides.Sell, strategy.Volume);
				if (expectedSide is not null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}, expected {expectedSide} {expectedVolume}. Every order must follow a Tenkan/Kijun cross beyond the cloud, or close the position on the opposite cross or at the Kijun stop.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(entries[Sides.Buy] + entries[Sides.Sell] > 1, "The fixture must trade crosses beyond the cloud.");
		IsTrue(crossExits + stopExits > 0, "The fixture must close positions.");
	}

	private const string HeikinFlip = "0086_Heikin_Ashi_Reversal";

	[TestMethod]
	[TestCategory("Shard00")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task S0086_HeikinAshiColorFlipsReverseThePosition(bool secondary)
	{
		decimal? haOpen = null;
		var haClose = 0m;
		bool? bullish = null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var reversals = 0;
		var violations = new List<string>();
		await Replay(HeikinFlip, (strategy, alternateSecurity) =>
		{
			if (secondary) strategy.Security = alternateSecurity;
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
			AreEqual(TimeSpan.FromMinutes(15).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				var close = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
				var open = haOpen is decimal previousOpen ? (previousOpen + haClose) / 2m : (candle.OpenPrice + candle.ClosePrice) / 2m;
				haOpen = open;
				haClose = close;
				if (close == open) return;
				var before = bullish;
				bullish = close > open;
				if (before is not bool wasBullish || wasBullish == bullish) return;
				var position = strategy.Position;
				if (bullish == true && position <= 0m) expectedSide = Sides.Buy;
				else if (bullish == false && position >= 0m) expectedSide = Sides.Sell;
				else return;
				expectedVolume = strategy.Volume + Math.Abs(position);
				if (position != 0m) reversals++;
				expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}, expected {expectedSide} {expectedVolume}. Every order must reverse to the new color of a Heikin-Ashi flip.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(reversals > 10, "The fixture must reverse repeatedly.");
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public Task S0086_PercentStopWorksBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(HeikinFlip, TimeSpan.FromDays(31), expectedFrame: TimeSpan.FromMinutes(15));

	private const string Williams = "0017_Williams_R";
	private const string Roc = "0018_ROC_Impulce";
	private const string Cci = "0019_CCI_Breakout";
	private const string MomentumPercentage = "0020_Momentum_Percentage";

	[TestMethod]
	[TestCategory("Shard01")]
	[DataRow(14)]
	[DataRow(7)]
	public async Task S0017_WilliamsCrossingsAndNeutralExits(int period)
	{
		var bars = new Queue<ICandleMessage>();
		decimal? previous = null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var neutralExits = 0;
		var oversoldEntries = 0;
		var overboughtEntries = 0;
		var buys = 0;
		var sells = 0;
		var violations = new List<string>();
		await Replay(Williams, (strategy, _) =>
		{
			IsTrue(strategy.Parameters.TryGetValue("StopLossPercent", out var stop), "README promises a configurable 2% stop, not unprotected oscillator trades.");
			AreEqual(2m, Convert.ToDecimal(stop.Value));
			AreEqual(14, strategy.Parameters["Period"].Value);
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "Period", period);
			// Isolate the oscillator's exact contract; ordinary inventory rows retain the published 2% stop.
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				bars.Enqueue(candle);
				if (bars.Count > period) bars.Dequeue();
				if (bars.Count < period) return;
				var high = bars.Max(bar => bar.HighPrice);
				var low = bars.Min(bar => bar.LowPrice);
				if (high == low) return;
				var value = -100m * (high - candle.ClosePrice) / (high - low);
				if (previous is decimal prev)
				{
					if (prev >= -80m && value < -80m && strategy.Position <= 0m)
					{
						expectedSide = Sides.Buy;
						expectedVolume = strategy.Volume + Math.Abs(strategy.Position);
						oversoldEntries++;
					}
					else if (prev <= -20m && value > -20m && strategy.Position >= 0m)
					{
						expectedSide = Sides.Sell;
						expectedVolume = strategy.Volume + Math.Abs(strategy.Position);
						overboughtEntries++;
					}
					else if (strategy.Position > 0m && value >= -50m || strategy.Position < 0m && value <= -50m)
					{
						expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
						expectedVolume = Math.Abs(strategy.Position);
						neutralExits++;
					}
					if (expectedSide is not null) expectedOrders++;
				}
				previous = value;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side == Sides.Buy) buys++; else sells++;
				if (order.Type != OrderTypes.Market || order.Side != expectedSide || order.Volume != expectedVolume)
					violations.Add("Every order must follow the independent high/low Williams %R entry into the oversold (< -80) or overbought (> -20) zone or the neutral-zone exit, with complete reversal/close quantity and no hidden cooldown.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		AreEqual(expectedOrders, actualOrders, "Every eligible zone entry or neutral exit must act; no cooldown may silently discard it.");
		IsTrue(buys > 0 && sells > 0 && neutralExits > 0 && oversoldEntries > 0 && overboughtEntries > 0, "The actual archive must exercise entries into both zones and the promised neutral exit.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard01")]
	public Task S0017_PercentStopWorksBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(Williams);

	[TestMethod]
	[TestCategory("Shard03")]
	public Task S0019_PercentStopWorksBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(Cci);

	[TestMethod]
	[TestCategory("Shard04")]
	public Task S0020_PercentStopWorksBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(MomentumPercentage, TimeSpan.FromDays(31), useSecondarySecurity: true);

	[TestMethod]
	[TestCategory("Shard02")]
	[DataRow(0.5)]
	[DataRow(1.0)]
	public async Task S0018_SignedPercentRocAndZeroExits(double threshold)
	{
		var closes = new Queue<decimal>();
		var count = 0;
		decimal? previous = null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var zeroExits = 0;
		var buys = 0;
		var sells = 0;
		var violations = new List<string>();
		await Replay(Roc, (strategy, _) =>
		{
			IsTrue(strategy.Parameters.TryGetValue("AtrMultiplier", out var stop), "README requires ATR risk, not an unprotected price-difference zero crossover.");
			AreEqual(2m, Convert.ToDecimal(stop.Value));
			AreEqual(12, strategy.Parameters["RocPeriod"].Value);
			AreEqual(14, strategy.Parameters["AtrPeriod"].Value);
			AreEqual(0.5m, Convert.ToDecimal(strategy.Parameters["ThresholdPercent"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "AtrMultiplier", 0m);
			SetParam(strategy, "ThresholdPercent", Convert.ToDecimal(threshold));
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				count++;
				closes.Enqueue(candle.ClosePrice);
				if (closes.Count > 13) closes.Dequeue();
				if (count < 14 || closes.Count < 13 || closes.Peek() == 0m) return;
				var value = 100m * (candle.ClosePrice - closes.Peek()) / closes.Peek();
				var level = Convert.ToDecimal(threshold);
				if (previous is decimal prev)
				{
					if (prev <= level && value > level && strategy.Position <= 0m)
					{
						expectedSide = Sides.Buy;
						expectedVolume = strategy.Volume + Math.Abs(strategy.Position);
					}
					else if (prev >= -level && value < -level && strategy.Position >= 0m)
					{
						expectedSide = Sides.Sell;
						expectedVolume = strategy.Volume + Math.Abs(strategy.Position);
					}
					else if (strategy.Position > 0m && value <= 0m || strategy.Position < 0m && value >= 0m)
					{
						expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
						expectedVolume = Math.Abs(strategy.Position);
						zeroExits++;
					}
					if (expectedSide is not null) expectedOrders++;
				}
				previous = value;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side == Sides.Buy) buys++; else sells++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add("ROC entries must cross the configured signed percentage level and exits must follow a real return to zero, without an undisclosed cooldown.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(buys > 0 && sells > 0 && zeroExits > 0);
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard02")]
	public Task S0018_AtrDistancesAndRiskParameterChangeRealExecutions()
		=> CheckAtrDistancesAndRiskParameterChangeRealExecutions(Roc, "ROC signal");

	[TestMethod]
	[TestCategory("Shard03")]
	[DataRow(20)]
	[DataRow(10)]
	public async Task S0019_CciBreakoutsAndZeroExits(int period)
	{
		var typicals = new Queue<decimal>();
		decimal? previous = null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var zeroExits = 0;
		var buys = 0;
		var sells = 0;
		var violations = new List<string>();
		await Replay(Cci, (strategy, _) =>
		{
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			AreEqual(20, strategy.Parameters["CciPeriod"].Value);
			IsTrue(strategy.Parameters.TryGetValue("StopLossPercent", out var stop));
			AreEqual(2m, Convert.ToDecimal(stop.Value));
			SetParam(strategy, "CciPeriod", period);
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				var typical = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
				typicals.Enqueue(typical);
				if (typicals.Count > period) typicals.Dequeue();
				if (typicals.Count < period) return;
				var average = typicals.Average();
				var deviation = typicals.Average(value => Math.Abs(value - average));
				if (deviation == 0m) return;
				var value = (typical - average) / deviation / 0.015m;
				if (previous is decimal prev)
				{
					if (prev <= 100m && value > 100m && strategy.Position <= 0m)
					{
						expectedSide = Sides.Buy;
						expectedVolume = strategy.Volume + Math.Abs(strategy.Position);
					}
					else if (prev >= -100m && value < -100m && strategy.Position >= 0m)
					{
						expectedSide = Sides.Sell;
						expectedVolume = strategy.Volume + Math.Abs(strategy.Position);
					}
					else if (strategy.Position > 0m && value <= 0m || strategy.Position < 0m && value >= 0m)
					{
						expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
						expectedVolume = Math.Abs(strategy.Position);
						zeroExits++;
					}
					if (expectedSide is not null) expectedOrders++;
				}
				previous = value;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side == Sides.Buy) buys++; else sells++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add("Every CCI order must follow independent typical-price/mean-deviation breakouts or zero exits; do not swallow Python value-conversion errors.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(buys > 0 && sells > 0 && zeroExits > 0);
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard04")]
	[DataRow(5.0, 20, true)]
	[DataRow(0.5, 10, false)]
	public async Task S0020_PercentageThresholdsAndSmaFilter(double threshold, int smaPeriod, bool useSecondarySecurity)
	{
		var closes = new Queue<decimal>();
		var smaCloses = new Queue<decimal>();
		decimal? previous = null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buys = 0;
		var sells = 0;
		var reversals = 0;
		var filteredCrossings = 0;
		var violations = new List<string>();
		await Replay(MomentumPercentage, (strategy, secondary) =>
		{
			if (useSecondarySecurity) strategy.Security = secondary;
			AreEqual(10, strategy.Parameters["MomentumPeriod"].Value);
			AreEqual(20, strategy.Parameters["SmaPeriod"].Value);
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			IsTrue(strategy.Parameters.TryGetValue("ThresholdPercent", out var level), "README promises 5% returns, not price-difference zero crossings.");
			AreEqual(5m, Convert.ToDecimal(level.Value));
			IsTrue(strategy.Parameters.TryGetValue("StopLossPercent", out var stop), "The published 2% stop must protect actual fills.");
			AreEqual(2m, Convert.ToDecimal(stop.Value));
			SetParam(strategy, "ThresholdPercent", Convert.ToDecimal(threshold));
			SetParam(strategy, "SmaPeriod", smaPeriod);
			// Only this signal oracle disables protection; default inventory rows retain 5%/2%.
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				closes.Enqueue(candle.ClosePrice);
				if (closes.Count > 11) closes.Dequeue();
				smaCloses.Enqueue(candle.ClosePrice);
				if (smaCloses.Count > smaPeriod) smaCloses.Dequeue();
				if (closes.Count < 11 || smaCloses.Count < smaPeriod || closes.Peek() == 0m) return;
				var momentum = 100m * (candle.ClosePrice - closes.Peek()) / closes.Peek();
				var sma = smaCloses.Average();
				var boundary = Convert.ToDecimal(threshold);
				if (previous is decimal prev)
				{
					if (prev <= boundary && momentum > boundary)
					{
						if (candle.ClosePrice <= sma) filteredCrossings++;
						else if (strategy.Position <= 0m) expectedSide = Sides.Buy;
					}
					else if (prev >= -boundary && momentum < -boundary)
					{
						if (candle.ClosePrice >= sma) filteredCrossings++;
						else if (strategy.Position >= 0m) expectedSide = Sides.Sell;
					}
					if (expectedSide is not null)
					{
						expectedVolume = strategy.Volume + Math.Abs(strategy.Position);
						expectedOrders++;
						if (strategy.Position != 0m) reversals++;
					}
				}
				previous = momentum;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side == Sides.Buy) buys++; else sells++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add("Every order must cross the configured signed percent threshold with SMA confirmation and full reversal quantity; no hidden cooldown or zero fade exit.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(actualOrders > 0, "The published 5% threshold must actually trade on the bundled archive, not be lowered for inventory acceptance.");
		if (threshold < 5.0)
			IsTrue(buys > 0 && sells > 0 && reversals > 0 && filteredCrossings > 0,
				"The lower-level fixture must exercise both directions, complete reversals and rejected SMA confirmations on actual archive bars.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	private const string Squeeze = "0021_Bollinger_Squeeze";

	[TestMethod]
	[TestCategory("Shard05")]
	[DataRow(20, 0.1)]
	[DataRow(14, 0.002)]
	public async Task S0021_SqueezeBreakoutsAndMiddleExits(int period, double threshold)
	{
		var closes = new Queue<decimal>();
		decimal? previousClose = null;
		var previousUpper = 0m;
		var previousLower = 0m;
		var previousWidth = 0m;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buyEntries = 0;
		var sellEntries = 0;
		var middleExits = 0;
		var rejectedCrossings = 0;
		var violations = new List<string>();
		await Replay(Squeeze, (strategy, _) =>
		{
			AreEqual(20, strategy.Parameters["BollingerPeriod"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["BollingerDeviation"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			IsTrue(strategy.Parameters.TryGetValue("SqueezeThreshold", out var level), "Computing unused band width is not the promised low-volatility squeeze filter.");
			AreEqual(0.1m, Convert.ToDecimal(level.Value));
			IsTrue(strategy.Parameters.TryGetValue("UseAtrStop", out var stop), "The conflicting Stops=No/ATR prose must have an explicit optional protection mode.");
			AreEqual(false, stop.Value);
			SetParam(strategy, "BollingerPeriod", period);
			SetParam(strategy, "SqueezeThreshold", Convert.ToDecimal(threshold));
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				closes.Enqueue(candle.ClosePrice);
				if (closes.Count > period) closes.Dequeue();
				if (closes.Count < period) return;
				var middle = closes.Average();
				if (middle == 0m) return;
				var variance = closes.Sum(close => (close - middle) * (close - middle)) / period;
				var distance = 2m * (decimal)Math.Sqrt((double)variance);
				var upper = middle + distance;
				var lower = middle - distance;
				var width = (upper - lower) / middle;
				if (previousClose is decimal prior)
				{
					var up = prior <= previousUpper && candle.ClosePrice > upper;
					var down = prior >= previousLower && candle.ClosePrice < lower;
					var narrow = previousWidth <= Convert.ToDecimal(threshold);
					if (!narrow && (up && strategy.Position <= 0m || down && strategy.Position >= 0m)) rejectedCrossings++;
					if (narrow && up && strategy.Position <= 0m)
					{
						expectedSide = Sides.Buy;
						expectedVolume = strategy.Volume + Math.Abs(strategy.Position);
						buyEntries++;
					}
					else if (narrow && down && strategy.Position >= 0m)
					{
						expectedSide = Sides.Sell;
						expectedVolume = strategy.Volume + Math.Abs(strategy.Position);
						sellEntries++;
					}
					else if (strategy.Position > 0m && candle.ClosePrice <= middle || strategy.Position < 0m && candle.ClosePrice >= middle)
					{
						expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
						expectedVolume = Math.Abs(strategy.Position);
						middleExits++;
					}
					if (expectedSide is not null) expectedOrders++;
				}
				previousClose = candle.ClosePrice;
				previousUpper = upper;
				previousLower = lower;
				previousWidth = width;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add("Every order must follow the independently calculated preceding-bar squeeze and actual outside-band crossing, or a middle exit, with one complete quantity and no hidden cooldown.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(buyEntries > 0 && sellEntries > 0 && middleExits > 0);
		if (threshold < 0.1) IsTrue(rejectedCrossings > 0, "The strict fixture must actually reject otherwise valid breakouts on wide preceding bands.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public Task S0021_AtrDistancesAndRiskParameterChangeRealExecutions()
		=> CheckAtrDistancesAndRiskParameterChangeRealExecutions(Squeeze, "Squeeze signal", optionalAtr: true);

	private const string AdxDi = "0022_ADX_DI";

	[TestMethod]
	[TestCategory("Shard06")]
	[DataRow(14, 25.0)]
	[DataRow(7, 15.0)]
	public async Task S0022_DirectionalCrossingsAndWeakeningExits(int period, double threshold)
	{
		var count = 0;
		var adxCount = 0;
		var atr = 0m;
		var plusMean = 0m;
		var minusMean = 0m;
		var adx = 0m;
		decimal? previousHigh = null, previousLow = null, previousClose = null;
		decimal? previousAdx = null;
		var previousPlus = 0m;
		var previousMinus = 0m;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buyEntries = 0;
		var sellEntries = 0;
		var weakeningExits = 0;
		var rejectedCrossings = 0;
		var violations = new List<string>();
		await Replay(AdxDi, (strategy, _) =>
		{
			AreEqual(14, strategy.Parameters["AdxPeriod"].Value);
			AreEqual(25m, Convert.ToDecimal(strategy.Parameters["AdxThreshold"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			IsTrue(strategy.Parameters.TryGetValue("AtrMultiplier", out var stop), "The documented ATR multiplier must provide actual protection.");
			AreEqual(2m, Convert.ToDecimal(stop.Value));
			AreEqual(14, strategy.Parameters["AtrPeriod"].Value);
			SetParam(strategy, "AdxPeriod", period);
			SetParam(strategy, "AdxThreshold", Convert.ToDecimal(threshold));
			// Isolate the directional contract; ordinary default rows retain the published ATR stop.
			SetParam(strategy, "AtrMultiplier", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				count++;
				var tr = previousClose is decimal close
					? Math.Max(candle.HighPrice - candle.LowPrice, Math.Max(Math.Abs(candle.HighPrice - close), Math.Abs(candle.LowPrice - close)))
					: candle.HighPrice - candle.LowPrice;
				var length = Math.Min(count, period);
				atr = (atr * (length - 1) + tr) / length;
				if (previousHigh is decimal high && previousLow is decimal low)
				{
					var up = candle.HighPrice - high;
					var down = low - candle.LowPrice;
					var dmLength = Math.Min(count - 1, period);
					plusMean = (plusMean * (dmLength - 1) + (up > 0m && up > down ? up : 0m)) / dmLength;
					minusMean = (minusMean * (dmLength - 1) + (down > 0m && down > up ? down : 0m)) / dmLength;
				}
				previousHigh = candle.HighPrice;
				previousLow = candle.LowPrice;
				previousClose = candle.ClosePrice;
				// Native DI has a first-delta and one-period formation delay before the sequential ADX mean starts.
				if (count < period + 2) return;
				var plus = atr == 0m ? 0m : 100m * plusMean / atr;
				var minus = atr == 0m ? 0m : 100m * minusMean / atr;
				var dx = plus + minus == 0m ? 0m : 100m * Math.Abs(plus - minus) / (plus + minus);
				adxCount++;
				var adxLength = Math.Min(adxCount, period);
				adx = (adx * (adxLength - 1) + dx) / adxLength;
				if (adxCount < period || count < 14) return;
				if (previousAdx is decimal prior)
				{
					var upCross = previousPlus <= previousMinus && plus > minus;
					var downCross = previousMinus <= previousPlus && minus > plus;
					var confirmed = adx >= Convert.ToDecimal(threshold) && adx > prior;
					if (!confirmed && (upCross && strategy.Position <= 0m || downCross && strategy.Position >= 0m)) rejectedCrossings++;
					if (confirmed && upCross && strategy.Position <= 0m)
					{
						expectedSide = Sides.Buy;
						expectedVolume = strategy.Volume + Math.Abs(strategy.Position);
						buyEntries++;
					}
					else if (confirmed && downCross && strategy.Position >= 0m)
					{
						expectedSide = Sides.Sell;
						expectedVolume = strategy.Volume + Math.Abs(strategy.Position);
						sellEntries++;
					}
					else if (strategy.Position > 0m && (adx < prior || downCross) || strategy.Position < 0m && (adx < prior || upCross))
					{
						expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
						expectedVolume = Math.Abs(strategy.Position);
						if (adx < prior) weakeningExits++;
					}
					if (expectedSide is not null) expectedOrders++;
				}
				previousAdx = adx;
				previousPlus = plus;
				previousMinus = minus;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add("Each order must follow independent Wilder DM/TR/DI/DX/ADX, a strict non-equality DI crossing with rising strong ADX, or an unblocked weakening/opposite exit, with full quantity.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(buyEntries + sellEntries > 0 && weakeningExits > 0 && rejectedCrossings > 0);
		if (period < 14) IsTrue(buyEntries > 0 && sellEntries > 0);
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public Task S0022_AtrDistancesAndRiskParameterChangeRealExecutions()
		=> CheckAtrDistancesAndRiskParameterChangeRealExecutions(AdxDi, "ADX signal", signalSetup: strategy =>
		{
			AreEqual(14, strategy.Parameters["AdxPeriod"].Value);
			AreEqual(25m, Convert.ToDecimal(strategy.Parameters["AdxThreshold"].Value));
			// Ordinary rows retain 14/25. This independently checked 7/15 signal fixture exercises repeated ATR fills.
			SetParam(strategy, "AdxPeriod", 7);
			SetParam(strategy, "AdxThreshold", 15m);
		});

	private const string Impulse = "0023_Elder_Impulse";

	private sealed class ImpulseEma(int length)
	{
		private decimal _sum;
		public int Count { get; private set; }
		public decimal Value { get; private set; }
		public bool Formed => Count >= length;
		public decimal Add(decimal value)
		{
			Count++;
			if (Count <= length)
			{
				_sum += value;
				Value = _sum / length;
			}
			else
				Value += (value - Value) * (2m / (length + 1));
			return Value;
		}
	}

	[TestMethod]
	[TestCategory("Shard07")]
	[DataRow(13, 12, 26, 9)]
	[DataRow(7, 5, 10, 3)]
	public async Task S0023_ImpulseColorsAndNeutralExits(int emaPeriod, int fastPeriod, int slowPeriod, int signalPeriod)
	{
		var ema = new ImpulseEma(emaPeriod);
		var fast = new ImpulseEma(fastPeriod);
		var slow = new ImpulseEma(slowPeriod);
		var signal = new ImpulseEma(signalPeriod);
		decimal? previousEma = null;
		var previousHistogram = 0m;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buyEntries = 0;
		var sellEntries = 0;
		var neutralExits = 0;
		var violations = new List<string>();
		await Replay(Impulse, (strategy, _) =>
		{
			AreEqual(13, strategy.Parameters["EmaPeriod"].Value);
			foreach (var (name, value) in new[] { ("MacdFastPeriod", 12), ("MacdSlowPeriod", 26), ("MacdSignalPeriod", 9) })
			{
				IsTrue(strategy.Parameters.TryGetValue(name, out var period), $"The published {name} must exist and drive the actual indicator.");
				AreEqual(value, period.Value);
			}
			IsTrue(strategy.Parameters.TryGetValue("StopLossPercent", out var stop));
			AreEqual(2m, Convert.ToDecimal(stop.Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "EmaPeriod", emaPeriod);
			SetParam(strategy, "MacdFastPeriod", fastPeriod);
			SetParam(strategy, "MacdSlowPeriod", slowPeriod);
			SetParam(strategy, "MacdSignalPeriod", signalPeriod);
			// Isolate color/filter/exit decisions; ordinary default rows keep native 2% protection.
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				var value = ema.Add(candle.ClosePrice);
				var macd = fast.Add(candle.ClosePrice) - slow.Add(candle.ClosePrice);
				if (slow.Formed) signal.Add(macd);
				if (!ema.Formed || !slow.Formed || !signal.Formed) return;
				var histogram = macd - signal.Value;
				if (previousEma is decimal prior)
				{
					var color = value > prior && histogram > previousHistogram ? 1
						: value < prior && histogram < previousHistogram ? -1 : 0;
					if (color == 1 && candle.ClosePrice > value && strategy.Position <= 0m)
					{
						expectedSide = Sides.Buy;
						expectedVolume = strategy.Volume + Math.Abs(strategy.Position);
						buyEntries++;
					}
					else if (color == -1 && candle.ClosePrice < value && strategy.Position >= 0m)
					{
						expectedSide = Sides.Sell;
						expectedVolume = strategy.Volume + Math.Abs(strategy.Position);
						sellEntries++;
					}
					else if (strategy.Position > 0m && color != 1 || strategy.Position < 0m && color != -1)
					{
						expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
						expectedVolume = Math.Abs(strategy.Position);
						if (color == 0) neutralExits++;
					}
					if (expectedSide is not null) expectedOrders++;
				}
				previousEma = value;
				previousHistogram = histogram;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add("Each order must follow independently seeded EMA/MACD/histogram slopes, price/EMA confirmation or neutral/opposite color exit, with full quantities and no hidden cooldown.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(buyEntries > 0 && sellEntries > 0 && neutralExits > 0);
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard07")]
	public Task S0023_PercentStopWorksBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(Impulse);

	private const string Laguerre = "0024_RSI_Laguerre";

	// Independent scalar recurrence, not a second invocation of the production indicator.
	private sealed class LaguerreOracle(decimal gamma)
	{
		private decimal _l0, _l1, _l2, _l3, _up, _down;
		public decimal Add(decimal close)
		{
			var l0 = (1m - gamma) * close + gamma * _l0;
			var l1 = -gamma * l0 + _l0 + gamma * _l1;
			var l2 = -gamma * l1 + _l1 + gamma * _l2;
			var l3 = -gamma * l2 + _l2 + gamma * _l3;
			var up = Math.Max(l0 - l1, 0m) + Math.Max(l1 - l2, 0m) + Math.Max(l2 - l3, 0m);
			var down = Math.Max(l1 - l0, 0m) + Math.Max(l2 - l1, 0m) + Math.Max(l3 - l2, 0m);
			_up = (1m - gamma) * up + gamma * _up;
			_down = (1m - gamma) * down + gamma * _down;
			(_l0, _l1, _l2, _l3) = (l0, l1, l2, l3);
			return _up + _down == 0m ? 50m : _up / (_up + _down) * 100m;
		}
	}

	[TestMethod]
	[TestCategory("Shard00")]
	[DataRow("0.7")]
	[DataRow("0.4")]
	public async Task S0024_LaguerreRecurrenceCrossingsAndMidpointExits(string gammaText)
	{
		var gamma = decimal.Parse(gammaText, CultureInfo.InvariantCulture);
		var oracle = new LaguerreOracle(gamma);
		decimal? previous = null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buyEntries = 0;
		var sellEntries = 0;
		var midpointExits = 0;
		var violations = new List<string>();
		await Replay(Laguerre, (strategy, _) =>
		{
			IsTrue(strategy.Parameters.TryGetValue("Gamma", out var smoothing), "The published Gamma must drive a genuine Laguerre recurrence, not an ordinary RSI period.");
			AreEqual(0.7m, Convert.ToDecimal(smoothing.Value));
			IsTrue(strategy.Parameters.TryGetValue("StopLossPercent", out var stop));
			AreEqual(2m, Convert.ToDecimal(stop.Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "Gamma", gamma);
			// Separate signal/quantity acceptance from the ordinary default and actual-fill stop rows.
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				var value = oracle.Add(candle.ClosePrice);
				if (previous is decimal prior)
				{
					if (prior < 30m && value >= 30m && strategy.Position <= 0m)
					{
						expectedSide = Sides.Buy;
						expectedVolume = strategy.Volume + Math.Abs(strategy.Position);
						buyEntries++;
					}
					else if (prior > 70m && value <= 70m && strategy.Position >= 0m)
					{
						expectedSide = Sides.Sell;
						expectedVolume = strategy.Volume + Math.Abs(strategy.Position);
						sellEntries++;
					}
					else if (strategy.Position > 0m && value >= 50m || strategy.Position < 0m && value <= 50m)
					{
						expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
						expectedVolume = Math.Abs(strategy.Position);
						midpointExits++;
					}
					if (expectedSide is not null) expectedOrders++;
				}
				previous = value;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add("Each order must follow the independently calculated four-stage Laguerre recurrence, native up/down smoothing, 30/70 crossings or midpoint exit, with complete quantities and no hidden cooldown.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(buyEntries > 0 && sellEntries > 0 && midpointExits > 0);
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public Task S0024_PercentStopWorksBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(Laguerre);

	private const string StochRsi = "0025_Stochastic_RSI_Cross";

	private sealed class RsiOracle(int length)
	{
		private decimal? _previousClose, _previousResult;
		private decimal _gain, _loss, _gainSum, _lossSum;
		public int Deltas { get; private set; }
		public bool Formed => Deltas >= length;
		public decimal? Add(decimal close)
		{
			var previous = _previousClose;
			_previousClose = close;
			if (previous is not decimal prior) return null;
			var delta = close - prior;
			var gain = Math.Max(delta, 0m);
			var loss = Math.Max(-delta, 0m);
			Deltas++;
			if (Deltas <= length)
			{
				_gainSum += gain;
				_lossSum += loss;
				_gain = _gainSum / length;
				_loss = _lossSum / length;
			}
			else
			{
				_gain = (_gain * (length - 1) + gain) / length;
				_loss = (_loss * (length - 1) + loss) / length;
			}
			if (delta == 0m && _previousResult is decimal same) return same;
			if (_gain + _loss == 0m) return _previousResult ?? 50m;
			return _previousResult = 100m * _gain / (_gain + _loss);
		}
	}

	[TestMethod]
	[TestCategory("Shard01")]
	[DataRow(14, 14, 3, 3)]
	[DataRow(5, 7, 2, 2)]
	[DataRow(1, 5, 3, 3)]
	public async Task S0025_ActualRsiNormalizationSmoothingCrossesAndExits(int rsiPeriod, int stochPeriod, int kPeriod, int dPeriod)
	{
		var rsi = new RsiOracle(rsiPeriod);
		var range = new Queue<decimal>();
		var kWindow = new Queue<decimal>();
		var dWindow = new Queue<decimal>();
		decimal? previousK = null;
		var previousD = 0m;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buyEntries = 0;
		var sellEntries = 0;
		var unqualifiedCrossExits = 0;
		var neutralRanges = 0;
		var violations = new List<string>();
		decimal? oracleRsi = null, oracleK = null, oracleD = null;
		var scalarChecks = 0;
		await Replay(StochRsi, (strategy, _) =>
		{
			foreach (var (name, defaultValue, value) in new[] { ("RsiPeriod", 14, rsiPeriod), ("StochPeriod", 14, stochPeriod), ("KPeriod", 3, kPeriod), ("DPeriod", 3, dPeriod) })
			{
				IsTrue(strategy.Parameters.TryGetValue(name, out var parameter), $"The actual StochRSI chain requires {name}.");
				AreEqual(defaultValue, parameter.Value);
				SetParam(strategy, name, value);
			}
			IsTrue(strategy.Parameters.TryGetValue("StopLossPercent", out var stop));
			AreEqual(2m, Convert.ToDecimal(stop.Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "StopLossPercent", 0m);
			var movingAverageIndex = 0;
			strategy.Indicators.Added += indicator =>
			{
				var label = indicator is RelativeStrengthIndex ? "RSI" : indicator is SimpleMovingAverage
					? ++movingAverageIndex == 1 ? "K" : "D" : null;
				if (label is null) return;
				indicator.Changed += (input, output) =>
				{
					if (!input.IsFinal || output.IsEmpty) return;
					var expected = label == "RSI" ? oracleRsi : label == "K" ? oracleK : oracleD;
					if (expected is not decimal number) return;
					var actual = output.GetValue<decimal>();
					scalarChecks++;
					if (Math.Abs(actual - number) > 0.0000000001m)
						violations.Add($"{input.Time:O} {label}: expected={number}, actual={actual}.");
				};
			};
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				oracleK = oracleD = null;
				var result = rsi.Add(candle.ClosePrice);
				oracleRsi = result;
				if (!rsi.Formed || result is not decimal value) return;
				range.Enqueue(value);
				if (range.Count > stochPeriod) range.Dequeue();
				if (range.Count < stochPeriod) return;
				var low = range.Min();
				var high = range.Max();
				if (high == low) neutralRanges++;
				var raw = high == low ? 50m : 100m * (value - low) / (high - low);
				kWindow.Enqueue(raw);
				if (kWindow.Count > kPeriod) kWindow.Dequeue();
				if (kWindow.Count < kPeriod) return;
				var k = kWindow.Sum() / kPeriod;
				oracleK = k;
				dWindow.Enqueue(k);
				if (dWindow.Count > dPeriod) dWindow.Dequeue();
				if (dWindow.Count < dPeriod) return;
				var d = dWindow.Sum() / dPeriod;
				oracleD = d;
				// The published decision scale is eight decimal places, including equality plateaus.
				k = Math.Round(k, 8);
				d = Math.Round(d, 8);
				if (previousK is decimal priorK)
				{
					var up = priorK <= previousD && k > d;
					var down = priorK >= previousD && k < d;
					if (up && k < 20m && strategy.Position <= 0m)
					{
						expectedSide = Sides.Buy;
						expectedVolume = strategy.Volume + Math.Abs(strategy.Position);
						buyEntries++;
					}
					else if (down && k > 80m && strategy.Position >= 0m)
					{
						expectedSide = Sides.Sell;
						expectedVolume = strategy.Volume + Math.Abs(strategy.Position);
						sellEntries++;
					}
					else if (strategy.Position > 0m && down || strategy.Position < 0m && up)
					{
						expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
						expectedVolume = Math.Abs(strategy.Position);
						unqualifiedCrossExits++;
					}
					if (expectedSide is not null) expectedOrders++;
				}
				previousK = k;
				previousD = d;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add($"{order.Time:O}: every order must follow independently calculated RSI, RSI extrema, sequential K/D SMAs and current-K extreme filter; opposite unqualified crosses exit only.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		AreEqual(expectedOrders, actualOrders, string.Join(Environment.NewLine, violations.Take(12)));
		IsTrue(scalarChecks > 100, "The independent chain must also match intermediate native indicator values.");
		IsTrue(buyEntries > 0 && sellEntries > 0 && unqualifiedCrossExits > 0);
		if (rsiPeriod == 1) IsTrue(neutralRanges > 0, "Real plateau RSI windows must exercise the zero-range neutral 50 branch.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard01")]
	public Task S0025_PercentStopWorksBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(StochRsi);

	private const string RsiReversion = "0026_RSI_Reversion";

	[TestMethod]
	[TestCategory("Shard02")]
	[DataRow(14, 30, 70, 50)]
	[DataRow(7, 35, 65, 45)]
	[DataRow(1, 30, 70, 50)]
	public async Task S0026_ExtremeEntryCrossingsAndIndependentNeutralExits(int period, int oversold, int overbought, int exitLevel)
	{
		var rsi = new RsiOracle(period);
		decimal? previous = null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buyEntries = 0;
		var sellEntries = 0;
		var neutralExits = 0;
		var zeroReadings = 0;
		var violations = new List<string>();
		await Replay(RsiReversion, (strategy, _) =>
		{
			AreEqual(14, strategy.Parameters["RsiPeriod"].Value);
			SetParam(strategy, "RsiPeriod", period);
			foreach (var (name, defaultValue, value) in new[] { ("OversoldThreshold", 30m, (decimal)oversold), ("OverboughtThreshold", 70m, (decimal)overbought), ("ExitLevel", 50m, (decimal)exitLevel) })
			{
				IsTrue(strategy.Parameters.TryGetValue(name, out var parameter), $"The published {name} must affect actual decisions.");
				AreEqual(defaultValue, Convert.ToDecimal(parameter.Value));
				SetParam(strategy, name, value);
			}
			IsTrue(strategy.Parameters.TryGetValue("StopLossPercent", out var stop));
			AreEqual(2m, Convert.ToDecimal(stop.Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				var result = rsi.Add(candle.ClosePrice);
				if (!rsi.Formed || result is not decimal value) return;
				if (value == 0m) zeroReadings++;
				if (previous is decimal prior)
				{
					if (prior >= oversold && value < oversold && strategy.Position <= 0m)
					{
						expectedSide = Sides.Buy;
						expectedVolume = strategy.Volume + Math.Abs(strategy.Position);
						buyEntries++;
					}
					else if (prior <= overbought && value > overbought && strategy.Position >= 0m)
					{
						expectedSide = Sides.Sell;
						expectedVolume = strategy.Volume + Math.Abs(strategy.Position);
						sellEntries++;
					}
					else if (strategy.Position > 0m && value >= exitLevel || strategy.Position < 0m && value <= exitLevel)
					{
						expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
						expectedVolume = Math.Abs(strategy.Position);
						neutralExits++;
					}
					if (expectedSide is not null) expectedOrders++;
				}
				previous = value;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add("Each order must follow independent Wilder RSI entering the oversold/overbought zone, or a neutral-level exit, with full reversal/closing quantities and no hidden SMA/cooldown.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(buyEntries > 0 && sellEntries > 0);
		if (period == 1) IsTrue(zeroReadings > 0, "A zero RSI is valid and must actually participate in entry/exit decisions.");
		else IsTrue(neutralExits > 0);
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard02")]
	public Task S0026_PercentStopWorksBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(RsiReversion);

	private const string BollingerReversion = "0027_Bollinger_Reversion";

	[TestMethod]
	[TestCategory("Shard03")]
	[DataRow(20, 2.0)]
	[DataRow(10, 1.5)]
	public async Task S0027_OutsideCloseEntriesAndReturnInsideExits(int period, double deviation)
	{
		var closes = new Queue<decimal>();
		var bars = 0;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buyEntries = 0;
		var sellEntries = 0;
		var insideExitsBeforeMiddle = 0;
		var violations = new List<string>();
		await Replay(BollingerReversion, (strategy, _) =>
		{
			AreEqual(20, strategy.Parameters["BollingerPeriod"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["BollingerDeviation"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			IsTrue(strategy.Parameters.TryGetValue("AtrMultiplier", out var stop), "The published ATR stop must actually exist.");
			AreEqual(2m, Convert.ToDecimal(stop.Value));
			IsTrue(strategy.Parameters.TryGetValue("AtrPeriod", out var atr));
			AreEqual(14, atr.Value);
			SetParam(strategy, "BollingerPeriod", period);
			SetParam(strategy, "BollingerDeviation", Convert.ToDecimal(deviation));
			// Signal-only isolation; default/risk rows retain the published ATR multiplier.
			SetParam(strategy, "AtrMultiplier", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				bars++;
				expectedSide = null;
				closes.Enqueue(candle.ClosePrice);
				if (closes.Count > period) closes.Dequeue();
				if (bars < Math.Max(period, 14)) return;
				var middle = closes.Average();
				var variance = closes.Sum(close => (close - middle) * (close - middle)) / period;
				var distance = Convert.ToDecimal(deviation) * (decimal)Math.Sqrt((double)variance);
				var upper = middle + distance;
				var lower = middle - distance;
				var price = candle.ClosePrice;
				if (strategy.Position > 0m && price >= lower)
				{
					expectedSide = Sides.Sell;
					expectedVolume = strategy.Position;
					if (price < middle) insideExitsBeforeMiddle++;
				}
				else if (strategy.Position < 0m && price <= upper)
				{
					expectedSide = Sides.Buy;
					expectedVolume = Math.Abs(strategy.Position);
					if (price > middle) insideExitsBeforeMiddle++;
				}
				else if (strategy.Position == 0m && price < lower)
				{
					expectedSide = Sides.Buy;
					expectedVolume = strategy.Volume;
					buyEntries++;
				}
				else if (strategy.Position == 0m && price > upper)
				{
					expectedSide = Sides.Sell;
					expectedVolume = strategy.Volume;
					sellEntries++;
				}
				if (expectedSide is not null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add("Every order must follow independent population Bollinger bands: flat outside-close entry, otherwise full exit on return inside, not an unrelated cooldown/time or middle-band exit.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(buyEntries > 0 && sellEntries > 0 && insideExitsBeforeMiddle > 0);
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard03")]
	public Task S0027_AtrDistancesAndRiskParameterChangeRealExecutions()
		=> CheckAtrDistancesAndRiskParameterChangeRealExecutions(BollingerReversion, "Bollinger reversion entry");

	private const string ZScore = "0028_ZScore";

	[TestMethod]
	[TestCategory("Shard04")]
	[DataRow(20, 20, 2.0, 0.0)]
	[DataRow(10, 12, 1.5, 0.5)]
	[DataRow(2, 2, 0.5, 0.25)]
	public async Task S0028_IndependentZScoreExtremesNeutralZoneAndZeroVariance(int maPeriod, int stdPeriod, double entryLevel, double exitLevel)
	{
		var meanWindow = new Queue<decimal>();
		var stdWindow = new Queue<decimal>();
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buyEntries = 0;
		var sellEntries = 0;
		var neutralExitsBeforeZero = 0;
		var zeroVarianceExits = 0;
		var violations = new List<string>();
		await Replay(ZScore, (strategy, _) =>
		{
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["ZScoreEntryThreshold"].Value));
			AreEqual(0m, Convert.ToDecimal(strategy.Parameters["ZScoreExitThreshold"].Value));
			AreEqual(20, strategy.Parameters["MAPeriod"].Value);
			AreEqual(20, strategy.Parameters["StdDevPeriod"].Value);
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			IsTrue(strategy.Parameters.TryGetValue("StopLossPercent", out var stop));
			AreEqual(2m, Convert.ToDecimal(stop.Value));
			SetParam(strategy, "MAPeriod", maPeriod);
			SetParam(strategy, "StdDevPeriod", stdPeriod);
			SetParam(strategy, "ZScoreEntryThreshold", Convert.ToDecimal(entryLevel));
			SetParam(strategy, "ZScoreExitThreshold", Convert.ToDecimal(exitLevel));
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				meanWindow.Enqueue(candle.ClosePrice);
				stdWindow.Enqueue(candle.ClosePrice);
				if (meanWindow.Count > maPeriod) meanWindow.Dequeue();
				if (stdWindow.Count > stdPeriod) stdWindow.Dequeue();
				if (meanWindow.Count < maPeriod || stdWindow.Count < stdPeriod) return;
				var mean = meanWindow.Average();
				var stdMean = stdWindow.Average();
				var variance = stdWindow.Sum(close => (close - stdMean) * (close - stdMean)) / stdPeriod;
				var sigma = (decimal)Math.Sqrt((double)variance);
				var z = sigma == 0m ? 0m : (candle.ClosePrice - mean) / sigma;
				var entry = Convert.ToDecimal(entryLevel);
				var exit = Convert.ToDecimal(exitLevel);
				if (strategy.Position > 0m && z >= -exit || strategy.Position < 0m && z <= exit)
				{
					expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
					expectedVolume = Math.Abs(strategy.Position);
					if (strategy.Position > 0m && z < 0m || strategy.Position < 0m && z > 0m) neutralExitsBeforeZero++;
					if (sigma == 0m) zeroVarianceExits++;
				}
				else if (strategy.Position == 0m && z < -entry)
				{
					expectedSide = Sides.Buy;
					expectedVolume = strategy.Volume;
					buyEntries++;
				}
				else if (strategy.Position == 0m && z > entry)
				{
					expectedSide = Sides.Sell;
					expectedVolume = strategy.Volume;
					sellEntries++;
				}
				if (expectedSide is not null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add("Each order must follow independently calculated SMA and its own population-standard-deviation window, signed extreme entries or return to the configured neutral zone, without hidden cooldowns.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(buyEntries > 0 && sellEntries > 0);
		if (maPeriod == 10) IsTrue(neutralExitsBeforeZero > 0, "A nonzero exit band must actually close before crossing zero, not merely add metadata.");
		if (stdPeriod == 2) IsTrue(zeroVarianceExits > 0, "Real constant-price windows must close held positions as neutral instead of blocking all exits.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard04")]
	public Task S0028_PercentStopWorksBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(ZScore);

	private const string MADeviation = "0029_MA_Deviation";

	[TestMethod]
	[TestCategory("Shard05")]
	[DataRow(20, 5.0)]
	[DataRow(10, 0.5)]
	public async Task S0029_PercentDeviationEntriesAndFullMeanExits(int period, double threshold)
	{
		var closes = new Queue<decimal>();
		var bars = 0;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buys = 0;
		var sells = 0;
		var exits = 0;
		var violations = new List<string>();
		await Replay(MADeviation, (strategy, secondary) =>
		{
			AreEqual(20, strategy.Parameters["MAPeriod"].Value);
			AreEqual(5m, Convert.ToDecimal(strategy.Parameters["DeviationPercent"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			IsTrue(strategy.Parameters.TryGetValue("AtrPeriod", out var atr));
			AreEqual(14, atr.Value);
			IsTrue(strategy.Parameters.TryGetValue("AtrMultiplier", out var multiplier));
			AreEqual(2m, Convert.ToDecimal(multiplier.Value));
			IsTrue(strategy.Parameters.TryGetValue("RiskPercent", out var risk), "ATR sizing must have an explicit configurable budget, not an unused indicator.");
			AreEqual(1m, Convert.ToDecimal(risk.Value));
			if (threshold == 5.0) strategy.Security = secondary;
			SetParam(strategy, "MAPeriod", period);
			SetParam(strategy, "DeviationPercent", Convert.ToDecimal(threshold));
			// Only the pure signal oracle disables ATR stop/sizing. Protected default rows retain both.
			SetParam(strategy, "AtrMultiplier", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				bars++;
				expectedSide = null;
				closes.Enqueue(candle.ClosePrice);
				if (closes.Count > period) closes.Dequeue();
				if (bars < Math.Max(period, 14)) return;
				var mean = closes.Average();
				var deviation = 100m * (candle.ClosePrice - mean) / mean;
				if (strategy.Position > 0m && candle.ClosePrice >= mean || strategy.Position < 0m && candle.ClosePrice <= mean)
				{
					expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
					expectedVolume = Math.Abs(strategy.Position);
					exits++;
				}
				else if (strategy.Position == 0m && Math.Abs(deviation) > Convert.ToDecimal(threshold))
				{
					expectedSide = deviation < 0m ? Sides.Buy : Sides.Sell;
					expectedVolume = strategy.Volume;
					if (expectedSide == Sides.Buy) buys++; else sells++;
				}
				if (expectedSide is not null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add("Every signal-only order must fade the independent percentage deviation while flat or fully close on return to SMA; no hidden cooldown or reversal.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(buys > 0 && sells > 0 && exits > 0);
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard05")]
	[DataRow(false, 14)]
	[DataRow(true, 7)]
	public async Task S0029_AtrSizingUsesBudgetCurrencyAndVolumeGrid(bool stepValue, int atrPeriod)
	{
		var closes = new Queue<decimal>();
		decimal? previousClose = null;
		var atr = 0m;
		var bars = 0;
		var lastClose = 0m;
		var entries = 0;
		var skippedSignals = 0;
		var cashCappedEntries = 0;
		var volumes = new HashSet<decimal>();
		var violations = new List<string>();
		await Replay(MADeviation, (strategy, _) =>
		{
			IsTrue(strategy.Parameters.ContainsKey("AtrPeriod") && strategy.Parameters.ContainsKey("AtrMultiplier") && strategy.Parameters.ContainsKey("RiskPercent"));
			SetParam(strategy, "AtrPeriod", atrPeriod);
			SetParam(strategy, "DeviationPercent", 0.5m);
			SetParam(strategy, "RiskPercent", 0.002m);
			strategy.Security.VolumeStep = 0.003m;
			strategy.Security.MinVolume = 0.004m;
			strategy.Security.MaxVolume = 0.099m;
			strategy.Security.Multiplier = 3m;
			strategy.Security.StepPrice = stepValue ? 0.07m : null;
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				var tr = previousClose is decimal close
					? Math.Max(candle.HighPrice - candle.LowPrice, Math.Max(Math.Abs(candle.HighPrice - close), Math.Abs(candle.LowPrice - close)))
					: candle.HighPrice - candle.LowPrice;
				bars++;
				var length = Math.Min(bars, atrPeriod);
				atr = (atr * (length - 1) + tr) / length;
				previousClose = candle.ClosePrice;
				lastClose = candle.ClosePrice;
				closes.Enqueue(lastClose);
				if (closes.Count > 20) closes.Dequeue();
				var cashPhase = bars >= 1000 && bars < 1500;
				strategy.Security.MaxVolume = cashPhase ? 1000m : 0.099m;
				var risk = cashPhase ? 50m : bars >= 2000 && bars < 4000 ? 0.000000001m : 0.002m;
				SetParam(strategy, "RiskPercent", risk);
				if (closes.Count == 20 && strategy.Position == 0m && Math.Abs(100m * (lastClose - closes.Average()) / closes.Average()) > 0.5m
					&& ExpectedVolume() == 0m) skippedSignals++;
			};
			decimal ExpectedVolume()
			{
				var moneyFactor = stepValue ? 0.07m / strategy.Security.PriceStep.Value : 3m;
				var balance = strategy.Portfolio.CurrentValue ?? strategy.Portfolio.BeginValue ?? 0m;
				var budget = balance * Convert.ToDecimal(strategy.Parameters["RiskPercent"].Value) / 100m;
				var distance = atr * 2m;
				if (distance <= 0m || balance <= 0m) return 0m;
				var units = Math.Min(strategy.Security.MaxVolume.Value, Math.Min(budget / (distance * moneyFactor), balance / (lastClose * moneyFactor)));
				var rounded = Math.Floor(units / 0.003m) * 0.003m;
				return rounded >= 0.006m ? rounded : 0m;
			}
			strategy.OrderRegistering += order =>
			{
				if (order.Comment != "MA deviation entry") return;
				entries++;
				volumes.Add(order.Volume);
				if (bars >= 1000 && bars < 1500 && order.Volume > 0.099m) cashCappedEntries++;
				var mean = closes.Average();
				if (closes.Count < 20 || strategy.Position != 0m || order.Volume != ExpectedVolume() || order.Volume <= 0m
					|| order.Type != OrderTypes.Market || order.Side != (lastClose < mean ? Sides.Buy : Sides.Sell)
					|| Math.Abs(100m * (lastClose - mean) / mean) <= 0.5m)
					violations.Add("Each entry quantity must use current budget / independent frozen ATR cash risk, correct StepPrice or Multiplier, floor grid/max/cash cap, and skip rather than exceed risk below the minimum grid.");
			};
		}, TimeSpan.FromDays(31));
		IsTrue(entries > 3 && volumes.Count > 1 && skippedSignals > 0 && cashCappedEntries > 0,
			$"Need changing quantities, actual cash-capped entries and rejected sub-minimum signals: entries={entries}, volumes={volumes.Count}, cashCapped={cashCappedEntries}, skipped={skippedSignals}.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public Task S0029_AtrDistancesAndRiskParameterChangeRealExecutions()
		=> CheckAtrDistancesAndRiskParameterChangeRealExecutions(MADeviation, "MA deviation entry", signalSetup: strategy =>
		{
			SetParam(strategy, "DeviationPercent", 0.5m);
			SetParam(strategy, "RiskPercent", 0.002m);
			strategy.Security.MaxVolume = 1m;
		});

	private const string VwapReversion = "0030_VWAP_Reversion";

	[TestMethod]
	[TestCategory("Shard06")]
	[DataRow(2.0, 14, 30, 70)]
	[DataRow(0.5, 14, 30, 70)]
	[DataRow(0.5, 7, 25, 75)]
	public async Task S0030_CumulativeTypicalPriceVolumeUtcSessionsAndMeanExits(double threshold, int rsiPeriod, int oversold, int overbought)
	{
		var rsi = new RsiOracle(rsiPeriod);
		decimal? expectedRsi = null;
		var rsiComparisons = 0;
		var nativeRsiSeen = false;
		var rsiRejections = 0;
		DateTime? session = null;
		var cumulativeValue = 0m;
		var cumulativeVolume = 0m;
		var sessions = 0;
		var expectedVwap = 0m;
		var comparisons = 0;
		var nativeVwapSeen = false;
		var wrongVwmaEntries = 0;
		var rollingBars = new Queue<ICandleMessage>();
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buys = 0;
		var sells = 0;
		var exits = 0;
		var violations = new List<string>();
		await Replay(VwapReversion, (strategy, _) =>
		{
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["DeviationPercent"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			IsTrue(strategy.Parameters.TryGetValue("StopLossPercent", out var stop));
			AreEqual(2m, Convert.ToDecimal(stop.Value));
			SetParam(strategy, "DeviationPercent", Convert.ToDecimal(threshold));
			IsTrue(strategy.Parameters.TryGetValue("RsiPeriod", out var period), "README entries are based on RSI as well as VWAP.");
			AreEqual(14, period.Value);
			SetParam(strategy, "RsiPeriod", rsiPeriod);
			foreach (var (name, defaultValue, value) in new[] { ("RsiOversold", 30m, (decimal)oversold), ("RsiOverbought", 70m, (decimal)overbought) })
			{
				IsTrue(strategy.Parameters.TryGetValue(name, out var level), $"The RSI confirmation level {name} must be a published parameter.");
				AreEqual(defaultValue, Convert.ToDecimal(level.Value));
				SetParam(strategy, name, value);
			}
			// Isolate every signal/quantity; default and intrabar rows retain native protection.
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.Indicators.Added += indicator =>
			{
				if (indicator is RelativeStrengthIndex)
				{
					nativeRsiSeen = true;
					indicator.Changed += (_, output) =>
					{
						if (!output.IsFinal || output.IsEmpty) return;
						rsiComparisons++;
						if (expectedRsi is not decimal oracle || Math.Abs(output.GetValue<decimal>() - oracle) > 0.00000001m)
							violations.Add("Native RSI must independently match Wilder RSI over every finished close, never reset per UTC session.");
					};
					return;
				}
				if (indicator is not VolumeWeightedAveragePrice) return;
				nativeVwapSeen = true;
				indicator.Changed += (_, output) =>
				{
					if (!output.IsFinal || output.IsEmpty) return;
					comparisons++;
					if (Math.Abs(output.GetValue<decimal>() - expectedVwap) > 0.00000001m)
						violations.Add("Native VWAP must independently match cumulative typical-price times actual volume, resetting before the first finished candle of each UTC date.");
				};
			};
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				expectedRsi = rsi.Add(candle.ClosePrice);
				var date = candle.OpenTime.ToUniversalTime().Date;
				if (session != date)
				{
					session = date;
					sessions++;
					cumulativeValue = 0m;
					cumulativeVolume = 0m;
				}
				var typical = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
				cumulativeValue += typical * candle.TotalVolume;
				cumulativeVolume += candle.TotalVolume;
				rollingBars.Enqueue(candle);
				if (rollingBars.Count > 32) rollingBars.Dequeue();
				if (cumulativeVolume <= 0m) return;
				expectedVwap = cumulativeValue / cumulativeVolume;
				var close = candle.ClosePrice;
				var deviation = 100m * (close - expectedVwap) / expectedVwap;
				if (strategy.Position > 0m && close >= expectedVwap || strategy.Position < 0m && close <= expectedVwap)
				{
					expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
					expectedVolume = Math.Abs(strategy.Position);
					exits++;
				}
				else if (strategy.Position == 0m && Math.Abs(deviation) > Convert.ToDecimal(threshold))
				{
					var confirmed = rsi.Formed && expectedRsi is decimal current && (deviation < 0m ? current < oversold : current > overbought);
					if (!confirmed)
					{
						rsiRejections++;
						return;
					}
					expectedSide = deviation < 0m ? Sides.Buy : Sides.Sell;
					expectedVolume = strategy.Volume;
					if (expectedSide == Sides.Buy) buys++; else sells++;
					if (rollingBars.Count == 32 && rollingBars.Sum(bar => bar.TotalVolume) > 0m)
					{
						var vwma = rollingBars.Sum(bar => bar.ClosePrice * bar.TotalVolume) / rollingBars.Sum(bar => bar.TotalVolume);
						if (Math.Abs(100m * (close - vwma) / vwma) <= Convert.ToDecimal(threshold)) wrongVwmaEntries++;
					}
				}
				if (expectedSide is not null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add("Every order must fade the current UTC-session cumulative VWAP while flat with RSI confirming (below oversold for buys, above overbought for sells), or fully exit at VWAP; a rolling close-price VWMA/cooldown cannot replace this contract.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(nativeVwapSeen && comparisons > 100 && sessions >= 30);
		IsTrue(nativeRsiSeen && rsiComparisons > 1000, "The strategy must actually compute its native RSI on every finished candle.");
		IsTrue(rsiRejections > 0, "The archive must contain VWAP deviations the RSI does not confirm, so the RSI actually decides entries.");
		IsTrue(buys > 0 && sells > 0 && exits > 0 && wrongVwmaEntries > 0, "Real archive orders must exercise both directions, mean exits and signals that the old VWMA would reject.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public Task S0030_PercentStopWorksBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(VwapReversion);

	private const string KeltnerReversion = "0031_Keltner_Reversion";

	[TestMethod]
	[TestCategory("Shard07")]
	[DataRow(20, 14, 2.0, 14, 30.0, 70.0)]
	[DataRow(10, 7, 1.5, 14, 30.0, 70.0)]
	[DataRow(20, 14, 2.0, 21, 35.0, 65.0)]
	public async Task S0031_IndependentEmaAtrOutsideEntriesAndReturnInsideExits(int emaPeriod, int atrPeriod, double width, int rsiPeriod, double oversold, double overbought)
	{
		var ema = new ImpulseEma(emaPeriod);
		var rsi = new RsiOracle(rsiPeriod);
		var oversoldLevel = Convert.ToDecimal(oversold);
		var overboughtLevel = Convert.ToDecimal(overbought);
		var atr = 0m;
		var bars = 0;
		decimal? previousClose = null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buys = 0;
		var sells = 0;
		var rsiRejectedEntries = 0;
		var insideExitsBeforeEma = 0;
		var violations = new List<string>();
		await Replay(KeltnerReversion, (strategy, _) =>
		{
			AreEqual(20, strategy.Parameters["EmaPeriod"].Value);
			AreEqual(14, strategy.Parameters["AtrPeriod"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["AtrMultiplier"].Value));
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["StopLossAtrMultiplier"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			IsTrue(strategy.Parameters.TryGetValue("RsiPeriod", out var rsiPeriodParameter), "README bases the entry signals on RSI as well as ATR and Keltner.");
			IsTrue(strategy.Parameters.TryGetValue("RsiOversold", out var oversoldParameter), "README bases the entry signals on RSI as well as ATR and Keltner.");
			IsTrue(strategy.Parameters.TryGetValue("RsiOverbought", out var overboughtParameter), "README bases the entry signals on RSI as well as ATR and Keltner.");
			AreEqual(14, rsiPeriodParameter.Value);
			AreEqual(30m, Convert.ToDecimal(oversoldParameter.Value));
			AreEqual(70m, Convert.ToDecimal(overboughtParameter.Value));
			SetParam(strategy, "EmaPeriod", emaPeriod);
			SetParam(strategy, "AtrPeriod", atrPeriod);
			SetParam(strategy, "AtrMultiplier", Convert.ToDecimal(width));
			SetParam(strategy, "RsiPeriod", rsiPeriod);
			SetParam(strategy, "RsiOversold", oversoldLevel);
			SetParam(strategy, "RsiOverbought", overboughtLevel);
			// Disable only the independent stop multiplier, never the channel width.
			SetParam(strategy, "StopLossAtrMultiplier", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				var middle = ema.Add(candle.ClosePrice);
				var rsiValue = rsi.Add(candle.ClosePrice);
				var tr = previousClose is decimal close
					? Math.Max(candle.HighPrice - candle.LowPrice, Math.Max(Math.Abs(candle.HighPrice - close), Math.Abs(candle.LowPrice - close)))
					: candle.HighPrice - candle.LowPrice;
				bars++;
				var length = Math.Min(bars, atrPeriod);
				atr = (atr * (length - 1) + tr) / length;
				previousClose = candle.ClosePrice;
				if (!ema.Formed || bars < atrPeriod) return;
				var lower = middle - atr * Convert.ToDecimal(width);
				var upper = middle + atr * Convert.ToDecimal(width);
				var price = candle.ClosePrice;
				if (strategy.Position > 0m && price >= lower)
				{
					expectedSide = Sides.Sell;
					expectedVolume = strategy.Position;
					if (price < middle) insideExitsBeforeEma++;
				}
				else if (strategy.Position < 0m && price <= upper)
				{
					expectedSide = Sides.Buy;
					expectedVolume = Math.Abs(strategy.Position);
					if (price > middle) insideExitsBeforeEma++;
				}
				else if (strategy.Position == 0m && price < lower && rsi.Formed && rsiValue < oversoldLevel)
				{
					expectedSide = Sides.Buy;
					expectedVolume = strategy.Volume;
					buys++;
				}
				else if (strategy.Position == 0m && price > upper && rsi.Formed && rsiValue > overboughtLevel)
				{
					expectedSide = Sides.Sell;
					expectedVolume = strategy.Volume;
					sells++;
				}
				else if (strategy.Position == 0m && (price < lower || price > upper) && rsi.Formed)
				{
					rsiRejectedEntries++;
				}
				if (expectedSide is not null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add($"{strategy.CurrentTime:O} {order.Side}: every order must follow independent native-style EMA, Wilder ATR and RSI: a flat outside-close entry only when RSI confirms it (below RsiOversold for a buy, above RsiOverbought for a sell), otherwise a full return-inside exit.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(buys > 0 && sells > 0 && insideExitsBeforeEma > 0);
		IsTrue(rsiRejectedEntries > 0, "The replay must contain outside closes the RSI does not confirm, or the RSI entry filter goes untested.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard07")]
	public Task S0031_StopAtrDistancesAndRiskParameterChangeRealExecutions()
		=> CheckAtrDistancesAndRiskParameterChangeRealExecutions(KeltnerReversion,
			"Keltner reversion entry", stopParameter: "StopLossAtrMultiplier");

	private const string AtrReversion = "0032_ATR_Reversion";

	[TestMethod]
	[TestCategory("Shard00")]
	[DataRow(14, 20, 2.0)]
	[DataRow(7, 10, 1.5)]
	public async Task S0032_IndependentAtrSpikesAndActualSmaCrossingExits(int atrPeriod, int maPeriod, double multiplier)
	{
		var closes = new Queue<decimal>();
		var atr = 0m;
		var bars = 0;
		decimal? previousClose = null;
		decimal? previousFormedClose = null;
		var previousMean = 0m;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buys = 0;
		var sells = 0;
		var crossExits = 0;
		var levelWithoutCross = 0;
		var violations = new List<string>();
		await Replay(AtrReversion, (strategy, _) =>
		{
			AreEqual(14, strategy.Parameters["AtrPeriod"].Value);
			AreEqual(20, strategy.Parameters["MAPeriod"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["AtrMultiplier"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			IsTrue(strategy.Parameters.TryGetValue("StopLossPercent", out var stop));
			AreEqual(2m, Convert.ToDecimal(stop.Value));
			SetParam(strategy, "AtrPeriod", atrPeriod);
			SetParam(strategy, "MAPeriod", maPeriod);
			SetParam(strategy, "AtrMultiplier", Convert.ToDecimal(multiplier));
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				var close = candle.ClosePrice;
				var tr = previousClose is decimal prev
					? Math.Max(candle.HighPrice - candle.LowPrice, Math.Max(Math.Abs(candle.HighPrice - prev), Math.Abs(candle.LowPrice - prev)))
					: candle.HighPrice - candle.LowPrice;
				bars++;
				var length = Math.Min(bars, atrPeriod);
				atr = (atr * (length - 1) + tr) / length;
				previousClose = close;
				closes.Enqueue(close);
				if (closes.Count > maPeriod) closes.Dequeue();
				if (bars < atrPeriod || closes.Count < maPeriod) return;
				var mean = closes.Average();
				if (previousFormedClose is decimal prevFormed)
				{
					var upwardCross = prevFormed <= previousMean && close > mean;
					var downwardCross = prevFormed >= previousMean && close < mean;
					if (strategy.Position > 0m && upwardCross || strategy.Position < 0m && downwardCross)
					{
						expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
						expectedVolume = Math.Abs(strategy.Position);
						crossExits++;
					}
					else if (strategy.Position == 0m && atr > 0m && Math.Abs(close - prevFormed) > atr * Convert.ToDecimal(multiplier))
					{
						expectedSide = close < prevFormed ? Sides.Buy : Sides.Sell;
						expectedVolume = strategy.Volume;
						if (expectedSide == Sides.Buy) buys++; else sells++;
					}
					else if (strategy.Position > 0m && close > mean || strategy.Position < 0m && close < mean)
						levelWithoutCross++;
					if (expectedSide is not null) expectedOrders++;
				}
				previousFormedClose = close;
				previousMean = mean;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add("Every order must fade a strict one-bar price move/current independent Wilder ATR while flat, or fully close on an actual price/SMA crossing, never merely its current side/cooldown.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(buys > 0 && sells > 0 && crossExits > 0);
		if (multiplier < 2.0) IsTrue(levelWithoutCross > 0, "The actual archive must distinguish being on the exit side from crossing there now.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public Task S0032_PercentStopWorksBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(AtrReversion);

	private const string MacdZero = "0033_MACD_Zero";

	[TestMethod]
	[TestCategory("Shard01")]
	[DataRow(12, 26, 9)]
	[DataRow(5, 10, 3)]
	public async Task S0033_IndependentMacdApproachesZeroAndExitsOnSignalCrosses(int fastPeriod, int slowPeriod, int signalPeriod)
	{
		var fast = new ImpulseEma(fastPeriod);
		var slow = new ImpulseEma(slowPeriod);
		var signal = new ImpulseEma(signalPeriod);
		decimal? previousMacd = null;
		var previousSignal = 0m;
		var expectedMacd = 0m;
		var expectedSignal = 0m;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buys = 0;
		var sells = 0;
		var signalExits = 0;
		var favorableCrossExits = 0;
		var histogramOppositeEntries = 0;
		var scalarChecks = 0;
		var formedBars = 0;
		var barTime = default(DateTime);
		var violations = new List<string>();
		await Replay(MacdZero, (strategy, _) =>
		{
			AreEqual(12, strategy.Parameters["FastPeriod"].Value);
			AreEqual(26, strategy.Parameters["SlowPeriod"].Value);
			AreEqual(9, strategy.Parameters["SignalPeriod"].Value);
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			IsTrue(strategy.Parameters.TryGetValue("StopLossPercent", out var stop));
			AreEqual(2m, Convert.ToDecimal(stop.Value));
			SetParam(strategy, "FastPeriod", fastPeriod);
			SetParam(strategy, "SlowPeriod", slowPeriod);
			SetParam(strategy, "SignalPeriod", signalPeriod);
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.Indicators.Added += indicator =>
			{
				if (indicator is not MovingAverageConvergenceDivergenceSignal) return;
				indicator.Changed += (_, output) =>
				{
					if (!output.IsFinal || !indicator.IsFormed || output is not MovingAverageConvergenceDivergenceSignalValue value) return;
					if (value.Macd is not decimal macd || value.Signal is not decimal sig) return;
					scalarChecks++;
					if (Math.Abs(macd - expectedMacd) > 0.00000001m || Math.Abs(sig - expectedSignal) > 0.00000001m)
						violations.Add("Both native MACD and its signal EMA must match independent configured EMA chains.");
				};
			};
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				if (expectedSide is not null) violations.Add($"Missing {expectedSide} at {barTime:O} before {candle.OpenTime:O}.");
				barTime = candle.OpenTime;
				expectedSide = null;
				var macd = fast.Add(candle.ClosePrice) - slow.Add(candle.ClosePrice);
				if (!slow.Formed) return;
				var sig = signal.Add(macd);
				expectedMacd = macd;
				expectedSignal = sig;
				if (!signal.Formed) return;
				formedBars++;
				if (previousMacd is decimal prev)
				{
					var upwardCross = prev <= previousSignal && macd > sig;
					var downwardCross = prev >= previousSignal && macd < sig;
					if (strategy.Position != 0m && (upwardCross || downwardCross))
					{
						expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
						expectedVolume = Math.Abs(strategy.Position);
						signalExits++;
						if (strategy.Position > 0m && upwardCross || strategy.Position < 0m && downwardCross) favorableCrossExits++;
					}
					else if (strategy.Position == 0m && macd < 0m && macd > prev)
					{
						expectedSide = Sides.Buy;
						expectedVolume = strategy.Volume;
						buys++;
						if (macd - sig > 0m) histogramOppositeEntries++;
					}
					else if (strategy.Position == 0m && macd > 0m && macd < prev)
					{
						expectedSide = Sides.Sell;
						expectedVolume = strategy.Volume;
						sells++;
						if (macd - sig < 0m) histogramOppositeEntries++;
					}
					if (expectedSide is not null) expectedOrders++;
				}
				previousMacd = macd;
				previousSignal = sig;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add("Every order must enter while the MACD line approaches zero without reaching it, or fully close a held position on either-direction actual MACD/signal crossing; no zero-cross proxy/cooldown.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		if (expectedSide is not null) violations.Add($"Missing final {expectedSide} at {barTime:O}.");
		AreEqual(expectedOrders, actualOrders, string.Join(Environment.NewLine, violations.Take(12)));
		AreEqual(formedBars, scalarChecks, "Check the native scalar pair from the very first independently fully formed candle, not one bar later.");
		IsTrue(buys > 0 && sells > 0 && signalExits > 0 && favorableCrossExits > 0 && histogramOppositeEntries > 0 && scalarChecks > 100);
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard01")]
	public Task S0033_PercentStopWorksBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(MacdZero);

	private const string LowVol = "0034_Low_Vol_Reversion";
	// The quiet-market threshold the README publishes, in percent of the rolling ATR mean.
	internal const double LowVolThreshold = 75.0;

	[TestMethod]
	[TestCategory("Shard02")]
	[DataRow(20, 14, 20, LowVolThreshold)]
	[DataRow(10, 7, 10, 80.0)]
	public async Task S0034_IndependentRollingAtrWindowAndUnconditionalMeanExits(int maPeriod, int atrPeriod, int lookback, double threshold)
	{
		var closes = new Queue<decimal>();
		var atrWindow = new Queue<decimal>();
		var bars = 0;
		var atr = 0m;
		var expectedAtrMean = 0m;
		var smoothedAtr = 0m;
		var smoothedCount = 0;
		decimal? previousClose = null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buys = 0;
		var sells = 0;
		var meanExits = 0;
		var exitsOutsideQuietMarket = 0;
		var differentFilters = 0;
		var scalarChecks = 0;
		var formedWindows = 0;
		var violations = new List<string>();
		await Replay(LowVol, (strategy, _) =>
		{
			AreEqual(20, strategy.Parameters["MAPeriod"].Value);
			AreEqual(14, strategy.Parameters["AtrPeriod"].Value);
			AreEqual(20, strategy.Parameters["AtrLookbackPeriod"].Value);
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			IsTrue(strategy.Parameters.TryGetValue("AtrMultiplier", out var stop));
			AreEqual(2m, Convert.ToDecimal(stop.Value));
			AssertPublishedLowVolThreshold(strategy);
			SetParam(strategy, "AtrThresholdPercent", Convert.ToDecimal(threshold));
			SetParam(strategy, "MAPeriod", maPeriod);
			SetParam(strategy, "AtrPeriod", atrPeriod);
			SetParam(strategy, "AtrLookbackPeriod", lookback);
			// Without the stop every position change is a strategy order this oracle predicts.
			SetParam(strategy, "AtrMultiplier", 0m);
			strategy.Indicators.Added += indicator =>
			{
				if (indicator is not SimpleMovingAverage || indicator.Name != "ATR rolling mean") return;
				indicator.Changed += (_, output) =>
				{
					if (!output.IsFinal || !indicator.IsFormed) return;
					scalarChecks++;
					if (Math.Abs(output.GetValue<decimal>() - expectedAtrMean) > 0.00000001m)
						violations.Add("The ATR mean must use only the last configured count of fully formed ATR values, not infinite-memory smoothing or unformed ATR samples.");
				};
			};
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				var close = candle.ClosePrice;
				var tr = previousClose is decimal prev
					? Math.Max(candle.HighPrice - candle.LowPrice, Math.Max(Math.Abs(candle.HighPrice - prev), Math.Abs(candle.LowPrice - prev)))
					: candle.HighPrice - candle.LowPrice;
				bars++;
				var length = Math.Min(bars, atrPeriod);
				atr = (atr * (length - 1) + tr) / length;
				previousClose = close;
				closes.Enqueue(close);
				if (closes.Count > maPeriod) closes.Dequeue();
				if (bars < atrPeriod) return;
				atrWindow.Enqueue(atr);
				if (atrWindow.Count > lookback) atrWindow.Dequeue();
				smoothedCount++;
				var smoothedLength = Math.Min(smoothedCount, lookback);
				smoothedAtr = (smoothedAtr * (smoothedLength - 1) + atr) / smoothedLength;
				if (atrWindow.Count < lookback) return;
				expectedAtrMean = atrWindow.Average();
				formedWindows++;
				if (closes.Count < maPeriod) return;
				var mean = closes.Average();
				var level = Convert.ToDecimal(threshold);
				var lowVol = atr < expectedAtrMean * level / 100m;
				var smoothedLowVol = atr < smoothedAtr * level / 100m;
				if (strategy.Position == 0m && lowVol != smoothedLowVol && close != mean) differentFilters++;
				if (strategy.Position > 0m && close >= mean || strategy.Position < 0m && close <= mean)
				{
					expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
					expectedVolume = Math.Abs(strategy.Position);
					meanExits++;
					if (!lowVol) exitsOutsideQuietMarket++;
				}
				else if (strategy.Position == 0m && lowVol && close != mean && atr > 0m)
				{
					expectedSide = close < mean ? Sides.Buy : Sides.Sell;
					expectedVolume = strategy.Volume;
					if (expectedSide == Sides.Buy) buys++; else sells++;
				}
				if (expectedSide is not null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add("Every order must fade a deviation only under the independent rolling ATR filter or fully close at the mean regardless of volatility/cooldown.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		AreEqual(expectedOrders, actualOrders);
		AreEqual(formedWindows, scalarChecks);
		IsTrue(buys > 0 && sells > 0 && meanExits > 0 && exitsOutsideQuietMarket > 0 && differentFilters > 0 && scalarChecks > 100,
			"The replay must enter quiet markets in both directions, exit at the mean outside quiet conditions and hit bars where the rolling window differs from infinite-memory smoothing.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard02")]
	public Task S0034_AtrDistancesAndRiskParameterChangeRealExecutions()
		=> CheckAtrDistancesAndRiskParameterChangeRealExecutions(LowVol, "Low volatility entry",
			signalSetup: AssertPublishedLowVolThreshold);

	private static void AssertPublishedLowVolThreshold(Strategy strategy)
		=> AreEqual(Convert.ToDecimal(LowVolThreshold), Convert.ToDecimal(strategy.Parameters["AtrThresholdPercent"].Value), "The README publishes a 75% quiet-market threshold.");

	private const string PercentB = "0035_Bollinger_B_Reversion";

	[TestMethod]
	[TestCategory("Shard03")]
	[DataRow(20, 2.0, 0.5)]
	[DataRow(10, 1.5, 0.35)]
	[DataRow(2, 0.5, 0.5)]
	public async Task S0035_IndependentPercentBOutsideEntriesAndThresholdCrossingExits(int period, double deviation, double exitValue)
	{
		var closes = new Queue<decimal>();
		decimal? previous = null;
		var expectedPercentB = 0m;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buys = 0;
		var sells = 0;
		var longExits = 0;
		var shortExits = 0;
		var zeroWidthExits = 0;
		var equalityExits = 0;
		var scalarChecks = 0;
		var nonzeroWindows = 0;
		var violations = new List<string>();
		await Replay(PercentB, (strategy, _) =>
		{
			AreEqual(20, strategy.Parameters["BollingerPeriod"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["BollingerDeviation"].Value));
			AreEqual(0.5m, Convert.ToDecimal(strategy.Parameters["ExitValue"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			IsTrue(strategy.Parameters.TryGetValue("StopLossPercent", out var stop));
			AreEqual(2m, Convert.ToDecimal(stop.Value));
			SetParam(strategy, "BollingerPeriod", period);
			SetParam(strategy, "BollingerDeviation", Convert.ToDecimal(deviation));
			SetParam(strategy, "ExitValue", Convert.ToDecimal(exitValue));
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.Indicators.Added += indicator =>
			{
				if (indicator is not BollingerPercentB) return;
				indicator.Changed += (_, output) =>
				{
					if (!output.IsFinal || !indicator.IsFormed || output.IsEmpty) return;
					scalarChecks++;
					if (Math.Abs(output.GetValue<decimal>() / 100m - expectedPercentB) > 0.00000001m)
						violations.Add("Native percent units must be normalized to the README's 0..1 scale and match independent population-standard-deviation Bollinger bands.");
				};
			};
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				closes.Enqueue(candle.ClosePrice);
				if (closes.Count > period) closes.Dequeue();
				if (closes.Count < period) return;
				var mean = closes.Average();
				var variance = closes.Sum(close => (close - mean) * (close - mean)) / period;
				var distance = Convert.ToDecimal(deviation) * (decimal)Math.Sqrt((double)variance);
				var pct = distance == 0m ? 0.5m : (candle.ClosePrice - (mean - distance)) / (2m * distance);
				expectedPercentB = pct;
				if (distance > 0m) nonzeroWindows++;
				var level = Convert.ToDecimal(exitValue);
				var upwardCross = previous is decimal prevUp && prevUp < level && pct >= level;
				var downwardCross = previous is decimal prevDown && prevDown > level && pct <= level;
				if (strategy.Position > 0m && upwardCross || strategy.Position < 0m && downwardCross)
				{
					expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
					expectedVolume = Math.Abs(strategy.Position);
					if (strategy.Position > 0m) longExits++; else shortExits++;
					if (distance == 0m) zeroWidthExits++;
					if (pct == level) equalityExits++;
				}
				else if (strategy.Position == 0m && (pct < 0m || pct > 1m))
				{
					expectedSide = pct < 0m ? Sides.Buy : Sides.Sell;
					expectedVolume = strategy.Volume;
					if (expectedSide == Sides.Buy) buys++; else sells++;
				}
				if (expectedSide is not null) expectedOrders++;
				previous = pct;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add("Every order must fade strict outside %B only while flat or close the full position on an actual inclusive threshold crossing; no level-only exit, hidden cooldown or degenerate-band directional signal.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		AreEqual(expectedOrders, actualOrders);
		AreEqual(nonzeroWindows, scalarChecks);
		IsTrue(buys > 0 && sells > 0 && longExits > 0 && shortExits > 0 && scalarChecks > 100);
		if (period == 2) IsTrue(zeroWidthExits > 0 && equalityExits > 0, "Actual constant-close windows must exit held positions at neutral %B=0.5 rather than falsely turn into %B=0.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard03")]
	public Task S0035_PercentStopWorksBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(PercentB);

	private const string AtrExpansion = "0036_ATR_Expansion";

	[TestMethod]
	[TestCategory("Shard04")]
	[DataRow(14, 20, false)]
	[DataRow(7, 10, false)]
	[DataRow(1, 2, true)]
	public async Task S0036_IndependentOneBarAtrExpansionAndAnyContractionExits(int atrPeriod, int maPeriod, bool secondary)
	{
		var closes = new Queue<decimal>();
		var atr = 0m;
		var bars = 0;
		decimal? previousClose = null;
		decimal? previousAtr = null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buys = 0;
		var sells = 0;
		var exits = 0;
		var mildExpansions = 0;
		var mildContractions = 0;
		var unchangedAtr = 0;
		var zeroAtrExits = 0;
		var violations = new List<string>();
		await Replay(AtrExpansion, (strategy, alternateSecurity) =>
		{
			if (secondary) strategy.Security = alternateSecurity;
			AreEqual(14, strategy.Parameters["AtrPeriod"].Value);
			AreEqual(20, strategy.Parameters["MAPeriod"].Value);
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			IsTrue(strategy.Parameters.TryGetValue("AtrMultiplier", out var stop));
			AreEqual(2m, Convert.ToDecimal(stop.Value));
			SetParam(strategy, "AtrPeriod", atrPeriod);
			SetParam(strategy, "MAPeriod", maPeriod);
			SetParam(strategy, "AtrMultiplier", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				var close = candle.ClosePrice;
				var tr = previousClose is decimal prevClose
					? Math.Max(candle.HighPrice - candle.LowPrice, Math.Max(Math.Abs(candle.HighPrice - prevClose), Math.Abs(candle.LowPrice - prevClose)))
					: candle.HighPrice - candle.LowPrice;
				bars++;
				var length = Math.Min(bars, atrPeriod);
				atr = (atr * (length - 1) + tr) / length;
				previousClose = close;
				closes.Enqueue(close);
				if (closes.Count > maPeriod) closes.Dequeue();
				if (bars < atrPeriod || closes.Count < maPeriod) return;
				var mean = closes.Average();
				if (previousAtr is decimal prev)
				{
					if (atr == prev) unchangedAtr++;
					if (strategy.Position != 0m && atr < prev)
					{
						expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
						expectedVolume = Math.Abs(strategy.Position);
						exits++;
						if (atr >= prev / 1.05m) mildContractions++;
						if (atr == 0m) zeroAtrExits++;
					}
					else if (strategy.Position == 0m && atr > prev && close != mean)
					{
						expectedSide = close > mean ? Sides.Buy : Sides.Sell;
						expectedVolume = strategy.Volume;
						if (expectedSide == Sides.Buy) buys++; else sells++;
						if (atr < prev * 1.05m) mildExpansions++;
					}
					if (expectedSide is not null) expectedOrders++;
				}
				previousAtr = atr;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add("Every order must follow independent strictly rising one-bar Wilder ATR/price-SMA direction while flat, or fully close on any ATR contraction, without an invented 1.05 ratio/5-bar lookback/cooldown.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(buys > 0 && sells > 0 && exits > 0);
		if (!secondary) IsTrue(mildExpansions > 0 && mildContractions > 0, "Real eligible signals must distinguish the README's any-rise/any-fall contract from a hidden 1.05 ratio.");
		else IsTrue(unchangedAtr > 0 && zeroAtrExits > 0, "Actual TON ATR1 must exercise equal-volatility no-action and zero-volatility contraction exits.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard04")]
	public Task S0036_AtrDistancesAndRiskParameterChangeRealExecutions()
		=> CheckAtrDistancesAndRiskParameterChangeRealExecutions(AtrExpansion, "ATR expansion entry");

	private const string Vix = "0037_VIX_Trigger";

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task S0037_RejectsMissingOrSameExternalInstrumentThenTradesWithExplicitInput()
	{
		await Replay(Vix, (strategy, secondary) =>
		{
			var start = typeof(Strategy).GetMethod("OnStarted2", BindingFlags.NonPublic | BindingFlags.Instance);
			foreach (var invalid in new[] { null, strategy.Security })
			{
				SetParam(strategy, "VixSecurity", invalid);
				var rejected = false;
				try { start.Invoke(strategy, [DateTime.MinValue]); }
				catch (TargetInvocationException exception)
				{
					IsTrue(exception.InnerException is InvalidOperationException && exception.InnerException.Message.Contains("VixSecurity"),
						"Reject the explicit external-input contract before touching subscriptions or falling back to the primary instrument.");
					rejected = true;
				}
				IsTrue(rejected, "A missing or primary-as-index configuration must be rejected before startup side effects.");
				AreEqual(0, strategy.Indicators.Count);
			}
			SetParam(strategy, "VixSecurity", secondary);
		}, TimeSpan.FromDays(7));
	}

	[TestMethod]
	[TestCategory("Shard05")]
	[DataRow(20, false)]
	[DataRow(10, true)]
	[DataRow(2, true)]
	public async Task S0037_TwoRealStreamMechanicsWithMatchedFinishedBarsNotVixMarketHistory(int period, bool reversed)
	{
		var closes = new Queue<decimal>();
		decimal? previousIndex = null;
		var indexTime = DateTime.MinValue;
		var mainTime = DateTime.MinValue;
		var processedTime = DateTime.MinValue;
		var direction = 0;
		var hasDirection = false;
		var mainClose = 0m;
		var mean = 0m;
		var expectedSide = (Sides?)null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var joinedPairs = 0;
		var equalIndexWhileHeld = 0;
		var equalPriceWhileFlat = 0;
		var buys = 0;
		var sells = 0;
		var exits = 0;
		var formedMainBars = 0;
		var scalarChecks = 0;
		var violations = new List<string>();
		await Replay(Vix, (strategy, secondary) =>
		{
			AreEqual(20, strategy.Parameters["MAPeriod"].Value);
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
			var primary = strategy.Security;
			strategy.Security = reversed ? secondary : primary;
			var index = reversed ? primary : secondary;
			SetParam(strategy, "VixSecurity", index);
			SetParam(strategy, "MAPeriod", period);
			SetParam(strategy, "StopLossPercent", 0m);
			var primaryId = strategy.Security.Id.ToSecurityId();
			var indexId = index.Id.ToSecurityId();
			IsTrue(primaryId != indexId);
			TestContext.WriteLine($"Mechanics fixture only: primary={primaryId}, external input={indexId}. No actual VIX history or economic/VIX performance claim.");
			strategy.Indicators.Added += indicator =>
			{
				if (indicator is not SimpleMovingAverage) return;
				indicator.Changed += (_, output) =>
				{
					if (!output.IsFinal || !indicator.IsFormed) return;
					scalarChecks++;
					if (Math.Abs(output.GetValue<decimal>() - mean) > 0.00000001m)
						violations.Add("The price SMA must use only the primary instrument's independent configured close window.");
				};
			};
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				if (candle.SecurityId == primaryId)
				{
					mainTime = candle.OpenTime;
					mainClose = candle.ClosePrice;
					closes.Enqueue(mainClose);
					if (closes.Count > period) closes.Dequeue();
					mean = closes.Average();
					if (closes.Count == period) formedMainBars++;
				}
				else if (candle.SecurityId == indexId)
				{
					indexTime = candle.OpenTime;
					hasDirection = previousIndex.HasValue;
					direction = previousIndex is decimal prev ? Math.Sign(candle.ClosePrice - prev) : 0;
					previousIndex = candle.ClosePrice;
				}
				else throw new InvalidOperationException("A third candle stream must not be substituted for either input.");
				if (!hasDirection || closes.Count < period || mainTime != indexTime || processedTime == mainTime) return;
				processedTime = mainTime;
				joinedPairs++;
				if (strategy.Position != 0m && direction < 0)
				{
					expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
					expectedVolume = Math.Abs(strategy.Position);
					exits++;
				}
				else if (strategy.Position == 0m && direction > 0 && mainClose != mean)
				{
					expectedSide = mainClose < mean ? Sides.Buy : Sides.Sell;
					expectedVolume = strategy.Volume;
					if (expectedSide == Sides.Buy) buys++; else sells++;
				}
				else if (strategy.Position != 0m && direction == 0) equalIndexWhileHeld++;
				else if (strategy.Position == 0m && direction > 0 && mainClose == mean) equalPriceWhileFlat++;
				if (expectedSide is not null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market || order.Security.Id != strategy.Security.Id)
					violations.Add("Every order must consume a single matched finished primary/external pair, with strict rise/fall and price/MA direction/full close; stale input, equal-as-falling and primary-as-index shortcuts are forbidden.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		AreEqual(expectedOrders, actualOrders);
		AreEqual(formedMainBars, scalarChecks);
		IsTrue(joinedPairs > 100 && buys > 0 && sells > 0 && exits > 0);
		if (!reversed) IsTrue(equalIndexWhileHeld > 0, "Real unchanged external candles while a position is held must not be mislabeled as falling.");
		if (period == 2) IsTrue(equalPriceWhileFlat > 0, "A real primary close/SMA equality must not turn into a short entry.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public Task S0037_PercentStopWorksBetweenFinishedBarsWithExplicitExternalInput()
		=> CheckPercentStopBetweenBars(Vix, setup: (strategy, secondary) => SetParam(strategy, "VixSecurity", secondary));

	private const string BbWidth = "0038_BB_Width";

	[TestMethod]
	[TestCategory("Shard06")]
	[DataRow(20, 2.0)]
	[DataRow(10, 1.5)]
	[DataRow(2, 2.0)]
	public async Task S0038_IndependentAbsoluteBandExpansionAndStrictContractionExits(int period, double deviation)
	{
		var closes = new Queue<decimal>();
		var bars = 0;
		decimal? previousWidth = null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buys = 0;
		var sells = 0;
		var exits = 0;
		var equalWidthWhileHeld = 0;
		var growthFromZero = 0;
		var normalizedDifferent = 0;
		var previousNormalized = 0m;
		var violations = new List<string>();
		await Replay(BbWidth, (strategy, _) =>
		{
			AreEqual(20, strategy.Parameters["BollingerPeriod"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["BollingerDeviation"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			IsTrue(strategy.Parameters.TryGetValue("AtrMultiplier", out var stop));
			AreEqual(2m, Convert.ToDecimal(stop.Value));
			IsTrue(strategy.Parameters.TryGetValue("AtrPeriod", out var atr));
			AreEqual(14, atr.Value);
			SetParam(strategy, "BollingerPeriod", period);
			SetParam(strategy, "BollingerDeviation", Convert.ToDecimal(deviation));
			SetParam(strategy, "AtrMultiplier", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				bars++;
				closes.Enqueue(candle.ClosePrice);
				if (closes.Count > period) closes.Dequeue();
				if (bars < Math.Max(period, 14)) return;
				var mean = closes.Average();
				var variance = closes.Sum(close => (close - mean) * (close - mean)) / period;
				var width = 2m * Convert.ToDecimal(deviation) * (decimal)Math.Sqrt((double)variance);
				var normalized = width / mean;
				if (previousWidth is decimal prev)
				{
					if (strategy.Position != 0m && width == prev) equalWidthWhileHeld++;
					if (strategy.Position == 0m && width > prev && prev == 0m && candle.ClosePrice != mean) growthFromZero++;
					if ((width > prev) != (normalized > previousNormalized)) normalizedDifferent++;
					if (strategy.Position != 0m && width < prev)
					{
						expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
						expectedVolume = Math.Abs(strategy.Position);
						exits++;
					}
					else if (strategy.Position == 0m && width > prev && candle.ClosePrice != mean)
					{
						expectedSide = candle.ClosePrice > mean ? Sides.Buy : Sides.Sell;
						expectedVolume = strategy.Volume;
						if (expectedSide == Sides.Buy) buys++; else sells++;
					}
					if (expectedSide is not null) expectedOrders++;
				}
				previousWidth = width;
				previousNormalized = normalized;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add("Every order must follow independent population-band absolute width increase/price-middle direction or full strict-contraction exit: equal width is not contraction, zero is not a seed sentinel, no cooldown/normalized-width proxy.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(buys > 0 && sells > 0 && exits > 0 && normalizedDifferent > 0);
		if (period == 2) IsTrue(equalWidthWhileHeld > 0 && growthFromZero > 0, "Actual BTC windows must distinguish unchanged width from contraction and an expanding zero-width baseline from uninitialized state.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public Task S0038_AtrDistancesAndRiskParameterChangeRealExecutions()
		=> CheckAtrDistancesAndRiskParameterChangeRealExecutions(BbWidth, "Bollinger width entry");

	private const string HvBreakout = "0039_HV_Breakout";

	[TestMethod]
	[TestCategory("Shard07")]
	[DataRow(20, 20)]
	[DataRow(10, 30)]
	[DataRow(2, 10)]
	public async Task S0039_IndependentSmaPriceDispersionBreakoutsAndSmaCrossExits(int hvPeriod, int maPeriod)
	{
		static Sides? Breakout(decimal close, decimal anchor, decimal relativeDeviation)
			=> close > anchor * (1m + relativeDeviation) ? Sides.Buy
				: close < anchor * (1m - relativeDeviation) ? Sides.Sell
				: (Sides?)null;

		var hvCloses = new Queue<decimal>();
		var maCloses = new Queue<decimal>();
		var bars = 0;
		var seeded = false;
		var staleAnchor = 0m;
		var previousClose = 0m;
		var previousMean = 0m;
		var expectedDeviation = 0m;
		var expectedMean = 0m;
		var formedBars = 0;
		var nativeDeviations = 0;
		var nativeMeans = 0;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buys = 0;
		var sells = 0;
		var exits = 0;
		var staleAnchorDisagreements = 0;
		var zeroDeviationBars = 0;
		var violations = new List<string>();
		await Replay(HvBreakout, (strategy, _) =>
		{
			AreEqual(20, strategy.Parameters["HvPeriod"].Value);
			AreEqual(20, strategy.Parameters["MAPeriod"].Value);
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			IsTrue(strategy.Parameters.TryGetValue("StopLossPercent", out var stop));
			AreEqual(2m, Convert.ToDecimal(stop.Value));
			SetParam(strategy, "HvPeriod", hvPeriod);
			SetParam(strategy, "MAPeriod", maPeriod);
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				bars++;
				var close = candle.ClosePrice;
				hvCloses.Enqueue(close);
				if (hvCloses.Count > hvPeriod) hvCloses.Dequeue();
				maCloses.Enqueue(close);
				if (maCloses.Count > maPeriod) maCloses.Dequeue();
				if (bars < Math.Max(hvPeriod, maPeriod)) return;
				formedBars++;
				var hvMean = hvCloses.Average();
				expectedDeviation = (decimal)Math.Sqrt((double)(hvCloses.Sum(value => (value - hvMean) * (value - hvMean)) / hvPeriod));
				expectedMean = maCloses.Average();
				if (expectedDeviation == 0m) zeroDeviationBars++;
				if (seeded && close > 0m)
				{
					var upwardCross = previousClose <= previousMean && close > expectedMean;
					var downwardCross = previousClose >= previousMean && close < expectedMean;
					var relativeDeviation = expectedDeviation / close;
					if (strategy.Position > 0m && downwardCross || strategy.Position < 0m && upwardCross)
					{
						expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
						expectedVolume = Math.Abs(strategy.Position);
						exits++;
					}
					else if (strategy.Position == 0m)
					{
						var breakout = Breakout(close, expectedMean, relativeDeviation);
						if (breakout != Breakout(close, staleAnchor, relativeDeviation)) staleAnchorDisagreements++;
						if (breakout is Sides side)
						{
							expectedSide = side;
							expectedVolume = strategy.Volume;
							if (side == Sides.Buy) buys++; else sells++;
							staleAnchor = close;
						}
					}
					if (expectedSide is not null) expectedOrders++;
				}
				else if (close > 0m)
				{
					seeded = true;
					staleAnchor = close;
				}
				previousClose = close;
				previousMean = expectedMean;
			};
			strategy.Indicators.Added += indicator =>
			{
				indicator.Changed += (_, value) =>
				{
					if (!value.IsFinal || !indicator.IsFormed || bars < Math.Max(hvPeriod, maPeriod)) return;
					if (indicator is StandardDeviation)
					{
						nativeDeviations++;
						if (Math.Abs(value.GetValue<decimal>() - expectedDeviation) > 0.00000001m)
							violations.Add("Native price population deviation must match the independent rolling Close window.");
					}
					else if (indicator is SimpleMovingAverage)
					{
						nativeMeans++;
						if (Math.Abs(value.GetValue<decimal>() - expectedMean) > 0.00000001m)
							violations.Add("Native SMA must match the independent Close window.");
					}
				};
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add("Every order must follow the SMA +/- relative price deviation breakout while flat, or fully exit on an actual adverse price/SMA crossing; no cooldown or stale entry-price anchor.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		AreEqual(expectedOrders, actualOrders);
		AreEqual(formedBars, nativeDeviations);
		AreEqual(formedBars, nativeMeans);
		IsTrue(buys > 0 && sells > 0 && exits > 0);
		IsTrue(staleAnchorDisagreements > 0, "The actual archive must contain flat bars where the SMA-anchored levels and a seeded/last-entry price anchor decide differently.");
		if (hvPeriod == 2) IsTrue(zeroDeviationBars > 0, "Actual equal-close windows must reach a formed zero price deviation.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard07")]
	public Task S0039_PercentStopWorksBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(HvBreakout);

	/// <summary>
	/// Folder key of the ATR Trailing example.
	/// </summary>
	protected const string AtrTrailing = "0040_ATR_Trailing";

	[TestMethod]
	[TestCategory("Shard00")]
	[DataRow(14, 20, 3.0)]
	[DataRow(7, 10, 1.5)]
	[DataRow(14, 20, 0.0)]
	public async Task S0040_IndependentSmaCrossingEntriesAndFillAnchoredNonRetreatingAtrTrail(int atrPeriod, int maPeriod, double multiplier)
	{
		var closes = new Queue<decimal>();
		var atr = 0m;
		var bars = 0;
		decimal? previousRawClose = null;
		decimal? previousReadyClose = null;
		var previousMean = 0m;
		decimal? stopLevel = null;
		var entryDistance = 0m;
		var entryVolume = 0m;
		var entryValue = 0m;
		Sides entrySide = default;
		Order pending = null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buys = 0;
		var sells = 0;
		var crossExits = 0;
		var flatWithoutCrossing = 0;
		var quoteExits = 0;
		var quoteExitFills = 0;
		var ratchets = 0;
		var rejectedRetreats = 0;
		var historicalWickWouldExit = 0;
		var fillAnchorsDifferentFromClose = 0;
		var lastClose = 0m;
		var stopOrders = new HashSet<Order>();
		var violations = new List<string>();
		await Replay(AtrTrailing, (strategy, _) =>
		{
			AreEqual(14, strategy.Parameters["AtrPeriod"].Value);
			AreEqual(20, strategy.Parameters["MAPeriod"].Value);
			AreEqual(3m, Convert.ToDecimal(strategy.Parameters["AtrMultiplier"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "AtrPeriod", atrPeriod);
			SetParam(strategy, "MAPeriod", maPeriod);
			SetParam(strategy, "AtrMultiplier", Convert.ToDecimal(multiplier));
			bool blocked() => pending is not null && pending.State is not (OrderStates.Done or OrderStates.Failed);
			void ratchet(decimal candidate)
			{
				if (stopLevel is decimal old)
				{
					if (entrySide == Sides.Buy ? candidate > old : candidate < old) ratchets++;
					else if (entrySide == Sides.Buy ? candidate < old : candidate > old) rejectedRetreats++;
					stopLevel = entrySide == Sides.Buy ? Math.Max(old, candidate) : Math.Min(old, candidate);
				}
				else stopLevel = candidate;
			}
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				var close = candle.ClosePrice;
				var tr = previousRawClose is decimal prev
					? Math.Max(candle.HighPrice - candle.LowPrice, Math.Max(Math.Abs(candle.HighPrice - prev), Math.Abs(candle.LowPrice - prev)))
					: candle.HighPrice - candle.LowPrice;
				bars++;
				var length = Math.Min(bars, atrPeriod);
				atr = (atr * (length - 1) + tr) / length;
				previousRawClose = close;
				closes.Enqueue(close);
				if (closes.Count > maPeriod) closes.Dequeue();
				if (bars < Math.Max(atrPeriod, maPeriod)) return;
				var mean = closes.Average();
				var upCross = previousReadyClose is decimal belowOrAt && belowOrAt <= previousMean && close > mean;
				var downCross = previousReadyClose is decimal aboveOrAt && aboveOrAt >= previousMean && close < mean;
				var adverseCross = strategy.Position > 0m && downCross || strategy.Position < 0m && upCross;
				if (strategy.Position != 0m && (strategy.Position > 0m) == (entrySide == Sides.Buy) && multiplier > 0.0)
				{
					ratchet(strategy.Position > 0m ? close - atr * Convert.ToDecimal(multiplier) : close + atr * Convert.ToDecimal(multiplier));
					if (!adverseCross && (strategy.Position > 0m ? candle.LowPrice <= stopLevel : candle.HighPrice >= stopLevel)) historicalWickWouldExit++;
				}
				if (!blocked())
				{
					if (upCross || downCross)
					{
						var side = upCross ? Sides.Buy : Sides.Sell;
						if (strategy.Position == 0m || (strategy.Position > 0m) != (side == Sides.Buy))
						{
							if (strategy.Position != 0m) crossExits++;
							expectedSide = side;
							expectedVolume = strategy.Volume + Math.Abs(strategy.Position);
							entrySide = side;
							entryDistance = atr * Convert.ToDecimal(multiplier);
							entryValue = entryVolume = 0m;
							stopLevel = null;
							if (side == Sides.Buy) buys++; else sells++;
							expectedOrders++;
						}
					}
					else if (strategy.Position == 0m && close != mean)
						flatWithoutCrossing++;
				}
				lastClose = close;
				previousReadyClose = close;
				previousMean = mean;
			};
			strategy.Level1Received += (_, quote) =>
			{
				expectedSide = null;
				if (strategy.Position == 0m || stopLevel is not decimal stop || blocked()) return;
				var price = quote.TryGetDecimal(strategy.Position > 0m ? Level1Fields.BestBidPrice : Level1Fields.BestAskPrice);
				if (price is decimal fresh && fresh > 0m && (strategy.Position > 0m ? fresh <= stop : fresh >= stop))
				{
					expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
					expectedVolume = Math.Abs(strategy.Position);
					expectedOrders++;
					quoteExits++;
				}
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add("Every order must follow the independent price/SMA crossing entry (closing an opposite position) or a fresh executable quote crossing the non-retreating actual-fill ATR trail; never a level entry without a crossing, a retroactive wick or a cooldown.");
				if (strategy.Position != 0m && expectedSide is not null && quoteExits > 0 && order.Comment == "ATR trailing stop")
					stopOrders.Add(order);
				pending = order;
				expectedSide = null;
			};
			strategy.Trades.TradeAdded += trade =>
			{
				if (strategy.Position == 0m)
				{
					if (stopOrders.Contains(trade.Order)) quoteExitFills++;
					stopLevel = null;
					entryVolume = entryValue = 0m;
				}
				else if (trade.Order.Side == entrySide && multiplier > 0.0)
				{
					entryVolume += trade.Trade.Volume;
					entryValue += trade.Trade.Price * trade.Trade.Volume;
					var entry = entryValue / entryVolume;
					if (entry != lastClose) fillAnchorsDifferentFromClose++;
					ratchet(entrySide == Sides.Buy ? entry - entryDistance : entry + entryDistance);
				}
			};
		}, TimeSpan.FromDays(31));
		AreEqual(expectedOrders, actualOrders, "Every price/SMA crossing that opens or reverses must act, and a flat price merely above or below the SMA must not enter.");
		IsTrue(buys > 0 && sells > 0 && crossExits > 0, "The real archive must exercise crossing entries in both directions and a crossing that reverses an open position.");
		IsTrue(flatWithoutCrossing > 0, "The real archive must reach flat bars off the SMA without a crossing, where a level entry would wrongly trade.");
		if (multiplier > 0.0)
			IsTrue(quoteExits > 0 && quoteExitFills > 0 && ratchets > 0 && rejectedRetreats > 0 && historicalWickWouldExit > 0 && fillAnchorsDifferentFromClose > 0,
				"The real archive must exercise actual quote-stop fills, tightening, forbidden retreat/retroactive wick and fill price different from the signal Close.");
		else AreEqual(0, quoteExits);
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S0040_RiskMultiplierChangesActualExecutionAndTightStopsFillWithinSignalBar()
	{
		var traces = new List<List<string>>();
		foreach (var multiplier in new[] { 0m, 3m, 0.00001m })
		{
			var trace = new List<string>();
			var bars = 0;
			var entryBar = 0;
			DateTime? entryFill = null;
			var tightStops = 0;
			var stopOrders = new HashSet<Order>();
			await Replay(AtrTrailing, (strategy, _) =>
			{
				SetParam(strategy, "AtrMultiplier", multiplier);
				strategy.CandleReceived += (_, candle) => { if (candle.State == CandleStates.Finished) bars++; };
				strategy.OrderRegistering += order =>
				{
					trace.Add($"{strategy.CurrentTime:O}|{order.Side}|{order.Volume}|{order.Comment}");
					if (order.Comment == "ATR trailing stop")
					{
						AreEqual(OrderTypes.Market, order.Type);
						AreEqual(Math.Abs(strategy.Position), order.Volume);
						AreEqual(strategy.Position > 0m ? Sides.Sell : Sides.Buy, order.Side);
						stopOrders.Add(order);
					}
				};
				strategy.Trades.TradeAdded += trade =>
				{
					if (trade.Order.Comment == "ATR trailing entry")
					{
						entryFill = strategy.CurrentTime;
						entryBar = bars;
					}
					else if (stopOrders.Contains(trade.Order) && strategy.Position == 0m && bars == entryBar
						&& entryFill is DateTime filled && strategy.CurrentTime < filled.AddMinutes(4))
						tightStops++;
				};
			}, TimeSpan.FromDays(7));
			if (multiplier == 0m) AreEqual(0, stopOrders.Count);
			if (multiplier == 0.00001m) IsTrue(tightStops > 0, "Actual tight-stop flatten fills must occur before another finished signal bar, not merely submit a later candle exit.");
			traces.Add(trace);
		}
		IsTrue(!traces[0].SequenceEqual(traces[1]) && !traces[1].SequenceEqual(traces[2]), "The ATR multiplier must change actual execution, not remain unused metadata.");
	}

	private const string VolMa = "0041_Vol_Adjusted_MA";

	[TestMethod]
	[TestCategory("Shard01")]
	[DataRow(20, 14, 2.0)]
	[DataRow(10, 7, 1.5)]
	[DataRow(40, 14, 1.0)]
	public async Task S0041_IndependentSmaWilderAtrBandEntriesAndActualAdverseCrossExits(int maPeriod, int atrPeriod, double multiplier)
	{
		var closes = new Queue<decimal>();
		var atr = 0m;
		var mean = 0m;
		var bars = 0;
		var formedBars = 0;
		var nativeAtr = 0;
		var nativeMean = 0;
		decimal? previousRawClose = null;
		decimal? previousReadyClose = null;
		var previousMean = 0m;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buys = 0;
		var sells = 0;
		var exits = 0;
		var violations = new List<string>();
		await Replay(VolMa, (strategy, _) =>
		{
			AreEqual(20, strategy.Parameters["MAPeriod"].Value);
			AreEqual(14, strategy.Parameters["ATRPeriod"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["ATRMultiplier"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			IsTrue(strategy.Parameters.TryGetValue("StopLossATRMultiplier", out var stop));
			AreEqual(2m, Convert.ToDecimal(stop.Value));
			SetParam(strategy, "MAPeriod", maPeriod);
			SetParam(strategy, "ATRPeriod", atrPeriod);
			SetParam(strategy, "ATRMultiplier", Convert.ToDecimal(multiplier));
			SetParam(strategy, "StopLossATRMultiplier", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				var close = candle.ClosePrice;
				var tr = previousRawClose is decimal prev
					? Math.Max(candle.HighPrice - candle.LowPrice, Math.Max(Math.Abs(candle.HighPrice - prev), Math.Abs(candle.LowPrice - prev)))
					: candle.HighPrice - candle.LowPrice;
				bars++;
				var length = Math.Min(bars, atrPeriod);
				atr = (atr * (length - 1) + tr) / length;
				previousRawClose = close;
				closes.Enqueue(close);
				if (closes.Count > maPeriod) closes.Dequeue();
				if (bars < Math.Max(atrPeriod, maPeriod)) return;
				formedBars++;
				mean = closes.Average();
				if (previousReadyClose is decimal prior &&
					(strategy.Position > 0m && prior >= previousMean && close < mean ||
					 strategy.Position < 0m && prior <= previousMean && close > mean))
				{
					expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
					expectedVolume = Math.Abs(strategy.Position);
					exits++;
				}
				else if (strategy.Position == 0m && (close > mean + atr * Convert.ToDecimal(multiplier) || close < mean - atr * Convert.ToDecimal(multiplier)))
				{
					expectedSide = close > mean ? Sides.Buy : Sides.Sell;
					expectedVolume = strategy.Volume;
					if (expectedSide == Sides.Buy) buys++; else sells++;
				}
				if (expectedSide is not null) expectedOrders++;
				previousReadyClose = close;
				previousMean = mean;
			};
			strategy.Indicators.Added += indicator =>
			{
				indicator.Changed += (_, value) =>
				{
					if (!value.IsFinal || !indicator.IsFormed || bars < Math.Max(atrPeriod, maPeriod)) return;
					if (indicator is AverageTrueRange)
					{
						nativeAtr++;
						if (Math.Abs(value.GetValue<decimal>() - atr) > 0.00000001m)
							violations.Add("Native Wilder ATR must match independent true-range smoothing.");
					}
					else if (indicator is SimpleMovingAverage)
					{
						nativeMean++;
						if (Math.Abs(value.GetValue<decimal>() - mean) > 0.00000001m)
							violations.Add("Native SMA must match the independent rolling Close mean.");
					}
				};
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add("Every order must strictly break a formed SMA plus/minus current Wilder ATR band while flat, or fully exit on a genuine adverse price/SMA crossing; no cooldown/EMA/constant-band proxy.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		AreEqual(expectedOrders, actualOrders);
		AreEqual(formedBars, nativeAtr);
		AreEqual(formedBars, nativeMean);
		IsTrue(buys > 0 && sells > 0 && exits > 0);
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard01")]
	public Task S0041_AtrDistancesAndSeparateRiskParameterChangeRealExecutions()
		=> CheckAtrDistancesAndRiskParameterChangeRealExecutions(VolMa, "Vol adjusted MA entry",
			stopParameter: "StopLossATRMultiplier", atrPeriodParameter: "ATRPeriod");

	private const string IvSpike = "0042_IV_Spike";

	// Packaged TON closes stand in for the IV readings, at a spike threshold a price series reaches.
	private const decimal IvSpikeFixtureThreshold = 1.002m;

	[TestMethod]
	[TestCategory("Shard02")]
	public async Task S0042_RejectsMissingOrSameIvInputThenTradesWithExplicitInput()
	{
		await Replay(IvSpike, (strategy, secondary) =>
		{
			var start = typeof(Strategy).GetMethod("OnStarted2", BindingFlags.NonPublic | BindingFlags.Instance);
			foreach (var invalid in new[] { null, strategy.Security })
			{
				SetParam(strategy, "IVSecurity", invalid);
				var rejected = false;
				try
				{
					start.Invoke(strategy, [DateTime.MinValue]);
				}
				catch (TargetInvocationException exception)
				{
					IsTrue(exception.InnerException is InvalidOperationException && exception.InnerException.Message.Contains("IVSecurity"),
						"Reject the explicit IV input contract before touching subscriptions or falling back to the traded instrument.");
					rejected = true;
				}
				IsTrue(rejected, "A missing IV input, or the traded instrument posing as its own IV, must be rejected before startup side effects.");
				AreEqual(0, strategy.Indicators.Count);
			}
			SetParam(strategy, "IVSecurity", secondary);
			SetParam(strategy, "IVSpikeThreshold", IvSpikeFixtureThreshold);
		}, TimeSpan.FromDays(7));
	}

	[TestMethod]
	[TestCategory("Shard02")]
	[DataRow(20, 20, 1.002, false)]
	[DataRow(10, 5, 1.001, false)]
	[DataRow(20, 10, 1.005, true)]
	public async Task S0042_SpikeFadesPriceAndIvDeclineExitsOnMatchedBarsNotIvMarketHistory(int maPeriod, int ivPeriod, double threshold, bool reversed)
	{
		var spikeThreshold = (decimal)threshold;
		var closes = new Queue<decimal>();
		var readings = new Queue<decimal>();
		decimal? previousReading = null;
		var ivTime = DateTime.MinValue;
		var mainTime = DateTime.MinValue;
		var processedTime = DateTime.MinValue;
		var hasChange = false;
		var rise = false;
		var decline = false;
		var jump = false;
		var ivFormed = false;
		var aboveAverage = false;
		var aboveAverageMultiple = false;
		var mainClose = 0m;
		var priceMean = 0m;
		var ivMean = 0m;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var joinedPairs = 0;
		var spikes = 0;
		var risesWithinThreshold = 0;
		var averageMultipleWithoutJump = 0;
		var spikesBelowAverageMultiple = 0;
		var jumpsNotAboveAverage = 0;
		var unchangedWhileHeld = 0;
		var buys = 0;
		var sells = 0;
		var exits = 0;
		var formedPriceBars = 0;
		var formedIvBars = 0;
		var priceChecks = 0;
		var ivChecks = 0;
		var violations = new List<string>();
		await Replay(IvSpike, (strategy, secondary) =>
		{
			AreEqual(20, strategy.Parameters["MAPeriod"].Value);
			AreEqual(20, strategy.Parameters["IVPeriod"].Value);
			AreEqual(1.5m, Convert.ToDecimal(strategy.Parameters["IVSpikeThreshold"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
			var primary = strategy.Security;
			strategy.Security = reversed ? secondary : primary;
			var iv = reversed ? primary : secondary;
			SetParam(strategy, "IVSecurity", iv);
			SetParam(strategy, "MAPeriod", maPeriod);
			SetParam(strategy, "IVPeriod", ivPeriod);
			SetParam(strategy, "IVSpikeThreshold", spikeThreshold);
			// Isolate the signal contract; the stop has its own test.
			SetParam(strategy, "StopLossPercent", 0m);
			var tradedId = strategy.Security.Id.ToSecurityId();
			var ivId = iv.Id.ToSecurityId();
			IsTrue(tradedId != ivId);
			TestContext.WriteLine($"Mechanics fixture only: traded={tradedId}, IV input={ivId}. No actual implied volatility history or IV performance claim.");
			strategy.Indicators.Added += indicator =>
			{
				if (indicator is not SimpleMovingAverage average) return;
				average.Changed += (input, output) =>
				{
					if (!output.IsFinal || !average.IsFormed || input is not CandleIndicatorValue { Value: { } candle }) return;
					if (candle.SecurityId == tradedId)
					{
						priceChecks++;
						if (average.Length != maPeriod || Math.Abs(output.GetValue<decimal>() - priceMean) > 0.00000001m)
							violations.Add("The price average must be the MAPeriod mean of the traded instrument's own closes.");
					}
					else if (candle.SecurityId == ivId)
					{
						ivChecks++;
						if (average.Length != ivPeriod || Math.Abs(output.GetValue<decimal>() - ivMean) > 0.00000001m)
							violations.Add("The IV average must be the IVPeriod mean of the IV instrument's closes.");
					}
					else violations.Add("Neither average may be fed by a third stream.");
				};
			};
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				if (candle.SecurityId == tradedId)
				{
					mainTime = candle.OpenTime;
					mainClose = candle.ClosePrice;
					closes.Enqueue(mainClose);
					if (closes.Count > maPeriod) closes.Dequeue();
					priceMean = closes.Average();
					if (closes.Count == maPeriod) formedPriceBars++;
				}
				else if (candle.SecurityId == ivId)
				{
					var reading = candle.ClosePrice;
					ivTime = candle.OpenTime;
					hasChange = previousReading.HasValue;
					rise = reading > previousReading;
					decline = reading < previousReading;
					// The threshold multiplies the prior reading; the IVPeriod average only has to be exceeded.
					jump = rise && previousReading is decimal prior && prior > 0m && reading >= spikeThreshold * prior;
					previousReading = reading;
					readings.Enqueue(reading);
					if (readings.Count > ivPeriod) readings.Dequeue();
					ivMean = readings.Average();
					ivFormed = readings.Count == ivPeriod;
					if (ivFormed) formedIvBars++;
					aboveAverage = ivFormed && reading > ivMean;
					aboveAverageMultiple = ivFormed && reading > spikeThreshold * ivMean;
				}
				else throw new InvalidOperationException("A third candle stream must not be substituted for either input.");
				if (!hasChange || closes.Count < maPeriod || mainTime != ivTime || processedTime == mainTime) return;
				processedTime = mainTime;
				joinedPairs++;
				if (strategy.Position != 0m)
				{
					if (decline)
					{
						expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
						expectedVolume = Math.Abs(strategy.Position);
						exits++;
					}
					else if (!rise) unchangedWhileHeld++;
				}
				else if (jump && aboveAverage)
				{
					spikes++;
					if (!aboveAverageMultiple) spikesBelowAverageMultiple++;
					if (mainClose != priceMean)
					{
						expectedSide = mainClose < priceMean ? Sides.Buy : Sides.Sell;
						expectedVolume = strategy.Volume;
						if (expectedSide == Sides.Buy) buys++; else sells++;
					}
				}
				else if (jump && ivFormed) jumpsNotAboveAverage++;
				else if (rise && !jump)
				{
					risesWithinThreshold++;
					if (aboveAverageMultiple) averageMultipleWithoutJump++;
				}
				if (expectedSide is not null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market || order.Security.Id != strategy.Security.Id)
					violations.Add("Every order must consume one matched finished traded/IV pair: from flat, an IV rise to at least the threshold times the prior reading that ends above the IVPeriod average fades the close's side of the MA; while held, a strict IV fall closes the whole position; nothing else trades.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		TestContext.WriteLine($"pairs={joinedPairs}, spikes={spikes}, buys={buys}, sells={sells}, exits={exits}, risesWithinThreshold={risesWithinThreshold}, averageMultipleWithoutJump={averageMultipleWithoutJump}, spikesBelowAverageMultiple={spikesBelowAverageMultiple}, jumpsNotAboveAverage={jumpsNotAboveAverage}, unchangedWhileHeld={unchangedWhileHeld}");
		AreEqual(expectedOrders, actualOrders);
		AreEqual(formedPriceBars, priceChecks);
		AreEqual(formedIvBars, ivChecks);
		IsTrue(joinedPairs > 100 && spikes > 0 && buys > 0 && sells > 0 && exits > 0, "The fixture must exercise spike entries on both sides of the MA and IV-decline exits.");
		IsTrue(risesWithinThreshold > 0, "IV rises smaller than the threshold times the prior reading must not be taken for spikes.");
		IsTrue(averageMultipleWithoutJump > 0, "A rise above the threshold times the IV average is not a spike without a threshold jump from the prior reading.");
		IsTrue(spikesBelowAverageMultiple > 0, "A threshold jump from the prior reading is a spike even when the reading stays below the threshold times the IV average.");
		IsTrue(jumpsNotAboveAverage > 0, "A threshold jump that leaves the reading at or below its IVPeriod average must not open a position.");
		if (!reversed) IsTrue(unchangedWhileHeld > 0, "An unchanged IV reading while held is not a decline and must not close the position.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard02")]
	public Task S0042_PercentStopWorksBetweenFinishedBarsWithExplicitIvInput()
		=> CheckPercentStopBetweenBars(IvSpike, setup: (strategy, secondary) =>
		{
			SetParam(strategy, "IVSecurity", secondary);
			SetParam(strategy, "IVSpikeThreshold", IvSpikeFixtureThreshold);
		});

	private const string Vcp = "0043_VCP";

	[TestMethod]
	[TestCategory("Shard03")]
	[DataRow(20, 20, 3)]
	[DataRow(10, 10, 2)]
	[DataRow(30, 20, 3)]
	public async Task S0043_IndependentPriorHighLowRangeContractionsAndSmaCrossExits(int maPeriod, int lookback, int contractions)
	{
		var ranges = new Queue<(decimal high, decimal low)>();
		var closes = new Queue<decimal>();
		var bars = 0;
		var rangeBars = 0;
		var meanBars = 0;
		var nativeHighs = 0;
		var nativeLows = 0;
		var nativeMeans = 0;
		var high = 0m;
		var low = 0m;
		var mean = 0m;
		decimal? previousHigh = null;
		var previousLow = 0m;
		var contractionCount = 0;
		var strictlyConsecutiveCount = 0;
		decimal? previousReadyClose = null;
		var previousMean = 0m;
		decimal? previousRawClose = null;
		var atr = 0m;
		var expandingAtrEntries = 0;
		var plateauPreservingEntries = 0;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buys = 0;
		var sells = 0;
		var exits = 0;
		var violations = new List<string>();
		await Replay(Vcp, (strategy, _) =>
		{
			AreEqual(20, strategy.Parameters["MAPeriod"].Value);
			IsTrue(strategy.Parameters.TryGetValue("LookbackPeriod", out var period), "README requires a real rolling High/Low range, not ATR bands.");
			AreEqual(20, period.Value);
			IsTrue(strategy.Parameters.TryGetValue("ContractionBars", out var required));
			AreEqual(3, required.Value);
			IsTrue(strategy.Parameters.TryGetValue("StopLossPercent", out var stop));
			AreEqual(2m, Convert.ToDecimal(stop.Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "MAPeriod", maPeriod);
			SetParam(strategy, "LookbackPeriod", lookback);
			SetParam(strategy, "ContractionBars", contractions);
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				bars++;
				var close = candle.ClosePrice;
				var previousAtr = atr;
				var tr = previousRawClose is decimal raw
					? Math.Max(candle.HighPrice - candle.LowPrice, Math.Max(Math.Abs(candle.HighPrice - raw), Math.Abs(candle.LowPrice - raw)))
					: candle.HighPrice - candle.LowPrice;
				var length = Math.Min(bars, 14);
				atr = (atr * (length - 1) + tr) / length;
				previousRawClose = close;
				ranges.Enqueue((candle.HighPrice, candle.LowPrice));
				if (ranges.Count > lookback) ranges.Dequeue();
				closes.Enqueue(close);
				if (closes.Count > maPeriod) closes.Dequeue();
				if (closes.Count == maPeriod) { mean = closes.Average(); meanBars++; }
				if (ranges.Count != lookback) return;
				rangeBars++;
				high = ranges.Max(range => range.high);
				low = ranges.Min(range => range.low);
				var entered = false;
				if (closes.Count == maPeriod && previousReadyClose is decimal prior && previousHigh is decimal upper)
				{
					if (strategy.Position > 0m && prior >= previousMean && close < mean ||
						strategy.Position < 0m && prior <= previousMean && close > mean)
					{
						expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
						expectedVolume = Math.Abs(strategy.Position);
						exits++;
					}
					else if (strategy.Position == 0m && contractionCount >= contractions && (close > upper || close < previousLow))
					{
						expectedSide = close > upper ? Sides.Buy : Sides.Sell;
						expectedVolume = strategy.Volume;
						if (expectedSide == Sides.Buy) buys++; else sells++;
						if (atr > previousAtr) expandingAtrEntries++;
						if (strictlyConsecutiveCount < contractions) plateauPreservingEntries++;
						entered = true;
					}
					if (expectedSide is not null) expectedOrders++;
				}
				if (previousHigh is decimal previousUpper)
				{
					var change = high - low - (previousUpper - previousLow);
					if (change < 0m) { contractionCount++; strictlyConsecutiveCount++; }
					else
					{
						strictlyConsecutiveCount = 0;
						if (change > 0m) contractionCount = 0;
					}
				}
				previousHigh = high;
				previousLow = low;
				if (entered) { contractionCount = 0; strictlyConsecutiveCount = 0; }
				if (closes.Count != maPeriod) return;
				previousReadyClose = close;
				previousMean = mean;
			};
			strategy.Indicators.Added += indicator =>
			{
				indicator.Changed += (_, value) =>
				{
					if (!value.IsFinal || !indicator.IsFormed) return;
					if (indicator is Highest)
					{
						nativeHighs++;
						if (value.GetValue<decimal>() != high) violations.Add("Highest must use the independent rolling candle High window.");
					}
					else if (indicator is Lowest)
					{
						nativeLows++;
						if (value.GetValue<decimal>() != low) violations.Add("Lowest must use the independent rolling candle Low window.");
					}
					else if (indicator is SimpleMovingAverage)
					{
						nativeMeans++;
						if (Math.Abs(value.GetValue<decimal>() - mean) > 0.00000001m) violations.Add("SMA must use the independent rolling Close window.");
					}
				};
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add("Every entry must break the PRIOR completed High/Low channel after actual range contractions; every held exit must fully close on an actual adverse Close/SMA crossing. No current-bar lookahead, ATR-band or cooldown substitute.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		AreEqual(expectedOrders, actualOrders);
		AreEqual(rangeBars, nativeHighs);
		AreEqual(rangeBars, nativeLows);
		AreEqual(meanBars, nativeMeans);
		IsTrue(buys > 0 && sells > 0 && exits > 0);
		IsTrue(expandingAtrEntries > 0, "Actual range breakouts must include entries with expanding ATR, contradicting the old ATR-decline proxy.");
		IsTrue(plateauPreservingEntries > 0, "The archive must distinguish a neutral equal range from an expansion that invalidates a contraction sequence.");
		TestContext.WriteLine($"MA={maPeriod}, range={lookback}, contractions={contractions}: orders={actualOrders}, buys={buys}, sells={sells}, exits={exits}, expandingATR={expandingAtrEntries}, neutralPlateau={plateauPreservingEntries}");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard03")]
	public Task S0043_PercentStopActuallyFlattensBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(Vcp);

	private const string AtrRangeExample = "0044_ATR_Range";

	[TestMethod]
	[TestCategory("Shard04")]
	[DataRow(20, 14, 5)]
	[DataRow(10, 7, 3)]
	[DataRow(10, 7, 1)]
	[DataRow(2, 1, 2)]
	public async Task S0044_IndependentExactNIntervalMovementStrictAtrAndEveryBarSmaCrossExits(int maPeriod, int atrPeriod, int lookback)
	{
		var closes = new Queue<decimal>();
		var movementWindow = new Queue<decimal>();
		var bars = 0;
		var formedBars = 0;
		var nativeMeans = 0;
		var nativeAtrs = 0;
		var mean = 0m;
		var atr = 0m;
		decimal? previousRawClose = null;
		decimal? previousReadyClose = null;
		var previousMean = 0m;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buys = 0;
		var sells = 0;
		var exits = 0;
		var exitsBetweenCheckpoints = 0;
		var nVersusNMinusOne = 0;
		var strictEqualityBars = 0;
		var violations = new List<string>();
		await Replay(AtrRangeExample, (strategy, secondary) =>
		{
			// Real TON bars cover exact nonzero movement/TR equality with ATR length one.
			if (atrPeriod == 1) strategy.Security = secondary;
			AreEqual(20, strategy.Parameters["MAPeriod"].Value);
			AreEqual(14, strategy.Parameters["ATRPeriod"].Value);
			AreEqual(5, strategy.Parameters["LookbackPeriod"].Value);
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			IsTrue(strategy.Parameters.TryGetValue("AtrMultiplier", out var stop), "The promised ATR stop must not remain absent metadata.");
			AreEqual(2m, Convert.ToDecimal(stop.Value));
			SetParam(strategy, "MAPeriod", maPeriod);
			SetParam(strategy, "ATRPeriod", atrPeriod);
			SetParam(strategy, "LookbackPeriod", lookback);
			SetParam(strategy, "AtrMultiplier", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				bars++;
				var close = candle.ClosePrice;
				var tr = previousRawClose is decimal raw
					? Math.Max(candle.HighPrice - candle.LowPrice, Math.Max(Math.Abs(candle.HighPrice - raw), Math.Abs(candle.LowPrice - raw)))
					: candle.HighPrice - candle.LowPrice;
				var length = Math.Min(bars, atrPeriod);
				atr = (atr * (length - 1) + tr) / length;
				previousRawClose = close;
				closes.Enqueue(close);
				if (closes.Count > maPeriod) closes.Dequeue();
				movementWindow.Enqueue(close);
				if (movementWindow.Count > lookback + 1) movementWindow.Dequeue();
				if (bars < Math.Max(maPeriod, atrPeriod)) return;
				formedBars++;
				mean = closes.Average();
				var checkpoint = bars % lookback == 0;
				if (previousReadyClose is decimal prior &&
					(strategy.Position > 0m && prior >= previousMean && close < mean ||
					 strategy.Position < 0m && prior <= previousMean && close > mean))
				{
					expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
					expectedVolume = Math.Abs(strategy.Position);
					exits++;
					if (!checkpoint) exitsBetweenCheckpoints++;
				}
				else if (strategy.Position == 0m && checkpoint && movementWindow.Count == lookback + 1)
				{
					var movement = close - movementWindow.Peek();
					var shortMovement = close - movementWindow.ElementAt(1);
					if (Math.Abs(movement) == atr && movement != 0m) strictEqualityBars++;
					if (Math.Abs(movement) > atr)
					{
						expectedSide = movement > 0m ? Sides.Buy : Sides.Sell;
						expectedVolume = strategy.Volume;
						if (expectedSide == Sides.Buy) buys++; else sells++;
						if (Math.Abs(shortMovement) <= atr || Math.Sign(shortMovement) != Math.Sign(movement)) nVersusNMinusOne++;
					}
				}
				if (expectedSide is not null) expectedOrders++;
				previousReadyClose = close;
				previousMean = mean;
			};
			strategy.Indicators.Added += indicator =>
			{
				indicator.Changed += (_, value) =>
				{
					if (!value.IsFinal || !indicator.IsFormed || bars < Math.Max(maPeriod, atrPeriod)) return;
					if (indicator is AverageTrueRange)
					{
						nativeAtrs++;
						if (Math.Abs(value.GetValue<decimal>() - atr) > 0.00000001m) violations.Add("Native Wilder ATR must match independent true-range smoothing.");
					}
					else if (indicator is SimpleMovingAverage)
					{
						nativeMeans++;
						if (Math.Abs(value.GetValue<decimal>() - mean) > 0.00000001m) violations.Add("Native SMA must match the independent rolling Close window.");
					}
				};
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add("Every flat entry must follow strict absolute Close[t]-Close[t-N] > current ATR at a cold-origin N-bar checkpoint. Every adverse Close/SMA cross must fully exit on ANY ready bar; no N-1-block, shifted warmup phase, equality entry or cooldown substitute.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		AreEqual(expectedOrders, actualOrders);
		AreEqual(formedBars, nativeAtrs);
		AreEqual(formedBars, nativeMeans);
		IsTrue(buys > 0 && sells > 0 && exits > 0 && nVersusNMinusOne > 0);
		if (lookback > 1) IsTrue(exitsBetweenCheckpoints > 0, "Real SMA exits must not wait for the next N-bar entry checkpoint.");
		if (atrPeriod == 1) IsTrue(strictEqualityBars > 0, "Real nonzero movement equal to ATR must remain a strict-boundary no-entry case.");
		TestContext.WriteLine($"MA={maPeriod}, ATR={atrPeriod}, N={lookback}: orders={actualOrders}, buys={buys}, sells={sells}, exits={exits}, betweenChecks={exitsBetweenCheckpoints}, NvsN-1={nVersusNMinusOne}, equalATR={strictEqualityBars}");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard04")]
	public Task S0044_FrozenActualFillAtrDistanceAndRiskChangesRealExecutions()
		=> CheckAtrDistancesAndRiskParameterChangeRealExecutions(AtrRangeExample, "ATR range entry", atrPeriodParameter: "ATRPeriod");

	private const string ChoppinessExample = "0045_Choppiness_Index_Breakout";

	[TestMethod]
	[TestCategory("Shard05")]
	[DataRow(20, 14, 38.2, 61.8)]
	[DataRow(30, 10, 45.0, 55.0)]
	[DataRow(2, 2, 38.2, 61.8)]
	public async Task S0045_IndependentLogRangeChoppinessLevelsAndFullHighIndexExits(int maPeriod, int period, double lowThreshold, double highThreshold)
	{
		var ranges = new Queue<(decimal high, decimal low, decimal tr)>();
		var closes = new Queue<decimal>();
		decimal? previousRawClose = null;
		var bars = 0;
		var formedBars = 0;
		var nativeSum = 0;
		var nativeHighs = 0;
		var nativeLows = 0;
		var nativeMeans = 0;
		var nativeRanges = 0;
		var high = 0m;
		var low = 0m;
		var mean = 0m;
		var tr = 0m;
		var sumTr = 0m;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buys = 0;
		var sells = 0;
		var exits = 0;
		var wrongDenominatorExits = 0;
		var adverseMaWithoutExit = 0;
		var undefinedRanges = 0;
		var violations = new List<string>();
		await Replay(ChoppinessExample, (strategy, secondary) =>
		{
			if (period == 2) strategy.Security = secondary;
			AreEqual(20, strategy.Parameters["MAPeriod"].Value);
			AreEqual(14, strategy.Parameters["ChoppinessPeriod"].Value);
			AreEqual(38.2m, Convert.ToDecimal(strategy.Parameters["ChoppinessThreshold"].Value));
			AreEqual(61.8m, Convert.ToDecimal(strategy.Parameters["HighChoppinessThreshold"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			IsTrue(strategy.Parameters.TryGetValue("StopLossPercent", out var stop));
			AreEqual(2m, Convert.ToDecimal(stop.Value));
			SetParam(strategy, "MAPeriod", maPeriod);
			SetParam(strategy, "ChoppinessPeriod", period);
			SetParam(strategy, "ChoppinessThreshold", Convert.ToDecimal(lowThreshold));
			SetParam(strategy, "HighChoppinessThreshold", Convert.ToDecimal(highThreshold));
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				bars++;
				tr = previousRawClose is decimal previous
					? Math.Max(candle.HighPrice - candle.LowPrice, Math.Max(Math.Abs(candle.HighPrice - previous), Math.Abs(candle.LowPrice - previous)))
					: candle.HighPrice - candle.LowPrice;
				previousRawClose = candle.ClosePrice;
				ranges.Enqueue((candle.HighPrice, candle.LowPrice, tr));
				if (ranges.Count > period) ranges.Dequeue();
				closes.Enqueue(candle.ClosePrice);
				if (closes.Count > maPeriod) closes.Dequeue();
				if (bars < Math.Max(period, maPeriod)) return;
				formedBars++;
				high = ranges.Max(range => range.high);
				low = ranges.Min(range => range.low);
				sumTr = ranges.Sum(range => range.tr);
				mean = closes.Average();
				if (high <= low || sumTr <= 0m) { undefinedRanges++; return; }
				var index = 100m * (decimal)Math.Log10((double)(sumTr / (high - low))) / (decimal)Math.Log10(period);
				if (strategy.Position != 0m && index > Convert.ToDecimal(highThreshold))
				{
					expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
					expectedVolume = Math.Abs(strategy.Position);
					exits++;
					var summedWidths = ranges.Sum(range => range.high - range.low);
					if (summedWidths > 0m && 100m * (decimal)Math.Log10((double)(sumTr / summedWidths)) / (decimal)Math.Log10(period) <= Convert.ToDecimal(highThreshold)) wrongDenominatorExits++;
				}
				else if (strategy.Position == 0m && index < Convert.ToDecimal(lowThreshold) && candle.ClosePrice != mean)
				{
					expectedSide = candle.ClosePrice > mean ? Sides.Buy : Sides.Sell;
					expectedVolume = strategy.Volume;
					if (expectedSide == Sides.Buy) buys++; else sells++;
				}
				else if (strategy.Position > 0m && candle.ClosePrice < mean || strategy.Position < 0m && candle.ClosePrice > mean) adverseMaWithoutExit++;
				if (expectedSide is not null) expectedOrders++;
			};
			strategy.Indicators.Added += indicator =>
			{
				indicator.Changed += (_, value) =>
				{
					if (!value.IsFinal || !indicator.IsFormed || bars < Math.Max(period, maPeriod)) return;
					var actual = value.GetValue<decimal>();
					decimal expected;
					if (indicator is Sum) { nativeSum++; expected = sumTr; }
					else if (indicator is Highest) { nativeHighs++; expected = high; }
					else if (indicator is Lowest) { nativeLows++; expected = low; }
					else if (indicator is SimpleMovingAverage) { nativeMeans++; expected = mean; }
					else if (indicator is AverageTrueRange) { nativeRanges++; expected = tr; }
					else return;
					if (Math.Abs(actual - expected) > 0.00000001m) violations.Add("Native TR1/Sum/High/Low/SMA components must match the independent actual candle windows, not the wrong sum-of-candle-widths denominator or a cold previous-Close=0 gap.");
				};
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add("Every flat entry must use canonical CHOP below the low LEVEL and strict Close/SMA direction; a held position exits in full only above the high LEVEL. No 99% proxy thresholds, sum-of-widths denominator, MA-exit or cooldown substitute.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		AreEqual(expectedOrders, actualOrders);
		AreEqual(formedBars, nativeSum);
		AreEqual(formedBars, nativeHighs);
		AreEqual(formedBars, nativeLows);
		AreEqual(formedBars, nativeMeans);
		AreEqual(formedBars, nativeRanges);
		IsTrue(buys > 0 && sells > 0 && exits > 0 && wrongDenominatorExits > 0);
		if (maPeriod == 30) IsTrue(adverseMaWithoutExit > 0, "An adverse SMA side alone must not replace the promised high-CHOP exit.");
		TestContext.WriteLine($"MA={maPeriod}, CHOP={period}: orders={actualOrders}, buys={buys}, sells={sells}, exits={exits}, denominatorDifferences={wrongDenominatorExits}, adverseMA={adverseMaWithoutExit}, undefined={undefinedRanges}");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public Task S0045_PercentStopActuallyFlattensBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(ChoppinessExample);

	private const string VolumeSpike = "0046_Volume_Spike";

	[TestMethod]
	[TestCategory("Shard06")]
	[DataRow(20, 20, 2.0)]
	[DataRow(10, 30, 1.5)]
	[DataRow(2, 2, 1.0)]
	public async Task S0046_IndependentCurrentVolumeMeanSpikeAndBelowMeanFullExits(int maPeriod, int volumePeriod, double multiplier)
	{
		var closes = new Queue<decimal>();
		var volumes = new Queue<decimal>();
		var bars = 0;
		var formedBars = 0;
		var nativePriceMeans = 0;
		var nativeVolumeMeans = 0;
		var mean = 0m;
		var volumeMean = 0m;
		decimal? previousVolume = null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buys = 0;
		var sells = 0;
		var exits = 0;
		var rollingSpikeRejectedByPreviousRatio = 0;
		var fallingVolumeAboveMeanHeld = 0;
		var equalityBars = 0;
		var violations = new List<string>();
		await Replay(VolumeSpike, (strategy, secondary) =>
		{
			if (volumePeriod == 2) strategy.Security = secondary;
			AreEqual(20, strategy.Parameters["MAPeriod"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["VolumeSpikeMultiplier"].Value));
			IsTrue(strategy.Parameters.TryGetValue("VolAvgPeriod", out var period), "The promised rolling volume mean cannot be replaced with the previous candle's volume.");
			AreEqual(20, period.Value);
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			IsTrue(strategy.Parameters.TryGetValue("StopLossPercent", out var stop));
			AreEqual(2m, Convert.ToDecimal(stop.Value));
			SetParam(strategy, "MAPeriod", maPeriod);
			SetParam(strategy, "VolAvgPeriod", volumePeriod);
			SetParam(strategy, "VolumeSpikeMultiplier", Convert.ToDecimal(multiplier));
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				bars++;
				var priorVolume = previousVolume;
				previousVolume = candle.TotalVolume;
				closes.Enqueue(candle.ClosePrice);
				if (closes.Count > maPeriod) closes.Dequeue();
				volumes.Enqueue(candle.TotalVolume);
				if (volumes.Count > volumePeriod) volumes.Dequeue();
				if (bars < Math.Max(maPeriod, volumePeriod)) return;
				formedBars++;
				mean = closes.Average();
				volumeMean = volumes.Average();
				if (candle.TotalVolume == volumeMean) equalityBars++;
				if (strategy.Position != 0m && candle.TotalVolume < volumeMean)
				{
					expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
					expectedVolume = Math.Abs(strategy.Position);
					exits++;
				}
				else if (strategy.Position == 0m && volumeMean > 0m && candle.TotalVolume > volumeMean * Convert.ToDecimal(multiplier) && candle.ClosePrice != mean)
				{
					expectedSide = candle.ClosePrice > mean ? Sides.Buy : Sides.Sell;
					expectedVolume = strategy.Volume;
					if (expectedSide == Sides.Buy) buys++; else sells++;
					if (priorVolume is decimal prior && candle.TotalVolume < prior * Convert.ToDecimal(multiplier)) rollingSpikeRejectedByPreviousRatio++;
				}
				else if (strategy.Position != 0m && priorVolume is decimal prior && candle.TotalVolume < prior) fallingVolumeAboveMeanHeld++;
				if (expectedSide is not null) expectedOrders++;
			};
			strategy.Indicators.Added += indicator =>
			{
				indicator.Changed += (_, value) =>
				{
					if (!value.IsFinal || !indicator.IsFormed || bars < Math.Max(maPeriod, volumePeriod) || indicator is not SimpleMovingAverage) return;
					var isVolume = indicator.Name == "Volume average";
					if (isVolume) nativeVolumeMeans++; else nativePriceMeans++;
					if (Math.Abs(value.GetValue<decimal>() - (isVolume ? volumeMean : mean)) > 0.00000001m)
						violations.Add("Native price and TotalVolume SMAs must match independent current-inclusive rolling windows at every fully ready bar.");
				};
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add("Every flat entry must strictly exceed rolling mean Volume times multiplier and follow Close/SMA direction. Every held exit must fully close below the volume mean, not on a one-bar decline, price/SMA exit or cooldown.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		AreEqual(expectedOrders, actualOrders);
		AreEqual(formedBars, nativePriceMeans);
		AreEqual(formedBars, nativeVolumeMeans);
		IsTrue(buys > 0 && sells > 0 && exits > 0);
		if (volumePeriod >= 20) IsTrue(rollingSpikeRejectedByPreviousRatio > 0 && fallingVolumeAboveMeanHeld > 0,
			"Real archive decisions must distinguish a rolling-mean spike from the previous-volume ratio, and an above-mean decline from a true below-mean exit.");
		TestContext.WriteLine($"MA={maPeriod}, volume={volumePeriod}, multiplier={multiplier}: orders={actualOrders}, buys={buys}, sells={sells}, exits={exits}, spikeVsPrevious={rollingSpikeRejectedByPreviousRatio}, aboveMeanDeclinesHeld={fallingVolumeAboveMeanHeld}, volumeEqualsMean={equalityBars}");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public Task S0046_PercentStopActuallyFlattensBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(VolumeSpike);

	private const string ObvBreakout = "0047_OBV_Breakout";

	[TestMethod]
	[TestCategory("Shard07")]
	[DataRow(20, 20)]
	[DataRow(10, 40)]
	[DataRow(1, 2)]
	public async Task S0047_IndependentPriorObvExtremaAndAnyActualObvSmaCrossFullExits(int lookback, int maPeriod)
	{
		var extrema = new Queue<decimal>();
		var averageWindow = new Queue<decimal>();
		var priceWindow = new Queue<decimal>();
		var bars = 0;
		var meanBars = 0;
		var nativeObvs = 0;
		var nativeMeans = 0;
		var obv = 0m;
		var mean = 0m;
		decimal? previousPrice = null;
		decimal? previousReadyObv = null;
		var previousMean = 0m;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buys = 0;
		var sells = 0;
		var exits = 0;
		var oneBarMoveWithoutBreakout = 0;
		var favorableCrossExits = 0;
		var crossesFromEquality = 0;
		var violations = new List<string>();
		await Replay(ObvBreakout, (strategy, secondary) =>
		{
			if (maPeriod == 2) strategy.Security = secondary;
			IsTrue(strategy.Parameters.TryGetValue("LookbackPeriod", out var period), "README requires previous OBV extrema, not only the latest OBV direction.");
			AreEqual(20, period.Value);
			IsTrue(strategy.Parameters.TryGetValue("OBVMAPeriod", out var average));
			AreEqual(20, average.Value);
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			IsTrue(strategy.Parameters.TryGetValue("StopLossPercent", out var stop));
			AreEqual(2m, Convert.ToDecimal(stop.Value));
			SetParam(strategy, "LookbackPeriod", lookback);
			SetParam(strategy, "OBVMAPeriod", maPeriod);
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				bars++;
				var priorPrice = previousPrice;
				var priorObv = obv;
				if (priorPrice is decimal close)
				{
					if (candle.ClosePrice > close) obv += candle.TotalVolume;
					else if (candle.ClosePrice < close) obv -= candle.TotalVolume;
				}
				previousPrice = candle.ClosePrice;
				averageWindow.Enqueue(obv);
				if (averageWindow.Count > maPeriod) averageWindow.Dequeue();
				priceWindow.Enqueue(candle.ClosePrice);
				if (priceWindow.Count > 20) priceWindow.Dequeue();
				var ready = averageWindow.Count == maPeriod;
				if (ready)
				{
					meanBars++;
					mean = averageWindow.Average();
					var upwardCross = previousReadyObv is decimal up && up <= previousMean && obv > mean;
					var downwardCross = previousReadyObv is decimal down && down >= previousMean && obv < mean;
					if (strategy.Position != 0m && (upwardCross || downwardCross))
					{
						expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
						expectedVolume = Math.Abs(strategy.Position);
						exits++;
						if (strategy.Position > 0m && upwardCross || strategy.Position < 0m && downwardCross) favorableCrossExits++;
						if (previousReadyObv == previousMean) crossesFromEquality++;
					}
					else if (strategy.Position == 0m && extrema.Count == lookback && priorPrice is decimal oldPrice)
					{
						var upper = extrema.Max();
						var lower = extrema.Min();
						if (obv > upper && candle.ClosePrice > oldPrice || obv < lower && candle.ClosePrice < oldPrice)
						{
							expectedSide = obv > upper ? Sides.Buy : Sides.Sell;
							expectedVolume = strategy.Volume;
							if (expectedSide == Sides.Buy) buys++; else sells++;
						}
						else if (priceWindow.Count == 20 &&
							(obv > priorObv && candle.ClosePrice > priceWindow.Average() || obv < priorObv && candle.ClosePrice < priceWindow.Average())) oneBarMoveWithoutBreakout++;
					}
					if (expectedSide is not null) expectedOrders++;
					previousReadyObv = obv;
					previousMean = mean;
				}
				// Only after evaluating the signal may the current OBV enter the extrema window.
				extrema.Enqueue(obv);
				if (extrema.Count > lookback) extrema.Dequeue();
			};
			strategy.Indicators.Added += indicator =>
			{
				indicator.Changed += (_, value) =>
				{
					if (!value.IsFinal || !indicator.IsFormed) return;
					if (indicator is OnBalanceVolume)
					{
						nativeObvs++;
						if (value.GetValue<decimal>() != obv) violations.Add("Native OBV must start at zero, add/subtract actual TotalVolume only on strict Close changes, and hold on equality.");
					}
					else if (indicator is SimpleMovingAverage)
					{
						nativeMeans++;
						if (Math.Abs(value.GetValue<decimal>() - mean) > 0.00000001m) violations.Add("The native SMA must average OBV itself, not price Close.");
					}
				};
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add("Every flat entry must strictly break PRIOR rolling OBV extrema with matching actual Close movement. A held position fully exits on ANY genuine OBV/OBV-SMA cross, not an OBV one-bar slope/price-SMA proxy, only-adverse cross, cooldown or current-window lookahead.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		AreEqual(expectedOrders, actualOrders);
		AreEqual(bars, nativeObvs);
		AreEqual(meanBars, nativeMeans);
		IsTrue(buys > 0 && sells > 0 && exits > 0);
		if (lookback > 1) IsTrue(oneBarMoveWithoutBreakout > 0, "Real one-bar volume/price confirmation must not substitute for a new OBV extreme.");
		if (maPeriod == 40) IsTrue(favorableCrossExits > 0, "The formal README's any-cross exit must include real favorable-direction crossings when the OBV mean window exceeds the entry lookback.");
		if (maPeriod == 2) IsTrue(crossesFromEquality > 0, "The real archive must distinguish a neutral OBV/mean touch from the subsequent departure crossing.");
		TestContext.WriteLine($"lookback={lookback}, OBVMA={maPeriod}: orders={actualOrders}, buys={buys}, sells={sells}, exits={exits}, nonBreakoutMoves={oneBarMoveWithoutBreakout}, favorableExits={favorableCrossExits}, equalityOriginExits={crossesFromEquality}");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard07")]
	public Task S0047_PercentStopActuallyFlattensBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(ObvBreakout);

	private const string VwapBreakout = "0048_VWAP_Breakout";

	[TestMethod]
	[TestCategory("Shard00")]
	[DataRow(false, 2.0)]
	[DataRow(true, 3.0)]
	public async Task S0048_IndependentUtcTypicalVwapActualCrossesStopAndReverse(bool secondarySecurity, double volume)
	{
		DateTime? session = null;
		var cumulativeValue = 0m;
		var cumulativeCloseValue = 0m;
		var cumulativeVolume = 0m;
		var expectedVwap = 0m;
		decimal? previousClose = null;
		var previousVwap = 0m;
		var previousCloseVwap = 0m;
		decimal? previousRollingVwap = null;
		var rollingBars = new Queue<ICandleMessage>();
		var sessions = 0;
		var bars = 0;
		var readyBars = 0;
		var nativeValues = 0;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buys = 0;
		var sells = 0;
		var exits = 0;
		var rapidExits = 0;
		var resetCrossesIgnored = 0;
		var entriesWithoutRollingCross = 0;
		var entriesWithoutCloseVwapCross = 0;
		var nativeDiffersFromVwma = 0;
		var lastEntryBar = 0;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var violations = new List<string>();
		await Replay(VwapBreakout, (strategy, secondary) =>
		{
			if (secondarySecurity) strategy.Security = secondary;
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			IsTrue(strategy.Parameters.TryGetValue("StopLossPercent", out var stop));
			AreEqual(2m, Convert.ToDecimal(stop.Value));
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.Volume = Convert.ToDecimal(volume);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				bars++;
				var oldClose = previousClose;
				var oldVwap = previousVwap;
				var date = candle.OpenTime.ToUniversalTime().Date;
				var newSession = session != date;
				if (newSession)
				{
					session = date;
					sessions++;
					cumulativeValue = cumulativeCloseValue = cumulativeVolume = 0m;
					previousClose = null;
				}
				cumulativeValue += (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m * candle.TotalVolume;
				cumulativeCloseValue += candle.ClosePrice * candle.TotalVolume;
				cumulativeVolume += candle.TotalVolume;
				rollingBars.Enqueue(candle);
				if (rollingBars.Count > 32) rollingBars.Dequeue();
				if (cumulativeVolume <= 0m) return;
				readyBars++;
				expectedVwap = cumulativeValue / cumulativeVolume;
				var closeVwap = cumulativeCloseValue / cumulativeVolume;
				var close = candle.ClosePrice;
				if (newSession && oldClose is decimal old &&
					(old <= oldVwap && close > expectedVwap || old >= oldVwap && close < expectedVwap)) resetCrossesIgnored++;
				var up = previousClose is decimal upClose && upClose <= previousVwap && close > expectedVwap;
				var down = previousClose is decimal downClose && downClose >= previousVwap && close < expectedVwap;
				decimal? rollingVwap = rollingBars.Count == 32 && rollingBars.Sum(b => b.TotalVolume) > 0m
					? rollingBars.Sum(b => b.ClosePrice * b.TotalVolume) / rollingBars.Sum(b => b.TotalVolume) : null;
				if (rollingVwap is decimal proxy && Math.Abs(proxy - expectedVwap) > 0.00000001m) nativeDiffersFromVwma++;
				if (up && strategy.Position <= 0m || down && strategy.Position >= 0m)
				{
					expectedSide = up ? Sides.Buy : Sides.Sell;
					expectedVolume = strategy.Position == 0m ? strategy.Volume : Math.Min(Math.Abs(strategy.Position), strategy.Volume) * 2m;
					if (strategy.Position != 0m)
					{
						exits++;
						if (bars - lastEntryBar < 500) rapidExits++;
					}
					lastEntryBar = bars;
					if (up) buys++; else sells++;
					if (rollingVwap is decimal currentProxy && previousRollingVwap is decimal previousProxy && previousClose is decimal prior &&
						!(up ? prior <= previousProxy && close > currentProxy : prior >= previousProxy && close < currentProxy)) entriesWithoutRollingCross++;
					if (previousClose is decimal priorClose &&
						!(up ? priorClose <= previousCloseVwap && close > closeVwap : priorClose >= previousCloseVwap && close < closeVwap)) entriesWithoutCloseVwapCross++;
				}
				if (expectedSide is not null) expectedOrders++;
				previousClose = close;
				previousVwap = expectedVwap;
				previousCloseVwap = closeVwap;
				previousRollingVwap = rollingVwap;
			};
			strategy.Indicators.Added += indicator =>
			{
				if (indicator is not VolumeWeightedAveragePrice) return;
				indicator.Changed += (_, output) =>
				{
					if (!output.IsFinal || output.IsEmpty) return;
					nativeValues++;
					if (Math.Abs(output.GetValue<decimal>() - expectedVwap) > 0.00000001m)
						violations.Add("Native VWAP must cumulatively average typical price times actual volume within each UTC date, including the first valid candle, not Close or a rolling VWMA.");
				};
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add("Every order must follow a real same-session Close/VWAP cross in its direction: Volume when flat, a bounded reversal of the opposite position when held. Session reset/side-only/500-bar cooldown/exit-only flattening cannot replace these rules.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		AreEqual(expectedOrders, actualOrders);
		AreEqual(readyBars, nativeValues);
		IsTrue(sessions >= 30 && buys > 0 && sells > 0 && exits > 0 && rapidExits > 0);
		IsTrue(resetCrossesIgnored > 0 && entriesWithoutRollingCross > 0 && entriesWithoutCloseVwapCross > 0 && nativeDiffersFromVwma > 0,
			"Real archive candles must expose reset jumps, rolling-Close VWMA substitutions and cumulative-Close price-source substitutions.");
		TestContext.WriteLine($"secondary={secondarySecurity}: bars={bars}, sessions={sessions}, native={nativeValues}, orders={actualOrders}, buys={buys}, sells={sells}, exits={exits}, rapidExits={rapidExits}, resetJumps={resetCrossesIgnored}, rollingRejects={entriesWithoutRollingCross}, closeSourceRejects={entriesWithoutCloseVwapCross}");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public Task S0048_PercentStopActuallyFlattensBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(VwapBreakout);

	private const string Vwma = "0049_VWMA";

	[TestMethod]
	[TestCategory("Shard01")]
	[DataRow(14)]
	[DataRow(30)]
	[DataRow(2)]
	public async Task S0049_IndependentRollingCloseVolumeMeanActualCrossesStopAndReverse(int period)
	{
		var window = new Queue<ICandleMessage>();
		var bars = 0;
		var readyBars = 0;
		var nativeValues = 0;
		var expectedMean = 0m;
		decimal? previousClose = null;
		var previousMean = 0m;
		var previousSma = 0m;
		DateTime? previousDate = null;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buys = 0;
		var sells = 0;
		var exits = 0;
		var rapidExits = 0;
		var lastEntryBar = 0;
		var entriesWithoutSmaCross = 0;
		var meansDifferentFromSma = 0;
		var crossingsFromEquality = 0;
		var dayBoundaryCrosses = 0;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var violations = new List<string>();
		await Replay(Vwma, (strategy, secondary) =>
		{
			if (period == 2) strategy.Security = secondary;
			AreEqual(14, strategy.Parameters["VWMAPeriod"].Value);
			SetParam(strategy, "VWMAPeriod", period);
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			IsTrue(strategy.Parameters.TryGetValue("StopLossPercent", out var stop));
			AreEqual(2m, Convert.ToDecimal(stop.Value));
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.Volume = period == 2 ? 3m : 2m;
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				bars++;
				window.Enqueue(candle);
				if (window.Count > period) window.Dequeue();
				if (window.Count < period) return;
				var totalVolume = window.Sum(b => b.TotalVolume);
				if (totalVolume <= 0m)
				{
					previousClose = null;
					return;
				}
				readyBars++;
				expectedMean = window.Sum(b => b.ClosePrice * b.TotalVolume) / totalVolume;
				var sma = window.Average(b => b.ClosePrice);
				if (Math.Abs(expectedMean - sma) > 0.00000001m) meansDifferentFromSma++;
				var up = previousClose is decimal upClose && upClose <= previousMean && candle.ClosePrice > expectedMean;
				var down = previousClose is decimal downClose && downClose >= previousMean && candle.ClosePrice < expectedMean;
				var date = candle.OpenTime.ToUniversalTime().Date;
				if (previousDate is not null && previousDate != date && (up || down)) dayBoundaryCrosses++;
				if (up && strategy.Position <= 0m || down && strategy.Position >= 0m)
				{
					expectedSide = up ? Sides.Buy : Sides.Sell;
					expectedVolume = strategy.Position == 0m ? strategy.Volume : Math.Min(Math.Abs(strategy.Position), strategy.Volume) * 2m;
					if (strategy.Position != 0m)
					{
						exits++;
						if (bars - lastEntryBar < 500) rapidExits++;
					}
					lastEntryBar = bars;
					if (up) buys++; else sells++;
					if (previousClose is decimal prior &&
						!(up ? prior <= previousSma && candle.ClosePrice > sma : prior >= previousSma && candle.ClosePrice < sma)) entriesWithoutSmaCross++;
				}
				if (expectedSide is not null)
				{
					expectedOrders++;
					if (previousClose == previousMean) crossingsFromEquality++;
				}
				previousClose = candle.ClosePrice;
				previousMean = expectedMean;
				previousSma = sma;
				previousDate = date;
			};
			strategy.Indicators.Added += indicator =>
			{
				if (indicator is not VolumeWeightedMovingAverage) return;
				indicator.Changed += (_, output) =>
				{
					if (!output.IsFinal || output.IsEmpty || !indicator.IsFormed) return;
					nativeValues++;
					if (Math.Abs(output.GetValue<decimal>() - expectedMean) > 0.00000001m)
						violations.Add("Every formed native VWMA must use the current-inclusive N-candle Close*TotalVolume / TotalVolume window, without a UTC reset, SMA or typical-price VWAP substitution.");
				};
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add("Every order must follow an actual Close/VWMA cross after warmup in its direction: Volume when flat, a bounded reversal of the opposite position when held. Exit-only flattening, a price-side level or cooldown-delayed action cannot replace it.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		TestContext.WriteLine($"period={period}: bars={bars}, native={nativeValues}, orders={actualOrders}, buys={buys}, sells={sells}, reversals={exits}, rapid={rapidExits}, SMArejects={entriesWithoutSmaCross}, equalityOrigin={crossingsFromEquality}, UTCboundaryCrosses={dayBoundaryCrosses}");
		AreEqual(expectedOrders, actualOrders);
		AreEqual(readyBars, nativeValues);
		IsTrue(buys > 0 && sells > 0 && exits > 0 && rapidExits > 0 && meansDifferentFromSma > 0 && dayBoundaryCrosses > 0);
		if (period > 2) IsTrue(entriesWithoutSmaCross > 0, "Real entries must distinguish volume weighting from a plain Close SMA.");
		if (period == 2) IsTrue(crossingsFromEquality > 0, "Actual TON candles must exercise departures from an equal Close/VWMA, without treating the equality itself as a cross.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard01")]
	public Task S0049_PercentStopActuallyFlattensBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(Vwma);

	private const string AdTrend = "0050_AD";

	[TestMethod]
	[TestCategory("Shard02")]
	[DataRow(20)]
	[DataRow(30)]
	[DataRow(3)]
	public async Task S0050_IndependentNativeMoneyFlowNeutralAdStepsAndFullRemainingExits(int maPeriod)
	{
		var closes = new Queue<decimal>();
		var bars = 0;
		var readyBars = 0;
		var nativeAds = 0;
		var nativeMeans = 0;
		var ad = 0m;
		var mean = 0m;
		decimal? previousAd = null;
		decimal? previousClose = null;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buys = 0;
		var sells = 0;
		var exits = 0;
		var rapidExits = 0;
		var lastEntryBar = 0;
		var entriesOppositeCloseDirection = 0;
		var heldDespiteAdverseMaSide = 0;
		var flatNeutralBelowMa = 0;
		var heldNeutralAd = 0;
		var zeroRangeBars = 0;
		var zeroAdValues = 0;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var violations = new List<string>();
		await Replay(AdTrend, (strategy, secondary) =>
		{
			if (maPeriod == 3) strategy.Security = secondary;
			AreEqual(20, strategy.Parameters["MAPeriod"].Value);
			SetParam(strategy, "MAPeriod", maPeriod);
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			IsTrue(strategy.Parameters.TryGetValue("StopLossPercent", out var stop));
			AreEqual(2m, Convert.ToDecimal(stop.Value));
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				bars++;
				strategy.Volume = (maPeriod == 3 ? 3m : 2m) * (bars % 2 == 0 ? 1m : 2m);
				var priorAd = previousAd;
				var priorClose = previousClose;
				var range = candle.HighPrice - candle.LowPrice;
				if (range != 0m)
					ad += ((candle.ClosePrice - candle.LowPrice) - (candle.HighPrice - candle.ClosePrice)) / range * candle.TotalVolume;
				else zeroRangeBars++;
				if (ad == 0m) zeroAdValues++;
				previousAd = ad;
				previousClose = candle.ClosePrice;
				closes.Enqueue(candle.ClosePrice);
				if (closes.Count > maPeriod) closes.Dequeue();
				if (closes.Count < maPeriod) return;
				readyBars++;
				mean = closes.Average();
				if (priorAd is not decimal oldAd) return;
				var up = ad > oldAd;
				var down = ad < oldAd;
				if (strategy.Position > 0m && down || strategy.Position < 0m && up)
				{
					expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
					expectedVolume = Math.Abs(strategy.Position);
					exits++;
					if (bars - lastEntryBar < 500) rapidExits++;
				}
				else if (strategy.Position == 0m)
				{
					if (up && candle.ClosePrice > mean || down && candle.ClosePrice < mean)
					{
						expectedSide = up ? Sides.Buy : Sides.Sell;
						expectedVolume = strategy.Volume;
						lastEntryBar = bars;
						if (up) buys++; else sells++;
						if (priorClose is decimal old && (up ? candle.ClosePrice <= old : candle.ClosePrice >= old)) entriesOppositeCloseDirection++;
					}
					else if (!up && !down && candle.ClosePrice < mean) flatNeutralBelowMa++;
				}
				else
				{
					if (!up && !down) heldNeutralAd++;
					if (strategy.Position > 0m && candle.ClosePrice < mean || strategy.Position < 0m && candle.ClosePrice > mean) heldDespiteAdverseMaSide++;
				}
				if (expectedSide is not null) expectedOrders++;
			};
			strategy.Indicators.Added += indicator =>
			{
				indicator.Changed += (_, value) =>
				{
					if (!value.IsFinal || !indicator.IsFormed) return;
					if (indicator is AccumulationDistributionLine)
					{
						nativeAds++;
						if (Math.Abs(value.GetValue<decimal>() - ad) > 0.00000001m) violations.Add("Native A/D must cumulatively add intrabar close-location money flow times actual TotalVolume, not OBV or price direction; zero-range candles add zero.");
					}
					else if (indicator is SimpleMovingAverage)
					{
						nativeMeans++;
						if (Math.Abs(value.GetValue<decimal>() - mean) > 0.00000001m) violations.Add("Native SMA must match the current-inclusive Close window from cold startup.");
					}
				};
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add("Every flat entry needs STRICT A/D direction plus the matching price/SMA side. Held exit needs STRICT adverse A/D direction for full remaining Position; unchanged A/D is neutral, not falling, and MA-side changes alone are not exits.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		TestContext.WriteLine($"MA={maPeriod}: bars={bars}, nativeAD={nativeAds}, nativeMA={nativeMeans}, orders={actualOrders}, buys={buys}, sells={sells}, exits={exits}, rapid={rapidExits}, oppositePriceEntries={entriesOppositeCloseDirection}, adverseMaHolds={heldDespiteAdverseMaSide}, flatNeutralBelowMA={flatNeutralBelowMa}, heldNeutralAD={heldNeutralAd}, zeroRangeBars={zeroRangeBars}, zeroAD={zeroAdValues}");
		AreEqual(expectedOrders, actualOrders);
		AreEqual(bars, nativeAds);
		AreEqual(readyBars, nativeMeans);
		IsTrue(buys > 0 && sells > 0 && exits > 0 && rapidExits > 0 && entriesOppositeCloseDirection > 0 && heldDespiteAdverseMaSide > 0);
		if (maPeriod == 3) IsTrue(zeroRangeBars > 0 && flatNeutralBelowMa > 0 && heldNeutralAd > 0,
			"Real TON candles must exercise zero-range/neutral A/D without false shorts or premature exits.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard02")]
	public Task S0050_PercentStopActuallyFlattensBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(AdTrend);

	private const string WeightedPrice = "0051_Volume_Weighted_Price_Breakout";

	[TestMethod]
	[TestCategory("Shard03")]
	[DataRow(20, 20)]
	[DataRow(30, 10)]
	[DataRow(3, 2)]
	public async Task S0051_IndependentWeightedCrossMaConfirmationAndActualMaExit(int maPeriod, int vwmaPeriod)
	{
		var closes = new Queue<decimal>();
		var weighted = new Queue<ICandleMessage>();
		decimal? previousClose = null;
		decimal? previousMa = null;
		decimal? previousVwma = null;
		decimal? expectedMa = null;
		decimal? expectedVwma = null;
		var bars = 0;
		var maReady = 0;
		var vwmaReady = 0;
		var nativeMas = 0;
		var nativeVwmas = 0;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buys = 0;
		var sells = 0;
		var exits = 0;
		var rapidExits = 0;
		var rejectedWeightedCrosses = 0;
		var flatAlignedWithoutCross = 0;
		var equalityOriginCrosses = 0;
		var weightedDifferentFromMa = 0;
		var lastEntryBar = 0;
		var paused = false;
		var pauseStarted = false;
		var pausedBars = 0;
		var resumedAdverseSideWithoutCross = 0;
		var laterActualExits = 0;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var violations = new List<string>();
		await Replay(WeightedPrice, (strategy, secondary) =>
		{
			if (maPeriod == 3) strategy.Security = secondary;
			AreEqual(20, strategy.Parameters["MAPeriod"].Value);
			AreEqual(20, strategy.Parameters["VWAPPeriod"].Value);
			SetParam(strategy, "MAPeriod", maPeriod);
			SetParam(strategy, "VWAPPeriod", vwmaPeriod);
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			IsTrue(strategy.Parameters.TryGetValue("StopLossPercent", out var stop));
			AreEqual(2m, Convert.ToDecimal(stop.Value));
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				bars++;
				strategy.Volume = (maPeriod == 3 ? 3m : 2m) * (bars % 2 == 0 ? 1m : 2m);
				closes.Enqueue(candle.ClosePrice);
				if (closes.Count > maPeriod) closes.Dequeue();
				weighted.Enqueue(candle);
				if (weighted.Count > vwmaPeriod) weighted.Dequeue();
				expectedMa = closes.Count == maPeriod ? closes.Average() : null;
				var totalVolume = weighted.Sum(b => b.TotalVolume);
				expectedVwma = weighted.Count == vwmaPeriod && totalVolume > 0m
					? weighted.Sum(b => b.ClosePrice * b.TotalVolume) / totalVolume : null;
				if (expectedMa is decimal ma) maReady++;
				if (expectedVwma is decimal vwma) vwmaReady++;
				if (expectedMa is decimal m && expectedVwma is decimal v && Math.Abs(m - v) > 0.00000001m)
					weightedDifferentFromMa++;
				var vwmaUp = previousClose is decimal oldUp && previousVwma is decimal oldWeightedUp &&
					oldUp <= oldWeightedUp && expectedVwma is decimal newWeightedUp && candle.ClosePrice > newWeightedUp;
				var vwmaDown = previousClose is decimal oldDown && previousVwma is decimal oldWeightedDown &&
					oldDown >= oldWeightedDown && expectedVwma is decimal newWeightedDown && candle.ClosePrice < newWeightedDown;
				var maUp = previousClose is decimal oldMaUpClose && previousMa is decimal oldMaUp &&
					oldMaUpClose <= oldMaUp && expectedMa is decimal newMaUp && candle.ClosePrice > newMaUp;
				var maDown = previousClose is decimal oldMaDownClose && previousMa is decimal oldMaDown &&
					oldMaDownClose >= oldMaDown && expectedMa is decimal newMaDown && candle.ClosePrice < newMaDown;
				if (strategy.Position == 0m && expectedMa is decimal filter && expectedVwma is decimal weightedMean)
				{
					if (vwmaUp && candle.ClosePrice <= filter || vwmaDown && candle.ClosePrice >= filter) rejectedWeightedCrosses++;
					if (!vwmaUp && !vwmaDown &&
						(candle.ClosePrice > weightedMean && candle.ClosePrice > filter || candle.ClosePrice < weightedMean && candle.ClosePrice < filter))
						flatAlignedWithoutCross++;
				}
				// Pause at a real adverse SMA cross while a position is held. On resume,
				// being on the adverse side alone must not masquerade as a new cross.
				if (!pauseStarted && strategy.Position != 0m &&
					(strategy.Position > 0m && maDown || strategy.Position < 0m && maUp))
				{
					pauseStarted = paused = true;
					strategy.TradingMode = StrategyTradingModes.Disabled;
				}
				if (paused)
				{
					pausedBars++;
					if (pausedBars > 1 && expectedMa is decimal currentMa &&
						(strategy.Position > 0m && candle.ClosePrice < currentMa && !maDown ||
						 strategy.Position < 0m && candle.ClosePrice > currentMa && !maUp))
					{
						paused = false;
						resumedAdverseSideWithoutCross++;
						strategy.TradingMode = StrategyTradingModes.Full;
					}
				}
				if (!paused && strategy.Position != 0m && (strategy.Position > 0m && maDown || strategy.Position < 0m && maUp))
				{
					expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
					expectedVolume = Math.Abs(strategy.Position);
					exits++;
					if (bars - lastEntryBar < 500) rapidExits++;
					if (resumedAdverseSideWithoutCross > 0) laterActualExits++;
				}
				else if (!paused && strategy.Position == 0m && expectedMa is decimal confirmedMa &&
					(vwmaUp && candle.ClosePrice > confirmedMa || vwmaDown && candle.ClosePrice < confirmedMa))
				{
					expectedSide = vwmaUp ? Sides.Buy : Sides.Sell;
					expectedVolume = strategy.Volume;
					lastEntryBar = bars;
					if (vwmaUp) buys++; else sells++;
				}
				if (expectedSide is not null)
				{
					expectedOrders++;
					if (previousClose == previousVwma || previousClose == previousMa) equalityOriginCrosses++;
				}
				previousClose = candle.ClosePrice;
				previousMa = expectedMa;
				previousVwma = expectedVwma;
			};
			strategy.Indicators.Added += indicator =>
			{
				indicator.Changed += (_, value) =>
				{
					if (!value.IsFinal || value.IsEmpty || !indicator.IsFormed) return;
					if (indicator is SimpleMovingAverage)
					{
						nativeMas++;
						if (expectedMa is not decimal m || Math.Abs(value.GetValue<decimal>() - m) > 0.00000001m)
							if (violations.Count < 12) violations.Add("Native SMA must equal the current-inclusive Close/MA-period mean on every formed bar.");
					}
					else if (indicator is VolumeWeightedMovingAverage)
					{
						nativeVwmas++;
						if (expectedVwma is not decimal v || Math.Abs(value.GetValue<decimal>() - v) > 0.00000001m)
							if (violations.Count < 12) violations.Add("Native VWMA must equal current-inclusive Close*TotalVolume / TotalVolume, with independent VWAPPeriod warmup and no daily reset.");
					}
				};
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					if (violations.Count < 12) violations.Add("Every entry needs an actual VWMA crossing AND SMA confirmation. Every held exit needs a fresh adverse SMA cross for FULL remaining Position, without cooldown or same-bar reversal.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		TestContext.WriteLine($"MA={maPeriod} VWMA={vwmaPeriod}: bars={bars}, nativeMA={nativeMas}, nativeVWMA={nativeVwmas}, orders={actualOrders}, buys={buys}, sells={sells}, exits={exits}, rapid={rapidExits}, rejectedCrosses={rejectedWeightedCrosses}, alignedNoCross={flatAlignedWithoutCross}, equalOrigin={equalityOriginCrosses}, pausedBars={pausedBars}, resumeNoCross={resumedAdverseSideWithoutCross}, laterExits={laterActualExits}");
		AreEqual(expectedOrders, actualOrders);
		AreEqual(maReady, nativeMas);
		AreEqual(vwmaReady, nativeVwmas);
		IsTrue(buys > 0 && sells > 0 && exits > 0 && rapidExits > 0 && weightedDifferentFromMa > 0 && flatAlignedWithoutCross > 0);
		IsTrue(pauseStarted && !paused && pausedBars > 1 && resumedAdverseSideWithoutCross > 0 && laterActualExits > 0,
			"Real permission pause must resume on an adverse SMA side without a new cross, then eventually exit on a later actual cross.");
		if (maPeriod != 3) IsTrue(rejectedWeightedCrosses > 0, "Archive must exercise weighted crosses rejected by the price/SMA confirmation filter.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));
	}

	[TestMethod]
	[TestCategory("Shard03")]
	public Task S0051_PercentStopActuallyFlattensBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(WeightedPrice);

	private const string VolumeDivergence = "0052_Volume_Divergence";

	[TestMethod]
	[TestCategory("Shard04")]
	[DataRow(20, 14)]
	[DataRow(30, 7)]
	[DataRow(3, 2)]
	public async Task S0052_IndependentOppositePriceVolumeSignalsAndAnyMaCrossExits(int maPeriod, int atrPeriod)
	{
		var closes = new Queue<decimal>();
		decimal? previousClose = null;
		decimal? previousVolume = null;
		decimal? previousMa = null;
		var mean = 0m;
		var atr = 0m;
		var bars = 0;
		var readyBars = 0;
		var nativeMeans = 0;
		var nativeAtrs = 0;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buys = 0;
		var sells = 0;
		var exits = 0;
		var rapidExits = 0;
		var lastEntryBar = 0;
		var longsClosedOnDownCross = 0;
		var shortsClosedOnUpCross = 0;
		var rejectedWithoutVolumeRise = 0;
		var volumeRisesWithoutPriceDirection = 0;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var violations = new List<string>();
		await Replay(VolumeDivergence, (strategy, secondary) =>
		{
			if (maPeriod == 3) strategy.Security = secondary;
			AreEqual(20, strategy.Parameters["MAPeriod"].Value);
			AreEqual(14, strategy.Parameters["ATRPeriod"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["StopLossATRMultiplier"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "MAPeriod", maPeriod);
			SetParam(strategy, "ATRPeriod", atrPeriod);
			SetParam(strategy, "StopLossATRMultiplier", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				bars++;
				strategy.Volume = (maPeriod == 3 ? 3m : 2m) * (bars % 2 == 0 ? 1m : 2m);
				var close = candle.ClosePrice;
				var volume = candle.TotalVolume;
				var tr = previousClose is decimal oldClose
					? Math.Max(candle.HighPrice - candle.LowPrice, Math.Max(Math.Abs(candle.HighPrice - oldClose), Math.Abs(candle.LowPrice - oldClose)))
					: candle.HighPrice - candle.LowPrice;
				var length = Math.Min(bars, atrPeriod);
				atr = (atr * (length - 1) + tr) / length;
				closes.Enqueue(close);
				if (closes.Count > maPeriod) closes.Dequeue();
				var maReady = closes.Count == maPeriod;
				if (maReady) mean = closes.Average();
				var atrReady = bars >= atrPeriod;
				var risingVolume = previousVolume is decimal oldVolume && volume > oldVolume;
				var priceDown = previousClose is decimal oldDown && close < oldDown;
				var priceUp = previousClose is decimal oldUp && close > oldUp;
				var maUp = previousClose is decimal priorUpClose && previousMa is decimal priorUpMa && maReady &&
					priorUpClose <= priorUpMa && close > mean;
				var maDown = previousClose is decimal priorDownClose && previousMa is decimal priorDownMa && maReady &&
					priorDownClose >= priorDownMa && close < mean;
				if (maReady && atrReady)
				{
					readyBars++;
					if (strategy.Position != 0m && (maUp || maDown))
					{
						expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
						expectedVolume = Math.Abs(strategy.Position);
						exits++;
						if (strategy.Position > 0m && maDown) longsClosedOnDownCross++;
						if (strategy.Position < 0m && maUp) shortsClosedOnUpCross++;
						if (bars - lastEntryBar < 500) rapidExits++;
					}
					else if (strategy.Position == 0m)
					{
						if (risingVolume && (priceDown || priceUp))
						{
							expectedSide = priceDown ? Sides.Buy : Sides.Sell;
							expectedVolume = strategy.Volume;
							lastEntryBar = bars;
							if (priceDown) buys++; else sells++;
						}
						else if (!risingVolume && (priceDown || priceUp)) rejectedWithoutVolumeRise++;
						if (risingVolume && !priceDown && !priceUp) volumeRisesWithoutPriceDirection++;
					}
					if (expectedSide is not null) expectedOrders++;
				}
				previousClose = close;
				previousVolume = volume;
				previousMa = maReady ? mean : null;
			};
			strategy.Indicators.Added += indicator =>
			{
				indicator.Changed += (_, value) =>
				{
					if (!value.IsFinal || value.IsEmpty || !indicator.IsFormed || bars < Math.Max(maPeriod, atrPeriod)) return;
					if (indicator is SimpleMovingAverage)
					{
						nativeMeans++;
						if (Math.Abs(value.GetValue<decimal>() - mean) > 0.00000001m && violations.Count < 12)
							violations.Add("Native SMA must match the current-inclusive Close mean for the selected period.");
					}
					else if (indicator is AverageTrueRange)
					{
						nativeAtrs++;
						if (Math.Abs(value.GetValue<decimal>() - atr) > 0.00000001m && violations.Count < 12)
							violations.Add("Native Wilder ATR must match independently calculated true-range smoothing from the first actual candle.");
					}
				};
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if ((order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market) && violations.Count < 12)
					violations.Add("Flat entry buys a lower close and sells a higher close on rising volume on either side of the SMA; a held position exits on any fresh price/SMA cross, up or down, for the FULL remaining Position, independent of continuing divergence and without cooldown.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		TestContext.WriteLine($"MA={maPeriod} ATR={atrPeriod}: bars={bars}, nativeMA={nativeMeans}, nativeATR={nativeAtrs}, orders={actualOrders}, buys={buys}, sells={sells}, exits={exits}, rapid={rapidExits}, longsOnDownCross={longsClosedOnDownCross}, shortsOnUpCross={shortsClosedOnUpCross}, volumeRejects={rejectedWithoutVolumeRise}, flatPriceWithVolRise={volumeRisesWithoutPriceDirection}");
		AreEqual(expectedOrders, actualOrders);
		AreEqual(readyBars, nativeMeans);
		AreEqual(readyBars, nativeAtrs);
		IsTrue(buys > 0 && sells > 0 && exits > 0 && rapidExits > 0 && longsClosedOnDownCross > 0 && shortsClosedOnUpCross > 0 && rejectedWithoutVolumeRise > 0);
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));
	}

	[TestMethod]
	[TestCategory("Shard04")]
	public Task S0052_FrozenActualFillAtrStopAndRiskParameterChangeRealExecutions()
		=> CheckAtrDistancesAndRiskParameterChangeRealExecutions(VolumeDivergence,
			"Volume divergence entry", stopParameter: "StopLossATRMultiplier", atrPeriodParameter: "ATRPeriod");

	private const string VolumeMaCross = "0053_Volume_MA_Cross";

	[TestMethod]
	[TestCategory("Shard05")]
	[DataRow(10, 50, 20)]
	[DataRow(4, 12, 20)]
	[DataRow(2, 3, 3)]
	public async Task S0053_IndependentTotalVolumeMaCrossPriceFilterAndFullReverseExits(int fastPeriod, int slowPeriod, int pricePeriod)
	{
		var prices = new Queue<decimal>();
		var fastVolumes = new Queue<decimal>();
		var slowVolumes = new Queue<decimal>();
		decimal? previousFast = null;
		decimal? previousSlow = null;
		var priceMean = 0m;
		var fastMean = 0m;
		var slowMean = 0m;
		var bars = 0;
		var readyBars = 0;
		var nativePrice = 0;
		var nativeFast = 0;
		var nativeSlow = 0;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buys = 0;
		var sells = 0;
		var exits = 0;
		var rapidExits = 0;
		var lastEntryBar = 0;
		var rejectedVolumeCrosses = 0;
		var flatCorrectSideWithoutVolumeCross = 0;
		var heldAcrossPriceMaCross = 0;
		var crossesFromVolumeEquality = 0;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var violations = new List<string>();
		await Replay(VolumeMaCross, (strategy, secondary) =>
		{
			if (slowPeriod == 3) strategy.Security = secondary;
			AreEqual(10, strategy.Parameters["FastVolumeMALength"].Value);
			AreEqual(50, strategy.Parameters["SlowVolumeMALength"].Value);
			AreEqual(20, strategy.Parameters["PriceMaPeriod"].Value);
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			IsTrue(strategy.Parameters.TryGetValue("StopLossPercent", out var stop));
			AreEqual(2m, Convert.ToDecimal(stop.Value));
			SetParam(strategy, "FastVolumeMALength", fastPeriod);
			SetParam(strategy, "SlowVolumeMALength", slowPeriod);
			SetParam(strategy, "PriceMaPeriod", pricePeriod);
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				bars++;
				strategy.Volume = (slowPeriod == 3 ? 3m : 2m) * (bars % 2 == 0 ? 1m : 2m);
				prices.Enqueue(candle.ClosePrice);
				if (prices.Count > pricePeriod) prices.Dequeue();
				fastVolumes.Enqueue(candle.TotalVolume);
				if (fastVolumes.Count > fastPeriod) fastVolumes.Dequeue();
				slowVolumes.Enqueue(candle.TotalVolume);
				if (slowVolumes.Count > slowPeriod) slowVolumes.Dequeue();
				var priceReady = prices.Count == pricePeriod;
				var fastReady = fastVolumes.Count == fastPeriod;
				var slowReady = slowVolumes.Count == slowPeriod;
				if (priceReady) priceMean = prices.Average();
				if (fastReady) fastMean = fastVolumes.Average();
				if (slowReady) slowMean = slowVolumes.Average();
				var up = previousFast is decimal oldFastUp && previousSlow is decimal oldSlowUp &&
					fastReady && slowReady && oldFastUp <= oldSlowUp && fastMean > slowMean;
				var down = previousFast is decimal oldFastDown && previousSlow is decimal oldSlowDown &&
					fastReady && slowReady && oldFastDown >= oldSlowDown && fastMean < slowMean;
				if (priceReady && fastReady && slowReady)
				{
					readyBars++;
					if (strategy.Position > 0m && down || strategy.Position < 0m && up)
					{
						expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
						expectedVolume = Math.Abs(strategy.Position);
						exits++;
						if (bars - lastEntryBar < 500) rapidExits++;
					}
					else if (strategy.Position == 0m)
					{
						if (up && candle.ClosePrice > priceMean || down && candle.ClosePrice < priceMean)
						{
							expectedSide = up ? Sides.Buy : Sides.Sell;
							expectedVolume = strategy.Volume;
							lastEntryBar = bars;
							if (up) buys++; else sells++;
						}
						else if (up || down) rejectedVolumeCrosses++;
						else if (candle.ClosePrice > priceMean && fastMean > slowMean ||
							candle.ClosePrice < priceMean && fastMean < slowMean) flatCorrectSideWithoutVolumeCross++;
					}
					else if (strategy.Position > 0m && candle.ClosePrice < priceMean ||
						strategy.Position < 0m && candle.ClosePrice > priceMean) heldAcrossPriceMaCross++;
					if (expectedSide is not null)
					{
						expectedOrders++;
						if (previousFast == previousSlow) crossesFromVolumeEquality++;
					}
				}
				previousFast = fastReady ? fastMean : null;
				previousSlow = slowReady ? slowMean : null;
			};
			strategy.Indicators.Added += indicator =>
			{
				indicator.Changed += (_, value) =>
				{
					if (!value.IsFinal || value.IsEmpty || !indicator.IsFormed || bars < Math.Max(pricePeriod, slowPeriod) || indicator is not SimpleMovingAverage) return;
					var expected = indicator.Name switch
					{
						"Price SMA" => priceMean,
						"Fast volume SMA" => fastMean,
						"Slow volume SMA" => slowMean,
						_ => decimal.MinValue,
					};
					if (indicator.Name == "Price SMA") nativePrice++;
					else if (indicator.Name == "Fast volume SMA") nativeFast++;
					else if (indicator.Name == "Slow volume SMA") nativeSlow++;
					if (expected == decimal.MinValue || Math.Abs(value.GetValue<decimal>() - expected) > 0.00000001m)
						if (violations.Count < 12) violations.Add("Each native SMA must match the independent current-inclusive Close or TotalVolume window, identified by its source and length.");
				};
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if ((order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market) && violations.Count < 12)
					violations.Add("Flat entry needs an actual fast/slow TotalVolume SMA cross plus price/SMA confirmation; held exit needs the opposite volume cross for FULL remaining Position, not a price/SMA cross, current MA side or cooldown.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		TestContext.WriteLine($"fast={fastPeriod} slow={slowPeriod} price={pricePeriod}: bars={bars}, nativePrice={nativePrice}, nativeFast={nativeFast}, nativeSlow={nativeSlow}, orders={actualOrders}, buys={buys}, sells={sells}, exits={exits}, rapid={rapidExits}, rejectedCrosses={rejectedVolumeCrosses}, alignedNoCross={flatCorrectSideWithoutVolumeCross}, heldAdversePriceSide={heldAcrossPriceMaCross}, equalityOrigin={crossesFromVolumeEquality}");
		AreEqual(expectedOrders, actualOrders);
		AreEqual(readyBars, nativePrice);
		AreEqual(readyBars, nativeFast);
		AreEqual(readyBars, nativeSlow);
		IsTrue(buys > 0 && sells > 0 && exits > 0 && rapidExits > 0 && rejectedVolumeCrosses > 0 && flatCorrectSideWithoutVolumeCross > 0 && heldAcrossPriceMaCross > 0);
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public Task S0053_PercentStopActuallyFlattensBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(VolumeMaCross);

	private const string CumulativeDelta = "0054_Cumulative_Delta_Breakout";
	private static readonly long _deltaFrameTicks = TimeSpan.FromMinutes(5).Ticks;
	private static readonly TimeSpan _deltaWindow = TimeSpan.FromDays(7);

	private sealed class CumulativeDeltaRun
	{
		public int ArchivedSlots { get; init; }
		public int Bars { get; set; }
		public int UpBreaks { get; set; }
		public int DownBreaks { get; set; }
		public int ZeroCrosses { get; set; }
		public int ProxyDisagreements { get; set; }
		public int ExpectedOrders { get; set; }
		public int ActualOrders { get; set; }
		public int Buys { get; set; }
		public int Sells { get; set; }
		public int Exits { get; set; }
		public int HeldMidnights { get; set; }
		public int UnsignedBars { get; set; }
		public int SignalsAfterUnsignedBar { get; set; }
		public List<string> Violations { get; } = [];

		public void AddViolation(string violation)
		{
			if (Violations.Count < 12)
				Violations.Add(violation);
		}
	}

	[TestMethod]
	[TestCategory("Shard06")]
	[DataRow(false, 20, true)]
	[DataRow(true, 5, false)]
	public async Task S0054_ArchivedTradeSidesDriveEveryBreakoutAndZeroExit(bool secondarySecurity, int lookback, bool zeroExitInWindow)
	{
		var run = await ReplayCumulativeDelta(secondarySecurity, lookback, unsignedDayOpen: false);
		IsTrue(run.ArchivedSlots > 1000 && run.Bars >= run.ArchivedSlots - 1 && run.UpBreaks > 0 && run.DownBreaks > 0 && run.ProxyDisagreements > 0);
		AreEqual(run.ExpectedOrders, run.ActualOrders);
		IsTrue(run.Buys + run.Sells > 0 && run.HeldMidnights > 0,
			"The real archive must hold a breakout position across UTC midnight, where the running total carries on without any daily reset.");
		// On the secondary instrument the running total never comes back through zero, so only the stop could close a trade there.
		if (zeroExitInWindow)
			IsTrue(run.ZeroCrosses > 0 && run.Exits > 0, "The real archive must exercise an actual held-position zero exit.");
		IsTrue(run.Violations.Count == 0, string.Join(Environment.NewLine, run.Violations));
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S0054_UnsignedBarAddsNoDeltaAndNeverHaltsSignals()
	{
		var run = await ReplayCumulativeDelta(secondarySecurity: false, lookback: 20, unsignedDayOpen: true);
		IsTrue(run.Bars >= run.ArchivedSlots - 1 && run.UnsignedBars >= 6);
		AreEqual(run.ExpectedOrders, run.ActualOrders);
		IsTrue(run.SignalsAfterUnsignedBar > 0 && run.Exits > 0,
			"Breakouts and zero exits must still act on a UTC day that opened with a bar carrying no aggressor sides.");
		IsTrue(run.Violations.Count == 0, string.Join(Environment.NewLine, run.Violations));
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public Task S0054_PercentStopFlattensFromRealBidAskBetweenSignalBars()
		=> CheckPercentStopBetweenBars(CumulativeDelta);

	private async Task<SortedDictionary<long, (decimal Buy, decimal Sell)>> LoadCumulativeDeltaSlots(string securityId)
	{
		var raw = new SortedDictionary<long, (decimal Buy, decimal Sell)>();
		using var registry = new StorageRegistry { DefaultDrive = new LocalMarketDataDrive(Paths.FileSystem, Paths.HistoryDataPath) };
		var storage = registry.GetStorage(securityId.ToSecurityId(), DataType.Ticks);
		var dates = await storage.GetDatesAsync().ToArrayAsync(CancellationToken);
		foreach (var date in dates.Where(date => date >= Paths.HistoryBeginDate.Date && date < Paths.HistoryBeginDate.Date.Add(_deltaWindow)))
			await foreach (var message in storage.LoadAsync(date).WithCancellation(CancellationToken))
			{
				var tick = (ExecutionMessage)message;
				var slot = tick.ServerTime.ToUniversalTime().Ticks / _deltaFrameTicks;
				raw.TryGetValue(slot, out var signed);
				if (tick.OriginSide == Sides.Buy) signed.Buy += tick.TradeVolume.Value;
				else if (tick.OriginSide == Sides.Sell) signed.Sell += tick.TradeVolume.Value;
				else throw new InvalidOperationException("An archived tick has no real aggressor side.");
				raw[slot] = signed;
			}
		return raw;
	}

	private async Task<CumulativeDeltaRun> ReplayCumulativeDelta(bool secondarySecurity, int lookback, bool unsignedDayOpen)
	{
		var securityId = secondarySecurity ? Paths.HistoryDefaultSecurity2 : Paths.HistoryDefaultSecurity;
		var raw = await LoadCumulativeDeltaSlots(securityId);
		var slots = raw.Keys.ToArray();
		var dayOpens = slots.GroupBy(slot => slot * _deltaFrameTicks / TimeSpan.TicksPerDay).Select(day => day.Min()).ToHashSet();
		var run = new CumulativeDeltaRun { ArchivedSlots = raw.Count };
		// One running total from the strategy's first bar: never reset by date, never suspended.
		var priorValues = new Queue<decimal>();
		var cumulative = 0m;
		decimal? previous = null;
		int? nextIndex = null;
		DateTime? lastDay = null;
		DateTime? unsignedDay = null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		await Replay(CumulativeDelta, (strategy, secondary) =>
		{
			if (secondarySecurity) strategy.Security = secondary;
			AreEqual(20, strategy.Parameters["LookbackPeriod"].Value);
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
			SetParam(strategy, "LookbackPeriod", lookback);
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.Volume = 2m;
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished || candle.SecurityId != strategy.Security.Id.ToSecurityId()) return;
				run.Bars++;
				expectedSide = null;
				var openTime = candle.OpenTime.ToUniversalTime();
				var slot = openTime.Ticks / _deltaFrameTicks;
				var index = Array.BinarySearch(slots, slot);
				if (index < 0)
				{
					run.AddViolation("A finished signal candle had no matching archived signed-trade slot.");
					return;
				}
				if (nextIndex is int next && index != next)
					run.AddViolation("Every archived signed-trade slot must reach the strategy exactly once and in order.");
				nextIndex = index + 1;
				var signed = raw[slot];
				if ((candle.BuyVolume ?? 0m) != signed.Buy || (candle.SellVolume ?? 0m) != signed.Sell ||
					candle.TotalVolume != signed.Buy + signed.Sell)
					run.AddViolation("Trade-built candle BuyVolume/SellVolume/TotalVolume must exactly equal raw archived aggressor-side ticks.");
				var estimated = candle.ClosePrice >= candle.OpenPrice ? candle.TotalVolume : -candle.TotalVolume;
				if (estimated != signed.Buy - signed.Sell) run.ProxyDisagreements++;
				if (unsignedDayOpen && dayOpens.Contains(slot))
				{
					// The strategy reads this same candle next: it keeps its volume but loses the aggressor sides.
					candle.BuyVolume = null;
					candle.SellVolume = null;
					signed = (0m, 0m);
					unsignedDay = openTime.Date;
					run.UnsignedBars++;
				}
				cumulative += signed.Buy - signed.Sell;
				var full = priorValues.Count == lookback;
				var up = full && cumulative > priorValues.Max();
				var down = full && cumulative < priorValues.Min();
				var zeroUp = previous is decimal priorUp && priorUp <= 0m && cumulative > 0m;
				var zeroDown = previous is decimal priorDown && priorDown >= 0m && cumulative < 0m;
				if (up) run.UpBreaks++;
				if (down) run.DownBreaks++;
				if (zeroUp || zeroDown) run.ZeroCrosses++;
				if (full) priorValues.Dequeue();
				priorValues.Enqueue(cumulative);
				previous = cumulative;
				if (lastDay is DateTime priorDay && priorDay != openTime.Date && strategy.Position != 0m) run.HeldMidnights++;
				lastDay = openTime.Date;
				if (strategy.Position > 0m && zeroDown || strategy.Position < 0m && zeroUp)
				{
					expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
					expectedVolume = Math.Abs(strategy.Position);
					run.Exits++;
				}
				else if (strategy.Position == 0m)
				{
					if (up) expectedSide = Sides.Buy;
					else if (down) expectedSide = Sides.Sell;
					if (expectedSide != null)
					{
						expectedVolume = strategy.Volume;
						if (expectedSide == Sides.Buy) run.Buys++; else run.Sells++;
					}
				}
				if (expectedSide == null) return;
				run.ExpectedOrders++;
				if (unsignedDay == openTime.Date) run.SignalsAfterUnsignedBar++;
			};
			strategy.OrderRegistering += order =>
			{
				run.ActualOrders++;
				if (order.Type != OrderTypes.Market || order.Side != expectedSide || order.Volume != expectedVolume)
					run.AddViolation("Every order must follow the independent running-total CVD breakout over prior values or its zero crossing, with FULL remaining exit and no cooldown.");
				expectedSide = null;
			};
		}, _deltaWindow);
		TestContext.WriteLine($"{securityId} lookback={lookback} python={IsPython} unsignedDayOpen={unsignedDayOpen}: raw5m={run.ArchivedSlots}, bars={run.Bars}, up={run.UpBreaks}, down={run.DownBreaks}, zero={run.ZeroCrosses}, proxyDisagreements={run.ProxyDisagreements}, expectedOrders={run.ExpectedOrders}, actualOrders={run.ActualOrders}, buys={run.Buys}, sells={run.Sells}, exits={run.Exits}, heldMidnights={run.HeldMidnights}, unsignedBars={run.UnsignedBars}, signalsAfterUnsigned={run.SignalsAfterUnsignedBar}");
		return run;
	}

	private const string VolumeSurge = "0055_Volume_Surge";

	[TestMethod]
	[TestCategory("Shard06")]
	[DataRow(20, 20, 2.0)]
	[DataRow(10, 30, 1.5)]
	[DataRow(2, 2, 1.0)]
	public async Task S0055_IndependentCurrentVolumeMeanSurgeAndBelowMeanFullExits(int maPeriod, int volumePeriod, double multiplier)
	{
		var closes = new Queue<decimal>();
		var volumes = new Queue<decimal>();
		var bars = 0;
		var formedBars = 0;
		var nativePriceMeans = 0;
		var nativeVolumeMeans = 0;
		var mean = 0m;
		var volumeMean = 0m;
		decimal? previousVolume = null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buys = 0;
		var sells = 0;
		var exits = 0;
		var rollingSurgeRejectedByPreviousRatio = 0;
		var fallingVolumeAboveMeanHeld = 0;
		var equalityBars = 0;
		var violations = new List<string>();
		await Replay(VolumeSurge, (strategy, secondary) =>
		{
			if (volumePeriod == 2) strategy.Security = secondary;
			AreEqual(20, strategy.Parameters["MAPeriod"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["VolumeSurgeMultiplier"].Value));
			IsTrue(strategy.Parameters.TryGetValue("VolumeAvgPeriod", out var period), "The promised rolling volume mean cannot be replaced with the previous candle's volume.");
			AreEqual(20, period.Value);
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			IsTrue(strategy.Parameters.TryGetValue("StopLossPercent", out var stop));
			AreEqual(2m, Convert.ToDecimal(stop.Value));
			SetParam(strategy, "MAPeriod", maPeriod);
			SetParam(strategy, "VolumeAvgPeriod", volumePeriod);
			SetParam(strategy, "VolumeSurgeMultiplier", Convert.ToDecimal(multiplier));
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				bars++;
				var priorVolume = previousVolume;
				previousVolume = candle.TotalVolume;
				closes.Enqueue(candle.ClosePrice);
				if (closes.Count > maPeriod) closes.Dequeue();
				volumes.Enqueue(candle.TotalVolume);
				if (volumes.Count > volumePeriod) volumes.Dequeue();
				if (bars < Math.Max(maPeriod, volumePeriod)) return;
				formedBars++;
				mean = closes.Average();
				volumeMean = volumes.Average();
				if (candle.TotalVolume == volumeMean) equalityBars++;
				if (strategy.Position != 0m && candle.TotalVolume < volumeMean)
				{
					expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
					expectedVolume = Math.Abs(strategy.Position);
					exits++;
				}
				else if (strategy.Position == 0m && volumeMean > 0m && candle.TotalVolume > volumeMean * Convert.ToDecimal(multiplier) && candle.ClosePrice != mean)
				{
					expectedSide = candle.ClosePrice > mean ? Sides.Buy : Sides.Sell;
					expectedVolume = strategy.Volume;
					if (expectedSide == Sides.Buy) buys++; else sells++;
					if (priorVolume is decimal prior && candle.TotalVolume < prior * Convert.ToDecimal(multiplier)) rollingSurgeRejectedByPreviousRatio++;
				}
				else if (strategy.Position != 0m && priorVolume is decimal prior && candle.TotalVolume < prior) fallingVolumeAboveMeanHeld++;
				if (expectedSide is not null) expectedOrders++;
			};
			strategy.Indicators.Added += indicator =>
			{
				indicator.Changed += (_, value) =>
				{
					if (!value.IsFinal || !indicator.IsFormed || bars < Math.Max(maPeriod, volumePeriod) || indicator is not SimpleMovingAverage) return;
					var isVolume = indicator.Name == "Volume average";
					if (isVolume) nativeVolumeMeans++; else nativePriceMeans++;
					if (Math.Abs(value.GetValue<decimal>() - (isVolume ? volumeMean : mean)) > 0.00000001m)
						violations.Add("Native price and TotalVolume SMAs must match independent current-inclusive rolling windows at every fully ready bar.");
				};
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add("Every flat entry must strictly exceed rolling mean Volume times multiplier and follow Close/SMA direction. Every held exit must fully close below the volume mean, not on a one-bar decline, price/SMA exit or cooldown.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(7));
		AreEqual(expectedOrders, actualOrders);
		AreEqual(formedBars, nativePriceMeans);
		AreEqual(formedBars, nativeVolumeMeans);
		IsTrue(buys > 0 && sells > 0 && exits > 0);
		if (volumePeriod >= 20) IsTrue(rollingSurgeRejectedByPreviousRatio > 0 && fallingVolumeAboveMeanHeld > 0,
			"Real archive decisions must distinguish a rolling-mean spike from the previous-volume ratio, and an above-mean decline from a true below-mean exit.");
		TestContext.WriteLine($"MA={maPeriod}, volume={volumePeriod}, multiplier={multiplier}: orders={actualOrders}, buys={buys}, sells={sells}, exits={exits}, spikeVsPrevious={rollingSurgeRejectedByPreviousRatio}, aboveMeanDeclinesHeld={fallingVolumeAboveMeanHeld}, volumeEqualsMean={equalityBars}");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public Task S0055_PercentStopActuallyFlattensBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(VolumeSurge);

	private const string DoubleBottom = "0056_Double_Bottom";

	[TestMethod]
	[TestCategory("Shard00")]
	[DataRow(false, 5, 2.0, 1.0)]
	[DataRow(true, 3, 3.0, 0.0)]
	public async Task S0056_ConfirmedLowsBullishLongOnlyAndPatternLowStop(bool secondarySecurity,
		int distance, double similarity, double stopBuffer)
	{
		ICandleMessage older = null;
		ICandleMessage middle = null;
		(int Bar, decimal Low)? lastPivot = null;
		decimal? candidateLow = null;
		decimal? activeStop = null;
		decimal? lastBid = null;
		decimal? lastFinishedLow = null;
		var candidateExpires = 0;
		var bars = 0;
		var pivots = 0;
		var tooClose = 0;
		var dissimilar = 0;
		var entries = 0;
		var exits = 0;
		var quoteExits = 0;
		var barExits = 0;
		var expectedEntry = false;
		var violations = new List<string>();
		await Replay(DoubleBottom, (strategy, secondary) =>
		{
			if (secondarySecurity) strategy.Security = secondary;
			AreEqual(5, strategy.Parameters["Distance"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["SimilarityPercent"].Value));
			AreEqual(1m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
			AreEqual(TimeSpan.FromMinutes(15).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "Distance", distance);
			SetParam(strategy, "SimilarityPercent", Convert.ToDecimal(similarity));
			SetParam(strategy, "StopLossPercent", Convert.ToDecimal(stopBuffer));
			strategy.Volume = 2m;
			strategy.Level1Received += (_, message) =>
			{
				if (message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid && bid > 0m)
					lastBid = bid;
			};
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished || candle.SecurityId != strategy.Security.Id.ToSecurityId()) return;
				bars++;
				expectedEntry = false;
				lastFinishedLow = candle.LowPrice;
				var left = older;
				var center = middle;
				older = center;
				middle = candle;
				if (strategy.Position > 0m) return;
				if (left is not null && center is not null && center.LowPrice < left.LowPrice && center.LowPrice <= candle.LowPrice)
				{
					pivots++;
					var pivot = (Bar: bars - 1, Low: center.LowPrice);
					if (lastPivot is { } first)
					{
						if (pivot.Bar - first.Bar < distance) tooClose++;
						else if (Math.Abs(pivot.Low - first.Low) * 100m > first.Low * Convert.ToDecimal(similarity)) dissimilar++;
						else
						{
							candidateLow = Math.Min(first.Low, pivot.Low);
							candidateExpires = bars + distance;
						}
					}
					lastPivot = pivot;
				}
				if (candidateLow is not decimal low) return;
				if (bars > candidateExpires || candle.LowPrice < low)
				{
					candidateLow = null;
					return;
				}
				if (candle.ClosePrice <= candle.OpenPrice) return;
				expectedEntry = true;
				activeStop = low * (1m - Convert.ToDecimal(stopBuffer) / 100m);
				candidateLow = null;
				lastPivot = null;
				lastBid = null;
				lastFinishedLow = null;
			};
			strategy.OrderRegistering += order =>
			{
				if (order.Side == Sides.Buy)
				{
					entries++;
					if (!expectedEntry || strategy.Position != 0m || order.Volume != strategy.Volume || order.Type != OrderTypes.Market)
						if (violations.Count < 12) violations.Add("Buy requires two causal pivot lows at least Distance bars apart, within SimilarityPercent, followed by a bullish finished candle.");
					expectedEntry = false;
				}
				else
				{
					exits++;
					var byQuote = lastBid is decimal bid && activeStop is decimal stop && bid <= stop;
					var byBar = lastFinishedLow is decimal low && activeStop is decimal stopLow && low <= stopLow;
					if (byQuote) quoteExits++;
					if (byBar) barExits++;
					if (strategy.Position <= 0m || order.Volume != strategy.Position || order.Type != OrderTypes.Market || !byQuote && !byBar)
						if (violations.Count < 12) violations.Add("Sell must close the full long at executable bid or a finished bar low below the pattern-low stop, never open a short.");
					older = middle = null;
					lastPivot = null;
					candidateLow = null;
				}
			};
		}, TimeSpan.FromDays(7));
		TestContext.WriteLine($"python={IsPython}, secondary={secondarySecurity}, distance={distance}, similarity={similarity}, stopBuffer={stopBuffer}: bars={bars}, pivots={pivots}, tooClose={tooClose}, dissimilar={dissimilar}, entries={entries}, exits={exits}, quoteExits={quoteExits}, barExits={barExits}");
		IsTrue(bars > 300 && pivots > 0 && tooClose > 0 && dissimilar > 0);
		IsTrue(entries > 0 && exits > 0, "Real archive must exercise confirmed longs and pattern-low stop exits.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));
	}

	private const string DoubleTop = "0057_Double_Top";

	[TestMethod]
	[TestCategory("Shard00")]
	[DataRow(false, 5, 2.0, 1.0)]
	[DataRow(true, 3, 3.0, 0.0)]
	public async Task S0057_ConfirmedHighsBearishShortOnlyAndPatternHighStop(bool secondarySecurity,
		int distance, double similarity, double stopBuffer)
	{
		ICandleMessage older = null;
		ICandleMessage middle = null;
		(int Bar, decimal High)? lastPivot = null;
		decimal? candidateHigh = null;
		decimal? activeStop = null;
		decimal? lastAsk = null;
		decimal? lastFinishedHigh = null;
		var candidateExpires = 0;
		var bars = 0;
		var pivots = 0;
		var tooClose = 0;
		var dissimilar = 0;
		var entries = 0;
		var exits = 0;
		var quoteExits = 0;
		var barExits = 0;
		var expectedEntry = false;
		var violations = new List<string>();
		await Replay(DoubleTop, (strategy, secondary) =>
		{
			if (secondarySecurity) strategy.Security = secondary;
			AreEqual(5, strategy.Parameters["Distance"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["SimilarityPercent"].Value));
			AreEqual(1m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
			AreEqual(TimeSpan.FromMinutes(15).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "Distance", distance);
			SetParam(strategy, "SimilarityPercent", Convert.ToDecimal(similarity));
			SetParam(strategy, "StopLossPercent", Convert.ToDecimal(stopBuffer));
			strategy.Volume = 2m;
			strategy.Level1Received += (_, message) =>
			{
				if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask && ask > 0m)
					lastAsk = ask;
			};
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished || candle.SecurityId != strategy.Security.Id.ToSecurityId()) return;
				bars++;
				expectedEntry = false;
				lastFinishedHigh = candle.HighPrice;
				var left = older;
				var center = middle;
				older = center;
				middle = candle;
				if (strategy.Position < 0m) return;
				if (left is not null && center is not null && center.HighPrice > left.HighPrice && center.HighPrice >= candle.HighPrice)
				{
					pivots++;
					var pivot = (Bar: bars - 1, High: center.HighPrice);
					if (lastPivot is { } first)
					{
						if (pivot.Bar - first.Bar < distance) tooClose++;
						else if (Math.Abs(pivot.High - first.High) * 100m > first.High * Convert.ToDecimal(similarity)) dissimilar++;
						else
						{
							candidateHigh = Math.Max(first.High, pivot.High);
							candidateExpires = bars + distance;
						}
					}
					lastPivot = pivot;
				}
				if (candidateHigh is not decimal high) return;
				if (bars > candidateExpires || candle.HighPrice > high)
				{
					candidateHigh = null;
					return;
				}
				if (candle.ClosePrice >= candle.OpenPrice) return;
				expectedEntry = true;
				activeStop = high * (1m + Convert.ToDecimal(stopBuffer) / 100m);
				candidateHigh = null;
				lastPivot = null;
				lastAsk = null;
				lastFinishedHigh = null;
			};
			strategy.OrderRegistering += order =>
			{
				if (order.Side == Sides.Sell)
				{
					entries++;
					if (!expectedEntry || strategy.Position != 0m || order.Volume != strategy.Volume || order.Type != OrderTypes.Market)
						if (violations.Count < 12) violations.Add("Sell requires two causal pivot highs at least Distance bars apart, within SimilarityPercent, followed by a bearish finished candle.");
					expectedEntry = false;
				}
				else
				{
					exits++;
					var byQuote = lastAsk is decimal ask && activeStop is decimal stop && ask >= stop;
					var byBar = lastFinishedHigh is decimal high && activeStop is decimal stopHigh && high >= stopHigh;
					if (byQuote) quoteExits++;
					if (byBar) barExits++;
					if (strategy.Position >= 0m || order.Volume != Math.Abs(strategy.Position) || order.Type != OrderTypes.Market || !byQuote && !byBar)
						if (violations.Count < 12) violations.Add("Buy must close the full short at executable ask or a finished bar high above the pattern-high stop, never open a long.");
					older = middle = null;
					lastPivot = null;
					candidateHigh = null;
				}
			};
		}, TimeSpan.FromDays(7));
		TestContext.WriteLine($"python={IsPython}, secondary={secondarySecurity}, distance={distance}, similarity={similarity}, stopBuffer={stopBuffer}: bars={bars}, pivots={pivots}, tooClose={tooClose}, dissimilar={dissimilar}, entries={entries}, exits={exits}, quoteExits={quoteExits}, barExits={barExits}");
		IsTrue(bars > 300 && pivots > 0 && tooClose > 0);
		if (!secondarySecurity)
			IsTrue(dissimilar > 0, "The BTC archive must reject at least one dissimilar pair of tops.");
		IsTrue(entries > 0 && exits > 0, "Real archive must exercise confirmed shorts and pattern-high stop exits.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));
	}

	private const string RsiOverboughtOversold = "0058_RSI_Overbought_Oversold";

	private sealed class RsiZoneReplay
	{
		public int ExpectedOrders { get; set; }
		public int SignalOrders { get; set; }
		public int BuyEntries { get; set; }
		public int SellEntries { get; set; }
		public int NeutralExits { get; set; }
		public int ZeroReadings { get; set; }
		public int PendingBars { get; set; }
		public int StopExits { get; set; }
		public int LongReentriesAfterStop { get; set; }
		public int ShortReentriesAfterStop { get; set; }
		public List<string> Violations { get; } = [];
	}

	[TestMethod]
	[TestCategory("Shard02")]
	[DataRow(14, 30, 70, 50)]
	[DataRow(7, 35, 65, 45)]
	[DataRow(1, 30, 70, 50)]
	public async Task S0058_ExtremeZoneEntriesAndIndependentNeutralExits(int period, int oversold, int overbought, int exitLevel)
	{
		var replay = await ReplayRsiZones(period, oversold, overbought, exitLevel, 0m);
		AreEqual(replay.ExpectedOrders, replay.SignalOrders, "Every finished bar with RSI below OversoldLevel, above OverboughtLevel or back at NeutralLevel must act, starting from the first formed RSI.");
		AreEqual(0, replay.StopExits, "With StopLossPercent at zero only the RSI rules may place orders.");
		IsTrue(replay.BuyEntries > 0 && replay.SellEntries > 0);
		if (period == 1) IsTrue(replay.ZeroReadings > 0, "A zero RSI is valid and must actually participate in entry/exit decisions.");
		else IsTrue(replay.NeutralExits > 0);
		IsTrue(replay.Violations.Count == 0, string.Join(Environment.NewLine, replay.Violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard02")]
	public async Task S0058_StoppedPositionReentersWhileRsiStaysExtreme()
	{
		var replay = await ReplayRsiZones(14, 30, 70, 50, 0.00001m);
		AreEqual(replay.ExpectedOrders, replay.SignalOrders, "A flat strategy must enter on every finished bar with RSI below OversoldLevel or above OverboughtLevel, not only on a fresh crossing.");
		IsTrue(replay.StopExits > 0, "The tight percent stop must actually flatten positions between finished bars.");
		IsTrue(replay.LongReentriesAfterStop > 0 && replay.ShortReentriesAfterStop > 0,
			"After the stop closes a position while RSI is still beyond the level, the next finished bar in the zone must enter again.");
		IsTrue(replay.Violations.Count == 0, string.Join(Environment.NewLine, replay.Violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard02")]
	public Task S0058_PercentStopWorksBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(RsiOverboughtOversold);

	private async Task<RsiZoneReplay> ReplayRsiZones(int period, int oversold, int overbought, int exitLevel, decimal stopPercent)
	{
		var rsi = new RsiOracle(period);
		var replay = new RsiZoneReplay();
		decimal? previous = null;
		Order last = null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedReentry = false;
		var flatAfterStop = false;
		await Replay(RsiOverboughtOversold, (strategy, _) =>
		{
			AreEqual(14, strategy.Parameters["RsiPeriod"].Value);
			SetParam(strategy, "RsiPeriod", period);
			foreach (var (name, defaultValue, value) in new[] { ("OversoldLevel", 30m, (decimal)oversold), ("OverboughtLevel", 70m, (decimal)overbought), ("NeutralLevel", 50m, (decimal)exitLevel) })
			{
				IsTrue(strategy.Parameters.TryGetValue(name, out var parameter), $"The published {name} must affect actual decisions.");
				AreEqual(defaultValue, Convert.ToDecimal(parameter.Value));
				SetParam(strategy, name, (int)value);
			}
			IsTrue(strategy.Parameters.TryGetValue("StopLossPercent", out var stop));
			AreEqual(2m, Convert.ToDecimal(stop.Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "StopLossPercent", stopPercent);
			// An RSI decision is placed while its candle is dispatched; a quote in between belongs to protection.
			strategy.Level1Received += (_, _) => expectedSide = null;
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				var result = rsi.Add(candle.ClosePrice);
				if (!rsi.Formed || result is not decimal value) return;
				if (value == 0m) replay.ZeroReadings++;
				var prior = previous;
				previous = value;
				if (last is not null && last.State is not (OrderStates.Done or OrderStates.Failed))
				{
					replay.PendingBars++;
					return;
				}
				if (value < oversold && strategy.Position <= 0m)
				{
					expectedSide = Sides.Buy;
					expectedVolume = strategy.Volume + Math.Abs(strategy.Position);
					expectedReentry = flatAfterStop && strategy.Position == 0m && prior < oversold;
					replay.BuyEntries++;
				}
				else if (value > overbought && strategy.Position >= 0m)
				{
					expectedSide = Sides.Sell;
					expectedVolume = strategy.Volume + Math.Abs(strategy.Position);
					expectedReentry = flatAfterStop && strategy.Position == 0m && prior > overbought;
					replay.SellEntries++;
				}
				else if (strategy.Position > 0m && value >= exitLevel || strategy.Position < 0m && value <= exitLevel)
				{
					expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
					expectedVolume = Math.Abs(strategy.Position);
					expectedReentry = false;
					replay.NeutralExits++;
				}
				if (expectedSide is not null) replay.ExpectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				last = order;
				if (expectedSide is Sides side)
				{
					replay.SignalOrders++;
					if (order.Side != side || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
						replay.Violations.Add("Each RSI order must buy while RSI is below the oversold level or sell while it is above the overbought level with Volume+|Position|, or close the whole position at the neutral level, with no crossing filter or cooldown.");
					else if (expectedReentry)
					{
						if (side == Sides.Buy) replay.LongReentriesAfterStop++;
						else replay.ShortReentriesAfterStop++;
					}
					flatAfterStop = false;
				}
				else
				{
					replay.StopExits++;
					if (strategy.Position == 0m || order.Side != (strategy.Position > 0m ? Sides.Sell : Sides.Buy) || order.Volume != Math.Abs(strategy.Position))
						replay.Violations.Add("Outside the RSI decisions only the percent stop may trade, and it must close the whole position.");
					flatAfterStop = true;
				}
				expectedSide = null;
			};
		}, TimeSpan.FromDays(7));
		TestContext.WriteLine($"RSI={period} {oversold}/{overbought}/{exitLevel} stop={stopPercent}%: expected={replay.ExpectedOrders}, signal={replay.SignalOrders}, buys={replay.BuyEntries}, sells={replay.SellEntries}, neutralExits={replay.NeutralExits}, stops={replay.StopExits}, reentries={replay.LongReentriesAfterStop}/{replay.ShortReentriesAfterStop}, pendingBars={replay.PendingBars}, zeroReadings={replay.ZeroReadings}");
		return replay;
	}

	private const string ShootingStar = "0060_Shooting_Star";

	[TestMethod]
	[TestCategory("Shard04")]
	[DataRow(false, true, 2.0, 1.0)]
	[DataRow(true, false, 1.5, 0.0)]
	[DataRow(true, true, 2.0, 1.0)]
	public async Task S0060_PriorAdvanceStarNextBarConfirmationAndHighStop(bool secondarySecurity,
		bool confirmationRequired, double ratio, double stopBuffer)
	{
		var priorCloses = new Queue<decimal>();
		(decimal High, decimal Close)? candidate = null;
		decimal? activeStop = null;
		decimal? lastAsk = null;
		decimal? lastBarHigh = null;
		var bars = 0;
		var stars = 0;
		var rejectedConfirmations = 0;
		var confirmationsAboveStarHigh = 0;
		var expectedEntries = 0;
		var entries = 0;
		var exits = 0;
		var quoteExits = 0;
		var barExits = 0;
		var expectedEntry = false;
		var violations = new List<string>();
		await Replay(ShootingStar, (strategy, secondary) =>
		{
			if (secondarySecurity) strategy.Security = secondary;
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["ShadowToBodyRatio"].Value));
			AreEqual(TimeSpan.FromMinutes(15).TimeFrame(), strategy.Parameters["CandleType"].Value);
			AreEqual(1m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
			AreEqual(true, strategy.Parameters["ConfirmationRequired"].Value);
			SetParam(strategy, "ShadowToBodyRatio", Convert.ToDecimal(ratio));
			SetParam(strategy, "StopLossPercent", Convert.ToDecimal(stopBuffer));
			SetParam(strategy, "ConfirmationRequired", confirmationRequired);
			strategy.Volume = 2m;
			strategy.Level1Received += (_, message) =>
			{
				if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask && ask > 0m)
					lastAsk = ask;
			};
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished || candle.SecurityId != strategy.Security.Id.ToSecurityId()) return;
				if (expectedEntry && violations.Count < 12)
					violations.Add($"An eligible shooting star short was not entered by {strategy.CurrentTime:o}.");
				bars++;
				expectedEntry = false;
				lastBarHigh = candle.HighPrice;
				var advance = priorCloses.Count == 3;
				if (advance)
				{
					var closes = priorCloses.ToArray();
					advance = closes[0] < closes[1] && closes[1] < closes[2];
					priorCloses.Dequeue();
				}
				priorCloses.Enqueue(candle.ClosePrice);
				if (strategy.Position < 0m) return;
				if (candidate is { } earlier)
				{
					candidate = null;
					if (candle.ClosePrice < earlier.Close)
					{
						expectedEntry = true;
						expectedEntries++;
						if (candle.HighPrice > earlier.High) confirmationsAboveStarHigh++;
						activeStop = earlier.High * (1m + Convert.ToDecimal(stopBuffer) / 100m);
					}
					else rejectedConfirmations++;
				}
				if (expectedEntry)
				{
					lastAsk = lastBarHigh = null;
					return;
				}
				var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
				var upper = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice);
				var lower = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice;
				if (!advance || body <= 0m || upper < body * Convert.ToDecimal(ratio) || lower > body * 0.5m) return;
				stars++;
				if (confirmationRequired) candidate = (candle.HighPrice, candle.ClosePrice);
				else
				{
					expectedEntry = true;
					expectedEntries++;
					activeStop = candle.HighPrice * (1m + Convert.ToDecimal(stopBuffer) / 100m);
					lastAsk = lastBarHigh = null;
				}
			};
			strategy.OrderRegistering += order =>
			{
				if (order.Side == Sides.Sell)
				{
					entries++;
					if (!expectedEntry || strategy.Position != 0m || order.Volume != strategy.Volume || order.Type != OrderTypes.Market)
						if (violations.Count < 12) violations.Add("Short entry requires three rising prior closes, a qualifying upper shadow and, when enabled, the very next candle closing lower.");
					expectedEntry = false;
				}
				else
				{
					exits++;
					var quoteHit = lastAsk is decimal ask && activeStop is decimal stop && ask >= stop;
					var barHit = lastBarHigh is decimal high && activeStop is decimal barStop && high >= barStop;
					if (quoteHit) quoteExits++;
					if (barHit) barExits++;
					if (strategy.Position >= 0m || order.Volume != Math.Abs(strategy.Position) || order.Type != OrderTypes.Market || !quoteHit && !barHit)
						if (violations.Count < 12) violations.Add("A buy must close the full short only at executable ask or finished bar High at/above the fixed star-high stop.");
					candidate = null;
				}
			};
		}, TimeSpan.FromDays(14));
		TestContext.WriteLine($"python={IsPython}, secondary={secondarySecurity}, confirm={confirmationRequired}, ratio={ratio}, stop={stopBuffer}: bars={bars}, stars={stars}, rejected={rejectedConfirmations}, aboveStarHigh={confirmationsAboveStarHigh}, expectedEntries={expectedEntries}, entries={entries}, exits={exits}, quoteExits={quoteExits}, barExits={barExits}");
		IsTrue(bars > 1000 && stars > 0 && entries > 0 && exits > 0);
		if (confirmationRequired) IsTrue(rejectedConfirmations > 0, "The archive must reject at least one failed next-bar confirmation.");
		if (confirmationRequired && secondarySecurity)
			IsTrue(confirmationsAboveStarHigh > 0, "The secondary archive must confirm a star with a next candle that trades above the star high and still closes lower.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));
		AreEqual(expectedEntries, entries, "Every star confirmed by a lower next close, or taken immediately without confirmation, must be entered.");
	}

	private const string MacdDivergence = "0061_MACD_Divergence";

	[TestMethod]
	[TestCategory("Shard05")]
	[DataRow(12, 26, 9, 5, false)]
	[DataRow(5, 10, 3, 3, true)]
	public async Task S0061_IndependentEmaPivotDivergenceThenSignalCross(int fastPeriod,
		int slowPeriod, int signalPeriod, int pivotWidth, bool secondarySecurity)
	{
		var fast = new ImpulseEma(fastPeriod);
		var slow = new ImpulseEma(slowPeriod);
		var signalEma = new ImpulseEma(signalPeriod);
		var window = new List<(decimal High, decimal Low, decimal Macd)>();
		(decimal Price, decimal Macd)? lastLow = null;
		(decimal Price, decimal Macd)? lastHigh = null;
		decimal? previousLine = null;
		decimal? previousSignal = null;
		var formedBars = 0;
		var nativeScalars = 0;
		var bullishUntil = 0;
		var bearishUntil = 0;
		var bullishDivergences = 0;
		var bearishDivergences = 0;
		var expiredOrUnconfirmed = 0;
		var buyEntries = 0;
		var sellEntries = 0;
		var exits = 0;
		var expectedOrders = 0;
		var actualOrders = 0;
		var expectedLine = 0m;
		var expectedSignalValue = 0m;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var violations = new List<string>();
		await Replay(MacdDivergence, (strategy, secondary) =>
		{
			if (secondarySecurity) strategy.Security = secondary;
			AreEqual(12, strategy.Parameters["FastMacdPeriod"].Value);
			AreEqual(26, strategy.Parameters["SlowMacdPeriod"].Value);
			AreEqual(9, strategy.Parameters["SignalPeriod"].Value);
			AreEqual(5, strategy.Parameters["DivergencePeriod"].Value);
			AreEqual(TimeSpan.FromMinutes(15).TimeFrame(), strategy.Parameters["CandleType"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
			SetParam(strategy, "FastMacdPeriod", fastPeriod);
			SetParam(strategy, "SlowMacdPeriod", slowPeriod);
			SetParam(strategy, "SignalPeriod", signalPeriod);
			SetParam(strategy, "DivergencePeriod", pivotWidth);
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.Volume = 2m;
			strategy.Indicators.Added += indicator =>
			{
				if (indicator is not MovingAverageConvergenceDivergenceSignal) return;
				indicator.Changed += (_, output) =>
				{
					if (!output.IsFinal || !indicator.IsFormed || output is not MovingAverageConvergenceDivergenceSignalValue value ||
						value.Macd is not decimal line || value.Signal is not decimal signal) return;
					nativeScalars++;
					if (Math.Abs(line - expectedLine) > 0.00000001m || Math.Abs(signal - expectedSignalValue) > 0.00000001m)
						if (violations.Count < 12) violations.Add("Native MACD and signal must match independent configured EMA chains on every formed candle.");
				};
			};
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished || candle.SecurityId != strategy.Security.Id.ToSecurityId()) return;
				expectedSide = null;
				var line = fast.Add(candle.ClosePrice) - slow.Add(candle.ClosePrice);
				if (!slow.Formed) return;
				var currentSignal = signalEma.Add(line);
				expectedLine = line;
				expectedSignalValue = currentSignal;
				if (!signalEma.Formed) return;
				formedBars++;
				var up = previousLine is decimal oldLineUp && previousSignal is decimal oldSignalUp &&
					oldLineUp <= oldSignalUp && line > currentSignal;
				var down = previousLine is decimal oldLineDown && previousSignal is decimal oldSignalDown &&
					oldLineDown >= oldSignalDown && line < currentSignal;
				var bullishReady = bullishUntil >= formedBars;
				var bearishReady = bearishUntil >= formedBars;
				previousLine = line;
				previousSignal = currentSignal;
				window.Add((candle.HighPrice, candle.LowPrice, line));
				if (window.Count == pivotWidth)
				{
					var centerIndex = pivotWidth / 2;
					var center = window[centerIndex];
					if (window.Where((_, index) => index != centerIndex).All(bar => center.Low < bar.Low))
					{
						var divergence = lastLow is { } prior && center.Low < prior.Price && center.Macd > prior.Macd;
						lastLow = (center.Low, center.Macd);
						if (divergence) { bullishUntil = formedBars + pivotWidth; bullishDivergences++; }
						else { if (bullishUntil >= formedBars) expiredOrUnconfirmed++; bullishUntil = 0; }
					}
					if (window.Where((_, index) => index != centerIndex).All(bar => center.High > bar.High))
					{
						var divergence = lastHigh is { } prior && center.High > prior.Price && center.Macd < prior.Macd;
						lastHigh = (center.High, center.Macd);
						if (divergence) { bearishUntil = formedBars + pivotWidth; bearishDivergences++; }
						else { if (bearishUntil >= formedBars) expiredOrUnconfirmed++; bearishUntil = 0; }
					}
					window.RemoveAt(0);
				}
				if (strategy.Position > 0m && down || strategy.Position < 0m && up)
				{
					expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
					expectedVolume = Math.Abs(strategy.Position);
					bullishUntil = bearishUntil = 0;
					exits++;
				}
				else if (strategy.Position == 0m)
				{
					if (bullishReady && up) { expectedSide = Sides.Buy; buyEntries++; }
					else if (bearishReady && down) { expectedSide = Sides.Sell; sellEntries++; }
					if (expectedSide != null)
					{
						expectedVolume = strategy.Volume;
						bullishUntil = bearishUntil = 0;
					}
				}
				if (expectedSide != null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					if (violations.Count < 12) violations.Add("Entry requires a confirmed price/MACD pivot divergence followed by a later signal-line cross; held exits require the reverse cross and FULL remaining Position.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		TestContext.WriteLine($"python={IsPython}, secondary={secondarySecurity}, EMA={fastPeriod}/{slowPeriod}/{signalPeriod}, width={pivotWidth}: formed={formedBars}, native={nativeScalars}, bullDiv={bullishDivergences}, bearDiv={bearishDivergences}, rejected={expiredOrUnconfirmed}, buys={buyEntries}, sells={sellEntries}, exits={exits}, expectedOrders={expectedOrders}, actualOrders={actualOrders}");
		AreEqual(expectedOrders, actualOrders);
		AreEqual(formedBars, nativeScalars);
		IsTrue(formedBars > 100 && bullishDivergences > 0 && bearishDivergences > 0 && buyEntries + sellEntries > 0 && exits > 0);
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public Task S0061_PercentStopFlattensBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(MacdDivergence, TimeSpan.FromDays(31), expectedFrame: TimeSpan.FromMinutes(15));

	private const decimal StochasticTolerance = 0.00000001m;
	private const string Stochastic = "0062_Stochastic_Overbought_Oversold";

	protected readonly record struct StochasticSignalCounts(int LongEntries, int ShortEntries, int NeutralExits, int EntriesPastFifty, int ExitsAgainstPosition);

	[TestMethod]
	[TestCategory("Shard06")]
	[DataRow(14, 3, 3, false)]
	[DataRow(10, 2, 4, true)]
	public async Task S0062_IndependentSlowStochasticTurnsAndNeutralCrosses(int stochPeriod, int kPeriod, int dPeriod, bool secondarySecurity)
	{
		var counts = await ReplayStochasticSignals(stochPeriod, kPeriod, dPeriod, secondarySecurity);
		IsTrue(counts.LongEntries > 0 && counts.ShortEntries > 0 && counts.NeutralExits > 0);
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S0062_EntryBarPastFiftyExitsOnCrossBack()
	{
		// An unsmoothed 5-bar %K often jumps from an extreme zone past 50 within one bar.
		var counts = await ReplayStochasticSignals(5, 1, 3, false);
		IsTrue(counts.EntriesPastFifty > 0 && counts.ExitsAgainstPosition > 0,
			"README exits on %K crossing 50, so a position opened with %K already past 50 must close when %K crosses back.");
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public Task S0062_PercentStopFlattensBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(Stochastic, TimeSpan.FromDays(31));

	protected async Task<StochasticSignalCounts> ReplayStochasticSignals(int stochPeriod, int kPeriod, int dPeriod, bool secondarySecurity)
	{
		var candles = new Queue<ICandleMessage>();
		var rawValues = new Queue<decimal>();
		var smoothedValues = new Queue<decimal>();
		decimal? previousK = null;
		var expectedRaw = 0m;
		var expectedK = 0m;
		var expectedD = 0m;
		var expectedPriorK = 0m;
		var nativeK = 0m;
		DateTime expectedTime = default;
		var nativeScalars = 0;
		var formedK = 0;
		var fullyFormed = 0;
		var longEntries = 0;
		var shortEntries = 0;
		var neutralExits = 0;
		var entriesPastFifty = 0;
		var exitsAgainstPosition = 0;
		var expectedOrders = 0;
		var actualOrders = 0;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var violations = new List<string>();
		await Replay(Stochastic, (strategy, secondary) =>
		{
			if (secondarySecurity) strategy.Security = secondary;
			AreEqual(14, strategy.Parameters["StochPeriod"].Value);
			AreEqual(3, strategy.Parameters["KPeriod"].Value);
			AreEqual(3, strategy.Parameters["DPeriod"].Value);
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
			SetParam(strategy, "StochPeriod", stochPeriod);
			SetParam(strategy, "KPeriod", kPeriod);
			SetParam(strategy, "DPeriod", dPeriod);
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.Volume = 2m;
			strategy.Indicators.Added += indicator =>
			{
				if (indicator is not StochasticOscillator) return;
				indicator.Changed += (_, output) =>
				{
					if (!output.IsFinal || !indicator.IsFormed ||
						output is not StochasticOscillatorValue value ||
						value.K is not decimal raw || value.D is not decimal smoothed) return;
					nativeScalars++;
					nativeK = smoothed;
					if (Math.Abs(raw - expectedRaw) > 0.00000001m ||
						Math.Abs(smoothed - expectedK) > 0.00000001m)
						if (violations.Count < 12) violations.Add("Native raw stochastic and smoothed K must match independently calculated high/low and SMA windows.");
				};
			};
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished ||
					candle.SecurityId != strategy.Security.Id.ToSecurityId()) return;
				if (expectedSide is not null && violations.Count < 12)
					violations.Add($"Missing {expectedSide}/{expectedVolume} order at {expectedTime:o}: priorK={expectedPriorK}, K={expectedK}, nativeK={nativeK}, D={expectedD}, position={strategy.Position}.");
				expectedSide = null;
				candles.Enqueue(candle);
				if (candles.Count > stochPeriod) candles.Dequeue();
				if (candles.Count < stochPeriod) return;
				var high = candles.Max(bar => bar.HighPrice);
				var low = candles.Min(bar => bar.LowPrice);
				var raw = high == low ? 0m : 100m * (candle.ClosePrice - low) / (high - low);
				expectedRaw = raw;
				rawValues.Enqueue(raw);
				if (rawValues.Count > kPeriod) rawValues.Dequeue();
				if (rawValues.Count < kPeriod) return;
				var k = rawValues.Average();
				expectedK = k;
				formedK++;
				smoothedValues.Enqueue(k);
				if (smoothedValues.Count > dPeriod) smoothedValues.Dequeue();
				if (smoothedValues.Count < dPeriod) return;
				fullyFormed++;
				var d = smoothedValues.Average();
				expectedD = d;
				expectedTime = candle.OpenTime;
				var prior = previousK;
				expectedPriorK = prior ?? 0m;
				previousK = k;
				if (prior is not decimal oldK) return;

				if (strategy.Position == 0m && oldK < 20m - StochasticTolerance &&
					k > oldK + StochasticTolerance && k > d + StochasticTolerance)
				{
					expectedSide = Sides.Buy;
					expectedVolume = strategy.Volume;
					longEntries++;
					if (k >= 50m - StochasticTolerance) entriesPastFifty++;
				}
				else if (strategy.Position == 0m && oldK > 80m + StochasticTolerance &&
					k < oldK - StochasticTolerance && k < d - StochasticTolerance)
				{
					expectedSide = Sides.Sell;
					expectedVolume = strategy.Volume;
					shortEntries++;
					if (k <= 50m + StochasticTolerance) entriesPastFifty++;
				}
				// README: %K crossing 50 closes the position whichever way it crosses.
				else if (strategy.Position > 0m &&
					(oldK < 50m - StochasticTolerance) != (k < 50m - StochasticTolerance))
				{
					expectedSide = Sides.Sell;
					expectedVolume = strategy.Position;
					neutralExits++;
					if (k < 50m - StochasticTolerance) exitsAgainstPosition++;
				}
				else if (strategy.Position < 0m &&
					(oldK > 50m + StochasticTolerance) != (k > 50m + StochasticTolerance))
				{
					expectedSide = Sides.Buy;
					expectedVolume = Math.Abs(strategy.Position);
					neutralExits++;
					if (k > 50m + StochasticTolerance) exitsAgainstPosition++;
				}
				if (expectedSide != null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					if (violations.Count < 12) violations.Add($"Unexpected order at {expectedTime:o}: actual={order.Side}/{order.Volume}/{order.Type}, expected={expectedSide}/{expectedVolume}/Market, priorK={expectedPriorK}, K={expectedK}, nativeK={nativeK}, D={expectedD}, position={strategy.Position}.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		if (expectedSide is not null && violations.Count < 12)
			violations.Add($"Missing terminal {expectedSide}/{expectedVolume} order at {expectedTime:o}: priorK={expectedPriorK}, K={expectedK}, nativeK={nativeK}, D={expectedD}.");
		TestContext.WriteLine($"python={IsPython}, culture={CultureInfo.CurrentCulture.Name}, secondary={secondarySecurity}, periods={stochPeriod}/{kPeriod}/{dPeriod}: native={nativeScalars}, K={formedK}, D={fullyFormed}, long={longEntries}, short={shortEntries}, neutral={neutralExits}, pastFifty={entriesPastFifty}, againstPosition={exitsAgainstPosition}, expectedOrders={expectedOrders}, actualOrders={actualOrders}");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));
		AreEqual(formedK, nativeScalars);
		AreEqual(expectedOrders, actualOrders);
		IsTrue(fullyFormed > 100);
		return new(longEntries, shortEntries, neutralExits, entriesPastFifty, exitsAgainstPosition);
	}

	private const string EngulfingBullish = "0063_Engulfing_Bullish";

	[TestMethod]
	[TestCategory("Shard07")]
	[DataRow(false, true, 3, 1.0)]
	[DataRow(true, false, 1, 0.0)]
	public async Task S0063_ConsecutiveDownBarsBodyEngulfAndPatternLowStop(bool secondarySecurity,
		bool requireDowntrend, int downtrendBars, double stopBuffer)
	{
		var recent = new List<ICandleMessage>();
		decimal? activeStop = null;
		decimal? lastBid = null;
		decimal? lastFinishedLow = null;
		var candidates = 0;
		var trendRejected = 0;
		var expectedEntries = 0;
		var entries = 0;
		var exits = 0;
		var quoteExits = 0;
		var barExits = 0;
		var expectedEntry = false;
		var violations = new List<string>();
		await Replay(EngulfingBullish, (strategy, secondary) =>
		{
			if (secondarySecurity) strategy.Security = secondary;
			AreEqual(TimeSpan.FromMinutes(15).TimeFrame(), strategy.Parameters["CandleType"].Value);
			AreEqual(1m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
			AreEqual(true, strategy.Parameters["RequireDowntrend"].Value);
			AreEqual(3, strategy.Parameters["DowntrendBars"].Value);
			SetParam(strategy, "StopLossPercent", Convert.ToDecimal(stopBuffer));
			SetParam(strategy, "RequireDowntrend", requireDowntrend);
			SetParam(strategy, "DowntrendBars", downtrendBars);
			strategy.Volume = 2m;
			strategy.Level1Received += (_, message) =>
			{
				if (message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid && bid > 0m)
					lastBid = bid;
			};
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished ||
					candle.SecurityId != strategy.Security.Id.ToSecurityId()) return;
				if (expectedEntry && violations.Count < 12)
					violations.Add("Eligible bullish engulfing did not issue its long order.");
				expectedEntry = false;
				lastFinishedLow = candle.LowPrice;
				if (strategy.Position > 0m)
				{
					if (activeStop is not decimal stop || candle.LowPrice > stop)
					{
						recent.Add(candle);
						if (recent.Count > downtrendBars) recent.RemoveAt(0);
					}
					return;
				}
				var previous = recent.LastOrDefault();
				var downtrend = !requireDowntrend ||
					recent.Count >= downtrendBars &&
					recent.TakeLast(downtrendBars).All(bar => bar.ClosePrice < bar.OpenPrice);
				var engulfing = previous is not null && previous.ClosePrice < previous.OpenPrice &&
					candle.ClosePrice > candle.OpenPrice &&
					candle.OpenPrice <= previous.ClosePrice &&
					candle.ClosePrice >= previous.OpenPrice;
				if (engulfing)
				{
					candidates++;
					if (!downtrend) trendRejected++;
					else
					{
						expectedEntry = true;
						expectedEntries++;
						activeStop = Math.Min(previous.LowPrice, candle.LowPrice) *
							(1m - Convert.ToDecimal(stopBuffer) / 100m);
					}
				}
				recent.Add(candle);
				if (recent.Count > downtrendBars) recent.RemoveAt(0);
			};
			strategy.OrderRegistering += order =>
			{
				if (order.Side == Sides.Buy)
				{
					entries++;
					if (!expectedEntry || strategy.Position != 0m ||
						order.Volume != strategy.Volume || order.Type != OrderTypes.Market)
						if (violations.Count < 12) violations.Add("Long requires a fully engulfed bearish body, optional consecutive bearish history, and no existing position.");
					expectedEntry = false;
					lastBid = null;
					lastFinishedLow = null;
				}
				else
				{
					exits++;
					var byQuote = lastBid is decimal bid && activeStop is decimal stop && bid <= stop;
					var byBar = lastFinishedLow is decimal low && activeStop is decimal stopLow && low <= stopLow;
					if (byQuote) quoteExits++;
					if (byBar) barExits++;
					if (strategy.Position <= 0m || order.Volume != strategy.Position ||
						order.Type != OrderTypes.Market || !byQuote && !byBar)
						if (violations.Count < 12) violations.Add("Sell must fully close the long after best bid or finished bar low breaches the buffered pattern-low stop, never open a short.");
					recent.Clear();
					activeStop = null;
				}
			};
		}, TimeSpan.FromDays(31));
		TestContext.WriteLine($"python={IsPython}, secondary={secondarySecurity}, downtrend={requireDowntrend}/{downtrendBars}, stop={stopBuffer}: candidates={candidates}, rejected={trendRejected}, expectedEntries={expectedEntries}, entries={entries}, exits={exits}, quotes={quoteExits}, bars={barExits}");
		AreEqual(expectedEntries, entries);
		IsTrue(candidates > 0 && entries > 0 && exits > 0 && quoteExits + barExits > 0);
		if (requireDowntrend) IsTrue(trendRejected > 0);
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));
	}

	private const string EngulfingBearish = "0064_Engulfing_Bearish";

	[TestMethod]
	[TestCategory("Shard00")]
	[DataRow(false, true, 3, 1.0)]
	[DataRow(true, false, 1, 0.0)]
	public async Task S0064_ConsecutiveUpBarsBodyEngulfAndPatternHighStop(bool secondarySecurity,
		bool requireUptrend, int uptrendBars, double stopBuffer)
	{
		var recent = new List<ICandleMessage>();
		decimal? activeStop = null;
		decimal? lastAsk = null;
		decimal? lastFinishedHigh = null;
		var candidates = 0;
		var trendRejected = 0;
		var expectedEntries = 0;
		var entries = 0;
		var exits = 0;
		var quoteExits = 0;
		var barExits = 0;
		var expectedEntry = false;
		var violations = new List<string>();
		await Replay(EngulfingBearish, (strategy, secondary) =>
		{
			if (secondarySecurity) strategy.Security = secondary;
			AreEqual(TimeSpan.FromMinutes(15).TimeFrame(), strategy.Parameters["CandleType"].Value);
			AreEqual(1m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
			AreEqual(true, strategy.Parameters["RequireUptrend"].Value);
			AreEqual(3, strategy.Parameters["UptrendBars"].Value);
			SetParam(strategy, "StopLossPercent", Convert.ToDecimal(stopBuffer));
			SetParam(strategy, "RequireUptrend", requireUptrend);
			SetParam(strategy, "UptrendBars", uptrendBars);
			strategy.Volume = 2m;
			strategy.Level1Received += (_, message) =>
			{
				if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask && ask > 0m)
					lastAsk = ask;
			};
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished ||
					candle.SecurityId != strategy.Security.Id.ToSecurityId()) return;
				if (expectedEntry && violations.Count < 12)
					violations.Add("Eligible bearish engulfing did not issue its short order.");
				expectedEntry = false;
				lastFinishedHigh = candle.HighPrice;
				if (strategy.Position < 0m)
				{
					if (activeStop is not decimal stop || candle.HighPrice < stop)
					{
						recent.Add(candle);
						if (recent.Count > uptrendBars) recent.RemoveAt(0);
					}
					return;
				}
				var previous = recent.LastOrDefault();
				var uptrend = !requireUptrend ||
					recent.Count >= uptrendBars &&
					recent.TakeLast(uptrendBars).All(bar => bar.ClosePrice > bar.OpenPrice);
				var engulfing = previous is not null && previous.ClosePrice > previous.OpenPrice &&
					candle.ClosePrice < candle.OpenPrice &&
					candle.OpenPrice >= previous.ClosePrice &&
					candle.ClosePrice <= previous.OpenPrice;
				if (engulfing)
				{
					candidates++;
					if (!uptrend) trendRejected++;
					else
					{
						expectedEntry = true;
						expectedEntries++;
						activeStop = Math.Max(previous.HighPrice, candle.HighPrice) *
							(1m + Convert.ToDecimal(stopBuffer) / 100m);
					}
				}
				recent.Add(candle);
				if (recent.Count > uptrendBars) recent.RemoveAt(0);
			};
			strategy.OrderRegistering += order =>
			{
				if (order.Side == Sides.Sell)
				{
					entries++;
					if (!expectedEntry || strategy.Position != 0m ||
						order.Volume != strategy.Volume || order.Type != OrderTypes.Market)
						if (violations.Count < 12) violations.Add("Short requires a fully engulfed bullish body, optional consecutive bullish history, and no existing position.");
					expectedEntry = false;
					lastAsk = null;
					lastFinishedHigh = null;
				}
				else
				{
					exits++;
					var byQuote = lastAsk is decimal ask && activeStop is decimal stop && ask >= stop;
					var byBar = lastFinishedHigh is decimal high && activeStop is decimal stopHigh && high >= stopHigh;
					if (byQuote) quoteExits++;
					if (byBar) barExits++;
					if (strategy.Position >= 0m || order.Volume != Math.Abs(strategy.Position) ||
						order.Type != OrderTypes.Market || !byQuote && !byBar)
						if (violations.Count < 12) violations.Add("Buy must fully cover the short after best ask or finished bar high breaches the buffered pattern-high stop, never open a long.");
					recent.Clear();
					activeStop = null;
				}
			};
		}, TimeSpan.FromDays(31));
		TestContext.WriteLine($"python={IsPython}, secondary={secondarySecurity}, uptrend={requireUptrend}/{uptrendBars}, stop={stopBuffer}: candidates={candidates}, rejected={trendRejected}, expectedEntries={expectedEntries}, entries={entries}, exits={exits}, quotes={quoteExits}, bars={barExits}");
		AreEqual(expectedEntries, entries);
		IsTrue(candidates > 0 && entries > 0 && exits > 0 && quoteExits + barExits > 0);
		if (requireUptrend) IsTrue(trendRejected > 0);
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));
	}

	private const string Pinbar = "0065_Pinbar_Reversal";

	[TestMethod]
	[TestCategory("Shard01")]
	[DataRow(2.0, 0.5, 20, false)]
	[DataRow(1.5, 0.25, 10, true)]
	public async Task S0065_IndependentPinbarGeometryTrendAndOppositeExit(double tailRatio,
		double oppositeRatio, int maPeriod, bool secondarySecurity)
	{
		var closes = new Queue<decimal>();
		var expectedSma = 0m;
		var nativeSmaValues = 0;
		var expectedOrders = 0;
		var actualOrders = 0;
		var longEntries = 0;
		var shortEntries = 0;
		var oppositeExits = 0;
		var trendRejected = 0;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var violations = new List<string>();
		await Replay(Pinbar, (strategy, secondary) =>
		{
			if (secondarySecurity) strategy.Security = secondary;
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["TailToBodyRatio"].Value));
			AreEqual(0.5m, Convert.ToDecimal(strategy.Parameters["OppositeTailRatio"].Value));
			AreEqual(20, strategy.Parameters["MAPeriod"].Value);
			AreEqual(TimeSpan.FromMinutes(15).TimeFrame(), strategy.Parameters["CandleType"].Value);
			AreEqual(1m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
			SetParam(strategy, "TailToBodyRatio", Convert.ToDecimal(tailRatio));
			SetParam(strategy, "OppositeTailRatio", Convert.ToDecimal(oppositeRatio));
			SetParam(strategy, "MAPeriod", maPeriod);
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.Volume = 2m;
			strategy.Indicators.Added += indicator =>
			{
				if (indicator is not SimpleMovingAverage) return;
				indicator.Changed += (_, output) =>
				{
					if (!output.IsFinal || !indicator.IsFormed) return;
					nativeSmaValues++;
					if (Math.Abs(output.GetValue<decimal>() - expectedSma) > 0.00000001m)
						if (violations.Count < 12) violations.Add("Native close SMA must match the independent current-inclusive rolling average.");
				};
			};
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished ||
					candle.SecurityId != strategy.Security.Id.ToSecurityId()) return;
				expectedSide = null;
				closes.Enqueue(candle.ClosePrice);
				if (closes.Count > maPeriod) closes.Dequeue();
				if (closes.Count < maPeriod) return;
				var sma = closes.Average();
				expectedSma = sma;
				var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
				if (body <= 0m) return;
				var lower = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice;
				var upper = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice);
				var bullish = lower >= body * Convert.ToDecimal(tailRatio) &&
					upper <= body * Convert.ToDecimal(oppositeRatio);
				var bearish = upper >= body * Convert.ToDecimal(tailRatio) &&
					lower <= body * Convert.ToDecimal(oppositeRatio);
				if (strategy.Position > 0m && bearish)
				{
					expectedSide = Sides.Sell;
					expectedVolume = strategy.Position;
					oppositeExits++;
				}
				else if (strategy.Position < 0m && bullish)
				{
					expectedSide = Sides.Buy;
					expectedVolume = Math.Abs(strategy.Position);
					oppositeExits++;
				}
				else if (strategy.Position == 0m && bullish && candle.ClosePrice > sma)
				{
					expectedSide = Sides.Buy;
					expectedVolume = strategy.Volume;
					longEntries++;
				}
				else if (strategy.Position == 0m && bearish && candle.ClosePrice < sma)
				{
					expectedSide = Sides.Sell;
					expectedVolume = strategy.Volume;
					shortEntries++;
				}
				else if (strategy.Position == 0m && bullish && candle.ClosePrice <= sma ||
					strategy.Position == 0m && bearish && candle.ClosePrice >= sma)
					trendRejected++;
				if (expectedSide != null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume ||
					order.Type != OrderTypes.Market)
					if (violations.Count < 12) violations.Add("Entry requires independently calculated pinbar geometry in the close-SMA trend direction; held exit requires an opposite pinbar and full remaining quantity.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		TestContext.WriteLine($"python={IsPython}, secondary={secondarySecurity}, ratio={tailRatio}/{oppositeRatio}, MA={maPeriod}: nativeSma={nativeSmaValues}, long={longEntries}, short={shortEntries}, oppositeExits={oppositeExits}, trendRejected={trendRejected}, expectedOrders={expectedOrders}, actualOrders={actualOrders}");
		AreEqual(expectedOrders, actualOrders);
		IsTrue(nativeSmaValues > 100 && longEntries > 0 && shortEntries > 0 &&
			oppositeExits > 0 && trendRejected > 0);
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));
	}

	[TestMethod]
	[TestCategory("Shard01")]
	public Task S0065_PercentStopFlattensBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(Pinbar, TimeSpan.FromDays(31), expectedFrame: TimeSpan.FromMinutes(15), expectedStopPercent: 1m);

	private const string ThreeBarUp = "0066_Three_Bar_Reversal_Up";

	[TestMethod]
	[TestCategory("Shard02")]
	[DataRow(false, true, 5, 1.0)]
	[DataRow(true, false, 2, 0.0)]
	public async Task S0066_ThreeCausalBarsNetTrendOppositeAndPatternStop(bool secondarySecurity, bool requireDowntrend, int trendLength, double stopBuffer)
	{
		var recent = new List<ICandleMessage>();
		decimal? activeStop = null;
		decimal? lastBid = null;
		decimal? lastFinishedLow = null;
		var candidates = 0;
		var trendRejected = 0;
		var expectedEntries = 0;
		var entries = 0;
		var exits = 0;
		var oppositeExits = 0;
		var stopExits = 0;
		var quoteExits = 0;
		var barExits = 0;
		var expectedEntry = false;
		var expectedOpposite = false;
		var possibleLateEntry = false;
		decimal? possibleLateStop = null;
		DateTime? lastProcessedOpenTime = null;
		var violations = new List<string>();
		var recentOrders = new Queue<string>();
		await Replay(ThreeBarUp, (strategy, secondary) =>
		{
			if (secondarySecurity) strategy.Security = secondary;
			AreEqual(TimeSpan.FromMinutes(15).TimeFrame(), strategy.Parameters["CandleType"].Value);
			AreEqual(1m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
			AreEqual(true, strategy.Parameters["RequireDowntrend"].Value);
			AreEqual(5, strategy.Parameters["DowntrendLength"].Value);
			SetParam(strategy, "StopLossPercent", Convert.ToDecimal(stopBuffer));
			SetParam(strategy, "RequireDowntrend", requireDowntrend);
			SetParam(strategy, "DowntrendLength", trendLength);
			strategy.Volume = 2m;
			strategy.Level1Received += (_, message) =>
			{
				if (message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid && bid > 0m)
					lastBid = bid;
			};
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished ||
					candle.SecurityId != strategy.Security.Id.ToSecurityId() ||
					candle.DataType != TimeSpan.FromMinutes(15).TimeFrame()) return;
				// The replay can deliver the same completed built candle more than once.
				if (lastProcessedOpenTime == candle.OpenTime) return;
				lastProcessedOpenTime = candle.OpenTime;
				if (expectedEntry && violations.Count < 12)
					violations.Add($"Eligible up reversal did not enter long by {strategy.CurrentTime:o}.");
				expectedEntry = expectedOpposite = false;
				possibleLateEntry = false;
				possibleLateStop = null;
				lastFinishedLow = candle.LowPrice;
				if (strategy.Position > 0m && activeStop is decimal stop && candle.LowPrice <= stop)
					return;
				var previous = recent.Count >= 1 ? recent[^1] : null;
				var older = recent.Count >= 2 ? recent[^2] : null;
				var upPattern = older is not null && previous.ClosePrice < previous.OpenPrice &&
					older.ClosePrice < older.OpenPrice && previous.LowPrice < older.LowPrice &&
					candle.ClosePrice > candle.OpenPrice && candle.ClosePrice > previous.HighPrice;
				var downPattern = older is not null && previous.ClosePrice > previous.OpenPrice &&
					older.ClosePrice > older.OpenPrice && previous.HighPrice > older.HighPrice &&
					candle.ClosePrice < candle.OpenPrice && candle.ClosePrice < previous.LowPrice;
				var downtrend = !requireDowntrend || recent.Count >= trendLength &&
					recent[^1].ClosePrice < recent[^trendLength].ClosePrice;
				possibleLateEntry = upPattern && downtrend;
				if (possibleLateEntry)
					possibleLateStop = Math.Min(Math.Min(older.LowPrice, previous.LowPrice), candle.LowPrice) *
						(1m - Convert.ToDecimal(stopBuffer) / 100m);
				if (strategy.Position > 0m && downPattern)
				{
					expectedOpposite = true;
					recent.Clear();
				}
				else if (strategy.Position == 0m && upPattern)
				{
					candidates++;
					if (!downtrend) trendRejected++;
					else
					{
						expectedEntry = true;
						expectedEntries++;
						activeStop = Math.Min(Math.Min(older.LowPrice, previous.LowPrice), candle.LowPrice) *
							(1m - Convert.ToDecimal(stopBuffer) / 100m);
					}
				}
				recent.Add(candle);
				if (recent.Count > trendLength) recent.RemoveAt(0);
			};
			strategy.OrderRegistering += order =>
			{
				recentOrders.Enqueue($"{strategy.CurrentTime:o}:{order.Side}/{order.Volume}/pos={strategy.Position}");
				if (recentOrders.Count > 8) recentOrders.Dequeue();
				if (order.Side == Sides.Buy)
				{
					entries++;
					if (!expectedEntry && possibleLateEntry && strategy.Position == 0m)
					{
						expectedEntries++;
						activeStop = possibleLateStop;
					}
					if (!expectedEntry && !possibleLateEntry || strategy.Position != 0m ||
						order.Volume != strategy.Volume || order.Type != OrderTypes.Market)
						if (violations.Count < 12) violations.Add($"Unexpected long at {strategy.CurrentTime:o}: expectedEntry={expectedEntry}, latePattern={possibleLateEntry}, recent={recent.Count}, position={strategy.Position}, volume={order.Volume}, orders={string.Join(" | ", recentOrders)}.");
					expectedEntry = false;
					possibleLateEntry = false;
					lastBid = null;
					lastFinishedLow = null;
				}
				else
				{
					exits++;
					var byOpposite = expectedOpposite;
					var byQuote = lastBid is decimal bid && activeStop is decimal stop && bid <= stop;
					var byBar = lastFinishedLow is decimal low && activeStop is decimal stopLow && low <= stopLow;
					if (byQuote) quoteExits++;
					if (byBar) barExits++;
					if (byOpposite) oppositeExits++;
					else if (byQuote || byBar) stopExits++;
					if (strategy.Position <= 0m || order.Volume != strategy.Position ||
						order.Type != OrderTypes.Market || !byOpposite && !byQuote && !byBar)
						if (violations.Count < 12) violations.Add("Sell must fully close the long on an opposite three-bar reversal or a best-bid/finished-low pattern stop, never open a short.");
					expectedOpposite = false;
					// The strategy retains the just-confirmed opposite-pattern candle
					// after closing; only stop exits discard candle history.
					if (!byOpposite) recent.Clear();
					activeStop = null;
				}
			};
		}, TimeSpan.FromDays(31));
		TestContext.WriteLine($"python={IsPython}, secondary={secondarySecurity}, trend={requireDowntrend}/{trendLength}, stop={stopBuffer}: candidates={candidates}, rejected={trendRejected}, expectedEntries={expectedEntries}, entries={entries}, exits={exits}, opposite={oppositeExits}, stops={stopExits}, quotes={quoteExits}, bars={barExits}");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));
		AreEqual(expectedEntries, entries);
		IsTrue(candidates > 0 && entries > 0 && exits > 0 && stopExits > 0);
		if (requireDowntrend) IsTrue(trendRejected > 0);
	}

	private const string ThreeBarDown = "0067_Three_Bar_Reversal_Down";

	[TestMethod]
	[TestCategory("Shard03")]
	[DataRow(false, true, 5, 1.0)]
	[DataRow(true, false, 2, 0.0)]
	public async Task S0067_ThreeCausalBarsNetTrendOppositeAndPatternStop(bool secondarySecurity, bool requireUptrend, int trendLength, double stopBuffer)
	{
		var recent = new List<ICandleMessage>();
		decimal? activeStop = null;
		decimal? lastAsk = null;
		decimal? lastFinishedHigh = null;
		DateTime? lastProcessedOpenTime = null;
		var candidates = 0;
		var trendRejected = 0;
		var expectedEntries = 0;
		var entries = 0;
		var exits = 0;
		var oppositeExits = 0;
		var stopExits = 0;
		var quoteExits = 0;
		var barExits = 0;
		var expectedEntry = false;
		var expectedOpposite = false;
		var possibleLateEntry = false;
		decimal? possibleLateStop = null;
		var violations = new List<string>();
		await Replay(ThreeBarDown, (strategy, secondary) =>
		{
			if (secondarySecurity) strategy.Security = secondary;
			AreEqual(TimeSpan.FromMinutes(15).TimeFrame(), strategy.Parameters["CandleType"].Value);
			AreEqual(1m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
			AreEqual(true, strategy.Parameters["RequireUptrend"].Value);
			AreEqual(5, strategy.Parameters["UptrendLength"].Value);
			SetParam(strategy, "StopLossPercent", Convert.ToDecimal(stopBuffer));
			SetParam(strategy, "RequireUptrend", requireUptrend);
			SetParam(strategy, "UptrendLength", trendLength);
			strategy.Volume = 2m;
			strategy.Level1Received += (_, message) =>
			{
				if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask && ask > 0m)
					lastAsk = ask;
			};
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished ||
					candle.SecurityId != strategy.Security.Id.ToSecurityId() ||
					candle.DataType != TimeSpan.FromMinutes(15).TimeFrame()) return;
				if (lastProcessedOpenTime == candle.OpenTime) return;
				lastProcessedOpenTime = candle.OpenTime;
				if (expectedEntry && violations.Count < 12)
					violations.Add($"Eligible down reversal did not enter short by {strategy.CurrentTime:o}.");
				expectedEntry = expectedOpposite = possibleLateEntry = false;
				possibleLateStop = null;
				lastFinishedHigh = candle.HighPrice;
				if (strategy.Position < 0m && activeStop is decimal stop && candle.HighPrice >= stop)
					return;
				var previous = recent.Count >= 1 ? recent[^1] : null;
				var older = recent.Count >= 2 ? recent[^2] : null;
				var downPattern = older is not null && previous.ClosePrice > previous.OpenPrice &&
					older.ClosePrice > older.OpenPrice && previous.HighPrice > older.HighPrice &&
					candle.ClosePrice < candle.OpenPrice && candle.ClosePrice < previous.LowPrice;
				var upPattern = older is not null && previous.ClosePrice < previous.OpenPrice &&
					older.ClosePrice < older.OpenPrice && previous.LowPrice < older.LowPrice &&
					candle.ClosePrice > candle.OpenPrice && candle.ClosePrice > previous.HighPrice;
				var uptrend = !requireUptrend || recent.Count >= trendLength &&
					recent[^1].ClosePrice > recent[^trendLength].ClosePrice;
				possibleLateEntry = downPattern && uptrend;
				if (possibleLateEntry)
					possibleLateStop = Math.Max(Math.Max(older.HighPrice, previous.HighPrice), candle.HighPrice) *
						(1m + Convert.ToDecimal(stopBuffer) / 100m);
				if (strategy.Position < 0m && upPattern)
				{
					expectedOpposite = true;
					recent.Clear();
				}
				else if (strategy.Position == 0m && downPattern)
				{
					candidates++;
					if (!uptrend) trendRejected++;
					else
					{
						expectedEntry = true;
						expectedEntries++;
						activeStop = possibleLateStop;
					}
				}
				recent.Add(candle);
				if (recent.Count > trendLength) recent.RemoveAt(0);
			};
			strategy.OrderRegistering += order =>
			{
				if (order.Side == Sides.Sell)
				{
					entries++;
					if (!expectedEntry && possibleLateEntry && strategy.Position == 0m)
					{
						expectedEntries++;
						activeStop = possibleLateStop;
					}
					if (!expectedEntry && !possibleLateEntry || strategy.Position != 0m ||
						order.Volume != strategy.Volume || order.Type != OrderTypes.Market)
						if (violations.Count < 12) violations.Add($"Unexpected short at {strategy.CurrentTime:o}: expectedEntry={expectedEntry}, latePattern={possibleLateEntry}, recent={recent.Count}, position={strategy.Position}, volume={order.Volume}.");
					expectedEntry = possibleLateEntry = false;
					lastAsk = null;
					lastFinishedHigh = null;
				}
				else
				{
					exits++;
					var byOpposite = expectedOpposite;
					var byQuote = lastAsk is decimal ask && activeStop is decimal stop && ask >= stop;
					var byBar = lastFinishedHigh is decimal high && activeStop is decimal stopHigh && high >= stopHigh;
					if (byQuote) quoteExits++;
					if (byBar) barExits++;
					if (byOpposite) oppositeExits++;
					else if (byQuote || byBar) stopExits++;
					if (strategy.Position >= 0m || order.Volume != Math.Abs(strategy.Position) ||
						order.Type != OrderTypes.Market || !byOpposite && !byQuote && !byBar)
						if (violations.Count < 12) violations.Add("Buy must fully cover the short on an opposite three-bar reversal or best-ask/finished-high pattern stop, never open a long.");
					expectedOpposite = false;
					// An opposite-pattern exit retains its signal candle as the next window's first bar.
					if (!byOpposite) recent.Clear();
					activeStop = null;
				}
			};
		}, TimeSpan.FromDays(31));
		TestContext.WriteLine($"python={IsPython}, secondary={secondarySecurity}, trend={requireUptrend}/{trendLength}, stop={stopBuffer}: candidates={candidates}, rejected={trendRejected}, expectedEntries={expectedEntries}, entries={entries}, exits={exits}, opposite={oppositeExits}, stops={stopExits}, asks={quoteExits}, bars={barExits}");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));
		AreEqual(expectedEntries, entries);
		IsTrue(candidates > 0 && entries > 0 && exits > 0 && stopExits > 0);
		if (requireUptrend) IsTrue(trendRejected > 0);
	}

	private const string CciDivergence = "0068_CCI_Divergence";

	[TestMethod]
	[TestCategory("Shard04")]
	[DataRow(20, 5, 100, -100, false, false)]
	[DataRow(14, 3, 50, -50, true, false)]
	[DataRow(20, 5, 1, -1, false, true)]
	public async Task S0068_IndependentTypicalPriceCciPivotsAndZeroExits(int cciPeriod,
		int pivotWidth, int overbought, int oversold, bool secondarySecurity, bool requireCarriedSignals)
	{
		var typicalPrices = new Queue<decimal>();
		var window = new List<(decimal High, decimal Low, decimal Cci)>();
		(decimal Price, decimal Cci)? lastLow = null;
		(decimal Price, decimal Cci)? lastHigh = null;
		Sides? signal = null;
		var signalAge = 0;
		var expiredUnreplaced = false;
		decimal? previousCci = null;
		var expectedCci = 0m;
		var cciReady = false;
		var formedBars = 0;
		var nativeScalars = 0;
		var lowPivots = 0;
		var highPivots = 0;
		var rejected = 0;
		var longEntries = 0;
		var shortEntries = 0;
		var zeroExits = 0;
		var delayedEntries = 0;
		var expiredBeforeFlat = 0;
		var expectedOrders = 0;
		var actualOrders = 0;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var violations = new List<string>();
		void Flag(Sides flagged)
		{
			signal = flagged;
			signalAge = 0;
			expiredUnreplaced = false;
		}
		await Replay(CciDivergence, (strategy, secondary) =>
		{
			if (secondarySecurity) strategy.Security = secondary;
			AreEqual(20, strategy.Parameters["CciPeriod"].Value);
			AreEqual(5, strategy.Parameters["DivergencePeriod"].Value);
			AreEqual(100m, Convert.ToDecimal(strategy.Parameters["OverboughtLevel"].Value));
			AreEqual(-100m, Convert.ToDecimal(strategy.Parameters["OversoldLevel"].Value));
			AreEqual(TimeSpan.FromMinutes(15).TimeFrame(), strategy.Parameters["CandleType"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
			SetParam(strategy, "CciPeriod", cciPeriod);
			SetParam(strategy, "DivergencePeriod", pivotWidth);
			SetParam(strategy, "OverboughtLevel", (decimal)overbought);
			SetParam(strategy, "OversoldLevel", (decimal)oversold);
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.Volume = 2m;
			strategy.Indicators.Added += indicator =>
			{
				if (indicator is not CommodityChannelIndex) return;
				indicator.Changed += (_, output) =>
				{
					if (!output.IsFinal || !indicator.IsFormed || output.IsEmpty || !cciReady) return;
					nativeScalars++;
					if (Math.Abs(output.GetValue<decimal>() - expectedCci) > 0.000001m &&
						violations.Count < 12)
						violations.Add("Native CCI must match independent typical-price mean-deviation CCI on every formed candle.");
				};
			};
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished || candle.SecurityId != strategy.Security.Id.ToSecurityId()) return;
				expectedSide = null;
				cciReady = false;
				typicalPrices.Enqueue((candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m);
				if (typicalPrices.Count > cciPeriod) typicalPrices.Dequeue();
				if (typicalPrices.Count < cciPeriod) return;
				var average = typicalPrices.Sum() / cciPeriod;
				var deviation = typicalPrices.Sum(value => Math.Abs(value - average)) / cciPeriod;
				if (deviation == 0m) return;
				var cci = (typicalPrices.Last() - average) / (0.015m * deviation);
				expectedCci = cci;
				cciReady = true;
				formedBars++;

				var upCross = previousCci is decimal prevUp && prevUp <= 0m && cci > 0m;
				var downCross = previousCci is decimal prevDown && prevDown >= 0m && cci < 0m;
				previousCci = cci;
				if (signal is not null && ++signalAge > pivotWidth)
				{
					signal = null;
					expiredUnreplaced = true;
				}
				window.Add((candle.HighPrice, candle.LowPrice, cci));
				if (window.Count == pivotWidth)
				{
					var middle = pivotWidth / 2;
					var pivot = window[middle];
					if (window.Where((_, index) => index != middle).All(bar => pivot.Low < bar.Low))
					{
						lowPivots++;
						var bullish = lastLow is { } prior && pivot.Low < prior.Price &&
							pivot.Cci > prior.Cci && pivot.Cci < oversold;
						if (bullish) Flag(Sides.Buy);
						else rejected++;
						lastLow = (pivot.Low, pivot.Cci);
					}
					if (window.Where((_, index) => index != middle).All(bar => pivot.High > bar.High))
					{
						highPivots++;
						var bearish = lastHigh is { } prior && pivot.High > prior.Price &&
							pivot.Cci < prior.Cci && pivot.Cci > overbought;
						if (bearish) Flag(Sides.Sell);
						else rejected++;
						lastHigh = (pivot.High, pivot.Cci);
					}
					window.RemoveAt(0);
				}

				if (strategy.Position > 0m && upCross || strategy.Position < 0m && downCross)
				{
					expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
					expectedVolume = Math.Abs(strategy.Position);
					zeroExits++;
				}
				else if (strategy.Position == 0m)
				{
					if (expiredUnreplaced) expiredBeforeFlat++;
					expiredUnreplaced = false;
					if (signal is Sides side)
					{
						expectedSide = side;
						expectedVolume = strategy.Volume;
						if (side == Sides.Buy) longEntries++; else shortEntries++;
						if (signalAge > 0) delayedEntries++;
						signal = null;
					}
				}
				if (expectedSide != null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Type != OrderTypes.Market || order.Side != expectedSide || order.Volume != expectedVolume)
					if (violations.Count < 12)
						violations.Add("Every market order must match the independent price/CCI divergence flag, live for DivergencePeriod bars after its pivot is confirmed, or the favorable zero crossing, with full exit size.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		TestContext.WriteLine($"python={IsPython}, secondary={secondarySecurity}, CCI={cciPeriod}, width={pivotWidth}, levels={oversold}/{overbought}: formed={formedBars}, native={nativeScalars}, lowPivots={lowPivots}, highPivots={highPivots}, rejected={rejected}, longs={longEntries}, shorts={shortEntries}, delayedEntries={delayedEntries}, expiredBeforeFlat={expiredBeforeFlat}, zeroExits={zeroExits}, expectedOrders={expectedOrders}, actualOrders={actualOrders}");
		AreEqual(expectedOrders, actualOrders);
		AreEqual(formedBars, nativeScalars);
		IsTrue(formedBars > 100 && lowPivots > 0 && highPivots > 0 && rejected > 0 &&
			longEntries + shortEntries > 0 && zeroExits > 0);
		if (requireCarriedSignals)
			IsTrue(delayedEntries > 0 && expiredBeforeFlat > 0,
				"The archive must exercise a divergence entered on a later flat bar and one that expired after DivergencePeriod bars before the position went flat.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));
	}

	[TestMethod]
	[TestCategory("Shard04")]
	public Task S0068_PercentStopFlattensBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(CciDivergence, TimeSpan.FromDays(31), expectedFrame: TimeSpan.FromMinutes(15));

	private const string BollingerBandReversal = "0069_Bollinger_Band_Reversal";

	[TestMethod]
	[TestCategory("Shard05")]
	[DataRow(20, 2.0, false)]
	[DataRow(10, 1.5, true)]
	public async Task S0069_IndependentBandsOpposingCandleAndMiddleExit(int period,
		double deviation, bool secondarySecurity)
	{
		var closes = new Queue<decimal>();
		var bars = 0;
		var filteredOuterCloses = 0;
		var longEntries = 0;
		var shortEntries = 0;
		var middleExits = 0;
		var expectedOrders = 0;
		var actualOrders = 0;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var violations = new List<string>();
		await Replay(BollingerBandReversal, (strategy, secondary) =>
		{
			if (secondarySecurity) strategy.Security = secondary;
			AreEqual(20, strategy.Parameters["BollingerPeriod"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["BollingerDeviation"].Value));
			AreEqual(14, strategy.Parameters["AtrPeriod"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["AtrMultiplier"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "BollingerPeriod", period);
			SetParam(strategy, "BollingerDeviation", Convert.ToDecimal(deviation));
			SetParam(strategy, "AtrMultiplier", 0m);
			strategy.Volume = 2m;
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished || candle.SecurityId != strategy.Security.Id.ToSecurityId()) return;
				bars++;
				expectedSide = null;
				closes.Enqueue(candle.ClosePrice);
				if (closes.Count > period) closes.Dequeue();
				if (bars < Math.Max(period, 14)) return;
				var middle = closes.Average();
				var variance = closes.Sum(close => (close - middle) * (close - middle)) / period;
				var distance = Convert.ToDecimal(deviation) * (decimal)Math.Sqrt((double)variance);
				var upper = middle + distance;
				var lower = middle - distance;
				if (strategy.Position > 0m && candle.ClosePrice >= middle)
				{
					expectedSide = Sides.Sell;
					expectedVolume = strategy.Position;
					middleExits++;
				}
				else if (strategy.Position < 0m && candle.ClosePrice <= middle)
				{
					expectedSide = Sides.Buy;
					expectedVolume = Math.Abs(strategy.Position);
					middleExits++;
				}
				else if (strategy.Position == 0m && candle.ClosePrice < lower)
				{
					if (candle.ClosePrice > candle.OpenPrice)
					{
						expectedSide = Sides.Buy;
						expectedVolume = strategy.Volume;
						longEntries++;
					}
					else filteredOuterCloses++;
				}
				else if (strategy.Position == 0m && candle.ClosePrice > upper)
				{
					if (candle.ClosePrice < candle.OpenPrice)
					{
						expectedSide = Sides.Sell;
						expectedVolume = strategy.Volume;
						shortEntries++;
					}
					else filteredOuterCloses++;
				}
				if (expectedSide is not null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Type != OrderTypes.Market || order.Side != expectedSide || order.Volume != expectedVolume)
					if (violations.Count < 12)
						violations.Add("Every order must match independent current-inclusive Bollinger bands, the opposing candle direction, or a full middle-band exit; no cooldown.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		TestContext.WriteLine($"python={IsPython}, secondary={secondarySecurity}, bands={period}/{deviation}: bars={bars}, filtered={filteredOuterCloses}, longs={longEntries}, shorts={shortEntries}, middleExits={middleExits}, expectedOrders={expectedOrders}, actualOrders={actualOrders}");
		AreEqual(expectedOrders, actualOrders);
		IsTrue(bars > 100 && filteredOuterCloses > 0 && longEntries > 0 && shortEntries > 0 && middleExits > 0);
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public Task S0069_AtrDistancesAndRiskParameterChangeRealExecutions()
		=> CheckAtrDistancesAndRiskParameterChangeRealExecutions(BollingerBandReversal, "Bollinger reversal entry");

	private const string MorningStar = "0070_Morning_Star";

	[TestMethod]
	[TestCategory("Shard06")]
	[DataRow(false, 1.0)]
	[DataRow(true, 0.0)]
	public async Task S0070_IndependentThreeBodiesMidpointTargetAndStop(bool secondarySecurity, double stopBuffer)
	{
		var recent = new List<ICandleMessage>();
		decimal? activeStop = null;
		decimal? activeTarget = null;
		decimal? lastBid = null;
		decimal? lastFinishedLow = null;
		decimal? lastFinishedHigh = null;
		DateTime? lastProcessedOpenTime = null;
		var candidates = 0;
		var rejectedBodies = 0;
		var expectedEntries = 0;
		var entries = 0;
		var exits = 0;
		var stopExits = 0;
		var targetExits = 0;
		var quoteExits = 0;
		var barExits = 0;
		var expectedEntry = false;
		var possibleLateEntry = false;
		decimal? possibleLateStop = null;
		decimal? possibleLateTarget = null;
		var violations = new List<string>();
		await Replay(MorningStar, (strategy, secondary) =>
		{
			if (secondarySecurity) strategy.Security = secondary;
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			AreEqual(1m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
			SetParam(strategy, "StopLossPercent", Convert.ToDecimal(stopBuffer));
			strategy.Volume = 2m;
			strategy.Level1Received += (_, message) =>
			{
				if (message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid && bid > 0m)
					lastBid = bid;
			};
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished ||
					candle.SecurityId != strategy.Security.Id.ToSecurityId() ||
					candle.DataType != TimeSpan.FromMinutes(5).TimeFrame()) return;
				if (lastProcessedOpenTime == candle.OpenTime) return;
				lastProcessedOpenTime = candle.OpenTime;
				if (expectedEntry && violations.Count < 12)
					violations.Add($"Eligible Morning Star did not enter by {strategy.CurrentTime:o}.");
				expectedEntry = false;
				possibleLateEntry = false;
				possibleLateStop = possibleLateTarget = null;
				lastFinishedLow = candle.LowPrice;
				lastFinishedHigh = candle.HighPrice;
				if (strategy.Position > 0m &&
					(activeStop is decimal stop && candle.LowPrice <= stop ||
					 activeTarget is decimal target && candle.HighPrice > target))
					return;
				if (strategy.Position == 0m && recent.Count == 2)
				{
					var first = recent[0];
					var middle = recent[1];
					var firstBody = first.OpenPrice - first.ClosePrice;
					var middleBody = Math.Abs(middle.ClosePrice - middle.OpenPrice);
					var thirdBody = candle.ClosePrice - candle.OpenPrice;
					if (firstBody > 0m && middleBody < firstBody / 2m && thirdBody > 0m)
					{
						candidates++;
						if (thirdBody < firstBody / 2m || candle.ClosePrice <=
							(first.HighPrice + first.LowPrice) / 2m)
							rejectedBodies++;
						else
						{
							expectedEntry = possibleLateEntry = true;
							expectedEntries++;
							possibleLateStop = activeStop = middle.LowPrice *
								(1m - Convert.ToDecimal(stopBuffer) / 100m);
							possibleLateTarget = activeTarget = candle.HighPrice;
						}
					}
				}
				recent.Add(candle);
				if (recent.Count > 2) recent.RemoveAt(0);
			};
			strategy.OrderRegistering += order =>
			{
				if (order.Side == Sides.Buy)
				{
					entries++;
					if (!expectedEntry && possibleLateEntry && strategy.Position == 0m)
					{
						expectedEntries++;
						activeStop = possibleLateStop;
						activeTarget = possibleLateTarget;
					}
					if (!expectedEntry && !possibleLateEntry || strategy.Position != 0m ||
						order.Volume != strategy.Volume || order.Type != OrderTypes.Market)
						if (violations.Count < 12)
							violations.Add($"Unexpected long at {strategy.CurrentTime:o}: pattern={expectedEntry}, position={strategy.Position}, volume={order.Volume}.");
					expectedEntry = possibleLateEntry = false;
					lastBid = null;
					lastFinishedLow = lastFinishedHigh = null;
				}
				else
				{
					exits++;
					var bidStop = lastBid is decimal bid && activeStop is decimal stop && bid <= stop;
					var bidTarget = lastBid is decimal highBid && activeTarget is decimal target &&
						highBid > target;
					var barStop = lastFinishedLow is decimal low && activeStop is decimal stopLow && low <= stopLow;
					var barTarget = lastFinishedHigh is decimal high && activeTarget is decimal targetHigh &&
						high > targetHigh;
					if (bidStop || barStop) stopExits++;
					else if (bidTarget || barTarget) targetExits++;
					if (bidStop || bidTarget) quoteExits++;
					if (barStop || barTarget) barExits++;
					if (strategy.Position <= 0m || order.Volume != strategy.Position ||
						order.Type != OrderTypes.Market ||
						!bidStop && !bidTarget && !barStop && !barTarget)
						if (violations.Count < 12)
							violations.Add("Every sell must fully close the long on best-bid/finished-bar target or middle-candle-low stop, never open a short.");
					recent.Clear();
					activeStop = activeTarget = null;
					lastBid = lastFinishedLow = lastFinishedHigh = null;
				}
			};
		}, TimeSpan.FromDays(31));
		TestContext.WriteLine($"python={IsPython}, secondary={secondarySecurity}, stop={stopBuffer}: candidates={candidates}, rejected={rejectedBodies}, expectedEntries={expectedEntries}, entries={entries}, exits={exits}, targets={targetExits}, stops={stopExits}, quotes={quoteExits}, bars={barExits}");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));
		AreEqual(expectedEntries, entries);
		IsTrue(candidates > 0 && rejectedBodies > 0 && entries > 0 && exits > 0 &&
			targetExits > 0 && stopExits > 0);
	}

	private const string EveningStar = "0071_Evening_Star";

	[TestMethod]
	[TestCategory("Shard07")]
	[DataRow(false, 1.0)]
	[DataRow(true, 0.0)]
	public async Task S0071_IndependentThreeBodiesMidpointTargetAndStop(bool secondarySecurity, double stopBuffer)
	{
		var recent = new List<ICandleMessage>();
		decimal? activeStop = null;
		decimal? activeTarget = null;
		decimal? lastAsk = null;
		decimal? lastFinishedHigh = null;
		decimal? lastFinishedLow = null;
		DateTime? lastProcessedOpenTime = null;
		DateTime? lastExitOpenTime = null;
		var candidates = 0;
		var rejectedMidpoints = 0;
		var expectedEntries = 0;
		var smallThirdEntries = 0;
		var entriesAcrossExits = 0;
		var entries = 0;
		var exits = 0;
		var stopExits = 0;
		var targetExits = 0;
		var quoteExits = 0;
		var barExits = 0;
		var expectedEntry = false;
		var violations = new List<string>();
		await Replay(EveningStar, (strategy, secondary) =>
		{
			if (secondarySecurity) strategy.Security = secondary;
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			AreEqual(1m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
			SetParam(strategy, "StopLossPercent", Convert.ToDecimal(stopBuffer));
			strategy.Volume = 2m;
			strategy.Level1Received += (_, message) =>
			{
				if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask && ask > 0m)
					lastAsk = ask;
			};
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished ||
					candle.SecurityId != strategy.Security.Id.ToSecurityId() ||
					candle.DataType != TimeSpan.FromMinutes(5).TimeFrame()) return;
				if (lastProcessedOpenTime == candle.OpenTime) return;
				lastProcessedOpenTime = candle.OpenTime;
				if (expectedEntry && violations.Count < 12)
					violations.Add($"Eligible Evening Star did not enter by {strategy.CurrentTime:o}.");
				expectedEntry = false;
				lastFinishedHigh = candle.HighPrice;
				lastFinishedLow = candle.LowPrice;
				if (strategy.Position == 0m && recent.Count == 2)
				{
					var first = recent[0];
					var middle = recent[1];
					var firstBody = first.ClosePrice - first.OpenPrice;
					var middleBody = Math.Abs(middle.ClosePrice - middle.OpenPrice);
					var thirdBody = candle.OpenPrice - candle.ClosePrice;
					if (firstBody > 0m && middleBody < firstBody / 2m && thirdBody > 0m)
					{
						candidates++;
						if (candle.ClosePrice >= (first.HighPrice + first.LowPrice) / 2m)
							rejectedMidpoints++;
						else
						{
							expectedEntry = true;
							expectedEntries++;
							if (thirdBody < firstBody / 2m)
								smallThirdEntries++;
							if (lastExitOpenTime is DateTime exitTime && first.OpenTime <= exitTime)
								entriesAcrossExits++;
							activeStop = middle.HighPrice *
								(1m + Convert.ToDecimal(stopBuffer) / 100m);
							activeTarget = candle.LowPrice;
						}
					}
				}
				recent.Add(candle);
				if (recent.Count > 2) recent.RemoveAt(0);
			};
			strategy.OrderRegistering += order =>
			{
				if (order.Side == Sides.Sell)
				{
					entries++;
					if (!expectedEntry || strategy.Position != 0m ||
						order.Volume != strategy.Volume || order.Type != OrderTypes.Market)
						if (violations.Count < 12)
							violations.Add($"Unexpected short at {strategy.CurrentTime:o}: pattern={expectedEntry}, position={strategy.Position}, volume={order.Volume}.");
					expectedEntry = false;
					lastAsk = null;
					lastFinishedHigh = lastFinishedLow = null;
				}
				else
				{
					exits++;
					var askStop = lastAsk is decimal ask && activeStop is decimal stop && ask >= stop;
					var askTarget = lastAsk is decimal lowAsk && activeTarget is decimal target &&
						lowAsk < target;
					var barStop = lastFinishedHigh is decimal high && activeStop is decimal stopHigh && high >= stopHigh;
					var barTarget = lastFinishedLow is decimal low && activeTarget is decimal targetLow &&
						low < targetLow;
					if (askStop || barStop) stopExits++;
					else if (askTarget || barTarget) targetExits++;
					if (askStop || askTarget) quoteExits++;
					if (barStop || barTarget) barExits++;
					if (strategy.Position >= 0m || order.Volume != Math.Abs(strategy.Position) ||
						order.Type != OrderTypes.Market ||
						!askStop && !askTarget && !barStop && !barTarget)
						if (violations.Count < 12)
							violations.Add("Every buy must fully cover the short on best-ask/finished-bar target or middle-candle-high stop, never open a long.");
					lastExitOpenTime = lastProcessedOpenTime;
					activeStop = activeTarget = null;
					lastAsk = lastFinishedHigh = lastFinishedLow = null;
				}
			};
		}, TimeSpan.FromDays(31));
		TestContext.WriteLine($"python={IsPython}, secondary={secondarySecurity}, stop={stopBuffer}: candidates={candidates}, rejectedMidpoints={rejectedMidpoints}, expectedEntries={expectedEntries}, smallThird={smallThirdEntries}, acrossExits={entriesAcrossExits}, entries={entries}, exits={exits}, targets={targetExits}, stops={stopExits}, quotes={quoteExits}, bars={barExits}");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));
		AreEqual(expectedEntries, entries);
		IsTrue(candidates > 0 && rejectedMidpoints > 0 && entries > 0 && exits > 0 &&
			targetExits > 0 && stopExits > 0);
		IsTrue(smallThirdEntries > 0, "The replay must enter on a bearish third candle whose body is under half the first body.");
		IsTrue(entriesAcrossExits > 0, "The replay must enter on a pattern that began before the preceding exit.");
	}

	private const string DojiReversal = "0072_Doji_Reversal";

	[TestMethod]
	[TestCategory("Shard00")]
	[DataRow(false, 0.1)]
	[DataRow(true, 0.2)]
	public async Task S0072_IndependentDojiTrendAndFarExtremeTarget(bool secondarySecurity, double threshold)
	{
		ICandleMessage older = null;
		ICandleMessage previous = null;
		decimal? targetHigh = null;
		decimal? targetLow = null;
		decimal? lastBid = null;
		decimal? lastAsk = null;
		decimal? lastFinishedHigh = null;
		decimal? lastFinishedLow = null;
		DateTime? lastProcessedOpenTime = null;
		Sides? expectedEntrySide = null;
		var dojis = 0;
		var noTrend = 0;
		var expectedEntries = 0;
		var longEntries = 0;
		var shortEntries = 0;
		var targetExits = 0;
		var quoteExits = 0;
		var barExits = 0;
		var violations = new List<string>();
		await Replay(DojiReversal, (strategy, secondary) =>
		{
			if (secondarySecurity) strategy.Security = secondary;
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			AreEqual(0.1m, Convert.ToDecimal(strategy.Parameters["DojiThreshold"].Value));
			AreEqual(1m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
			SetParam(strategy, "DojiThreshold", Convert.ToDecimal(threshold));
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.Volume = 2m;
			strategy.Level1Received += (_, message) =>
			{
				if (message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid && bid > 0m)
					lastBid = bid;
				if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask && ask > 0m)
					lastAsk = ask;
			};
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished ||
					candle.SecurityId != strategy.Security.Id.ToSecurityId() ||
					candle.DataType != TimeSpan.FromMinutes(5).TimeFrame()) return;
				if (lastProcessedOpenTime == candle.OpenTime) return;
				lastProcessedOpenTime = candle.OpenTime;
				if (expectedEntrySide is not null && violations.Count < 12)
					violations.Add($"Eligible doji did not enter by {strategy.CurrentTime:o}.");
				expectedEntrySide = null;
				lastFinishedHigh = candle.HighPrice;
				lastFinishedLow = candle.LowPrice;
				if (strategy.Position == 0m && older is not null && previous is not null)
				{
					var range = candle.HighPrice - candle.LowPrice;
					if (range > 0m &&
						Math.Abs(candle.OpenPrice - candle.ClosePrice) / range < Convert.ToDecimal(threshold))
					{
						dojis++;
						if (previous.ClosePrice < older.ClosePrice)
						{
							expectedEntrySide = Sides.Buy;
							expectedEntries++;
							targetHigh = candle.HighPrice;
							targetLow = null;
						}
						else if (previous.ClosePrice > older.ClosePrice)
						{
							expectedEntrySide = Sides.Sell;
							expectedEntries++;
							targetLow = candle.LowPrice;
							targetHigh = null;
						}
						else noTrend++;
					}
				}
				older = previous;
				previous = candle;
			};
			strategy.OrderRegistering += order =>
			{
				if (strategy.Position == 0m)
				{
					if (order.Side == Sides.Buy) longEntries++;
					else shortEntries++;
					if (order.Type != OrderTypes.Market || order.Side != expectedEntrySide ||
						order.Volume != strategy.Volume)
						if (violations.Count < 12)
							violations.Add("Entry requires a strict nonzero-range doji and opposite move between the two preceding closes; no hidden cooldown.");
					expectedEntrySide = null;
					lastBid = lastAsk = null;
					lastFinishedHigh = lastFinishedLow = null;
				}
				else
				{
					var longPosition = strategy.Position > 0m;
					var quoteTarget = longPosition
						? lastBid is decimal bid && targetHigh is decimal high && bid > high
						: lastAsk is decimal ask && targetLow is decimal low && ask < low;
					var barTarget = longPosition
						? lastFinishedHigh is decimal barHigh && targetHigh is decimal barTargetHigh && barHigh > barTargetHigh
						: lastFinishedLow is decimal barLow && targetLow is decimal barTargetLow && barLow < barTargetLow;
					if (quoteTarget) quoteExits++;
					if (barTarget) barExits++;
					if (quoteTarget || barTarget) targetExits++;
					if (order.Type != OrderTypes.Market ||
						order.Side != (longPosition ? Sides.Sell : Sides.Buy) ||
						order.Volume != Math.Abs(strategy.Position) ||
						!quoteTarget && !barTarget)
						if (violations.Count < 12)
							violations.Add("Without percent protection, every exit must fully close at the far doji extreme using executable bid/ask or a finished-bar fallback.");
					targetHigh = targetLow = null;
					lastBid = lastAsk = null;
					lastFinishedHigh = lastFinishedLow = null;
				}
			};
		}, TimeSpan.FromDays(31));
		TestContext.WriteLine($"python={IsPython}, secondary={secondarySecurity}, threshold={threshold}: dojis={dojis}, equalPriorCloses={noTrend}, expectedEntries={expectedEntries}, longs={longEntries}, shorts={shortEntries}, targets={targetExits}, quotes={quoteExits}, bars={barExits}");
		AreEqual(expectedEntries, longEntries + shortEntries);
		IsTrue(dojis > 0 && longEntries > 0 && shortEntries > 0 && targetExits > 0);
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public Task S0072_PercentStopFlattensBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(DojiReversal, TimeSpan.FromDays(31), expectedFrame: TimeSpan.FromMinutes(5), expectedStopPercent: 1m);

	private const string KeltnerChannelReversal = "0073_Keltner_Channel_Reversal";

	[TestMethod]
	[TestCategory("Shard01")]
	[DataRow(20, 14, 2.0, false)]
	[DataRow(10, 7, 1.5, true)]
	public async Task S0073_IndependentEmaAtrOpposingCandleAndMiddleExit(int emaPeriod,
		int atrPeriod, double width, bool secondarySecurity)
	{
		var ema = new ImpulseEma(emaPeriod);
		var atr = 0m;
		var bars = 0;
		decimal? previousClose = null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var filteredOuterCloses = 0;
		var longEntries = 0;
		var shortEntries = 0;
		var middleExits = 0;
		var violations = new List<string>();
		await Replay(KeltnerChannelReversal, (strategy, secondary) =>
		{
			if (secondarySecurity) strategy.Security = secondary;
			AreEqual(20, strategy.Parameters["EmaPeriod"].Value);
			AreEqual(14, strategy.Parameters["AtrPeriod"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["AtrMultiplier"].Value));
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["StopLossAtrMultiplier"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "EmaPeriod", emaPeriod);
			SetParam(strategy, "AtrPeriod", atrPeriod);
			SetParam(strategy, "AtrMultiplier", Convert.ToDecimal(width));
			SetParam(strategy, "StopLossAtrMultiplier", 0m);
			strategy.Volume = 2m;
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished || candle.SecurityId != strategy.Security.Id.ToSecurityId()) return;
				expectedSide = null;
				var middle = ema.Add(candle.ClosePrice);
				var tr = previousClose is decimal close
					? Math.Max(candle.HighPrice - candle.LowPrice,
						Math.Max(Math.Abs(candle.HighPrice - close), Math.Abs(candle.LowPrice - close)))
					: candle.HighPrice - candle.LowPrice;
				bars++;
				var length = Math.Min(bars, atrPeriod);
				atr = (atr * (length - 1) + tr) / length;
				previousClose = candle.ClosePrice;
				if (!ema.Formed || bars < atrPeriod) return;
				var lower = middle - atr * Convert.ToDecimal(width);
				var upper = middle + atr * Convert.ToDecimal(width);
				var price = candle.ClosePrice;
				if (strategy.Position > 0m && price >= middle)
				{
					expectedSide = Sides.Sell;
					expectedVolume = strategy.Position;
					middleExits++;
				}
				else if (strategy.Position < 0m && price <= middle)
				{
					expectedSide = Sides.Buy;
					expectedVolume = Math.Abs(strategy.Position);
					middleExits++;
				}
				else if (strategy.Position == 0m && price < lower)
				{
					if (price > candle.OpenPrice)
					{
						expectedSide = Sides.Buy;
						expectedVolume = strategy.Volume;
						longEntries++;
					}
					else filteredOuterCloses++;
				}
				else if (strategy.Position == 0m && price > upper)
				{
					if (price < candle.OpenPrice)
					{
						expectedSide = Sides.Sell;
						expectedVolume = strategy.Volume;
						shortEntries++;
					}
					else filteredOuterCloses++;
				}
				if (expectedSide is not null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Type != OrderTypes.Market || order.Side != expectedSide || order.Volume != expectedVolume)
					if (violations.Count < 12)
						violations.Add("Every order must match independent current-inclusive EMA and Wilder ATR bands, an opposing candle, or a full middle-EMA exit; no shared-length indicator or cooldown.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		TestContext.WriteLine($"python={IsPython}, secondary={secondarySecurity}, EMA={emaPeriod}, ATR={atrPeriod}, width={width}: bars={bars}, filtered={filteredOuterCloses}, longs={longEntries}, shorts={shortEntries}, middleExits={middleExits}, expectedOrders={expectedOrders}, actualOrders={actualOrders}");
		AreEqual(expectedOrders, actualOrders);
		IsTrue(bars > 100 && filteredOuterCloses > 0 && longEntries > 0 && shortEntries > 0 && middleExits > 0);
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));
	}

	[TestMethod]
	[TestCategory("Shard01")]
	public Task S0073_StopAtrDistancesAndRiskParameterChangeRealExecutions()
		=> CheckAtrDistancesAndRiskParameterChangeRealExecutions(KeltnerChannelReversal, "Keltner reversal entry",
			stopParameter: "StopLossAtrMultiplier");

	private const string WilliamsDivergence = "0074_Williams_R_Divergence";

	[TestMethod]
	[TestCategory("Shard02")]
	[DataRow(14, 5, false)]
	[DataRow(7, 3, true)]
	public async Task S0074_IndependentCloseDivergenceAndExtremeExits(int period, int divergencePeriod, bool secondarySecurity)
	{
		var bars = new Queue<ICandleMessage>();
		var history = new Queue<(decimal Close, decimal WilliamsR)>();
		var expectedValue = 0m;
		var valueReady = false;
		var formedBars = 0;
		var flatRanges = 0;
		var nativeScalars = 0;
		var bullishDivergences = 0;
		var bearishDivergences = 0;
		var rejected = 0;
		var longEntries = 0;
		var shortEntries = 0;
		var extremeExits = 0;
		var expectedOrders = 0;
		var actualOrders = 0;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var violations = new List<string>();
		await Replay(WilliamsDivergence, (strategy, secondary) =>
		{
			if (secondarySecurity) strategy.Security = secondary;
			AreEqual(14, strategy.Parameters["WilliamsRPeriod"].Value);
			AreEqual(5, strategy.Parameters["DivergencePeriod"].Value);
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
			SetParam(strategy, "WilliamsRPeriod", period);
			SetParam(strategy, "DivergencePeriod", divergencePeriod);
			SetParam(strategy, "StopLossPercent", 0m);
			strategy.Volume = 2m;
			strategy.Indicators.Added += indicator =>
			{
				if (indicator is not WilliamsR) return;
				indicator.Changed += (_, output) =>
				{
					if (!output.IsFinal || !indicator.IsFormed || output.IsEmpty || !valueReady) return;
					nativeScalars++;
					if (Math.Abs(output.GetValue<decimal>() - expectedValue) > 0.000001m && violations.Count < 12)
						violations.Add("Native Williams %R must match the independent current-inclusive high/low value on every formed candle.");
				};
			};
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished || candle.SecurityId != strategy.Security.Id.ToSecurityId()) return;
				expectedSide = null;
				valueReady = false;
				bars.Enqueue(candle);
				if (bars.Count > period) bars.Dequeue();
				if (bars.Count < period) return;
				var high = bars.Max(bar => bar.HighPrice);
				var low = bars.Min(bar => bar.LowPrice);
				if (high == low)
				{
					flatRanges++;
					return;
				}
				var value = -100m * (high - candle.ClosePrice) / (high - low);
				expectedValue = value;
				valueReady = true;
				formedBars++;

				var bullish = false;
				var bearish = false;
				if (history.Count == divergencePeriod)
				{
					var prior = history.Peek();
					if (candle.ClosePrice < prior.Close && value > prior.WilliamsR)
					{
						bullish = value < -80m;
						if (bullish) bullishDivergences++;
						else rejected++;
					}
					if (candle.ClosePrice > prior.Close && value < prior.WilliamsR)
					{
						bearish = value > -20m;
						if (bearish) bearishDivergences++;
						else rejected++;
					}
				}
				history.Enqueue((candle.ClosePrice, value));
				if (history.Count > divergencePeriod) history.Dequeue();

				if (strategy.Position > 0m && value >= -20m || strategy.Position < 0m && value <= -80m)
				{
					expectedSide = strategy.Position > 0m ? Sides.Sell : Sides.Buy;
					expectedVolume = Math.Abs(strategy.Position);
					extremeExits++;
				}
				else if (strategy.Position == 0m)
				{
					if (bullish) { expectedSide = Sides.Buy; longEntries++; }
					else if (bearish) { expectedSide = Sides.Sell; shortEntries++; }
					if (expectedSide != null) expectedVolume = strategy.Volume;
				}
				if (expectedSide != null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Type != OrderTypes.Market || order.Side != expectedSide || order.Volume != expectedVolume)
					if (violations.Count < 12)
						violations.Add("Every market order must match the independent close/%R divergence against the reading DivergencePeriod bars back in the extreme zone or the opposite-extreme full exit.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		TestContext.WriteLine($"python={IsPython}, secondary={secondarySecurity}, %R={period}, divergence={divergencePeriod}: formed={formedBars}, flat={flatRanges}, native={nativeScalars}, bullish={bullishDivergences}, bearish={bearishDivergences}, rejected={rejected}, longs={longEntries}, shorts={shortEntries}, extremeExits={extremeExits}, expectedOrders={expectedOrders}, actualOrders={actualOrders}");
		AreEqual(0, flatRanges, "The independent oracle assumes the packaged archive has no flat %R lookback.");
		AreEqual(expectedOrders, actualOrders);
		AreEqual(formedBars, nativeScalars);
		IsTrue(formedBars > 100 && bullishDivergences > 0 && bearishDivergences > 0 && rejected > 0 &&
			longEntries + shortEntries > 0 && extremeExits > 0);
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));
	}

	[TestMethod]
	[TestCategory("Shard02")]
	public Task S0074_PercentStopFlattensBetweenFinishedBars()
		=> CheckPercentStopBetweenBars(WilliamsDivergence, TimeSpan.FromDays(31));

	private const string FalseBreakoutTrap = "0105_False_Breakout_Trap";

	[TestMethod]
	[TestCategory("Shard01")]
	public async Task S0105_StopBeyondFailedBreakoutLevel()
	{
		const double stopPercent = 0.05;
		var stopFraction = Convert.ToDecimal(stopPercent) / 100m;
		var period = 0;
		var highs = new List<decimal>();
		var lows = new List<decimal>();
		var fromQuote = false;
		decimal? bid = null;
		decimal? ask = null;
		var barHigh = 0m;
		var barLow = 0m;
		var rangeHigh = 0m;
		var rangeLow = 0m;
		var falseUp = false;
		var falseDown = false;
		decimal? stop = null;
		Order exitOrder = null;
		Sides? expectedEntry = null;
		var candleExitExpected = false;
		var quoteStopExpected = false;
		var bars = 0;
		int? lastEntryBar = null;
		var longEntries = 0;
		var shortEntries = 0;
		var quoteStops = 0;
		var barStops = 0;
		var signalExits = 0;
		var quickReentries = 0;
		var violations = new List<string>();

		void violate(DateTime time, string text)
		{
			if (violations.Count < 12)
				violations.Add($"{time:O}: {text}");
		}

		bool exitPending() => exitOrder is not null && exitOrder.State is not (OrderStates.Done or OrderStates.Failed);

		await Replay(FalseBreakoutTrap, (strategy, _) =>
		{
			IsTrue(strategy.Parameters.TryGetValue("StopLoss", out var stopLoss), "README promises a configurable percent stop loss.");
			AreEqual(2m, Convert.ToDecimal(stopLoss.Value));
			AreEqual(TimeSpan.FromMinutes(15).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(1).TimeFrame());
			SetParam(strategy, "StopLoss", Convert.ToDecimal(stopPercent));
			period = Convert.ToInt32(strategy.Parameters["LookbackPeriod"].Value);
			var security = strategy.Security.Id.ToSecurityId();

			strategy.CandleReceived += (_, candle) =>
			{
				if (strategy.ProcessState != ProcessStates.Started || candle.State != CandleStates.Finished || candle.SecurityId != security)
					return;
				if (expectedEntry is not null)
					violate(strategy.CurrentTime, $"The {expectedEntry} entry on the previous candle's false breakout was not placed.");
				if (candleExitExpected)
					violate(strategy.CurrentTime, "The stop or opposite false breakout on the previous candle did not close the position.");
				if (quoteStopExpected)
					violate(strategy.CurrentTime, "A quote at or beyond the stop did not close the position before the next candle.");
				expectedEntry = null;
				candleExitExpected = quoteStopExpected = false;
				fromQuote = false;
				bars++;
				barHigh = candle.HighPrice;
				barLow = candle.LowPrice;
				falseUp = falseDown = false;
				if (!strategy.IsFormedAndOnlineAndAllowTrading())
					return;
				highs.Add(candle.HighPrice);
				lows.Add(candle.LowPrice);
				if (highs.Count > period + 1)
				{
					highs.RemoveAt(0);
					lows.RemoveAt(0);
				}
				if (highs.Count < period + 1)
					return;
				rangeHigh = highs.Take(period).Max();
				rangeLow = lows.Take(period).Min();
				falseUp = candle.HighPrice > rangeHigh && candle.ClosePrice < rangeHigh;
				falseDown = candle.LowPrice < rangeLow && candle.ClosePrice > rangeLow;
				var position = strategy.Position;
				if (position == 0m)
					expectedEntry = falseDown ? Sides.Buy : falseUp ? Sides.Sell : (Sides?)null;
				else if (!exitPending())
					candleExitExpected = position > 0m
						? stop is decimal longStop && barLow <= longStop || falseUp
						: stop is decimal shortStop && barHigh >= shortStop || falseDown;
			};

			strategy.Level1Received += (_, quote) =>
			{
				if (strategy.ProcessState != ProcessStates.Started || quote.SecurityId != security)
					return;
				var quoteBid = quote.TryGetDecimal(Level1Fields.BestBidPrice);
				var quoteAsk = quote.TryGetDecimal(Level1Fields.BestAskPrice);
				if (quoteBid > 0m)
					bid = quoteBid;
				if (quoteAsk > 0m)
					ask = quoteAsk;
				fromQuote = true;
				var position = strategy.Position;
				if (stop is decimal level && !exitPending() &&
					(position > 0m && quoteBid > 0m && quoteBid <= level || position < 0m && quoteAsk > 0m && quoteAsk >= level))
					quoteStopExpected = true;
			};

			strategy.OrderRegistering += order =>
			{
				var position = strategy.Position;
				if (position == 0m)
				{
					if (fromQuote || order.Side != expectedEntry || order.Type != OrderTypes.Market || order.Volume != strategy.Volume)
						violate(strategy.CurrentTime, "An entry must be one market order against a false breakout of the previous LookbackPeriod range on a finished candle.");
					expectedEntry = null;
					stop = order.Side == Sides.Buy ? rangeLow * (1m - stopFraction) : rangeHigh * (1m + stopFraction);
					if (order.Side == Sides.Buy)
						longEntries++;
					else
						shortEntries++;
					if (lastEntryBar is int previousEntry && bars - previousEntry < 500)
						quickReentries++;
					lastEntryBar = bars;
					return;
				}

				var isLong = position > 0m;
				var quoteStop = fromQuote && stop is decimal quoteLevel && (isLong ? bid <= quoteLevel : ask >= quoteLevel);
				var barStop = !fromQuote && stop is decimal barLevel && (isLong ? barLow <= barLevel : barHigh >= barLevel);
				var signal = !fromQuote && (isLong ? falseUp : falseDown);
				if (order.Side != (isLong ? Sides.Sell : Sides.Buy) || order.Volume != Math.Abs(position) || order.Type != OrderTypes.Market)
					violate(strategy.CurrentTime, "An exit must close the whole position at market without reversing it.");
				if (!quoteStop && !barStop && !signal)
					violate(strategy.CurrentTime, $"Exit before the stop {stop} beyond the failed breakout level or an opposite false breakout (bid={bid}, ask={ask}, bar={barLow}..{barHigh}).");
				if (exitPending())
					violate(strategy.CurrentTime, "An exit must not overlap a pending cover order.");
				if (quoteStop)
					quoteStops++;
				else if (barStop)
					barStops++;
				else if (signal)
					signalExits++;
				if (fromQuote)
					quoteStopExpected = false;
				else
					candleExitExpected = false;
				exitOrder = order;
				stop = null;
			};
		}, TimeSpan.FromDays(3));

		TestContext.WriteLine($"python={IsPython}: bars={bars}, longs={longEntries}, shorts={shortEntries}, quoteStops={quoteStops}, barStops={barStops}, signalExits={signalExits}, quickReentries={quickReentries}");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));
		IsTrue(longEntries > 0 && shortEntries > 0, "The archive must exercise false breakouts in both directions.");
		IsTrue(quoteStops > 0, "A quote beyond the failed breakout level must close the position between finished candles.");
		IsTrue(quickReentries > 0, "Entries must follow each false breakout, not wait out a cooldown of hundreds of candles.");
	}

	private const string KeltnerSeasonal = "0333_Keltner_Seasonal_Filter";

	// Monthly bias the example ships with, January first.
	private static readonly decimal[] _keltnerSeasonalBias = [0.8m, 0.3m, 0.6m, 0.7m, 0.2m, -0.3m, -0.1m, -0.4m, -0.8m, 0.1m, 0.9m, 0.7m];

	/// <summary>
	/// README: 5-minute candles, Keltner breakouts filtered by the month's seasonal bias, and stops that
	/// are ATR multiples. Every order is checked against an independent EMA / Wilder ATR oracle: a breakout
	/// entry freezes its stop AtrMultiplier x ATR away from the entry close, a finished close through that
	/// stop flattens, and a close back through the EMA flattens as well. Nothing else may trade.
	/// </summary>
	[TestMethod]
	[TestCategory("Shard05")]
	[DataRow(20, 14, 2.0, false)]
	[DataRow(10, 7, 1.0, true)]
	[DataRow(20, 14, 0.5, true)]
	public async Task S0333_SeasonalKeltnerBreakoutsWithAtrMultipleStops(int emaPeriod, int atrPeriod, double multiplier, bool tightStop)
	{
		var fiveMinutes = TimeSpan.FromMinutes(5).TimeFrame();
		var width = Convert.ToDecimal(multiplier);
		var threshold = 0.5m;
		var ema = new ImpulseEma(emaPeriod);
		var atr = 0m;
		var bars = 0;
		decimal? previousClose = null;
		var stop = 0m;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var entries = 0;
		var stopExits = 0;
		var stopExitsAboveEma = 0;
		var emaExits = 0;
		var seasonalRejections = 0;
		var otherFrames = 0;
		var violations = new List<string>();
		await Replay(KeltnerSeasonal, (strategy, _) =>
		{
			AreEqual(20, strategy.Parameters["EmaPeriod"].Value);
			AreEqual(14, strategy.Parameters["AtrPeriod"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["AtrMultiplier"].Value));
			AreEqual(threshold, Convert.ToDecimal(strategy.Parameters["SeasonalThreshold"].Value));
			AreEqual(fiveMinutes, strategy.Parameters["CandleType"].Value, "README: CandleType = TimeSpan.FromMinutes(5).TimeFrame(), intraday (5m) data.");
			SetParam(strategy, "EmaPeriod", emaPeriod);
			SetParam(strategy, "AtrPeriod", atrPeriod);
			SetParam(strategy, "AtrMultiplier", width);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished) return;
				expectedSide = null;
				if (candle.DataType != fiveMinutes) otherFrames++;
				var middle = ema.Add(candle.ClosePrice);
				var tr = previousClose is decimal close
					? Math.Max(candle.HighPrice - candle.LowPrice, Math.Max(Math.Abs(candle.HighPrice - close), Math.Abs(candle.LowPrice - close)))
					: candle.HighPrice - candle.LowPrice;
				bars++;
				var length = Math.Min(bars, atrPeriod);
				atr = (atr * (length - 1) + tr) / length;
				previousClose = candle.ClosePrice;
				if (!ema.Formed || bars < atrPeriod) return;
				var price = candle.ClosePrice;
				var position = strategy.Position;
				var bias = _keltnerSeasonalBias[candle.OpenTime.Month - 1];
				var upper = middle + atr * width;
				var lower = middle - atr * width;
				Sides? entry = null;
				if (bias > threshold)
				{
					if (price > upper && position <= 0m) entry = Sides.Buy;
				}
				else if (bias < -threshold)
				{
					if (price < lower && position >= 0m) entry = Sides.Sell;
				}
				if (stop > 0m && (position > 0m && price <= stop || position < 0m && price >= stop))
				{
					expectedSide = position > 0m ? Sides.Sell : Sides.Buy;
					expectedVolume = Math.Abs(position);
					stopExits++;
					if (position > 0m ? price >= middle : price <= middle) stopExitsAboveEma++;
					stop = 0m;
				}
				else if (entry is Sides side)
				{
					expectedSide = side;
					expectedVolume = strategy.Volume + Math.Abs(position);
					stop = side == Sides.Buy ? price - atr * width : price + atr * width;
					entries++;
				}
				else if (position > 0m && price < middle || position < 0m && price > middle)
				{
					expectedSide = position > 0m ? Sides.Sell : Sides.Buy;
					expectedVolume = Math.Abs(position);
					stop = 0m;
					emaExits++;
				}
				if (position == 0m && price < lower && bias > threshold) seasonalRejections++;
				if (expectedSide is not null) expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add($"{strategy.CurrentTime:O} {order.Type} {order.Side} {order.Volume}: expected {expectedSide?.ToString() ?? "no order"} {expectedVolume}. Orders come only from a seasonally allowed Keltner breakout, a finished close through the stop frozen AtrMultiplier x ATR from the entry close, or a close back through the EMA.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		AreEqual(0, otherFrames, "README: the strategy works on 5-minute candles.");
		AreEqual(expectedOrders, actualOrders, "Every entry, ATR stop and EMA exit the oracle predicts must be sent, and nothing else.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		IsTrue(entries > 0 && emaExits > 0, $"The archive must exercise breakout entries and EMA exits: entries={entries}, emaExits={emaExits}.");
		IsTrue(seasonalRejections > 0, "The archive must contain lower-band breakouts that March's bullish bias keeps from opening shorts.");
		if (tightStop)
			IsTrue(stopExitsAboveEma > 0, $"The ATR-multiple stop must flatten positions the EMA exit would have kept: stopExits={stopExits}, aboveEma={stopExitsAboveEma}.");
	}

	private const string SyntheticLending = "0402_Synthetic_Lending_Rates";

	[TestMethod]
	[TestCategory("Shard02")]
	public async Task S0402_InputsAreExplicitAndContradictoryInputsAreRejected()
	{
		SyntheticLendingRatesOracle oracle = null;

		await Replay(SyntheticLending, (strategy, secondary) =>
		{
			var primary = strategy.Security;

			foreach (var input in new[] { "FundingRateSecurity", "LendingRateSecurity", "OnChainLegSecurity" })
				IsNull(strategy.Parameters[input].Value, $"{input} is an explicit input; it must not default to any instrument.");

			IsFalse(strategy.Parameters.TryGetValue("DerivativeLegSecurity", out _), "The derivative market's leg is the strategy's own Security, not a second input beside it.");

			AreEqual(5m, Convert.ToDecimal(strategy.Parameters["EntryThreshold"].Value));
			AreEqual(1m, Convert.ToDecimal(strategy.Parameters["ExitThreshold"].Value));
			AreEqual(50m, Convert.ToDecimal(strategy.Parameters["SpreadCap"].Value));
			AreEqual(1_000_000m, Convert.ToDecimal(strategy.Parameters["MinLegTurnover"].Value));
			AreEqual(10_000m, Convert.ToDecimal(strategy.Parameters["LegNotional"].Value));
			AreEqual(24, strategy.Parameters["RebalanceBars"].Value);
			AreEqual(TimeSpan.FromHours(1).TimeFrame(), strategy.Parameters["CandleType"].Value);

			var start = typeof(Strategy).GetMethod("OnStarted2", BindingFlags.NonPublic | BindingFlags.Instance);
			var subscriptions = strategy.Subscriptions.Subscriptions.Count();

			void AssertRejected(string input)
			{
				var rejected = false;

				try
				{
					start.Invoke(strategy, [DateTime.MinValue]);
				}
				catch (TargetInvocationException exception)
				{
					IsTrue(exception.InnerException is InvalidOperationException && exception.InnerException.Message.StartsWith($"{input} ", StringComparison.Ordinal),
						$"Startup must be refused naming {input} first; got {exception.InnerException}.");
					rejected = true;
				}

				IsTrue(rejected, $"Startup must be refused while {input} is missing or contradicts another input.");
				AreEqual(subscriptions, strategy.Subscriptions.Subscriptions.Count(), "A refused start must not subscribe to any stream.");
			}

			strategy.Security = null;
			AssertRejected("Security");
			strategy.Security = primary;
			AssertRejected("FundingRateSecurity");
			SetParam(strategy, "FundingRateSecurity", primary);
			AssertRejected("LendingRateSecurity");
			SetParam(strategy, "LendingRateSecurity", secondary);
			AssertRejected("OnChainLegSecurity");

			// Two legs on one instrument net to nothing, and a series against itself has no spread.
			SetParam(strategy, "OnChainLegSecurity", primary);
			AssertRejected("OnChainLegSecurity");
			SetParam(strategy, "OnChainLegSecurity", secondary);
			SetParam(strategy, "LendingRateSecurity", primary);
			AssertRejected("LendingRateSecurity");
			SetParam(strategy, "LendingRateSecurity", secondary);

			// Only time-frame bars give the four streams common periods.
			SetParam(strategy, "CandleType", 1000.Tick());
			AssertRejected("CandleType");
			SetParam(strategy, "CandleType", TimeSpan.FromHours(1).TimeFrame());

			// An exit level at the entry level would close pairs the entry rule reopens; a cap at it leaves no spread to open on.
			SetParam(strategy, "ExitThreshold", 5m);
			AssertRejected("ExitThreshold");
			SetParam(strategy, "ExitThreshold", 1m);
			SetParam(strategy, "SpreadCap", 5m);
			AssertRejected("SpreadCap");

			// Mechanics fixture: packaged BTC and TON stand in for both rate series and both venue legs.
			oracle = SyntheticLendingRatesOracle.SpreadFixture(primary, secondary, primary, secondary, minLegTurnover: 0m);
			oracle.Attach(strategy);
		}, TimeSpan.FromDays(7));

		oracle.AssertEveryOrderMatched();
		TestContext.WriteLine(oracle.Summary());
	}

	[TestMethod]
	[TestCategory("Shard02")]
	[DataRow(true)]
	[DataRow(false)]
	public async Task S0402_LendsWhereTheRateIsHigherUntilTheSpreadRevertsOrHitsTheCap(bool derivativeRateHigher)
	{
		SyntheticLendingRatesOracle oracle = null;

		await Replay(SyntheticLending, (strategy, secondary) =>
		{
			var primary = strategy.Security;

			// Mechanics fixture: packaged BTC and TON stand in for both rate series and both venue legs;
			// swapping the two series makes the on-chain venue the one that pays more.
			oracle = derivativeRateHigher
				? SyntheticLendingRatesOracle.SpreadFixture(primary, secondary, primary, secondary, minLegTurnover: 0m)
				: SyntheticLendingRatesOracle.SpreadFixture(secondary, primary, primary, secondary, minLegTurnover: 0m);
			oracle.Attach(strategy);
		}, TimeSpan.FromDays(31));

		TestContext.WriteLine(oracle.Summary());
		oracle.AssertEveryOrderMatched();
		oracle.AssertBothLegsTraded();

		if (derivativeRateHigher)
			IsTrue(oracle.DerivativeLendingEntries > 0 && oracle.OnChainLendingEntries == 0, $"Every pair must lend in the derivative market, the venue that pays more. {oracle.Summary()}");
		else
			IsTrue(oracle.OnChainLendingEntries > 0 && oracle.DerivativeLendingEntries == 0, $"Every pair must lend on-chain, the venue that pays more. {oracle.Summary()}");

		IsTrue(oracle.ReversionExits > 0, $"A reverted spread must close an open pair on its own. {oracle.Summary()}");
		IsTrue(oracle.CapExits > 0, $"The spread cap must close an open pair on its own. {oracle.Summary()}");
		IsTrue(oracle.CapBlockedEntries > 0, $"The spread cap must keep a pair from opening. {oracle.Summary()}");
		IsTrue(oracle.Rebalances > 0, $"Held pairs must be resized back to the leg notional. {oracle.Summary()}");
	}

	[TestMethod]
	[TestCategory("Shard02")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task S0402_LiquidityFilterAndStopWatchEachLegsTradedValueInItsLots(bool tonIsDerivativeLeg)
	{
		// Mechanics fixture: TON trades in lots of 10 coins, so its sizes and traded value must be counted per lot.
		const decimal tonLot = 10m;

		// Half of the packaged TON hours trade less value than this, so liquidity comes and goes all month.
		var threshold = await SyntheticLendingHourlyTurnover(Paths.HistoryDefaultSecurity2, 0.5) * tonLot;
		SyntheticLendingRatesOracle oracle = null;

		await Replay(SyntheticLending, (strategy, secondary) =>
		{
			var primary = strategy.Security;
			secondary.Multiplier = tonLot;

			// Mechanics fixture: the spread always qualifies and never reverts or reaches the cap, so only liquidity
			// opens and closes pairs. The rates stay BTC over TON, so the derivative venue lends: the thin TON leg is
			// the one sold, or with the legs swapped the one bought.
			var (derivativeLeg, onChainLeg) = tonIsDerivativeLeg ? (secondary, primary) : (primary, secondary);
			oracle = new SyntheticLendingRatesOracle(primary, secondary, derivativeLeg, onChainLeg,
				entryThreshold: 0m, exitThreshold: -1_000_000m, spreadCap: 1_000_000m, minLegTurnover: threshold, legNotional: 10_000m, rebalanceBars: 6);
			oracle.Attach(strategy);
		}, TimeSpan.FromDays(31));

		TestContext.WriteLine($"liquidity threshold={threshold}; {oracle.Summary()}");
		oracle.AssertEveryOrderMatched();
		oracle.AssertBothLegsTraded();

		IsTrue(oracle.LiquidityExits > 0, $"A leg trading less than the threshold must close an open pair on its own. {oracle.Summary()}");
		IsTrue(oracle.LiquidityBlockedEntries > 0, $"A leg trading less than the threshold must keep a pair from opening. {oracle.Summary()}");
		IsTrue(oracle.DerivativeLendingEntries > 1, $"A pair must reopen once liquidity returns. {oracle.Summary()}");
	}

	[TestMethod]
	[TestCategory("Shard02")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task S0402_LiquidityStopClosesThePairOnAPeriodALegTradesNothingIn(bool tonIsDerivativeLeg)
	{
		SyntheticLendingRatesOracle oracle = null;

		// The packaged TON trades begin at 12:31 on March 1, so the replay runs to 18:00 to cover five hours of them.
		await Replay(SyntheticLending, (strategy, secondary) =>
		{
			var primary = strategy.Security;

			// Ten-second bars built from the packaged trades: many of them pass without a single TON trade.
			SetParam(strategy, "CandleType", TimeSpan.FromSeconds(10).TimeFrame());

			// Mechanics fixture: the spread always qualifies and never reverts or reaches the cap, and any trade at all is
			// liquid enough, so pairs open on periods both legs traded in and close on a period one of them did not.
			var (derivativeLeg, onChainLeg) = tonIsDerivativeLeg ? (secondary, primary) : (primary, secondary);
			oracle = new SyntheticLendingRatesOracle(primary, secondary, derivativeLeg, onChainLeg,
				entryThreshold: 0m, exitThreshold: -1_000_000m, spreadCap: 1_000_000m, minLegTurnover: 0.01m, legNotional: 10_000m, rebalanceBars: 6);
			oracle.Attach(strategy);
		}, TimeSpan.FromHours(18));

		TestContext.WriteLine(oracle.Summary());
		oracle.AssertEveryOrderMatched();
		oracle.AssertBothLegsTraded();

		IsTrue(oracle.NoTradeExits > 0, $"A period in which a leg trades nothing must close an open pair. {oracle.Summary()}");
		IsTrue(oracle.LiquidityBlockedEntries > 0, $"A period in which a leg trades nothing must keep a pair from opening. {oracle.Summary()}");
		IsTrue(oracle.DerivativeLendingEntries > 1, $"A pair must reopen once both legs trade again. {oracle.Summary()}");
	}

	[TestMethod]
	[TestCategory("Shard02")]
	public async Task S0402_ExitRulesStillCloseThePairWhenOnlyReducingPositionsIsAllowed()
	{
		SyntheticLendingRatesOracle oracle = null;
		var reducing = false;

		await Replay(SyntheticLending, (strategy, secondary) =>
		{
			var primary = strategy.Security;

			// Attached before the oracle, so a switch is seen by the oracle and the strategy on the same bar.
			strategy.CandleReceived += (_, _) =>
			{
				if (reducing || strategy.ProcessState != ProcessStates.Started || strategy.Orders.Any(order => order.State is not (OrderStates.Done or OrderStates.Failed)))
					return;

				// Once the first pair is held on both legs, trading is limited to reducing positions.
				if ((strategy.GetPositionValue(primary, strategy.Portfolio) ?? 0m) != 0m && (strategy.GetPositionValue(secondary, strategy.Portfolio) ?? 0m) != 0m)
				{
					strategy.TradingMode = StrategyTradingModes.ReducePositionOnly;
					reducing = true;
				}
			};

			// Mechanics fixture: packaged BTC and TON stand in for both rate series and both venue legs.
			oracle = SyntheticLendingRatesOracle.SpreadFixture(primary, secondary, primary, secondary, minLegTurnover: 0m);
			oracle.Attach(strategy);
		}, TimeSpan.FromDays(31));

		TestContext.WriteLine(oracle.Summary());
		oracle.AssertEveryOrderMatched();
		oracle.AssertBothLegsTraded();

		IsTrue(reducing, $"The first pair must be held on both legs before trading is limited. {oracle.Summary()}");
		AreEqual(1, oracle.DerivativeLendingEntries + oracle.OnChainLendingEntries, $"No pair may open while only reducing positions is allowed. {oracle.Summary()}");
		AreEqual(1, oracle.Exits, $"The exit rules must close the pair opened before trading was limited. {oracle.Summary()}");
		IsTrue(oracle.ModeBlockedEntries > 0, $"Entry signals must come and be passed over while only reducing positions is allowed. {oracle.Summary()}");
	}

	/// <summary>
	/// Traded value (volume times close) of the packaged hour below which the given share of that
	/// instrument's traded hours lie.
	/// </summary>
	private async Task<decimal> SyntheticLendingHourlyTurnover(string securityId, double share)
	{
		using var registry = new StorageRegistry { DefaultDrive = new LocalMarketDataDrive(Paths.FileSystem, Paths.HistoryDataPath) };
		var storage = registry.GetStorage(securityId.ToSecurityId(), TimeSpan.FromMinutes(1).TimeFrame());
		var hours = new SortedDictionary<long, (decimal Volume, decimal Close)>();

		foreach (var date in (await storage.GetDatesAsync().ToArrayAsync(CancellationToken)).Order())
		{
			await foreach (var message in storage.LoadAsync(date).WithCancellation(CancellationToken))
			{
				var candle = (ICandleMessage)message;
				var hour = candle.OpenTime.Ticks / TimeSpan.TicksPerHour;

				hours[hour] = hours.TryGetValue(hour, out var bar)
					? (bar.Volume + candle.TotalVolume, candle.ClosePrice)
					: (candle.TotalVolume, candle.ClosePrice);
			}
		}

		var turnovers = hours.Values.Select(bar => bar.Volume * bar.Close).Where(value => value > 0m).Order().ToArray();
		IsTrue(turnovers.Length > 0, $"The packaged history has no traded hour of {securityId}.");

		return turnovers[(int)(turnovers.Length * share)];
	}

	protected const string VolatilityRiskPremium = "0408_Volatility_Risk_Premium";

	// The strategy reports the midpoint of a bisection bracket no wider than 1e-5, so it is within 5e-6 of the exact volatility.
	private const double VrpVolTolerance = 1e-5;

	/// <summary>
	/// A candle the strategy acts on, with the positions and working orders it finds before acting.
	/// </summary>
	protected readonly record struct VrpCandle(bool IsOption, DateTime OpenTime, decimal Close, decimal UnderlyingPosition, decimal OptionPosition, int ActiveOrders);

	private sealed record VrpContract(OptionTypes Type, double Strike, DateTime Expiry, decimal Contracts, int Period, double TradingHoursPerYear,
		double SpikeRatio, double VegaStopPoints, double RiskFree, double Dividend, TimeSpan TimeFrame, decimal VolumeStep, string UnderlyingId, string OptionId);

	private sealed class VrpSummary
	{
		public int Sells { get; set; }
		public int EntryHedges { get; set; }
		public int Rebalances { get; set; }
		public int SpikeBuybacks { get; set; }
		public int VegaBuybacks { get; set; }
		public int ExpiryBuybacks { get; set; }
		public int ResalesAfterBuyback { get; set; }
		public int PremiumBars { get; set; }
		public int NoPremiumBars { get; set; }
		public int NativeDeviations { get; set; }
		public int RoundingTies { get; set; }
		public int UnmatchedShortBars { get; set; }
		public int UnmatchedRebalances { get; set; }
		public int UnmatchedExpiryBuybacks { get; set; }

		public override string ToString()
			=> $"sells={Sells}, entryHedges={EntryHedges}, rebalances={Rebalances}, spike={SpikeBuybacks}, vega={VegaBuybacks}, expiry={ExpiryBuybacks}, " +
				$"resalesAfterBuyback={ResalesAfterBuyback}, IV>RV bars={PremiumBars}, IV<=RV bars={NoPremiumBars}, nativeDeviations={NativeDeviations}, " +
				$"roundingTies={RoundingTies}, short bars without an option trade={UnmatchedShortBars}, rebalances on them={UnmatchedRebalances}, " +
				$"expiry buy-backs on them={UnmatchedExpiryBuybacks}";
	}

	/// <summary>
	/// Records every candle the strategy acts on, in the order it receives them, with the positions and working
	/// orders it finds before acting, every order with the candle that produced it and the native realized
	/// deviation values.
	/// </summary>
	protected sealed class VrpRecorder
	{
		private readonly SecurityId _underlyingId;
		private readonly SecurityId _optionId;
		private DateTime? _underlyingTime;
		private DateTime? _optionTime;

		public VrpRecorder(Strategy strategy, Security option)
		{
			_underlyingId = strategy.Security.Id.ToSecurityId();
			_optionId = option.Id.ToSecurityId();

			strategy.CandleReceived += (_, candle) => OnCandle(strategy, option, candle);
			strategy.OrderRegistering += order => Orders.Add((Candles.Count - 1, order));
			strategy.Indicators.Added += indicator =>
			{
				if (indicator is not StandardDeviation)
					return;

				indicator.Changed += (_, value) =>
				{
					if (value.IsFinal)
						Deviations.Add((UnderlyingCloses.Count - 1, value.GetValue<decimal>(), indicator.IsFormed));
				};
			};
		}

		public List<(DateTime Time, decimal Close)> UnderlyingCloses { get; } = [];
		public List<VrpCandle> Candles { get; } = [];
		public List<(int Candle, Order Order)> Orders { get; } = [];
		public List<(int Bar, decimal Value, bool IsFormed)> Deviations { get; } = [];
		public int UnexpectedCandles { get; private set; }

		private void OnCandle(Strategy strategy, Security option, ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
				return;

			var isOption = candle.SecurityId == _optionId;

			if (!isOption && candle.SecurityId != _underlyingId)
			{
				UnexpectedCandles++;
				return;
			}

			// The strategy ignores a candle that does not move its own stream forward.
			if ((isOption ? _optionTime : _underlyingTime) is DateTime last && candle.OpenTime <= last)
				return;

			if (isOption)
			{
				_optionTime = candle.OpenTime;
			}
			else
			{
				_underlyingTime = candle.OpenTime;
				UnderlyingCloses.Add((candle.OpenTime, candle.ClosePrice));
			}

			Candles.Add(new(isOption, candle.OpenTime, candle.ClosePrice, strategy.Position,
				strategy.GetPositionValue(option, strategy.Portfolio) ?? 0m,
				strategy.Orders.Count(order => order.State is not (OrderStates.Done or OrderStates.Failed))));
		}
	}

	/// <summary>
	/// Replays the recorded candles through an independent model of the README, in the order the strategy received
	/// them. Every underlying bar checks expiration and a realized volatility spike and otherwise rebalances the
	/// delta hedge with the latest implied volatility, whether or not the option traded in that bar. A bar in which
	/// the option traded as well inverts its price with an independent Black-Scholes formula, then stops the short
	/// out on the vega stop or sells an out-of-the-money option while implied exceeds realized volatility. Every
	/// submitted order is compared with what the model expects from that candle.
	/// </summary>
	private sealed class VrpReplay(VrpRecorder recorder, VrpContract contract, IReadOnlyDictionary<DateTime, double> realized, VrpSummary summary, List<string> violations)
	{
		private static readonly double _yearTicks = TimeSpan.FromDays(365).Ticks;

		private decimal _optionPosition;
		private decimal _hedgePosition;
		private DateTime? _underlyingTime;
		private DateTime? _optionTime;
		private DateTime? _matchedTime;
		private DateTime? _buyBackTime;
		private double _underlyingClose;
		private double _optionClose;
		private double _entryImplied;
		private double _entryRealized;
		private double? _lastImplied;

		public void Run()
		{
			var matchedBars = MatchedBars(recorder.Candles);
			var ordersByCandle = recorder.Orders.ToLookup(entry => entry.Candle, entry => entry.Order);

			for (var i = 0; i < recorder.Candles.Count; i++)
			{
				var candle = recorder.Candles[i];
				var at = $"{(candle.IsOption ? "option" : "underlying")} candle {candle.OpenTime:O}";

				if (candle.ActiveOrders != 0 || candle.OptionPosition != _optionPosition || candle.UnderlyingPosition != _hedgePosition)
				{
					violations.Add($"{at}: every earlier order must be filled before the next candle; active {candle.ActiveOrders}, option {candle.OptionPosition} against {_optionPosition}, hedge {candle.UnderlyingPosition} against {_hedgePosition}.");
					return;
				}

				var orders = new Queue<Order>(ordersByCandle[i]);

				if (candle.IsOption)
				{
					_optionTime = candle.OpenTime;
					_optionClose = (double)candle.Close;
				}
				else
				{
					_underlyingTime = candle.OpenTime;
					_underlyingClose = (double)candle.Close;
					UnderlyingBar(at, orders, candle.OpenTime, matchedBars.Contains(candle.OpenTime));
				}

				if (_underlyingTime is DateTime bar && _optionTime == bar && _matchedTime != bar)
				{
					_matchedTime = bar;
					MatchedBar(at, orders, bar);
				}

				if (orders.Count > 0)
					violations.Add($"{at}: unexpected orders [{Describe(orders)}].");
			}
		}

		/// <summary>
		/// Bars in which both instruments traded before the underlying's next bar, as the strategy matches them.
		/// </summary>
		private static HashSet<DateTime> MatchedBars(IEnumerable<VrpCandle> candles)
		{
			var matched = new HashSet<DateTime>();
			DateTime? underlying = null;
			DateTime? option = null;

			foreach (var candle in candles)
			{
				if (candle.IsOption)
					option = candle.OpenTime;
				else
					underlying = candle.OpenTime;

				if (underlying is DateTime bar && option == bar)
					matched.Add(bar);
			}

			return matched;
		}

		private void UnderlyingBar(string at, Queue<Order> orders, DateTime bar, bool isMatched)
		{
			// Nothing is decided before the realized volatility window is full, and a flat book has nothing to manage.
			if (!realized.TryGetValue(bar, out var realizedVol) || _optionPosition == 0m)
				return;

			var remaining = contract.Expiry - (bar + contract.TimeFrame);

			if (!isMatched)
				summary.UnmatchedShortBars++;

			// The last bar that closes before expiration is the last chance to buy the option back.
			if (remaining <= contract.TimeFrame)
			{
				BuyBack(at, orders, bar, "expiration");
				summary.ExpiryBuybacks++;

				if (!isMatched)
					summary.UnmatchedExpiryBuybacks++;

				return;
			}

			var spikeLevel = contract.SpikeRatio * _entryRealized;
			RequireMargin(at, realizedVol - spikeLevel, 1e-9 * spikeLevel, "realized volatility and the spike level");

			if (realizedVol >= spikeLevel)
			{
				BuyBack(at, orders, bar, "volatility spike");
				summary.SpikeBuybacks++;
				return;
			}

			var rebalanced = ExpectHedge(at, orders, _hedgePosition, -_optionPosition, remaining, _lastImplied.Value);

			if (rebalanced != _hedgePosition)
			{
				summary.Rebalances++;

				if (!isMatched)
					summary.UnmatchedRebalances++;
			}

			_hedgePosition = rebalanced;
		}

		private void MatchedBar(string at, Queue<Order> orders, DateTime bar)
		{
			var remaining = contract.Expiry - (bar + contract.TimeFrame);

			if (!realized.TryGetValue(bar, out var realizedVol) || remaining <= TimeSpan.Zero ||
				VrpImpliedVolatility(contract, _optionClose, _underlyingClose, remaining.Ticks / _yearTicks) is not double impliedVol)
				return;

			_lastImplied = impliedVol;

			if (impliedVol > realizedVol)
				summary.PremiumBars++;
			else
				summary.NoPremiumBars++;

			// One option trade per bar: a bar that bought the option back does not sell it again.
			if (_buyBackTime == bar)
				return;

			if (_optionPosition < 0m)
			{
				var rise = impliedVol - _entryImplied - contract.VegaStopPoints / 100;
				RequireMargin(at, rise, 2 * VrpVolTolerance, "the implied volatility rise and the vega stop");

				if (rise >= 0)
				{
					BuyBack(at, orders, bar, "vega stop");
					summary.VegaBuybacks++;
				}

				return;
			}

			RequireMargin(at, impliedVol - realizedVol, VrpVolTolerance, "implied and realized volatility");
			var isOutOfTheMoney = contract.Type == OptionTypes.Call ? contract.Strike > _underlyingClose : contract.Strike < _underlyingClose;

			if (impliedVol <= realizedVol || !isOutOfTheMoney || remaining <= contract.TimeFrame)
				return;

			ExpectOrder(at, orders, contract.OptionId, Sides.Sell, contract.Contracts, "Sell option");
			_hedgePosition = ExpectHedge(at, orders, 0m, contract.Contracts, remaining, impliedVol);

			if (_hedgePosition != 0m)
				summary.EntryHedges++;

			if (_buyBackTime is DateTime buyBack && bar - buyBack == contract.TimeFrame)
				summary.ResalesAfterBuyback++;

			_optionPosition = -contract.Contracts;
			_entryImplied = impliedVol;
			_entryRealized = realizedVol;
			summary.Sells++;
		}

		private void BuyBack(string at, Queue<Order> orders, DateTime bar, string reason)
		{
			ExpectOrder(at, orders, contract.OptionId, Sides.Buy, -_optionPosition, "Buy back option: " + reason);

			if (_hedgePosition != 0m)
				ExpectOrder(at, orders, contract.UnderlyingId, _hedgePosition > 0m ? Sides.Sell : Sides.Buy, Math.Abs(_hedgePosition), "Close delta hedge");

			_optionPosition = 0m;
			_hedgePosition = 0m;
			_buyBackTime = bar;
		}

		private void ExpectOrder(string at, Queue<Order> orders, string securityId, Sides side, decimal volume, string comment)
		{
			if (orders.TryPeek(out var order) && order.Security?.Id == securityId && order.Side == side && order.Volume == volume &&
				order.Type == OrderTypes.Market && order.Comment == comment)
			{
				orders.Dequeue();
				return;
			}

			violations.Add($"{at}: expected {securityId} {side} {volume} '{comment}', submitted [{Describe(orders)}].");
		}

		/// <summary>
		/// Checks that the hedge after this candle's hedge order, if any, is the Black-Scholes delta of the short
		/// contracts rounded to the volume step, and returns it. The strategy prices delta with its own implied
		/// volatility, which lies within the solver bracket around the independent one, so a target on a rounding
		/// boundary may land on either neighbour.
		/// </summary>
		private decimal ExpectHedge(string at, Queue<Order> orders, decimal current, decimal shortContracts, TimeSpan remaining, double sigma)
		{
			var years = remaining.Ticks / _yearTicks;
			var units = (double)(shortContracts * VolatilityRiskPremiumOptionFixture.Multiplier);
			decimal[] allowed =
			[
				.. new[] { sigma - VrpVolTolerance, sigma, sigma + VrpVolTolerance }
					.Select(deviation => (decimal)Math.Round(units * VrpBlackScholes(contract, _underlyingClose, years, deviation).Delta / (double)contract.VolumeStep, MidpointRounding.AwayFromZero) * contract.VolumeStep)
					.Distinct()
			];

			if (allowed.Length > 1)
				summary.RoundingTies++;

			var hedge = current;

			if (orders.TryPeek(out var order) && order.Comment == "Delta hedge")
			{
				orders.Dequeue();

				if (order.Security?.Id != contract.UnderlyingId || order.Type != OrderTypes.Market)
					violations.Add($"{at}: a delta hedge must be a market order on the underlying, got {order.Security?.Id} {order.Type}.");

				hedge += order.Side == Sides.Buy ? order.Volume : -order.Volume;
			}

			if (!allowed.Contains(hedge))
				violations.Add($"{at}: hedge {current} -> {hedge}; it must become the delta offset rounded to the volume step, {string.Join(" or ", allowed)}.");

			return hedge;
		}

		private void RequireMargin(string at, double difference, double tolerance, string what)
		{
			if (Math.Abs(difference) <= tolerance)
				violations.Add($"{at}: {what} differ by {difference}, inside the precision {tolerance}; the fixture cannot decide this bar independently.");
		}

		private static string Describe(IEnumerable<Order> orders)
			=> string.Join("; ", orders.Select(order => $"{order.Security?.Id} {order.Side} {order.Volume} {order.Type} '{order.Comment}'"));
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S0408_RejectsAnOptionThatIsMissingIncompleteOrOnAnotherUnderlying()
	{
		await Replay(VolatilityRiskPremium, (strategy, secondary) =>
		{
			var start = typeof(Strategy).GetMethod("OnStarted2", BindingFlags.NonPublic | BindingFlags.Instance);
			var underlying = strategy.Security;

			Security Spoiled(Action<Security> spoil)
			{
				var option = VolatilityRiskPremiumOptionFixture.Describe(new() { Id = secondary.Id }, underlying, OptionTypes.Call,
					VolatilityRiskPremiumOptionFixture.CallStrike, VolatilityRiskPremiumOptionFixture.QuarterlyExpiry);
				spoil(option);
				return option;
			}

			// The missing option goes first: a required parameter accepts null only while it still holds null.
			(Security Option, string Case)[] invalid =
			[
				(null, "no option"),
				(underlying, "the underlying itself"),
				(new Security { Id = secondary.Id }, "not an option"),
				(VolatilityRiskPremiumOptionFixture.Describe(new() { Id = secondary.Id }, new() { Id = "ETHUSDT@BNBFT" }, OptionTypes.Call,
					VolatilityRiskPremiumOptionFixture.CallStrike, VolatilityRiskPremiumOptionFixture.QuarterlyExpiry), "an option on another underlying"),
				(Spoiled(option => option.OptionType = null), "an option without its call or put type"),
				(Spoiled(option => option.Strike = null), "an option without a strike"),
				(Spoiled(option => option.Strike = 0m), "an option with a zero strike"),
				(Spoiled(option => option.ExpiryDate = null), "an option without an expiry"),
			];

			foreach (var (option, description) in invalid)
			{
				SetParam(strategy, "Option", option);
				var rejected = false;

				try
				{
					start.Invoke(strategy, [DateTime.MinValue]);
				}
				catch (TargetInvocationException exception)
				{
					IsTrue(exception.InnerException is InvalidOperationException && exception.InnerException.Message.Contains("Option"),
						$"{description}: the option contract is validated before any subscription instead of falling back to the underlying.");
					rejected = true;
				}

				IsTrue(rejected, $"{description} must be rejected.");
				AreEqual(0, strategy.Indicators.Count, description);
			}

			SetParam(strategy, "Option", VolatilityRiskPremiumOptionFixture.QuarterlyCall(secondary, underlying));
		}, TimeSpan.FromDays(3));
	}

	[TestMethod]
	[TestCategory("Shard00")]
	[DataRow(OptionTypes.Call, 0d, 0d, 8760d)]
	[DataRow(OptionTypes.Put, 0d, 0d, 8760d)]
	[DataRow(OptionTypes.Call, 0.05, 0.02, 6240d)]
	public async Task S0408_SellsHedgesAndBuysBackAgainstIndependentBlackScholes(OptionTypes type, double riskFree, double dividend, double tradingHours)
	{
		VrpRecorder recorder = null;
		VrpContract contract = null;

		await Replay(VolatilityRiskPremium, (strategy, secondary) =>
		{
			AreEqual(TimeSpan.FromHours(1).TimeFrame(), strategy.Parameters["CandleType"].Value);
			AreEqual(24, strategy.Parameters["RealizedVolPeriod"].Value);
			AreEqual(8760m, Convert.ToDecimal(strategy.Parameters["TradingHoursPerYear"].Value));
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["SpikeRatio"].Value));
			AreEqual(5m, Convert.ToDecimal(strategy.Parameters["VegaStopPoints"].Value));
			AreEqual(0m, Convert.ToDecimal(strategy.Parameters["RiskFreeRate"].Value));
			AreEqual(0m, Convert.ToDecimal(strategy.Parameters["DividendYield"].Value));
			SetParam(strategy, "RiskFreeRate", (decimal)riskFree);
			SetParam(strategy, "DividendYield", (decimal)dividend);
			SetParam(strategy, "TradingHoursPerYear", (decimal)tradingHours);

			var strike = type == OptionTypes.Call ? VolatilityRiskPremiumOptionFixture.CallStrike : VolatilityRiskPremiumOptionFixture.PutStrike;
			var option = VolatilityRiskPremiumOptionFixture.Describe(secondary, strategy.Security, type, strike, VolatilityRiskPremiumOptionFixture.QuarterlyExpiry);
			SetParam(strategy, "Option", option);
			contract = new(type, (double)strike, option.ExpiryDate.Value, strategy.Volume, 24, tradingHours, 2, 5, riskFree, dividend,
				TimeSpan.FromHours(1), strategy.Security.VolumeStep.Value, strategy.Security.Id, option.Id);
			recorder = new(strategy, option);
			TestContext.WriteLine($"Mechanics fixture only: {option.Id} prices stand in for the premiums of a {type} {strike} on {strategy.Security.Id}; no option market history.");
		}, TimeSpan.FromDays(31));

		var summary = VerifyVolatilityRiskPremium(recorder, contract);
		TestContext.WriteLine(summary.ToString());

		IsTrue(summary.PremiumBars > 0 && summary.NoPremiumBars > 0, "The fixture's implied volatility must cross the realized volatility, so both sides of the entry condition occur.");
		IsTrue(summary.Sells > 1 && summary.EntryHedges > 0, "Out-of-the-money options must be sold, each with its opening delta hedge.");
		IsTrue(summary.Rebalances > summary.Sells, "The hedge must be rebalanced periodically while the option is short, not only when it is sold.");
		IsTrue(summary.SpikeBuybacks > 0, "A realized volatility spike must buy the option back.");
		IsTrue(summary.VegaBuybacks > 0, "The vega stop must buy the option back.");
		IsTrue(summary.ResalesAfterBuyback > 0, "The entry has no condition besides implied above realized, so the fixture must sell again on the bar right after a buy-back.");
		IsTrue(summary.NativeDeviations > 0);
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S0408_BuysBackOnTheLastBarBeforeExpiration()
	{
		VrpRecorder recorder = null;
		VrpContract contract = null;

		await Replay(VolatilityRiskPremium, (strategy, secondary) =>
		{
			// Push the spike and vega exits out of reach so the short is held to its expiration.
			SetParam(strategy, "SpikeRatio", 1000m);
			SetParam(strategy, "VegaStopPoints", 100000m);

			var option = VolatilityRiskPremiumOptionFixture.Describe(secondary, strategy.Security, OptionTypes.Call,
				VolatilityRiskPremiumOptionFixture.CallStrike, VolatilityRiskPremiumOptionFixture.WeeklyExpiry);
			SetParam(strategy, "Option", option);
			contract = new(OptionTypes.Call, (double)VolatilityRiskPremiumOptionFixture.CallStrike, option.ExpiryDate.Value, strategy.Volume, 24, 8760, 1000, 100000, 0, 0,
				TimeSpan.FromHours(1), strategy.Security.VolumeStep.Value, strategy.Security.Id, option.Id);
			recorder = new(strategy, option);
		}, TimeSpan.FromDays(9));

		var summary = VerifyVolatilityRiskPremium(recorder, contract);
		TestContext.WriteLine(summary.ToString());

		IsTrue(summary.Sells > 0 && summary.Rebalances > 0, "The option must be sold and hedged before it expires.");
		AreEqual(1, summary.ExpiryBuybacks, "The short must be bought back once, on the last bar that closes before expiration.");
		AreEqual(0, summary.SpikeBuybacks + summary.VegaBuybacks);
		IsTrue(recorder.Candles.Any(candle => !candle.IsOption && candle.OpenTime + contract.TimeFrame > contract.Expiry), "The replay must continue past expiration to show nothing is sold afterwards.");
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S0408_BuysBackAndRehedgesWithoutWaitingForAnOptionTrade()
	{
		VrpRecorder recorder = null;
		VrpContract contract = null;

		await Replay(VolatilityRiskPremium, (strategy, secondary) =>
		{
			// The stand-in does not trade in every five-minute bar; spike and vega exits are out of reach so the short lives to expiration.
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "SpikeRatio", 1000m);
			SetParam(strategy, "VegaStopPoints", 100000m);

			var option = VolatilityRiskPremiumOptionFixture.Describe(secondary, strategy.Security, OptionTypes.Call,
				VolatilityRiskPremiumOptionFixture.CallStrike, VolatilityRiskPremiumOptionFixture.GapExpiry);
			SetParam(strategy, "Option", option);
			contract = new(OptionTypes.Call, (double)VolatilityRiskPremiumOptionFixture.CallStrike, option.ExpiryDate.Value, strategy.Volume, 24, 8760, 1000, 100000, 0, 0,
				TimeSpan.FromMinutes(5), strategy.Security.VolumeStep.Value, strategy.Security.Id, option.Id);
			recorder = new(strategy, option);
		}, TimeSpan.FromDays(9));

		var summary = VerifyVolatilityRiskPremium(recorder, contract);
		TestContext.WriteLine(summary.ToString());

		IsTrue(summary.Sells > 0 && summary.UnmatchedShortBars > 0, "The short must live through bars in which the option did not trade.");
		IsTrue(summary.UnmatchedRebalances > 0, "The hedge must be rebalanced on bars in which the option did not trade.");
		AreEqual(1, summary.ExpiryBuybacks, "The short must be bought back once, at expiration.");
		AreEqual(1, summary.UnmatchedExpiryBuybacks, "The option did not trade in the last bar that closes before expiration, and the short must still be bought back on it.");
		IsTrue(recorder.Candles.Any(candle => !candle.IsOption && candle.OpenTime + contract.TimeFrame > contract.Expiry), "The replay must continue past expiration to show nothing is sold afterwards.");
	}

	/// <summary>
	/// Checks the strategy's realized volatility window against an independent population deviation of log returns
	/// and replays the recorded candles through <see cref="VrpReplay"/>.
	/// </summary>
	private VrpSummary VerifyVolatilityRiskPremium(VrpRecorder recorder, VrpContract contract)
	{
		var summary = new VrpSummary();
		var violations = new List<string>();

		AreEqual(0, recorder.UnexpectedCandles, "Only the underlying and the option streams may reach the strategy.");
		IsTrue(recorder.Orders.All(entry => entry.Candle >= 0), "No order may precede the first candle.");

		// Population deviation of the last Period log returns, annualized by the bars the underlying trades in a year.
		var annualization = Math.Sqrt(contract.TradingHoursPerYear * TimeSpan.TicksPerHour / contract.TimeFrame.Ticks);
		var returns = new List<double>();
		var deviations = new Dictionary<int, double>();
		var realized = new Dictionary<DateTime, double>();

		for (var i = 1; i < recorder.UnderlyingCloses.Count; i++)
		{
			returns.Add(Math.Log((double)recorder.UnderlyingCloses[i].Close / (double)recorder.UnderlyingCloses[i - 1].Close));

			if (returns.Count < contract.Period)
				continue;

			var window = returns.Skip(returns.Count - contract.Period).ToArray();
			var mean = window.Average();
			var deviation = Math.Sqrt(window.Sum(value => (value - mean) * (value - mean)) / contract.Period);
			deviations[i] = deviation;
			realized[recorder.UnderlyingCloses[i].Time] = deviation * annualization;
		}

		foreach (var (bar, value, isFormed) in recorder.Deviations)
		{
			if (!isFormed)
				continue;

			summary.NativeDeviations++;

			if (!deviations.TryGetValue(bar, out var expected) || Math.Abs((double)value - expected) > 1e-9 * expected + 1e-15)
				violations.Add($"Underlying bar {bar}: native deviation {value} is not the population deviation of the last {contract.Period} log returns.");
		}

		AreEqual(deviations.Count, summary.NativeDeviations, "Every formed realized volatility window must come from the strategy's deviation of log returns.");

		new VrpReplay(recorder, contract, realized, summary, violations).Run();

		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		return summary;
	}

	/// <summary>
	/// Black-Scholes premium and delta with a continuous dividend yield, written out independently of the platform model.
	/// </summary>
	private static (double Premium, double Delta) VrpBlackScholes(VrpContract contract, double asset, double years, double sigma)
	{
		var root = Math.Sqrt(years);
		var d1 = (Math.Log(asset / contract.Strike) + (contract.RiskFree - contract.Dividend + sigma * sigma / 2) * years) / (sigma * root);
		var d2 = d1 - sigma * root;
		var carry = Math.Exp(-contract.Dividend * years);
		var discount = Math.Exp(-contract.RiskFree * years);

		return contract.Type == OptionTypes.Call
			? (asset * carry * VrpNormal(d1) - contract.Strike * discount * VrpNormal(d2), carry * VrpNormal(d1))
			: (contract.Strike * discount * VrpNormal(-d2) - asset * carry * VrpNormal(-d1), -carry * VrpNormal(-d1));
	}

	/// <summary>
	/// Volatility at which the Black-Scholes premium equals the observed price, by bisection to machine precision;
	/// undefined when the price does not exceed the value of the option without volatility.
	/// </summary>
	private static double? VrpImpliedVolatility(VrpContract contract, double premium, double asset, double years)
	{
		var forward = asset * Math.Exp(-contract.Dividend * years) - contract.Strike * Math.Exp(-contract.RiskFree * years);
		var zeroVolatilityValue = Math.Max(0, contract.Type == OptionTypes.Call ? forward : -forward);

		if (premium <= zeroVolatilityValue)
			return null;

		var low = 0d;
		var high = 2d;

		while (VrpBlackScholes(contract, asset, years, high).Premium < premium)
		{
			if (high >= 1000)
				return null;

			low = high;
			high *= 2;
		}

		for (var i = 0; i < 200 && high - low > 1e-14; i++)
		{
			var middle = (low + high) / 2;

			if (VrpBlackScholes(contract, asset, years, middle).Premium > premium)
				high = middle;
			else
				low = middle;
		}

		return (low + high) / 2;
	}

	private static double VrpNormal(double x)
		=> 0.5 * VrpErfc(-x / Math.Sqrt(2));

	/// <summary>
	/// Complementary error function: the Abramowitz and Stegun 7.1.6 series below 1.5, Laplace's continued fraction above.
	/// </summary>
	private static double VrpErfc(double z)
	{
		if (z < 0)
			return 2 - VrpErfc(-z);

		if (z < 1.5)
		{
			var term = z;
			var sum = z;

			for (var n = 1; n < 300 && term > 1e-17 * sum; n++)
			{
				term *= 2 * z * z / (2 * n + 1);
				sum += term;
			}

			return 1 - 2 / Math.Sqrt(Math.PI) * Math.Exp(-z * z) * sum;
		}

		var fraction = z;

		for (var k = 100; k >= 1; k--)
			fraction = z + k / 2d / fraction;

		return Math.Exp(-z * z) / Math.Sqrt(Math.PI) / fraction;
	}

	private const string WtiBrentSpread = "0410_WTIBrent_Spread";

	[TestMethod]
	[TestCategory("Shard02")]
	public async Task S0410_PublishesItsDefaultsAndRefusesAMissingOrRepeatedBrentLeg()
	{
		var model = new WtiBrentSpreadModel();

		await Replay(WtiBrentSpread, (strategy, second) =>
		{
			AreEqual(TimeSpan.FromMinutes(15).TimeFrame(), strategy.Parameters["CandleType"].Value, "C# and Python publish the same candle type.");
			AreEqual(20, Convert.ToInt32(strategy.Parameters["Lookback"].Value), "C# and Python publish the same lookback.");
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["EntryZScore"].Value), "C# and Python publish the same entry threshold.");
			AreEqual(1m, Convert.ToDecimal(strategy.Parameters["StopWidening"].Value), "C# and Python publish the same stop widening.");
			AreEqual(5, Convert.ToInt32(strategy.Parameters["RollDays"].Value), "C# and Python publish the same roll days.");
			IsNull(strategy.Parameters["BrentSecurity"].Value, "The Brent contract is the trader's choice and has no default.");
			Throws<ArgumentOutOfRangeException>(() => SetParam(strategy, "Lookback", 1), "A single bar has no deviation to measure the spread against, so the lookback must refuse it.");

			var start = typeof(Strategy).GetMethod("OnStarted2", BindingFlags.NonPublic | BindingFlags.Instance);
			var subscriptions = strategy.Subscriptions.Subscriptions.Count();

			foreach (var invalid in new[] { null, strategy.Security })
			{
				SetParam(strategy, "BrentSecurity", invalid);
				var rejected = false;

				try
				{
					start.Invoke(strategy, [DateTime.MinValue]);
				}
				catch (TargetInvocationException exception)
				{
					IsTrue(exception.InnerException is InvalidOperationException && exception.InnerException.Message.Contains("BrentSecurity"),
						$"README: the spread is WTI against Brent, so the start must name the missing or repeated Brent leg; got {exception.InnerException}.");
					rejected = true;
				}

				IsTrue(rejected, "A missing Brent contract, or the WTI contract given again as Brent, must be refused before the strategy runs.");
				AreEqual(subscriptions, strategy.Subscriptions.Subscriptions.Count(), "A refused start must not subscribe to either leg.");
			}

			// Mechanics fixture: packaged TON stands in for the Brent leg against BTC, not crude oil history.
			SetParam(strategy, "BrentSecurity", second);
			model.Attach(strategy);
		}, TimeSpan.FromDays(7));

		TestContext.WriteLine($"python={IsPython}: {model}");
		model.AssertMatched();
	}

	[TestMethod]
	[TestCategory("Shard02")]
	public async Task S0410_BuysTheCheapGradeAndSellsTheExpensiveOneForTheSameDollarsBothWays()
	{
		var model = new WtiBrentSpreadModel();

		await Replay(WtiBrentSpread, (strategy, second) =>
		{
			// Mechanics fixture, not crude oil history: TON in lots of 100 coins is the WTI leg and BTC the Brent leg,
			// so the grade the README buys or sells is set by the spread, not by which price is lower.
			var btc = strategy.Security;
			strategy.Security = second;
			second.Multiplier = 100m;
			strategy.Volume = 10m;
			SetParam(strategy, "BrentSecurity", btc);
			model.Attach(strategy);
		}, TimeSpan.FromDays(31));

		TestContext.WriteLine($"python={IsPython}: {model}");
		model.AssertMatched();
		IsTrue(model.LongEntries > 0 && model.ShortEntries > 0,
			$"README: a deviation either way buys the grade that is cheap against the average and sells the expensive one; got {model.LongEntries} long and {model.ShortEntries} short WTI entries.");
		IsTrue(model.DeepEntries > 0,
			"README: a pair opens at any deviation beyond the threshold, however large, and the archive must open some with the z-score a full stop widening past it.");
		IsTrue(model.AverageExits > 0, "README: the pair is closed when the spread returns to its average.");
		IsTrue(model.StopExits > 0, "README: the pair is stopped when the spread keeps widening.");
		IsTrue(model.StopBlockedSignals > 0, "After a stop the same side waits until the spread is back inside the entry threshold, and the archive must show it waiting.");
		AreEqual(0, model.UnsizedSignals, "Ten lots of TON must always balance against a BTC quantity within its volume limits.");
	}

	[TestMethod]
	[TestCategory("Shard02")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task S0410_ClosesThePairBeforeTheEarlierExpiryAndOpensNoneInsideTheRollWindow(bool brentExpiresFirst)
	{
		var duration = TimeSpan.FromDays(10);
		var unexpiring = new WtiBrentSpreadModel();

		// Mechanics fixture: packaged TON stands in for the Brent leg against BTC, not crude oil history.
		await Replay(WtiBrentSpread, (strategy, second) =>
		{
			SetParam(strategy, "BrentSecurity", second);
			unexpiring.Attach(strategy);
		}, duration);

		unexpiring.AssertMatched();

		// The roll comes due halfway through the longest pair held without expiries, early enough to leave
		// days of entry signals inside the roll window.
		var holdings = unexpiring.Holdings
			.Where(holding => holding.Close is DateTime close && close < Paths.HistoryBeginDate.AddDays(7))
			.ToArray();

		IsTrue(holdings.Length > 0, $"The first week must hold and close a pair. {unexpiring}");

		var longest = holdings.MaxBy(holding => holding.Close.Value - holding.Open);
		var rollAt = longest.Open + (longest.Close.Value - longest.Open) / 2;
		var rolled = new WtiBrentSpreadModel();

		await Replay(WtiBrentSpread, (strategy, second) =>
		{
			var rollDays = Convert.ToInt32(strategy.Parameters["RollDays"].Value);
			var (earlier, later) = brentExpiresFirst ? (second, strategy.Security) : (strategy.Security, second);

			earlier.ExpiryDate = rollAt.AddDays(rollDays);
			// The later contract comes due after the replay, so only the earlier expiry can trigger the roll.
			later.ExpiryDate = rollAt.AddDays(rollDays + 30);
			SetParam(strategy, "BrentSecurity", second);
			rolled.Attach(strategy);
		}, duration);

		TestContext.WriteLine($"python={IsPython}, brentExpiresFirst={brentExpiresFirst}, rollAt={rollAt:O}, held {longest.Open:O}..{longest.Close:O}: {rolled}");
		rolled.AssertMatched();
		AreEqual(1, rolled.RollExits, "README: the pair open when the earlier contract comes within the roll days is closed at the roll.");
		IsTrue(rolled.RollExitTime >= rollAt && rolled.RollExitTime <= longest.Close,
			$"The roll must close the pair on the first bar of the roll window, {rolled.RollExitTime:O}, not wait for the spread.");
		IsTrue(rolled.Holdings.All(holding => holding.Open < rollAt), "No pair may open inside the roll window.");
		IsTrue(rolled.RollBlockedSignals > 0, "Entry signals inside the roll window must be passed over, and the archive must show some.");
	}

	private const string CaptainBacktestModel = "0601_Captain_Backtest_Model";

	[TestMethod]
	[TestCategory("Shard01")]
	[DataRow(25.0, 75.0, 16)]
	[DataRow(1000000.0, 1000000.0, 16)]
	[DataRow(1000000.0, 1000000.0, 13)]
	public async Task S0601_MorningRangeBiasRetracementEntriesAndRiskRewardOrWindowExits(double risk, double reward, int tradeEndHour)
	{
		var rangeStart = new TimeSpan(6, 0, 0);
		var rangeEnd = new TimeSpan(10, 0, 0);
		var takeStart = new TimeSpan(10, 0, 0);
		var takeEnd = new TimeSpan(11, 15, 0);
		var tradeStart = new TimeSpan(10, 0, 0);
		var tradeEnd = TimeSpan.FromHours(tradeEndHour);
		var riskPoints = Convert.ToDecimal(risk);
		var rewardPoints = Convert.ToDecimal(reward);
		var riskRewardReachable = risk < 1000.0;

		DateTime? session = null;
		decimal? rangeHigh = null;
		decimal? rangeLow = null;
		var bias = 0;
		var retraced = false;
		var traded = false;
		ICandleMessage previous = null;
		var entryPrice = 0m;
		var expected = new Queue<(Sides Side, decimal Volume, string Reason)>();
		var expectedOrders = 0;
		var actualOrders = 0;
		var sessions = 0;
		var longBias = 0;
		var shortBias = 0;
		var unbiasedBreakouts = 0;
		var longEntries = 0;
		var shortEntries = 0;
		var riskRewardExits = 0;
		var windowExits = 0;
		var sessionRollCloses = 0;
		var violations = new List<string>();

		void expect(Sides side, decimal volume, string reason)
		{
			expected.Enqueue((side, volume, reason));
			expectedOrders++;
		}

		await Replay(CaptainBacktestModel, (strategy, _) =>
		{
			AreEqual(new TimeSpan(6, 0, 0), strategy.Parameters["PrevRangeStart"].Value);
			AreEqual(new TimeSpan(10, 0, 0), strategy.Parameters["PrevRangeEnd"].Value);
			AreEqual(new TimeSpan(10, 0, 0), strategy.Parameters["TakeStart"].Value);
			AreEqual(new TimeSpan(11, 15, 0), strategy.Parameters["TakeEnd"].Value);
			AreEqual(new TimeSpan(10, 0, 0), strategy.Parameters["TradeStart"].Value);
			AreEqual(new TimeSpan(16, 0, 0), strategy.Parameters["TradeEnd"].Value);
			AreEqual(25m, Convert.ToDecimal(strategy.Parameters["Risk"].Value));
			AreEqual(75m, Convert.ToDecimal(strategy.Parameters["Reward"].Value));
			SetParam(strategy, "TradeEnd", tradeEnd);
			SetParam(strategy, "Risk", riskPoints);
			SetParam(strategy, "Reward", rewardPoints);

			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished || candle.SecurityId != strategy.Security.Id.ToSecurityId())
					return;

				expected.Clear();
				var position = strategy.Position;
				var exitSide = position > 0m ? Sides.Sell : Sides.Buy;

				var date = candle.OpenTime.Date;
				if (session != date)
				{
					if (session is not null && position != 0m)
					{
						sessionRollCloses++;
						expect(exitSide, Math.Abs(position), "session change close");
					}

					session = date;
					sessions++;
					rangeHigh = rangeLow = null;
					bias = 0;
					retraced = traded = false;
					previous = null;
					entryPrice = 0m;
				}

				var time = candle.OpenTime.TimeOfDay;
				if (time >= rangeStart && time < rangeEnd)
				{
					rangeHigh = rangeHigh is decimal high ? Math.Max(high, candle.HighPrice) : candle.HighPrice;
					rangeLow = rangeLow is decimal low ? Math.Min(low, candle.LowPrice) : candle.LowPrice;
					previous = candle;
					return;
				}

				if (position != 0m && entryPrice > 0m)
				{
					var stopHit = position > 0m ? candle.LowPrice <= entryPrice - riskPoints : candle.HighPrice >= entryPrice + riskPoints;
					var targetHit = position > 0m ? candle.HighPrice >= entryPrice + rewardPoints : candle.LowPrice <= entryPrice - rewardPoints;
					if (stopHit || targetHit)
					{
						riskRewardExits++;
						expect(exitSide, Math.Abs(position), "fixed risk/reward exit");
						previous = candle;
						return;
					}
				}

				if (time >= tradeEnd)
				{
					if (position != 0m)
					{
						windowExits++;
						expect(exitSide, Math.Abs(position), "end of trade window exit");
					}

					previous = candle;
					return;
				}

				if (bias == 0 && rangeHigh is decimal top && rangeLow is decimal bottom && time >= takeStart && time <= takeEnd)
				{
					var brokeHigh = candle.HighPrice > top;
					var brokeLow = candle.LowPrice < bottom;
					if (brokeHigh != brokeLow)
					{
						bias = brokeHigh ? 1 : -1;
						if (brokeHigh)
							longBias++;
						else
							shortBias++;
					}
				}

				if (bias == 0 || traded || time < tradeStart || previous is null)
				{
					if (bias == 0 && previous is not null && time >= tradeStart && (candle.ClosePrice > previous.HighPrice || candle.ClosePrice < previous.LowPrice))
						unbiasedBreakouts++;

					previous = candle;
					return;
				}

				var pullback = bias > 0
					? candle.ClosePrice < candle.OpenPrice || candle.LowPrice < previous.LowPrice
					: candle.ClosePrice > candle.OpenPrice || candle.HighPrice > previous.HighPrice;
				if (pullback)
					retraced = true;

				var breakout = bias > 0 ? candle.ClosePrice > previous.HighPrice : candle.ClosePrice < previous.LowPrice;
				if (retraced && breakout)
				{
					traded = true;
					entryPrice = candle.ClosePrice;
					if (bias > 0)
						longEntries++;
					else
						shortEntries++;
					expect(bias > 0 ? Sides.Buy : Sides.Sell, strategy.Volume, bias > 0 ? "long entry after retracement" : "short entry after retracement");
				}

				previous = candle;
			};

			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (!expected.TryDequeue(out var next))
				{
					if (violations.Count < 12)
						violations.Add($"{strategy.CurrentTime:O}: unexpected {order.Side} {order.Volume}; README trades only a breakout of the previous candle in the morning-range bias after a retracement and exits only at the fixed risk/reward or the end of the trade window.");
					return;
				}

				if ((order.Side != next.Side || order.Volume != next.Volume || order.Type != OrderTypes.Market) && violations.Count < 12)
					violations.Add($"{strategy.CurrentTime:O}: {next.Reason} expected {next.Side} {next.Volume} at market, got {order.Side} {order.Volume} {order.Type}.");
			};
		}, TimeSpan.FromDays(31));

		TestContext.WriteLine($"python={IsPython}, risk={risk}, reward={reward}, tradeEnd={tradeEnd}: sessions={sessions}, longBias={longBias}, shortBias={shortBias}, unbiasedBreakouts={unbiasedBreakouts}, longEntries={longEntries}, shortEntries={shortEntries}, riskRewardExits={riskRewardExits}, windowExits={windowExits}, sessionRollCloses={sessionRollCloses}, orders={actualOrders}/{expectedOrders}");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));
		AreEqual(expectedOrders, actualOrders, "Every README entry and exit must act, and nothing else may trade.");
		IsTrue(longEntries > 0 && shortEntries > 0, "README trades both directions: the real archive must reach entries on long-bias and short-bias sessions.");
		IsTrue(unbiasedBreakouts > 0, "The real archive must reach breakouts before the morning range sets a bias, which must not trade.");
		AreEqual(0, sessionRollCloses, "README exits at the fixed risk/reward or at the end of the trade window, never by carrying a position into the next session.");

		if (riskRewardReachable)
		{
			IsTrue(riskRewardExits > 0, "The fixed point stop and target must close real trades.");
		}
		else
		{
			AreEqual(0, riskRewardExits);
			AreEqual(longEntries + shortEntries, windowExits, "With an unreachable stop and target every trade must leave at the end of the trade window.");
		}
	}

	private const string QuantumSentimentFlux = "0703_Quantum_Sentiment_Flux_Beginners";

	[TestMethod]
	[TestCategory("Shard07")]
	public Task S0703_DefaultsEnterOnlyOnCrossBarsWhoseGapExceedsAtrThreshold()
		=> CheckQuantumSentimentFlux(null);

	[TestMethod]
	[TestCategory("Shard07")]
	[DataRow(5, 12, 0.15, 10, 2.0, 0, 2.0)]
	[DataRow(7, 14, 0.0, 14, 1.0, 20, 1.0)]
	public Task S0703_ConfiguredEntriesOnlyOnCrossBarsWhoseGapExceedsAtrThreshold(int fastPeriod, int slowPeriod, double threshold, int atrPeriod, double atrMultiplier, int cooldownBars, double quantity)
		=> CheckQuantumSentimentFlux(strategy =>
		{
			SetParam(strategy, "FastEmaPeriod", fastPeriod);
			SetParam(strategy, "SlowEmaPeriod", slowPeriod);
			SetParam(strategy, "MaStrengthThreshold", Convert.ToDecimal(threshold));
			SetParam(strategy, "AtrPeriod", atrPeriod);
			SetParam(strategy, "AtrMultiplier", Convert.ToDecimal(atrMultiplier));
			SetParam(strategy, "CooldownBars", cooldownBars);
			SetParam(strategy, "Quantity", Convert.ToDecimal(quantity));
		});

	/// <summary>
	/// Replays the archive against an independent model of the README: an entry only on the bar where the
	/// fast EMA crosses the slow EMA with a gap strictly above ATR * MaStrengthThreshold, an exit at one ATR
	/// multiple against the trade or two in its favour, and the cooldown after an exit.
	/// </summary>
	private async Task CheckQuantumSentimentFlux(Action<Strategy> configure)
	{
		var fastPeriod = 0;
		var slowPeriod = 0;
		var atrPeriod = 0;
		var threshold = 0m;
		var atrMultiplier = 0m;
		var cooldownBars = 0;
		var quantity = 0m;
		var fastMultiplier = 0m;
		var slowMultiplier = 0m;
		var fastSum = 0m;
		var slowSum = 0m;
		var fast = 0m;
		var slow = 0m;
		var atr = 0m;
		decimal? previousClose = null;
		decimal? previousFast = null;
		decimal? previousSlow = null;
		var bars = 0;
		var entryPrice = 0m;
		var entryAtr = 0m;
		var cooldown = 0;
		var lastCross = 0;
		var lastCrossTraded = true;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var longEntries = 0;
		var shortEntries = 0;
		var stopExits = 0;
		var targetExits = 0;
		var weakCrosses = 0;
		var crossesDuringCooldown = 0;
		var strongGapsAfterUntradedCross = 0;
		var nativeFast = 0;
		var nativeSlow = 0;
		var nativeAtr = 0;
		var violations = new List<string>();

		void violate(string message)
		{
			if (violations.Count < 12)
				violations.Add(message);
		}

		await Replay(QuantumSentimentFlux, (strategy, _) =>
		{
			foreach (var id in new[] { "CandleType", "FastEmaPeriod", "SlowEmaPeriod", "AtrPeriod", "AtrMultiplier", "MaStrengthThreshold", "CooldownBars", "Quantity" })
				IsTrue(strategy.Parameters.ContainsKey(id), $"README lists the {id} parameter, but the strategy does not expose it.");

			configure?.Invoke(strategy);

			fastPeriod = Convert.ToInt32(strategy.Parameters["FastEmaPeriod"].Value);
			slowPeriod = Convert.ToInt32(strategy.Parameters["SlowEmaPeriod"].Value);
			atrPeriod = Convert.ToInt32(strategy.Parameters["AtrPeriod"].Value);
			threshold = Convert.ToDecimal(strategy.Parameters["MaStrengthThreshold"].Value);
			atrMultiplier = Convert.ToDecimal(strategy.Parameters["AtrMultiplier"].Value);
			cooldownBars = Convert.ToInt32(strategy.Parameters["CooldownBars"].Value);
			quantity = Convert.ToDecimal(strategy.Parameters["Quantity"].Value);
			fastMultiplier = 2m / (fastPeriod + 1);
			slowMultiplier = 2m / (slowPeriod + 1);
			IsTrue(fastPeriod < slowPeriod, $"The fast EMA ({fastPeriod}) must be shorter than the slow EMA ({slowPeriod}).");

			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished || strategy.ProcessState != ProcessStates.Started)
					return;

				if (expectedSide is not null)
					violate($"{candle.OpenTime:O}: the {expectedSide} {expectedVolume} order due on the previous bar was never registered.");

				expectedSide = null;
				bars++;

				var close = candle.ClosePrice;

				if (bars <= fastPeriod)
				{
					fastSum += close;
					fast = fastSum / fastPeriod;
				}
				else
				{
					fast = (close - fast) * fastMultiplier + fast;
				}

				if (bars <= slowPeriod)
				{
					slowSum += close;
					slow = slowSum / slowPeriod;
				}
				else
				{
					slow = (close - slow) * slowMultiplier + slow;
				}

				var trueRange = previousClose is decimal priorClose
					? Math.Max(candle.HighPrice - candle.LowPrice, Math.Max(Math.Abs(priorClose - candle.HighPrice), Math.Abs(priorClose - candle.LowPrice)))
					: candle.HighPrice - candle.LowPrice;
				var atrCount = Math.Min(bars, atrPeriod);
				atr = (atr * (atrCount - 1) + trueRange) / atrCount;
				previousClose = close;

				if (previousFast is not decimal oldFast || previousSlow is not decimal oldSlow)
				{
					previousFast = fast;
					previousSlow = slow;
					return;
				}

				var crossUp = oldFast <= oldSlow && fast > slow;
				var crossDown = oldFast >= oldSlow && fast < slow;
				previousFast = fast;
				previousSlow = slow;

				if (crossUp || crossDown)
				{
					lastCross = crossUp ? 1 : -1;
					lastCrossTraded = false;
				}

				var position = strategy.Position;

				if (position != 0m && entryPrice > 0m && entryAtr > 0m)
				{
					var stopDistance = entryAtr * atrMultiplier;
					var stopped = position > 0m ? candle.LowPrice <= entryPrice - stopDistance : candle.HighPrice >= entryPrice + stopDistance;
					var targeted = position > 0m ? candle.HighPrice >= entryPrice + stopDistance * 2m : candle.LowPrice <= entryPrice - stopDistance * 2m;

					if (stopped || targeted)
					{
						expectedSide = position > 0m ? Sides.Sell : Sides.Buy;
						expectedVolume = Math.Abs(position);
						expectedOrders++;

						if (stopped)
							stopExits++;
						else
							targetExits++;

						entryPrice = 0m;
						entryAtr = 0m;
						cooldown = cooldownBars;
						return;
					}
				}

				if (cooldown > 0)
					cooldown--;

				if (position != 0m || atr <= 0m)
					return;

				if (cooldown > 0)
				{
					if (crossUp || crossDown)
						crossesDuringCooldown++;

					return;
				}

				var required = atr * threshold;

				if ((crossUp && fast - slow > required) || (crossDown && slow - fast > required))
				{
					expectedSide = crossUp ? Sides.Buy : Sides.Sell;
					expectedVolume = quantity;
					expectedOrders++;

					if (crossUp)
						longEntries++;
					else
						shortEntries++;

					entryPrice = close;
					entryAtr = atr;
					lastCrossTraded = true;
				}
				else if (crossUp || crossDown)
				{
					weakCrosses++;
				}
				else if (!lastCrossTraded && Math.Sign(fast - slow) == lastCross && Math.Abs(fast - slow) > required)
				{
					strongGapsAfterUntradedCross++;
				}
			};

			strategy.Indicators.Added += indicator =>
			{
				indicator.Changed += (_, value) =>
				{
					if (!value.IsFinal || value.IsEmpty)
						return;

					decimal expected;

					if (indicator is AverageTrueRange averageTrueRange && averageTrueRange.Length == atrPeriod)
					{
						nativeAtr++;
						expected = atr;
					}
					else if (indicator is ExponentialMovingAverage fastEma && fastEma.Length == fastPeriod)
					{
						nativeFast++;
						expected = fast;
					}
					else if (indicator is ExponentialMovingAverage slowEma && slowEma.Length == slowPeriod)
					{
						nativeSlow++;
						expected = slow;
					}
					else
					{
						violate($"Unexpected indicator {indicator.GetType().Name}: README names a fast EMA, a slow EMA and an ATR of the configured lengths.");
						return;
					}

					var actual = value.GetValue<decimal>();

					if (Math.Abs(actual - expected) > 0.0000000001m)
						violate($"{strategy.CurrentTime:O}: native {indicator.GetType().Name} is {actual}, the independent close-based value is {expected}.");
				};
			};

			strategy.OrderRegistering += order =>
			{
				actualOrders++;

				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
				{
					var due = expectedSide is null ? "no order" : $"{expectedSide} {expectedVolume} at market";
					violate($"{strategy.CurrentTime:O}: expected {due}, got {order.Side} {order.Volume} {order.Type}. An entry is allowed only while flat and outside the cooldown, on the bar where the fast EMA crosses the slow EMA with a gap strictly above ATR * MaStrengthThreshold, in the direction of the cross, for Quantity; an exit closes the whole position at one ATR multiple against the entry or two in its favour.");
				}

				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));

		TestContext.WriteLine($"python={IsPython} fast={fastPeriod} slow={slowPeriod} threshold={threshold} atr={atrPeriod} multiplier={atrMultiplier} cooldown={cooldownBars} quantity={quantity}: " +
			$"bars={bars}, orders={actualOrders}, long={longEntries}, short={shortEntries}, stops={stopExits}, targets={targetExits}, " +
			$"weakCrosses={weakCrosses}, crossesDuringCooldown={crossesDuringCooldown}, strongGapsAfterUntradedCross={strongGapsAfterUntradedCross}");

		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));
		AreEqual(expectedOrders, actualOrders, "Every strong cross bar met while flat and outside the cooldown must enter, and every ATR stop or target hit must exit.");
		AreEqual(bars, nativeFast, "The fast EMA must process every finished candle.");
		AreEqual(bars, nativeSlow, "The slow EMA must process every finished candle.");
		AreEqual(bars, nativeAtr, "The ATR must process every finished candle.");
		IsTrue(longEntries > 0 && shortEntries > 0, "The archive must exercise strong crosses in both directions.");
		IsTrue(stopExits > 0 && targetExits > 0, "The archive must exercise both the ATR stop and the two-ATR target.");
		IsTrue(strongGapsAfterUntradedCross > 0, "The archive must reach bars without a cross whose gap exceeds the threshold after an untraded cross, where the README forbids an entry.");

		if (threshold > 0m)
			IsTrue(weakCrosses > 0, "The archive must reach cross bars whose gap does not exceed the ATR threshold, where the README forbids an entry.");

		if (cooldownBars > 0)
			IsTrue(crossesDuringCooldown > 0, "The archive must reach crosses inside the cooldown, where no entry is allowed.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task S1005_MaWithLogistic()
	{
		async Task<OrderTraceRecorder> Run(decimal takeProfit, decimal stopLoss)
		{
			var recorder = new OrderTraceRecorder();

			await Replay("1005_MA_With_Logistic", (strategy, _) =>
			{
				SetParam(strategy, "FastLength", 3);
				SetParam(strategy, "SlowLength", 8);
				SetParam(strategy, "TakeProfitPercent", takeProfit);
				SetParam(strategy, "StopLossPercent", stopLoss);
				recorder.Attach(strategy);
			}, TimeSpan.FromDays(7));

			return recorder;
		}

		var wideProtection = await Run(100m, 100m);
		var tightTakeProfit = await Run(0.5m, 100m);
		var tightStopLoss = await Run(100m, 0.5m);

		tightTakeProfit.AssertDiffersFrom(wideProtection, "Changing TakeProfitPercent did not affect submitted orders.");
		tightStopLoss.AssertDiffersFrom(wideProtection, "Changing StopLossPercent did not affect submitted orders.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task S1101_TrailingExitRequiresNewSignalTransition()
	{
		var recorder = new OrderTraceRecorder();

		// Short MACD periods, a 15m higher frame and a 0.01% trail give many trailing exits and fresh agreements within three days, which this re-entry check needs.
		await Replay("1101_Multi_Timeframe_MACD", (strategy, _) =>
		{
			SetParam(strategy, "FastLength", 2);
			SetParam(strategy, "SlowLength", 3);
			SetParam(strategy, "SignalLength", 2);
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "HigherCandleType", TimeSpan.FromMinutes(15).TimeFrame());
			SetParam(strategy, "UseTrailingStop", true);
			SetParam(strategy, "TrailingStopPercent", 0.01m);
			recorder.Attach(strategy);
		}, TimeSpan.FromDays(3));

		recorder.AssertNoSameSideReentryWithinAfterFirstExit(TimeSpan.FromMinutes(5));
	}

	[TestMethod]
	[TestCategory("Shard01")]
	public Task S1153_Pairs()
		=> Replay("1153_Pairs", (s, sec2) => SetParam(s, "ReferenceSecurity", sec2));

	[TestMethod]
	[TestCategory("Shard03")]
	public async Task S1507_UltimateTemplate()
	{
		async Task<OrderTraceRecorder> Run(int fastLength, int slowLength, decimal takeProfit, decimal stopLoss, bool assertDefaults = false)
		{
			var recorder = new OrderTraceRecorder();

			await Replay("1507_Ultimate_Template", (strategy, _) =>
			{
				if (assertDefaults)
				{
					IsTrue(strategy.Parameters.TryGetValue("TakeProfitPercent", out var parameter));
					AreEqual(IsPython ? typeof(double) : typeof(decimal), parameter.Value.GetType(), "TakeProfitPercent must be numeric.");
					AreEqual(3m, Convert.ToDecimal(parameter.Value), "Unexpected TakeProfitPercent default.");
				}

				SetParam(strategy, "FastLength", fastLength);
				SetParam(strategy, "SlowLength", slowLength);
				SetParam(strategy, "TakeProfitPercent", takeProfit);
				SetParam(strategy, "StopLossPercent", stopLoss);
				recorder.Attach(strategy);
			}, TimeSpan.FromDays(7));

			return recorder;
		}

		var baseline = await Run(9, 21, 100m, 100m, assertDefaults: true);
		var alternatePeriods = await Run(2, 60, 100m, 100m);
		var tightTakeProfit = await Run(9, 21, 0.5m, 100m);
		var tightStopLoss = await Run(9, 21, 100m, 0.01m);

		alternatePeriods.AssertDiffersFrom(baseline, "Changing FastLength and SlowLength did not affect submitted orders.");
		tightTakeProfit.AssertDiffersFrom(baseline, "Changing TakeProfitPercent did not affect submitted orders.");
		tightStopLoss.AssertDiffersFrom(baseline, "Changing StopLossPercent did not affect submitted orders.");
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S1704_MartiniStartsWithRealStopOrders()
	{
		var recorder = new OrderTraceRecorder();

		await Replay("1704_Martini_Martingale", (strategy, _) => recorder.Attach(strategy));

		recorder.AssertFirstTwoAreOppositeConditionalStops();
	}

	[TestMethod]
	[TestCategory("Shard01")]
	public async Task S1801_PerceptronStopEndsCurrentBar()
	{
		var recorder = new OrderTraceRecorder();

		await Replay("1801_Artificial_Intelligence_Perceptron", (strategy, _) =>
		{
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "StopLoss", 1m);
			recorder.Attach(strategy);
		});

		recorder.AssertAtMostOneOrderPerTimestamp();
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public Task S2000_HftSpreaderForForts()
		// A full month creates tens of thousands of fills. One natural day still
		// exercises hundreds of entry/exit cycles without turning CI into a load test.
		=> Replay("2000_HFT_Spreader_for_FORTS", null, TimeSpan.FromDays(1));

	[TestMethod]
	[TestCategory("Shard00")]
	public Task S2096_BreakoutBarsTrend()
		// Compact parameters make the signal reachable in the bundled history window.
		=> Replay("2096_Breakout_Bars_Trend", (s, _) =>
		{
			s.Volume = 0.001m;
			SetParam(s, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(s, "Negatives", 0);
		});

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S2208_HedgeAverageProtectionStartsAfterEntryBar()
	{
		var recorder = new OrderTraceRecorder();

		await Replay("2208_Hedge_Average", (strategy, _) =>
		{
			SetParam(strategy, "Period1", 2);
			SetParam(strategy, "Period2", 3);
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "StopLoss", 0.01m);
			SetParam(strategy, "TakeProfit", 0.01m);
			recorder.Attach(strategy);
		}, TimeSpan.FromDays(2));

		recorder.AssertFirstOppositeAfter(TimeSpan.FromTicks(1));
		recorder.AssertFirstOppositeWithin(TimeSpan.FromMinutes(10));
	}

	[TestMethod]
	[TestCategory("Shard03")]
	public async Task S2403_ReOpenPositions()
	{
		var recorder = new OrderTraceRecorder();

		await Replay("2403_ReOpen_Positions", (strategy, _) =>
		{
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "ProfitThreshold", -1_000_000m);
			SetParam(strategy, "MaxPositions", 3);
			SetParam(strategy, "StopLossPoints", 100m);
			SetParam(strategy, "TakeProfitPoints", 100m);
			recorder.Attach(strategy);
		}, TimeSpan.FromDays(2));

		recorder.AssertFirstSide(Sides.Buy);
		recorder.AssertBasketExitAfterEntries(3, 3m);
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S2502_21HourSessionBreakout()
	{
		var recorder = new OrderTraceRecorder();

		await Replay("2502_21Hour_Session_Breakout", (strategy, _) =>
		{
			strategy.Security.PriceStep = 0.01m;
			SetParam(strategy, "CandleType", TimeSpan.FromHours(4).TimeFrame());
			SetParam(strategy, "FirstSessionStartHour", 20);
			SetParam(strategy, "FirstSessionStopHour", 21);
			SetParam(strategy, "StepPoints", 1m);
			SetParam(strategy, "TakeProfitPoints", 1_000_000m);
			recorder.Attach(strategy);
		}, TimeSpan.FromDays(2));

		recorder.AssertFirstOrderHour(20);
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S2606_StatisticsRepeatingBehavior()
	{
		var recorder = new OrderTraceRecorder();

		await Replay("2606_Statistics_Repeating_Behavior", (strategy, _) =>
		{
			strategy.Security.VolumeStep = 0.5m;
			strategy.Security.MinVolume = 1m;
			strategy.Security.MaxVolume = 10m;
			SetParam(strategy, "InitialVolume", 3.4m);
			SetParam(strategy, "MartingaleFactor", 2m);
			SetParam(strategy, "StopLossPips", 1);
			recorder.Attach(strategy);
		}, TimeSpan.FromDays(2));

		recorder.AssertFirstVolume(3m);
		recorder.AssertContainsVolume(6m);
	}

	[TestMethod]
	[TestCategory("Shard07")]
	public Task S2679_MulticurrencyOverlayHedge()
		=> Replay("2679_Multicurrency_Overlay_Hedge", (s, sec2) =>
		{
			SetParam(s, "Universe", new[] { s.Security, sec2 });
			SetParam(s, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(s, "CorrelationThreshold", 0.01m);
			SetParam(s, "CorrelationLookback", 50);
			SetParam(s, "RangeLength", 20);
			SetParam(s, "AtrLookback", 20);
			SetParam(s, "MaxSpread", 100000m);
			SetParam(s, "OverlayThreshold", 0.001m);
			SetParam(s, "RecalculationHour", 0);
		});

	[TestMethod]
	[TestCategory("Shard01")]
	public Task S2705_Spreader2()
		=> Replay("2705_Spreader_2", (s, sec2) =>
		{
			SetParam(s, "SecondSecurity", sec2);
			SetParam(s, "DayBars", 10);
			SetParam(s, "ShiftLength", 3);
			SetParam(s, "TargetProfit", 1m);
		});

	/// <summary>
	/// The hedge is the example: both legs open in the same direction and are closed together when
	/// their combined open profit reaches the money target. One filled order on one instrument is
	/// not that, so the orders have to show both.
	/// </summary>
	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S2798_ImproveMaRsiHedge()
	{
		var recorder = new PairedOrderRecorder();
		Security primary = null;
		Security hedge = null;

		await Replay("2798_Improve_MA_RSI_Hedge", (strategy, second) =>
		{
			IsTrue(
				strategy.Parameters.TryGetValue("HedgeSecurity", out _),
				"README makes the hedge instrument a required input, so it has to be a parameter.");
			SetParam(strategy, "HedgeSecurity", second);
			primary = strategy.Security;
			hedge = second;
			recorder.Attach(strategy);
		});

		recorder.AssertTradesBoth(primary, hedge);
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S2808_MultiPairCloserClosesBasketAtProfitTarget()
	{
		// The target is set to the highest result the basket reaches, so it closes where it first gets there.
		var known = await ProbeMultiPairCloserBasket();
		var peak = known.Max(evaluation => evaluation.Basket.Value);

		IsTrue(peak > 0m, $"The replayed hour must lift the basket above zero, or no profit target can be reached; it peaked at {peak}.");

		var run = await ReplayMultiPairCloserBasket(peak, MultiPairCloserUnreachable, MultiPairCloserPublishedMinAge);

		run.AssertSingleClosing(known.First(evaluation => evaluation.Basket == peak).At, MultiPairCloserFixture.ProfitTargetReason, 2);
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S2808_MultiPairCloserClosesBasketBelowMaxLoss()
	{
		// The limit is set at the first low of the basket below zero, which is not below the limit, so the basket
		// closes at the next, lower low, the first result that is.
		var known = await ProbeMultiPairCloserBasket();
		var lows = new List<int>();

		for (var index = 0; index < known.Length; index++)
		{
			if (known[index].Basket < 0m && (lows.Count == 0 || known[index].Basket < known[lows[^1]].Basket))
				lows.Add(index);
		}

		IsTrue(lows.Count >= 2, $"The replayed hour must take the basket below zero twice, each time lower; it did {lows.Count} time(s).");

		var run = await ReplayMultiPairCloserBasket(MultiPairCloserUnreachable, -known[lows[0]].Basket.Value, MultiPairCloserPublishedMinAge);

		run.AssertSingleClosing(known[lows[1]].At, MultiPairCloserFixture.MaxLossReason, 2);
	}

	// Neither limit can be reached with it: a basket of a few coins never moves a million.
	private const decimal MultiPairCloserUnreachable = 1000000m;

	private const int MultiPairCloserPublishedMinAge = 60;

	// The basket of the long and the short leg as the replay moves it, from the minute the legs may be closed until
	// the probe closes them: both limits at zero make every result act, and MinAgeSeconds holds the legs until minute 50,
	// early enough for the replay to run on past the closing.
	private async Task<(TimeSpan At, decimal? Basket, decimal?[] Reported)[]> ProbeMultiPairCloserBasket()
	{
		var probe = await ReplayMultiPairCloserBasket(0m, 0m, 2400);

		probe.AssertSingleClosing(50, null, 2);

		return [.. probe.Baskets.Where(evaluation => evaluation.At >= TimeSpan.FromMinutes(11) && evaluation.At < TimeSpan.FromMinutes(50) && evaluation.Basket is not null)];
	}

	// A long leg 0 and a short leg 1 opened at minute 10, both watched.
	private async Task<MultiPairCloserFixture> ReplayMultiPairCloserBasket(decimal profitTarget, decimal maxLoss, int minAgeSeconds)
	{
		var fixture = MultiPairCloserFixture.LongShortBasket();

		await Replay(MultiPairCloser, (strategy, secondary) =>
		{
			MultiPairCloserFixture.WatchBothLegs(strategy, secondary);
			SetParam(strategy, "ProfitTarget", profitTarget);
			SetParam(strategy, "MaxLoss", maxLoss);
			SetParam(strategy, "MinAgeSeconds", minAgeSeconds);
			fixture.Attach(strategy, secondary);
		}, MultiPairCloserFixture.ReplayDuration);

		fixture.AssertFollowsReadme();
		return fixture;
	}

	[TestMethod]
	[TestCategory("Shard03")]
	public async Task S2907_CcfpProducesTwoLegNonUsdSignal()
	{
		var recorder = new OrderTraceRecorder();

		await Replay("2907_CCFp_Currency_Strength", (strategy, second) =>
		{
			var firstId = strategy.Security.Id;
			var secondId = second.Id;
			// Base-currency majors and quote-currency majors must use distinct streams.
			// Mixing orientations on the same stream makes its strength cancel USD and
			// maps both extrema to one security, so that fixture cannot exercise two legs.
			foreach (var name in new[] { "EURUSD", "GBPUSD", "AUDUSD", "NZDUSD" })
				SetParam(strategy, name, firstId);
			foreach (var name in new[] { "USDCAD", "USDCHF", "USDJPY" })
				SetParam(strategy, name, secondId);
			SetParam(strategy, "FastMa", 2);
			SetParam(strategy, "SlowMa", 3);
			SetParam(strategy, "StrengthStep", 0.000001m);
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			recorder.Attach(strategy);
		});

		recorder.AssertContainsTwoLegSignal("(TOPDOWN)");
	}

	/// <summary>
	/// Folder key of the Multi Timeframe MACD example.
	/// </summary>
	protected const string MultiTimeframeMacd = "1101_Multi_Timeframe_MACD";

	[TestMethod]
	[TestCategory("Shard05")]
	[DataRow(true, true)]
	[DataRow(true, false)]
	[DataRow(false, true)]
	[DataRow(false, false)]
	public async Task S1101_ShowSwitchesDrawEachTimeframeInItsOwnArea(bool showCurrent, bool showHigher)
	{
		var working = TimeSpan.FromMinutes(5).TimeFrame();
		var higher = TimeSpan.FromHours(1).TimeFrame();
		var chart = new ChartRecorder();
		var finished = new Dictionary<DataType, List<DateTime>> { [working] = [], [higher] = [] };

		await Replay(MultiTimeframeMacd, (strategy, _) =>
		{
			AssertMultiTimeframeMacdDefaults(strategy);

			SetParam(strategy, "ShowCurrentTimeframe", showCurrent);
			SetParam(strategy, "ShowHigherTimeframe", showHigher);
			strategy.SetChart(chart.Chart);

			// The platform draws a subscription's candles only while the strategy is started.
			strategy.CandleReceived += (subscription, candle) =>
			{
				if (candle.State == CandleStates.Finished && strategy.ProcessState == ProcessStates.Started && finished.TryGetValue(subscription.DataType, out var times))
					times.Add(candle.OpenTime);
			};
		}, TimeSpan.FromDays(4));

		var shown = new List<DataType>();

		if (showCurrent)
			shown.Add(working);

		if (showHigher)
			shown.Add(higher);

		var areas = chart.Areas;
		AreEqual(shown.Count, areas.Length, "Each shown timeframe gets its own chart area, the working one first, and a hidden one gets none.");

		for (var i = 0; i < areas.Length; i++)
		{
			var frame = shown[i];
			var elements = chart.ElementsOf(areas[i]);
			AreEqual(1, elements.Length, $"The {frame} area shows the candles of that timeframe alone.");
			IsTrue(elements[0] is IChartCandleElement, $"The {frame} area must show candles.");

			var drawn = chart.DrawnOn(elements[0]);
			IsTrue(drawn.All(value => value.Values is [DataType dataType, ..] && dataType.Equals(frame)), $"The {frame} area must draw the {frame} subscription only.");
			IsTrue(finished[frame].Count > 0);
			AreEqual(finished[frame], drawn.Select(value => value.Time).ToList(), $"Every finished {frame} candle must be drawn in its area once, in order.");
		}
	}

	/// <summary>
	/// The default values the README publishes, in either language.
	/// </summary>
	protected static void AssertMultiTimeframeMacdDefaults(Strategy strategy)
	{
		AreEqual(12, strategy.Parameters["FastLength"].Value);
		AreEqual(26, strategy.Parameters["SlowLength"].Value);
		AreEqual(9, strategy.Parameters["SignalLength"].Value);
		AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
		AreEqual(TimeSpan.FromHours(1).TimeFrame(), strategy.Parameters["HigherCandleType"].Value);
		AreEqual(true, strategy.Parameters["ShowCurrentTimeframe"].Value);
		AreEqual(true, strategy.Parameters["ShowHigherTimeframe"].Value);
		AreEqual("Crossover", strategy.Parameters["Entry"].Value.ToString());
		AreEqual(false, strategy.Parameters["UseTrailingStop"].Value);
		AreEqual(2m, Convert.ToDecimal(strategy.Parameters["TrailingStopPercent"].Value));
	}

	private const string PriceFlip = "1189_Price_Flip";

	[TestMethod]
	[TestCategory("Shard05")]
	[DataRow(100, 100, 12, 14, true)]
	[DataRow(30, 50, 5, 20, false)]
	[DataRow(3, 4, 20, 5, false)]
	public async Task S1189_MirroredRangeSideAndFastSlowSmaCrossReverse(int maxLookback, int minLookback, int fastLength, int slowLength, bool useTrendFilter)
	{
		// The Python example averages in floating point, so a comparison within this distance of a tie may fall either way there.
		var tolerance = IsPython ? 0.000001m : 0m;
		var otherWindows = Math.Max(Math.Max(maxLookback, minLookback), slowLength);
		var warmup = Math.Max(otherWindows, fastLength);
		var window = new List<ICandleMessage>();
		decimal? previousClose = null;
		decimal? previousInverted = null;
		decimal? previousFast = null;
		decimal? previousSlow = null;
		Sides? expectedSide = null;
		var optionalSides = new HashSet<Sides>();
		var expectedVolume = 0m;
		var bars = 0;
		var readyBars = 0;
		var expectedOrders = 0;
		var actualOrders = 0;
		var ambiguousBars = 0;
		var ambiguousOrders = 0;
		var buys = 0;
		var sells = 0;
		var reversals = 0;
		var truncatedFastSides = 0;
		var violations = new List<string>();

		await Replay(PriceFlip, (strategy, _) =>
		{
			AreEqual(100, strategy.Parameters["TickerMaxLookback"].Value);
			AreEqual(100, strategy.Parameters["TickerMinLookback"].Value);
			AreEqual(12, strategy.Parameters["FastMaLength"].Value);
			AreEqual(14, strategy.Parameters["SlowMaLength"].Value);
			AreEqual(true, strategy.Parameters["UseTrendFilter"].Value);
			SetParam(strategy, "TickerMaxLookback", maxLookback);
			SetParam(strategy, "TickerMinLookback", minLookback);
			SetParam(strategy, "FastMaLength", fastLength);
			SetParam(strategy, "SlowMaLength", slowLength);
			SetParam(strategy, "UseTrendFilter", useTrendFilter);

			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				expectedSide = null;
				optionalSides.Clear();
				bars++;
				window.Add(candle);
				if (window.Count > warmup)
					window.RemoveAt(0);
				if (window.Count < warmup)
					return;

				readyBars++;
				decimal Sma(int length) => window.Skip(window.Count - length).Average(c => c.ClosePrice);
				var fast = Sma(fastLength);
				var slow = Sma(slowLength);
				var close = candle.ClosePrice;
				var position = strategy.Position;

				if (fastLength > otherWindows && Math.Sign(Sma(otherWindows) - slow) != Math.Sign(fast - slow))
					truncatedFastSides++;

				if (previousClose is decimal prevClose && previousInverted is decimal prevInverted &&
					previousFast is decimal prevFast && previousSlow is decimal prevSlow)
				{
					var actions = new HashSet<Sides?>();
					foreach (var previousCross in PriceFlipSigns(prevFast - prevSlow, tolerance))
						foreach (var cross in PriceFlipSigns(fast - slow, tolerance))
							foreach (var previousSide in PriceFlipSigns(prevClose - prevInverted, tolerance))
								foreach (var trendSide in PriceFlipSigns(close - slow, tolerance))
								{
									var signal = PriceFlipSignal(previousCross, cross, previousSide, trendSide, useTrendFilter);
									actions.Add(signal > 0 && position <= 0m ? Sides.Buy : signal < 0 && position >= 0m ? Sides.Sell : (Sides?)null);
								}

					if (actions.Count == 1)
					{
						expectedSide = actions.Single();
						if (expectedSide is not null)
							expectedOrders++;
					}
					else
					{
						ambiguousBars++;
						foreach (var action in actions)
						{
							if (action is Sides side)
								optionalSides.Add(side);
						}
					}

					expectedVolume = strategy.Volume + Math.Abs(position);
				}

				var high = window.Skip(window.Count - maxLookback).Max(c => c.HighPrice);
				var low = window.Skip(window.Count - minLookback).Min(c => c.LowPrice);
				previousClose = close;
				previousInverted = high + low - close;
				previousFast = fast;
				previousSlow = slow;
			};

			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				var required = expectedSide == order.Side;
				var optional = !required && optionalSides.Contains(order.Side);

				if (!required && !optional || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
				{
					if (violations.Count < 12)
						violations.Add($"{strategy.CurrentTime:O}: expected {expectedSide?.ToString() ?? "no order"} {expectedVolume} at market, got {order.Side} {order.Volume} {order.Type}. README trades only a {fastLength}/{slowLength} SMA cross with the previous close on the same side of the {maxLookback}/{minLookback} high+low mirror{(useTrendFilter ? " and the close beyond the slow SMA" : string.Empty)}, reversing with Volume + |Position|.");
				}
				else
				{
					if (optional)
						ambiguousOrders++;

					if (order.Side == Sides.Buy)
						buys++;
					else
						sells++;

					if (strategy.Position != 0m)
						reversals++;
				}

				expectedSide = null;
				optionalSides.Clear();
			};
		}, TimeSpan.FromDays(31));

		TestContext.WriteLine($"python={IsPython}, lookbacks={maxLookback}/{minLookback}, sma={fastLength}/{slowLength}, trendFilter={useTrendFilter}: bars={bars}, ready={readyBars}, orders={actualOrders}/{expectedOrders}, ambiguousBars={ambiguousBars}, ambiguousOrders={ambiguousOrders}, buys={buys}, sells={sells}, reversals={reversals}, truncatedFastSides={truncatedFastSides}");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));
		AreEqual(expectedOrders + ambiguousOrders, actualOrders, "Every README signal must act, and nothing else may trade.");
		IsTrue(buys > 0 && sells > 0 && reversals > 0, "The real archive must reach long and short signals and reverse on the opposite one.");

		if (fastLength > otherWindows)
			IsTrue(truncatedFastSides > 0, "The fixture must tell an SMA of FastMaLength closes from one cut to the lookback and slow windows.");
	}

	private static int[] PriceFlipSigns(decimal difference, decimal tolerance)
		=> tolerance > 0m && Math.Abs(difference) <= tolerance ? [-1, 0, 1] : [Math.Sign(difference)];

	private static int PriceFlipSignal(int previousCross, int cross, int previousSide, int trendSide, bool useTrendFilter)
	{
		if (previousCross <= 0 && cross > 0 && previousSide > 0 && (!useTrendFilter || trendSide > 0))
			return 1;

		if (previousCross >= 0 && cross < 0 && previousSide < 0 && (!useTrendFilter || trendSide < 0))
			return -1;

		return 0;
	}

	private const string Renko = "1243_Renko";

	[TestMethod]
	[TestCategory("Shard03")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task S1243_BrickReversalsEnterAndReverseTheWholePosition(bool ticks)
	{
		const decimal boxSize = 10m;
		var brickType = new Unit(boxSize).Renko();

		// Ticks are not pre-cached and triple the Level1 volume; three hours keeps the tick rows inside the 60s replay budget.
		var duration = ticks ? TimeSpan.FromHours(3) : TimeSpan.FromDays(31);

		var bricks = 0;
		(decimal Close, DateTime OpenTime, DateTime CloseTime)? lastBrick = null;
		var lastDirection = 0;
		(Sides Side, decimal Volume)? expected = null;
		var expectedOrders = 0;
		var orders = 0;
		var longEntries = 0;
		var shortEntries = 0;
		var reversals = 0;
		var violations = new List<string>();
		var gaps = new List<string>();
		var missingBricks = 0;

		void violate(string message)
		{
			if (violations.Count < 12)
				violations.Add(message);
		}

		await Replay(Renko, (strategy, _) =>
		{
			AreEqual(boxSize, Convert.ToDecimal(strategy.Parameters["BoxSize"].Value));

			// The README leaves the brick source open; the tick rows hold the same contract on the other source the code offers.
			if (ticks)
				SetParam(strategy, "PriceSource", DataType.Ticks);

			strategy.CandleReceived += (subscription, candle) =>
			{
				// Only a brick that has just completed may call for an order.
				expected = null;

				if (candle.State != CandleStates.Finished)
					return;

				bricks++;

				if (!brickType.Equals(subscription.DataType))
					violate($"{strategy.CurrentTime:O}: README trades Renko bricks of {boxSize}, got {subscription.DataType}.");

				if (ticks && !DataType.Ticks.Equals(candle.BuildFrom))
					violate($"{strategy.CurrentTime:O}: brick built from {candle.BuildFrom}, not from the trade ticks PriceSource selects.");

				if (Math.Abs(candle.ClosePrice - candle.OpenPrice) != boxSize)
					violate($"{strategy.CurrentTime:O}: brick {candle.OpenPrice} -> {candle.ClosePrice} is not {boxSize} points.");

				// A gap between consecutive bricks means the strategy never saw the bricks in between.
				if (lastBrick is { } previous && candle.OpenPrice != previous.Close)
				{
					missingBricks += (int)(Math.Abs(candle.OpenPrice - previous.Close) / boxSize);

					if (gaps.Count < 12)
						gaps.Add($"{candle.OpenTime:O}: brick opens at {candle.OpenPrice}, not where the previous brick ({previous.OpenTime:O}..{previous.CloseTime:O}) closed ({previous.Close}).");
				}

				lastBrick = (candle.ClosePrice, candle.OpenTime, candle.CloseTime);

				var direction = Math.Sign(candle.ClosePrice - candle.OpenPrice);

				if (direction == 0)
					return;

				var reversal = lastDirection != 0 && direction != lastDirection;
				lastDirection = direction;

				if (!reversal)
					return;

				var held = strategy.Position;

				if (direction > 0 && held <= 0m)
				{
					expected = (Sides.Buy, strategy.Volume + Math.Abs(held));
					longEntries++;
				}
				else if (direction < 0 && held >= 0m)
				{
					expected = (Sides.Sell, strategy.Volume + Math.Abs(held));
					shortEntries++;
				}

				if (expected is null)
					return;

				expectedOrders++;

				if (held != 0m)
					reversals++;
			};

			strategy.OrderRegistering += order =>
			{
				orders++;

				if (expected is not { } next)
					violate($"{strategy.CurrentTime:O}: {order.Side} {order.Volume} with position {strategy.Position} and no brick reversal calling for it; README enters only on the opposite brick and has no stops.");
				else if (order.Side != next.Side || order.Volume != next.Volume || order.Type != OrderTypes.Market)
					violate($"{strategy.CurrentTime:O}: the brick reversal expected {next.Side} {next.Volume} at market, got {order.Side} {order.Volume} {order.Type}.");

				expected = null;
			};
		}, duration);

		TestContext.WriteLine($"python={IsPython}, ticks={ticks}: bricks={bricks}, missingBricks>={missingBricks}, orders={orders}/{expectedOrders}, longEntries={longEntries}, shortEntries={shortEntries}, reversals={reversals}");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));
		AreEqual(expectedOrders, orders, "Every brick reversal must act, and nothing else may trade.");
		IsTrue(longEntries > 0 && shortEntries > 0, "The archive must reach a bullish brick after a bearish one and a bearish brick after a bullish one.");
		IsTrue(reversals > 0, "The opposite brick must close the open position and enter the other way.");
		IsTrue(gaps.Count == 0, $"At least {missingBricks} bricks never reached the strategy, so it cannot act on every reversal the README trades:{Environment.NewLine}{string.Join(Environment.NewLine, gaps)}");
	}

	private const string RenkoTrendReversalV2 = "1607_Renko_Trend_Reversal_V2";

	[TestMethod]
	[TestCategory("Shard07")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task S1607_RenkoCrossEntriesAndStopOrTargetExits(bool allowShorts)
	{
		var atrLength = 0;
		var stopPercent = 0m;
		var takePercent = 0m;
		var tradeStart = TimeSpan.Zero;
		var tradeEnd = TimeSpan.Zero;

		var bars = 0;
		var atr = 0m;
		decimal? previousClose = null;
		decimal? brickClose = null;
		var lastDirection = 0;

		(Sides Side, decimal Volume)? expected = null;
		var expectedOrders = 0;
		var marketOrders = 0;
		var marketSells = 0;
		var longEntries = 0;
		var shortEntries = 0;
		var reversals = 0;
		var bearishCrossesWhileLong = 0;

		var position = 0m;
		var lowFill = 0m;
		var highFill = 0m;
		var protectiveOrders = new HashSet<Order>();
		var stopExits = 0;
		var takeExits = 0;
		var protectiveCloses = 0;
		var violations = new List<string>();

		void violate(string message)
		{
			if (violations.Count < 12)
				violations.Add(message);
		}

		bool inWindow(TimeSpan time)
			=> tradeStart <= tradeEnd
				? time >= tradeStart && time <= tradeEnd
				: time >= tradeStart || time <= tradeEnd;

		await Replay(RenkoTrendReversalV2, (strategy, _) =>
		{
			atrLength = Convert.ToInt32(strategy.Parameters["RenkoAtrLength"].Value);
			stopPercent = Convert.ToDecimal(strategy.Parameters["StopLossPct"].Value);
			takePercent = Convert.ToDecimal(strategy.Parameters["TakeProfitPct"].Value);
			AreEqual(10, atrLength);
			AreEqual(3m, stopPercent);
			AreEqual(20m, takePercent);
			IsTrue((bool)strategy.Parameters["AllowShorts"].Value, "README trades both directions unless shorts are disabled.");
			SetParam(strategy, "AllowShorts", allowShorts);

			if (!allowShorts)
			{
				// A long that no cross closes must meet its levels within the one-month archive.
				stopPercent = 1m;
				takePercent = 2m;
				SetParam(strategy, "StopLossPct", 1m);
				SetParam(strategy, "TakeProfitPct", 2m);
			}

			tradeStart = (TimeSpan)strategy.Parameters["TradeStart"].Value;
			tradeEnd = (TimeSpan)strategy.Parameters["TradeEnd"].Value;

			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished || candle.SecurityId != strategy.Security.Id.ToSecurityId())
					return;

				expected = null;

				// Wilder ATR of the source candles sizes every brick.
				var trueRange = candle.HighPrice - candle.LowPrice;
				if (previousClose is decimal close)
					trueRange = Math.Max(trueRange, Math.Max(Math.Abs(close - candle.HighPrice), Math.Abs(close - candle.LowPrice)));

				previousClose = candle.ClosePrice;
				bars++;
				var length = Math.Min(bars, atrLength);
				atr = (atr * (length - 1) + trueRange) / length;

				if (bars < atrLength || atr <= 0m)
					return;

				if (brickClose is not decimal level)
				{
					brickClose = candle.ClosePrice;
					return;
				}

				var direction = 0;

				while (candle.ClosePrice >= level + atr)
				{
					level += atr;
					direction = 1;
				}

				while (candle.ClosePrice <= level - atr)
				{
					level -= atr;
					direction = -1;
				}

				brickClose = level;

				if (direction == 0)
					return;

				var cross = lastDirection != 0 && direction != lastDirection;
				lastDirection = direction;

				if (!cross || !inWindow(candle.OpenTime.TimeOfDay))
					return;

				var held = strategy.Position;

				if (direction > 0 && held <= 0m)
				{
					expected = (Sides.Buy, strategy.Volume + Math.Abs(held));
					longEntries++;
				}
				else if (direction < 0 && allowShorts && held >= 0m)
				{
					expected = (Sides.Sell, strategy.Volume + Math.Abs(held));
					shortEntries++;
				}
				else if (direction < 0 && !allowShorts && held > 0m)
				{
					bearishCrossesWhileLong++;
				}

				if (expected is not null)
				{
					expectedOrders++;

					if (held != 0m)
						reversals++;
				}
			};

			strategy.OrderRegistering += order =>
			{
				if (order.Type == OrderTypes.Market)
				{
					marketOrders++;

					if (order.Side == Sides.Sell)
						marketSells++;

					if (expected is not { } next)
						violate($"{strategy.CurrentTime:O}: {order.Side} {order.Volume} at market with position {strategy.Position} and no Renko open/close cross calling for it; README enters on a cross and otherwise leaves only at the {stopPercent}% stop or the {takePercent}% target.");
					else if (order.Side != next.Side || order.Volume != next.Volume)
						violate($"{strategy.CurrentTime:O}: the Renko cross expected {next.Side} {next.Volume} at market, got {order.Side} {order.Volume}.");

					expected = null;
					return;
				}

				protectiveOrders.Add(order);

				if (position == 0m || order.Side != (position > 0m ? Sides.Sell : Sides.Buy) || order.Volume != Math.Abs(position))
				{
					violate($"{strategy.CurrentTime:O}: protective {order.Side} {order.Volume} must close the whole position {position}.");
					return;
				}

				// The native level sits on the average entry price, which lies between the lowest and highest entry fill.
				var step = strategy.Security.PriceStep ?? 0m;
				var stopHit = position > 0m
					? order.Price <= highFill * (1m - stopPercent / 100m) + step
					: order.Price >= lowFill * (1m + stopPercent / 100m) - step;
				var targetHit = position > 0m
					? order.Price >= lowFill * (1m + takePercent / 100m) - step
					: order.Price <= highFill * (1m - takePercent / 100m) + step;

				if (stopHit)
					stopExits++;
				else if (targetHit)
					takeExits++;
				else
					violate($"{strategy.CurrentTime:O}: exit at {order.Price} for position {position} entered at {lowFill}..{highFill} reaches neither the {stopPercent}% stop nor the {takePercent}% target.");
			};

			strategy.Trades.TradeAdded += trade =>
			{
				var volume = trade.Trade.Volume;
				var signed = trade.Order.Side == Sides.Buy ? volume : -volume;
				var before = position;
				position += signed;

				if (position == 0m)
				{
					lowFill = highFill = 0m;

					if (protectiveOrders.Contains(trade.Order))
						protectiveCloses++;
				}
				else if (before == 0m || Math.Sign(before) != Math.Sign(position))
				{
					lowFill = highFill = trade.Trade.Price;
				}
				else if (Math.Sign(signed) == Math.Sign(position))
				{
					lowFill = Math.Min(lowFill, trade.Trade.Price);
					highFill = Math.Max(highFill, trade.Trade.Price);
				}
			};
		}, TimeSpan.FromDays(31));

		TestContext.WriteLine($"python={IsPython}, allowShorts={allowShorts}: bars={bars}, orders={marketOrders}/{expectedOrders}, longEntries={longEntries}, shortEntries={shortEntries}, reversals={reversals}, bearishCrossesWhileLong={bearishCrossesWhileLong}, stopExits={stopExits}, takeExits={takeExits}, protectiveCloses={protectiveCloses}");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));
		AreEqual(expectedOrders, marketOrders, "Every Renko open/close cross inside the time window must act, and nothing else may trade at market.");
		IsTrue(longEntries > 0, "The real archive must reach bullish Renko crosses that open longs.");

		if (allowShorts)
		{
			IsTrue(shortEntries > 0 && reversals > 0, "With shorts allowed a bearish cross must open shorts and the opposite cross must reverse the position on the real archive.");
		}
		else
		{
			AreEqual(0, marketSells, "With shorts disabled no bearish cross may sell at market, neither to open a short nor to close a long.");
			IsTrue(bearishCrossesWhileLong > 0, "The real archive must reach bearish crosses while a long is open, and with shorts disabled they must leave the long alone.");
			IsTrue(protectiveCloses > 0, "With shorts disabled a long must actually leave through the stop or the target.");
		}
	}

	private const string Martini = "1704_Martini_Martingale";

	private sealed class MartiniReplay
	{
		public List<decimal> LegVolumes { get; } = [];
		public int SplitLegs { get; set; }
		public int LegsAfterSplit { get; set; }
		public int Closes { get; set; }
		public List<string> Violations { get; } = [];
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S1704_MartingaleLegDoublesThePreviousOrderVolumeExactly()
	{
		var replay = await ReplayMartini(0.7m, null);
		IsTrue(replay.LegVolumes.Contains(1.4m),
			$"After a 0.7 entry the first martingale leg must be exactly 1.4 on the 0.001 volume grid; legs sent: {string.Join(", ", replay.LegVolumes.Distinct().Take(8))}.");
		IsTrue(replay.Closes > 0, "ProfitClose must actually end cycles in the replay.");
		IsTrue(replay.Violations.Count == 0, string.Join(Environment.NewLine, replay.Violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S1704_LegFilledInSeveralTradesCountsAsOneOrder()
	{
		// A leg larger than the emulator's synthesized liquidity at the touch walks the book and fills in several trades.
		var replay = await ReplayMartini(750_000_000m, 10_000_000_000m);
		IsTrue(replay.SplitLegs > 0 && replay.LegsAfterSplit > 0,
			$"The replay must send a leg after one that filled in several trades: split legs={replay.SplitLegs}, legs after a split={replay.LegsAfterSplit}.");
		IsTrue(replay.Violations.Count == 0, string.Join(Environment.NewLine, replay.Violations.Take(12)));
	}

	private async Task<MartiniReplay> ReplayMartini(decimal initialVolume, decimal? maxVolume)
	{
		var replay = new MartiniReplay();
		var orderCount = 0;
		var lastPrice = 0m;
		var lastLegVolume = 0m;
		var lastLegSplit = false;
		long? countedOrderId = null;
		var closing = false;
		var cycleBase = 0m;
		Order leg = null;
		(Sides Side, decimal Volume, bool Close)? expected = null;
		var expectedAfterSplit = false;
		await Replay(Martini, (strategy, _) =>
		{
			var step = Convert.ToDecimal(strategy.Parameters["Step"].Value);
			var profitClose = Convert.ToDecimal(strategy.Parameters["ProfitClose"].Value);
			SetParam(strategy, "InitialVolume", initialVolume);
			var security = strategy.Security;
			if (maxVolume is decimal max)
				security.MaxVolume = max;

			decimal Normalize(decimal volume)
			{
				if (security.MaxVolume is decimal upper && upper > 0m)
					volume = Math.Min(volume, upper);
				if (security.MinVolume is decimal lower && lower > 0m)
					volume = Math.Max(volume, lower);
				if (security.VolumeStep is decimal grid && grid > 0m)
					volume = Math.Floor(volume / grid) * grid;
				return volume;
			}

			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished || strategy.ProcessState != ProcessStates.Started)
					return;
				if (expected is not null)
					replay.Violations.Add($"{candle.OpenTime:O}: the previous bar required {expected}, but no order was sent.");
				expected = null;
				var position = strategy.Position;
				if (position == 0m)
					return;
				if (!closing && strategy.PnL - cycleBase >= profitClose)
				{
					expected = (position > 0m ? Sides.Sell : Sides.Buy, Math.Abs(position), true);
					return;
				}
				if (closing || orderCount == 0 || leg is { State: not (OrderStates.Done or OrderStates.Failed) })
					return;
				var adverse = step * orderCount;
				if (position > 0m && candle.LowPrice <= lastPrice - adverse)
					expected = (Sides.Sell, Normalize(lastLegVolume * 2m), false);
				else if (position < 0m && candle.HighPrice >= lastPrice + adverse)
					expected = (Sides.Buy, Normalize(lastLegVolume * 2m), false);
				expectedAfterSplit = lastLegSplit;
			};
			strategy.OrderRegistering += order =>
			{
				if (order.Type == OrderTypes.Conditional)
				{
					cycleBase = strategy.PnL;
					if (order.Volume != initialVolume || strategy.Position != 0m)
						replay.Violations.Add($"{strategy.CurrentTime:O}: initial stop {order.Side} {order.Volume} must carry InitialVolume {initialVolume} while flat.");
					return;
				}
				if (expected is not { } wanted)
				{
					replay.Violations.Add($"{strategy.CurrentTime:O}: market {order.Side} {order.Volume} was sent without a README reason.");
					return;
				}
				expected = null;
				if (order.Side != wanted.Side || order.Volume != wanted.Volume)
					replay.Violations.Add($"{strategy.CurrentTime:O}: expected {wanted.Side} {wanted.Volume}, sent {order.Side} {order.Volume}.");
				if (wanted.Close)
				{
					closing = true;
					replay.Closes++;
					return;
				}
				leg = order;
				replay.LegVolumes.Add(order.Volume);
				if (expectedAfterSplit)
					replay.LegsAfterSplit++;
			};
			strategy.Trades.TradeAdded += trade =>
			{
				if (closing)
				{
					if (strategy.Position == 0m)
					{
						closing = false;
						orderCount = 0;
						lastPrice = lastLegVolume = 0m;
						lastLegSplit = false;
						countedOrderId = null;
						leg = null;
					}
					return;
				}
				lastPrice = trade.Trade.Price;
				if (countedOrderId != trade.Order.TransactionId)
				{
					countedOrderId = trade.Order.TransactionId;
					orderCount++;
					lastLegVolume = trade.Order.Volume;
					lastLegSplit = false;
				}
				else if (!lastLegSplit)
				{
					lastLegSplit = true;
					replay.SplitLegs++;
				}
			};
		}, TimeSpan.FromDays(3));
		return replay;
	}

	protected const string TimerExample = "1788_Timer";

	protected sealed class TimerReplay
	{
		public int Entries;
		public int Reversals;
		public int SameWindowEntries;
		public int TrailingExits;
		public int TrailingExitsAfterAdvance;
		public int WickTrailingExits;
		public int ProtectiveExits;
		public int StopLossCandles;
		public int StopLossCandlesInsideTrail;
		public int TakeProfitCandles;

		public List<string> Violations { get; } = [];
		public List<string> Trace { get; } = [];
	}

	[TestMethod]
	[TestCategory("Shard04")]
	[DataRow(60, 0)]
	[DataRow(1800, 1)]
	public async Task S1788_LevelsStayValidAfterEntryUntilRecalculation(int waitSeconds, int minSameWindowEntries)
	{
		var replay = await ReplayTimer(waitSeconds, takeProfit: 0m, stopLoss: 0m, trailingStop: 0m);

		IsTrue(replay.Violations.Count == 0, string.Join(Environment.NewLine, replay.Violations));
		IsTrue(replay.Entries > 0 && replay.Reversals > 0, "The archive must produce level breakouts that open and reverse positions.");
		IsTrue(replay.SameWindowEntries >= minSameWindowEntries,
			$"Levels set every {waitSeconds}s must stay valid after an entry: the opposite level has to reverse the position before the next recalculation (observed {replay.SameWindowEntries}).");
		AreEqual(0, replay.ProtectiveExits);
	}

	[TestMethod]
	[TestCategory("Shard04")]
	public async Task S1788_FixedStopLossStaysActiveBesideTrailingStop()
	{
		var replay = await ReplayTimer(waitSeconds: 60, takeProfit: 0m, stopLoss: 20m, trailingStop: 50m);

		IsTrue(replay.Violations.Count == 0, string.Join(Environment.NewLine, replay.Violations));
		IsTrue(replay.StopLossCandles > 0 && replay.StopLossCandlesInsideTrail > 0,
			"The archive must close past the 20-point stop-loss while still inside the 50-point trailing distance, where a trailing stop standing in for the stop-loss would keep the position.");
		IsTrue(replay.ProtectiveExits > 0 && replay.TrailingExits > 0, "The stop-loss and the trailing stop must both close positions in one run.");
	}

	[TestMethod]
	[TestCategory("Shard04")]
	public async Task S1788_TrailingStopFollowsBestPriceSinceEntry()
	{
		var replay = await ReplayTimer(waitSeconds: 60, takeProfit: 0m, stopLoss: 5000m, trailingStop: 300m);

		IsTrue(replay.Violations.Count == 0, string.Join(Environment.NewLine, replay.Violations));
		IsTrue(replay.TrailingExits > 0 && replay.TrailingExitsAfterAdvance > 0 && replay.WickTrailingExits > 0,
			"The archive must exercise trailing exits measured from a best price that moved past the entry, including ones reached by a candle's extreme but not by its close.");
	}

	/// <summary>
	/// Replays the example in this class's language against the README model.
	/// </summary>
	protected Task<TimerReplay> ReplayTimer(int waitSeconds, decimal takeProfit, decimal stopLoss, decimal trailingStop)
		=> ReplayTimer((setup, duration) => Replay(TimerExample, setup, duration), waitSeconds, takeProfit, stopLoss, trailingStop);

	/// <summary>
	/// Replays the strategy over the archive while an independent model of the README rules predicts
	/// every order the candle handler registers. Protective exits are registered by the platform before
	/// the handler runs; they are only checked to close the position, and a close past the stop-loss or
	/// take-profit distance from the fill must be answered by one.
	/// </summary>
	/// <param name="run">Runs the example with the given setup over the given span of the archive.</param>
	protected async Task<TimerReplay> ReplayTimer(Func<Action<Strategy, Security>, TimeSpan, Task> run, int waitSeconds, decimal takeProfit, decimal stopLoss, decimal trailingStop)
	{
		var result = new TimerReplay();
		var expected = new Queue<(Sides Side, decimal Volume, string Rule)>();
		AverageTrueRange atr = null;
		var step = 1m;
		var pipDistance = 0m;
		var tradeVolume = 0m;
		decimal? buyLevel = null;
		decimal? sellLevel = null;
		DateTime? lastLevelTime = null;
		var windowHasEntry = false;
		decimal? extreme = null;
		var entryClose = 0m;
		var planned = 0m;
		var filled = 0m;
		var fillPrice = 0m;
		var bestSinceFill = 0m;
		(decimal Held, decimal FillPrice, decimal Best)? beforeProtection = null;
		var protectionNet = 0m;
		var inStrategyHandler = false;
		var afterHandlerAttached = false;

		void violate(DateTime time, string text)
		{
			if (result.Violations.Count < 12)
				result.Violations.Add($"{time:O}: {text}");
		}

		await run((strategy, _) =>
		{
			foreach (var name in new[] { "WaitSeconds", "PipDistance", "AtrPeriod", "TakeProfit", "StopLoss", "TrailingStop", "TradeVolume", "CandleType", "UseTradingHours", "StartTime", "StopTime" })
				IsTrue(strategy.Parameters.ContainsKey(name), $"The README parameter {name} is missing.");

			SetParam(strategy, "WaitSeconds", waitSeconds);
			SetParam(strategy, "TakeProfit", takeProfit);
			SetParam(strategy, "StopLoss", stopLoss);
			SetParam(strategy, "TrailingStop", trailingStop);
			SetParam(strategy, "UseTradingHours", false);

			atr = new AverageTrueRange { Length = Convert.ToInt32(strategy.Parameters["AtrPeriod"].Value) };
			pipDistance = Convert.ToDecimal(strategy.Parameters["PipDistance"].Value);
			tradeVolume = Convert.ToDecimal(strategy.Parameters["TradeVolume"].Value);
			step = strategy.Security.PriceStep is decimal priceStep && priceStep > 0m ? priceStep : 1m;

			strategy.OrderRegistering += order =>
			{
				var signed = order.Side == Sides.Buy ? order.Volume : -order.Volume;
				result.Trace.Add($"O|{strategy.CurrentTime:O}|{(inStrategyHandler ? "rule" : "protection")}|{order.Side}|{order.Volume:0.########}");

				if (inStrategyHandler)
				{
					if (!expected.TryDequeue(out var next))
						violate(strategy.CurrentTime, $"{order.Side} {order.Volume} was registered although no README rule calls for an order.");
					else if (next.Side != order.Side || next.Volume != order.Volume || order.Type != OrderTypes.Market)
						violate(strategy.CurrentTime, $"{order.Type} {order.Side} {order.Volume} was registered where the {next.Rule} calls for a market {next.Side} {next.Volume}.");
				}
				else
				{
					beforeProtection ??= (filled, fillPrice, bestSinceFill);
					var held = beforeProtection.Value.Held + protectionNet;

					if (held == 0m || Math.Sign(signed) == Math.Sign(held) || order.Volume != Math.Abs(held) || order.Type != OrderTypes.Market)
						violate(strategy.CurrentTime, $"A protective {order.Type} {order.Side} {order.Volume} does not close the {held} position.");

					protectionNet += signed;
					result.ProtectiveExits++;
				}

				planned += signed;
			};

			strategy.Trades.TradeAdded += trade =>
			{
				var price = trade.Trade.Price;
				var volume = trade.Order.Side == Sides.Buy ? trade.Trade.Volume : -trade.Trade.Volume;
				result.Trace.Add($"F|{strategy.CurrentTime:O}|{trade.Order.Side}|{price}|{trade.Trade.Volume:0.########}");

				var before = filled;
				filled += volume;

				if (filled == 0m)
					fillPrice = bestSinceFill = 0m;
				else if (before == 0m || Math.Sign(before) != Math.Sign(filled))
					fillPrice = bestSinceFill = price;
				else if (Math.Sign(volume) == Math.Sign(filled))
					fillPrice = (fillPrice * Math.Abs(before) + price * Math.Abs(volume)) / Math.Abs(filled);
			};

			strategy.CandleReceived += (_, candle) =>
			{
				if (!afterHandlerAttached)
				{
					// Subscribed after the strategy's own candle handler, so it runs once that handler returns.
					afterHandlerAttached = true;
					strategy.CandleReceived += (_, handled) =>
					{
						inStrategyHandler = false;

						if (handled.State != CandleStates.Finished)
							return;

						if (expected.Count > 0)
							violate(strategy.CurrentTime, $"The strategy did not register {string.Join(", ", expected.Select(e => $"{e.Side} {e.Volume} ({e.Rule})"))}.");

						expected.Clear();
						beforeProtection = null;
						protectionNet = 0m;
					};
				}

				inStrategyHandler = true;

				if (candle.State != CandleStates.Finished || strategy.ProcessState != ProcessStates.Started)
					return;

				var close = candle.ClosePrice;
				var (held, heldFillPrice, heldBest) = beforeProtection ?? (filled, fillPrice, bestSinceFill);

				if (held != 0m)
				{
					var isLong = held > 0m;
					var isClosed = held + protectionNet == 0m;

					if (stopLoss > 0m && (isLong ? close <= heldFillPrice - (stopLoss + 1m) * step : close >= heldFillPrice + (stopLoss + 1m) * step))
					{
						result.StopLossCandles++;

						if (trailingStop > 0m && (isLong ? close > heldBest - trailingStop * step : close < heldBest + trailingStop * step))
							result.StopLossCandlesInsideTrail++;

						if (!isClosed)
							violate(candle.CloseTime, $"Close {close} is past the {stopLoss}-point stop-loss of the {held} position filled at {heldFillPrice}, yet nothing closed it.");
					}

					if (takeProfit > 0m && (isLong ? close >= heldFillPrice + (takeProfit + 1m) * step : close <= heldFillPrice - (takeProfit + 1m) * step))
					{
						result.TakeProfitCandles++;

						if (!isClosed)
							violate(candle.CloseTime, $"Close {close} is past the {takeProfit}-point take-profit of the {held} position filled at {heldFillPrice}, yet nothing closed it.");
					}

					if (beforeProtection is null)
						bestSinceFill = isLong ? Math.Max(heldBest, candle.HighPrice) : Math.Min(heldBest, candle.LowPrice);
				}

				var atrValue = atr.Process(candle);

				if (!atr.IsFormed || atrValue.IsEmpty || atrValue.GetValue<decimal>() <= 0m)
					return;

				var position = planned;

				if (position == 0m)
				{
					extreme = null;
				}
				else if (trailingStop > 0m && extreme is decimal best)
				{
					var trail = trailingStop * step;
					var isLong = position > 0m;

					if (isLong ? candle.LowPrice <= best - trail : candle.HighPrice >= best + trail)
					{
						expected.Enqueue((isLong ? Sides.Sell : Sides.Buy, Math.Abs(position), "trailing stop"));
						result.TrailingExits++;

						if (best != entryClose)
							result.TrailingExitsAfterAdvance++;

						if (isLong ? close > best - trail : close < best + trail)
							result.WickTrailingExits++;

						position = 0m;
						extreme = null;
					}
					else
					{
						extreme = isLong ? Math.Max(best, candle.HighPrice) : Math.Min(best, candle.LowPrice);
					}
				}

				Sides? entry = buyLevel is decimal buy && close >= buy && position <= 0m ? Sides.Buy
					: sellLevel is decimal sell && close <= sell && position >= 0m ? Sides.Sell
					: null;

				if (entry is Sides side)
				{
					expected.Enqueue((side, tradeVolume + Math.Abs(position), "level breakout"));
					result.Entries++;

					if (position != 0m)
						result.Reversals++;

					if (windowHasEntry)
						result.SameWindowEntries++;

					windowHasEntry = true;
					extreme = entryClose = close;
				}

				if (lastLevelTime is not DateTime last || candle.CloseTime - last >= TimeSpan.FromSeconds(waitSeconds))
				{
					var distance = pipDistance * step + atrValue.GetValue<decimal>();
					buyLevel = close + distance;
					sellLevel = close - distance;
					lastLevelTime = candle.CloseTime;
					windowHasEntry = false;
				}
			};
		}, TimeSpan.FromDays(31));

		return result;
	}

	private const string Go = "1807_GO";

	private sealed class GoRun
	{
		public List<decimal> Values { get; } = [];
		public List<string> Violations { get; } = [];
		public int ReadyBars { get; set; }
		public int ExpectedOrders { get; set; }
		public int ActualOrders { get; set; }
		public int AmbiguousBars { get; set; }
		public int AmbiguousOrders { get; set; }
		public int LongCloses { get; set; }
		public int ShortCloses { get; set; }
		public int Reversals { get; set; }
		public int Closes => LongCloses + ShortCloses;
	}

	[TestMethod]
	[TestCategory("Shard07")]
	public async Task S1807_CloseAndOppositeOpenActOnTheSameFinishedCandle()
	{
		var zero = await ReplayGo(0m, 0m);
		AssertGoRun(zero, 0m, 0m);
		IsTrue(zero.LongCloses > 0 && zero.ShortCloses > 0, "The archive must close both a long and a short by the README GO rules.");
		AreEqual(zero.Closes, zero.Reversals, "With both levels at zero a close always meets the opposite open rule on the same candle, so every close must reverse at once.");

		var levels = PickGoLevels(zero.Values);
		IsTrue(levels is not null, "The recorded GO series must offer levels that produce both a plain close and a close followed by the opposite open.");
		var (openLevel, closeLevelDiff) = levels.Value;

		var spread = await ReplayGo(openLevel, closeLevelDiff);
		AssertGoRun(spread, openLevel, closeLevelDiff);
		IsTrue(spread.Reversals > 0 && spread.Closes > spread.Reversals, "Positive levels must both close without a new position and reverse when GO also passes the opposite open level.");
	}

	private async Task<GoRun> ReplayGo(decimal openLevel, decimal closeLevelDiff)
	{
		var run = new GoRun();
		var expected = new Queue<(Sides side, decimal volume)>();
		var ambiguousBar = false;
		var bars = 0;
		decimal? openEma = null;
		decimal? highEma = null;
		decimal? lowEma = null;
		decimal? closeEma = null;

		void RequirePlaced(string when)
		{
			if (expected.Count == 0)
				return;

			run.Violations.Add($"{when}: {string.Join(", ", expected.Select(order => $"{order.side} {order.volume}"))} required by the README GO rules on the previous finished candle was not placed on it.");
			expected.Clear();
		}

		await Replay(Go, (strategy, _) =>
		{
			foreach (var name in new[] { "MaPeriod", "OpenLevel", "CloseLevelDiff", "ShowGo", "CandleType" })
				IsTrue(strategy.Parameters.ContainsKey(name), $"README parameter {name} must exist.");

			var period = Convert.ToInt32(strategy.Parameters["MaPeriod"].Value);
			SetParam(strategy, "OpenLevel", openLevel);
			SetParam(strategy, "CloseLevelDiff", closeLevelDiff);

			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished || strategy.ProcessState != ProcessStates.Started)
					return;

				RequirePlaced($"{candle.OpenTime:O}");
				ambiguousBar = false;
				bars++;
				openEma = GoEma(openEma, candle.OpenPrice, period);
				highEma = GoEma(highEma, candle.HighPrice, period);
				lowEma = GoEma(lowEma, candle.LowPrice, period);
				closeEma = GoEma(closeEma, candle.ClosePrice, period);

				if (bars < period)
					return;

				var o = openEma.Value;
				var h = highEma.Value;
				var l = lowEma.Value;
				var c = closeEma.Value;
				var go = ((c - o) + (h - o) + (l - o) + (c - l) + (c - h)) * candle.TotalVolume;
				run.ReadyBars++;
				run.Values.Add(go);

				// The Python example works in floating point, so GO within this distance of a level may compare either way there.
				var margin = IsPython ? 0.000001m * (1m + candle.TotalVolume) : 0m;

				if (margin > 0m && new[] { openLevel, openLevel - closeLevelDiff }.Any(level => Math.Abs(Math.Abs(go) - Math.Abs(level)) <= margin))
				{
					run.AmbiguousBars++;
					ambiguousBar = true;
					return;
				}

				var position = strategy.Position;
				var orders = GoOrders(position, go, openLevel, closeLevelDiff, strategy.Volume);

				foreach (var order in orders)
					expected.Enqueue((order.side, order.volume));

				run.ExpectedOrders += orders.Length;

				if (orders.Any(order => order.close))
				{
					if (position > 0m)
						run.LongCloses++;
					else
						run.ShortCloses++;

					if (orders.Length > 1)
						run.Reversals++;
				}
			};

			strategy.OrderRegistering += order =>
			{
				run.ActualOrders++;

				if (ambiguousBar)
				{
					run.AmbiguousOrders++;
					return;
				}

				if (!expected.TryDequeue(out var next))
					run.Violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume} {order.Type} follows no README GO rule.");
				else if (order.Side != next.side || order.Volume != next.volume || order.Type != OrderTypes.Market)
					run.Violations.Add($"{strategy.CurrentTime:O}: expected {next.side} {next.volume} at market, got {order.Side} {order.Volume} {order.Type}.");
			};
		}, TimeSpan.FromDays(31));

		RequirePlaced("end of replay");
		return run;
	}

	private void AssertGoRun(GoRun run, decimal openLevel, decimal closeLevelDiff)
	{
		TestContext.WriteLine($"python={IsPython}, OpenLevel={openLevel}, CloseLevelDiff={closeLevelDiff}: ready={run.ReadyBars}, orders={run.ActualOrders}/{run.ExpectedOrders}, ambiguousBars={run.AmbiguousBars}, ambiguousOrders={run.AmbiguousOrders}, longCloses={run.LongCloses}, shortCloses={run.ShortCloses}, reversals={run.Reversals}");
		IsTrue(run.ReadyBars > 0, "The archive must reach GO values past the EMA warmup.");
		IsTrue(run.Violations.Count == 0, string.Join(Environment.NewLine, run.Violations.Take(12)));
		AreEqual(run.ExpectedOrders + run.AmbiguousOrders, run.ActualOrders, "Every README GO rule must act on its own finished candle, and nothing else may trade.");
	}

	private static decimal? GoEma(decimal? previous, decimal value, int period)
		=> previous is decimal prior ? prior + 2m / (period + 1m) * (value - prior) : value;

	private static (Sides side, decimal volume, bool close)[] GoOrders(decimal position, decimal go, decimal openLevel, decimal closeLevelDiff, decimal volume)
	{
		var orders = new List<(Sides side, decimal volume, bool close)>();
		var closeLevel = openLevel - closeLevelDiff;

		if (position > 0m && go < closeLevel)
		{
			orders.Add((Sides.Sell, position, true));
			position = 0m;
		}
		else if (position < 0m && go > -closeLevel)
		{
			orders.Add((Sides.Buy, -position, true));
			position = 0m;
		}

		if (position == 0m)
		{
			if (go > openLevel)
				orders.Add((Sides.Buy, volume, false));
			else if (go < -openLevel)
				orders.Add((Sides.Sell, volume, false));
		}

		return [.. orders];
	}

	private static (decimal openLevel, decimal closeLevelDiff)? PickGoLevels(IReadOnlyList<decimal> values)
	{
		var magnitudes = values.Select(value => Math.Abs(value)).Where(value => value > 0m).Distinct().Order().ToArray();

		// Midpoints between neighbouring magnitudes keep every level away from all GO values the archive produces.
		var levels = Enumerable.Range(0, Math.Max(0, magnitudes.Length - 1)).Select(i => (magnitudes[i] + magnitudes[i + 1]) / 2m).ToArray();

		foreach (var openIndex in new[] { 5, 4, 6, 3, 7 }.Select(tenth => levels.Length * tenth / 10))
		{
			foreach (var closeIndex in new[] { 5, 3, 7, 1 }.Select(tenth => openIndex * tenth / 10))
			{
				if (closeIndex >= openIndex)
					continue;

				var openLevel = levels[openIndex];
				var closeLevelDiff = openLevel - levels[closeIndex];
				var (closes, reversals) = SimulateGo(values, openLevel, closeLevelDiff);

				if (reversals > 0 && closes > reversals)
					return (openLevel, closeLevelDiff);
			}
		}

		return null;
	}

	private static (int closes, int reversals) SimulateGo(IEnumerable<decimal> values, decimal openLevel, decimal closeLevelDiff)
	{
		var position = 0m;
		var closes = 0;
		var reversals = 0;

		foreach (var go in values)
		{
			var orders = GoOrders(position, go, openLevel, closeLevelDiff, 1m);

			if (orders.Any(order => order.close))
			{
				closes++;

				if (orders.Length > 1)
					reversals++;
			}

			foreach (var order in orders)
				position = order.close ? 0m : order.side == Sides.Buy ? 1m : -1m;
		}

		return (closes, reversals);
	}

	private const string DmiPowerMove = "0013_DMI_Power_Move";
	private const string TradingViewSupertrendFlip = "0014_TradingView_Supertrend_Flip";
	private const string RsiDivergence = "0016_RSI_Divergence";
	private const string KeltnerRlSignal = "0343_Keltner_Reinforcement_Learning_Signal";
	private const string GridBot = "0425_Grid_Bot";
	private const string BullishReversalBar = "0578_Bullish_Reversal_Bar";
	private const string BuyAndHold = "0579_Buy_And_Hold";
	private const string BuyOn5DayLow = "0582_Buy_On_5_Day_Low";
	private const string BuySellBullishEngulfing = "0585_BuySell_Bullish_Engulfing";
	private const string EngulfingCandlestick = "0752_Engulfing_Candlestick";
	private const string IbsInternalBarStrength = "0904_Ibs_Internal_Bar_Strength";
	private const string MultiTimeframeMacdExample = "1101_Multi_Timeframe_MACD";
	private const string SilverMidnightCandleColor = "1300_SILVER_Midnight_Candle_Color_1_Hour_Delay_and_SL_TP";
	private const string TimeExample = "1437_Time";
	private const string MartiniMartingale = "1704_Martini_Martingale";
	private const string Icai = "2308_ICAi";
	private const string OcoPendingOrdersExample = "3008_OCO_Pending_Orders";
	private const string RangeFollowerExample = "3406_Range_Follower";
	private const string SampleDetectEconomicCalendar = "3507_Sample_Detect_Economic_Calendar";

	[TestMethod]
	[TestCategory("Shard04")]
	public async Task S2308_FixedProtectionAndFullReversal()
	{
		var recorder = new OrderTraceRecorder();
		await Replay(Icai,
			(strategy, _) =>
			{
				IsTrue(strategy.Parameters.TryGetValue("StopLoss", out var stop), "README promises fixed stop-loss protection.");
				IsTrue(strategy.Parameters.TryGetValue("TakeProfit", out var take), "README promises fixed take-profit protection.");
				AreEqual(1000m, Convert.ToDecimal(stop.Value));
				AreEqual(2000m, Convert.ToDecimal(take.Value));
				SetParam(strategy, "StopLoss", 0m);
				SetParam(strategy, "TakeProfit", 0m);
				SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
				SetParam(strategy, "Length", 3);
				recorder.Attach(strategy);
			}, TimeSpan.FromDays(3));

		recorder.AssertReverses(1m);
	}

	[TestMethod]
	[TestCategory("Shard04")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task S2308_ProtectiveExitClosesInsteadOfReversing(bool takeProfit)
	{
		var recorder = new OrderTraceRecorder();
		await Replay(Icai,
			(strategy, _) =>
			{
				SetParam(strategy, "CandleType", TimeSpan.FromMinutes(15).TimeFrame());
				SetParam(strategy, "Length", 3);
				SetParam(strategy, "StopLoss", takeProfit ? 0m : 0.01m);
				SetParam(strategy, "TakeProfit", takeProfit ? 0.01m : 0m);
				recorder.Attach(strategy);
			}, TimeSpan.FromDays(3));
		recorder.AssertFirstOppositeVolume(1m);
	}

	[TestMethod]
	[TestCategory("Shard04")]
	public async Task S2308_EverySignalMatchesPublishedSlopeInequalities()
	{
		var ma = new SimpleMovingAverage { Length = 3 };
		var std = new StandardDeviation { Length = 3 };
		decimal? previous = null;
		var previousSlope = 0m;
		var signals = new Dictionary<DateTime, Sides?>();
		var orders = new Dictionary<long, (DateTime Time, Sides Side)>();
		var plateauReversals = 0;
		await Replay(Icai,
			(strategy, _) =>
			{
				SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
				SetParam(strategy, "Length", 3);
				SetParam(strategy, "StopLoss", 0m);
				SetParam(strategy, "TakeProfit", 0m);
				strategy.CandleReceived += (_, candle) =>
				{
					if (candle.State != CandleStates.Finished)
						return;
					var average = ma.Process(candle.ClosePrice, candle.OpenTime, true).GetValue<decimal>();
					var deviation = std.Process(candle.ClosePrice, candle.OpenTime, true).GetValue<decimal>();
					if (!ma.IsFormed || !std.IsFormed)
						return;
					var prior = previous ?? average;
					var distanceSquared = (prior - average) * (prior - average);
					var variance = deviation * deviation;
					var weight = distanceSquared >= variance && distanceSquared != 0m ? 1m - variance / distanceSquared : 0m;
					var current = prior + weight * (average - prior);
					var slope = current - prior;
					// These signs are the README's Prev/PrevPrev/Current inequalities, including equality on Current.
					Sides? signal = previousSlope < 0m && slope >= 0m ? Sides.Buy
						: previousSlope > 0m && slope <= 0m ? Sides.Sell : null;
					if (signal != null && slope == 0m)
						plateauReversals++;
					signals[strategy.CurrentTime] = signal;
					previous = current;
					previousSlope = slope;
				};
				strategy.OrderReceived += (_, order) => orders.TryAdd(order.TransactionId, (strategy.CurrentTime, order.Side));
			}, TimeSpan.FromDays(3));

		IsTrue(plateauReversals > 0, "The fixture must distinguish >=/<= from strict Current inequalities.");
		foreach (var order in orders.Values)
		{
			IsTrue(signals.TryGetValue(order.Time, out var expected), "No candle signal was available at order time.");
			AreEqual((Sides?)order.Side, expected, "The order must follow the published slope reversal, not a departure from a plateau.");
		}
	}

	[TestMethod]
	[TestCategory("Shard07")]
	public async Task S0343_PublishedChannelDefaults()
	{
		await Replay(KeltnerRlSignal,
			(strategy, _) =>
			{
				AreEqual(20, strategy.Parameters["EmaPeriod"].Value);
				AreEqual(14, strategy.Parameters["AtrPeriod"].Value);
				AreEqual(2m, Convert.ToDecimal(strategy.Parameters["AtrMultiplier"].Value));
				AreEqual(2m, Convert.ToDecimal(strategy.Parameters["StopLossAtr"].Value));
				AreEqual(TimeSpan.FromMinutes(15).TimeFrame(), strategy.Parameters["CandleType"].Value);
			}, TimeSpan.FromDays(31));
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S0582_PreviousRangeEntriesAndPreviousHighExits()
	{
		var candles = new List<(DateTime Time, decimal Close, decimal High, decimal Low)>();
		var orders = new Dictionary<long, (DateTime Time, Sides Side, decimal Volume)>();
		const int period = 5;

		await Replay(BuyOn5DayLow,
			(strategy, _) =>
			{
				IsTrue(strategy.Parameters.TryGetValue("LowestPeriod", out var lowest), "The entry must use the previous N-bar low, not an EMA crossover.");
				AreEqual(period, lowest.Value);
				AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
				AreEqual(new DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero), strategy.Parameters["StartTime"].Value);
				AreEqual(new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero), strategy.Parameters["EndTime"].Value);
				strategy.CandleReceived += (_, candle) =>
				{
					if (candle.State == CandleStates.Finished)
						candles.Add((strategy.CurrentTime, candle.ClosePrice, candle.HighPrice, candle.LowPrice));
				};
				strategy.OrderReceived += (_, order) => orders.TryAdd(order.TransactionId, (strategy.CurrentTime, order.Side, order.Volume));
			}, TimeSpan.FromDays(3));

		var position = 0m;
		foreach (var order in orders.Values.OrderBy(order => order.Time))
		{
			var index = candles.FindLastIndex(candle => candle.Time <= order.Time);
			IsTrue(index >= period, "An order arrived before the previous-low window was available.");
			var candle = candles[index];
			if (order.Side == Sides.Buy)
			{
				AreEqual(0m, position, "A long-only strategy must not pyramid or cover a short.");
				IsTrue(candle.Close < candles.Skip(index - period).Take(period).Min(previous => previous.Low), "Entry was not below the previous N-bar low.");
				position += order.Volume;
			}
			else
			{
				AreEqual(position, order.Volume, "A sell must close the existing long, never open a short.");
				IsTrue(candle.Close > candles[index - 1].High, "Exit was not above the previous bar's high.");
				position -= order.Volume;
			}
		}
		IsTrue(orders.Values.Any(order => order.Side == Sides.Sell), "The fixture must exercise an exit as well as an entry.");
	}

	[TestMethod]
	[TestCategory("Shard03")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task S0579_BuyOnceAndHoldUntilEndDate(bool datedWindow)
	{
		var orders = new Dictionary<long, (DateTime Time, Sides Side, decimal Volume)>();
		var start = Paths.HistoryBeginDate.AddHours(1);
		var end = start.AddHours(1);
		await Replay(BuyAndHold,
			(strategy, _) =>
			{
				IsTrue(strategy.Parameters.TryGetValue("StartDate", out var startDate), "README requires a dated entry, not an EMA crossover.");
				IsTrue(strategy.Parameters.TryGetValue("EndDate", out var endDate), "README requires holding until the end date.");
				AreEqual(new DateTimeOffset(2018, 1, 1, 0, 0, 0, TimeSpan.Zero), startDate.Value);
				AreEqual(new DateTimeOffset(2069, 12, 31, 0, 0, 0, TimeSpan.Zero), endDate.Value);
				if (datedWindow)
				{
					startDate.Value = new DateTimeOffset(start, TimeSpan.Zero);
					endDate.Value = new DateTimeOffset(end, TimeSpan.Zero);
				}
				strategy.OrderReceived += (_, order) => orders.TryAdd(order.TransactionId, (strategy.CurrentTime, order.Side, order.Volume));
			}, TimeSpan.FromDays(3));

		var trace = orders.Values.OrderBy(order => order.Time).ToArray();
		AreEqual(datedWindow ? 2 : 1, trace.Length, "Buy and hold must buy once, never re-enter or short.");
		AreEqual(Sides.Buy, trace[0].Side);
		AreEqual(1m, trace[0].Volume);
		if (datedWindow)
		{
			IsTrue(trace[0].Time >= start && trace[0].Time <= start.AddMinutes(1));
			AreEqual(Sides.Sell, trace[1].Side);
			AreEqual(1m, trace[1].Volume);
			IsTrue(trace[1].Time >= end && trace[1].Time <= end.AddMinutes(1));
		}
	}

	[TestMethod]
	[TestCategory("Shard00")]
	[DataRow(false, false)]
	[DataRow(true, true)]
	[DataRow(false, true)]
	[DataRow(true, false)]
	public async Task S0752_SelectedEngulfingAndExactHoldingPeriod(bool bearish, bool shortSide)
	{
		var candles = new List<(DateTime Time, decimal Open, decimal Close)>();
		var orders = new Dictionary<long, (DateTime Time, Sides Side, decimal Volume)>();
		var entrySide = shortSide ? Sides.Sell : Sides.Buy;
		const int holdPeriods = 17;
		await Replay(EngulfingCandlestick,
			(strategy, _) =>
			{
				IsTrue(strategy.Parameters.TryGetValue("HoldPeriods", out var hold), "README requires a bar-count exit, not an EMA crossover.");
				AreEqual(holdPeriods, hold.Value);
				AreEqual("Bullish", Convert.ToString(strategy.Parameters["Pattern"].Value));
				AreEqual(Sides.Buy, strategy.Parameters["Side"].Value);
				AreEqual(TimeSpan.FromMinutes(15).TimeFrame(), strategy.Parameters["CandleType"].Value);
				SetParam(strategy, "Pattern", bearish ? "Bearish" : "Bullish");
				SetParam(strategy, "Side", entrySide);
				strategy.CandleReceived += (_, candle) =>
				{
					if (candle.State == CandleStates.Finished)
						candles.Add((strategy.CurrentTime, candle.OpenPrice, candle.ClosePrice));
				};
				strategy.OrderReceived += (_, order) => orders.TryAdd(order.TransactionId, (strategy.CurrentTime, order.Side, order.Volume));
			}, TimeSpan.FromDays(3));

		var entryIndex = -1;
		var exits = 0;
		foreach (var order in orders.Values.OrderBy(order => order.Time))
		{
			var index = candles.FindLastIndex(candle => candle.Time <= order.Time);
			AreEqual(1m, order.Volume);
			if (entryIndex < 0)
			{
				AreEqual(entrySide, order.Side);
				IsTrue(index > 0);
				var previous = candles[index - 1];
				var current = candles[index];
				IsTrue(bearish
					? previous.Close > previous.Open && current.Close < current.Open && current.Open >= previous.Close && current.Close <= previous.Open
					: previous.Close < previous.Open && current.Close > current.Open && current.Open <= previous.Close && current.Close >= previous.Open,
					"Entry did not follow the selected engulfing pattern.");
				entryIndex = index;
			}
			else
			{
				AreNotEqual(entrySide, order.Side);
				AreEqual(holdPeriods, index - entryIndex, "Exit must occur after exactly the configured number of bars.");
				entryIndex = -1;
				exits++;
			}
		}
		IsTrue(exits > 0, "The fixture must exercise a complete entry/exit cycle.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task S0013_PublishedDefaultsAndAtrStop()
	{
		async Task<OrderTraceRecorder> Run(decimal atrMultiplier)
		{
			var recorder = new OrderTraceRecorder();
			await Replay(DmiPowerMove,
				(strategy, _) =>
				{
					IsTrue(strategy.Parameters.TryGetValue("AdxExitThreshold", out var exit), "README promises an exit when ADX fades.");
					IsTrue(strategy.Parameters.TryGetValue("AtrMultiplier", out var stop), "README promises an ATR stop.");
					AreEqual(14, strategy.Parameters["DmiPeriod"].Value);
					AreEqual(5m, Convert.ToDecimal(strategy.Parameters["DiDifferenceThreshold"].Value));
					AreEqual(30m, Convert.ToDecimal(strategy.Parameters["AdxThreshold"].Value));
					AreEqual(25m, Convert.ToDecimal(exit.Value));
					AreEqual(2m, Convert.ToDecimal(stop.Value));
					AreEqual(TimeSpan.FromMinutes(15).TimeFrame(), strategy.Parameters["CandleType"].Value);
					SetParam(strategy, "AtrMultiplier", atrMultiplier);
					recorder.Attach(strategy);
				}, TimeSpan.FromDays(7));
			return recorder;
		}
		var baseline = await Run(2m);
		var tight = await Run(0.00001m);
		tight.AssertDiffersFrom(baseline, "The declared ATR protection must affect exits.");
		tight.AssertFirstOppositeWithin(TimeSpan.FromMinutes(30));
	}

	[TestMethod]
	[TestCategory("Shard01")]
	public async Task S0425_OrdersRequireAnActualGridLineTouch()
	{
		var candles = new List<(DateTime Time, decimal Close, decimal High, decimal Low)>();
		var orders = new Dictionary<long, (DateTime Time, Sides Side, decimal Volume)>();
		const decimal lower = 60000m;
		const decimal upper = 74000m;
		const int count = 10;
		await Replay(GridBot,
			(strategy, _) =>
			{
				AreEqual(lower, Convert.ToDecimal(strategy.Parameters["LowerLimit"].Value));
				AreEqual(upper, Convert.ToDecimal(strategy.Parameters["UpperLimit"].Value));
				strategy.CandleReceived += (_, candle) =>
				{
					if (candle.State == CandleStates.Finished)
						candles.Add((strategy.CurrentTime, candle.ClosePrice, candle.HighPrice, candle.LowPrice));
				};
				strategy.OrderReceived += (_, order) => orders.TryAdd(order.TransactionId, (strategy.CurrentTime, order.Side, order.Volume));
			}, TimeSpan.FromDays(31));

		var position = 0m;
		foreach (var order in orders.Values.OrderBy(order => order.Time))
		{
			var candle = candles.Last(candle => candle.Time <= order.Time);
			var touches = Enumerable.Range(0, count + 1)
				.Select(index => (Index: index, Price: lower + index * (upper - lower) / count))
				.Where(level => level.Price >= candle.Low && level.Price <= candle.High)
				.OrderBy(level => Math.Abs(level.Price - candle.Close)).ThenBy(level => level.Index).ToArray();
			IsTrue(touches.Length > 0, "Rounding a price to a line is not a grid touch.");
			AreEqual(touches[0].Index < count / 2m ? Sides.Buy : Sides.Sell, order.Side);
			AreNotEqual(count / 2m, (decimal)touches[0].Index, "The middle line must be neutral.");
			AreEqual(1m + Math.Abs(position), order.Volume, "An opposite signal must close the old exposure before opening the new one.");
			position += order.Side == Sides.Buy ? order.Volume : -order.Volume;
		}
		IsTrue(orders.Values.Any(order => order.Side == Sides.Buy) && orders.Values.Any(order => order.Side == Sides.Sell), "The fixture must exercise both halves of the grid.");
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S0016_EntriesRequireDivergentConfirmedPricePivots()
	{
		var rsi = new RelativeStrengthIndex { Length = 14 };
		var bars = new List<(decimal High, decimal Low, decimal Rsi)>();
		(decimal Price, decimal Rsi)? previousLow = null;
		(decimal Price, decimal Rsi)? previousHigh = null;
		var signals = new Dictionary<DateTime, (bool Buy, bool Sell)>();
		var orders = new Dictionary<long, (DateTime Time, Sides Side, decimal Volume)>();
		await Replay(RsiDivergence,
			(strategy, _) =>
			{
				IsTrue(strategy.Parameters.TryGetValue("StopLossPercent", out var stop), "README promises a percent stop.");
				AreEqual(2m, Convert.ToDecimal(stop.Value));
				SetParam(strategy, "StopLossPercent", 0m);
				strategy.CandleReceived += (_, candle) =>
				{
					if (candle.State != CandleStates.Finished)
						return;
					var result = rsi.Process(candle.ClosePrice, candle.OpenTime, true);
					if (!rsi.IsFormed || result.IsEmpty)
						return;
					var value = result.GetValue<decimal>();
					bars.Add((candle.HighPrice, candle.LowPrice, value));
					if (bars.Count < 3)
						return;
					var left = bars[^3];
					var pivot = bars[^2];
					var right = bars[^1];
					var buy = false;
					var sell = false;
					if (pivot.Low < left.Low && pivot.Low < right.Low)
					{
						buy = previousLow is { } low && pivot.Low < low.Price && pivot.Rsi > low.Rsi;
						previousLow = (pivot.Low, pivot.Rsi);
					}
					if (pivot.High > left.High && pivot.High > right.High)
					{
						sell = previousHigh is { } high && pivot.High > high.Price && pivot.Rsi < high.Rsi;
						previousHigh = (pivot.High, pivot.Rsi);
					}
					signals[strategy.CurrentTime] = (buy, sell);
				};
				strategy.OrderReceived += (_, order) => orders.TryAdd(order.TransactionId, (strategy.CurrentTime, order.Side, order.Volume));
			}, TimeSpan.FromDays(7));

		var position = 0m;
		foreach (var order in orders.Values.OrderBy(order => order.Time))
		{
			IsTrue(signals.TryGetValue(order.Time, out var signal), "No confirmed pivot was available at entry time.");
			IsTrue(order.Side == Sides.Buy ? signal.Buy : signal.Sell, "A 30/70 RSI crossing is not a price/RSI divergence.");
			AreEqual(1m + Math.Abs(position), order.Volume);
			position += order.Side == Sides.Buy ? order.Volume : -order.Volume;
		}
		IsTrue(orders.Values.Any(order => order.Side == Sides.Buy) && orders.Values.Any(order => order.Side == Sides.Sell));
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S0016_PercentStopClosesInsteadOfReversing()
	{
		var orders = new Dictionary<long, (DateTime Time, Sides Side, decimal Volume)>();
		await Replay(RsiDivergence,
			(strategy, _) =>
			{
				SetParam(strategy, "StopLossPercent", 0.00001m);
				strategy.OrderReceived += (_, order) => orders.TryAdd(order.TransactionId, (strategy.CurrentTime, order.Side, order.Volume));
			}, TimeSpan.FromDays(7));

		var position = 0m;
		var protectedExits = 0;
		foreach (var (_, order) in orders.OrderBy(pair => pair.Value.Time).ThenBy(pair => pair.Key))
		{
			if ((position > 0m && order.Side == Sides.Sell || position < 0m && order.Side == Sides.Buy) && order.Volume == Math.Abs(position))
				protectedExits++;
			position += order.Side == Sides.Buy ? order.Volume : -order.Volume;
			IsTrue(Math.Abs(position) <= 1m, "Protection must never add or reverse exposure.");
		}
		IsTrue(protectedExits > 0, "The tiny stop must close an entry, not only produce the two-unit divergence reversals.");
	}

	[TestMethod]
	[TestCategory("Shard01")]
	public async Task S0425_DoesNotClampAnUntouchedOutOfRangePrice()
	{
		var switchTime = Paths.HistoryBeginDate.AddHours(1);
		var orders = new Dictionary<long, DateTime>();
		await Replay(GridBot,
			(strategy, _) =>
			{
				AreEqual(74000m, Convert.ToDecimal(strategy.Parameters["UpperLimit"].Value));
				AreEqual(60000m, Convert.ToDecimal(strategy.Parameters["LowerLimit"].Value));
				AreEqual(10, strategy.Parameters["GridCount"].Value);
				// Packaged BTC (about 59k-74k) never reaches a 45000-48000 grid; after the first hour the default range takes over.
				SetParam(strategy, "UpperLimit", 48000m);
				SetParam(strategy, "LowerLimit", 45000m);
				strategy.CandleReceived += (_, candle) =>
				{
					if (candle.State == CandleStates.Finished && strategy.CurrentTime >= switchTime)
					{
						SetParam(strategy, "UpperLimit", 74000m);
						SetParam(strategy, "LowerLimit", 60000m);
					}
				};
				strategy.OrderReceived += (_, order) => orders.TryAdd(order.TransactionId, strategy.CurrentTime);
				// Restore the configured settings before the harness compares against its pre-run clone.
				strategy.ProcessStateChanged += changed =>
				{
					if (ReferenceEquals(changed, strategy) && changed.ProcessState == ProcessStates.Stopped)
					{
						SetParam(strategy, "UpperLimit", 48000m);
						SetParam(strategy, "LowerLimit", 45000m);
					}
				};
			}, TimeSpan.FromDays(7));
		IsTrue(orders.Values.All(time => time >= switchTime), "An out-of-range candle did not touch the grid and must not submit an order.");
	}

	[TestMethod]
	[TestCategory("Shard00")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task S0904_PreviousBarIbsEmaSpacingAndBasketExit(bool emaFilter)
	{
		var ema = new ExponentialMovingAverage { Length = 220 };
		var candles = new List<(DateTime Time, decimal Close, decimal Ibs, decimal Ema)>();
		var orders = new Dictionary<long, (DateTime Time, Sides Side, decimal Volume)>();
		const decimal spacing = 0.25m;
		const int duration = 14;
		await Replay(IbsInternalBarStrength,
			(strategy, _) =>
			{
				IsTrue(strategy.Parameters.TryGetValue("IbsEntryThreshold", out var entry), "README requires IBS, not an EMA crossover.");
				AreEqual(0.09m, Convert.ToDecimal(entry.Value));
				AreEqual(0.985m, Convert.ToDecimal(strategy.Parameters["IbsExitThreshold"].Value));
				AreEqual(220, strategy.Parameters["EmaPeriod"].Value);
				AreEqual(0m, Convert.ToDecimal(strategy.Parameters["MinEntryPct"].Value));
				AreEqual(duration, strategy.Parameters["MaxTradeDuration"].Value);
				SetParam(strategy, "UseEmaFilter", emaFilter);
				SetParam(strategy, "MinEntryPct", spacing);
				strategy.Volume = 0.1m;
				strategy.CandleReceived += (_, candle) =>
				{
					if (candle.State != CandleStates.Finished)
						return;
					var result = ema.Process(candle.ClosePrice, candle.OpenTime, true);
					var range = candle.HighPrice - candle.LowPrice;
					candles.Add((strategy.CurrentTime, candle.ClosePrice, range == 0m ? 0.5m : (candle.ClosePrice - candle.LowPrice) / range,
						result.IsEmpty ? 0m : result.GetValue<decimal>()));
				};
				strategy.OrderReceived += (_, order) => orders.TryAdd(order.TransactionId, (strategy.CurrentTime, order.Side, order.Volume));
			}, TimeSpan.FromDays(3));

		var position = 0m;
		var firstEntry = -1;
		var lastEntryPrice = 0m;
		var exits = 0;
		foreach (var (_, order) in orders.OrderBy(pair => pair.Value.Time).ThenBy(pair => pair.Key))
		{
			var index = candles.FindLastIndex(candle => candle.Time <= order.Time);
			IsTrue(index > 0);
			var previous = candles[index - 1];
			var closes = position > 0m && order.Side == Sides.Sell || position < 0m && order.Side == Sides.Buy;
			if (closes)
			{
				AreEqual(Math.Abs(position), order.Volume, "The exit must close the entire IBS basket, not reverse one unit.");
				IsTrue(index - firstEntry >= duration || (position > 0m ? previous.Ibs > 0.985m : previous.Ibs < 0.09m), "Neither the previous-bar IBS exit nor the holding limit was reached.");
				position = 0m;
				firstEntry = -1;
				lastEntryPrice = 0m;
				exits++;
				continue;
			}

			AreEqual(0.1m, order.Volume);
			IsTrue(order.Side == Sides.Buy ? previous.Ibs < 0.09m : previous.Ibs > 0.985m, "Entry must use the previous bar's IBS, not the current candle or a moving-average crossover.");
			if (emaFilter)
				IsTrue(order.Side == Sides.Buy ? previous.Close > previous.Ema : previous.Close < previous.Ema, "Entry must agree with the optional EMA trend filter.");
			if (position != 0m)
				IsTrue(100m * Math.Abs(candles[index].Close - lastEntryPrice) / lastEntryPrice >= spacing, "An added entry violated the minimum price spacing.");
			else
				firstEntry = index;
			lastEntryPrice = candles[index].Close;
			position += order.Side == Sides.Buy ? order.Volume : -order.Volume;
		}
		IsTrue(exits > 0, "The fixture must exercise basket exits.");
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S0904_LongOnlyAndExactHoldingLimit()
	{
		var candles = new List<DateTime>();
		var orders = new Dictionary<long, (DateTime Time, Sides Side, decimal Volume)>();
		await Replay(IbsInternalBarStrength,
			(strategy, _) =>
			{
				SetParam(strategy, "UseEmaFilter", false);
				SetParam(strategy, "AllowShort", false);
				SetParam(strategy, "MaxTradeDuration", 2);
				SetParam(strategy, "IbsEntryThreshold", 0.5m);
				SetParam(strategy, "IbsExitThreshold", 1m);
				SetParam(strategy, "MinEntryPct", 100m);
				strategy.CandleReceived += (_, candle) =>
				{
					if (candle.State == CandleStates.Finished)
						candles.Add(strategy.CurrentTime);
				};
				strategy.OrderReceived += (_, order) => orders.TryAdd(order.TransactionId, (strategy.CurrentTime, order.Side, order.Volume));
			}, TimeSpan.FromDays(2));

		var entry = -1;
		var exits = 0;
		foreach (var (_, order) in orders.OrderBy(pair => pair.Value.Time).ThenBy(pair => pair.Key))
		{
			var index = candles.FindLastIndex(time => time <= order.Time);
			AreEqual(1m, order.Volume);
			if (entry < 0)
			{
				AreEqual(Sides.Buy, order.Side, "AllowShort=false must not open short exposure.");
				entry = index;
			}
			else
			{
				AreEqual(Sides.Sell, order.Side);
				AreEqual(2, index - entry, "The time exit must close exactly at the holding limit, without restarting its clock on added entries.");
				entry = -1;
				exits++;
			}
		}
		IsTrue(exits > 0);
	}

	[TestMethod]
	[TestCategory("Shard05")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task S1101_PublishedFramesAndIndependentMacdAgreement(bool zeroLine)
	{
		var working = TimeSpan.FromMinutes(5);
		var higher = TimeSpan.FromHours(1);
		var frames = new Dictionary<TimeSpan, (int Count, decimal Fast, decimal Slow, decimal Signal)>();
		var orders = new List<(Sides Side, decimal Volume, int Agreement)>();
		int Direction(TimeSpan frame)
		{
			if (!frames.TryGetValue(frame, out var value) || value.Count < 35)
				return 0;
			return Math.Sign(value.Fast - value.Slow - (zeroLine ? 0m : value.Signal));
		}
		await Replay(MultiTimeframeMacdExample,
			(strategy, _) =>
			{
				AreEqual(12, strategy.Parameters["FastLength"].Value);
				AreEqual(26, strategy.Parameters["SlowLength"].Value);
				AreEqual(9, strategy.Parameters["SignalLength"].Value);
				AreEqual(working.TimeFrame(), strategy.Parameters["CandleType"].Value);
				AreEqual(higher.TimeFrame(), strategy.Parameters["HigherCandleType"].Value);
				if (zeroLine)
				{
					var entry = strategy.Parameters["Entry"];
					entry.Value = IsPython ? "ZeroLine" : Enum.Parse(entry.Value.GetType(), "ZeroLine");
				}
				strategy.CandleReceived += (subscription, candle) =>
				{
					if (candle.State != CandleStates.Finished || subscription.DataType.Arg is not TimeSpan frame)
						return;
					var previous = frames.GetValueOrDefault(frame);
					var close = candle.ClosePrice;
					var fast = previous.Count == 0 ? close : previous.Fast + 2m / 13m * (close - previous.Fast);
					var slow = previous.Count == 0 ? close : previous.Slow + 2m / 27m * (close - previous.Slow);
					var macd = fast - slow;
					var signal = previous.Count == 0 ? macd : previous.Signal + 2m / 10m * (macd - previous.Signal);
					frames[frame] = (previous.Count + 1, fast, slow, signal);
				};
				strategy.OrderRegistering += order =>
				{
					var current = Direction(working);
					var slow = Direction(higher);
					orders.Add((order.Side, order.Volume, current != 0 && current == slow ? current : 0));
				};
			}, TimeSpan.FromDays(7));

		AreEqual(2, frames.Count, "A multi-timeframe strategy must consume two independent candle streams.");
		var position = 0m;
		foreach (var order in orders)
		{
			AreEqual(order.Side == Sides.Buy ? 1 : -1, order.Agreement, "Both MACD frames must agree at submission time.");
			AreEqual(1m + Math.Abs(position), order.Volume);
			position += order.Side == Sides.Buy ? order.Volume : -order.Volume;
		}
		IsTrue(orders.Any(order => order.Side == Sides.Buy) && orders.Any(order => order.Side == Sides.Sell));
	}

	[TestMethod]
	[TestCategory("Shard05")]
	[DataRow(0)]
	[DataRow(10)]
	public async Task S1437_EntryAfterHighHoldsAboveOpenAndExitWhenItFails(int ticksFromOpen)
	{
		DateTime? firstQuote = null;
		var quoteCount = 0;
		var candleCount = 0;
		var orders = new List<(DateTime Time, Sides Side)>();
		long? barMinute = null;
		var barOpen = 0m;
		var barHigh = 0m;
		var condition = false;
		DateTime? conditionSince = null;
		var quoteTime = default(DateTime);
		var boundaryHits = 0;
		var position = 0m;
		var violations = new List<string>();
		await Replay(TimeExample,
			(strategy, _) =>
			{
				AreEqual(0, strategy.Parameters["TicksFromOpen"].Value);
				AreEqual(20, strategy.Parameters["SecondsCondition"].Value);
				AreEqual(true, strategy.Parameters["ResetOnNewBar"].Value);
				AreEqual(TimeSpan.FromMinutes(1).TimeFrame(), strategy.Parameters["CandleType"].Value);
				SetParam(strategy, "TicksFromOpen", ticksFromOpen);
				var threshold = ticksFromOpen * strategy.Security.PriceStep.Value;
				// Subscribed before the strategy binds its own Level1 handler, so this model is current when an order registers.
				strategy.Level1Received += (_, quote) =>
				{
					// The strategy's Level1 binding delivers only the messages carrying its build field, the best ask.
					if (quote.TryGetDecimal(Level1Fields.BestAskPrice) is not decimal ask)
						return;
					firstQuote ??= quote.ServerTime;
					quoteCount++;
					var price = quote.TryGetDecimal(Level1Fields.LastTradePrice) ?? ask;
					if (price <= 0m)
						return;
					var minute = quote.ServerTime.Ticks / TimeSpan.TicksPerMinute;
					if (barMinute != minute)
					{
						barMinute = minute;
						barOpen = barHigh = price;
					}
					else
						barHigh = Math.Max(barHigh, price);
					if (barHigh - barOpen == threshold)
						boundaryHits++;
					condition = barHigh - barOpen > threshold;
					conditionSince = condition ? conditionSince ?? quote.ServerTime : null;
					quoteTime = quote.ServerTime;
				};
				strategy.CandleReceived += (_, _) => candleCount++;
				strategy.OrderRegistering += order =>
				{
					orders.Add((strategy.CurrentTime, order.Side));
					if (order.Side == Sides.Buy)
					{
						if (position != 0m || !condition || conditionSince is not DateTime since || quoteTime - since < TimeSpan.FromSeconds(20))
							violations.Add($"Buy at {quoteTime:O}: position={position}, high-open={barHigh - barOpen}, threshold={threshold}, held since {conditionSince:O}.");
						position += order.Volume;
					}
					else
					{
						if (position <= 0m || order.Volume != position || condition)
							violations.Add($"Sell {order.Volume} at {quoteTime:O}: position={position}, high-open={barHigh - barOpen}, threshold={threshold}.");
						position -= order.Volume;
					}
				};
			}, ticksFromOpen == 0 ? TimeSpan.FromHours(3) : TimeSpan.FromDays(3));

		IsTrue(quoteCount > 0, "Bid/ask-only Level1 messages must reach the strategy.");
		AreEqual(0, candleCount, "The intrabar timer must consume real quote updates, not a candle proxy.");
		IsTrue(orders.Count > 0);
		IsTrue(orders[0].Time >= firstQuote.Value.AddSeconds(20), "No entry before the published twenty-second interval.");
		IsTrue(boundaryHits > 0, "The replay must reach a bar high exactly at the threshold, where the high does not yet exceed it.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		IsTrue(orders.Any(order => order.Side == Sides.Buy) && orders.Any(order => order.Side == Sides.Sell) && orders.Count > 2,
			"The high-above-open condition fails on each new bar, so entries and exits must repeat.");
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S3406_DailyAtrIsWilderNotRollingMean()
	{
		decimal? previousClose = null;
		var expectedAtr = 0m;
		var days = 0;
		var sampledDay = 0;
		var samples = new List<(decimal Expected, decimal? Actual)>();
		await Replay(RangeFollowerExample,
			(strategy, _) =>
			{
				strategy.Volume = 0.1m;
				strategy.CandleReceived += (subscription, candle) =>
				{
					if (subscription.DataType.Arg is not TimeSpan frame || frame != TimeSpan.FromDays(1) || candle.State != CandleStates.Finished)
						return;
					var trueRange = previousClose is decimal previous
						? Math.Max(candle.HighPrice - candle.LowPrice, Math.Max(Math.Abs(candle.HighPrice - previous), Math.Abs(candle.LowPrice - previous)))
						: candle.HighPrice - candle.LowPrice;
					days++;
					var count = Math.Min(days, 20);
					expectedAtr = (expectedAtr * (count - 1) + trueRange) / count;
					previousClose = candle.ClosePrice;
				};
				strategy.Level1Received += (_, _) =>
				{
					if (days < 20 || sampledDay == days)
						return;
					var indicator = strategy.Indicators.OfType<AverageTrueRange>().SingleOrDefault();
					samples.Add((expectedAtr, indicator?.GetCurrentValue()));
					sampledDay = days;
				};
			}, TimeSpan.FromDays(31));

		IsTrue(samples.Count >= 10, "The archive must cover the twenty-day warm-up and later Wilder updates.");
		foreach (var sample in samples)
		{
			IsTrue(sample.Actual.HasValue, "README requires the native daily AverageTrueRange binding, not a manual rolling mean.");
			IsTrue(Math.Abs(sample.Expected - sample.Actual.Value) < 0.00000001m, "Daily ATR must use Wilder smoothing after twenty bars.");
		}
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S3406_BreakoutAndProtectionUseLiveQuotes()
	{
		decimal? previousDailyClose = null;
		var dailyAtr = 0m;
		var dailyCount = 0;
		var session = DateTime.MinValue;
		var high = 0m;
		var low = 0m;
		var sessionAtr = 0m;
		var skipSession = false;
		var lastInput = string.Empty;
		var resetSession = false;
		var bid = 0m;
		var ask = 0m;
		var candleHigh = 0m;
		var candleLow = 0m;
		var stop = 0m;
		var take = 0m;
		var quoteExits = 0;
		var entrySide = Sides.Buy;
		var entryVolume = 0m;
		var entryValue = 0m;
		var entryTrigger = 0m;
		var entryResidual = 0m;
		Order exitOrder = null;
		var entries = new Dictionary<DateTime, int>();
		var violations = new List<string>();
		await Replay(RangeFollowerExample,
			(strategy, _) =>
			{
				strategy.Volume = 0.1m;
				strategy.CandleReceived += (subscription, candle) =>
				{
					if (candle.State != CandleStates.Finished || subscription.DataType.Arg is not TimeSpan frame)
						return;
					if (frame == TimeSpan.FromDays(1))
					{
						var trueRange = previousDailyClose is decimal previous
							? Math.Max(candle.HighPrice - candle.LowPrice, Math.Max(Math.Abs(candle.HighPrice - previous), Math.Abs(candle.LowPrice - previous)))
							: candle.HighPrice - candle.LowPrice;
						dailyCount++;
						var count = Math.Min(dailyCount, 20);
						dailyAtr = (dailyAtr * (count - 1) + trueRange) / count;
						previousDailyClose = candle.ClosePrice;
						return;
					}
					lastInput = "candle";
					candleHigh = candle.HighPrice;
					candleLow = candle.LowPrice;
					resetSession = session != candle.OpenTime.Date;
					if (resetSession)
					{
						session = candle.OpenTime.Date;
						high = candle.HighPrice;
						low = candle.LowPrice;
						sessionAtr = dailyCount >= 20 ? dailyAtr : 0m;
						skipSession = sessionAtr > 0m && high - low > sessionAtr * 0.6m;
					}
					else
					{
						high = Math.Max(high, candle.HighPrice);
						low = Math.Min(low, candle.LowPrice);
					}
				};
				strategy.Level1Received += (_, quote) =>
				{
					bid = quote.TryGetDecimal(Level1Fields.BestBidPrice) ?? bid;
					ask = quote.TryGetDecimal(Level1Fields.BestAskPrice) ?? ask;
					lastInput = "quote";
					resetSession = false;
					if (session == quote.ServerTime.Date)
					{
						high = Math.Max(high, Math.Max(bid, ask));
						low = Math.Min(low, Math.Min(bid, ask));
					}
				};
				strategy.Trades.TradeAdded += trade =>
				{
					if (trade.Order.Side == entrySide)
					{
						entryVolume += trade.Trade.Volume;
						entryValue += trade.Trade.Price * trade.Trade.Volume;
					}
					else if (entryVolume > 0m)
					{
						var closed = Math.Min(entryVolume, trade.Trade.Volume);
						entryValue -= entryValue / entryVolume * closed;
						entryVolume -= closed;
					}
					if (entryVolume <= 0m) return;
					var actualEntry = entryValue / entryVolume;
					stop = entrySide == Sides.Buy ? actualEntry - entryTrigger : actualEntry + entryTrigger;
					take = entrySide == Sides.Buy ? actualEntry + entryResidual : actualEntry - entryResidual;
				};
				strategy.OrderRegistering += order =>
				{
					if (strategy.Position == 0m)
					{
						var trigger = sessionAtr * 0.6m;
						var longDistance = bid - low;
						var shortDistance = high - ask;
						if (trigger <= 0m || skipSession || Math.Max(longDistance, shortDistance) <= trigger)
							violations.Add($"Entry without a warmed-up, eligible quote breakout at {strategy.CurrentTime:O}.");
						if (strategy.CurrentTime.Date != session)
							violations.Add($"Entry at {strategy.CurrentTime:O} before the first working candle of that date initialised its session.");
						if (order.Side != (longDistance >= shortDistance ? Sides.Buy : Sides.Sell))
							violations.Add("Entry must choose the larger session excursion.");
						entries[session] = entries.GetValueOrDefault(session) + 1;
						entrySide = order.Side;
						entryVolume = entryValue = 0m;
						entryTrigger = trigger;
						entryResidual = sessionAtr - trigger;
					}
					else
					{
						var isLong = strategy.Position > 0m;
						var quoteCrossed = isLong ? bid <= stop || bid >= take : ask >= stop || ask <= take;
						var candleCrossed = isLong ? candleLow <= stop || candleHigh >= take : candleHigh >= stop || candleLow <= take;
						if (!(resetSession || lastInput == "quote" && quoteCrossed || lastInput == "candle" && candleCrossed))
							violations.Add($"Exit before the published ATR stop/target or session reset at {strategy.CurrentTime:O}.");
						if (lastInput == "quote" && quoteCrossed)
							quoteExits++;
						if (order.Volume != Math.Abs(strategy.Position) || order.Side != (isLong ? Sides.Sell : Sides.Buy))
							violations.Add("A protective exit must flatten the whole position, not reverse it.");
						if (exitOrder is not null && exitOrder.State is not (OrderStates.Done or OrderStates.Failed))
							violations.Add("Native protection, manual checks and session reset must not overlap pending exits.");
						exitOrder = order;
					}
				};
			}, TimeSpan.FromDays(31));

		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		IsTrue(entries.Count > 0 && entries.Values.All(count => count == 1), "At most one entry is allowed per session.");
		IsTrue(quoteExits > 0, "The fixture must exercise a real Level1 protective exit between candle callbacks.");
	}

	[TestMethod]
	[TestCategory("Shard00")]
	[DataRow("BuyLimitPrice", true)]
	[DataRow("BuyStopPrice", true)]
	[DataRow("SellLimitPrice", true)]
	[DataRow("SellStopPrice", true)]
	[DataRow("BuyLimitPrice", false)]
	[DataRow("BuyStopPrice", false)]
	[DataRow("SellLimitPrice", false)]
	[DataRow("SellStopPrice", false)]
	public async Task S3008_FourQuoteTriggersRequireArmingAndRespectOco(string trigger, bool linked)
	{
		var activationTime = Paths.HistoryBeginDate.AddMinutes(1);
		var buy = trigger.StartsWith("Buy", StringComparison.Ordinal);
		var orders = new List<(DateTime Time, Sides Side, decimal Volume)>();
		var candles = 0;
		Strategy captured = null;
		bool? armedAtStop = null;
		await Replay(OcoPendingOrdersExample,
			(strategy, _) =>
			{
				captured = strategy;
				AreEqual(false, strategy.Parameters["Armed"].Value, "The documented discretionary default is unarmed.");
				SetParam(strategy, "OrderVolume", 2m);
				SetParam(strategy, "UseOcoLink", linked);
				var level = trigger is "BuyLimitPrice" or "SellStopPrice" ? 1000000m : 1m;
				SetParam(strategy, trigger, level);
				strategy.Level1Received += (_, quote) =>
				{
					if (quote.ServerTime >= activationTime && orders.Count == 0)
						SetParam(strategy, "Armed", true);
				};
				strategy.CandleReceived += (_, _) => candles++;
				strategy.OrderRegistering += order =>
				{
					orders.Add((strategy.CurrentTime, order.Side, order.Volume));
					// Install another eligible trigger while the first one is executing. Only linked OCO may clear it.
					if (orders.Count == 1)
						SetParam(strategy, buy ? "SellLimitPrice" : "BuyStopPrice", 1m);
				};
				strategy.ProcessStateChanged += _ =>
				{
					if (strategy.ProcessState == ProcessStates.Stopped)
						armedAtStop = (bool)strategy.Parameters["Armed"].Value;
				};
			}, TimeSpan.FromHours(1));

		AreEqual(linked ? 1 : 2, orders.Count, "Linked OCO consumes all triggers; unlinked mode retains the other side.");
		IsTrue(orders.All(order => order.Time >= activationTime && order.Volume == 2m), "An eligible price level must not trade while unarmed.");
		AreEqual(buy ? Sides.Buy : Sides.Sell, orders[0].Side);
		if (!linked)
			AreEqual(buy ? Sides.Sell : Sides.Buy, orders[1].Side);
		AreEqual((bool?)false, armedAtStop, "All consumed triggers must disarm during the run.");
		AreEqual(false, captured.Parameters["Armed"].Value, "Reset must preserve the original unarmed configuration.");
		AreEqual(0, candles, "Manual OCO must not use a candle proxy.");
	}

	[TestMethod]
	[TestCategory("Shard01")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task S0585_BullishEngulfingAndEquitySizing(bool trendFilter)
	{
		var closes = new Queue<decimal>();
		decimal? previousOpen = null;
		decimal? previousClose = null;
		decimal? previousAverage = null;
		var entryEligible = false;
		var price = 0m;
		var buys = 0;
		var violations = new List<string>();
		await Replay(BuySellBullishEngulfing,
			(strategy, _) =>
			{
				IsTrue(strategy.Parameters.TryGetValue("OrderPercent", out var percent), "README requires sizing as a percentage of current equity.");
				AreEqual(30m, Convert.ToDecimal(percent.Value));
				AreEqual(2m, Convert.ToDecimal(strategy.Parameters["TakeProfitPercent"].Value));
				AreEqual(2m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
				AreEqual(TimeSpan.FromMinutes(15).TimeFrame(), strategy.Parameters["CandleType"].Value);
				var trend = strategy.Parameters["TrendMode"];
				AreEqual("SMA50", trend.Value.ToString());
				if (!trendFilter)
					trend.Value = IsPython ? "None" : Enum.Parse(trend.Value.GetType(), "None");
				SetParam(strategy, "TakeProfitPercent", 0.05m);
				SetParam(strategy, "StopLossPercent", 0.05m);
				strategy.CandleReceived += (_, candle) =>
				{
					if (candle.State != CandleStates.Finished)
						return;
					price = candle.ClosePrice;
					entryEligible = previousOpen is decimal open && previousClose is decimal close && open > close
						&& candle.ClosePrice > candle.OpenPrice && candle.OpenPrice <= close && candle.ClosePrice >= open
						&& (!trendFilter || previousAverage is decimal average && close < average);
					closes.Enqueue(candle.ClosePrice);
					if (closes.Count > 50)
						closes.Dequeue();
					previousAverage = closes.Count == 50 ? closes.Average() : null;
					previousOpen = candle.OpenPrice;
					previousClose = candle.ClosePrice;
				};
				strategy.OrderRegistering += order =>
				{
					if (order.Side == Sides.Buy)
					{
						buys++;
						if (!entryEligible || strategy.Position != 0m)
							violations.Add("Buy without a bullish body engulfing / selected prior-bar SMA trend, or while already invested.");
						var equity = strategy.Portfolio.CurrentValue ?? strategy.Portfolio.BeginValue ?? 0m;
						var raw = equity * 30m / 100m / price;
						var step = strategy.Security.VolumeStep.Value;
						var expected = Math.Floor(Math.Min(raw, strategy.Security.MaxVolume.Value) / step) * step;
						if (order.Volume != expected)
							violations.Add($"Entry volume {order.Volume} differs from the rounded 30% equity allocation {expected}.");
					}
					else if (strategy.Position <= 0m || order.Volume != strategy.Position)
						violations.Add("Sell must close the current long; it must never initiate or reverse into a short.");
				};
			}, TimeSpan.FromDays(3));
		IsTrue(buys > 1, "The fixture must exercise repeated eligible patterns and equity updates.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
	}

	[TestMethod]
	[TestCategory("Shard01")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task S0585_IndependentPercentProtectionClosesLong(bool profit)
	{
		var entries = 0;
		var exits = 0;
		var violations = new List<string>();
		await Replay(BuySellBullishEngulfing,
			(strategy, _) =>
			{
				IsTrue(strategy.Parameters.ContainsKey("OrderPercent"));
				SetParam(strategy, "OrderPercent", 0.01m);
				SetParam(strategy, "StopLossPercent", profit ? 0m : 0.000001m);
				SetParam(strategy, "TakeProfitPercent", profit ? 0.000001m : 0m);
				strategy.OrderRegistering += order =>
				{
					if (order.Side == Sides.Buy)
						entries++;
					else
					{
						exits++;
						if (strategy.Position <= 0m || order.Volume != strategy.Position)
							violations.Add("Percent protection must close exactly the actual long position.");
					}
				};
			}, TimeSpan.FromDays(3));
		IsTrue(entries > 0 && exits > 0, "The selected stop or target must actually fire with the other one disabled.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));
	}

	private sealed class ShiftedSmmaOracle(int length, int shift)
	{
		private readonly List<decimal> _input = new();
		private readonly List<decimal> _averages = new();

		public decimal? Process(decimal median)
		{
			_input.Add(median);
			if (_input.Count < length)
				return null;
			_averages.Add(_input.Count == length ? _input.Average() : (_averages[^1] * (length - 1) + median) / length);
			return _averages.Count > shift ? _averages[_averages.Count - 1 - shift] : null;
		}
	}

	[TestMethod]
	[TestCategory("Shard02")]
	[DataRow(false, false)]
	[DataRow(true, false)]
	[DataRow(false, true)]
	[DataRow(true, true)]
	public async Task S0578_ShiftedAlligatorConfirmationFiltersAndBarLowStop(bool enableAo, bool enableMfi)
	{
		var jaw = new ShiftedSmmaOracle(13, 8);
		var teeth = new ShiftedSmmaOracle(8, 5);
		var lips = new ShiftedSmmaOracle(5, 3);
		var medians = new Queue<decimal>();
		decimal? previousLow = null, previousLips = null, previousAo = null, previousMfi = null;
		decimal previousVolume = 0m, stopLoss = 0m, lastBid = 0m;
		(decimal High, decimal Low, bool BullishBody)? pending = null;
		(decimal Jaw, decimal Teeth, decimal Lips)? currentLines = null;
		var buyEligible = false;
		var candleExit = false;
		var expectedBuys = 0;
		var buys = 0;
		var exits = 0;
		var quoteStopExits = 0;
		var nonBullishBodyEntries = 0;
		var nonRisingLipsConfirmations = 0;
		var nonRisingLipsReversals = 0;
		var violations = new List<string>();
		await Replay(BullishReversalBar,
			(strategy, _) =>
			{
				IsTrue(strategy.Parameters.ContainsKey("EnableAo") && strategy.Parameters.ContainsKey("EnableMfi"), "README declares optional AO/MFI filters, not an EMA cross.");
				AreEqual(false, strategy.Parameters["EnableAo"].Value);
				AreEqual(false, strategy.Parameters["EnableMfi"].Value);
				AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
				SetParam(strategy, "EnableAo", enableAo);
				SetParam(strategy, "EnableMfi", enableMfi);
				strategy.Level1Received += (_, quote) =>
				{
					if (quote.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
						lastBid = Convert.ToDecimal(bid);
				};
				strategy.CandleReceived += (_, candle) =>
				{
					if (candle.State != CandleStates.Finished)
						return;
					var median = (candle.HighPrice + candle.LowPrice) / 2m;
					var j = jaw.Process(median);
					var t = teeth.Process(median);
					var l = lips.Process(median);
					currentLines = j is decimal jv && t is decimal tv && l is decimal lv ? (jv, tv, lv) : null;
					medians.Enqueue(median);
					if (medians.Count > 34)
						medians.Dequeue();
					decimal? ao = medians.Count == 34 ? medians.TakeLast(5).Average() - medians.Average() : null;
					decimal? mfi = candle.TotalVolume > 0m ? (candle.HighPrice - candle.LowPrice) / candle.TotalVolume : null;
					buyEligible = false;
					candleExit = strategy.Position > 0m && (candle.LowPrice <= stopLoss || l < previousLips);
					if (strategy.Position == 0m && currentLines is { } lines)
					{
						if (pending is { } setup && candle.LowPrice <= setup.Low)
							pending = null;
						if (pending is { } confirmation && candle.ClosePrice > confirmation.High)
						{
							buyEligible = true;
							stopLoss = confirmation.Low;
							pending = null;
							expectedBuys++;
							if (!confirmation.BullishBody)
								nonBullishBodyEntries++;
							if (!(l > previousLips))
								nonRisingLipsConfirmations++;
						}
						else if (candle.LowPrice < previousLow && candle.ClosePrice > median
							&& candle.HighPrice < Math.Min(lines.Jaw, Math.Min(lines.Teeth, lines.Lips))
							&& (!enableAo || ao is decimal a && previousAo is decimal pa && a > pa)
							&& (!enableMfi || mfi is decimal m && previousMfi is decimal pm && m < pm && candle.TotalVolume > previousVolume))
						{
							if (l > previousLips)
								pending = (candle.HighPrice, candle.LowPrice, candle.ClosePrice > candle.OpenPrice);
							else
								nonRisingLipsReversals++;
						}
					}
					previousLow = candle.LowPrice;
					previousVolume = candle.TotalVolume;
					previousLips = l;
					previousAo = ao;
					previousMfi = mfi;
				};
				strategy.OrderRegistering += order =>
				{
					if (order.Side == Sides.Buy)
					{
						buys++;
						if (!buyEligible || strategy.Position != 0m)
							violations.Add("Entry must follow a below-Alligator new low closing in its upper half on rising lips (plus the selected AO/MFI filters), confirmed only by a later close above that bar's high.");
						var actual = strategy.Indicators.OfType<Alligator>().SingleOrDefault();
						if (actual == null || currentLines is not { } lines || actual.Jaw.GetCurrentValue<decimal>() != lines.Jaw
							|| actual.Teeth.GetCurrentValue<decimal>() != lines.Teeth || actual.Lips.GetCurrentValue<decimal>() != lines.Lips)
							violations.Add("Native Alligator must match independent median-price SMMA with published 13/8, 8/5, 5/3 lengths/shifts.");
					}
					else
					{
						exits++;
						if (strategy.Position <= 0m || order.Volume != strategy.Position || !(candleExit || lastBid > 0m && lastBid <= stopLoss))
							violations.Add("Sell must close the actual long only on its stored bar-low stop or falling lips.");
						if (!candleExit && lastBid > 0m && lastBid <= stopLoss)
							quoteStopExits++;
					}
				};
			}, TimeSpan.FromDays(31));
		IsTrue(buys > 0 && exits > 0, "Every filter combination must exercise real entry and exit on the packaged archive.");
		AreEqual(expectedBuys, buys, "Every eligible flat-position confirmation must be traded.");
		if (!enableAo && !enableMfi)
		{
			IsTrue(quoteStopExits > 0, "The bar-low stop must also react to real bids between finished candles.");
			IsTrue(nonBullishBodyEntries > 0, "README setup is a new low closing in its upper half; a setup bar closing at or below its open must still be traded.");
			IsTrue(nonRisingLipsConfirmations > 0, "README confirmation is a close above the setup high alone; it must be traded on a bar whose lips did not rise.");
			IsTrue(nonRisingLipsReversals > 0, "The bullish trend turn belongs to the setup bar; new-low reversal bars without rising lips must be rejected.");
		}
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(15)));
	}

	[TestMethod]
	[TestCategory("Shard04")]
	public async Task S1300_PreviousNyMidnightColourAndDstClock()
	{
		var ny = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
		var midnight = new Dictionary<DateTime, Sides>();
		var entries = new Dictionary<DateTime, (DateTime Utc, Sides Side)>();
		var firstQuotes = new Dictionary<DateTime, (DateTime Local, decimal Position)>();
		var exits = 0;
		var violations = new List<string>();
		await Replay(SilverMidnightCandleColor,
			(strategy, _) =>
			{
				IsTrue(strategy.Parameters.ContainsKey("TakeProfitLongTicks") && strategy.Parameters.ContainsKey("TakeProfitShortTicks")
					&& strategy.Parameters.ContainsKey("StopLossTicks"), "README requires directional tick targets and a fixed tick stop, not EMA/cooldown.");
				AreEqual(57, strategy.Parameters["TakeProfitLongTicks"].Value);
				AreEqual(48, strategy.Parameters["TakeProfitShortTicks"].Value);
				AreEqual(200, strategy.Parameters["StopLossTicks"].Value);
				AreEqual(TimeSpan.FromHours(1).TimeFrame(), strategy.Parameters["CandleType"].Value);
				strategy.Level1Received += (_, quote) =>
				{
					var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(quote.ServerTime, DateTimeKind.Utc), ny);
					if (local.Hour == 1)
						firstQuotes.TryAdd(local.Date, (local, strategy.Position));
				};
				strategy.CandleReceived += (_, candle) =>
				{
					if (candle.State != CandleStates.Finished)
						return;
					var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(candle.OpenTime, DateTimeKind.Utc), ny);
					if (local.TimeOfDay == TimeSpan.Zero)
						midnight[local.Date] = candle.ClosePrice > candle.OpenPrice ? Sides.Buy : Sides.Sell;
				};
				strategy.OrderRegistering += order =>
				{
					if (strategy.Position != 0m)
					{
						exits++;
						if (order.Side != (strategy.Position > 0m ? Sides.Sell : Sides.Buy) || order.Volume != Math.Abs(strategy.Position))
							violations.Add("Protection must close, never average or reverse, the actual position.");
						return;
					}
					var utc = DateTime.SpecifyKind(strategy.CurrentTime, DateTimeKind.Utc);
					var local = TimeZoneInfo.ConvertTimeFromUtc(utc, ny);
					if (local.Hour != 1 || local.Minute != 0 || !entries.TryAdd(local.Date, (utc, order.Side)))
						violations.Add("Exactly one entry may occur in the first observable minute of 01:00 New York, respecting DST.");
					if (!midnight.TryGetValue(local.Date.AddDays(-1), out var expected) || order.Side != expected)
						violations.Add("Direction must follow the preceding New York calendar day's midnight H1 candle, not today's candle or EMA.");
				};
			}, TimeSpan.FromDays(31));
		var eligibleDays = firstQuotes.Where(pair => pair.Value.Local.Minute == 0 && midnight.ContainsKey(pair.Key.AddDays(-1)))
			.Select(pair => pair.Key).OrderBy(day => day).ToArray();
		AreEqual(29, eligibleDays.Length, "The physical Level1 archive has March 1..30 only; March 1 lacks the preceding midnight candle.");
		IsTrue(eligibleDays.SequenceEqual(entries.Keys.OrderBy(day => day)), "Every available 01:00 quote with a preceding midnight signal must trade exactly once.");
		IsFalse(entries.ContainsKey(new DateTime(2024, 3, 31)), "No quote history exists on March 31; the sample must not fabricate an entry clock or quotes.");
		IsTrue(entries.Values.Any(entry => entry.Side == Sides.Buy) && entries.Values.Any(entry => entry.Side == Sides.Sell));
		var differentToday = entries.Count(pair => midnight.TryGetValue(pair.Key, out var today) && today != pair.Value.Side);
		IsTrue(differentToday > 0 && exits > 0, "The completed history must distinguish the preceding day's candle from today's (not yet completed at entry). SL/TP must also close positions.");
		AreEqual(6, entries[new DateTime(2024, 3, 9)].Utc.Hour, "Before DST, 01:00 New York is 06:00 UTC.");
		AreEqual(5, entries[new DateTime(2024, 3, 11)].Utc.Hour, "After DST, 01:00 New York is 05:00 UTC.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(15)));
	}

	[TestMethod]
	[TestCategory("Shard04")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task S1300_TickTargetOrStopUsesFillPriceAndSecurityStep(bool profit)
	{
		var bidPrice = 0m;
		var askPrice = 0m;
		var entryValue = 0m;
		var entryVolume = 0m;
		Order entryOrder = null;
		var entrySide = Sides.Buy;
		var buys = 0;
		var sells = 0;
		var exits = 0;
		var violations = new List<string>();
		await Replay(SilverMidnightCandleColor,
			(strategy, security) =>
			{
				IsTrue(strategy.Parameters.ContainsKey("StopLossTicks"));
				strategy.Volume = 1m;
				SetParam(strategy, "StopLossTicks", profit ? 0 : 1);
				SetParam(strategy, "TakeProfitLongTicks", profit ? 57 : 0);
				SetParam(strategy, "TakeProfitShortTicks", profit ? 48 : 0);
				strategy.Level1Received += (_, quote) =>
				{
					if (quote.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
						bidPrice = Convert.ToDecimal(bid);
					if (quote.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
						askPrice = Convert.ToDecimal(ask);
				};
				strategy.Trades.TradeAdded += trade =>
				{
					if (entryOrder != null && trade.Order.TransactionId == entryOrder.TransactionId)
					{
						entryValue += trade.Trade.Price * trade.Trade.Volume;
						entryVolume += trade.Trade.Volume;
					}
					else if (entryVolume > 0m)
					{
						var remaining = Math.Max(0m, entryVolume - trade.Trade.Volume);
						entryValue = entryValue / entryVolume * remaining;
						entryVolume = remaining;
					}
				};
				strategy.OrderRegistering += order =>
				{
					if (strategy.Position == 0m)
					{
						entryOrder = order;
						entrySide = order.Side;
						if (entrySide == Sides.Buy) buys++; else sells++;
						entryValue = entryVolume = 0m;
						return;
					}
					exits++;
					var price = entrySide == Sides.Buy ? bidPrice : askPrice;
					var entryPrice = entryVolume > 0m ? entryValue / entryVolume : 0m;
					var offset = security.PriceStep.Value * (profit ? entrySide == Sides.Buy ? 57 : 48 : 1);
					var limit = entryPrice + (profit == (entrySide == Sides.Buy) ? offset : -offset);
					var hit = profit == (entrySide == Sides.Buy) ? price >= limit : price <= limit;
					if (entryPrice <= 0m || !hit || order.Volume != Math.Abs(strategy.Position) || order.Side == entrySide)
						violations.Add($"Exit does not match {(profit ? "target" : "stop")} at {limit} from fill {entryPrice}, step {security.PriceStep}, observed {price}.");
				};
			}, TimeSpan.FromDays(31));
		IsTrue(buys > 0 && sells > 0 && exits > 0, "Both directional targets / stops must be exercised independently.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(15)));
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S1704_RealTickClockAndInitialStopCancellation()
	{
		var pairs = new List<List<Order>>();
		var filled = new HashSet<long>();
		var cancelled = new HashSet<long>();
		var violations = new List<string>();
		var lastClose = 0m;
		var subscriptionSeen = false;
		await Replay(MartiniMartingale,
			(strategy, _) =>
			{
				AreEqual(10m, Convert.ToDecimal(strategy.Parameters["Step"].Value));
				AreEqual(10m, Convert.ToDecimal(strategy.Parameters["ProfitClose"].Value));
				AreEqual(0.1m, Convert.ToDecimal(strategy.Parameters["InitialVolume"].Value));
				strategy.SubscriptionStarted += subscription =>
				{
					if (!subscription.DataType.IsTFCandles)
						return;
					subscriptionSeen = true;
					AreEqual(MarketDataBuildModes.Build, subscription.MarketData.BuildMode,
						"Initial exchange stops need the actual intrabar tick sequence, not an invented OHLC path.");
					AreEqual(DataType.Ticks, subscription.MarketData.BuildFrom);
				};
				strategy.CandleReceived += (_, candle) =>
				{
					if (candle.State == CandleStates.Finished)
						lastClose = candle.ClosePrice;
				};
				strategy.OrderRegistering += order =>
				{
					if (order.Type != OrderTypes.Conditional)
						return;
					if (pairs.Count == 0 || pairs[^1].Count == 2)
						pairs.Add([]);
					pairs[^1].Add(order);
					var activation = Convert.ToDecimal(order.Condition.Parameters["ActivationPrice"]);
					var expected = lastClose + (order.Side == Sides.Buy ? 10m : -10m);
					if (lastClose <= 0m || activation != expected || order.Volume != 0.1m || strategy.Position != 0m)
						violations.Add("Every initial stop must be Step away from the completed tick-built candle, at InitialVolume, while flat.");
				};
				strategy.Trades.TradeAdded += trade =>
				{
					if (trade.Order.Type == OrderTypes.Conditional)
						filled.Add(trade.Order.TransactionId);
				};
				strategy.OrderReceived += (_, order) =>
				{
					if (order.Type == OrderTypes.Conditional && order.State == OrderStates.Done && order.Balance == order.Volume)
						cancelled.Add(order.TransactionId);
				};
			}, TimeSpan.FromHours(3));
		IsTrue(subscriptionSeen && pairs.Count >= 2, "Multiple default cycles must be exercised, including the old second-cycle cancellation failure.");
		var triggered = pairs.Where(pair => pair.Any(order => filled.Contains(order.TransactionId))).ToArray();
		IsTrue(triggered.Length >= 2);
		foreach (var pair in triggered)
		{
			AreEqual(2, pair.Count);
			AreNotEqual(pair[0].Side, pair[1].Side);
			AreEqual(1, pair.Count(order => filled.Contains(order.TransactionId)), "The actual tick history must not fill both initial stops in a cycle.");
			AreEqual(1, pair.Count(order => cancelled.Contains(order.TransactionId)), "The other real conditional order must actually be cancelled, not silently forgotten.");
		}
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(15)));
	}

	[TestMethod]
	[TestCategory("Shard07")]
	public async Task S0343_NeuralTemporalDifferenceUpdate()
	{
		await Replay(KeltnerRlSignal,
			(strategy, _) =>
			{
				IsTrue(strategy.Parameters.ContainsKey("LearningRate"), "RL with neural networks requires trainable weights, not a fixed boolean decision matrix.");
				AreEqual(0.05, Convert.ToDouble(strategy.Parameters["LearningRate"].Value));
				AreEqual(0.9, Convert.ToDouble(strategy.Parameters["DiscountFactor"].Value));
				AreEqual(0.1, Convert.ToDouble(strategy.Parameters["Exploration"].Value));
				AreEqual(42, Convert.ToInt32(strategy.Parameters["RandomSeed"].Value));
				dynamic implementation = strategy;
				dynamic model = implementation.CreateLearningModel();
				double[] state = [0.2, -0.3, 0.1, 0.4];
				double[] next = [-0.1, 0.25, 0.3, -0.2];
				var before = LearningValues((object)model.GetWeights());
				AreEqual(67, before.Length, "The model must have four features, eight trainable tanh hidden neurons and three action outputs, including biases.");
				uint randomState = 42;
				double NextRandom()
				{
					randomState = unchecked(1664525u * randomState + 1013904223u);
					return randomState / 4294967296.0;
				}
				var seededWeights = Enumerable.Range(0, 67).Select(_ => (NextRandom() - 0.5) * 0.2).ToArray();
				IsTrue(before.SequenceEqual(seededWeights), "Both languages must implement the published seed and identical initialization, not merely repeat themselves.");
				var q = NeuralForward(before, state);
				var predicted = LearningValues((object)model.Predict(state));
				for (var action = 0; action < 3; action++)
					AreEqual(q[action], predicted[action], 1e-12);
				var error = Math.Clamp(0.3 + 0.9 * NeuralForward(before, next).Max() - q[1], -1.0, 1.0);
				model.Learn(state, 1, 0.3, next);
				var after = LearningValues((object)model.GetWeights());
				for (var index = 0; index < before.Length; index++)
				{
					const double epsilon = 1e-6;
					var higher = (double[])before.Clone();
					var lower = (double[])before.Clone();
					higher[index] += epsilon;
					lower[index] -= epsilon;
					var gradient = (NeuralForward(higher, state)[1] - NeuralForward(lower, state)[1]) / (2 * epsilon);
					AreEqual(before[index] + 0.05 * error * gradient, after[index], 1e-9,
						"Each hidden/output weight must follow the independently differentiated TD semi-gradient; the future Q target is detached.");
				}
				IsFalse(before.SequenceEqual(after));
				AreEqual(1, (int)model.Updates);
				dynamic sameSeed = implementation.CreateLearningModel();
				IsTrue(before.SequenceEqual(LearningValues((object)sameSeed.GetWeights())), "Fresh models must be deterministic and must not share learned state.");
				for (var step = 0; step < 30; step++)
				{
					var expected = NextRandom() < 0.1 ? (int)(NextRandom() * 3) : Array.IndexOf(q, q.Max());
					AreEqual(expected, (int)sameSeed.SelectAction(state), "Epsilon-greedy action selection must use the same seeded random stream in both languages.");
				}
				SetParam(strategy, "RandomSeed", 43);
				dynamic otherSeed = implementation.CreateLearningModel();
				IsFalse(before.SequenceEqual(LearningValues((object)otherSeed.GetWeights())));
				SetParam(strategy, "RandomSeed", 42);
				SetParam(strategy, "LearningRate", 0.0);
				dynamic frozen = implementation.CreateLearningModel();
				frozen.Learn(state, 1, 0.3, next);
				IsTrue(before.SequenceEqual(LearningValues((object)frozen.GetWeights())), "LearningRate=0 must freeze both layers, not switch to a different hand-written signal.");
				SetParam(strategy, "LearningRate", 0.05);
			}, TimeSpan.FromDays(7));
	}

	[TestMethod]
	[TestCategory("Shard07")]
	public async Task S0343_CausalLearningDrivesEveryBreakoutOrder()
	{
		async Task<OrderTraceRecorder> Run(double rate)
		{
			var recorder = new OrderTraceRecorder();
			var violations = new List<string>();
			var candleCount = 0;
			var modelSteps = 0;
			var closeSum = 0m;
			var ema = 0m;
			var atr = 0m;
			var priorClose = 0m;
			var modelClose = 0m;
			var modelAtr = 0m;
			var entryPrice = 0m;
			var cooldown = 0;
			var previousAbove = false;
			var previousBelow = false;
			var buy = false;
			var sell = false;
			var signal = 0;
			double[] previousFeatures = null;
			var registered = new HashSet<DateTime>();
			await Replay(KeltnerRlSignal,
				(strategy, _) =>
				{
					SetParam(strategy, "LearningRate", rate);
					dynamic implementation = strategy;
					dynamic reference = implementation.CreateLearningModel();
					recorder.Attach(strategy);
					strategy.CandleReceived += (_, candle) =>
					{
						if (candle.State != CandleStates.Finished)
							return;
						candleCount++;
						var trueRange = candleCount == 1 ? candle.HighPrice - candle.LowPrice
							: Math.Max(candle.HighPrice - candle.LowPrice, Math.Max(Math.Abs(candle.HighPrice - priorClose), Math.Abs(candle.LowPrice - priorClose)));
						var atrCount = Math.Min(candleCount, 14);
						atr = (atr * (atrCount - 1) + trueRange) / atrCount;
						priorClose = candle.ClosePrice;
						if (candleCount <= 20)
						{
							closeSum += candle.ClosePrice;
							ema = closeSum / 20m;
						}
						else
							ema += (candle.ClosePrice - ema) * (2m / 21m);
						if (candleCount < 20 || atr <= 0m)
							return;
						dynamic actual = implementation.LearningModel;
						var actualWeights = LearningValues((object)actual.GetWeights());
						var expectedWeights = LearningValues((object)reference.GetWeights());
						if (actualWeights.Zip(expectedWeights).Any(pair => Math.Abs(pair.First - pair.Second) > 1e-10)
							|| (int)actual.Updates != (int)reference.Updates || (int)implementation.CurrentSignal != signal)
							violations.Add($"Model state differs before {candle.OpenTime:O}: training must use only already completed transitions.");
						double[] features =
						[
							Math.Tanh((double)((candle.ClosePrice - ema) / atr)),
							modelClose == 0m ? 0.0 : Math.Tanh((double)((candle.ClosePrice - modelClose) / atr)),
							modelAtr == 0m ? 0.0 : Math.Tanh((double)((atr - modelAtr) / modelAtr)),
							Math.Tanh((double)((candle.ClosePrice - candle.OpenPrice) / atr)),
						];
						if (previousFeatures != null)
						{
							var direction = signal == 1 ? 1.0 : signal == 2 ? -1.0 : 0.0;
							var reward = Math.Clamp(direction * (double)((candle.ClosePrice - modelClose) / modelAtr), -1.0, 1.0);
							reference.Learn(previousFeatures, signal, reward, features);
						}
						signal = (int)reference.SelectAction(features);
						previousFeatures = features;
						modelClose = candle.ClosePrice;
						modelAtr = atr;
						modelSteps++;
						if (cooldown > 0) cooldown--;
						if (strategy.Position == 0m) entryPrice = 0m;
						var above = modelClose > ema + 2m * atr;
						var below = modelClose < ema - 2m * atr;
						buy = !previousAbove && above && signal == 1;
						sell = !previousBelow && below && signal == 2;
						previousAbove = above;
						previousBelow = below;
					};
					strategy.OrderRegistering += order =>
					{
						var position = strategy.Position;
						var entry = order.Volume == strategy.Volume + Math.Abs(position);
						var validEntry = cooldown == 0 && (order.Side == Sides.Buy ? buy && position <= 0m : sell && position >= 0m);
						var validExit = position != 0m && order.Volume == Math.Abs(position) && (position > 0m
							? order.Side == Sides.Sell && (modelClose < ema || modelClose < entryPrice - 2m * atr)
							: order.Side == Sides.Buy && (modelClose > ema || modelClose > entryPrice + 2m * atr));
						if (!registered.Add(strategy.CurrentTime) || (entry ? !validEntry : !validExit)
							|| (int)implementation.CurrentSignal != signal)
							violations.Add("Every order must follow the causal learned action plus the independent Keltner/cooldown/exit oracle, with one order per bar.");
						entryPrice = entry ? modelClose : 0m;
						cooldown = 48;
					};
				}, TimeSpan.FromDays(31));
			IsTrue(modelSteps > 100, "Online learning must run across many formed bars, including bars with no trade.");
			IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(15)));
			return recorder;
		}
		var learned = await Run(0.05);
		var frozen = await Run(0.0);
		learned.AssertDiffersFrom(frozen, "Learning must change actual order decisions, not just diagnostic counters or unused weights.");
	}

	[TestMethod]
	[TestCategory("Shard06")]
	[DataRow(true)]
	[DataRow(false)]
	public async Task S0014_SupertrendDirectionAndVolumeConfirmedFlips(bool filter)
	{
		var volumes = new Queue<decimal>();
		var count = 0;
		var atr = 0m;
		var previousClose = 0m;
		decimal? upper = null;
		decimal? lower = null;
		decimal? trendClose = null;
		bool? up = null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buys = 0;
		var sells = 0;
		var reversals = 0;
		var rejected = 0;
		var lowVolumeExits = 0;
		var violations = new List<string>();
		await Replay(TradingViewSupertrendFlip,
			(strategy, _) =>
			{
				IsTrue(strategy.Parameters.ContainsKey("VolumeAvgPeriod"), "README promises an actual 20-bar volume confirmation, not unfiltered Supertrend flips.");
				AreEqual(10, strategy.Parameters["SupertrendPeriod"].Value);
				AreEqual(3m, Convert.ToDecimal(strategy.Parameters["SupertrendMultiplier"].Value));
				AreEqual(20, strategy.Parameters["VolumeAvgPeriod"].Value);
				AreEqual(true, strategy.Parameters["UseVolumeFilter"].Value);
				AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
				SetParam(strategy, "UseVolumeFilter", filter);
				strategy.CandleReceived += (_, candle) =>
				{
					if (candle.State != CandleStates.Finished)
						return;
					count++;
					expectedSide = null;
					volumes.Enqueue(candle.TotalVolume);
					if (volumes.Count > 20) volumes.Dequeue();
					var trueRange = count == 1 ? candle.HighPrice - candle.LowPrice
						: Math.Max(candle.HighPrice - candle.LowPrice, Math.Max(Math.Abs(candle.HighPrice - previousClose), Math.Abs(candle.LowPrice - previousClose)));
					var atrCount = Math.Min(count, 10);
					atr = (atr * (atrCount - 1) + trueRange) / atrCount;
					previousClose = candle.ClosePrice;
					if (count < 10)
						return;
					var median = (candle.HighPrice + candle.LowPrice) / 2m;
					var basicUpper = median + 3m * atr;
					var basicLower = median - 3m * atr;
					upper = upper == null || basicUpper < upper || trendClose > upper ? basicUpper : upper;
					lower = lower == null || basicLower > lower || trendClose < lower ? basicLower : lower;
					var nextUp = up is null ? candle.ClosePrice >= median
						: up.Value ? candle.ClosePrice > lower : candle.ClosePrice >= upper;
					var flip = up is bool previousUp && nextUp != previousUp;
					up = nextUp;
					trendClose = candle.ClosePrice;
					if (!flip)
						return;
					var confirmed = !filter || (volumes.Count == 20 && candle.TotalVolume > volumes.Average());
					if (!confirmed) rejected++;
					var opposite = nextUp ? strategy.Position < 0m : strategy.Position > 0m;
					if (confirmed && (nextUp ? strategy.Position <= 0m : strategy.Position >= 0m))
					{
						expectedSide = nextUp ? Sides.Buy : Sides.Sell;
						expectedVolume = strategy.Volume + Math.Abs(strategy.Position);
						if (opposite) reversals++;
					}
					else if (opposite)
					{
						expectedSide = nextUp ? Sides.Buy : Sides.Sell;
						expectedVolume = Math.Abs(strategy.Position);
						lowVolumeExits++;
					}
					if (expectedSide != null) expectedOrders++;
				};
				strategy.OrderRegistering += order =>
				{
					actualOrders++;
					if (order.Side == Sides.Buy) buys++; else sells++;
					if (expectedSide != order.Side || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
						violations.Add("Every order must follow a real Supertrend direction flip, volume confirmation for entries and unconditional opposite-flip exits.");
					expectedSide = null;
				};
			}, TimeSpan.FromDays(31));
		AreEqual(expectedOrders, actualOrders, "Every eligible flip must trade; rejected flips cannot cause delayed entries on later high-volume bars.");
		IsTrue(buys > 0 && sells > 0 && reversals > 0);
		if (filter) IsTrue(rejected > 0 && lowVolumeExits > 0, "The actual archive must exercise both rejected entries and low-volume opposite-flip exits.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(15)));
	}

	[TestMethod]
	[TestCategory("Shard03")]
	[DataRow(false, 0)]
	[DataRow(true, 0)]
	[DataRow(true, 1)]
	public async Task S3507_CalendarPlacesAndSettlesRealConditionalOrders(bool expiry, int lead)
	{
		var beginning = Paths.HistoryBeginDate;
		var early = beginning.AddMinutes(2);
		var notHigh = beginning.AddMinutes(10);
		var release = beginning.AddHours(1);
		var bid = 0m;
		var ask = 0m;
		var orders = new List<Order>();
		var firstPair = new List<Order>();
		var notHighStops = 0;
		var fills = new HashSet<long>();
		var cancellations = new Dictionary<long, DateTime>();
		var violations = new List<string>();
		var tickSubscription = false;
		await Replay(SampleDetectEconomicCalendar,
			(strategy, _) =>
			{
				SetParam(strategy, "OrderVolume", 2m);
				SetParam(strategy, "StopLossPoints", 10);
				SetParam(strategy, "TakeProfitPoints", 10);
				SetParam(strategy, "TrailingStopPoints", 0);
				SetParam(strategy, "LeadMinutes", lead);
				SetParam(strategy, "PostMinutes", 5);
				SetParam(strategy, "ExpiryMinutes", 1);
				SetParam(strategy, "BuyDistancePoints", expiry ? 1000000 : 1);
				SetParam(strategy, "SellDistancePoints", expiry ? 1000000 : 1);
				SetParam(strategy, "CalendarDefinition",
					$"{beginning.AddMinutes(1):yyyy-MM-dd HH:mm};EUR;High;Wrong currency\n"
					+ $"{beginning.AddMinutes(1):yyyy-MM-dd HH:mm};USD;Low;Low impact\n"
					+ (expiry ? $"{early:yyyy-MM-dd HH:mm};USD;High;Expired pending pair\n" : "")
					+ $"{notHigh:yyyy-MM-dd HH:mm};USD;Nfp;Nfp is not High\n"
					+ $"{notHigh.AddMinutes(10):yyyy-MM-dd HH:mm};USD;Medium;Medium is not High\n"
					+ $"{release:yyyy-MM-dd HH:mm};USD;High;Actual traded event");
				strategy.SubscriptionStarted += subscription => tickSubscription |= subscription.DataType == DataType.Ticks;
				strategy.ProcessStateChanged += changed =>
				{
					if (expiry && changed.ProcessState == ProcessStates.Stopped)
					{
						// Restore the fixture's live edit before the unchanged clone/settings round-trip.
						SetParam(strategy, "BuyDistancePoints", 1000000);
						SetParam(strategy, "SellDistancePoints", 1000000);
					}
				};
				strategy.Level1Received += (_, quote) =>
				{
					if (quote.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue)) bid = Convert.ToDecimal(bidValue);
					if (quote.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue)) ask = Convert.ToDecimal(askValue);
					if (expiry && quote.ServerTime >= beginning.AddMinutes(30))
					{
						SetParam(strategy, "BuyDistancePoints", 1);
						SetParam(strategy, "SellDistancePoints", 1);
					}
				};
				strategy.OrderRegistering += order =>
				{
					orders.Add(order);
					if (orders.Count <= 2)
					{
						AreEqual(OrderTypes.Conditional, order.Type, "README promises two actual pending stop orders, not internal price thresholds followed by market entries.");
						firstPair.Add(order);
					}
					if (order.Type != OrderTypes.Conditional) return;
					if (strategy.CurrentTime >= notHigh.AddMinutes(-lead) && strategy.CurrentTime < release.AddMinutes(-lead))
						notHighStops++;
					var eventTime = expiry && firstPair.Contains(order) ? early : release;
					var distance = expiry && firstPair.Contains(order) ? 1000000 : 1;
					var activation = Convert.ToDecimal(order.Condition.Parameters["ActivationPrice"]);
					var expected = order.Side == Sides.Buy ? ask + distance * strategy.Security.PriceStep.Value : bid - distance * strategy.Security.PriceStep.Value;
					if (activation != expected || order.Volume != 2m || strategy.CurrentTime < eventTime.AddMinutes(-lead) || strategy.CurrentTime > eventTime.AddMinutes(5))
						violations.Add("A real news stop must use the latest quote, configured distance/PriceStep/volume and only the matching high-impact UTC event window.");
				};
				strategy.Trades.TradeAdded += trade =>
				{
					if (trade.Order.Type == OrderTypes.Conditional) fills.Add(trade.Order.TransactionId);
				};
				strategy.OrderReceived += (_, order) =>
				{
					if (order.Type == OrderTypes.Conditional && order.State == OrderStates.Done && order.Balance == order.Volume)
						cancellations.TryAdd(order.TransactionId, strategy.CurrentTime);
				};
			}, TimeSpan.FromHours(3));
		IsTrue(tickSubscription, "Native stop matching must consume real archive ticks; quotes contain no LastTradePrice.");
		AreEqual(2, firstPair.Count);
		AreNotEqual(firstPair[0].Side, firstPair[1].Side);
		AreEqual(0, notHighStops, "README: only High triggers trades, so the USD Nfp and Medium events must not arm a stop pair.");
		AreEqual(expiry ? 4 : 2, orders.Count(order => order.Type == OrderTypes.Conditional), "Each eligible event must create exactly one real stop pair.");
		if (expiry)
		{
			foreach (var order in firstPair)
			{
				IsFalse(fills.Contains(order.TransactionId));
				IsTrue(cancellations.TryGetValue(order.TransactionId, out var time), "Timeout must acknowledge cancellation of both pending orders, not just clear local variables.");
				IsTrue(time >= early.AddMinutes(1) && time < early.AddMinutes(1).AddSeconds(5), "ExpiryMinutes is measured after release and is checked by the market-time timer.");
			}
		}
		var tradedPair = orders.Where(order => order.Type == OrderTypes.Conditional).TakeLast(2).ToArray();
		AreEqual(1, tradedPair.Count(order => fills.Contains(order.TransactionId)), "The traded pair must execute one real conditional entry.");
		AreEqual(1, tradedPair.Count(order => cancellations.ContainsKey(order.TransactionId)), "OCO must actually acknowledge cancelling the opposite pending stop.");
		IsTrue(orders.Any(order => order.Type == OrderTypes.Market), "The configured SL/TP must close the entered exposure.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(15)));
	}

	[TestMethod]
	[TestCategory("Shard03")]
	[DataRow(false, false)]
	[DataRow(true, false)]
	[DataRow(false, true)]
	public async Task S3507_ActualFillsDriveRiskAndTrailingExits(bool trailing, bool minimum)
	{
		var bid = 0m;
		var ask = 0m;
		var entryValue = 0m;
		var filled = 0m;
		decimal? trailingStop = null;
		var exitExpected = false;
		var expectedVolume = 0m;
		var exits = 0;
		var tightened = false;
		var previousExit = (Order)null;
		var violations = new List<string>();
		await Replay(SampleDetectEconomicCalendar,
			(strategy, _) =>
			{
				SetParam(strategy, "OrderVolume", 2m);
				SetParam(strategy, "UseMoneyManagement", !trailing);
				var riskPercent = minimum ? 0.0000001m : 0.001m;
				SetParam(strategy, "RiskPercent", riskPercent);
				SetParam(strategy, "StopLossPoints", trailing ? 0 : 100);
				SetParam(strategy, "TakeProfitPoints", trailing ? 0 : 100);
				SetParam(strategy, "TrailingStopPoints", trailing ? 100 : 0);
				SetParam(strategy, "BuyDistancePoints", 1);
				SetParam(strategy, "SellDistancePoints", 1);
				SetParam(strategy, "LeadMinutes", 0);
				SetParam(strategy, "CalendarDefinition", $"{Paths.HistoryBeginDate.AddMinutes(2):yyyy-MM-dd HH:mm};USD;High;Risk and fill protection");
				if (!trailing)
				{
					strategy.Security.VolumeStep = 0.3m;
					strategy.Security.MinVolume = 0.65m;
					strategy.Security.MaxVolume = 9.8m;
				}
				strategy.Trades.TradeAdded += trade =>
				{
					var volume = trade.Trade.Volume;
					if (trade.Order.Type == OrderTypes.Conditional)
					{
						filled += volume;
						entryValue += volume * trade.Trade.Price;
					}
					else if (filled > 0m)
					{
						var closed = Math.Min(filled, volume);
						entryValue -= entryValue / filled * closed;
						filled -= closed;
					}
				};
				strategy.Level1Received += (_, quote) =>
				{
					if (quote.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue)) bid = Convert.ToDecimal(bidValue);
					if (quote.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue)) ask = Convert.ToDecimal(askValue);
					exitExpected = false;
					if (filled <= 0m || strategy.Position == 0m || bid <= 0m || ask <= 0m) return;
					var entry = entryValue / filled;
					var direction = strategy.Position > 0m ? 1m : -1m;
					var price = direction > 0m ? bid : ask;
					var distance = 100 * strategy.Security.PriceStep.Value;
					if (trailing && direction * (price - entry) >= distance)
					{
						var candidate = price - direction * distance;
						if (trailingStop is null || direction * (candidate - trailingStop.Value) > 0m)
						{
							trailingStop = candidate;
							tightened = true;
						}
					}
					exitExpected = trailing ? trailingStop is decimal stop && direction * (price - stop) <= 0m
						: direction * (price - entry) <= -distance || direction * (price - entry) >= distance;
					expectedVolume = Math.Abs(strategy.Position);
				};
				strategy.OrderRegistering += order =>
				{
					if (order.Type == OrderTypes.Conditional)
					{
						var expected = 2m;
						if (!trailing)
						{
							var loss = 100m * ((strategy.Security.StepPrice ?? 0m) > 0m ? strategy.Security.StepPrice.Value
								: strategy.Security.PriceStep.Value * (strategy.Security.Multiplier ?? 1m));
							var risk = (strategy.Portfolio.CurrentValue ?? strategy.Portfolio.BeginValue ?? 0m) * riskPercent / 100m;
							expected = Math.Max(0.9m, Math.Floor(Math.Min(9.6m, risk / loss) / 0.3m) * 0.3m);
						}
						AreEqual(expected, order.Volume, "RiskPercent, stop distance, multiplier and the actual volume grid must determine both stop quantities.");
						return;
					}
					exits++;
					if (!exitExpected || order.Volume != expectedVolume || order.Side != (strategy.Position > 0m ? Sides.Sell : Sides.Buy)
						|| previousExit is { State: OrderStates.Pending or OrderStates.Active })
						violations.Add("Protective exits must use actual-fill VWAP, executable bid/ask, a monotonic trailing stop and only the current exposure without overlapping exits.");
					previousExit = order;
				};
			}, TimeSpan.FromHours(3));
		IsTrue(exits > 0, "This real archive window must exercise the configured protective exit, not just an entry.");
		AreEqual(0m, filled, "The acknowledged protective fills must close the entered exposure.");
		if (trailing) IsTrue(tightened, "The trail-only scenario must actually arm and tighten before closing.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(15)));
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S3406_NativeProtectionUsesEachSessionsAtr()
	{
		var entries = 0;
		var distances = new List<decimal>();
		object takeIdentity = null;
		object stopIdentity = null;
		var flags = BindingFlags.Instance | BindingFlags.NonPublic;
		var takeField = typeof(Strategy).GetField("_takeProfit", flags);
		var stopField = typeof(Strategy).GetField("_stopLoss", flags);
		var controllerField = typeof(Strategy).GetField("_protectiveController", flags);
		var positionsField = typeof(Strategy).GetField("_posControllers", flags);
		decimal sessionAtr = 0m;
		var session = DateTime.MinValue;
		await Replay(RangeFollowerExample,
			(strategy, _) =>
			{
				strategy.CandleReceived += (subscription, candle) =>
				{
					if (candle.State != CandleStates.Finished || subscription.DataType.Arg is not TimeSpan frame || frame != TimeSpan.FromMinutes(15)
						|| candle.OpenTime.Date == session) return;
					session = candle.OpenTime.Date;
					var indicator = strategy.Indicators.OfType<AverageTrueRange>().Single();
					sessionAtr = indicator.IsFormed ? indicator.GetCurrentValue() : 0m;
				};
				strategy.OrderRegistering += order =>
				{
					if (strategy.Position != 0m) return;
					entries++;
					IsNotNull(controllerField.GetValue(strategy), "README promises an active StartProtection controller, not only handwritten quote thresholds.");
					var take = (Unit)takeField.GetValue(strategy);
					var stop = (Unit)stopField.GetValue(strategy);
					IsNotNull(take);
					IsNotNull(stop);
					IsTrue(Math.Abs(stop.Value - sessionAtr * 0.6m) < 0.00000001m);
					IsTrue(Math.Abs(take.Value - sessionAtr * 0.4m) < 0.00000001m);
					AreEqual(UnitTypes.Absolute, take.Type);
					AreEqual(UnitTypes.Absolute, stop.Type);
					if (takeIdentity is not null)
					{
						IsTrue(ReferenceEquals(takeIdentity, take) && ReferenceEquals(stopIdentity, stop), "The cached native controller must keep live Unit references whose values change only between flat sessions.");
						var cached = ((IEnumerable)positionsField.GetValue(strategy)).Cast<object>().Single();
						var positionController = cached is DictionaryEntry item ? item.Value : cached.GetType().GetProperty("Value").GetValue(cached);
						var behaviour = positionController.GetType().GetField("_behaviour", flags).GetValue(positionController);
						var nativeTake = behaviour.GetType().GetProperty("TakeValue", flags).GetValue(behaviour);
						var nativeStop = behaviour.GetType().GetProperty("StopValue", flags).GetValue(behaviour);
						IsTrue(ReferenceEquals(take, nativeTake) && ReferenceEquals(stop, nativeStop), "Changing only the factory's values is insufficient: the actual cached native behaviour must see this session's distances.");
					}
					takeIdentity = take;
					stopIdentity = stop;
					distances.Add(stop.Value);
				};
			}, TimeSpan.FromDays(31));
		IsTrue(entries >= 3 && distances.Distinct().Count() >= 3, "The real archive must exercise several distinct daily ATR protection distances.");
	}

	private static double[] LearningValues(object values)
		=> ((IEnumerable)values).Cast<object>().Select(Convert.ToDouble).ToArray();

	private static double[] NeuralForward(double[] weights, double[] features)
	{
		var hidden = Enumerable.Range(0, 8).Select(neuron => Math.Tanh(weights[neuron * 5 + 4]
			+ Enumerable.Range(0, 4).Sum(feature => weights[neuron * 5 + feature] * features[feature]))).ToArray();
		return Enumerable.Range(0, 3).Select(action => weights[40 + action * 9 + 8]
			+ Enumerable.Range(0, 8).Sum(neuron => weights[40 + action * 9 + neuron] * hidden[neuron])).ToArray();
	}

	private const string MultiPairCloser = "2808_Multi_Pair_Closer";
	private const string MultiPairCloserMissingSymbol = "NOSUCH@NOWHERE";

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S2808_ClosesOnlyPositionsOlderThanMinAge()
	{
		// Both limits at zero put every known basket beyond one of them, so only the age of each leg decides. Leg 1
		// is opened when leg 0 has aged the ten minutes MinAgeSeconds asks for.
		var fixture = new MultiPairCloserFixture()
			.Open(10, 0, Sides.Buy, 0.002m)
			.Open(20, 1, Sides.Sell, 20m);
		string[] ids = null;

		await Replay(MultiPairCloser, (strategy, secondary) =>
		{
			AssertMultiPairCloserDefaults(strategy);
			MultiPairCloserFixture.WatchBothLegs(strategy, secondary);
			SetParam(strategy, "ProfitTarget", 0m);
			SetParam(strategy, "MaxLoss", 0m);
			SetParam(strategy, "MinAgeSeconds", 600);
			fixture.Attach(strategy, secondary);
			ids = [strategy.Security.Id, secondary.Id];
		}, MultiPairCloserFixture.ReplayDuration);

		fixture.AssertFollowsReadme();

		var closings = fixture.Closings;

		AreEqual(2, closings.Count, "The young leg must survive the first closing and go with the second.");
		AreEqual(TimeSpan.FromMinutes(20), closings[0].At);
		AreEqual(new MultiPairCloserFixture.ExitOrder(ids[0], Sides.Sell, 0.002m, OrderTypes.Market), closings[0].Orders.Single(),
			"README: only positions older than MinAgeSeconds are flattened.");
		AreEqual(TimeSpan.FromMinutes(30), closings[1].At);
		AreEqual(new MultiPairCloserFixture.ExitOrder(ids[1], Sides.Buy, 20m, OrderTypes.Market), closings[1].Orders.Single());
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S2808_MinAgeCountsFromWhenThePositionOpened()
	{
		// Five-minute candles are evaluated every five minutes and the leg is bought between two evaluations, twice:
		// once at the start and once after it was flattened. Both limits at zero put every known basket beyond one.
		var fixture = new MultiPairCloserFixture()
			.OpenOnClock(13, 0, Sides.Buy, 0.002m)
			.OpenOnClock(23, 0, Sides.Buy, 0.002m);
		string id = null;

		await Replay(MultiPairCloser, (strategy, secondary) =>
		{
			SetParam(strategy, "WatchedSymbols", strategy.Security.Id);
			SetParam(strategy, "ProfitTarget", 0m);
			SetParam(strategy, "MaxLoss", 0m);
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "MinAgeSeconds", 360);
			fixture.Attach(strategy, secondary);
			id = strategy.Security.Id;
		}, MultiPairCloserFixture.ReplayDuration);

		fixture.AssertFollowsReadme();

		var closings = fixture.Closings;
		var exit = new MultiPairCloserFixture.ExitOrder(id, Sides.Sell, 0.002m, OrderTypes.Market);

		AreEqual(2, closings.Count, "Each opening of the leg must be flattened once.");
		AreEqual(TimeSpan.FromMinutes(15), closings[0].At,
			"README: MinAgeSeconds is counted from when the position became non-zero, so the second evaluation after the fill closes it.");
		AreEqual(exit, closings[0].Orders.Single());
		AreEqual(TimeSpan.FromMinutes(25), closings[1].At,
			"README: a position that was flat and opened again is aged from its new opening, not from the first one.");
		AreEqual(exit, closings[1].Orders.Single());
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S2808_PositionsOutsideTheWatchListStayOutOfTheBasket()
	{
		// Only leg 0 is watched and both limits at zero put every known basket beyond one of them, so leg 0 goes as
		// soon as it is a minute old, while leg 1 is neither summed into the basket nor closed.
		var fixture = new MultiPairCloserFixture()
			.Open(10, 0, Sides.Buy, 0.002m)
			.Open(10, 1, Sides.Sell, 20m);
		string id = null;

		await Replay(MultiPairCloser, (strategy, secondary) =>
		{
			SetParam(strategy, "WatchedSymbols", strategy.Security.Id);
			SetParam(strategy, "ProfitTarget", 0m);
			SetParam(strategy, "MaxLoss", 0m);
			fixture.Attach(strategy, secondary);
			id = strategy.Security.Id;
		}, MultiPairCloserFixture.ReplayDuration);

		fixture.AssertFollowsReadme();
		fixture.AssertSingleClosing(11, null, 1);
		AreEqual(new MultiPairCloserFixture.ExitOrder(id, Sides.Sell, 0.002m, OrderTypes.Market), fixture.Closings[0].Orders[0],
			"README: the basket is the watched symbols, so a position outside them is neither counted nor closed.");
		IsTrue(fixture.Baskets.Single(evaluation => evaluation.At == TimeSpan.FromMinutes(11)).Reported[1] is decimal unwatched && unwatched != 0m,
			"Leg 1 must carry a floating profit of its own when leg 0 is closed, or leaving it out of the basket proves nothing.");
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S2808_EmptyWatchListSupervisesTheAssignedSecurity()
	{
		// Both limits at zero put every known basket beyond one of them, so leg 0 goes as soon as it is a minute old.
		var fixture = new MultiPairCloserFixture()
			.Open(10, 0, Sides.Buy, 0.002m);
		string id = null;

		await Replay(MultiPairCloser, (strategy, secondary) =>
		{
			SetParam(strategy, "WatchedSymbols", string.Empty);
			SetParam(strategy, "ProfitTarget", 0m);
			SetParam(strategy, "MaxLoss", 0m);
			// Not the default, so the slippage in the closing line has to come from the parameter.
			SetParam(strategy, "Slippage", 7);
			fixture.Attach(strategy, secondary);
			id = strategy.Security.Id;
		}, MultiPairCloserFixture.ReplayDuration);

		fixture.AssertFollowsReadme();
		fixture.AssertSingleClosing(11, null, 1);
		AreEqual(new MultiPairCloserFixture.ExitOrder(id, Sides.Sell, 0.002m, OrderTypes.Market), fixture.Closings[0].Orders[0]);
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S2808_UnknownWatchedSymbolStopsTheStart()
	{
		var error = await ThrowsAsync<InvalidOperationException>(() => Replay(MultiPairCloser,
			(strategy, _) => SetParam(strategy, "WatchedSymbols", $"{strategy.Security.Id},{MultiPairCloserMissingSymbol}"),
			MultiPairCloserFixture.ReplayDuration));

		IsTrue(error.Message.Contains(MultiPairCloserMissingSymbol, StringComparison.Ordinal),
			$"README: the watched identifiers must be available through the connector; the error names the missing one. Got: {error.Message}");
	}

	private static void AssertMultiPairCloserDefaults(Strategy strategy)
	{
		var parameters = strategy.Parameters;

		AreEqual("GBPUSD,USDCAD,USDCHF,USDSEK", (string)parameters["WatchedSymbols"].Value, "README default WatchedSymbols.");
		AreEqual(60m, Convert.ToDecimal(parameters["ProfitTarget"].Value), "README default ProfitTarget.");
		AreEqual(60m, Convert.ToDecimal(parameters["MaxLoss"].Value), "README default MaxLoss.");
		AreEqual(10, Convert.ToInt32(parameters["Slippage"].Value), "README default Slippage.");
		AreEqual(60, Convert.ToInt32(parameters["MinAgeSeconds"].Value), "README default MinAgeSeconds.");
		AreEqual<object>(TimeSpan.FromMinutes(1).TimeFrame(), parameters["CandleType"].Value, "README default CandleType.");
	}

	private const string Ccfp = "2907_CCFp_Currency_Strength";
	private const int CcfpFastLength = 2;
	private const int CcfpSlowLength = 3;
	private const decimal CcfpStrengthStep = 0.000001m;

	// Of two equal strengths the one listed first is taken as the top or the down currency.
	private static readonly string[] _ccfpCurrencies = ["USD", "EUR", "GBP", "CHF", "JPY", "AUD", "CAD", "NZD"];
	private static readonly string[] _ccfpUsdQuotedPairs = ["EURUSD", "GBPUSD", "AUDUSD", "NZDUSD"];
	private static readonly string[] _ccfpUsdBasePairs = ["USDCAD", "USDCHF", "USDJPY"];

	private sealed class CcfpPairModel
	{
		public bool IsUsdQuoted { get; init; }
		public Queue<decimal> Closes { get; } = new();
		public decimal Ratio { get; set; }
		public DateTime? Time { get; set; }
	}

	[TestMethod]
	[TestCategory("Shard03")]
	[DataRow(true)]
	[DataRow(false)]
	public async Task S2907_OrdersFollowCcfpStrengthsAndActualPositions(bool closeOpposite)
	{
		var pairs = new Dictionary<Subscription, CcfpPairModel>();
		Dictionary<string, decimal> previous = null;
		DateTime? lastEvaluation = null;
		var expected = new Queue<(string SecurityId, Sides Side, decimal Volume, string Rule)>();
		var violations = new List<string>();
		var signals = 0;
		var twoLegSignals = 0;
		var expectedOrders = 0;
		var actualOrders = 0;
		var closingOrders = 0;
		var entriesOverOpposite = 0;
		var entriesAfterManualClose = 0;
		var heldLegs = 0;

		void violate(DateTime time, string text)
		{
			if (violations.Count < 12)
				violations.Add($"{time:O}: {text}");
		}

		static string describe(IEnumerable<(string SecurityId, Sides Side, decimal Volume, string Rule)> orders)
			=> string.Join(", ", orders.Select(o => $"{o.Side} {o.Volume} {o.SecurityId} ({o.Rule})"));

		void expect(string securityId, Sides side, decimal volume, string rule)
		{
			expected.Enqueue((securityId, side, volume, rule));
			expectedOrders++;
		}

		await Replay(Ccfp, (strategy, second) =>
		{
			var first = strategy.Security;
			SetupCcfpFixture(strategy, second, closeOpposite);

			(Security Security, Sides Side) leg(string currency, bool wantCurrencyLong)
				=> currency is "EUR" or "GBP" or "AUD" or "NZD"
					? (first, wantCurrencyLong ? Sides.Buy : Sides.Sell)
					: (second, wantCurrencyLong ? Sides.Sell : Sides.Buy);

			strategy.CandleReceived += (subscription, candle) =>
			{
				if (candle.State != CandleStates.Finished || strategy.ProcessState != ProcessStates.Started
					|| subscription.DataType.Arg is not TimeSpan frame || frame != TimeSpan.FromMinutes(5))
					return;

				// Runs before the strategy's own handler, so the orders of the previous evaluation are all in.
				if (expected.Count > 0)
				{
					violate(candle.OpenTime, $"The strategy did not register {describe(expected)}.");
					expected.Clear();
				}

				if (!pairs.TryGetValue(subscription, out var pair))
				{
					pair = new() { IsUsdQuoted = candle.SecurityId == first.Id.ToSecurityId() };
					pairs.Add(subscription, pair);
				}

				pair.Closes.Enqueue(candle.ClosePrice);

				if (pair.Closes.Count > CcfpSlowLength)
					pair.Closes.Dequeue();

				if (pair.Closes.Count < CcfpSlowLength)
					return;

				var slow = pair.Closes.Sum() / CcfpSlowLength;
				var fast = pair.Closes.Skip(CcfpSlowLength - CcfpFastLength).Sum() / CcfpFastLength;

				if (fast == 0m || slow == 0m)
					return;

				pair.Ratio = fast / slow;
				pair.Time = candle.OpenTime;

				// The strengths are recomputed once, when all seven pairs have finished the same bar.
				if (pairs.Count < 7 || pairs.Values.Any(p => p.Time != pair.Time) || pair.Time == lastEvaluation)
					return;

				lastEvaluation = pair.Time;

				// The pairs are subscribed in the README order, so transaction ids tell which major each series is.
				var quoted = pairs.Where(p => p.Value.IsUsdQuoted).OrderBy(p => p.Key.TransactionId).Select(p => p.Value.Ratio).ToArray();
				var based = pairs.Where(p => !p.Value.IsUsdQuoted).OrderBy(p => p.Key.TransactionId).Select(p => p.Value.Ratio).ToArray();

				if (quoted.Length != 4 || based.Length != 3)
				{
					violate(candle.OpenTime, $"Expected four XXXUSD and three USDXXX candle series, got {quoted.Length} and {based.Length}.");
					return;
				}

				// Each currency's fast/slow ratio against USD; a cross ratio is the quotient of two of them.
				var ratios = new Dictionary<string, decimal>
				{
					["USD"] = 1m,
					["EUR"] = quoted[0],
					["GBP"] = quoted[1],
					["AUD"] = quoted[2],
					["NZD"] = quoted[3],
					["CAD"] = 1m / based[0],
					["CHF"] = 1m / based[1],
					["JPY"] = 1m / based[2],
				};

				// CCFp: a currency's strength is the sum over the other seven of its cross ratio, fast over slow, less one.
				var strengths = _ccfpCurrencies.ToDictionary(c => c, c => _ccfpCurrencies.Where(d => d != c).Aggregate(0m, (sum, d) => sum + (ratios[c] / ratios[d] - 1m)));
				var top = _ccfpCurrencies.MaxBy(c => strengths[c]);
				var down = _ccfpCurrencies.MinBy(c => strengths[c]);
				var last = previous;
				previous = strengths;

				if (last is null
					|| last[top] - last[down] >= CcfpStrengthStep
					|| strengths[top] - strengths[down] < CcfpStrengthStep
					|| strengths[top] <= last[top]
					|| strengths[down] >= last[down])
					return;

				var legs = new List<(Security Security, Sides Side)>();

				if (top != "USD")
					legs.Add(leg(top, true));

				if (down != "USD")
					legs.Add(leg(down, false));

				// Signals in turn find each pair as a manual tool may leave it: untouched, flat, opposite, already held.
				var manualMode = signals % 4;
				signals++;

				if (legs.Count == 2)
					twoLegSignals++;

				foreach (var (security, side) in legs)
				{
					var direction = side == Sides.Buy ? 1m : -1m;
					var ownPosition = strategy.GetPositionValue(security, strategy.Portfolio) ?? 0m;
					decimal? manual = manualMode switch
					{
						1 => 0m,
						2 => -3m * direction * strategy.Volume,
						3 => 2m * direction * strategy.Volume,
						_ => null,
					};

					if (manual is decimal value)
						strategy.SetPositionValue(security, strategy.Portfolio, value, strategy.CurrentTime);

					var position = strategy.GetPositionValue(security, strategy.Portfolio) ?? 0m;

					if (position * direction > 0m)
					{
						heldLegs++;
						continue;
					}

					if (position != 0m)
					{
						if (closeOpposite)
						{
							expect(security.Id, side, Math.Abs(position), "closing order for the opposite position");
							closingOrders++;
						}
						else
						{
							entriesOverOpposite++;
						}
					}
					else if (manualMode == 1 && ownPosition != 0m)
						entriesAfterManualClose++;

					expect(security.Id, side, strategy.Volume, $"{top}/{down} entry");
				}
			};

			strategy.OrderRegistering += order =>
			{
				actualOrders++;

				if (!expected.TryDequeue(out var next))
				{
					violate(strategy.CurrentTime, $"{order.Type} {order.Side} {order.Volume} on {order.Security?.Id} was registered although no README rule calls for an order.");
					return;
				}

				if (order.Security?.Id != next.SecurityId || order.Side != next.Side || order.Volume != next.Volume || order.Type != OrderTypes.Market || order.Comment != "(TOPDOWN)")
				{
					violate(strategy.CurrentTime, $"{order.Type} {order.Side} {order.Volume} on {order.Security?.Id} '{order.Comment}' was registered where the {next.Rule} calls for a market {next.Side} {next.Volume} on {next.SecurityId} '(TOPDOWN)'.");
					return;
				}

				var attached = next.SecurityId == first.Id ? first : second;

				if (order.Security.PriceStep != attached.PriceStep || order.Security.VolumeStep != attached.VolumeStep)
					violate(strategy.CurrentTime, $"The order on {next.SecurityId} does not trade the attached security: price step {order.Security.PriceStep}, volume step {order.Security.VolumeStep} instead of {attached.PriceStep}, {attached.VolumeStep}.");
			};
		}, TimeSpan.FromDays(7));

		if (expected.Count > 0)
			violations.Add($"The strategy did not register {describe(expected)} after the last evaluation.");

		TestContext.WriteLine($"python={IsPython}, closeOpposite={closeOpposite}: signals={signals}, twoLeg={twoLegSignals}, orders={actualOrders}/{expectedOrders}, closing={closingOrders}, overOpposite={entriesOverOpposite}, afterManualClose={entriesAfterManualClose}, held={heldLegs}");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));
		AreEqual(expectedOrders, actualOrders, "Every order must come from a CCFp top/down crossing traded against the actual position.");
		IsTrue(twoLegSignals > 0, "The archive must produce a signal between two non-USD currencies, which trades both of their USD pairs.");
		IsTrue(entriesAfterManualClose > 0, "The fixture must reach entries on pairs whose position was closed outside the strategy.");
		IsTrue(heldLegs > 0, "The fixture must reach signals on pairs already held in the signal's direction.");

		if (closeOpposite)
			IsTrue(closingOrders > 0, "The fixture must reach opposite positions, which Close Opposite closes with a market order before the entry.");
		else
			IsTrue(entriesOverOpposite > 0, "The fixture must reach opposite positions, over which the entry is sent without a closing order.");
	}

	[TestMethod]
	[TestCategory("Shard03")]
	public async Task S2907_ParametersAndResetMatchAcrossLanguages()
	{
		Strategy strategy = null;
		object evaluatedBeforeReset = null;

		await Replay(Ccfp, (current, second) =>
		{
			strategy = current;

			foreach (var (id, name, group) in new[]
			{
				("EURUSD", "EURUSD", "Securities"),
				("GBPUSD", "GBPUSD", "Securities"),
				("AUDUSD", "AUDUSD", "Securities"),
				("NZDUSD", "NZDUSD", "Securities"),
				("USDCAD", "USDCAD", "Securities"),
				("USDCHF", "USDCHF", "Securities"),
				("USDJPY", "USDJPY", "Securities"),
				("FastMa", "Fast MA", "Indicators"),
				("SlowMa", "Slow MA", "Indicators"),
				("StrengthStep", "Strength Step", "Signal"),
				("CloseOpposite", "Close Opposite", "Trading"),
				("CandleType", "Candle Type", "General"),
			})
			{
				IsTrue(current.Parameters.TryGetValue(id, out var param), $"The README parameter {id} is missing.");
				var display = param.Attributes.OfType<DisplayAttribute>().FirstOrDefault();
				AreEqual(name, display?.Name, $"{id} must be shown as '{name}'.");
				AreEqual(group, display?.GroupName, $"{id} must sit in the '{group}' group in both languages.");
			}

			SetupCcfpFixture(current, second, closeOpposite: true);

			current.ProcessStateChanged += changed =>
			{
				if (ReferenceEquals(changed, current) && changed.ProcessState == ProcessStates.Stopped)
					evaluatedBeforeReset = ReadCcfpLastEvaluationTime(current);
			};
		}, TimeSpan.FromDays(3));

		IsNotNull(evaluatedBeforeReset, "The replay must evaluate common bars before it stops.");
		IsNull(ReadCcfpLastEvaluationTime(strategy), "Reset must forget the last evaluated bar, so a restarted run evaluates its first common bar.");
	}

	private static void SetupCcfpFixture(Strategy strategy, Security second, bool closeOpposite)
	{
		// XXXUSD majors on one stream and USDXXX majors on the other; one orientation per stream keeps
		// the two groups apart, so signals between two non-USD currencies trade both streams.
		foreach (var name in _ccfpUsdQuotedPairs)
			SetParam(strategy, name, strategy.Security.Id);

		foreach (var name in _ccfpUsdBasePairs)
			SetParam(strategy, name, second.Id);

		SetParam(strategy, "FastMa", CcfpFastLength);
		SetParam(strategy, "SlowMa", CcfpSlowLength);
		SetParam(strategy, "StrengthStep", CcfpStrengthStep);
		SetParam(strategy, "CloseOpposite", closeOpposite);
		SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
	}

	private object ReadCcfpLastEvaluationTime(Strategy strategy)
	{
		if (!IsPython)
			return strategy.GetType().GetField("_lastEvaluationTime", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(strategy);

		dynamic implementation = strategy;
		return implementation._last_evaluation_time;
	}

	private const string OcoPendingOrders = "3008_OCO_Pending_Orders";

	private static readonly (string Id, string Name)[] _ocoParameterNames =
	[
		("OrderVolume", "Order volume"),
		("BuyLimitPrice", "Buy limit price"),
		("BuyStopPrice", "Buy stop price"),
		("SellLimitPrice", "Sell limit price"),
		("SellStopPrice", "Sell stop price"),
		("StopLossPips", "Stop loss (pips)"),
		("TakeProfitPips", "Take profit (pips)"),
		("UseOcoLink", "Use OCO link"),
		("Armed", "Armed"),
	];

	// The trigger entry names the level, its price and the quote that reached it.
	private static readonly Regex _ocoTriggerLog = new(
		@"^(?<name>Buy limit|Buy stop|Sell limit|Sell stop) (?<level>\S+) hit by (?<quote>ask|bid) (?<price>[^:\s]+):",
		RegexOptions.CultureInvariant);

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S3008_ParametersCarryReadmeNames()
	{
		await Replay(OcoPendingOrders, (strategy, _) =>
		{
			foreach (var (id, name) in _ocoParameterNames)
			{
				var display = strategy.Parameters[id].Attributes.OfType<DisplayAttribute>().SingleOrDefault();

				AreEqual(name, display?.Name, $"README names the parameter {id} \"{name}\".");
				IsFalse(string.IsNullOrEmpty(display?.Description), $"README describes the parameter {id}.");
			}

			SetParam(strategy, "BuyStopPrice", 1m);
			SetParam(strategy, "Armed", true);
		}, TimeSpan.FromHours(1));
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S3008_EveryUpdateComparesAllStoredLevels()
	{
		var activationTime = Paths.HistoryBeginDate.AddMinutes(1);
		var updates = 0;
		var armedUpdate = 0;
		var orders = new List<(int Update, Sides Side, decimal Volume)>();
		bool? armedAtStop = null;

		await Replay(OcoPendingOrders, (strategy, _) =>
		{
			SetParam(strategy, "OrderVolume", 2m);
			SetParam(strategy, "UseOcoLink", false);

			// Any ask is at or below this buy limit and any bid is at or above this sell limit.
			SetParam(strategy, "BuyLimitPrice", 1000000m);
			SetParam(strategy, "SellLimitPrice", 1m);

			strategy.Level1Received += (_, quote) =>
			{
				updates++;

				if (armedUpdate == 0 && quote.ServerTime >= activationTime)
				{
					armedUpdate = updates;
					SetParam(strategy, "Armed", true);
				}
			};
			strategy.OrderRegistering += order => orders.Add((updates, order.Side, order.Volume));
			strategy.ProcessStateChanged += _ =>
			{
				if (strategy.ProcessState == ProcessStates.Stopped)
					armedAtStop = (bool)strategy.Parameters["Armed"].Value;
			};
		}, TimeSpan.FromHours(1));

		IsTrue(armedUpdate > 0, "The replay must reach the arming time.");
		AreEqual(2, orders.Count, "Each satisfied level fires exactly once and is then cleared.");
		AreEqual(Sides.Buy, orders[0].Side);
		AreEqual(Sides.Sell, orders[1].Side);
		IsTrue(orders.All(order => order.Volume == 2m && order.Update >= armedUpdate), "Orders carry the configured volume and are sent only once armed.");
		AreEqual(orders[0].Update, orders[1].Update,
			"README: every update compares the stored bid and ask with all thresholds, so the second satisfied level fires on the same update instead of waiting for a later quote.");
		AreEqual((bool?)false, armedAtStop, "With every level consumed the strategy disarms itself.");
	}

	[TestMethod]
	[TestCategory("Shard00")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task S3008_EveryTriggerAndTheAutomaticResetAreLogged(bool linked)
	{
		var activationTime = Paths.HistoryBeginDate.AddMinutes(1);
		var armed = false;
		decimal? bid = null;
		decimal? ask = null;
		var journal = new List<string>();
		var violations = new List<string>();

		await Replay(OcoPendingOrders, (strategy, _) =>
		{
			SetParam(strategy, "UseOcoLink", linked);

			// Any ask is at or below this buy limit and any bid is at or below this sell stop.
			SetParam(strategy, "BuyLimitPrice", 1000000m);
			SetParam(strategy, "SellStopPrice", 1000000m);

			strategy.Level1Received += (_, quote) =>
			{
				bid = quote.TryGetDecimal(Level1Fields.BestBidPrice) ?? bid;
				ask = quote.TryGetDecimal(Level1Fields.BestAskPrice) ?? ask;

				if (!armed && quote.ServerTime >= activationTime)
				{
					armed = true;
					SetParam(strategy, "Armed", true);
				}
			};
			strategy.Log += message =>
			{
				if (message.Level != LogLevels.Info)
					return;

				var text = message.Message;
				var match = _ocoTriggerLog.Match(text);

				if (!match.Success)
				{
					if (text.Contains("disarmed", StringComparison.OrdinalIgnoreCase))
						journal.Add("reset");

					return;
				}

				var name = match.Groups["name"].Value;
				var level = ParseOcoLogNumber(match.Groups["level"].Value);
				var price = ParseOcoLogNumber(match.Groups["price"].Value);
				var isBuy = name.StartsWith("Buy", StringComparison.Ordinal);
				var latest = isBuy ? ask : bid;

				if (match.Groups["quote"].Value != (isBuy ? "ask" : "bid"))
					violations.Add($"{name} is compared with the best {(isBuy ? "ask" : "bid")}: {text}");

				if (price != latest)
					violations.Add($"{name} must report the latest stored quote {latest}: {text}");

				var reached = name switch
				{
					"Buy limit" => price <= level,
					"Buy stop" => price >= level,
					"Sell limit" => price >= level,
					_ => price <= level,
				};

				if (!reached)
					violations.Add($"{name} fired before the quote reached the level: {text}");

				journal.Add(name);
			};
			strategy.OrderRegistering += order => journal.Add($"{order.Side} order");
		}, TimeSpan.FromHours(1));

		string[] expected = linked
			? ["Buy limit", "Buy order", "reset"]
			: ["Buy limit", "Buy order", "Sell stop", "Sell order", "reset"];

		AreEqual(string.Join(" | ", expected), string.Join(" | ", journal),
			"README: a log entry precedes every triggered order, and the automatic reset of Armed is reported once.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));
	}

	// C# formats the numbers with the current culture, Python always with a dot.
	private static decimal ParseOcoLogNumber(string text)
		=> decimal.Parse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture);

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S3008_OcoArmedLevel1Trigger()
	{
		var recorder = new OrderTraceRecorder();
		Strategy captured = null;
		bool? armedAtStop = null;

		await Replay("3008_OCO_Pending_Orders", (strategy, _) =>
		{
			captured = strategy;
			SetParam(strategy, "OrderVolume", 2m);
			SetParam(strategy, "BuyStopPrice", 0.01m);
			SetParam(strategy, "UseOcoLink", true);
			SetParam(strategy, "Armed", true);
			strategy.ProcessStateChanged += _ =>
			{
				if (strategy.ProcessState == ProcessStates.Stopped)
					armedAtStop = (bool)strategy.Parameters["Armed"].Value;
			};
			recorder.Attach(strategy);
		});

		recorder.AssertFirstSide(Sides.Buy);
		recorder.AssertFirstVolume(2m);
		AreEqual((bool?)false, armedAtStop, "The live one-shot trigger must disarm after firing.");
		AreEqual(true, captured.Parameters["Armed"].Value, "Reset must restore the original configuration for a repeatable run.");
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public Task S3064_TwoPerbar()
		// This intentionally trades on nearly every bar. A natural one-day window
		// retains high trade coverage without generating ~16k fills per language.
		=> Replay("3064_Two_PerBar", null, TimeSpan.FromDays(1));

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S3104_MaMacdPositionAveragingProtection()
	{
		var recorder = new OrderTraceRecorder();

		await Replay("3104_MA_MACD_Position_Averaging", (strategy, _) =>
		{
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "MaPeriod", 3);
			SetParam(strategy, "MacdFastPeriod", 2);
			SetParam(strategy, "MacdSlowPeriod", 4);
			SetParam(strategy, "MacdSignalPeriod", 2);
			SetParam(strategy, "IndentPips", 0);
			SetParam(strategy, "MacdRatio", 0m);
			SetParam(strategy, "StopLossPips", 1);
			SetParam(strategy, "TakeProfitPips", 1);
			recorder.Attach(strategy);
		}, TimeSpan.FromDays(2));

		recorder.AssertFirstOppositeAfter(TimeSpan.FromTicks(1));
		recorder.AssertFirstOppositeWithin(TimeSpan.FromMinutes(10));
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S3206_RiskRewardRatioProtection()
	{
		var recorder = new OrderTraceRecorder();

		await Replay("3206_Risk_Reward_Ratio", (strategy, _) =>
		{
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "FastMaPeriod", 2);
			SetParam(strategy, "SlowMaPeriod", 3);
			SetParam(strategy, "MomentumThreshold", 0m);
			SetParam(strategy, "StopLossPips", 1);
			SetParam(strategy, "RewardRatio", 1m);
			SetParam(strategy, "EnableTrailing", false);
			SetParam(strategy, "EnableBreakEven", false);
			recorder.Attach(strategy);
		}, TimeSpan.FromDays(2));

		recorder.AssertFirstOppositeAfter(TimeSpan.FromTicks(1));
		recorder.AssertFirstOppositeWithin(TimeSpan.FromMinutes(10));
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task S3301_CryptoAnalysisProtection()
	{
		var recorder = new OrderTraceRecorder();

		await Replay("3301_Crypto_Analysis", (strategy, _) =>
		{
			// Every stream runs on 5-minute candles with short periods, so entries come within the first days of the replay.
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "MomentumCandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "MacdCandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "FastMaPeriod", 2);
			SetParam(strategy, "SlowMaPeriod", 3);
			SetParam(strategy, "MomentumPeriod", 2);
			SetParam(strategy, "MomentumBuyThreshold", 0m);
			SetParam(strategy, "MomentumSellThreshold", 0m);
			SetParam(strategy, "MacdFastLength", 2);
			SetParam(strategy, "MacdSlowLength", 3);
			SetParam(strategy, "MacdSignalLength", 2);
			SetParam(strategy, "StopLossPips", 1);
			SetParam(strategy, "TakeProfitPips", 1);
			SetParam(strategy, "TrailingStopPips", 0);
			SetParam(strategy, "UseBreakEven", false);
			recorder.Attach(strategy);
		}, TimeSpan.FromDays(7));

		recorder.AssertFirstOppositeAfter(TimeSpan.FromTicks(1));
		recorder.AssertFirstOppositeWithin(TimeSpan.FromMinutes(10));
	}

	[TestMethod]
	[TestCategory("Shard03")]
	public async Task S3507_EconomicCalendarArmsNewsStops()
	{
		var recorder = new OrderTraceRecorder();
		var eventTime = Paths.HistoryBeginDate.Date.AddDays(1).AddHours(12);

		await Replay("3507_Sample_Detect_Economic_Calendar", (strategy, _) =>
		{
			SetParam(strategy, "TradeNews", true);
			SetParam(strategy, "OrderVolume", 2m);
			SetParam(strategy, "StopLossPoints", 10);
			SetParam(strategy, "TakeProfitPoints", 10);
			SetParam(strategy, "TrailingStopPoints", 0);
			SetParam(strategy, "BuyDistancePoints", 1);
			SetParam(strategy, "SellDistancePoints", 1);
			SetParam(strategy, "LeadMinutes", 60);
			SetParam(strategy, "PostMinutes", 60);
			SetParam(strategy, "ExpiryMinutes", 180);
			SetParam(strategy, "BaseCurrency", "USD");
			SetParam(strategy, "CalendarDefinition", $"{eventTime:yyyy-MM-dd HH:mm};USD;High;Behavior test");
			recorder.Attach(strategy);
		});

		recorder.AssertFirstVolume(2m);
		recorder.AssertFirstOrderAfterStart(TimeSpan.FromMinutes(1));
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S3710_Rrsrandomness()
	{
		// The modes go in as numbers both versions accept: Mode 0 = DoubleSide, 1 = OneSide; MoneyRiskMode 0 = FixedMoney, 1 = BalancePercentage.
		async Task<OrderTraceRecorder> Run(
			int mode,
			decimal minVolume,
			decimal maxVolume,
			decimal maxSpread,
			int riskMode,
			decimal riskValue,
			decimal takeProfit = 1m,
			decimal stopLoss = 1m,
			TimeSpan? postTradeHorizon = null,
			bool assertParameters = false)
		{
			var recorder = new OrderTraceRecorder();

			await Replay("3710_RRSRandomness", (strategy, _) =>
			{
				if (assertParameters)
				{
					// Python declares the modes as ints and the fractional inputs as floats, C# as enums and decimals.
					var modeType = IsPython ? typeof(int) : typeof(Enum);
					var fractionalType = IsPython ? typeof(double) : typeof(decimal);

					(string name, Type type)[] expected =
					[
						("Mode", modeType),
						("MinVolume", fractionalType),
						("MaxVolume", fractionalType),
						("TakeProfitPoints", fractionalType),
						("StopLossPoints", fractionalType),
						("TrailingStartPoints", fractionalType),
						("TrailingGapPoints", fractionalType),
						("MaxSpreadPoints", fractionalType),
						("SlippagePoints", fractionalType),
						("MoneyRiskMode", modeType),
						("RiskValue", fractionalType),
						("TradeComment", typeof(string)),
						("CandleType", typeof(DataType)),
					];

					foreach (var (name, type) in expected)
					{
						IsTrue(strategy.Parameters.TryGetValue(name, out var parameter), $"The example has no '{name}' parameter.");
						IsInstanceOfType(parameter.Value, type, $"Parameter '{name}' has an unexpected type.");
					}
				}

				SetParam(strategy, "Mode", mode);
				SetParam(strategy, "MinVolume", minVolume);
				SetParam(strategy, "MaxVolume", maxVolume);
				SetParam(strategy, "MaxSpreadPoints", maxSpread);
				SetParam(strategy, "MoneyRiskMode", riskMode);
				SetParam(strategy, "RiskValue", riskValue);
				SetParam(strategy, "TakeProfitPoints", takeProfit);
				SetParam(strategy, "StopLossPoints", stopLoss);
				SetParam(strategy, "TrailingStartPoints", 0m);
				SetParam(strategy, "TrailingGapPoints", 0m);
				SetParam(strategy, "TradeComment", "RRS-test");
				SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
				recorder.Attach(strategy);
			}, TimeSpan.FromDays(2), postTradeHorizon);

			return recorder;
		}

		var fixedVolume = await Run(0, 0.123m, 0.123m, 1_000_000m, 0, 1_000_000m, assertParameters: true);
		fixedVolume.AssertFirstSide(Sides.Buy);
		fixedVolume.AssertFirstVolume(0.123m);
		fixedVolume.AssertFirstComment("RRS-test");

		var oneSide = await Run(1, 0.123m, 0.123m, 1_000_000m, 0, 1_000_000m);
		oneSide.AssertDiffersFrom(fixedVolume, "Changing Mode did not affect submitted orders.");

		var horizon = TimeSpan.FromHours(12);
		var wideRisk = await Run(0, 100m, 100m, 1_000_000m, 0, 1_000_000m, 0m, 0m, horizon);
		var tightRisk = await Run(0, 100m, 100m, 1_000_000m, 0, 0.01m, 0m, 0m, horizon);
		var percentageRisk = await Run(0, 100m, 100m, 1_000_000m, 1, 0.01m, 0m, 0m, horizon);

		tightRisk.AssertDiffersFrom(wideRisk, "Changing RiskValue did not affect submitted orders.");
		percentageRisk.AssertDiffersFrom(tightRisk, "Changing MoneyRiskMode did not affect submitted orders.");
	}

	[TestMethod]
	[TestCategory("Shard01")]
	public async Task S3801_OrderStabilizationDoesNotLookAhead()
	{
		var recorder = new OrderTraceRecorder();
		DateTime? firstFinishedCandle = null;
		DateTime? firstOrder = null;
		var position = 0m;
		var largestPosition = 0m;

		await Replay("3801_OrderStabilization", (strategy, _) =>
		{
			// A wide pair renewed every other candle keeps positions open across expiries.
			SetParam(strategy, "OrderDistancePoints", 1000m);
			SetParam(strategy, "ExpirationMinutes", 10);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State == CandleStates.Finished && strategy.ProcessState == ProcessStates.Started)
					firstFinishedCandle ??= strategy.CurrentTime;
			};
			strategy.OrderRegistering += order => firstOrder ??= strategy.CurrentTime;
			strategy.Trades.TradeAdded += trade =>
			{
				position += trade.Order.Side == Sides.Buy ? trade.Trade.Volume : -trade.Trade.Volume;
				largestPosition = Math.Max(largestPosition, Math.Abs(position));
			};
			recorder.Attach(strategy);
		});

		recorder.AssertFirstTwoAreOppositeConditionalStops();
		IsTrue(firstFinishedCandle is not null && firstOrder >= firstFinishedCandle,
			$"The first stop pair must wait for the first finished candle: first finished candle {firstFinishedCandle:O}, first order {firstOrder:O}.");
		IsTrue(largestPosition <= 0.1m,
			$"The position must never exceed OrderVolume 0.1, so an expired stop on the side of the open position must not be placed again; largest position {largestPosition}.");
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public Task S4048_BurgExtrapolatorForecast()
		=> Replay("4048_Burg_Extrapolator_Forecast", null, TimeSpan.FromDays(1));

	[TestMethod]
	[TestCategory("Shard07")]
	public async Task S4207_RichKohonenMap()
	{
		// The published MapPath persists the maps between runs; each replay trains from its own empty file.
		var directory = Path.Combine(AppContext.BaseDirectory, "RichKohonenMap", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);

		try
		{
			await Replay("4207_Rich_Kohonen_Map", (strategy, _) => SetParam(strategy, "MapPath", Path.Combine(directory, "rl.bin")));
		}
		finally
		{
			Directory.Delete(directory, true);
		}
	}

	private const string MaMacdAveraging = "3104_MA_MACD_Position_Averaging";

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S3104_DefaultTrailingStopFollowsCloseAndActsFromNextCandle()
	{
		var legs = await ReplayMaMacdAveraging(5, 5);

		IsTrue(legs.Activations > 0 && legs.MovedStopsInsideCandle > 0 && legs.TrailedExits > 0,
			$"The default 5/5 pip trail must start on closes, keep legs whose own candle already reaches the new stop, and close them on a later candle (activations={legs.Activations}, moved stops inside their candle={legs.MovedStopsInsideCandle}, trailed exits={legs.TrailedExits}).");
	}

	[TestMethod]
	[TestCategory("Shard00")]
	[DataRow(5000, 2000)]
	[DataRow(3000, 0)]
	public async Task S3104_TrailingStopTightensOnlyAfterFurtherStepProgress(int trailingStopPips, int trailingStepPips)
	{
		var legs = await ReplayMaMacdAveraging(trailingStopPips, trailingStepPips);

		IsTrue(legs.Activations > 0 && legs.MovedStopsInsideCandle > 0 && legs.TrailedExits > 0 && legs.Retightenings > 0,
			$"The archive must start trails, tighten them again on further progress and close legs on the trailed stop (activations={legs.Activations}, retightenings={legs.Retightenings}, moved stops inside their candle={legs.MovedStopsInsideCandle}, trailed exits={legs.TrailedExits}).");

		if (trailingStepPips > 0)
			IsTrue(legs.HeldBack > 0, $"Progress smaller than the {trailingStepPips}-pip step must leave a trailed stop where it is (held back={legs.HeldBack}).");
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S3104_ZeroTrailingStopLeavesOnlyStopAndTake()
	{
		var legs = await ReplayMaMacdAveraging(0, 5);

		AreEqual(0, legs.Activations, "A zero TrailingStopPips must not move any stop.");
		AreEqual(0, legs.TrailedExits);
		IsTrue(legs.FixedExits > 0, "Legs must still close on their stop-loss or take-profit.");
	}

	private async Task<MaMacdAveragingLegs> ReplayMaMacdAveraging(int trailingStopPips, int trailingStepPips)
	{
		const int stopLossPips = 30000;
		const int takeProfitPips = 30000;

		MaMacdAveragingLegs legs = null;
		var pendingSell = 0m;
		var pendingBuy = 0m;
		var entries = 0;
		var exits = 0;
		var violations = new List<string>();

		await Replay(MaMacdAveraging, (strategy, _) =>
		{
			AreEqual(TimeSpan.FromHours(1).TimeFrame(), strategy.Parameters["CandleType"].Value);
			AreEqual(50, strategy.Parameters["StopLossPips"].Value);
			AreEqual(50, strategy.Parameters["TakeProfitPips"].Value);
			AreEqual(5, strategy.Parameters["TrailingStopPips"].Value);
			AreEqual(5, strategy.Parameters["TrailingStepPips"].Value);
			AreEqual(30, strategy.Parameters["StepLossingPips"].Value);

			// On BTC a 50-pip stop and take lie inside nearly every candle, so every leg would close before any trail.
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "StopLossPips", stopLossPips);
			SetParam(strategy, "TakeProfitPips", takeProfitPips);
			SetParam(strategy, "StepLossingPips", 20000);
			SetParam(strategy, "TrailingStopPips", trailingStopPips);
			SetParam(strategy, "TrailingStepPips", trailingStepPips);

			var priceStep = strategy.Security.PriceStep ?? 0m;

			if (IsPython)
				legs = new MaMacdAveragingLegs<double>(priceStep, stopLossPips, takeProfitPips, trailingStopPips, trailingStepPips);
			else
				legs = new MaMacdAveragingLegs<decimal>(priceStep, stopLossPips, takeProfitPips, trailingStopPips, trailingStepPips);

			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished || strategy.ProcessState != ProcessStates.Started)
					return;

				if (pendingSell != 0m || pendingBuy != 0m)
					violations.Add($"{candle.OpenTime:O}: the previous candle crossed stored levels of {pendingSell} long and {pendingBuy} short volume, yet no exit order followed.");

				(pendingSell, pendingBuy) = legs.Protect(candle);
			};

			strategy.OrderRegistering += order =>
			{
				if (order.Type != OrderTypes.Market)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume} must be a market order.");

				var pending = order.Side == Sides.Sell ? pendingSell : pendingBuy;

				if (pending > 0m)
				{
					if (Math.Abs(order.Volume - pending) > 0.000001m)
						violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume} must close exactly the {pending} held by legs whose stop or take the candle crossed.");

					if (order.Side == Sides.Sell)
						pendingSell = 0m;
					else
						pendingBuy = 0m;

					exits++;
				}
				else if (legs.Side is Sides side && side != order.Side)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume} closes a leg although the candle crossed neither its stop nor its take as they stood before that candle.");
				else
				{
					legs.Enter(order.Side, order.Volume);
					entries++;
				}
			};
		}, TimeSpan.FromDays(31));

		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		IsTrue(entries > 0 && exits > 0, $"The archive must open and close legs (entries={entries}, exits={exits}).");

		return legs;
	}

	private abstract class MaMacdAveragingLegs
	{
		public int Activations { get; protected set; }
		public int Retightenings { get; protected set; }
		public int HeldBack { get; protected set; }
		public int MovedStopsInsideCandle { get; protected set; }
		public int TrailedExits { get; protected set; }
		public int FixedExits { get; protected set; }

		public abstract Sides? Side { get; }

		/// <summary>
		/// Closes the legs whose stop or take the candle crossed as they stood before it, then trails the rest on its close.
		/// </summary>
		/// <returns>Volume to sell for closed long legs and to buy for closed short legs.</returns>
		public abstract (decimal Sell, decimal Buy) Protect(ICandleMessage candle);

		/// <summary>
		/// Adds a leg entered at the close of the candle last passed to <see cref="Protect"/>.
		/// </summary>
		public abstract void Enter(Sides side, decimal volume);
	}

	/// <summary>
	/// The README's per-leg levels computed in the strategy's own number type (decimal in C#, double in Python), so a
	/// price that lands exactly on a level resolves the same way in the model as in the strategy.
	/// </summary>
	private sealed class MaMacdAveragingLegs<T> : MaMacdAveragingLegs
		where T : struct, INumber<T>
	{
		private readonly List<Leg> _legs = [];
		private readonly T _pip;
		private readonly int _stopLossPips;
		private readonly int _takeProfitPips;
		private readonly int _trailingStopPips;
		private readonly int _trailingStepPips;
		private T _close;
		private DateTime _openTime;

		public MaMacdAveragingLegs(decimal priceStep, int stopLossPips, int takeProfitPips, int trailingStopPips, int trailingStepPips)
		{
			_pip = priceStep <= 0m
				? T.CreateChecked(0.0001m)
				: priceStep is 0.00001m or 0.001m ? T.CreateChecked(priceStep) * T.CreateChecked(10) : T.CreateChecked(priceStep);
			_stopLossPips = stopLossPips;
			_takeProfitPips = takeProfitPips;
			_trailingStopPips = trailingStopPips;
			_trailingStepPips = trailingStepPips;
		}

		public override Sides? Side => _legs.Count == 0 ? null : _legs[0].Side;

		public override (decimal Sell, decimal Buy) Protect(ICandleMessage candle)
		{
			_close = T.CreateChecked(candle.ClosePrice);
			_openTime = candle.OpenTime;

			var low = T.CreateChecked(candle.LowPrice);
			var high = T.CreateChecked(candle.HighPrice);
			var sell = 0m;
			var buy = 0m;

			for (var i = _legs.Count - 1; i >= 0; i--)
			{
				var leg = _legs[i];
				if (candle.OpenTime <= leg.EntryTime)
					continue;

				var isLong = leg.Side == Sides.Buy;
				var stopHit = leg.Stop is T stop && (isLong ? low <= stop : high >= stop);
				var takeHit = leg.Take is T take && (isLong ? high >= take : low <= take);

				if (stopHit || takeHit)
				{
					if (isLong)
						sell += leg.Volume;
					else
						buy += leg.Volume;

					if (stopHit && leg.IsTrailed)
						TrailedExits++;
					else
						FixedExits++;

					_legs.RemoveAt(i);
				}
				else if (Trail(leg) && (isLong ? low <= leg.Stop.Value : high >= leg.Stop.Value))
					MovedStopsInsideCandle++;
			}

			return (sell, buy);
		}

		public override void Enter(Sides side, decimal volume)
		{
			var stop = T.CreateChecked(_stopLossPips) * _pip;
			var take = T.CreateChecked(_takeProfitPips) * _pip;
			var isLong = side == Sides.Buy;

			_legs.Add(new()
			{
				Side = side,
				Volume = volume,
				Entry = _close,
				EntryTime = _openTime,
				Stop = _stopLossPips > 0 ? isLong ? _close - stop : _close + stop : null,
				Take = _takeProfitPips > 0 ? isLong ? _close + take : _close - take : null,
			});
		}

		private bool Trail(Leg leg)
		{
			if (_trailingStopPips <= 0)
				return false;

			var trail = T.CreateChecked(_trailingStopPips) * _pip;
			var step = T.CreateChecked(_trailingStepPips) * _pip;
			var isLong = leg.Side == Sides.Buy;

			if ((isLong ? _close - leg.Entry : leg.Entry - _close) < trail + step)
				return false;

			var candidate = isLong ? _close - trail : _close + trail;

			if (leg.Stop is T stop && (isLong ? candidate < stop + step : candidate > stop - step))
			{
				if (leg.IsTrailed && (isLong ? candidate > stop : candidate < stop))
					HeldBack++;

				return false;
			}

			if (!leg.IsTrailed)
				Activations++;
			else if (candidate != leg.Stop.Value)
				Retightenings++;

			leg.Stop = candidate;
			leg.IsTrailed = true;
			return true;
		}

		private sealed class Leg
		{
			public Sides Side { get; init; }
			public decimal Volume { get; init; }
			public T Entry { get; init; }
			public DateTime EntryTime { get; init; }
			public T? Stop { get; set; }
			public T? Take { get; init; }
			public bool IsTrailed { get; set; }
		}
	}

	private const string Lbs = "3114_LBS";

	[TestMethod]
	[TestCategory("Shard02")]
	[DataRow(10, 11, 12)]
	[DataRow(0, 7, 0)]
	public async Task S3114_EntryStopsFollowBarEndingAtTradingHour(int hour1, int hour2, int hour3)
	{
		int[] hours = [hour1, hour2, hour3];
		var trace = await ReplayLbs(hours, null, null, TimeSpan.FromDays(14));

		TestContext.WriteLine($"python={IsPython}, hours={hour1}/{hour2}/{hour3}: {trace}");
		IsTrue(trace.Violations.Count == 0, string.Join(Environment.NewLine, trace.Violations));
		AreEqual(trace.ExpectedPairs, trace.Pairs, "Every flat bar that ends at an enabled trading hour, with quotes known, must place the buy and sell stops.");
		IsTrue(trace.PairHours.SetEquals(hours.Where(hour => hour != 0)), $"Every enabled trading hour must place a pair on the replay, got hours {string.Join(",", trace.PairHours.Order())}.");
		IsTrue(trace.CloseInsideBar > 0, "The replay's bars must report a close time inside the bar, or the fixture cannot tell the bar end from the close time.");
	}

	[TestMethod]
	[TestCategory("Shard02")]
	public async Task S3114_EntryStopsOnlyWhileOnline()
	{
		var offlineFrom = Paths.HistoryBeginDate.AddDays(4);
		var offlineTo = Paths.HistoryBeginDate.AddDays(8);
		var trace = await ReplayLbs([10, 11, 12], offlineFrom, offlineTo, TimeSpan.FromDays(12));

		TestContext.WriteLine($"python={IsPython}, offline={offlineFrom:O}..{offlineTo:O}: {trace}");
		AreEqual(0, trace.OfflineEntries, "README: orders are only sent when the strategy is online.");
		IsTrue(trace.Violations.Count == 0, string.Join(Environment.NewLine, trace.Violations));
		IsTrue(trace.OfflineEligibleBars > 0, "The offline window must contain flat bars ending at a trading hour with quotes known, or it proves nothing.");
		IsTrue(trace.PairsBeforeOffline > 0, "Stop pairs must be placed while the strategy is online, before the window.");
		IsTrue(trace.OnlineRestored, "Once nothing holds it offline, history emulation must report the strategy online again.");
		IsTrue(trace.PairsAfterOffline > 0, "Stop pairs must be placed again once the strategy is back online.");
	}

	private async Task<LbsTrace> ReplayLbs(int[] hours, DateTime? offlineFrom, DateTime? offlineTo, TimeSpan duration)
	{
		const string buyComment = "LBS buy breakout";
		const string sellComment = "LBS sell breakout";

		var frame = TimeSpan.FromHours(1);
		var enabled = hours.Where(hour => hour != 0).ToHashSet();
		var setOnline = typeof(Strategy).GetProperty(nameof(Strategy.IsOnline))?.GetSetMethod(nonPublic: true);
		var refreshOnline = typeof(Strategy).GetMethod("RefreshOnlineState", BindingFlags.Instance | BindingFlags.NonPublic);
		IsNotNull(setOnline, "Strategy.IsOnline must keep a setter the test can use to take the strategy offline.");
		IsNotNull(refreshOnline, "Strategy must keep RefreshOnlineState, which recomputes IsOnline from the subscriptions.");

		var trace = new LbsTrace();
		decimal? bid = null;
		decimal? ask = null;
		var offline = false;
		(DateTime bar, int hour, decimal buy, decimal sell)? expected = null;
		var buyPlaced = false;
		var sellPlaced = false;

		void violate(string text)
		{
			if (trace.Violations.Count < 12)
				trace.Violations.Add(text);
		}

		void settle()
		{
			if (expected is { } pending && !(buyPlaced && sellPlaced))
				violate($"The flat bar opened at {pending.bar:O} ends at {pending.hour:00}:00, an enabled trading hour, with quotes known, but its stop pair was not placed (buy={buyPlaced}, sell={sellPlaced}).");

			expected = null;
			buyPlaced = sellPlaced = false;
		}

		await Replay(Lbs, (strategy, _) =>
		{
			AreEqual(10, strategy.Parameters["Hour1"].Value);
			AreEqual(11, strategy.Parameters["Hour2"].Value);
			AreEqual(12, strategy.Parameters["Hour3"].Value);
			AreEqual(frame.TimeFrame(), strategy.Parameters["CandleType"].Value);
			AreEqual(0, Convert.ToInt32(strategy.Parameters["MoneyMode"].Value), "README: MoneyMode defaults to FixedLot.");
			AreEqual(1m, Convert.ToDecimal(strategy.Parameters["VolumeOrRisk"].Value));
			SetParam(strategy, "Hour1", hours[0]);
			SetParam(strategy, "Hour2", hours[1]);
			SetParam(strategy, "Hour3", hours[2]);

			var security = strategy.Security.Id.ToSecurityId();
			var point = strategy.Security.PriceStep is decimal step && step > 0m ? step : 0.0001m;

			strategy.Level1Received += (_, quote) =>
			{
				// The strategy binds its quotes with BuildField = BestAskPrice, so it only sees updates carrying an ask.
				if (strategy.ProcessState != ProcessStates.Started || quote.SecurityId != security ||
					quote.TryGetDecimal(Level1Fields.BestAskPrice) is not decimal quoteAsk)
					return;

				if (quote.TryGetDecimal(Level1Fields.BestBidPrice) is decimal quoteBid && quoteBid > 0m)
					bid = quoteBid;

				if (quoteAsk > 0m)
					ask = quoteAsk;
			};

			strategy.CandleReceived += (_, candle) =>
			{
				if (strategy.ProcessState != ProcessStates.Started || candle.State != CandleStates.Finished ||
					candle.SecurityId != security || candle.DataType != frame.TimeFrame())
					return;

				settle();
				trace.Bars++;

				var barEnd = candle.OpenTime + frame;
				if (candle.CloseTime.Hour != barEnd.Hour)
					trace.CloseInsideBar++;

				var wasOffline = offline;
				offline = candle.OpenTime >= offlineFrom && candle.OpenTime < offlineTo;

				if (offline)
					setOnline.Invoke(strategy, [false]);
				else if (wasOffline)
				{
					refreshOnline.Invoke(strategy, null);
					trace.OnlineRestored = strategy.IsOnline;
				}

				if (strategy.Position != 0m || !enabled.Contains(barEnd.Hour) || bid is not decimal lastBid || ask is not decimal lastAsk)
					return;

				if (offline)
				{
					trace.OfflineEligibleBars++;
					return;
				}

				if (!strategy.IsFormedAndOnlineAndAllowTrading())
				{
					violate($"History emulation left the strategy unable to trade (online={strategy.IsOnline}) at the flat bar opened {candle.OpenTime:O}, which ends at an enabled trading hour.");
					return;
				}

				var spread = Math.Max(0m, lastAsk - lastBid);
				var buffer = Math.Max(3m * spread, 10m * point);
				expected = (candle.OpenTime, barEnd.Hour,
					strategy.Security.ShrinkPrice(Math.Max(candle.HighPrice, lastAsk + buffer), ShrinkRules.Auto),
					strategy.Security.ShrinkPrice(Math.Min(candle.LowPrice, lastBid - buffer), ShrinkRules.Auto));
				trace.ExpectedPairs++;
			};

			strategy.OrderRegistering += order =>
			{
				var isBuy = order.Comment == buyComment;
				if (!isBuy && order.Comment != sellComment)
					return;

				if (offline)
				{
					trace.OfflineEntries++;
					violate($"{strategy.CurrentTime:O}: the '{order.Comment}' stop was sent while the strategy was not online.");
					return;
				}

				if (expected is not { } pending)
				{
					violate($"{strategy.CurrentTime:O}: the '{order.Comment}' stop was sent although the finished bar does not end at an enabled trading hour ({string.Join("/", enabled)}), a position is open or quotes are missing.");
					return;
				}

				var activation = Convert.ToDecimal(order.Condition.Parameters["ActivationPrice"]);
				var price = isBuy ? pending.buy : pending.sell;
				if (order.Side != (isBuy ? Sides.Buy : Sides.Sell) || order.Type != OrderTypes.Conditional || order.Volume != 1m || activation != price)
					violate($"{strategy.CurrentTime:O}: expected a {(isBuy ? "buy" : "sell")} stop of 1 at {price} after the bar opened {pending.bar:O}, got {order.Side} {order.Type} {order.Volume} at {activation}.");

				if (isBuy ? buyPlaced : sellPlaced)
					violate($"{strategy.CurrentTime:O}: a second '{order.Comment}' stop was sent for the bar opened {pending.bar:O}.");

				if (isBuy)
					buyPlaced = true;
				else
					sellPlaced = true;

				if (!buyPlaced || !sellPlaced)
					return;

				trace.Pairs++;
				trace.PairHours.Add(pending.hour);

				if (pending.bar < offlineFrom)
					trace.PairsBeforeOffline++;
				else if (pending.bar >= offlineTo)
					trace.PairsAfterOffline++;

				expected = null;
				buyPlaced = sellPlaced = false;
			};
		}, duration);

		settle();
		return trace;
	}

	private sealed class LbsTrace
	{
		public List<string> Violations { get; } = [];
		public HashSet<int> PairHours { get; } = [];
		public int Bars { get; set; }
		public int CloseInsideBar { get; set; }
		public int ExpectedPairs { get; set; }
		public int Pairs { get; set; }
		public int PairsBeforeOffline { get; set; }
		public int PairsAfterOffline { get; set; }
		public int OfflineEligibleBars { get; set; }
		public int OfflineEntries { get; set; }
		public bool OnlineRestored { get; set; }

		public override string ToString()
			=> $"bars={Bars}, closeInsideBar={CloseInsideBar}, expectedPairs={ExpectedPairs}, pairs={Pairs}, pairHours={string.Join(",", PairHours.Order())}, " +
				$"pairsBeforeOffline={PairsBeforeOffline}, pairsAfterOffline={PairsAfterOffline}, offlineEligibleBars={OfflineEligibleBars}, offlineEntries={OfflineEntries}, onlineRestored={OnlineRestored}";
	}

	private const string Bruno = "3121_Bruno";

	[TestMethod]
	[TestCategory("Shard01")]
	[DataRow(0.1, 1.6)]
	[DataRow(0.2, 1.5)]
	public async Task S3121_SignalsAddMultipliedVolumeAndCloseOnlyTheOppositePosition(double baseVolume, double multiplier)
	{
		var multiplied = new List<decimal>();
		var step = 0m;
		var orders = 0;
		var entriesFromFlat = 0;
		var stackedEntries = 0;
		var reversals = 0;
		var buys = 0;
		var sells = 0;
		var violations = new List<string>();

		await Replay(Bruno, (strategy, _) =>
		{
			AreEqual(1.6m, Convert.ToDecimal(strategy.Parameters["SignalMultiplier"].Value));
			AreEqual(20m, Convert.ToDecimal(strategy.Parameters["AdxPositiveThreshold"].Value));
			AreEqual(40m, Convert.ToDecimal(strategy.Parameters["AdxNegativeThreshold"].Value));
			AreEqual(8, strategy.Parameters["FastEmaPeriod"].Value);
			AreEqual(21, strategy.Parameters["SlowEmaPeriod"].Value);
			AreEqual(13, strategy.Parameters["MacdFastPeriod"].Value);
			AreEqual(34, strategy.Parameters["MacdSlowPeriod"].Value);
			AreEqual(8, strategy.Parameters["MacdSignalPeriod"].Value);
			AreEqual(21, strategy.Parameters["StochasticPeriod"].Value);
			AreEqual(3, strategy.Parameters["StochasticKsmoothing"].Value);
			AreEqual(3, strategy.Parameters["StochasticDsmoothing"].Value);
			AreEqual(80m, Convert.ToDecimal(strategy.Parameters["StochasticOverbought"].Value));
			AreEqual(20m, Convert.ToDecimal(strategy.Parameters["StochasticOversold"].Value));
			AreEqual(TimeSpan.FromHours(1).TimeFrame(), strategy.Parameters["CandleType"].Value);

			SetParam(strategy, "BaseVolume", Convert.ToDecimal(baseVolume));
			SetParam(strategy, "SignalMultiplier", Convert.ToDecimal(multiplier));

			// Zero disables the protective levels, so every order comes from a signal.
			SetParam(strategy, "StopLossPips", 0);
			SetParam(strategy, "TakeProfitPips", 0);
			SetParam(strategy, "TrailingStopPips", 0);

			step = strategy.Security.VolumeStep ?? 0m;
			IsTrue(step > 0m, "The replayed instrument must state its volume step.");

			// Four filters, so a signal is BaseVolume times SignalMultiplier once to four times, in whole volume steps.
			var volume = Convert.ToDecimal(baseVolume);
			for (var i = 0; i < 4; i++)
			{
				volume *= Convert.ToDecimal(multiplier);
				multiplied.Add(Math.Floor(volume / step) * step);
			}

			strategy.OrderRegistering += order =>
			{
				orders++;
				var position = strategy.Position;
				var opposite = order.Side == Sides.Buy ? Math.Max(0m, -position) : Math.Max(0m, position);
				var added = order.Volume - opposite;

				if (order.Type != OrderTypes.Market || !multiplied.Any(v => Math.Abs(v - added) < step / 2m))
				{
					if (violations.Count < 12)
						violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume} {order.Type} at position {position}. README closes only an opposite position ({opposite}) and adds a market order of the multiplied volume ({string.Join(", ", multiplied)}).");

					return;
				}

				if (position == 0m)
					entriesFromFlat++;
				else if (opposite > 0m)
					reversals++;
				else
					stackedEntries++;

				if (order.Side == Sides.Buy)
					buys++;
				else
					sells++;
			};
		}, TimeSpan.FromDays(31));

		TestContext.WriteLine($"python={IsPython}, base={baseVolume}, multiplier={multiplier}, volumes={string.Join("/", multiplied)}: orders={orders}, fromFlat={entriesFromFlat}, stacked={stackedEntries}, reversals={reversals}, buys={buys}, sells={sells}");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));
		IsTrue(stackedEntries > 0, "The real archive must reach a signal on the side already held, which adds only the multiplied volume.");
		IsTrue(reversals > 0, "The real archive must reach an opposite signal, which closes the held position and opens the multiplied volume.");
		IsTrue(buys > 0 && sells > 0, "The real archive must reach both long and short signals.");
	}

	private const string Vlado = "3122_Vlado";

	private readonly record struct VladoSignalCounts(int Orders, int Longs, int Shorts, int Reversals, int AtLevel);

	[TestMethod]
	[TestCategory("Shard02")]
	public async Task S3122_DefaultLevelsReverseOnHourlyWilliamsR()
	{
		var counts = await ReplayVladoSignals(-75m, -25m, TimeSpan.FromHours(1), TimeSpan.FromDays(31));

		IsTrue(counts.Longs > 0 && counts.Shorts > 0 && counts.Reversals > 0, "The archive must reach both default levels and reverse a held position with one order.");
	}

	[TestMethod]
	[TestCategory("Shard02")]
	public async Task S3122_ReadingEqualToALevelEntersAPosition()
	{
		// %R never leaves [-100, 0], so at these levels only a reading exactly on a level can signal.
		var counts = await ReplayVladoSignals(-100m, 0m, TimeSpan.FromMinutes(1), TimeSpan.FromDays(5));

		AreEqual(counts.Orders, counts.AtLevel);
		IsTrue(counts.Longs > 0 && counts.Shorts > 0, "README enters long at %R <= OversoldLevel and short at %R >= OverboughtLevel, so a reading equal to a level must trade in both directions.");
	}

	[TestMethod]
	[TestCategory("Shard02")]
	public async Task S3122_ChartShowsCandlesAndTradesAboveAWilliamsRPane()
	{
		var chart = new ChartRecorder();
		var bars = new Queue<ICandleMessage>();
		var finished = new HashSet<DateTime>();
		var williams = new Dictionary<DateTime, decimal>();

		await Replay(Vlado, (strategy, _) =>
		{
			strategy.SetChart(chart.Chart);

			var period = Convert.ToInt32(strategy.Parameters["WilliamsPeriod"].Value);

			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				finished.Add(candle.OpenTime);

				if (VladoWilliamsR(bars, candle, period) is decimal value)
					williams[candle.OpenTime] = value;
			};
		}, TimeSpan.FromDays(31));

		var areas = chart.Areas;
		AreEqual(2, areas.Length, "README: the main area shows candles and trades, a secondary pane plots Williams %R.");

		var main = chart.ElementsOf(areas[0]);
		var candles = main.OfType<IChartCandleElement>().ToArray();
		var trades = main.OfType<IChartTradeElement>().ToArray();
		AreEqual(1, candles.Length, "The main area must show the candles.");
		AreEqual(1, trades.Length, "The main area must show the strategy's trades.");
		IsFalse(main.OfType<IChartIndicatorElement>().Any(), "Williams %R belongs to its own pane, not over the candles.");

		var pane = chart.ElementsOf(areas[1]);
		AreEqual(1, pane.Length, "The secondary pane plots Williams %R alone.");
		var line = pane[0] as IChartIndicatorElement;
		IsNotNull(line, "The secondary pane must plot an indicator.");

		var drawnCandles = chart.DrawnOn(candles[0]);
		AreEqual(finished.Count, drawnCandles.Length, "Every finished candle must be drawn in the main area.");
		IsTrue(drawnCandles.All(v => finished.Contains(v.Time)));
		IsTrue(chart.DrawnOn(trades[0]).Length > 0, "The strategy's trades must be drawn over the candles.");

		var plotted = chart.DrawnOn(line)
			.Where(v => williams.ContainsKey(v.Time))
			.Select(v => (v.Time, Value: (IIndicatorValue)v.Values[0]))
			.ToArray();

		var mismatches = plotted
			.Where(p => p.Value.IsEmpty || Math.Abs(p.Value.GetValue<decimal>() - williams[p.Time]) > 0.000001m)
			.Take(12)
			.Select(p => $"{p.Time:O}: plotted {(p.Value.IsEmpty ? "nothing" : p.Value.GetValue<decimal>().ToString())}, Williams %R {williams[p.Time]}")
			.ToArray();

		TestContext.WriteLine($"python={IsPython}: areas={areas.Length}, candles={drawnCandles.Length}, trades={chart.DrawnOn(trades[0]).Length}, williams={williams.Count}, plotted={plotted.Length}");
		IsTrue(williams.Count > 100);
		AreEqual(williams.Count, plotted.Length, "Every bar with a complete WilliamsPeriod window must be plotted in the pane.");
		IsTrue(mismatches.Length == 0, string.Join(Environment.NewLine, mismatches));
	}

	private async Task<VladoSignalCounts> ReplayVladoSignals(decimal oversold, decimal overbought, TimeSpan frame, TimeSpan duration)
	{
		var bars = new Queue<ICandleMessage>();
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var longs = 0;
		var shorts = 0;
		var reversals = 0;
		var atLevel = 0;
		var violations = new List<string>();

		await Replay(Vlado, (strategy, _) =>
		{
			AreEqual(-75m, Convert.ToDecimal(strategy.Parameters["OversoldLevel"].Value));
			AreEqual(-25m, Convert.ToDecimal(strategy.Parameters["OverboughtLevel"].Value));
			AreEqual(TimeSpan.FromHours(1).TimeFrame(), strategy.Parameters["CandleType"].Value);

			SetParam(strategy, "OversoldLevel", oversold);
			SetParam(strategy, "OverboughtLevel", overbought);
			SetParam(strategy, "CandleType", frame.TimeFrame());
			strategy.Volume = 2m;

			var period = Convert.ToInt32(strategy.Parameters["WilliamsPeriod"].Value);

			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				expectedSide = null;

				if (VladoWilliamsR(bars, candle, period) is not decimal value)
					return;

				var position = strategy.Position;

				if (value <= oversold && position <= 0m)
				{
					expectedSide = Sides.Buy;
					longs++;
				}
				else if (value >= overbought && position >= 0m)
				{
					expectedSide = Sides.Sell;
					shorts++;
				}
				else
					return;

				expectedVolume = strategy.Volume + Math.Abs(position);
				expectedOrders++;

				if (position != 0m)
					reversals++;

				if (value == oversold || value == overbought)
					atLevel++;
			};

			strategy.OrderRegistering += order =>
			{
				actualOrders++;

				if ((order.Type != OrderTypes.Market || order.Side != expectedSide || order.Volume != expectedVolume) && violations.Count < 12)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Type} {order.Volume}, expected {expectedSide?.ToString() ?? "no order"} {expectedVolume}. README goes long at %R <= OversoldLevel when flat or short, short at %R >= OverboughtLevel when flat or long, with one market order of Volume + |Position|.");

				expectedSide = null;
			};
		}, duration);

		TestContext.WriteLine($"python={IsPython}, levels={oversold}/{overbought}, frame={frame}: expected={expectedOrders}, actual={actualOrders}, longs={longs}, shorts={shorts}, reversals={reversals}, atLevel={atLevel}");
		AreEqual(expectedOrders, actualOrders, "Every reading at or beyond a level must act on a flat or opposite position, and nothing else may trade.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));

		return new(expectedOrders, longs, shorts, reversals, atLevel);
	}

	/// <summary>
	/// Williams %R of the candle over the last <paramref name="period"/> finished candles, the current
	/// one included; <see langword="null"/> until the window is full or when its range is flat.
	/// </summary>
	private static decimal? VladoWilliamsR(Queue<ICandleMessage> bars, ICandleMessage candle, int period)
	{
		bars.Enqueue(candle);

		if (bars.Count > period)
			bars.Dequeue();

		if (bars.Count < period)
			return null;

		var high = bars.Max(bar => bar.HighPrice);
		var low = bars.Min(bar => bar.LowPrice);

		return high == low ? null : -100m * (high - candle.ClosePrice) / (high - low);
	}

	private const string RiskReward = "3206_Risk_Reward_Ratio";

	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S3206_DefaultsTradeTheReadmeIndicatorConfluence()
	{
		var model = await ReplayRiskReward(strategy =>
		{
			AreEqual(TimeSpan.FromMinutes(15).TimeFrame(), strategy.Parameters["CandleType"].Value);
			AreEqual(0.1m, Convert.ToDecimal(strategy.Parameters["TradeVolume"].Value));
			AreEqual(6, Convert.ToInt32(strategy.Parameters["FastMaPeriod"].Value));
			AreEqual(85, Convert.ToInt32(strategy.Parameters["SlowMaPeriod"].Value));
			AreEqual(0.3m, Convert.ToDecimal(strategy.Parameters["MomentumThreshold"].Value));
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["RewardRatio"].Value));
			AreEqual(20, Convert.ToInt32(strategy.Parameters["StopLossPips"].Value));
			AreEqual(10, Convert.ToInt32(strategy.Parameters["MaxPositions"].Value));
			AreEqual(true, (bool)strategy.Parameters["EnableTrailing"].Value);
			AreEqual(40, Convert.ToInt32(strategy.Parameters["TrailingStopPips"].Value));
			AreEqual(true, (bool)strategy.Parameters["EnableBreakEven"].Value);
			AreEqual(30, Convert.ToInt32(strategy.Parameters["BreakEvenTriggerPips"].Value));
			AreEqual(30, Convert.ToInt32(strategy.Parameters["BreakEvenOffsetPips"].Value));
			AreEqual(false, (bool)strategy.Parameters["ExitSwitch"].Value);
		});

		IsTrue(model.Entries > 0 && model.Exits > 0, $"The README defaults must open and close legs on the archive (entries={model.Entries}, exits={model.Exits}).");
		IsTrue(model.StochasticDecisive > 0,
			$"The archive must contain candidate bars where the slowed %K(5, 2) against the %D(21, 10, 4) decides otherwise than the raw %K(5) against an unslowed %D(21, 10) (decisive bars={model.StochasticDecisive}).");
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S3206_EachEntryKeepsStopAndTakeFromItsOwnFill()
	{
		var model = await ReplayRiskReward(strategy =>
		{
			// A 400-dollar stop keeps several BTC legs open at once, each from its own fill.
			strategy.Security.PriceStep = 0.01m;
			SetRiskReward(strategy, stopLossPips: 40000, rewardRatio: 2m, isTrailing: false, trailingStopPips: 0, isBreakEven: false, breakEvenTriggerPips: 0, breakEvenOffsetPips: 0);
		});

		TestContext.WriteLine($"legs held at once={model.MaxFilledLegs}, fills away from the signal close={model.FillsAwayFromSignalClose}, exits leaving other legs open={model.SeparateLegExits}.");

		IsTrue(model.MaxFilledLegs >= 2, $"Repeated signals must pyramid into several filled legs (most held at once={model.MaxFilledLegs}).");
		IsTrue(model.SeparateLegExits > 0, $"A leg whose own stop or take is crossed must close alone while the others stay open (such exits={model.SeparateLegExits}).");
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S3206_TrailingStartsOnlyAfterTheTrailingDistance()
	{
		var model = await ReplayRiskReward(strategy =>
		{
			// A 300-dollar stop lies closer than a 400-dollar trail as soon as a leg is 100 dollars in profit.
			strategy.Security.PriceStep = 0.01m;
			SetRiskReward(strategy, stopLossPips: 30000, rewardRatio: 3m, isTrailing: true, trailingStopPips: 40000, isBreakEven: false, breakEvenTriggerPips: 0, breakEvenOffsetPips: 0);
		});

		IsTrue(model.TrailingHeldBack > 0,
			$"The archive must contain legs between 100 and 400 dollars in profit, whose stop stays put until the advance reaches the trailing distance (held back={model.TrailingHeldBack}).");
		IsTrue(model.TrailingActivations > 0 && model.TrailedExits > 0,
			$"Legs that advance by the trailing distance must start trailing and close on the trailed stop (activations={model.TrailingActivations}, trailed exits={model.TrailedExits}).");
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S3206_BreakEvenMovesTheStopToTheFillPlusOffset()
	{
		var model = await ReplayRiskReward(strategy =>
		{
			strategy.Security.PriceStep = 0.01m;
			SetRiskReward(strategy, stopLossPips: 30000, rewardRatio: 3m, isTrailing: false, trailingStopPips: 40000, isBreakEven: true, breakEvenTriggerPips: 25000, breakEvenOffsetPips: 5000);
		});

		IsTrue(model.BreakEvenMoves > 0 && model.BreakEvenExits > 0,
			$"Legs 250 dollars in profit must move their stop to 50 dollars past their own fill and close there on a pullback (moves={model.BreakEvenMoves}, break-even exits={model.BreakEvenExits}).");
	}

	[TestMethod]
	[TestCategory("Shard06")]
	[DataRow(0.001)]
	[DataRow(0.00001)]
	public async Task S3206_PipIsOnePriceStep(double priceStep)
	{
		var step = (decimal)priceStep;

		var model = await ReplayRiskReward(strategy =>
		{
			// Three- and five-digit steps are where a MetaTrader-style pip would be ten steps.
			strategy.Security.PriceStep = step;
			SetRiskReward(strategy, stopLossPips: (int)(200m / step), rewardRatio: 2m, isTrailing: false, trailingStopPips: 0, isBreakEven: false, breakEvenTriggerPips: 0, breakEvenOffsetPips: 0);
		});

		IsTrue(model.Exits > 0, $"A 200-dollar stop and 400-dollar take counted in steps of {step} must close legs on the archive (exits={model.Exits}).");
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S3206_MissingPriceStepLeavesRiskDistancesDisabled()
	{
		var model = await ReplayRiskReward(strategy => strategy.Security.PriceStep = null);

		AreEqual(0, model.CandlesWithPriceStep, "The replay must run the instrument without a price step.");
		IsTrue(model.Entries > 0, $"Entries must still follow the signals (entries={model.Entries}).");
		AreEqual(0, model.Exits, "Without a price step no stop, take, break-even or trail exists, so no leg may be closed.");
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S3206_ExitSwitchFlattensAndForgetsEveryLeg()
	{
		var candles = 0;
		var switchedOn = 0;

		var model = await ReplayRiskReward(strategy =>
		{
			strategy.Security.PriceStep = 0.01m;
			SetRiskReward(strategy, stopLossPips: 40000, rewardRatio: 2m, isTrailing: false, trailingStopPips: 0, isBreakEven: false, breakEvenTriggerPips: 0, breakEvenOffsetPips: 0);
		}, (strategy, current) =>
		{
			candles++;

			if (switchedOn == 0 && current.FilledLegs >= 2 && strategy.Position != 0m)
			{
				SetParam(strategy, "ExitSwitch", true);
				switchedOn = candles;
			}
			else if (switchedOn > 0 && candles == switchedOn + 4)
				SetParam(strategy, "ExitSwitch", false);
		});

		IsTrue(switchedOn > 0, "The archive must reach a moment with at least two filled legs to switch the exit on.");
		IsTrue(model.Flattens > 0, $"The exit switch must flatten the position on the next completed bar (flattens={model.Flattens}).");
		IsTrue(model.EntriesAfterFlatten > 0, $"Trading must resume with fresh legs once the switch is off (entries after the flatten={model.EntriesAfterFlatten}).");
	}

	private static void SetRiskReward(Strategy strategy, int stopLossPips, decimal rewardRatio, bool isTrailing, int trailingStopPips, bool isBreakEven, int breakEvenTriggerPips, int breakEvenOffsetPips)
	{
		SetParam(strategy, "StopLossPips", stopLossPips);
		SetParam(strategy, "RewardRatio", rewardRatio);
		SetParam(strategy, "EnableTrailing", isTrailing);
		SetParam(strategy, "TrailingStopPips", trailingStopPips);
		SetParam(strategy, "EnableBreakEven", isBreakEven);
		SetParam(strategy, "BreakEvenTriggerPips", breakEvenTriggerPips);
		SetParam(strategy, "BreakEvenOffsetPips", breakEvenOffsetPips);
	}

	/// <summary>
	/// Replays the strategy next to <see cref="RiskRewardModel"/> and fails on the first order the README would not send.
	/// </summary>
	/// <param name="configure">Sets the parameters and the instrument before the model reads them.</param>
	/// <param name="beforeCandle">Runs on every finished candle before the model and the strategy see it.</param>
	/// <returns>The model with its counters after the replay.</returns>
	private async Task<RiskRewardModel> ReplayRiskReward(Action<Strategy> configure, Action<Strategy, RiskRewardModel> beforeCandle = null)
	{
		RiskRewardModel model = null;

		await Replay(RiskReward, (strategy, _) =>
		{
			configure(strategy);

			model = IsPython ? new RiskRewardModel<double>(strategy) : new RiskRewardModel<decimal>(strategy);

			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished || strategy.ProcessState != ProcessStates.Started)
					return;

				beforeCandle?.Invoke(strategy, model);
				model.OnCandle(candle, strategy.Position, (bool)strategy.Parameters["ExitSwitch"].Value, strategy.Security.PriceStep);
			};

			strategy.OrderRegistering += model.OnOrder;
			strategy.OwnTradeReceived += (_, trade) => model.OnTrade(trade, strategy.Security.PriceStep);
		}, TimeSpan.FromDays(31));

		IsTrue(model.Violations.Count == 0, string.Join(Environment.NewLine, model.Violations.Take(12)));

		return model;
	}

	private abstract class RiskRewardModel
	{
		public List<string> Violations { get; } = [];
		public int Entries { get; protected set; }
		public int Exits { get; protected set; }
		public int Flattens { get; protected set; }
		public int EntriesAfterFlatten { get; protected set; }
		public int MaxFilledLegs { get; protected set; }
		public int FillsAwayFromSignalClose { get; protected set; }
		public int SeparateLegExits { get; protected set; }
		public int TrailingActivations { get; protected set; }
		public int TrailingHeldBack { get; protected set; }
		public int TrailedExits { get; protected set; }
		public int BreakEvenMoves { get; protected set; }
		public int BreakEvenExits { get; protected set; }
		public int StochasticDecisive { get; protected set; }
		public int CandlesWithPriceStep { get; protected set; }

		public abstract int FilledLegs { get; }

		/// <summary>
		/// Works out the orders the README requires on a finished candle, as the strategy sees it right after.
		/// </summary>
		public abstract void OnCandle(ICandleMessage candle, decimal position, bool isExitSwitch, decimal? priceStep);

		/// <summary>
		/// Matches an order the strategy registers against the ones the last candle required.
		/// </summary>
		public abstract void OnOrder(Order order);

		/// <summary>
		/// Moves an entry leg to the volume-weighted price of its own trades and places its stop and take there.
		/// </summary>
		public abstract void OnTrade(MyTrade trade, decimal? priceStep);
	}

	private enum RiskRewardOrderPurposes
	{
		Entry,
		Exit,
		Flatten,
	}

	private enum RiskRewardStopSources
	{
		Initial,
		BreakEven,
		Trailing,
	}

	/// <summary>
	/// The README's signals and per-leg risk rules computed in the strategy's own number type (decimal in C#, double in
	/// Python), so a value that lands exactly on a threshold resolves the same way in the model as in the strategy.
	/// </summary>
	private sealed class RiskRewardModel<T> : RiskRewardModel
		where T : struct, INumber<T>
	{
		private readonly List<Leg> _legs = [];
		private readonly List<Expectation> _expected = [];
		private readonly List<T> _momentumDistances = [];

		private readonly WeightedMovingAverage _fastLwma;
		private readonly WeightedMovingAverage _slowLwma;
		private readonly RelativeStrengthIndex _rsi = new() { Length = 14 };
		private readonly MovingAverageConvergenceDivergenceSignal _macd = new()
		{
			Macd =
			{
				ShortMa = { Length = 12 },
				LongMa = { Length = 26 },
			},
			SignalMa = { Length = 9 },
		};
		private readonly RateOfChange _momentum = new() { Length = 14 };
		private readonly StochasticK _fastK = new() { Length = 5 };
		private readonly StochasticK _slowK = new() { Length = 21 };
		private readonly SimpleMovingAverage _fastMain = new() { Length = 2 };
		private readonly SimpleMovingAverage _slowMain = new() { Length = 4 };
		private readonly SimpleMovingAverage _slowSignal = new() { Length = 10 };
		private readonly SimpleMovingAverage _unslowedSlowSignal = new() { Length = 10 };

		private readonly decimal _volume;
		private readonly T _volumeNumber;
		private readonly int _maxPositions;
		private readonly T _threshold;
		private readonly T _rewardRatio;
		private readonly int _stopLossPips;
		private readonly bool _isTrailing;
		private readonly int _trailingStopPips;
		private readonly bool _isBreakEven;
		private readonly int _breakEvenTriggerPips;
		private readonly int _breakEvenOffsetPips;

		private DateTime _time;

		public RiskRewardModel(Strategy strategy)
		{
			object param(string name) => strategy.Parameters[name].Value;

			_volume = Convert.ToDecimal(param("TradeVolume"));
			_volumeNumber = Number(param("TradeVolume"));
			_maxPositions = Convert.ToInt32(param("MaxPositions"));
			_threshold = Number(param("MomentumThreshold"));
			_rewardRatio = Number(param("RewardRatio"));
			_stopLossPips = Convert.ToInt32(param("StopLossPips"));
			_isTrailing = (bool)param("EnableTrailing");
			_trailingStopPips = Convert.ToInt32(param("TrailingStopPips"));
			_isBreakEven = (bool)param("EnableBreakEven");
			_breakEvenTriggerPips = Convert.ToInt32(param("BreakEvenTriggerPips"));
			_breakEvenOffsetPips = Convert.ToInt32(param("BreakEvenOffsetPips"));
			_fastLwma = new() { Length = Convert.ToInt32(param("FastMaPeriod")) };
			_slowLwma = new() { Length = Convert.ToInt32(param("SlowMaPeriod")) };
		}

		public override int FilledLegs => _legs.Count(l => l.Fill is not null);

		public override void OnCandle(ICandleMessage candle, decimal position, bool isExitSwitch, decimal? priceStep)
		{
			if (_expected.Count > 0)
			{
				Violations.Add($"{_time:O}: the candle required {string.Join(", ", _expected.Select(e => $"{e.Purpose} {e.Side} {e.Volume}"))}, yet the strategy did not send it.");
				_expected.Clear();
			}

			_time = candle.OpenTime;

			if (priceStep is not null)
				CandlesWithPriceStep++;

			var fastLwma = _fastLwma.Process(candle);
			var slowLwma = _slowLwma.Process(candle);
			var rsi = _rsi.Process(candle);
			var macd = (IMovingAverageConvergenceDivergenceSignalValue)_macd.Process(candle);
			var momentum = _momentum.Process(candle);
			var fastK = _fastK.Process(candle);
			var slowK = _slowK.Process(candle);

			var fastMain = FastMain(fastK);
			var slowSignal = SlowSignal(slowK);
			var unslowedSlowSignal = UnslowedSlowSignal(slowK);

			if (_momentum.IsFormed && !momentum.IsEmpty)
			{
				_momentumDistances.Add(T.Abs(Number(momentum.ToDecimal())));

				if (_momentumDistances.Count > 3)
					_momentumDistances.RemoveAt(0);
			}

			if (isExitSwitch)
			{
				if (position != 0m)
					Expect(position > 0m ? Sides.Sell : Sides.Buy, Math.Abs(position), RiskRewardOrderPurposes.Flatten, default, default);

				_legs.Clear();
				return;
			}

			if (Protect(candle, priceStep))
				return;

			if (fastMain is not T fastLine || slowSignal is not T slowLine)
				return;

			if (!_fastLwma.IsFormed || !_slowLwma.IsFormed || !_rsi.IsFormed || !_macd.IsFormed || !_momentum.IsFormed)
				return;

			if (macd.Macd is not decimal macdMainValue || macd.Signal is not decimal macdSignalValue)
				return;

			var macdMain = Number(macdMainValue);
			var macdSignal = Number(macdSignalValue);
			var rsiValue = Number(rsi.ToDecimal());
			var fast = Number(fastLwma.ToDecimal());
			var slow = Number(slowLwma.ToDecimal());
			var burst = _momentumDistances.Count == 0 ? T.Zero : _momentumDistances.Max();
			var fifty = T.CreateChecked(50);

			var longFilters = rsiValue > fifty && fast > slow && macdMain > macdSignal && macdMain > T.Zero && burst >= _threshold;
			var shortFilters = rsiValue < fifty && fast < slow && macdMain < macdSignal && macdMain < T.Zero && burst >= _threshold;
			var longSignal = fastLine > slowLine && longFilters;
			var shortSignal = fastLine < slowLine && shortFilters;

			if (unslowedSlowSignal is T unslowedLine && !fastK.IsEmpty)
			{
				var rawK = Number(fastK.ToDecimal());

				if ((longFilters && (fastLine > slowLine) != (rawK > unslowedLine)) || (shortFilters && (fastLine < slowLine) != (rawK < unslowedLine)))
					StochasticDecisive++;
			}

			var current = Number(position);
			var maxExposure = T.CreateChecked(_maxPositions) * _volumeNumber;

			if (longSignal && position >= 0m && current + _volumeNumber <= maxExposure)
				Expect(Sides.Buy, _volume, RiskRewardOrderPurposes.Entry, candle.OpenTime, Number(candle.ClosePrice));
			else if (shortSignal && position <= 0m && T.Abs(current - _volumeNumber) <= maxExposure)
				Expect(Sides.Sell, _volume, RiskRewardOrderPurposes.Entry, candle.OpenTime, Number(candle.ClosePrice));
		}

		public override void OnOrder(Order order)
		{
			if (_expected.Count == 0)
			{
				Violations.Add($"{_time:O}: {order.Side} {order.Volume} was sent although the candle required no order.");
				return;
			}

			var expected = _expected[0];
			_expected.RemoveAt(0);

			if (order.Type != OrderTypes.Market || order.Side != expected.Side || Math.Abs(order.Volume - expected.Volume) > 0.000001m)
				Violations.Add($"{_time:O}: the candle required a market {expected.Purpose} {expected.Side} {expected.Volume}, the strategy sent {order.Type} {order.Side} {order.Volume}.");

			switch (expected.Purpose)
			{
				case RiskRewardOrderPurposes.Entry:
					_legs.Add(new()
					{
						Order = order,
						Side = expected.Side,
						EntryTime = expected.EntryTime,
						SignalClose = expected.SignalClose,
					});

					Entries++;

					if (Flattens > 0)
						EntriesAfterFlatten++;

					break;

				case RiskRewardOrderPurposes.Exit:
					Exits++;
					break;

				case RiskRewardOrderPurposes.Flatten:
					Flattens++;
					break;
			}
		}

		public override void OnTrade(MyTrade trade, decimal? priceStep)
		{
			if (trade?.Order is null || trade.Trade is null)
				return;

			var leg = _legs.FirstOrDefault(l => l.Order == trade.Order);
			if (leg is null)
				return;

			var price = Number(trade.Trade.Price);
			var volume = Number(trade.Trade.Volume);

			leg.Volume += trade.Trade.Volume;
			leg.FilledVolume += volume;
			leg.FilledValue += price * volume;

			var fill = leg.FilledValue / leg.FilledVolume;

			if (leg.Fill is null && fill != leg.SignalClose)
				FillsAwayFromSignalClose++;

			leg.Fill = fill;
			leg.Best = fill;
			leg.StopSource = RiskRewardStopSources.Initial;

			if (priceStep > 0m)
			{
				var stopDistance = T.CreateChecked(_stopLossPips) * Number(priceStep.Value);
				var takeDistance = stopDistance * _rewardRatio;
				var isLong = leg.Side == Sides.Buy;

				leg.Stop = isLong ? fill - stopDistance : fill + stopDistance;
				leg.Take = isLong ? fill + takeDistance : fill - takeDistance;
			}
			else
			{
				leg.Stop = null;
				leg.Take = null;
			}

			MaxFilledLegs = Math.Max(MaxFilledLegs, FilledLegs);
		}

		private T? FastMain(IIndicatorValue fastK)
		{
			if (!_fastK.IsFormed || fastK.IsEmpty)
				return null;

			var main = _fastMain.Process(fastK);
			return _fastMain.IsFormed && !main.IsEmpty ? Number(main.ToDecimal()) : null;
		}

		private T? SlowSignal(IIndicatorValue slowK)
		{
			if (!_slowK.IsFormed || slowK.IsEmpty)
				return null;

			var main = _slowMain.Process(slowK);
			if (!_slowMain.IsFormed || main.IsEmpty)
				return null;

			var signal = _slowSignal.Process(main);
			return _slowSignal.IsFormed && !signal.IsEmpty ? Number(signal.ToDecimal()) : null;
		}

		private T? UnslowedSlowSignal(IIndicatorValue slowK)
		{
			if (!_slowK.IsFormed || slowK.IsEmpty)
				return null;

			var signal = _unslowedSlowSignal.Process(slowK);
			return _unslowedSlowSignal.IsFormed && !signal.IsEmpty ? Number(signal.ToDecimal()) : null;
		}

		private bool Protect(ICandleMessage candle, decimal? priceStep)
		{
			T? pip = priceStep > 0m ? Number(priceStep.Value) : null;
			var high = Number(candle.HighPrice);
			var low = Number(candle.LowPrice);
			var sell = 0m;
			var buy = 0m;

			for (var i = _legs.Count - 1; i >= 0; i--)
			{
				var leg = _legs[i];
				if (leg.Fill is not T fill || candle.OpenTime <= leg.EntryTime)
					continue;

				var isLong = leg.Side == Sides.Buy;
				leg.Best = isLong ? T.Max(leg.Best, high) : T.Min(leg.Best, low);

				if (pip is T step)
					MoveStop(leg, fill, step);

				var stopHit = leg.Stop is T stop && (isLong ? low <= stop : high >= stop);
				var takeHit = leg.Take is T take && (isLong ? high >= take : low <= take);

				if (!stopHit && !takeHit)
					continue;

				if (stopHit && leg.StopSource == RiskRewardStopSources.Trailing)
					TrailedExits++;
				else if (stopHit && leg.StopSource == RiskRewardStopSources.BreakEven)
					BreakEvenExits++;

				if (isLong)
					sell += leg.Volume;
				else
					buy += leg.Volume;

				_legs.RemoveAt(i);
			}

			if (sell > 0m)
				Expect(Sides.Sell, sell, RiskRewardOrderPurposes.Exit, default, default);

			if (buy > 0m)
				Expect(Sides.Buy, buy, RiskRewardOrderPurposes.Exit, default, default);

			if (sell == 0m && buy == 0m)
				return false;

			if (FilledLegs > 0)
				SeparateLegExits++;

			return true;
		}

		private void MoveStop(Leg leg, T fill, T pip)
		{
			var isLong = leg.Side == Sides.Buy;
			var advance = isLong ? leg.Best - fill : fill - leg.Best;

			if (_isBreakEven && advance >= T.CreateChecked(_breakEvenTriggerPips) * pip)
			{
				var offset = T.CreateChecked(_breakEvenOffsetPips) * pip;

				if (Tighten(leg, isLong ? fill + offset : fill - offset))
				{
					leg.StopSource = RiskRewardStopSources.BreakEven;
					BreakEvenMoves++;
				}
			}

			if (_isTrailing && _trailingStopPips > 0)
			{
				var trail = T.CreateChecked(_trailingStopPips) * pip;
				var candidate = isLong ? leg.Best - trail : leg.Best + trail;

				if (advance >= trail)
				{
					if (Tighten(leg, candidate))
					{
						if (leg.StopSource != RiskRewardStopSources.Trailing)
							TrailingActivations++;

						leg.StopSource = RiskRewardStopSources.Trailing;
					}
				}
				else if (IsTighter(leg, candidate))
					TrailingHeldBack++;
			}
		}

		private static bool IsTighter(Leg leg, T candidate)
			=> leg.Stop is not T stop || (leg.Side == Sides.Buy ? candidate > stop : candidate < stop);

		private static bool Tighten(Leg leg, T candidate)
		{
			if (!IsTighter(leg, candidate))
				return false;

			leg.Stop = candidate;
			return true;
		}

		private void Expect(Sides side, decimal volume, RiskRewardOrderPurposes purpose, DateTime entryTime, T signalClose)
			=> _expected.Add(new(side, volume, purpose, entryTime, signalClose));

		private static T Number(object value) => value switch
		{
			double d => T.CreateChecked(d),
			decimal m => T.CreateChecked(m),
			int i => T.CreateChecked(i),
			_ => T.CreateChecked(Convert.ToDecimal(value)),
		};

		private readonly record struct Expectation(Sides Side, decimal Volume, RiskRewardOrderPurposes Purpose, DateTime EntryTime, T SignalClose);

		private sealed class Leg
		{
			public Order Order { get; init; }
			public Sides Side { get; init; }
			public DateTime EntryTime { get; init; }
			public T SignalClose { get; init; }
			public decimal Volume { get; set; }
			public T FilledVolume { get; set; }
			public T FilledValue { get; set; }
			public T? Fill { get; set; }
			public T Best { get; set; }
			public T? Stop { get; set; }
			public T? Take { get; set; }
			public RiskRewardStopSources StopSource { get; set; }
		}
	}

	private const string CryptoAnalysis = "3301_Crypto_Analysis";

	private static readonly string[] _cryptoAnalysisParameters =
	[
		"OrderVolume", "UseMoneyTakeProfit", "MoneyTakeProfit", "UsePercentTakeProfit", "PercentTakeProfit",
		"EnableMoneyTrailing", "MoneyTrailTarget", "MoneyTrailStop", "StopLossPips", "TakeProfitPips", "TrailingStopPips",
		"UseBreakEven", "BreakEvenTriggerPips", "BreakEvenOffsetPips", "FastMaPeriod", "SlowMaPeriod", "MomentumPeriod",
		"MomentumBuyThreshold", "MomentumSellThreshold", "MacdFastLength", "MacdSlowLength", "MacdSignalLength",
		"UseEquityStop", "EquityRiskPercent", "CandleType", "MomentumCandleType", "MacdCandleType",
	];

	/// <summary>
	/// A wide stop and a tight trail that keep a position open long enough for the trail to act.
	/// </summary>
	private static readonly (string Name, object Value)[] _cryptoAnalysisTrailing =
	[
		("StopLossPips", 20000),
		("TakeProfitPips", 0),
		("TrailingStopPips", 500),
		("UseBreakEven", false),
	];

	/// <summary>
	/// Independent model of the README: the Bollinger/LWMA/RSI entry confirmed by the momentum burst and the MACD
	/// filter, and the protection block on closed candles. It gives the order every finished signal candle must
	/// produce and counts the candles where a rule the README rules out would have traded differently.
	/// </summary>
	private sealed class CryptoAnalysisModel
	{
		private const int _rsiLength = 14;
		private const int _bandLength = 20;
		private const decimal _bandWidth = 2m;

		private readonly RelativeStrengthIndex _rsi = new() { Length = _rsiLength };
		private readonly List<(decimal Open, decimal High, decimal Low, decimal Close)> _bars = [];
		private readonly List<decimal> _momentumCloses = [];
		private readonly Queue<decimal> _deviations = new();

		private readonly decimal _orderVolume;
		private readonly bool _useMoneyTakeProfit;
		private readonly decimal _moneyTakeProfit;
		private readonly bool _usePercentTakeProfit;
		private readonly decimal _percentTakeProfit;
		private readonly bool _enableMoneyTrailing;
		private readonly decimal _moneyTrailTarget;
		private readonly decimal _moneyTrailStop;
		private readonly int _stopLossPips;
		private readonly int _takeProfitPips;
		private readonly int _trailingStopPips;
		private readonly bool _useBreakEven;
		private readonly int _breakEvenTriggerPips;
		private readonly int _breakEvenOffsetPips;
		private readonly int _fastPeriod;
		private readonly int _slowPeriod;
		private readonly int _momentumPeriod;
		private readonly decimal _buyThreshold;
		private readonly decimal _sellThreshold;
		private readonly int _macdFastLength;
		private readonly int _macdSlowLength;
		private readonly int _macdSignalLength;
		private readonly bool _useEquityStop;
		private readonly decimal _equityRiskPercent;
		private readonly decimal _pip;
		private readonly decimal _multiplier;

		private int _barCount;
		private int _macdCount;
		private decimal? _macdFast;
		private decimal? _macdSlow;
		private decimal? _macdSignal;
		private bool _started;
		private decimal _initialEquity;
		private decimal _peakEquity;
		private decimal _entry;
		private decimal? _stop;
		private decimal? _take;
		private decimal? _shadowStop;
		private decimal _best;
		private decimal? _moneyTrailPeak;

		public CryptoAnalysisModel(Strategy strategy)
		{
			var parameters = strategy.Parameters;

			decimal number(string name) => Convert.ToDecimal(parameters[name].Value);
			int integer(string name) => Convert.ToInt32(parameters[name].Value);
			bool flag(string name) => (bool)parameters[name].Value;

			_orderVolume = number("OrderVolume");
			_useMoneyTakeProfit = flag("UseMoneyTakeProfit");
			_moneyTakeProfit = number("MoneyTakeProfit");
			_usePercentTakeProfit = flag("UsePercentTakeProfit");
			_percentTakeProfit = number("PercentTakeProfit");
			_enableMoneyTrailing = flag("EnableMoneyTrailing");
			_moneyTrailTarget = number("MoneyTrailTarget");
			_moneyTrailStop = number("MoneyTrailStop");
			_stopLossPips = integer("StopLossPips");
			_takeProfitPips = integer("TakeProfitPips");
			_trailingStopPips = integer("TrailingStopPips");
			_useBreakEven = flag("UseBreakEven");
			_breakEvenTriggerPips = integer("BreakEvenTriggerPips");
			_breakEvenOffsetPips = integer("BreakEvenOffsetPips");
			_fastPeriod = integer("FastMaPeriod");
			_slowPeriod = integer("SlowMaPeriod");
			_momentumPeriod = integer("MomentumPeriod");
			_buyThreshold = number("MomentumBuyThreshold");
			_sellThreshold = number("MomentumSellThreshold");
			_macdFastLength = integer("MacdFastLength");
			_macdSlowLength = integer("MacdSlowLength");
			_macdSignalLength = integer("MacdSignalLength");
			_useEquityStop = flag("UseEquityStop");
			_equityRiskPercent = number("EquityRiskPercent");

			// README: pips come from the tick size, with 0.00001 and 0.001 steps multiplied by 10.
			var step = strategy.Security.PriceStep ?? 0m;
			_pip = step <= 0m ? 0.0001m : step is 0.00001m or 0.001m ? step * 10m : step;
			_multiplier = strategy.Security.Multiplier ?? 1m;
		}

		public int ExpectedOrders { get; private set; }
		public int Entries { get; private set; }
		public Dictionary<string, int> Exits { get; } = [];

		/// <summary>Candles where a stop trailed before any new high (long) or low (short) would have been hit.</summary>
		public int TrailBeforeNewExtreme { get; private set; }

		/// <summary>Candles whose own high or low moved the stop to a level the same candle crossed.</summary>
		public int SameCandleMoves { get; private set; }

		/// <summary>Money trailing exits taken after the profit fell back below the target.</summary>
		public int MoneyTrailBelowTarget { get; private set; }

		/// <summary>Candles where a plain-sum RSI(14) would have produced another order than the Wilder RSI(14).</summary>
		public int RsiDecides { get; private set; }

		/// <summary>Candles where a burst equal to the threshold would have produced another order.</summary>
		public int MomentumAtThreshold { get; private set; }

		/// <summary>Candles where the unarmed money trail saw a profit equal to the target.</summary>
		public int MoneyTrailAtTarget { get; private set; }

		/// <summary>Candles where the equity drawdown equalled the limit.</summary>
		public int EquityAtLimit { get; private set; }

		public List<decimal> EntryMomentums { get; } = [];
		public decimal? MaxUnarmedFloating { get; private set; }
		public decimal? MaxEquityDrawdown { get; private set; }

		/// <summary>The highest profit the percent take-profit checked, as a percentage of the initial equity.</summary>
		public decimal? MaxPercentProfit { get; private set; }

		public void Start(decimal initialEquity)
		{
			if (_started)
				return;

			_started = true;
			_initialEquity = initialEquity;
			_peakEquity = initialEquity;
		}

		public void OnMomentum(ICandleMessage candle)
		{
			_momentumCloses.Add(candle.ClosePrice);

			if (_momentumCloses.Count > _momentumPeriod + 5)
				_momentumCloses.RemoveAt(0);

			if (_momentumCloses.Count <= _momentumPeriod)
				return;

			var previous = _momentumCloses[^(_momentumPeriod + 1)];

			if (previous == 0m)
				return;

			// README: Momentum(14) is a ratio times 100, measured as its deviation from the 100 baseline.
			_deviations.Enqueue(Math.Abs(candle.ClosePrice / previous * 100m - 100m));

			if (_deviations.Count > 3)
				_deviations.Dequeue();
		}

		public void OnMacd(ICandleMessage candle)
		{
			_macdCount++;
			_macdFast = Ema(_macdFast, candle.ClosePrice, _macdFastLength);
			_macdSlow = Ema(_macdSlow, candle.ClosePrice, _macdSlowLength);
			_macdSignal = Ema(_macdSignal, _macdFast.Value - _macdSlow.Value, _macdSignalLength);
		}

		public (Sides Side, decimal Volume)? OnPrimary(ICandleMessage candle, decimal position, Portfolio portfolio)
		{
			Start(portfolio?.CurrentValue ?? portfolio?.BeginValue ?? 0m);

			var required = Math.Max(Math.Max(_fastPeriod, _slowPeriod), _bandLength + 1);

			_barCount++;
			_bars.Add((candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice));

			if (_bars.Count > required + 5)
				_bars.RemoveAt(0);

			var rsiValue = _rsi.Process(candle.ClosePrice, candle.ServerTime, true);

			if (position != 0m && Protect(candle, position, portfolio))
			{
				ResetTrade();
				ExpectedOrders++;
				return (position > 0m ? Sides.Sell : Sides.Buy, Math.Abs(position));
			}

			if (_barCount < required || !_rsi.IsFormed || _deviations.Count < 3 || _macdCount < _macdSlowLength + _macdSignalLength)
				return null;

			var previous = _bars[^2];
			var (upper, lower) = BandsBeforeThisCandle();
			var bearish = Lwma(_fastPeriod) < Lwma(_slowPeriod);
			var momentum = _deviations.Max();
			var macd = _macdFast.Value - _macdSlow.Value;
			var longSetup = previous.Low <= lower && bearish && macd > _macdSignal.Value;
			var shortSetup = previous.High >= upper && bearish && macd < _macdSignal.Value;

			Sides? decide(decimal rsi, bool inclusiveBurst)
			{
				var longBurst = inclusiveBurst ? momentum >= _buyThreshold : momentum > _buyThreshold;
				var shortBurst = inclusiveBurst ? momentum >= _sellThreshold : momentum > _sellThreshold;

				if (longSetup && rsi > 50m && longBurst && position <= 0m)
					return Sides.Buy;

				if (shortSetup && rsi < 50m && shortBurst && position >= 0m)
					return Sides.Sell;

				return null;
			}

			var wilder = rsiValue.ToDecimal();
			var side = decide(wilder, false);

			if (decide(PlainSumRsi(), false) != side)
				RsiDecides++;

			if (decide(wilder, true) != side)
				MomentumAtThreshold++;

			if (side is not Sides entrySide)
				return null;

			EntryMomentums.Add(momentum);
			var volume = _orderVolume + Math.Abs(position);
			Enter(entrySide, candle.ClosePrice);
			ExpectedOrders++;
			return (entrySide, volume);
		}

		private bool Protect(ICandleMessage candle, decimal position, Portfolio portfolio)
		{
			var isLong = position > 0m;
			var direction = isLong ? 1m : -1m;
			var floating = _entry == 0m ? 0m : (candle.ClosePrice - _entry) * direction * Math.Abs(position) * _multiplier;
			var equity = (portfolio?.CurrentValue ?? portfolio?.BeginValue ?? _initialEquity) + floating;
			_peakEquity = Math.Max(_peakEquity, equity);

			// README: protective rules work on closed candles, against the levels as they stood before the candle.
			if (IsHit(_stop, _take, candle, isLong))
				return Exit("level");

			if (IsHit(_shadowStop, null, candle, isLong))
				TrailBeforeNewExtreme++;

			if (_useMoneyTakeProfit && floating >= _moneyTakeProfit)
				return Exit("money take-profit");

			if (_usePercentTakeProfit && _initialEquity > 0m)
			{
				var percent = floating * 100m / _initialEquity;
				MaxPercentProfit = MaxPercentProfit is decimal highest ? Math.Max(highest, percent) : percent;

				// README: the percent target is calculated from the initial equity.
				if (floating >= _initialEquity * _percentTakeProfit / 100m)
					return Exit("percent take-profit");
			}

			if (_enableMoneyTrailing)
			{
				if (_moneyTrailPeak is decimal peak)
					_moneyTrailPeak = Math.Max(peak, floating);
				else
				{
					MaxUnarmedFloating = MaxUnarmedFloating is decimal highest ? Math.Max(highest, floating) : floating;

					// README: the trail starts once the profit exceeds the target.
					if (floating > _moneyTrailTarget)
						_moneyTrailPeak = floating;
					else if (floating == _moneyTrailTarget)
						MoneyTrailAtTarget++;
				}

				// README: once started, the giveback from the peak closes the position wherever the profit is.
				if (_moneyTrailPeak is decimal armed && armed - floating >= _moneyTrailStop)
				{
					if (floating < _moneyTrailTarget)
						MoneyTrailBelowTarget++;

					return Exit("money trailing");
				}
			}

			if (_useEquityStop && _peakEquity > 0m)
			{
				var drawdown = (_peakEquity - equity) / _peakEquity * 100m;
				MaxEquityDrawdown = MaxEquityDrawdown is decimal deepest ? Math.Max(deepest, drawdown) : drawdown;

				// README: the drawdown has to surpass the limit.
				if (drawdown == _equityRiskPercent)
					EquityAtLimit++;
				else if (drawdown > _equityRiskPercent)
					return Exit("equity stop");
			}

			MoveLevels(candle, isLong);
			return false;
		}

		private void MoveLevels(ICandleMessage candle, bool isLong)
		{
			var direction = isLong ? 1m : -1m;
			var extreme = isLong ? candle.HighPrice : candle.LowPrice;
			var before = _stop;

			if (_useBreakEven && (extreme - _entry) * direction >= _breakEvenTriggerPips * _pip)
			{
				var level = _entry + direction * _breakEvenOffsetPips * _pip;
				_stop = Tighten(_stop, level, direction);
				_shadowStop = Tighten(_shadowStop, level, direction);
			}

			var newExtreme = (extreme - _best) * direction > 0m;

			if (newExtreme)
				_best = extreme;

			if (_trailingStopPips > 0)
			{
				var trailed = _best - direction * _trailingStopPips * _pip;

				// README: the stop is pulled behind a new high (long) or low (short) only, never back.
				if (newExtreme)
					_stop = Tighten(_stop, trailed, direction);

				// The rule the README rules out pulls it behind the best price on every candle.
				_shadowStop = Tighten(_shadowStop, trailed, direction);
			}

			// README: a level moved with this candle's high or low is first checked on the next candle.
			if (_stop != before && IsHit(_stop, null, candle, isLong))
				SameCandleMoves++;
		}

		private void Enter(Sides side, decimal price)
		{
			var direction = side == Sides.Buy ? 1m : -1m;

			_entry = price;
			_stop = _stopLossPips > 0 ? price - direction * _stopLossPips * _pip : null;
			_take = _takeProfitPips > 0 ? price + direction * _takeProfitPips * _pip : null;
			_shadowStop = _stop;
			_best = price;
			_moneyTrailPeak = null;
			Entries++;
		}

		private bool Exit(string reason)
		{
			Exits[reason] = Exits.GetValueOrDefault(reason) + 1;
			return true;
		}

		private void ResetTrade()
		{
			_entry = 0m;
			_stop = null;
			_take = null;
			_shadowStop = null;
			_best = 0m;
			_moneyTrailPeak = null;
		}

		private static decimal? Tighten(decimal? stop, decimal level, decimal direction)
			=> stop is not decimal current || (level - current) * direction > 0m ? level : stop;

		private static bool IsHit(decimal? stop, decimal? take, ICandleMessage candle, bool isLong)
			=> isLong
				? (stop is decimal longStop && candle.LowPrice <= longStop) || (take is decimal longTake && candle.HighPrice >= longTake)
				: (stop is decimal shortStop && candle.HighPrice >= shortStop) || (take is decimal shortTake && candle.LowPrice <= shortTake);

		/// <summary>
		/// README: Bollinger Band (20, 2) of the closes up to the previous candle, with the population deviation.
		/// </summary>
		private (decimal Upper, decimal Lower) BandsBeforeThisCandle()
		{
			var end = _bars.Count - 1;
			var total = 0m;

			for (var i = end - _bandLength; i < end; i++)
				total += _bars[i].Close;

			var mean = total / _bandLength;
			var squares = 0m;

			for (var i = end - _bandLength; i < end; i++)
			{
				var deviation = _bars[i].Close - mean;
				squares += deviation * deviation;
			}

			var width = _bandWidth * (decimal)Math.Sqrt((double)(squares / _bandLength));
			return (mean + width, mean - width);
		}

		/// <summary>
		/// README: linear weighted moving average of the typical price, the latest candle weighted most.
		/// </summary>
		private decimal Lwma(int period)
		{
			var start = _bars.Count - period;
			var total = 0m;
			var weights = 0m;

			for (var i = 0; i < period; i++)
			{
				var bar = _bars[start + i];
				var weight = i + 1m;
				total += (bar.High + bar.Low + bar.Close) / 3m * weight;
				weights += weight;
			}

			return total / weights;
		}

		/// <summary>
		/// The plain-sum RSI(14): summed gains against summed losses of the last 14 closes.
		/// </summary>
		private decimal PlainSumRsi()
		{
			var gains = 0m;
			var losses = 0m;

			for (var i = _bars.Count - _rsiLength; i < _bars.Count; i++)
			{
				var change = _bars[i].Close - _bars[i - 1].Close;

				if (change > 0m)
					gains += change;
				else
					losses -= change;
			}

			return losses == 0m ? 100m : 100m - 100m / (1m + gains / losses);
		}

		private static decimal Ema(decimal? previous, decimal value, int length)
			=> previous is decimal last ? last + 2m / (length + 1m) * (value - last) : value;
	}

	private sealed class CryptoAnalysisRun
	{
		public CryptoAnalysisModel Model { get; set; }
		public List<(DateTime Time, Sides Side, decimal Volume)> Orders { get; } = [];
		public List<string> Violations { get; } = [];
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task S3301_StopTrailsOnlyNewExtremesAndMovedLevelsWaitForTheNextCandle()
	{
		var run = await ReplayCryptoAnalysis(CryptoAnalysisFixture(
			[.. _cryptoAnalysisTrailing, ("UseBreakEven", true), ("BreakEvenTriggerPips", 300), ("BreakEvenOffsetPips", 2)]), TimeSpan.FromDays(31));

		IsTrue(run.Model.Entries > 0 && run.Model.Exits.GetValueOrDefault("level") > 0, "The archive must reach entries and stop exits.");
		IsTrue(run.Model.TrailBeforeNewExtreme > 0, "The archive must reach candles where a stop trailed before any new high or low would have closed the position; the README trails only a new extreme.");
		IsTrue(run.Model.SameCandleMoves > 0, "The archive must reach candles that moved the stop with their own high or low to a level they crossed; the README checks that level from the next candle.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task S3301_MoneyTrailClosesOnTheGivebackEvenBelowTheTarget()
	{
		var run = await ReplayCryptoAnalysis(CryptoAnalysisFixture(
			("StopLossPips", 20000),
			("TakeProfitPips", 0),
			("TrailingStopPips", 0),
			("UseBreakEven", false),
			("EnableMoneyTrailing", true),
			("MoneyTrailTarget", 1m),
			("MoneyTrailStop", 2m)), TimeSpan.FromDays(31));

		IsTrue(run.Model.Exits.GetValueOrDefault("money trailing") > 0, "The archive must reach money trailing exits.");
		IsTrue(run.Model.MoneyTrailBelowTarget > 0, "The archive must reach a giveback of MoneyTrailStop after the profit fell back below MoneyTrailTarget; the README closes the position there.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task S3301_EntriesAreConfirmedByTheWilderRsi()
	{
		var run = await ReplayCryptoAnalysis(CryptoAnalysisFixture(), TimeSpan.FromDays(31));

		IsTrue(run.Model.Entries > 0, "The archive must reach entries.");
		IsTrue(run.Model.RsiDecides > 0, "The archive must reach candles where a plain-sum RSI(14) would have traded differently from the standard Wilder RSI(14) the README names.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task S3301_MomentumBurstMustExceedTheThreshold()
	{
		var probe = await ReplayCryptoAnalysis(CryptoAnalysisFixture(_cryptoAnalysisTrailing), TimeSpan.FromDays(14));
		IsTrue(probe.Model.EntryMomentums.Count > 0, "The probe must reach entries.");

		// The weakest burst that opened a position becomes the threshold: the same entry must now be refused.
		var threshold = probe.Model.EntryMomentums.Min();
		var run = await ReplayCryptoAnalysis(CryptoAnalysisFixture(
			[.. _cryptoAnalysisTrailing, ("MomentumBuyThreshold", threshold), ("MomentumSellThreshold", threshold)]), TimeSpan.FromDays(14));

		IsTrue(run.Model.MomentumAtThreshold > 0, $"The replay must reach the entry whose burst equals the threshold {threshold}; the README requires the burst to exceed it.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task S3301_MoneyTrailStartsOnlyAboveTheTarget()
	{
		var probe = await ReplayCryptoAnalysis(CryptoAnalysisFixture(
			[.. _cryptoAnalysisTrailing, ("EnableMoneyTrailing", true), ("MoneyTrailTarget", 1000000m), ("MoneyTrailStop", 0m)]), TimeSpan.FromDays(14));
		IsTrue(probe.Model.MaxUnarmedFloating is decimal highest && highest > 0m, "The probe must reach a profit.");

		// The best profit becomes the target: reaching it without exceeding it must not start the trail.
		var target = probe.Model.MaxUnarmedFloating.Value;
		var run = await ReplayCryptoAnalysis(CryptoAnalysisFixture(
			[.. _cryptoAnalysisTrailing, ("EnableMoneyTrailing", true), ("MoneyTrailTarget", target), ("MoneyTrailStop", 0m)]), TimeSpan.FromDays(14));

		IsTrue(run.Model.MoneyTrailAtTarget > 0, $"The replay must reach a profit equal to MoneyTrailTarget {target}.");
		AreEqual(probe.Orders, run.Orders, "A profit equal to MoneyTrailTarget does not exceed it, so with a zero MoneyTrailStop the trail must not close anything.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task S3301_EquityStopNeedsTheDrawdownToSurpassTheLimit()
	{
		var probe = await ReplayCryptoAnalysis(CryptoAnalysisFixture(
			[.. _cryptoAnalysisTrailing, ("UseEquityStop", true), ("EquityRiskPercent", 100m)]), TimeSpan.FromDays(14));
		IsTrue(probe.Model.MaxEquityDrawdown is decimal deepest && deepest > 0m, "The probe must reach an equity drawdown.");

		// The deepest drawdown becomes the limit: equalling it must not close the position.
		var limit = probe.Model.MaxEquityDrawdown.Value;
		var run = await ReplayCryptoAnalysis(CryptoAnalysisFixture(
			[.. _cryptoAnalysisTrailing, ("UseEquityStop", true), ("EquityRiskPercent", limit)]), TimeSpan.FromDays(14));

		IsTrue(run.Model.EquityAtLimit > 0, $"The replay must reach a drawdown equal to EquityRiskPercent {limit}.");
		AreEqual(probe.Orders, run.Orders, "A drawdown equal to EquityRiskPercent does not surpass it, so the equity stop must not close anything.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task S3301_EveryProtectionTogetherFollowsTheReadme()
	{
		(string Name, object Value)[] protections =
		[
			// One-minute signal candles give the month enough trades for every protection to close some of them.
			("CandleType", TimeSpan.FromMinutes(1).TimeFrame()),
			("StopLossPips", 20000),
			("TakeProfitPips", 0),
			("TrailingStopPips", 8000),
			("UseBreakEven", true),
			("BreakEvenTriggerPips", 1000),
			("BreakEvenOffsetPips", 50),
			("UseMoneyTakeProfit", true),
			("MoneyTakeProfit", 15m),
			("UsePercentTakeProfit", true),
			("EnableMoneyTrailing", true),
			("MoneyTrailTarget", 3m),
			("MoneyTrailStop", 1m),
			("UseEquityStop", true),
			("EquityRiskPercent", 0.004m),
		];

		var probe = await ReplayCryptoAnalysis(CryptoAnalysisFixture([.. protections, ("PercentTakeProfit", 100m)]), TimeSpan.FromDays(31));
		IsTrue(probe.Model.MaxPercentProfit is decimal highest && highest > 0m, "The probe must reach a profit the percent take-profit checks.");

		// Just under the best profit the percent target saw: every earlier profit stays below it, so the candle that made it now takes the profit.
		var percent = probe.Model.MaxPercentProfit.Value * 0.999999999m;
		var run = await ReplayCryptoAnalysis(CryptoAnalysisFixture([.. protections, ("PercentTakeProfit", percent)]), TimeSpan.FromDays(31));

		foreach (var reason in new[] { "level", "money take-profit", "percent take-profit", "money trailing", "equity stop" })
			IsTrue(run.Model.Exits.GetValueOrDefault(reason) > 0, $"The archive must reach a {reason} exit while every protection is on.");
	}

	/// <summary>
	/// Streams and filters the one-month archive reaches, with the given parameters on top.
	/// </summary>
	private static Dictionary<string, object> CryptoAnalysisFixture(params (string Name, object Value)[] values)
	{
		var fixture = new Dictionary<string, object>
		{
			// Shorter MACD, signal and momentum frames and a slow LWMA of 20 give the month enough entries for every rule to act.
			["MacdCandleType"] = TimeSpan.FromHours(1).TimeFrame(),
			["CandleType"] = TimeSpan.FromMinutes(5).TimeFrame(),
			["MomentumCandleType"] = TimeSpan.FromMinutes(15).TimeFrame(),
			["SlowMaPeriod"] = 20,
			["MomentumBuyThreshold"] = 0m,
			["MomentumSellThreshold"] = 0m,
		};

		foreach (var (name, value) in values)
			fixture[name] = value;

		return fixture;
	}

	/// <summary>
	/// The default values the README publishes, in either language.
	/// </summary>
	private static void AssertCryptoAnalysisDefaults(Strategy strategy)
	{
		foreach (var id in _cryptoAnalysisParameters)
			IsTrue(strategy.Parameters.ContainsKey(id), $"README lists the {id} parameter, but the strategy does not expose it.");

		AreEqual(TimeSpan.FromMinutes(15).TimeFrame(), strategy.Parameters["CandleType"].Value, "README: the signal timeframe defaults to M15.");
		AreEqual(TimeSpan.FromHours(1).TimeFrame(), strategy.Parameters["MomentumCandleType"].Value, "README: the momentum timeframe defaults to H1.");
		AreEqual(TimeSpan.FromHours(4).TimeFrame(), strategy.Parameters["MacdCandleType"].Value, "README: the MACD filter runs on 4-hour candles by default.");
		AreEqual(6, strategy.Parameters["FastMaPeriod"].Value, "README: the fast LWMA defaults to 6.");
		AreEqual(85, strategy.Parameters["SlowMaPeriod"].Value, "README: the slow LWMA defaults to 85.");
		AreEqual(14, strategy.Parameters["MomentumPeriod"].Value, "README: Momentum(14).");
		AreEqual(12, strategy.Parameters["MacdFastLength"].Value, "README: MACD (12, 26, 9).");
		AreEqual(26, strategy.Parameters["MacdSlowLength"].Value, "README: MACD (12, 26, 9).");
		AreEqual(9, strategy.Parameters["MacdSignalLength"].Value, "README: MACD (12, 26, 9).");
	}

	/// <summary>
	/// Replays the archive with the fixture over the published defaults and checks every order against the model.
	/// </summary>
	private async Task<CryptoAnalysisRun> ReplayCryptoAnalysis(IReadOnlyDictionary<string, object> fixture, TimeSpan duration)
	{
		var run = new CryptoAnalysisRun();
		(Sides Side, decimal Volume)? expected = null;

		void violate(string message)
		{
			if (run.Violations.Count < 12)
				run.Violations.Add(message);
		}

		await Replay(CryptoAnalysis, (strategy, _) =>
		{
			AssertCryptoAnalysisDefaults(strategy);

			foreach (var (name, value) in fixture)
				SetParam(strategy, name, value);

			var model = run.Model = new CryptoAnalysisModel(strategy);
			var primary = ((DataType)strategy.Parameters["CandleType"].Value).Arg;
			var momentum = ((DataType)strategy.Parameters["MomentumCandleType"].Value).Arg;
			var macd = ((DataType)strategy.Parameters["MacdCandleType"].Value).Arg;

			IsTrue(!primary.Equals(momentum) && !primary.Equals(macd) && !momentum.Equals(macd), "The model tells the three streams apart by their time frames.");

			strategy.ProcessStateChanged += changed =>
			{
				if (ReferenceEquals(changed, strategy) && changed.ProcessState == ProcessStates.Started)
					model.Start(strategy.Portfolio?.CurrentValue ?? strategy.Portfolio?.BeginValue ?? 0m);
			};

			// Subscribed before the strategy binds its own handlers, so the model is current when an order registers.
			strategy.CandleReceived += (subscription, candle) =>
			{
				if (candle.State != CandleStates.Finished || strategy.ProcessState != ProcessStates.Started)
					return;

				var frame = subscription.DataType.Arg;

				if (frame.Equals(momentum))
					model.OnMomentum(candle);
				else if (frame.Equals(macd))
					model.OnMacd(candle);
				else if (frame.Equals(primary))
				{
					if (expected is { } due)
						violate($"{candle.OpenTime:O}: the {due.Side} {due.Volume} order due on the previous signal candle was never registered.");

					expected = model.OnPrimary(candle, strategy.Position, strategy.Portfolio);
				}
			};

			strategy.OrderRegistering += order =>
			{
				run.Orders.Add((strategy.CurrentTime, order.Side, order.Volume));

				if (expected is not { } due || order.Side != due.Side || order.Volume != due.Volume || order.Type != OrderTypes.Market)
				{
					var wanted = expected is { } pending ? $"{pending.Side} {pending.Volume} at market" : "no order";
					violate($"{strategy.CurrentTime:O}: expected {wanted}, got {order.Side} {order.Volume} {order.Type}.");
				}

				expected = null;
			};
		}, duration);

		var outcome = run.Model;
		TestContext.WriteLine($"python={IsPython} days={duration.TotalDays}: orders={run.Orders.Count}, entries={outcome.Entries}, " +
			$"exits={string.Join(", ", outcome.Exits.Select(pair => $"{pair.Key}={pair.Value}"))}, trailBeforeNewExtreme={outcome.TrailBeforeNewExtreme}, " +
			$"sameCandleMoves={outcome.SameCandleMoves}, moneyTrailBelowTarget={outcome.MoneyTrailBelowTarget}, rsiDecides={outcome.RsiDecides}, " +
			$"momentumAtThreshold={outcome.MomentumAtThreshold}, moneyTrailAtTarget={outcome.MoneyTrailAtTarget}, equityAtLimit={outcome.EquityAtLimit}, " +
			$"maxPercentProfit={outcome.MaxPercentProfit}");

		IsTrue(run.Violations.Count == 0, "Every order must be the one the README gives for its signal candle: the entry filters with the Wilder RSI(14) and a burst exceeding the threshold, " +
			"or a protective exit against the levels as they stood before the candle, a stop trailed only behind new extremes, a money trail that keeps watching the giveback once started, " +
			"and drawdown and profit limits that have to be surpassed or exceeded." + Environment.NewLine + string.Join(Environment.NewLine, run.Violations));
		AreEqual(outcome.ExpectedOrders, run.Orders.Count, "Every order the README gives must be registered, and nothing else may trade.");

		return run;
	}

	/// <summary>
	/// Folder key of the Range Follower example.
	/// </summary>
	protected const string RangeFollower = "3406_Range_Follower";

	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S3406_NoEntryBeforeTheDaysFirstWorkingCandle()
	{
		var session = DateTime.MinValue;
		var sessionAtr = 0m;
		var skipped = false;
		var high = 0m;
		var low = 0m;
		decimal? bid = null;
		decimal? ask = null;
		var inWindow = false;
		var windows = 0;
		var windowBreakouts = 0;
		var tradedSessions = new HashSet<DateTime>();
		var entriesPerDay = new Dictionary<DateTime, int>();
		var violations = new List<string>();

		await Replay(RangeFollower, (strategy, _) =>
		{
			var percent = strategy.Parameters["TriggerPercent"];
			AreEqual(60m, Convert.ToDecimal(percent.Value));
			AreEqual(TimeSpan.FromMinutes(15).TimeFrame(), strategy.Parameters["CandleType"].Value);
			strategy.Volume = 0.1m;

			void SetPercent(decimal value) => SetParam(strategy, "TriggerPercent", value);

			// Until the first working candle of a date finishes, the previous session is still loaded. A 10% trigger
			// makes its frozen range a breakout there, so an entry allowed before the new session starts shows up.
			void OpenWindowOnNewDate()
			{
				if (session == DateTime.MinValue || strategy.CurrentTime.Date <= session || inWindow)
					return;

				inWindow = true;
				windows++;
				SetPercent(10m);
			}

			strategy.ProcessStateChanged += changed =>
			{
				if (ReferenceEquals(changed, strategy) && changed.ProcessState == ProcessStates.Stopped)
					SetPercent(60m);
			};

			strategy.CandleReceived += (subscription, candle) =>
			{
				if (candle.State != CandleStates.Finished || subscription.DataType.Arg is not TimeSpan frame || frame != TimeSpan.FromMinutes(15))
					return;

				if (candle.OpenTime.Date == session)
				{
					high = Math.Max(high, candle.HighPrice);
					low = Math.Min(low, candle.LowPrice);
					OpenWindowOnNewDate();
					return;
				}

				if (inWindow)
				{
					inWindow = false;
					SetPercent(60m);
				}

				session = candle.OpenTime.Date;
				high = candle.HighPrice;
				low = candle.LowPrice;
				var atr = strategy.Indicators.OfType<AverageTrueRange>().Single();
				sessionAtr = atr.IsFormed ? atr.GetCurrentValue() : 0m;
				skipped = sessionAtr > 0m && high - low > sessionAtr * 60m / 100m;
			};

			strategy.Level1Received += (_, quote) =>
			{
				if (!RangeFollowerTakeQuote(quote, ref bid, ref ask) || bid is not decimal currentBid || ask is not decimal currentAsk)
					return;

				if (session == quote.ServerTime.Date)
				{
					high = Math.Max(high, Math.Max(currentBid, currentAsk));
					low = Math.Min(low, Math.Min(currentBid, currentAsk));
				}

				OpenWindowOnNewDate();

				if (inWindow && sessionAtr > 0m && !skipped && !tradedSessions.Contains(session) && strategy.Position == 0m
					&& strategy.IsFormedAndOnlineAndAllowTrading() && strategy.Orders.All(order => order.State is OrderStates.Done or OrderStates.Failed)
					&& Math.Max(currentBid - low, high - currentAsk) > sessionAtr * 10m / 100m)
					windowBreakouts++;
			};

			strategy.OrderRegistering += order =>
			{
				if (strategy.Position != 0m)
					return;

				var day = strategy.CurrentTime.Date;
				entriesPerDay[day] = entriesPerDay.GetValueOrDefault(day) + 1;
				tradedSessions.Add(session);

				if (day != session && violations.Count < 12)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} entry before the first working candle of {day:yyyy-MM-dd} initialised its session, on the range and ATR of {session:yyyy-MM-dd}.");
			};
		}, TimeSpan.FromDays(31));

		TestContext.WriteLine($"python={IsPython}: windows={windows}, windowBreakouts={windowBreakouts}, entries={entriesPerDay.Values.Sum()} on {entriesPerDay.Count} days");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));
		IsTrue(entriesPerDay.Count > 0 && entriesPerDay.Values.All(count => count == 1), "Only one trade is allowed per day.");
		IsTrue(windowBreakouts > 0, "The fixture must reach a new date whose quotes clear the trigger against the previous session's range before the date's first working candle.");
	}

	/// <summary>
	/// Applies a Level1 update as the strategy's bid and ask subscriptions see it: only a message carrying a best bid or
	/// best ask reaches them, and only positive prices replace the latest ones.
	/// </summary>
	protected static bool RangeFollowerTakeQuote(Level1ChangeMessage quote, ref decimal? bid, ref decimal? ask)
	{
		var quoteBid = quote.TryGetDecimal(Level1Fields.BestBidPrice);
		var quoteAsk = quote.TryGetDecimal(Level1Fields.BestAskPrice);

		if (quoteBid is null && quoteAsk is null)
			return false;

		if (quoteBid > 0m)
			bid = quoteBid;

		if (quoteAsk > 0m)
			ask = quoteAsk;

		return true;
	}

	private const string EconomicCalendar = "3507_Sample_Detect_Economic_Calendar";

	[TestMethod]
	[TestCategory("Shard03")]
	[DataRow("yyyy-MM-dd HH:mm")]
	[DataRow("yyyy-MM-dd HH:mm:ss")]
	[DataRow("yyyy/MM/dd HH:mm")]
	[DataRow("yyyy/MM/dd HH:mm:ss")]
	[DataRow("dd.MM.yyyy HH:mm")]
	[DataRow("dd.MM.yyyy HH:mm:ss")]
	public async Task S3507_CalendarReadsEveryDocumentedTimestampFormat(string format)
	{
		// A mid-minute release tells a parsed seconds field from a dropped one; 01.03 also tells dd.MM from MM.dd.
		var withSeconds = format.EndsWith(":ss", StringComparison.Ordinal);
		var release = Paths.HistoryBeginDate.AddMinutes(10).AddSeconds(withSeconds ? 30 : 0);
		var armed = new List<DateTime>();

		await Replay(EconomicCalendar, (strategy, _) =>
		{
			SetParam(strategy, "OrderVolume", 2m);
			SetParam(strategy, "StopLossPoints", 10);
			SetParam(strategy, "TakeProfitPoints", 10);
			SetParam(strategy, "TrailingStopPoints", 0);
			SetParam(strategy, "BuyDistancePoints", 1);
			SetParam(strategy, "SellDistancePoints", 1);
			SetParam(strategy, "LeadMinutes", 0);
			SetParam(strategy, "CalendarDefinition", $"{release.ToString(format, CultureInfo.InvariantCulture)};USD;High;Timestamp format");
			strategy.OrderRegistering += order =>
			{
				if (order.Type == OrderTypes.Conditional)
					armed.Add(strategy.CurrentTime);
			};
		}, TimeSpan.FromHours(1));

		AreEqual(2, armed.Count, $"README accepts '{format}' timestamps, so the High USD row must arm exactly one stop pair.");
		IsTrue(armed.All(time => time >= release && time <= release.AddMinutes(5)),
			$"The pair must arm at the UTC release read from the row, seconds included: release {release:O}, armed {string.Join(", ", armed.Select(time => time.ToString("O")))}.");
	}

	/// <summary>
	/// Folder key of the Order Stabilization example.
	/// </summary>
	protected const string OrderStabilization = "3801_OrderStabilization";

	private static readonly string[] _orderStabilizationParameters =
		["OrderVolume", "OrderDistancePoints", "ProfitThreshold", "AbsoluteFixation", "StabilizationPoints", "ExpirationMinutes", "CandleType"];

	private sealed class OrderStabilizationReplay
	{
		public int Pairs { get; set; }
		public int Entries { get; set; }
		public int OppositeFills { get; set; }
		public int ProfitExits { get; set; }
		public int StabilizationExits { get; set; }
		public int FlatRenewals { get; set; }
		public int OpenRenewals { get; set; }
		public List<string> Violations { get; } = [];
	}

	[TestMethod]
	[TestCategory("Shard01")]
	[DataRow("defaults")]
	[DataRow("exits")]
	[DataRow("renewals")]
	public async Task S3801_PairedStopsStabilizationExitsAndExpiry(string scenario)
	{
		var replay = await ReplayOrderStabilization(scenario);

		switch (scenario)
		{
			case "defaults":
				IsTrue(replay.Pairs > 0 && replay.Entries > 0 && replay.OppositeFills > 0,
					$"The archive must place stop pairs, fill one side and then the opposite stop still pending: pairs={replay.Pairs}, entries={replay.Entries}, opposite fills={replay.OppositeFills}.");
				break;
			case "exits":
				IsTrue(replay.Pairs > 0 && replay.Entries > 0 && replay.ProfitExits > 0 && replay.StabilizationExits > 0,
					$"The archive must exercise the AbsoluteFixation exit and the stabilization exits: pairs={replay.Pairs}, entries={replay.Entries}, profit exits={replay.ProfitExits}, stabilization exits={replay.StabilizationExits}.");
				break;
			case "renewals":
				IsTrue(replay.Entries > 0 && replay.FlatRenewals > 0 && replay.OpenRenewals > 0,
					$"The archive must expire pending stops both while flat and while a position is open: entries={replay.Entries}, flat renewals={replay.FlatRenewals}, renewals with a position={replay.OpenRenewals}.");
				break;
		}

		IsTrue(replay.Violations.Count == 0, string.Join(Environment.NewLine, replay.Violations.Take(12)));
	}

	/// <summary>
	/// Activation price of a stop order of the Order Stabilization example.
	/// </summary>
	/// <param name="order">The order.</param>
	/// <returns>The activation price, or null when the order carries no stop condition.</returns>
	protected static decimal? OrderStabilizationActivation(Order order)
		=> order.Condition is StopOrderCondition condition ? condition.ActivationPrice : null;

	private static bool IsOrderStabilizationStopWorking(Order order)
		=> order.State is not (OrderStates.Done or OrderStates.Failed);

	private static void AssertOrderStabilizationParameters(Strategy strategy)
	{
		AreEqual(0.1m, Convert.ToDecimal(strategy.Parameters["OrderVolume"].Value));
		AreEqual(20m, Convert.ToDecimal(strategy.Parameters["OrderDistancePoints"].Value));
		AreEqual(-2m, Convert.ToDecimal(strategy.Parameters["ProfitThreshold"].Value));
		AreEqual(30m, Convert.ToDecimal(strategy.Parameters["AbsoluteFixation"].Value));
		AreEqual(25m, Convert.ToDecimal(strategy.Parameters["StabilizationPoints"].Value));
		AreEqual(20, Convert.ToInt32(strategy.Parameters["ExpirationMinutes"].Value));
		AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);

		foreach (var name in _orderStabilizationParameters)
		{
			var display = strategy.Parameters[name].Attributes.OfType<DisplayAttribute>().SingleOrDefault();

			IsTrue(display is not null && !string.IsNullOrEmpty(display.Name) && !string.IsNullOrEmpty(display.Description) && !string.IsNullOrEmpty(display.GroupName),
				$"README promises parameter metadata with descriptions and grouping, but {name} has no display name, description or group.");
		}
	}

	/// <summary>
	/// Replays the strategy against an independent model of the README rules, evaluated at every finished candle:
	/// flat with nothing pending places a buy stop above and a sell stop below the close; a filled stop leaves the
	/// opposite one pending; an open position is closed by the stabilization and profit rules measured from the
	/// actual fills; pending stops are renewed after ExpirationMinutes without ever re-arming the open side.
	/// </summary>
	private async Task<OrderStabilizationReplay> ReplayOrderStabilization(string scenario)
	{
		var replay = new OrderStabilizationReplay();
		var stops = new HashSet<Order>();
		var pending = new List<Order>();
		var armedAt = new Dictionary<Order, DateTime>();
		var cancelRequired = new HashSet<Order>();
		var expected = new List<(Sides Side, OrderTypes Type, decimal Volume, decimal? Activation)>();
		var filled = 0m;
		var entryValue = 0m;
		decimal? previousBody = null;
		var candleOpen = default(DateTime);

		await Replay(OrderStabilization, (strategy, _) =>
		{
			AssertOrderStabilizationParameters(strategy);

			var security = strategy.Security;
			var step = security.PriceStep.Value;

			// One price step is worth two in account currency, so profit depends on StepPrice and not on the raw move.
			security.StepPrice = step * 2m;

			switch (scenario)
			{
				case "defaults":
					break;
				case "exits":
					SetParam(strategy, "OrderDistancePoints", 50m / step);
					SetParam(strategy, "ProfitThreshold", -2.01m);
					SetParam(strategy, "AbsoluteFixation", 10.01m);
					SetParam(strategy, "StabilizationPoints", 20.05m / step);
					SetParam(strategy, "ExpirationMinutes", 0);
					break;
				case "renewals":
					SetParam(strategy, "OrderDistancePoints", 300m / step);
					SetParam(strategy, "ProfitThreshold", 1_000_000_000m);
					SetParam(strategy, "AbsoluteFixation", 1_000_000_000m);
					SetParam(strategy, "StabilizationPoints", 0.05m / step);
					SetParam(strategy, "ExpirationMinutes", 15);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "Unknown scenario.");
			}

			var volume = Convert.ToDecimal(strategy.Parameters["OrderVolume"].Value);
			var distance = Convert.ToDecimal(strategy.Parameters["OrderDistancePoints"].Value) * step;
			var profitThreshold = Convert.ToDecimal(strategy.Parameters["ProfitThreshold"].Value);
			var absoluteFixation = Convert.ToDecimal(strategy.Parameters["AbsoluteFixation"].Value);
			var bodyLimit = Convert.ToDecimal(strategy.Parameters["StabilizationPoints"].Value) * step;
			var expiration = TimeSpan.FromMinutes(Convert.ToInt32(strategy.Parameters["ExpirationMinutes"].Value));

			decimal FloatingPnL(decimal price)
			{
				var position = strategy.Position;

				if (position == 0m || filled == 0m)
					return 0m;

				var entryPrice = entryValue / Math.Abs(filled);
				var direction = position > 0m ? 1m : -1m;
				var move = (price - entryPrice) * direction;
				return move / step * security.StepPrice.Value * Math.Abs(position);
			}

			void ExpectStop(Sides side, decimal close)
				=> expected.Add((side, OrderTypes.Conditional, volume, side == Sides.Buy ? close + distance : close - distance));

			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished || strategy.ProcessState != ProcessStates.Started)
					return;

				foreach (var missing in expected)
					replay.Violations.Add($"{candleOpen:O}: the README required {missing.Type} {missing.Side} {missing.Volume} at {missing.Activation}, but it was not sent.");

				foreach (var stop in cancelRequired.Where(IsOrderStabilizationStopWorking))
					replay.Violations.Add($"{candleOpen:O}: the pending {stop.Side} stop at {OrderStabilizationActivation(stop)} had to be cancelled, but it was left working.");

				expected.Clear();
				cancelRequired.Clear();

				candleOpen = candle.OpenTime;

				var close = candle.ClosePrice;
				var body = Math.Abs(close - candle.OpenPrice);
				var position = strategy.Position;
				pending.RemoveAll(stop => !IsOrderStabilizationStopWorking(stop));
				var working = pending.ToArray();

				if (Math.Abs(position) > volume)
					replay.Violations.Add($"{candleOpen:O}: position {position} is larger than OrderVolume {volume}.");

				var exit = false;

				if (position != 0m)
				{
					var pnl = FloatingPnL(close);
					var oneSmall = body <= bodyLimit;
					var twoSmall = oneSmall && previousBody is decimal previous && previous <= bodyLimit;
					var profitExit = pnl >= absoluteFixation;
					var stabilizationExit = (oneSmall && pnl > profitThreshold) || twoSmall;

					if (profitExit)
						replay.ProfitExits++;

					if (stabilizationExit)
						replay.StabilizationExits++;

					exit = profitExit || stabilizationExit;

					if (exit)
					{
						expected.Add((position > 0m ? Sides.Sell : Sides.Buy, OrderTypes.Market, Math.Abs(position), null));
						cancelRequired.UnionWith(working);
					}
				}

				if (!exit)
				{
					if (position == 0m && working.Length == 0)
					{
						ExpectStop(Sides.Buy, close);
						ExpectStop(Sides.Sell, close);
						replay.Pairs++;
					}
					else if (expiration > TimeSpan.Zero && working.Any(stop => candle.OpenTime - armedAt[stop] >= expiration))
					{
						if (position == 0m)
						{
							ExpectStop(Sides.Buy, close);
							ExpectStop(Sides.Sell, close);
							cancelRequired.UnionWith(working);
							replay.FlatRenewals++;
						}
						else
						{
							var oppositeSide = position > 0m ? Sides.Sell : Sides.Buy;
							var opposite = working.Where(stop => stop.Side == oppositeSide).ToArray();

							if (opposite.Length > 0)
							{
								ExpectStop(oppositeSide, close);
								cancelRequired.UnionWith(opposite);
								replay.OpenRenewals++;
							}
						}
					}
				}

				previousBody = body;
			};

			strategy.OrderRegistering += order =>
			{
				if (strategy.ProcessState != ProcessStates.Started)
					return;

				var activation = OrderStabilizationActivation(order);
				var index = expected.FindIndex(e => e.Side == order.Side && e.Type == order.Type && e.Volume == order.Volume && e.Activation == activation);

				if (index < 0)
					replay.Violations.Add($"{strategy.CurrentTime:O}: {order.Type} {order.Side} {order.Volume} at {activation} was sent without a README reason (position {strategy.Position}).");
				else
					expected.RemoveAt(index);

				if (order.Type == OrderTypes.Conditional)
				{
					stops.Add(order);
					pending.Add(order);
					armedAt[order] = candleOpen;
				}
			};

			strategy.OrderCanceling += order =>
			{
				if (strategy.ProcessState == ProcessStates.Started && !cancelRequired.Contains(order))
					replay.Violations.Add($"{strategy.CurrentTime:O}: the {order.Side} stop at {OrderStabilizationActivation(order)} was cancelled although neither an exit nor its expiry asked for it; the opposite stop must remain pending.");
			};

			strategy.Trades.TradeAdded += trade =>
			{
				var price = trade.Trade.Price;
				var tradeVolume = trade.Trade.Volume;
				var signed = trade.Order.Side == Sides.Buy ? tradeVolume : -tradeVolume;
				var previous = filled;

				filled += signed;

				if (filled == 0m)
					entryValue = 0m;
				else if (previous == 0m || Math.Sign(previous) == Math.Sign(signed))
					entryValue += price * tradeVolume;
				else if (Math.Sign(filled) != Math.Sign(previous))
					entryValue = price * Math.Abs(filled);
				else
					entryValue -= entryValue / Math.Abs(previous) * tradeVolume;

				var isStop = stops.Contains(trade.Order);

				if (Math.Abs(filled) > volume)
					replay.Violations.Add($"{strategy.CurrentTime:O}: the {(isStop ? "stop" : "market")} {trade.Order.Side} fill made the position {filled}, larger than OrderVolume {volume}.");

				if (isStop)
				{
					if (previous == 0m)
						replay.Entries++;
					else if (filled == 0m)
						replay.OppositeFills++;
				}
			};
		}, TimeSpan.FromDays(10));

		return replay;
	}

	private const string FiveMa = "3908_Five_MA_Multi_Timeframe";

	private sealed class FiveMaRun
	{
		public List<string> Violations { get; } = [];
		public int Evaluations { get; set; }
		public int ExpectedOrders { get; set; }
		public int ActualOrders { get; set; }
		public int Entries { get; set; }
		public int Closes { get; set; }
		public int HeldAtCloseLevel { get; set; }
		public int DecidedWithoutSecondaryAc { get; set; }
		public int DecidedByPrimaryAc { get; set; }
		public int DecidedByTertiaryAc { get; set; }
	}

	[TestMethod]
	[TestCategory("Shard04")]
	public async Task S3908_DefaultsCloseOnlyWhenOppositeGradeExceedsCloseLevel()
	{
		var run = await ReplayFiveMa(strategy =>
		{
			AreEqual(TimeSpan.FromMinutes(15).TimeFrame(), strategy.Parameters["CandleType"].Value);
			AreEqual(TimeSpan.FromMinutes(60).TimeFrame(), strategy.Parameters["HigherTimeframe1"].Value);
			AreEqual(TimeSpan.FromMinutes(240).TimeFrame(), strategy.Parameters["HigherTimeframe2"].Value);
			AreEqual(5, strategy.Parameters["FirstPeriod"].Value);
			AreEqual(8, strategy.Parameters["SecondPeriod"].Value);
			AreEqual(13, strategy.Parameters["ThirdPeriod"].Value);
			AreEqual(21, strategy.Parameters["FourthPeriod"].Value);
			AreEqual(34, strategy.Parameters["FifthPeriod"].Value);
			AreEqual(0, strategy.Parameters["OpenLevel"].Value);
			AreEqual(1, strategy.Parameters["CloseLevel"].Value);
		});

		IsTrue(run.Closes > 0, "The archive must close positions on an opposite grade above the default CloseLevel of 1.");
		IsTrue(run.HeldAtCloseLevel > 0, "The archive must reach open positions whose opposite grade equals CloseLevel, which the README does not let close.");
		IsTrue(run.DecidedByTertiaryAc > 0, "The archive must reach evaluations that the Accelerator Oscillator vote of the slowest timeframe decides.");
	}

	[TestMethod]
	[TestCategory("Shard04")]
	[DataRow(0, 0)]
	[DataRow(1, 1)]
	public async Task S3908_AcceleratorVotesOnlyOnPrimaryAndSlowestTimeframes(int openLevel, int closeLevel)
	{
		var run = await ReplayFiveMa(strategy =>
		{
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "HigherTimeframe1", TimeSpan.FromMinutes(15).TimeFrame());
			SetParam(strategy, "HigherTimeframe2", TimeSpan.FromMinutes(60).TimeFrame());
			SetParam(strategy, "OpenLevel", openLevel);
			SetParam(strategy, "CloseLevel", closeLevel);
		});

		IsTrue(run.Closes > 0, $"The archive must close positions on an opposite grade above CloseLevel {closeLevel}.");
		IsTrue(run.HeldAtCloseLevel > 0, $"The archive must reach open positions whose opposite grade equals CloseLevel {closeLevel}, which the README does not let close.");
		IsTrue(run.DecidedWithoutSecondaryAc > 0, "The archive must reach evaluations where an Accelerator Oscillator vote on HigherTimeframe1 would change the order.");
		IsTrue(run.DecidedByPrimaryAc > 0, "The archive must reach evaluations that the Accelerator Oscillator vote of the primary timeframe decides.");
		IsTrue(run.DecidedByTertiaryAc > 0, "The archive must reach evaluations that the Accelerator Oscillator vote of HigherTimeframe2 decides.");
	}

	/// <summary>
	/// Replays the archive against an independent model of the README: five SMA slopes vote on every timeframe, the
	/// Accelerator Oscillator adds a vote on the primary and the slowest timeframe only, each timeframe's votes form a
	/// percentage score graded 2 above 75% and 1 above 50%, the signal is the weakest grade of the three, a flat book
	/// opens on a grade above OpenLevel, and a position closes only when the opposite grade exceeds CloseLevel.
	/// </summary>
	private async Task<FiveMaRun> ReplayFiveMa(Action<Strategy> configure)
	{
		var run = new FiveMaRun();
		var frames = new Dictionary<TimeSpan, FiveMaFrame>();
		FiveMaFrame primary = null;
		FiveMaFrame secondary = null;
		FiveMaFrame tertiary = null;
		var openLevel = 0;
		var closeLevel = 0;
		(Sides Side, decimal Volume)? expected = null;

		void violate(string message)
		{
			if (run.Violations.Count < 12)
				run.Violations.Add(message);
		}

		await Replay(FiveMa, (strategy, _) =>
		{
			foreach (var id in new[] { "CandleType", "HigherTimeframe1", "HigherTimeframe2", "FirstPeriod", "SecondPeriod", "ThirdPeriod", "FourthPeriod", "FifthPeriod", "OpenLevel", "CloseLevel" })
				IsTrue(strategy.Parameters.ContainsKey(id), $"README lists the {id} parameter, but the strategy does not expose it.");

			configure(strategy);

			var periods = new[] { "FirstPeriod", "SecondPeriod", "ThirdPeriod", "FourthPeriod", "FifthPeriod" }
				.Select(id => Convert.ToInt32(strategy.Parameters[id].Value))
				.ToArray();
			openLevel = Convert.ToInt32(strategy.Parameters["OpenLevel"].Value);
			closeLevel = Convert.ToInt32(strategy.Parameters["CloseLevel"].Value);

			FiveMaFrame create(string id)
			{
				FiveMaFrame frame = IsPython ? new FiveMaFrame<double>(periods) : new FiveMaFrame<decimal>(periods);
				frames.Add((TimeSpan)((DataType)strategy.Parameters[id].Value).Arg, frame);
				return frame;
			}

			primary = create("CandleType");
			secondary = create("HigherTimeframe1");
			tertiary = create("HigherTimeframe2");

			strategy.CandleReceived += (subscription, candle) =>
			{
				if (candle.State != CandleStates.Finished || strategy.ProcessState != ProcessStates.Started)
					return;

				if (expected is { } due)
					violate($"{candle.OpenTime:O}: the {due.Side} {due.Volume} order due on the previous finished candle was never registered.");

				expected = null;

				if (subscription.DataType.Arg is not TimeSpan timeFrame || !frames.TryGetValue(timeFrame, out var frame))
				{
					violate($"{candle.OpenTime:O}: a finished {subscription.DataType} candle belongs to none of the three README timeframes.");
					return;
				}

				frame.Process(candle);

				if (!primary.IsReady || !secondary.IsReady || !tertiary.IsReady)
					return;

				run.Evaluations++;

				var position = strategy.Position;
				var volume = strategy.Volume;

				var signal = FiveMaSignal(primary.Grades(true), secondary.Grades(false), tertiary.Grades(true));
				var order = FiveMaOrder(position, volume, signal, openLevel, closeLevel, false);

				if (!Equals(order, FiveMaOrder(position, volume, FiveMaSignal(primary.Grades(true), secondary.Grades(true), tertiary.Grades(true)), openLevel, closeLevel, false)))
					run.DecidedWithoutSecondaryAc++;

				if (!Equals(order, FiveMaOrder(position, volume, FiveMaSignal(primary.Grades(false), secondary.Grades(false), tertiary.Grades(true)), openLevel, closeLevel, false)))
					run.DecidedByPrimaryAc++;

				if (!Equals(order, FiveMaOrder(position, volume, FiveMaSignal(primary.Grades(true), secondary.Grades(false), tertiary.Grades(false)), openLevel, closeLevel, false)))
					run.DecidedByTertiaryAc++;

				if (position != 0m && (position > 0m ? signal.Bearish : signal.Bullish) == closeLevel)
					run.HeldAtCloseLevel++;

				if (order is not { } next)
					return;

				expected = next;
				run.ExpectedOrders++;

				if (position == 0m)
					run.Entries++;
				else
					run.Closes++;
			};

			strategy.OrderRegistering += order =>
			{
				run.ActualOrders++;

				if (expected is not { } due || order.Side != due.Side || order.Volume != due.Volume || order.Type != OrderTypes.Market)
				{
					var wanted = expected is { } e ? $"{e.Side} {e.Volume} at market" : "no order";
					violate($"{strategy.CurrentTime:O}: expected {wanted}, got {order.Side} {order.Volume} {order.Type}. A position closes only when the opposite grade exceeds CloseLevel, and a flat book opens only on a grade above OpenLevel; the Accelerator Oscillator votes on the primary and the slowest timeframe, not on HigherTimeframe1.");
				}

				expected = null;
			};
		}, TimeSpan.FromDays(31));

		if (expected is { } last)
			violate($"End of replay: the {last.Side} {last.Volume} order due on the last finished candle was never registered.");

		TestContext.WriteLine($"python={IsPython} open={openLevel} close={closeLevel}: candles={primary?.Candles}/{secondary?.Candles}/{tertiary?.Candles}, " +
			$"evaluations={run.Evaluations}, orders={run.ActualOrders}/{run.ExpectedOrders}, entries={run.Entries}, closes={run.Closes}, " +
			$"heldAtCloseLevel={run.HeldAtCloseLevel}, decidedWithoutSecondaryAc={run.DecidedWithoutSecondaryAc}, " +
			$"decidedByPrimaryAc={run.DecidedByPrimaryAc}, decidedByTertiaryAc={run.DecidedByTertiaryAc}");

		IsTrue(run.Violations.Count == 0, string.Join(Environment.NewLine, run.Violations));
		AreEqual(run.ExpectedOrders, run.ActualOrders, "Every README order must be placed on its own finished candle, and nothing else may trade.");
		IsTrue(primary.Candles > 0 && secondary.Candles > 0 && tertiary.Candles > 0, "All three README timeframes must deliver finished candles.");
		IsTrue(run.Evaluations > 0, "The archive must reach evaluations with all three timeframes scored.");
		IsTrue(run.Entries > 0, "The archive must open positions on the README signal.");

		return run;
	}

	private static (int Bullish, int Bearish) FiveMaSignal((int Bullish, int Bearish) primary, (int Bullish, int Bearish) secondary, (int Bullish, int Bearish) tertiary)
		=> (Math.Min(primary.Bullish, Math.Min(secondary.Bullish, tertiary.Bullish)), Math.Min(primary.Bearish, Math.Min(secondary.Bearish, tertiary.Bearish)));

	/// <summary>
	/// The order the README places on one evaluation, or <see langword="null"/> when it places none.
	/// </summary>
	/// <param name="closeAtLevel">Also close when the opposite grade only equals CloseLevel.</param>
	private static (Sides Side, decimal Volume)? FiveMaOrder(decimal position, decimal volume, (int Bullish, int Bearish) signal, int openLevel, int closeLevel, bool closeAtLevel)
	{
		if (position != 0m)
		{
			var opposite = position > 0m ? signal.Bearish : signal.Bullish;

			if (opposite > closeLevel || (closeAtLevel && opposite == closeLevel))
				return (position > 0m ? Sides.Sell : Sides.Buy, Math.Abs(position));

			return null;
		}

		if (signal.Bullish > openLevel)
			return (Sides.Buy, volume);

		if (signal.Bearish > openLevel)
			return (Sides.Sell, volume);

		return null;
	}

	private abstract class FiveMaFrame
	{
		public int Candles { get; protected set; }
		public bool IsReady { get; protected set; }
		protected abstract int SmaCount { get; }
		protected int SmaBullish { get; set; }
		protected int SmaBearish { get; set; }
		protected bool AcBullish { get; set; }
		protected bool AcBearish { get; set; }

		public abstract void Process(ICandleMessage candle);

		/// <summary>
		/// Grades of the timeframe's bullish and bearish scores: 2 above 75% of its votes, 1 above 50%, otherwise 0.
		/// </summary>
		/// <param name="withAc">Whether the Accelerator Oscillator votes on this timeframe next to the SMA slopes.</param>
		public (int Bullish, int Bearish) Grades(bool withAc)
		{
			var voters = SmaCount + (withAc ? 1 : 0);
			var bullish = SmaBullish + (withAc && AcBullish ? 1 : 0);
			var bearish = SmaBearish + (withAc && AcBearish ? 1 : 0);

			return (Grade(bullish, voters), Grade(bearish, voters));
		}

		private static int Grade(int votes, int voters)
			=> votes * 100 > 75 * voters ? 2 : votes * 100 > 50 * voters ? 1 : 0;
	}

	/// <summary>
	/// One timeframe of the README computed in the strategy's own number type (decimal in C#, double in Python), so a
	/// slope or an oscillator step that is exactly flat resolves the same way in the model as in the strategy.
	/// </summary>
	private sealed class FiveMaFrame<T>(int[] periods) : FiveMaFrame
		where T : struct, INumber<T>
	{
		private readonly List<T> _closes = [];
		private readonly List<T> _medians = [];
		private readonly List<T> _ao = [];
		private readonly List<T> _ac = [];
		private T[] _previous;

		protected override int SmaCount => periods.Length;

		public override void Process(ICandleMessage candle)
		{
			Candles++;
			_closes.Add(T.CreateChecked(candle.ClosePrice));
			_medians.Add((T.CreateChecked(candle.HighPrice) + T.CreateChecked(candle.LowPrice)) / T.CreateChecked(2));

			// Accelerator Oscillator: AO = SMA5 - SMA34 of the median price, AC = AO - SMA5 of AO.
			if (_medians.Count >= 34)
			{
				var ao = Mean(_medians, 5) - Mean(_medians, 34);
				_ao.Add(ao);

				if (_ao.Count >= 5)
					_ac.Add(ao - Mean(_ao, 5));
			}

			if (_closes.Count < periods.Max())
				return;

			var smas = periods.Select(period => Mean(_closes, period)).ToArray();

			if (_previous is T[] previous)
			{
				SmaBullish = smas.Where((sma, i) => sma > previous[i]).Count();
				SmaBearish = smas.Where((sma, i) => sma < previous[i]).Count();

				var n = _ac.Count;
				AcBullish = n >= 4 && _ac[n - 4] < _ac[n - 3] && _ac[n - 3] < _ac[n - 2] && _ac[n - 2] < _ac[n - 1];
				AcBearish = n >= 4 && _ac[n - 4] > _ac[n - 3] && _ac[n - 3] > _ac[n - 2] && _ac[n - 2] > _ac[n - 1];
				IsReady = true;
			}

			_previous = smas;
		}

		private static T Mean(List<T> values, int count)
		{
			var sum = T.Zero;

			for (var i = values.Count - count; i < values.Count; i++)
				sum += values[i];

			return sum / T.CreateChecked(count);
		}
	}

	private const string RichKohonenMap = "4207_Rich_Kohonen_Map";

	private static readonly string[] _richKohonenMapNames = ["buy", "sell", "hold"];

	/// <summary>
	/// Independent model of the README: a seven-element vector of the latest open and Tom DeMark projections
	/// of the five candles before it, the action of the map holding the nearest prototype, and training that
	/// only adds the previous vector to the map its open-to-open move selects.
	/// </summary>
	private sealed class RichKohonenModel
	{
		public const int VectorSize = 7;
		public const int FileSize = 45000 * VectorSize * sizeof(double);
		public static readonly int[] Capacities = [10000, 10000, 25000];

		private static readonly int[] _decisions = [1, -1, 0];

		private readonly List<ICandleMessage> _window = [];

		public RichKohonenModel(List<double[]>[] maps)
		{
			Maps = maps;
			Loaded = [.. maps.Select(map => map.Count)];
		}

		public List<double[]>[] Maps { get; }
		public int[] Loaded { get; }
		public int[] Samples { get; } = new int[3];
		public int[] Decisions { get; } = new int[3];
		public int LoadedNearest { get; private set; }

		/// <summary>
		/// Hold samples whose move was smaller than MinPips.
		/// </summary>
		public int QuietHolds { get; private set; }

		/// <summary>
		/// Hold samples whose move was beyond MaxPips.
		/// </summary>
		public int SpikeHolds { get; private set; }

		/// <summary>
		/// Samples whose move was exactly +MinPips, -MinPips, +MaxPips and -MaxPips.
		/// </summary>
		public int[] BoundHits { get; } = new int[4];

		/// <summary>
		/// Feeds a finished candle and returns the decision (1 buy, -1 sell, 0 hold) once seven candles are known.
		/// </summary>
		public int? Add(ICandleMessage candle, decimal step, decimal minPips, decimal maxPips)
		{
			_window.Add(candle);

			if (_window.Count > 7)
				_window.RemoveAt(0);

			if (_window.Count < 7)
				return null;

			var decision = Classify(Vector(0));
			var move = (_window[^1].OpenPrice - _window[^2].OpenPrice) / step;
			var map = move >= minPips && move <= maxPips ? 0 : move <= -minPips && move >= -maxPips ? 1 : 2;

			Samples[map]++;

			if (move == minPips)
				BoundHits[0]++;
			else if (move == -minPips)
				BoundHits[1]++;

			if (move == maxPips)
				BoundHits[2]++;
			else if (move == -maxPips)
				BoundHits[3]++;

			if (map == 2)
			{
				if (Math.Abs(move) < minPips)
					QuietHolds++;
				else
					SpikeHolds++;
			}

			if (Maps[map].Count < Capacities[map])
				Maps[map].Add(Vector(1));

			return decision;
		}

		private double[] Vector(int offset)
		{
			var last = _window.Count - 1 - offset;
			var first = last - 5;
			double pivot = 0, projectedHigh = 0, projectedLow = 0;
			var high = decimal.MinValue;
			var low = decimal.MaxValue;

			for (var i = first; i < last; i++)
			{
				var candle = _window[i];
				var (p, h, l) = DeMark(candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice);
				pivot += (double)p;
				projectedHigh += (double)h;
				projectedLow += (double)l;
				high = Math.Max(high, candle.HighPrice);
				low = Math.Min(low, candle.LowPrice);
			}

			var (barPivot, barHigh, barLow) = DeMark(_window[first].OpenPrice, high, low, _window[last - 1].ClosePrice);

			return [(double)_window[last].OpenPrice, pivot / 5d, projectedHigh / 5d, projectedLow / 5d, (double)barPivot, (double)barHigh, (double)barLow];
		}

		private static (decimal pivot, decimal high, decimal low) DeMark(decimal open, decimal high, decimal low, decimal close)
		{
			var x = close < open ? high + 2m * low + close : close > open ? 2m * high + low + close : high + low + 2m * close;
			return (x / 4m, x / 2m - low, x / 2m - high);
		}

		private int Classify(double[] vector)
		{
			var best = double.PositiveInfinity;
			var nearestMap = 2;
			var nearestLoaded = false;

			for (var map = 0; map < Maps.Length; map++)
			{
				for (var row = 0; row < Maps[map].Count; row++)
				{
					var prototype = Maps[map][row];
					var sum = 0d;

					for (var i = 0; i < VectorSize; i++)
						sum += (vector[i] - prototype[i]) * (vector[i] - prototype[i]);

					var distance = Math.Sqrt(sum);

					if (distance < best)
					{
						best = distance;
						nearestMap = map;
						nearestLoaded = row < Loaded[map];
					}
				}
			}

			Decisions[nearestMap]++;

			if (nearestLoaded)
				LoadedNearest++;

			return _decisions[nearestMap];
		}
	}

	private sealed class RichKohonenRun
	{
		public RichKohonenModel Model { get; init; }
		public List<string> MapWarnings { get; } = [];
		public decimal MinPips { get; set; }
		public decimal MaxPips { get; set; }
		public int Orders { get; set; }

		/// <summary>
		/// Finished candles at which the strategy held a long position.
		/// </summary>
		public int LongBars { get; set; }

		/// <summary>
		/// Finished candles at which the strategy held a short position.
		/// </summary>
		public int ShortBars { get; set; }
	}

	[TestMethod]
	[TestCategory("Shard07")]
	public async Task S4207_ClassifiesDeMarkVectorsAndSavesAppendOnlyMapsAsZeroPaddedMatrices()
	{
		var directory = CreateRichKohonenDirectory();

		try
		{
			var path = Path.Combine(directory, "rl.bin");
			var run = await ReplayRichKohonen(path, NewRichKohonenModel(), TimeSpan.FromDays(14), band: RichKohonenHalfPipBand);

			AreEqual(0, run.MapWarnings.Count, $"README: a missing map file is ignored. {string.Join(" ", run.MapWarnings)}");
			AssertRichKohonenFile(path, run.Model.Maps);
			IsTrue(run.Model.Samples.All(count => count > 0), "The archive must train the buy, the sell and the hold map.");
			IsTrue(run.Model.Decisions.All(count => count > 0), "The archive must reach buy, sell and hold decisions.");
		}
		finally
		{
			Directory.Delete(directory, true);
		}
	}

	[TestMethod]
	[TestCategory("Shard07")]
	public async Task S4207_PublishedBandTrainsAllThreeMapsAndTradesBothWaysFromAnEmptyMap()
	{
		var directory = CreateRichKohonenDirectory();

		try
		{
			var path = Path.Combine(directory, "rl.bin");
			var run = await ReplayRichKohonen(path, NewRichKohonenModel(), TimeSpan.FromDays(31), band: null);

			AreEqual(10000m, run.MinPips, "C# and Python publish the same MinPips.");
			AreEqual(100000m, run.MaxPips, "C# and Python publish the same MaxPips.");
			AreEqual(0, run.MapWarnings.Count, $"README: a missing map file is ignored. {string.Join(" ", run.MapWarnings)}");
			AssertRichKohonenFile(path, run.Model.Maps);
			IsTrue(run.Model.Samples.All(count => count > 0), "With the published MinPips and MaxPips the archive must train the buy, the sell and the hold map.");
			IsTrue(run.Model.QuietHolds > 0 && run.Model.SpikeHolds > 0, "README: a move below MinPips or beyond MaxPips trains the hold map, and the archive's hourly moves must reach both sides of the published band.");
			IsTrue(run.Model.Decisions[0] > 0 && run.Model.Decisions[1] > 0, $"From an empty map the published band must reach buy and sell decisions, got {string.Join("/", run.Model.Decisions)} buy/sell/hold decisions.");
			IsTrue(run.LongBars > 0 && run.ShortBars > 0, $"From an empty map the published band must hold long and short positions, got {run.LongBars} long and {run.ShortBars} short hours.");
		}
		finally
		{
			Directory.Delete(directory, true);
		}
	}

	[TestMethod]
	[TestCategory("Shard07")]
	public async Task S4207_MovesOnTheBandBoundsTrainTheBuyAndSellMaps()
	{
		var directory = CreateRichKohonenDirectory();

		try
		{
			var path = Path.Combine(directory, "rl.bin");
			// Within three days the hourly opens move exactly +-237.40 and +-255.80, and binary floating point
			// computes two of these moves just outside the band.
			var run = await ReplayRichKohonen(path, NewRichKohonenModel(), TimeSpan.FromDays(3), band: step => (237.40m / step, 255.80m / step));

			IsTrue(run.Model.BoundHits.All(count => count > 0), $"The archive must move exactly +MinPips, -MinPips, +MaxPips and -MaxPips, got {string.Join("/", run.Model.BoundHits)}.");
			AreEqual(0, run.MapWarnings.Count, $"README: a missing map file is ignored. {string.Join(" ", run.MapWarnings)}");
			AssertRichKohonenFile(path, run.Model.Maps);
		}
		finally
		{
			Directory.Delete(directory, true);
		}
	}

	[TestMethod]
	[TestCategory("Shard07")]
	public async Task S4207_LoadsTheSavedMatricesAndResumesTrainingAfterTheirNonEmptyRows()
	{
		var directory = CreateRichKohonenDirectory();

		try
		{
			var path = Path.Combine(directory, "rl.bin");
			var first = await ReplayRichKohonen(path, NewRichKohonenModel(), TimeSpan.FromDays(7), band: RichKohonenHalfPipBand);
			AssertRichKohonenFile(path, first.Model.Maps);

			var saved = ReadRichKohonenFile(path);
			var second = await ReplayRichKohonen(path, new RichKohonenModel([.. saved.Select(map => map.ToList())]), TimeSpan.FromDays(14), band: RichKohonenHalfPipBand);

			AreEqual(0, second.MapWarnings.Count, $"A file in the README layout must load. {string.Join(" ", second.MapWarnings)}");
			AssertRichKohonenFile(path, second.Model.Maps);

			for (var map = 0; map < saved.Length; map++)
				IsTrue(second.Model.Maps[map].Count > saved[map].Count, $"README: training resumes after the {saved[map].Count} non-empty rows of the loaded {_richKohonenMapNames[map]} matrix.");

			IsTrue(second.Model.LoadedNearest > 0, "The loaded prototypes must take part in the classification.");
		}
		finally
		{
			Directory.Delete(directory, true);
		}
	}

	[TestMethod]
	[TestCategory("Shard07")]
	public async Task S4207_StartsFromEmptyMapsWhenTheFileIsShorterThanTheThreeMatrices()
	{
		var directory = CreateRichKohonenDirectory();

		try
		{
			var path = Path.Combine(directory, "rl.bin");
			var truncated = new byte[RichKohonenModel.FileSize - sizeof(double)];

			for (var offset = 0; offset < truncated.Length; offset += sizeof(double))
				BinaryPrimitives.WriteDoubleLittleEndian(truncated.AsSpan(offset), 1d);

			await File.WriteAllBytesAsync(path, truncated, CancellationToken);

			var run = await ReplayRichKohonen(path, NewRichKohonenModel(), TimeSpan.FromDays(7), band: RichKohonenHalfPipBand);

			IsTrue(run.MapWarnings.Count > 0, "A map file shorter than the three matrices cannot be loaded, and the strategy must say so.");
			AssertRichKohonenFile(path, run.Model.Maps);
		}
		finally
		{
			Directory.Delete(directory, true);
		}
	}

	private static RichKohonenModel NewRichKohonenModel()
		=> new([[], [], []]);

	/// <summary>
	/// Half-pip bounds: whole-pip moves never sit on a bound, and a band of about 200 price units trains all three maps.
	/// </summary>
	private static (decimal min, decimal max) RichKohonenHalfPipBand(decimal step)
		=> (0.5m, Math.Round(200m / step) + 0.5m);

	private static string CreateRichKohonenDirectory()
	{
		var directory = Path.Combine(AppContext.BaseDirectory, "RichKohonenMap", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		return directory;
	}

	/// <summary>
	/// Replays the archive with the maps persisted to <paramref name="path"/> and checks every order against the model.
	/// <paramref name="band"/> turns the instrument's price step into the MinPips and MaxPips to run with; null keeps the strategy's published values.
	/// </summary>
	private async Task<RichKohonenRun> ReplayRichKohonen(string path, RichKohonenModel model, TimeSpan duration, Func<decimal, (decimal min, decimal max)> band)
	{
		var run = new RichKohonenRun { Model = model };
		var step = 0m;
		var minPips = 0m;
		var maxPips = 0m;
		var lots = 0m;
		var bars = 0;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var violations = new List<string>();

		void violate(string message)
		{
			if (violations.Count < 12)
				violations.Add(message);
		}

		await Replay(RichKohonenMap, (strategy, _) =>
		{
			foreach (var id in new[] { "MinPips", "MaxPips", "TakeProfit", "StopLoss", "Lots", "Slippage", "MapPath", "EAName", "CandleType" })
				IsTrue(strategy.Parameters.ContainsKey(id), $"README lists the {id} parameter, but the strategy does not expose it.");

			AreEqual(TimeSpan.FromHours(1).TimeFrame(), strategy.Parameters["CandleType"].Value, "README: the default candle type is a 1-hour time frame.");
			AreEqual("rl.bin", strategy.Parameters["MapPath"].Value, "README: the maps are stored in rl.bin by default.");

			// An instrument capped at one unit keeps the target exposure at one unit whatever the replay does to the balance.
			strategy.Security.MaxVolume = 1m;
			step = strategy.Security.PriceStep is decimal priceStep && priceStep > 0m ? priceStep : 1m;

			if (band is null)
			{
				minPips = Convert.ToDecimal(strategy.Parameters["MinPips"].Value);
				maxPips = Convert.ToDecimal(strategy.Parameters["MaxPips"].Value);
			}
			else
			{
				(minPips, maxPips) = band(step);
				SetParam(strategy, "MinPips", minPips);
				SetParam(strategy, "MaxPips", maxPips);
			}

			run.MinPips = minPips;
			run.MaxPips = maxPips;
			SetParam(strategy, "MapPath", path);
			lots = Convert.ToDecimal(strategy.Parameters["Lots"].Value);

			strategy.Log += message =>
			{
				if (message.Level == LogLevels.Warning && message.Message.Contains(path, StringComparison.Ordinal))
					run.MapWarnings.Add(message.Message);
			};

			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished || strategy.ProcessState != ProcessStates.Started)
					return;

				if (expectedSide is not null)
					violate($"{candle.OpenTime:O}: the {expectedSide} {expectedVolume} order due on the previous bar was never registered.");

				expectedSide = null;
				bars++;

				if (strategy.Position > 0m)
					run.LongBars++;
				else if (strategy.Position < 0m)
					run.ShortBars++;

				if (model.Add(candle, step, minPips, maxPips) is not int decision)
					return;

				var difference = decision * RichKohonenVolume(strategy, lots) - strategy.Position;

				if (difference == 0m)
					return;

				expectedSide = difference > 0m ? Sides.Buy : Sides.Sell;
				expectedVolume = Math.Abs(difference);
				expectedOrders++;
			};

			strategy.OrderRegistering += order =>
			{
				run.Orders++;

				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
				{
					var due = expectedSide is null ? "no order" : $"{expectedSide} {expectedVolume} at market";
					violate($"{strategy.CurrentTime:O}: expected {due}, got {order.Side} {order.Volume} {order.Type}. README: the current vector (latest open and DeMark projections of the five candles before it) takes the action of the map holding the nearest prototype, and training only adds vectors, so every stored prototype stays as it was learned.");
				}

				expectedSide = null;
			};
		}, duration);

		TestContext.WriteLine($"python={IsPython} step={step} pips=[{minPips}, {maxPips}] days={duration.TotalDays}: bars={bars}, orders={run.Orders}, long/short bars={run.LongBars}/{run.ShortBars}, " +
			$"loaded={string.Join("/", model.Loaded)}, samples={string.Join("/", model.Samples)}, quiet/spike holds={model.QuietHolds}/{model.SpikeHolds}, bound hits={string.Join("/", model.BoundHits)}, " +
			$"decisions={string.Join("/", model.Decisions)}, loadedNearest={model.LoadedNearest}");

		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));
		AreEqual(expectedOrders, run.Orders, "Every change of the classified action must move the position to its target exposure, and nothing else may trade.");

		return run;
	}

	/// <summary>
	/// README: floor(balance / 50) / 10, the Lots fallback when that is zero, within the instrument's volume limits.
	/// </summary>
	private static decimal RichKohonenVolume(Strategy strategy, decimal lots)
	{
		var balance = strategy.Portfolio.CurrentValue ?? strategy.Portfolio.BeginValue ?? 0m;
		var volume = balance > 0m ? Math.Floor(balance / 50m) / 10m : 0m;

		if (volume <= 0m)
			volume = lots;

		var security = strategy.Security;

		if (security.MaxVolume is decimal max && max > 0m)
			volume = Math.Min(volume, max);

		if (security.MinVolume is decimal min && min > 0m)
			volume = Math.Max(volume, min);

		if (security.VolumeStep is decimal volumeStep && volumeStep > 0m)
			volume = Math.Floor(volume / volumeStep) * volumeStep;

		return volume > 0m ? volume : lots;
	}

	/// <summary>
	/// Reads the three matrices of the README layout and returns the non-empty rows of each.
	/// </summary>
	private static List<double[]>[] ReadRichKohonenFile(string path)
		=> ParseRichKohonenMatrices(ReadRichKohonenBytes(path));

	private static byte[] ReadRichKohonenBytes(string path)
	{
		IsTrue(File.Exists(path), $"README: the maps are persisted to MapPath ({path}).");

		var bytes = File.ReadAllBytes(path);
		AreEqual(RichKohonenModel.FileSize, bytes.Length, "README: the file holds the buy, sell and hold matrices one after another with no header, 10000, 10000 and 25000 rows of seven doubles (about 2.5 MB).");

		return bytes;
	}

	private static List<double[]>[] ParseRichKohonenMatrices(byte[] bytes)
	{
		var maps = new List<double[]>[] { [], [], [] };
		var offset = 0;

		for (var map = 0; map < maps.Length; map++)
		{
			for (var row = 0; row < RichKohonenModel.Capacities[map]; row++)
			{
				var vector = new double[RichKohonenModel.VectorSize];

				for (var i = 0; i < vector.Length; i++, offset += sizeof(double))
					vector[i] = BinaryPrimitives.ReadDoubleLittleEndian(bytes.AsSpan(offset));

				if (vector.Any(value => value != 0d))
					maps[map].Add(vector);
			}
		}

		return maps;
	}

	/// <summary>
	/// The file must hold exactly the model's prototypes, in the order they were learned, at the top of each matrix and zeros below them.
	/// </summary>
	private static void AssertRichKohonenFile(string path, List<double[]>[] expected)
	{
		var bytes = ReadRichKohonenBytes(path);
		var stored = ParseRichKohonenMatrices(bytes);

		for (var map = 0; map < expected.Length; map++)
			AreEqual(expected[map].Count, stored[map].Count, $"README: every trained vector is added to the {_richKohonenMapNames[map]} map, and the file keeps each map's rows.");

		var violations = new List<string>();
		var offset = 0;

		for (var map = 0; map < expected.Length; map++)
		{
			for (var row = 0; row < RichKohonenModel.Capacities[map]; row++)
			{
				var vector = row < expected[map].Count ? expected[map][row] : null;

				for (var i = 0; i < RichKohonenModel.VectorSize; i++, offset += sizeof(double))
				{
					var actual = BinaryPrimitives.ReadDoubleLittleEndian(bytes.AsSpan(offset));
					var wanted = vector?[i] ?? 0d;
					var matches = vector is null ? actual == 0d : Math.Abs(actual - wanted) <= 1e-9 * Math.Max(1d, Math.Abs(wanted));

					if (!matches && violations.Count < 12)
						violations.Add($"{_richKohonenMapNames[map]} row {row}, element {i}: the file holds {actual}, the README model expects {wanted}.");
				}
			}
		}

		IsTrue(violations.Count == 0, "README: element 0 is the open, elements 1-6 are DeMark projections of the five previous candles, and a learned row is never rewritten." + Environment.NewLine + string.Join(Environment.NewLine, violations));
	}
}

/// <summary>
/// The account a 2808 Multi Pair Closer run supervises. Its positions are opened by market orders sent to the
/// connector outside the strategy, and their floating profit is what the platform reports for each of them, which
/// is what the README has the strategy read. Every evaluation of the strategy is checked against the README rules
/// applied to what the fixture opened and to the profit the platform reported for it.
/// </summary>
/// <remarks>
/// Leg 0 is the strategy's own security, leg 1 the second packaged instrument. Minutes are counted from
/// <see cref="Origin"/>. A step scheduled at a minute runs on the first finished candle opened at or after that
/// minute, before the strategy evaluates it. A clock step runs on the first quote the fixture receives at or after
/// its minute and must fall between two evaluations. The fixture subscribes the quotes of leg 0 when it has clock
/// steps, and the quotes of a leg it trades that the strategy does not watch, so that leg has a book to fill against.
/// </remarks>
sealed class MultiPairCloserFixture
{
	/// <summary>Reason the strategy logs when the basket reached <c>ProfitTarget</c>.</summary>
	public const string ProfitTargetReason = "reached the profit target";

	/// <summary>Reason the strategy logs when the basket dropped below <c>-MaxLoss</c>.</summary>
	public const string MaxLossReason = "fell below the loss limit";

	/// <summary>
	/// Where the fixture counts its minutes from: 13:00 of the first replay day. The second packaged instrument has no
	/// market data before 12:31 that day, so no account position can be opened on it earlier.
	/// </summary>
	public static readonly DateTime Origin = Paths.HistoryBeginDate.AddHours(13);

	/// <summary>
	/// How long a run replays: an hour past <see cref="Origin"/>, which holds every scenario and its post-trade horizon.
	/// </summary>
	public static readonly TimeSpan ReplayDuration = Origin - Paths.HistoryBeginDate + TimeSpan.FromHours(1);

	private const string _basketPrefix = "Basket: ";
	private const string _unknown = "n/a";

	private static readonly Regex _closingLog = new(
		@"^Closing (?<id>\S+): basket (?<total>\S+) (?<reason>reached the profit target|fell below the loss limit), (?<side>sell|buy) (?<volume>\S+) at market \(slippage (?<slippage>\d+)\)\.$",
		RegexOptions.CultureInvariant);

	private readonly List<(TimeSpan At, Action Act)> _steps = [];
	private readonly List<(TimeSpan At, Action Act)> _clockSteps = [];
	private readonly List<DateTime> _clockStepTimes = [];
	private readonly HashSet<int> _openedLegs = [];
	private readonly List<Subscription> _feeds = [];
	private readonly List<Evaluation> _evaluations = [];
	private readonly List<string> _problems = [];
	private readonly Security[] _legs = new Security[2];
	private readonly decimal[] _volumes = new decimal[2];
	private readonly DateTime?[] _openedAt = new DateTime?[2];

	private Strategy _strategy;
	private Connector _connector;
	private int[] _watched;
	private decimal _profitTarget;
	private decimal _maxLoss;
	private int _minAgeSeconds;
	private int _slippage;
	private int _nextStep;
	private int _nextClockStep;
	private Evaluation _current;

	/// <summary>
	/// Every evaluation at which the README rules close something: when (the open time of the evaluated candle,
	/// counted from <see cref="Origin"/>), why and what.
	/// </summary>
	public IReadOnlyList<(TimeSpan At, string Reason, ExitOrder[] Orders)> Closings
		=> [.. _evaluations
			.Where(evaluation => evaluation.Closes.Count > 0)
			.Select(evaluation => (evaluation.CandleOpen - Origin, evaluation.Reason, evaluation.Closes.ToArray()))];

	/// <summary>
	/// The basket result of every evaluation and the floating profit reported for each leg at it: when (the open time
	/// of the evaluated candle, counted from <see cref="Origin"/>) and how much.
	/// </summary>
	public IReadOnlyList<(TimeSpan At, decimal? Basket, decimal?[] Reported)> Baskets
		=> [.. _evaluations.Select(evaluation => (evaluation.CandleOpen - Origin, evaluation.Basket, evaluation.Reported))];

	/// <summary>
	/// A basket of a long leg 0 and a short leg 1, both opened at minute 10.
	/// </summary>
	public static MultiPairCloserFixture LongShortBasket()
		=> new MultiPairCloserFixture()
			.Open(10, 0, Sides.Buy, 0.002m)
			.Open(10, 1, Sides.Sell, 20m);

	/// <summary>
	/// Makes the strategy watch both packaged instruments.
	/// </summary>
	public static void WatchBothLegs(Strategy strategy, Security secondary)
		=> strategy.Parameters["WatchedSymbols"].Value = $"{strategy.Security.Id},{secondary.Id}";

	/// <summary>
	/// Opens a position on a leg with a market order sent to the connector, not by the strategy, right before the
	/// strategy evaluates the candle of that minute.
	/// </summary>
	public MultiPairCloserFixture Open(int minute, int leg, Sides side, decimal volume)
	{
		_openedLegs.Add(leg);
		return AddStep(_steps, minute, () => OpenLeg(leg, side, volume));
	}

	/// <summary>
	/// Opens a position on a leg with a market order sent to the connector once the market clock reaches the minute,
	/// between two evaluations of the strategy.
	/// </summary>
	public MultiPairCloserFixture OpenOnClock(int minute, int leg, Sides side, decimal volume)
	{
		_openedLegs.Add(leg);
		return AddStep(_clockSteps, minute, () => OpenLeg(leg, side, volume));
	}

	/// <summary>
	/// Joins the run. Call it from the setup callback.
	/// </summary>
	public void Attach(Strategy strategy, Security secondary)
	{
		_strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
		_connector = (Connector)strategy.Connector;
		_legs[0] = strategy.Security;
		_legs[1] = secondary ?? throw new ArgumentNullException(nameof(secondary));

		strategy.ProcessStateChanged += OnProcessStateChanged;
		strategy.CandleReceived += OnCandle;
		strategy.OrderRegistering += OnOrderRegistering;
		strategy.OwnTradeReceived += OnOwnTrade;
		strategy.Log += OnLog;
		_connector.Level1Received += OnFeedQuote;
	}

	/// <summary>
	/// Every evaluation logged one summary with the profit of each watched instrument and the basket result, and
	/// closed exactly the positions the README rules close with market orders, each announced by a closing line with
	/// the slippage. Every clock step ran, and none of them at the moment of an evaluation.
	/// </summary>
	public void AssertFollowsReadme()
	{
		Assert.IsTrue(_problems.Count == 0, string.Join(Environment.NewLine, _problems));
		Assert.IsTrue(_evaluations.Count > 0, "The strategy never evaluated the basket.");
		Assert.AreEqual(_steps.Count, _nextStep, "The replay ended before every fixture step ran.");
		Assert.AreEqual(_clockSteps.Count, _nextClockStep, "The replay ended before every clock step ran.");

		foreach (var time in _clockStepTimes)
			Assert.IsFalse(_evaluations.Any(evaluation => evaluation.Time == time),
				$"A clock step ran at {time:O}, when the strategy evaluated the basket too; it has to fall between evaluations.");

		foreach (var evaluation in _evaluations)
		{
			var context = Describe(evaluation);
			var summaries = evaluation.Journal
				.Where(entry => entry.Log is not null && TryParseSummary(entry.Log, out _, out _))
				.ToArray();

			Assert.AreEqual(1, summaries.Length, $"README: every evaluation logs the profit per instrument and the basket result. {context}");
			AssertSummary(evaluation, summaries[0].Log, context);

			var orders = evaluation.Journal.Where(entry => entry.Order is not null).Select(entry => entry.Order.Value).ToArray();

			foreach (var order in orders)
				Assert.AreEqual<OrderTypes?>(OrderTypes.Market, order.Type, $"README: positions are flattened with BuyMarket / SellMarket, and {Format(order)} is not a market order. {context}");

			Assert.AreEqual(
				string.Join(", ", evaluation.Closes.Select(Format)),
				string.Join(", ", orders.Select(Format)),
				$"README: the basket closes when its floating profit reaches ProfitTarget or drops below -MaxLoss, and only positions older than MinAgeSeconds. {context}");

			for (var index = 0; index < evaluation.Journal.Count; index++)
			{
				if (evaluation.Journal[index].Order is not ExitOrder order)
					continue;

				var match = FindAnnouncement(evaluation.Journal, index);

				Assert.IsTrue(match.Success, $"The exit {Format(order)} was not announced by a closing line. {context}");
				Assert.AreEqual(order.SecurityId, match.Groups["id"].Value, context);
				Assert.AreEqual(order.Side == Sides.Sell ? "sell" : "buy", match.Groups["side"].Value, context);
				Assert.AreEqual(order.Volume, ParseNumber(match.Groups["volume"].Value), context);
				Assert.AreEqual(evaluation.Basket, (decimal?)ParseNumber(match.Groups["total"].Value), context);
				Assert.AreEqual(evaluation.Reason, match.Groups["reason"].Value, context);
				Assert.AreEqual(_slippage, int.Parse(match.Groups["slippage"].Value, CultureInfo.InvariantCulture), $"README: Slippage is logged. {context}");
			}
		}
	}

	/// <summary>
	/// The README rules closed positions exactly once: on the candle opened at the given minute, for the given
	/// reason, with the given number of exit orders.
	/// </summary>
	public void AssertSingleClosing(int minute, string reason, int orders)
		=> AssertSingleClosing(TimeSpan.FromMinutes(minute), reason, orders);

	/// <summary>
	/// The README rules closed positions exactly once: on the candle opened at the given offset from
	/// <see cref="Origin"/>, for the given reason or for either one when it is <see langword="null"/>, with the given
	/// number of exit orders.
	/// </summary>
	public void AssertSingleClosing(TimeSpan at, string reason, int orders)
	{
		var closings = Closings;
		var trace = string.Join("; ", closings.Select(closing => $"{closing.At} {closing.Reason}: {string.Join(", ", closing.Orders.Select(Format))}"));

		Assert.AreEqual(1, closings.Count, $"Closings: {trace}.");
		Assert.AreEqual(at, closings[0].At, $"Closings: {trace}.");

		if (reason is not null)
			Assert.AreEqual(reason, closings[0].Reason, $"Closings: {trace}.");

		Assert.AreEqual(orders, closings[0].Orders.Length, $"Closings: {trace}.");
	}

	private static bool TryParseSummary(string text, out (string Id, string Value)[] parts, out string basket)
	{
		parts = null;
		basket = null;

		var pieces = text.Split("; ");

		if (!pieces[^1].StartsWith(_basketPrefix, StringComparison.Ordinal))
			return false;

		var parsed = new (string Id, string Value)[pieces.Length - 1];

		for (var index = 0; index < parsed.Length; index++)
		{
			var separator = pieces[index].IndexOf(": ", StringComparison.Ordinal);

			if (separator <= 0)
				return false;

			parsed[index] = (pieces[index][..separator], pieces[index][(separator + 2)..]);
		}

		parts = parsed;
		basket = pieces[^1][_basketPrefix.Length..];
		return true;
	}

	// The closing line logged after the previous order and nearest to this one.
	private static Match FindAnnouncement(List<JournalEntry> journal, int orderIndex)
	{
		for (var index = orderIndex - 1; index >= 0 && journal[index].Order is null; index--)
		{
			var match = _closingLog.Match(journal[index].Log);

			if (match.Success)
				return match;
		}

		return Match.Empty;
	}

	private static decimal? ParseValue(string text)
		=> text == _unknown ? null : ParseNumber(text);

	private static decimal ParseNumber(string text)
		=> decimal.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);

	private static string Format(ExitOrder order)
		=> $"{order.Side} {order.Volume.ToString(CultureInfo.InvariantCulture)} {order.SecurityId} {order.Type}";

	private MultiPairCloserFixture AddStep(List<(TimeSpan At, Action Act)> steps, int minute, Action act)
	{
		var at = TimeSpan.FromMinutes(minute);

		if (steps.Count > 0 && at < steps[^1].At)
			throw new ArgumentException("Fixture steps are added in time order.", nameof(minute));

		steps.Add((at, act));
		return this;
	}

	private void OnProcessStateChanged(IStrategy strategy)
	{
		if (strategy.ProcessState != ProcessStates.Started || _watched is not null)
			return;

		ReadSettings();
		SubscribeFeeds();
	}

	// A leg outside the watch list has no book unless its quotes are subscribed, and finished-only candles move the
	// market clock only when they close; quotes come every few seconds and never share the strategy's subscriptions.
	private void SubscribeFeeds()
	{
		var quoted = _openedLegs.Where(leg => !_watched.Contains(leg)).ToHashSet();

		if (_clockSteps.Count > 0)
			quoted.Add(0);

		foreach (var leg in quoted)
		{
			var feed = new Subscription(DataType.Level1, _legs[leg]);

			_feeds.Add(feed);
			_connector.Subscribe(feed);
		}
	}

	private void OnFeedQuote(Subscription subscription, Level1ChangeMessage quote)
	{
		if (!_feeds.Contains(subscription) || _strategy.ProcessState != ProcessStates.Started)
			return;

		var now = _strategy.CurrentTime;

		while (_nextClockStep < _clockSteps.Count && _clockSteps[_nextClockStep].At <= now - Origin)
		{
			_clockStepTimes.Add(now);
			_clockSteps[_nextClockStep++].Act();
		}
	}

	private void OnCandle(Subscription subscription, ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || _strategy.ProcessState != ProcessStates.Started)
			return;

		if (_watched is null)
			ReadSettings();

		var offset = candle.OpenTime - Origin;

		while (_nextStep < _steps.Count && _steps[_nextStep].At <= offset)
			_steps[_nextStep++].Act();

		_current = Evaluate(candle.OpenTime);
		_evaluations.Add(_current);
	}

	private void ReadSettings()
	{
		var parameters = _strategy.Parameters;

		_profitTarget = Convert.ToDecimal(parameters["ProfitTarget"].Value, CultureInfo.InvariantCulture);
		_maxLoss = Convert.ToDecimal(parameters["MaxLoss"].Value, CultureInfo.InvariantCulture);
		_minAgeSeconds = Convert.ToInt32(parameters["MinAgeSeconds"].Value, CultureInfo.InvariantCulture);
		_slippage = Convert.ToInt32(parameters["Slippage"].Value, CultureInfo.InvariantCulture);

		var ids = ((string)parameters["WatchedSymbols"].Value ?? string.Empty)
			.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		// An empty list means the strategy's own security, which is leg 0.
		_watched = ids.Length == 0 ? [0] : [.. ids.Select(LegOf)];

		if (_watched.Any(leg => leg < 0))
			_problems.Add($"The fixture holds only {_legs[0].Id} and {_legs[1].Id}, the strategy watches {string.Join(",", ids)}.");
	}

	// The README rules applied to the account as the fixture made it, before the strategy acts on this candle.
	private Evaluation Evaluate(DateTime candleOpen)
	{
		var now = _strategy.CurrentTime;
		var reported = Enumerable.Range(0, _legs.Length).Select(ReadReportedProfit).ToArray();
		var evaluation = new Evaluation(now, candleOpen, [.. _volumes], reported, [.. _openedAt]);
		decimal? basket = 0m;
		var anyOpen = false;

		foreach (var leg in _watched.Where(leg => leg >= 0))
		{
			if (_volumes[leg] == 0m)
			{
				evaluation.Profits.Add(0m);
				continue;
			}

			anyOpen = true;
			evaluation.Profits.Add(reported[leg]);
			basket += reported[leg];
		}

		evaluation.Basket = basket;

		if (!anyOpen || basket is not decimal total)
			return evaluation;

		if (total >= _profitTarget)
			evaluation.Reason = ProfitTargetReason;
		else if (total < -_maxLoss)
			evaluation.Reason = MaxLossReason;
		else
			return evaluation;

		foreach (var leg in _watched.Where(leg => leg >= 0))
		{
			if (_volumes[leg] == 0m || (now - _openedAt[leg].Value).TotalSeconds < _minAgeSeconds)
				continue;

			evaluation.Closes.Add(new(_legs[leg].Id, _volumes[leg] > 0m ? Sides.Sell : Sides.Buy, Math.Abs(_volumes[leg]), OrderTypes.Market));
		}

		return evaluation;
	}

	private void OpenLeg(int leg, Sides side, decimal volume)
	{
		// Tagged with an owner of its own, so the strategy never takes the order for an unclaimed one of its own.
		var order = new Order
		{
			Security = _legs[leg],
			Portfolio = _strategy.Portfolio,
			Side = side,
			Volume = volume,
			Type = OrderTypes.Market,
			UserOrderId = "account",
		};

		_connector.RegisterOrder(order);

		// The replay fills a market order while it is being registered, so the position exists from now on.
		if (order.State != OrderStates.Done || order.Balance != 0m)
		{
			_problems.Add($"The account order {side} {volume} {_legs[leg].Id} at {_strategy.CurrentTime:O} did not fill at once: state {order.State}, balance {order.Balance}.");
			return;
		}

		Apply(leg, side == Sides.Buy ? volume : -volume);
	}

	// The floating profit the platform reports for the account's open position on a leg, summed as the README has the
	// strategy read it; one missing value leaves the sum unknown.
	private decimal? ReadReportedProfit(int leg)
	{
		decimal? profit = 0m;

		foreach (var position in _connector.Positions)
		{
			if (!position.StrategyId.IsEmpty() || !position.PortfolioName.EqualsIgnoreCase(_strategy.Portfolio.Name)
				|| position.Security?.Id.EqualsIgnoreCase(_legs[leg].Id) != true || position.CurrentValue is not decimal value || value == 0m)
				continue;

			profit += position.UnrealizedPnL;
		}

		return profit;
	}

	private void OnOwnTrade(Subscription subscription, MyTrade trade)
	{
		var leg = LegOf(trade.Order.Security?.Id);

		if (leg < 0)
		{
			_problems.Add($"The strategy traded {trade.Order.Security?.Id}, which the fixture does not hold.");
			return;
		}

		var volume = trade.Trade.Volume;
		Apply(leg, trade.Order.Side == Sides.Buy ? volume : -volume);
	}

	private void OnOrderRegistering(Order order)
	{
		if (_current is null)
		{
			_problems.Add($"The strategy sent {order.Side} {order.Volume} {order.Security?.Id} before its first evaluation.");
			return;
		}

		_current.Journal.Add(new(null, new ExitOrder(order.Security?.Id, order.Side, order.Volume, order.Type)));
	}

	private void OnLog(LogMessage message)
	{
		if (message.Level == LogLevels.Info)
			_current?.Journal.Add(new(message.Message, null));
	}

	// A position is first seen when it turns non-zero, and forgotten when it is flat again.
	private void Apply(int leg, decimal delta)
	{
		var before = _volumes[leg];
		_volumes[leg] = before + delta;

		if (_volumes[leg] == 0m)
			_openedAt[leg] = null;
		else if (before == 0m)
			_openedAt[leg] = _strategy.CurrentTime;
	}

	private int LegOf(string securityId)
		=> Array.FindIndex(_legs, security => security is not null && security.Id.EqualsIgnoreCase(securityId));

	private void AssertSummary(Evaluation evaluation, string text, string context)
	{
		TryParseSummary(text, out var parts, out var basket);

		Assert.AreEqual(evaluation.Profits.Count, parts.Length, $"The summary names every watched instrument once: '{text}'. {context}");

		var watched = _watched.Where(leg => leg >= 0).ToArray();

		for (var index = 0; index < parts.Length; index++)
		{
			Assert.AreEqual(_legs[watched[index]].Id, parts[index].Id, $"Summary '{text}'. {context}");
			Assert.AreEqual(evaluation.Profits[index], ParseValue(parts[index].Value), $"Profit of {parts[index].Id} in '{text}'. {context}");
		}

		Assert.AreEqual(evaluation.Basket, ParseValue(basket), $"Basket result in '{text}'. {context}");
	}

	private string Describe(Evaluation evaluation)
	{
		var legs = Enumerable.Range(0, _legs.Length).Select(leg =>
			$"{_legs[leg]?.Id}: volume {evaluation.Volumes[leg]}, reported {evaluation.Reported[leg]?.ToString() ?? _unknown}, opened {evaluation.OpenedAt[leg]:O}");

		var journal = evaluation.Journal.Select(entry => entry.Log ?? $"order {Format(entry.Order.Value)}");

		return $"Evaluation at {evaluation.Time:O} (candle {evaluation.CandleOpen:O}); {string.Join("; ", legs)}; journal: [{string.Join(" | ", journal)}].";
	}

	/// <summary>
	/// An exit order: the instrument, its side, its volume and its type.
	/// </summary>
	public readonly record struct ExitOrder(string SecurityId, Sides Side, decimal Volume, OrderTypes? Type);

	private sealed record JournalEntry(string Log, ExitOrder? Order);

	private sealed class Evaluation(DateTime time, DateTime candleOpen, decimal[] volumes, decimal?[] reported, DateTime?[] openedAt)
	{
		public DateTime Time { get; } = time;
		public DateTime CandleOpen { get; } = candleOpen;
		public decimal[] Volumes { get; } = volumes;
		public decimal?[] Reported { get; } = reported;
		public DateTime?[] OpenedAt { get; } = openedAt;
		public List<decimal?> Profits { get; } = [];
		public decimal? Basket { get; set; }
		public string Reason { get; set; }
		public List<ExitOrder> Closes { get; } = [];
		public List<JournalEntry> Journal { get; } = [];
	}
}

/// <summary>
/// Works out, from the bars the strategy receives, every order the Synthetic Lending Rates README
/// calls for, checks each order the strategy submits against it and records the fills of each leg.
/// </summary>
/// <remarks>
/// Each rate is the latest close of its series, however long ago the series printed. A period is decided
/// once either leg shows a later one, and a leg with no bar in that period traded nothing in it. Traded
/// value and sizes count each leg in its lots.
/// </remarks>
sealed class SyntheticLendingRatesOracle
{
	private readonly SecurityId _fundingId;
	private readonly SecurityId _lendingId;
	private readonly SecurityId _derivativeId;
	private readonly SecurityId _onChainId;
	private readonly List<(string SecurityId, Sides Side, decimal Volume)> _expected = [];
	private readonly HashSet<(string SecurityId, Sides Side)> _filled = [];
	private readonly List<string> _violations = [];
	private Strategy _strategy;
	private ICandleMessage _funding;
	private ICandleMessage _lending;
	private ICandleMessage _derivative;
	private ICandleMessage _onChain;
	private DateTime? _period;
	private DateTime? _decided;
	private int _heldBars;

	/// <summary>
	/// Creates the oracle for the given inputs and levels; <see cref="Attach"/> hands the same values to the strategy.
	/// </summary>
	public SyntheticLendingRatesOracle(Security fundingRate, Security lendingRate, Security derivativeLeg, Security onChainLeg,
		decimal entryThreshold, decimal exitThreshold, decimal spreadCap, decimal minLegTurnover, decimal legNotional, int rebalanceBars)
	{
		FundingRate = fundingRate ?? throw new ArgumentNullException(nameof(fundingRate));
		LendingRate = lendingRate ?? throw new ArgumentNullException(nameof(lendingRate));
		DerivativeLeg = derivativeLeg ?? throw new ArgumentNullException(nameof(derivativeLeg));
		OnChainLeg = onChainLeg ?? throw new ArgumentNullException(nameof(onChainLeg));
		EntryThreshold = entryThreshold;
		ExitThreshold = exitThreshold;
		SpreadCap = spreadCap;
		MinLegTurnover = minLegTurnover;
		LegNotional = legNotional;
		RebalanceBars = rebalanceBars;

		_fundingId = fundingRate.Id.ToSecurityId();
		_lendingId = lendingRate.Id.ToSecurityId();
		_derivativeId = derivativeLeg.Id.ToSecurityId();
		_onChainId = onChainLeg.Id.ToSecurityId();
	}

	/// <summary>Stream standing in for the synthetic lending rate from perpetual funding.</summary>
	public Security FundingRate { get; }

	/// <summary>Stream standing in for the on-chain lending yield.</summary>
	public Security LendingRate { get; }

	/// <summary>Leg bought to lend and sold to borrow in the derivative market; the strategy gets it as its Security.</summary>
	public Security DerivativeLeg { get; }

	/// <summary>Leg bought to lend and sold to borrow on-chain.</summary>
	public Security OnChainLeg { get; }

	/// <summary>Spread above which a pair opens.</summary>
	public decimal EntryThreshold { get; }

	/// <summary>Spread in favour of the open pair at or below which it has reverted.</summary>
	public decimal ExitThreshold { get; }

	/// <summary>Spread at or above which no pair is held.</summary>
	public decimal SpreadCap { get; }

	/// <summary>Value each leg must trade in a period, counted in its lots.</summary>
	public decimal MinLegTurnover { get; }

	/// <summary>Value held in each leg, counted in its lots.</summary>
	public decimal LegNotional { get; }

	/// <summary>Held periods between resizings.</summary>
	public int RebalanceBars { get; }

	/// <summary>Periods on which the rules were evaluated: both rates known, trading allowed and no order working.</summary>
	public int DecidedPeriods { get; private set; }

	/// <summary>Orders the rules called for.</summary>
	public int ExpectedOrders { get; private set; }

	/// <summary>Orders the strategy submitted.</summary>
	public int SubmittedOrders { get; private set; }

	/// <summary>Pairs opened lending in the derivative market: its leg bought, the on-chain leg sold.</summary>
	public int DerivativeLendingEntries { get; private set; }

	/// <summary>Pairs opened lending on-chain: the on-chain leg bought, the derivative leg sold.</summary>
	public int OnChainLendingEntries { get; private set; }

	/// <summary>Pairs closed, for whatever reason.</summary>
	public int Exits { get; private set; }

	/// <summary>Pairs closed only because the spread reverted to the exit level.</summary>
	public int ReversionExits { get; private set; }

	/// <summary>Pairs closed only because the spread reached the cap.</summary>
	public int CapExits { get; private set; }

	/// <summary>Pairs closed only because a leg traded less value than the liquidity stop allows.</summary>
	public int LiquidityExits { get; private set; }

	/// <summary>Liquidity exits on a period in which a leg traded nothing at all.</summary>
	public int NoTradeExits { get; private set; }

	/// <summary>Flat periods on liquid legs where only the spread cap kept a pair from opening.</summary>
	public int CapBlockedEntries { get; private set; }

	/// <summary>Flat periods with a qualifying spread where only liquidity kept a pair from opening.</summary>
	public int LiquidityBlockedEntries { get; private set; }

	/// <summary>Flat periods that met every entry rule while trading was limited to reducing positions.</summary>
	public int ModeBlockedEntries { get; private set; }

	/// <summary>Held periods on which the legs were resized back to the leg notional.</summary>
	public int Rebalances { get; private set; }

	/// <summary>
	/// Levels on the spread of the packaged closes, BTC minus TON (BTC trades about 60,800-73,800 in
	/// March 2024, TON a few dollars): hourly pairs open above 67,000, revert at 64,000 and hit the
	/// cap at 72,000, and are resized every six held hours.
	/// </summary>
	public static SyntheticLendingRatesOracle SpreadFixture(Security fundingRate, Security lendingRate, Security derivativeLeg, Security onChainLeg, decimal minLegTurnover)
		=> new(fundingRate, lendingRate, derivativeLeg, onChainLeg, 67_000m, 64_000m, 72_000m, minLegTurnover, 10_000m, 6);

	/// <summary>
	/// Hands the inputs and levels to the strategy, the derivative leg as its Security, and starts following
	/// its bars, orders and fills.
	/// </summary>
	public void Attach(Strategy strategy)
	{
		_strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));

		strategy.Security = DerivativeLeg;
		Set("FundingRateSecurity", FundingRate);
		Set("LendingRateSecurity", LendingRate);
		Set("OnChainLegSecurity", OnChainLeg);
		Set("EntryThreshold", EntryThreshold);
		Set("ExitThreshold", ExitThreshold);
		Set("SpreadCap", SpreadCap);
		Set("MinLegTurnover", MinLegTurnover);
		Set("LegNotional", LegNotional);
		Set("RebalanceBars", RebalanceBars);

		strategy.CandleReceived += OnCandle;
		strategy.OrderRegistering += OnOrder;
		strategy.OwnTradeReceived += OnTrade;
	}

	/// <summary>
	/// Every order the rules called for was submitted as a market order of that side and size on that
	/// leg, and nothing else was submitted.
	/// </summary>
	public void AssertEveryOrderMatched()
	{
		SettleBar();

		Assert.IsTrue(_violations.Count == 0,
			$"{_violations.Count} deviation(s) from the rules. {Summary()}{Environment.NewLine}{string.Join(Environment.NewLine, _violations.Take(12))}");
		Assert.IsTrue(ExpectedOrders > 0, $"The fixture never called for an order. {Summary()}");
		Assert.AreEqual(ExpectedOrders, SubmittedOrders, $"Submitted orders differ from the orders the rules call for. {Summary()}");
	}

	/// <summary>
	/// Each leg was filled both buying and selling, so pairs were opened and closed in both venues.
	/// </summary>
	public void AssertBothLegsTraded()
	{
		foreach (var leg in new[] { DerivativeLeg, OnChainLeg })
		{
			foreach (var side in new[] { Sides.Buy, Sides.Sell })
				Assert.IsTrue(_filled.Contains((leg.Id, side)), $"No {side} fill on {leg.Id}. {Summary()}");
		}
	}

	/// <summary>
	/// The counters of the run.
	/// </summary>
	public string Summary()
		=> $"decided periods={DecidedPeriods}, orders expected/submitted={ExpectedOrders}/{SubmittedOrders}, " +
			$"entries lending derivative/on-chain={DerivativeLendingEntries}/{OnChainLendingEntries}, " +
			$"exits all/reverted/capped/illiquid/no trade={Exits}/{ReversionExits}/{CapExits}/{LiquidityExits}/{NoTradeExits}, " +
			$"entries blocked by cap/liquidity/mode={CapBlockedEntries}/{LiquidityBlockedEntries}/{ModeBlockedEntries}, rebalances={Rebalances}, " +
			$"fills={string.Join(", ", _filled.Select(fill => $"{fill.SecurityId} {fill.Side}"))}.";

	private static decimal Floor(decimal volume, Security leg)
		=> leg.VolumeStep is decimal step && step > 0m ? Math.Floor(volume / step) * step : volume;

	private static decimal Lot(Security leg)
		=> leg.Multiplier is decimal multiplier && multiplier > 0m ? multiplier : 1m;

	private static string Format(IEnumerable<(string SecurityId, Sides Side, decimal Volume)> orders)
		=> orders.Any() ? string.Join(", ", orders.Select(order => $"{order.SecurityId} {order.Side} {order.Volume}")) : "nothing";

	private void Set(string name, object value)
	{
		Assert.IsTrue(_strategy.Parameters.TryGetValue(name, out var parameter), $"The strategy has no '{name}' input.");
		parameter.Value = value;
	}

	private void OnCandle(Subscription subscription, ICandleMessage candle)
	{
		if (_strategy.ProcessState != ProcessStates.Started)
			return;

		var id = candle.SecurityId;

		// Bars come in time order: once a leg shows a later period, no stream adds anything to the pending one.
		if ((id == _derivativeId || id == _onChainId) && (_period is null || candle.OpenTime > _period))
		{
			if (_period is DateTime pending)
				Decide(pending);

			_period = candle.OpenTime;
		}

		// The same instrument may stand in for several roles; each keeps its latest bar, finished or not.
		if (id == _fundingId)
			_funding = candle;

		if (id == _lendingId)
			_lending = candle;

		if (id == _derivativeId)
			_derivative = candle;

		if (id == _onChainId)
			_onChain = candle;
	}

	private void Decide(DateTime period)
	{
		SettleBar();
		_decided = period;

		if (_funding is null || _lending is null)
			return;

		// Nothing is decided while trading is barred altogether or an earlier order is still working.
		if (!_strategy.IsFormedAndOnlineAndAllowTrading(StrategyTradingModes.ReducePositionOnly)
			|| _strategy.Orders.Any(order => order.State is not (OrderStates.Done or OrderStates.Failed)))
			return;

		DecidedPeriods++;

		var spread = _funding.ClosePrice - _lending.ClosePrice;
		var width = Math.Abs(spread);
		var derivativeTraded = _derivative is not null && _derivative.OpenTime == period;
		var onChainTraded = _onChain is not null && _onChain.OpenTime == period;
		var liquid = Value(_derivative, DerivativeLeg, derivativeTraded) >= MinLegTurnover && Value(_onChain, OnChainLeg, onChainTraded) >= MinLegTurnover;
		var derivativeHeld = _strategy.GetPositionValue(DerivativeLeg, _strategy.Portfolio) ?? 0m;
		var onChainHeld = _strategy.GetPositionValue(OnChainLeg, _strategy.Portfolio) ?? 0m;
		// Opening and resizing can enlarge a leg, so they need full trading rights; closing does not.
		var mayEnlarge = _strategy.IsFormedAndOnlineAndAllowTrading();

		if (derivativeHeld == 0m && onChainHeld == 0m)
		{
			if (width <= EntryThreshold)
				return;

			if (width >= SpreadCap)
			{
				if (liquid)
					CapBlockedEntries++;

				return;
			}

			if (!liquid)
			{
				LiquidityBlockedEntries++;
				return;
			}

			if (!mayEnlarge)
			{
				ModeBlockedEntries++;
				return;
			}

			var derivativeUnits = Units(DerivativeLeg, _derivative);
			var onChainUnits = Units(OnChainLeg, _onChain);

			if (derivativeUnits == 0m || onChainUnits == 0m)
				return;

			// The venue paying the higher rate is lent to: its leg is bought and the other leg is sold.
			if (spread > 0m)
			{
				Expect(DerivativeLeg, Sides.Buy, derivativeUnits);
				Expect(OnChainLeg, Sides.Sell, onChainUnits);
				DerivativeLendingEntries++;
			}
			else
			{
				Expect(DerivativeLeg, Sides.Sell, derivativeUnits);
				Expect(OnChainLeg, Sides.Buy, onChainUnits);
				OnChainLendingEntries++;
			}

			_heldBars = 0;
			return;
		}

		var derivativeLends = derivativeHeld > 0m || derivativeHeld == 0m && onChainHeld < 0m;
		var favour = derivativeLends ? spread : -spread;
		var reverted = favour <= ExitThreshold;
		var capped = width >= SpreadCap;

		if (reverted || capped || !liquid)
		{
			Exits++;

			if (reverted && !capped && liquid)
				ReversionExits++;
			else if (capped && !reverted && liquid)
				CapExits++;
			else if (!liquid && !reverted && !capped)
			{
				LiquidityExits++;

				if (!derivativeTraded || !onChainTraded)
					NoTradeExits++;
			}

			Close(DerivativeLeg, derivativeHeld);
			Close(OnChainLeg, onChainHeld);
			return;
		}

		if (++_heldBars < RebalanceBars || !mayEnlarge)
			return;

		_heldBars = 0;

		var derivativeTarget = Units(DerivativeLeg, _derivative);
		var onChainTarget = Units(OnChainLeg, _onChain);

		if (derivativeTarget == 0m || onChainTarget == 0m)
			return;

		var resized = Resize(DerivativeLeg, derivativeLends ? derivativeTarget : -derivativeTarget, derivativeHeld);
		resized |= Resize(OnChainLeg, derivativeLends ? -onChainTarget : onChainTarget, onChainHeld);

		if (resized)
			Rebalances++;
	}

	// Traded value of the leg's bar in the period, in its lots; a leg that did not trade in the period has none.
	private static decimal Value(ICandleMessage bar, Security leg, bool traded)
		=> traded ? bar.TotalVolume * bar.ClosePrice * Lot(leg) : 0m;

	private decimal Units(Security leg, ICandleMessage bar)
	{
		// The leg's last traded price, whichever period it is from.
		var lotValue = bar is null ? 0m : bar.ClosePrice * Lot(leg);

		if (lotValue <= 0m)
			return 0m;

		var units = Floor(LegNotional / lotValue, leg);
		return units >= (leg.MinVolume ?? 0m) ? units : 0m;
	}

	private bool Resize(Security leg, decimal target, decimal held)
	{
		var change = target - held;
		var volume = Floor(Math.Abs(change), leg);

		if (volume <= 0m || volume < (leg.MinVolume ?? 0m))
			return false;

		Expect(leg, change > 0m ? Sides.Buy : Sides.Sell, volume);
		return true;
	}

	private void Close(Security leg, decimal held)
	{
		if (held != 0m)
			Expect(leg, held > 0m ? Sides.Sell : Sides.Buy, Math.Abs(held));
	}

	private void Expect(Security leg, Sides side, decimal volume)
	{
		_expected.Add((leg.Id, side, volume));
		ExpectedOrders++;
	}

	private void OnOrder(Order order)
	{
		SubmittedOrders++;

		var index = _expected.FindIndex(expected => expected.SecurityId.EqualsIgnoreCase(order.Security?.Id) && expected.Side == order.Side && expected.Volume == order.Volume);

		if (index < 0 || order.Type != OrderTypes.Market)
		{
			_violations.Add($"{_strategy.CurrentTime:O}: {order.Type} {order.Security?.Id} {order.Side} {order.Volume} was not called for; expected {Format(_expected)}.");
			return;
		}

		_expected.RemoveAt(index);
	}

	private void OnTrade(Subscription subscription, MyTrade trade)
	{
		if (trade.Order?.Security?.Id is string id)
			_filled.Add((id, trade.Order.Side));
	}

	private void SettleBar()
	{
		if (_expected.Count == 0)
			return;

		_violations.Add($"{_decided:O}: called for but never submitted: {Format(_expected)}.");
		_expected.Clear();
	}
}

/// <summary>
/// Option contracts for the volatility risk premium example. The packaged archive holds no option market,
/// so the other packaged instrument is described as an option on the primary one and its own prices serve
/// as the option premiums: a mechanics fixture, not option market history.
/// </summary>
static class VolatilityRiskPremiumOptionFixture
{
	/// <summary>
	/// Quarterly expiration of the fixture contracts, 29 March 2024 08:00 UTC.
	/// </summary>
	public static readonly DateTime QuarterlyExpiry = new(2024, 3, 29, 8, 0, 0, DateTimeKind.Utc);

	/// <summary>
	/// Weekly expiration inside the first packaged week, 8 March 2024 08:00 UTC.
	/// </summary>
	public static readonly DateTime WeeklyExpiry = new(2024, 3, 8, 8, 0, 0, DateTimeKind.Utc);

	/// <summary>
	/// Expiration at 8 March 2024 09:00 UTC. The stand-in stream has no trade from 08:50 to 08:55, so on
	/// five-minute bars the last bar that closes before this expiration has no option price.
	/// </summary>
	public static readonly DateTime GapExpiry = new(2024, 3, 8, 9, 0, 0, DateTimeKind.Utc);

	/// <summary>
	/// Call strike above every packaged underlying price, so the call stays out of the money.
	/// </summary>
	public const decimal CallStrike = 90000m;

	/// <summary>
	/// Put strike below every packaged underlying price, so the put stays out of the money.
	/// </summary>
	public const decimal PutStrike = 45000m;

	/// <summary>
	/// Index-style contract multiplier: one contract covers 100 units of the underlying.
	/// </summary>
	public const decimal Multiplier = 100m;

	/// <summary>
	/// Describes <paramref name="stream"/> as an option on <paramref name="underlying"/>; the prices of
	/// <paramref name="stream"/> become the option premiums.
	/// </summary>
	/// <param name="stream">Packaged instrument whose prices stand in for the premiums.</param>
	/// <param name="underlying">Underlying instrument of the option.</param>
	/// <param name="type">Call or put.</param>
	/// <param name="strike">Strike price.</param>
	/// <param name="expiry">Exact expiration moment in UTC.</param>
	/// <returns><paramref name="stream"/>, now described as the option.</returns>
	public static Security Describe(Security stream, Security underlying, OptionTypes type, decimal strike, DateTime expiry)
	{
		ArgumentNullException.ThrowIfNull(stream);
		ArgumentNullException.ThrowIfNull(underlying);

		stream.Type = SecurityTypes.Option;
		stream.OptionType = type;
		stream.Strike = strike;
		stream.ExpiryDate = expiry;
		stream.UnderlyingSecurityId = underlying.Id;
		stream.Multiplier = Multiplier;

		return stream;
	}

	/// <summary>
	/// The quarterly out-of-the-money call the example sells with its default parameters.
	/// </summary>
	/// <param name="stream">Packaged instrument whose prices stand in for the premiums.</param>
	/// <param name="underlying">Underlying instrument of the option.</param>
	/// <returns><paramref name="stream"/>, now described as the call.</returns>
	public static Security QuarterlyCall(Security stream, Security underlying)
		=> Describe(stream, underlying, OptionTypes.Call, CallStrike, QuarterlyExpiry);
}

/// <summary>
/// Independent model of the WTI-Brent spread README, held against every order either leg receives. The spread is
/// the WTI close minus the Brent close of bars both legs finished at the same time, measured against the population
/// average and standard deviation of the last Lookback spreads. Beyond the entry z-score, however far, the grade
/// that is cheap against that average is bought and the expensive one sold for the same dollar amount. The pair
/// closes at the average, at the stop fixed at entry (the stop widening, in entry deviations, past the entry
/// spread), or once the earlier expiry of the two contracts is within the roll days, and no pair opens inside
/// that window.
/// </summary>
sealed class WtiBrentSpreadModel
{
	public const string EntryComment = "Spread entry";
	public const string AverageComment = "Spread at average";
	public const string StopComment = "Spread stop";
	public const string RollComment = "Contract roll";

	private const int _maxViolations = 12;

	private readonly Queue<decimal> _window = new();
	private readonly Queue<(Security Security, Sides Side, decimal Volume, string Comment)> _due = new();
	private readonly List<string> _violations = [];
	private readonly List<(DateTime Open, DateTime? Close)> _holdings = [];

	private Security _wti;
	private Security _brent;
	private SecurityId _wtiId;
	private SecurityId _brentId;
	private int _lookback;
	private decimal _entryZScore;
	private decimal _stopWidening;
	private int _rollDays;
	private DateTime? _wtiTime;
	private decimal _wtiClose;
	private DateTime? _brentTime;
	private decimal _brentClose;
	private DateTime? _pairTime;
	private decimal? _stopSpread;
	private int _blockedSign;
	private decimal _heldWti;
	private decimal _heldBrent;

	/// <summary>
	/// Bars both legs finished at the same time.
	/// </summary>
	public int Pairs { get; private set; }

	/// <summary>
	/// Pairs opened long WTI and short Brent.
	/// </summary>
	public int LongEntries { get; private set; }

	/// <summary>
	/// Pairs opened short WTI and long Brent.
	/// </summary>
	public int ShortEntries { get; private set; }

	/// <summary>
	/// Pairs opened with the z-score at least the stop widening beyond the entry threshold.
	/// </summary>
	public int DeepEntries { get; private set; }

	/// <summary>
	/// Pairs closed because the spread returned to its average.
	/// </summary>
	public int AverageExits { get; private set; }

	/// <summary>
	/// Pairs closed because the spread widened to the stop level fixed at entry.
	/// </summary>
	public int StopExits { get; private set; }

	/// <summary>
	/// Pairs closed because the earlier expiry came within the roll days.
	/// </summary>
	public int RollExits { get; private set; }

	/// <summary>
	/// Strategy time of the first roll exit.
	/// </summary>
	public DateTime? RollExitTime { get; private set; }

	/// <summary>
	/// Entry signals held back because the same side was stopped out and the spread has not come back inside
	/// the entry threshold since.
	/// </summary>
	public int StopBlockedSignals { get; private set; }

	/// <summary>
	/// Entry signals held back because the earlier expiry is within the roll days.
	/// </summary>
	public int RollBlockedSignals { get; private set; }

	/// <summary>
	/// Entry signals no Brent volume within the instrument limits could balance.
	/// </summary>
	public int UnsizedSignals { get; private set; }

	/// <summary>
	/// Orders the model called for.
	/// </summary>
	public int ExpectedOrders { get; private set; }

	/// <summary>
	/// Orders the strategy registered.
	/// </summary>
	public int Orders { get; private set; }

	/// <summary>
	/// Orders the strategy registered for the WTI leg.
	/// </summary>
	public int WtiOrders { get; private set; }

	/// <summary>
	/// Orders the strategy registered for the Brent leg.
	/// </summary>
	public int BrentOrders { get; private set; }

	/// <summary>
	/// Fills of the WTI leg.
	/// </summary>
	public int WtiTrades { get; private set; }

	/// <summary>
	/// Fills of the Brent leg.
	/// </summary>
	public int BrentTrades { get; private set; }

	/// <summary>
	/// When each pair was opened and, once it was, closed, by the strategy's clock.
	/// </summary>
	public IReadOnlyList<(DateTime Open, DateTime? Close)> Holdings => _holdings;

	/// <summary>
	/// Follows the strategy's candles, orders and fills. The strategy's parameters are read once it runs.
	/// </summary>
	public void Attach(Strategy strategy)
	{
		strategy.CandleReceived += (_, candle) => OnCandle(strategy, candle);
		strategy.OrderRegistering += order => OnOrder(strategy, order);
		strategy.OwnTradeReceived += (_, trade) => OnTrade(trade);
	}

	/// <summary>
	/// Every order was called for by the model and every order it called for was sent, and both legs were traded.
	/// </summary>
	public void AssertMatched()
	{
		Assert.IsTrue(_violations.Count == 0, string.Join(Environment.NewLine, _violations));
		Assert.AreEqual(0, _due.Count, $"Orders due on the last pair never reached the market: {string.Join(", ", _due.Select(order => Describe(order)))}.");
		Assert.AreEqual(ExpectedOrders, Orders, "Every order must be one the README calls for, and every one it calls for must be sent.");
		Assert.IsTrue(WtiOrders > 0 && BrentOrders > 0, $"README: the spread is traded by buying one grade and selling the other, so both legs need orders; WTI {WtiOrders}, Brent {BrentOrders}.");
		Assert.IsTrue(WtiTrades > 0 && BrentTrades > 0, $"Both legs must be filled; WTI {WtiTrades}, Brent {BrentTrades} fills.");
	}

	/// <inheritdoc />
	public override string ToString()
		=> $"pairs={Pairs}, entries long/short/deep={LongEntries}/{ShortEntries}/{DeepEntries}, exits average/stop/roll={AverageExits}/{StopExits}/{RollExits}, " +
			$"blocked stop/roll={StopBlockedSignals}/{RollBlockedSignals}, unsized={UnsizedSignals}, orders={Orders} of {ExpectedOrders} (WTI {WtiOrders}, Brent {BrentOrders}), " +
			$"fills WTI/Brent={WtiTrades}/{BrentTrades}";

	private void OnCandle(Strategy strategy, ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || strategy.ProcessState != ProcessStates.Started)
			return;

		Configure(strategy);

		if (_due.Count > 0)
		{
			Violate($"{strategy.CurrentTime:O}: {string.Join(", ", _due.Select(order => Describe(order)))} due on the previous pair never reached the market.");
			_due.Clear();
		}

		if (candle.SecurityId == _wtiId)
		{
			if (_wtiTime is DateTime last && candle.OpenTime <= last)
				return;

			_wtiTime = candle.OpenTime;
			_wtiClose = candle.ClosePrice;
		}
		else if (candle.SecurityId == _brentId)
		{
			if (_brentTime is DateTime last && candle.OpenTime <= last)
				return;

			_brentTime = candle.OpenTime;
			_brentClose = candle.ClosePrice;
		}
		else
		{
			Violate($"{candle.SecurityId}: only the WTI and the Brent contracts may feed the spread.");
			return;
		}

		// A bar only one leg finished is not a spread, and is not carried over to a later bar of the other leg.
		if (_wtiTime is not DateTime time || _brentTime != time || _pairTime == time)
			return;

		_pairTime = time;
		ProcessPair(strategy);
	}

	private void Configure(Strategy strategy)
	{
		if (_wti is not null)
			return;

		_wti = strategy.Security;
		_brent = (Security)strategy.Parameters["BrentSecurity"].Value;
		_wtiId = _wti.Id.ToSecurityId();
		_brentId = _brent.Id.ToSecurityId();
		_lookback = Convert.ToInt32(strategy.Parameters["Lookback"].Value);
		_entryZScore = Convert.ToDecimal(strategy.Parameters["EntryZScore"].Value);
		_stopWidening = Convert.ToDecimal(strategy.Parameters["StopWidening"].Value);
		_rollDays = Convert.ToInt32(strategy.Parameters["RollDays"].Value);
	}

	private void ProcessPair(Strategy strategy)
	{
		Pairs++;

		var spread = _wtiClose - _brentClose;
		_window.Enqueue(spread);

		if (_window.Count > _lookback)
			_window.Dequeue();

		if (_window.Count < _lookback)
			return;

		// Population statistics of the last Lookback spreads, the current one included.
		var average = _window.Sum() / _lookback;
		var deviation = (decimal)Math.Sqrt((double)(_window.Sum(value => (value - average) * (value - average)) / _lookback));
		var zScore = deviation == 0m ? 0m : (spread - average) / deviation;

		if (_blockedSign != 0 && _blockedSign * zScore <= _entryZScore)
			_blockedSign = 0;

		var now = strategy.CurrentTime;
		var wtiPosition = strategy.Position;
		var brentPosition = strategy.GetPositionValue(_brent, strategy.Portfolio) ?? 0m;

		if (wtiPosition != 0m || brentPosition != 0m)
			ExpectExit(now, spread, zScore, wtiPosition, brentPosition);
		else
			ExpectEntry(strategy.Volume, now, spread, deviation, zScore);
	}

	private void ExpectExit(DateTime now, decimal spread, decimal zScore, decimal wtiPosition, decimal brentPosition)
	{
		if (wtiPosition != _heldWti || brentPosition != _heldBrent)
			Violate($"{now:O}: the pair holds WTI {wtiPosition} and Brent {brentPosition}, but was opened as WTI {_heldWti} and Brent {_heldBrent}. README: one grade long and the other short for the same dollar amount.");

		// 1 holds long WTI and short Brent, -1 the opposite.
		var side = wtiPosition != 0m ? Math.Sign(wtiPosition) : -Math.Sign(brentPosition);
		var reason = ExitReason(now, side, spread, zScore);

		switch (reason)
		{
			case RollComment:
				RollExits++;
				RollExitTime ??= now;
				break;
			case StopComment:
				StopExits++;
				_blockedSign = -side;
				break;
			case AverageComment:
				AverageExits++;
				break;
			default:
				return;
		}

		if (wtiPosition != 0m)
			Expect(_wti, wtiPosition > 0m ? Sides.Sell : Sides.Buy, Math.Abs(wtiPosition), reason);

		if (brentPosition != 0m)
			Expect(_brent, brentPosition > 0m ? Sides.Sell : Sides.Buy, Math.Abs(brentPosition), reason);

		_stopSpread = null;

		if (_holdings.Count > 0 && _holdings[^1].Close is null)
			_holdings[^1] = (_holdings[^1].Open, now);
	}

	private string ExitReason(DateTime now, int side, decimal spread, decimal zScore)
	{
		if (IsRollDue(now))
			return RollComment;

		if (_stopSpread is decimal stop && (side > 0 ? spread <= stop : spread >= stop))
			return StopComment;

		return (side > 0 ? zScore >= 0m : zScore <= 0m) ? AverageComment : null;
	}

	private void ExpectEntry(decimal volume, DateTime now, decimal spread, decimal deviation, decimal zScore)
	{
		// 1 buys WTI while it is cheap against Brent, -1 sells it while it is expensive.
		var direction = zScore > _entryZScore ? -1
			: zScore < -_entryZScore ? 1
			: 0;

		if (direction == 0)
			return;

		if (IsRollDue(now))
		{
			RollBlockedSignals++;
			return;
		}

		if (_blockedSign == -direction)
		{
			StopBlockedSignals++;
			return;
		}

		var wtiValue = volume * _wtiClose * LotSize(_wti);
		var brentLotValue = _brentClose * LotSize(_brent);
		var brentVolume = BrentVolume(wtiValue, brentLotValue);

		if (brentVolume <= 0m)
		{
			UnsizedSignals++;
			return;
		}

		if (Math.Abs(brentVolume * brentLotValue - wtiValue) > VolumeStep(_brent) * brentLotValue / 2m)
			Violate($"{now:O}: {brentVolume} Brent at {_brentClose} is not the dollar value of {volume} WTI at {_wtiClose} to the nearest volume step.");

		// README: stop on spread widening, fixed at entry past the entry spread.
		_stopSpread = spread - direction * _stopWidening * deviation;
		_heldWti = direction * volume;
		_heldBrent = -direction * brentVolume;
		Expect(_wti, direction > 0 ? Sides.Buy : Sides.Sell, volume, EntryComment);
		Expect(_brent, direction > 0 ? Sides.Sell : Sides.Buy, brentVolume, EntryComment);
		_holdings.Add((now, null));

		if (direction > 0)
			LongEntries++;
		else
			ShortEntries++;

		if (Math.Abs(zScore) >= _entryZScore + _stopWidening)
			DeepEntries++;
	}

	private decimal BrentVolume(decimal wtiValue, decimal brentLotValue)
	{
		if (wtiValue <= 0m || brentLotValue <= 0m)
			return 0m;

		var step = VolumeStep(_brent);
		var volume = Math.Round(wtiValue / brentLotValue / step, MidpointRounding.AwayFromZero) * step;

		if (_brent.MinVolume is decimal min && volume < min || _brent.MaxVolume is decimal max && max > 0m && volume > max)
			return 0m;

		return volume;
	}

	private bool IsRollDue(DateTime now)
	{
		var expiries = new[] { _wti.ExpiryDate, _brent.ExpiryDate }.Where(date => date is not null).Select(date => date.Value).ToArray();
		return expiries.Length > 0 && now >= expiries.Min().AddDays(-_rollDays);
	}

	private void Expect(Security security, Sides side, decimal volume, string comment)
	{
		_due.Enqueue((security, side, volume, comment));
		ExpectedOrders++;
	}

	private void OnOrder(Strategy strategy, Order order)
	{
		Orders++;

		var securityId = order.Security?.Id;

		if (_wti is not null && securityId == _wti.Id)
			WtiOrders++;
		else if (_brent is not null && securityId == _brent.Id)
			BrentOrders++;

		if (!_due.TryDequeue(out var due))
		{
			Violate($"{strategy.CurrentTime:O}: {Describe(order)} is not called for by the README.");
			return;
		}

		if (securityId != due.Security.Id || order.Side != due.Side || order.Volume != due.Volume || order.Type != OrderTypes.Market || order.Comment != due.Comment)
			Violate($"{strategy.CurrentTime:O}: expected {Describe(due)}, got {Describe(order)}.");
	}

	private void OnTrade(MyTrade trade)
	{
		var securityId = trade.Order?.Security?.Id;

		if (_wti is not null && securityId == _wti.Id)
			WtiTrades++;
		else if (_brent is not null && securityId == _brent.Id)
			BrentTrades++;
	}

	private void Violate(string message)
	{
		if (_violations.Count < _maxViolations)
			_violations.Add(message);
	}

	private static decimal LotSize(Security security)
		=> security.Multiplier is decimal multiplier && multiplier > 0m ? multiplier : 1m;

	private static decimal VolumeStep(Security security)
		=> security.VolumeStep is decimal step && step > 0m ? step : 1m;

	private static string Describe((Security Security, Sides Side, decimal Volume, string Comment) order)
		=> $"{order.Side} {order.Volume} {order.Security.Id} at market '{order.Comment}'";

	private static string Describe(Order order)
		=> $"{order.Side} {order.Volume} {order.Security?.Id} {order.Type} '{order.Comment}'";
}

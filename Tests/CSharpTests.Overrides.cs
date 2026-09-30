namespace StockSharp.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using StockSharp.Samples.Strategies;

partial class CSharpTests
{
	[TestMethod]
	[TestCategory("Shard07")]
	public async Task S0503_AdvancedPositionManagement()
	{
		AreEqual(
			AdvancedPositionManagementStrategy.ProtectiveExitAction.Evaluate,
			AdvancedPositionManagementStrategy.ResolveProtectiveExitAction(0.6m, OrderStates.Done),
			"A terminal 0.4 partial fill must preserve protection and allow retrying the residual 0.6 position.");
		AreEqual(
			AdvancedPositionManagementStrategy.ProtectiveExitAction.Wait,
			AdvancedPositionManagementStrategy.ResolveProtectiveExitAction(0.6m, OrderStates.Active));
		AreEqual(
			AdvancedPositionManagementStrategy.ProtectiveExitAction.Reset,
			AdvancedPositionManagementStrategy.ResolveProtectiveExitAction(0m, OrderStates.Done));

		await RunStrategy<AdvancedPositionManagementStrategy>(CancellationToken, (strategy, _) =>
		{
			strategy.FastLength = 3;
			strategy.SlowLength = 8;
			strategy.StopLossPercent = 0.01m;
			strategy.TakeProfitPercent = 0.01m;
			strategy.CooldownBars = 1;
		}, replayDuration: TimeSpan.FromDays(7));
	}

	[TestMethod]
	[TestCategory("Shard04")]
	public async Task S1908_RandomTrailingStop()
	{
		async Task<EntrySideRecorder> Replay(int randomSeed)
		{
			var recorder = new EntrySideRecorder();

			await RunStrategy<RandomTrailingStopStrategy>(CancellationToken, (strategy, _) =>
			{
				strategy.CandleType = TimeSpan.FromMinutes(5).TimeFrame();
				strategy.SmaPeriod = 2;
				strategy.SleepBars = 1;
				strategy.MinStopLevel = 0.0001m;
				strategy.TrailingStep = 0.0001m;
				strategy.RandomSeed = randomSeed;
				recorder.Attach(strategy);
			}, replayDuration: TimeSpan.FromDays(1), postTradeHorizon: TimeSpan.FromHours(1));

			return recorder;
		}

		var baseline = await Replay(42);
		var repeated = await Replay(42);
		var alternate = await Replay(43);

		baseline.AssertSupportsBothSides();
		repeated.AssertSameAs(baseline);
		alternate.AssertDiffersFrom(baseline);

		var (stopHit, nextStop) = RandomTrailingStopStrategy.EvaluateTrailingStop(
			isLong: false,
			currentStop: 120m,
			closePrice: 100m,
			lowPrice: 99m,
			highPrice: 111m,
			stopDistance: 0.5m,
			trailingDistance: 0.1m);

		IsFalse(stopHit, "A stop tightened from the current candle must not trigger against that candle's earlier high.");
		AreEqual(100.5m, nextStop);
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task S2101_LinearRegressionSlopeV1()
	{
		var slope = LinearRegressionSlopeV1Strategy.CreateSlopeIndicator(3);
		IIndicatorValue value = null;
		var start = new DateTime(2024, 1, 1);
		var prices = new[] { 100m, 102m, 104m };

		for (var i = 0; i < prices.Length; i++)
			value = slope.Process(new DecimalIndicatorValue(slope, prices[i], start.AddMinutes(i)) { IsFinal = true });

		AreEqual(2m, value.GetValue<decimal>(), "The strategy indicator must output the regression coefficient, not the fitted price.");

		var recorder = new OrderTraceRecorder();
		await RunStrategy<LinearRegressionSlopeV1Strategy>(CancellationToken, (strategy, _) =>
		{
			strategy.CandleType = TimeSpan.FromMinutes(5).TimeFrame();
			strategy.Length = 3;
			strategy.TriggerShift = 1;
			strategy.StopLossPct = 100m;
			strategy.TakeProfitPct = 100m;
			recorder.Attach(strategy);
		}, replayDuration: TimeSpan.FromDays(1));

		recorder.AssertFirstVolume(1m);
	}

	[TestMethod]
	[TestCategory("Shard07")]
	public async Task S2703_SelfOptimizingRsiOrMfiTraderV3()
	{
		AreEqual(
			SelfOptimizingRsiOrMfiTraderV3Strategy.ProtectiveExitAction.Evaluate,
			SelfOptimizingRsiOrMfiTraderV3Strategy.ResolveProtectiveExitAction(0.6m, OrderStates.Done),
			"A terminal 0.4 partial fill must preserve protection and allow retrying the residual 0.6 position.");
		AreEqual(
			SelfOptimizingRsiOrMfiTraderV3Strategy.ProtectiveExitAction.Wait,
			SelfOptimizingRsiOrMfiTraderV3Strategy.ResolveProtectiveExitAction(0.6m, OrderStates.Active));
		AreEqual(
			SelfOptimizingRsiOrMfiTraderV3Strategy.ProtectiveExitAction.Reset,
			SelfOptimizingRsiOrMfiTraderV3Strategy.ResolveProtectiveExitAction(0m, OrderStates.Done));

		async Task<OrderTraceRecorder> Replay(bool useDynamicVolume)
		{
			var recorder = new OrderTraceRecorder();

			await RunStrategy<SelfOptimizingRsiOrMfiTraderV3Strategy>(CancellationToken, (strategy, _) =>
			{
				strategy.Security.PriceStep = 1m;
				strategy.Security.MaxVolume = 10m;
				strategy.OptimizingPeriods = 20;
				strategy.IndicatorPeriod = 5;
				strategy.UseAggressiveEntries = true;
				strategy.UseDynamicTargets = false;
				strategy.StaticStopLossPoints = 1;
				strategy.StaticTakeProfitPoints = 1;
				strategy.UseDynamicVolume = useDynamicVolume;
				strategy.RiskPercent = 10m;
				strategy.BaseVolume = 3m;
				recorder.Attach(strategy);
			}, replayDuration: TimeSpan.FromHours(4));

			return recorder;
		}

		var staticVolume = await Replay(useDynamicVolume: false);
		var dynamicVolume = await Replay(useDynamicVolume: true);

		staticVolume.AssertFirstVolume(3m);
		dynamicVolume.AssertFirstVolume(10m);

		var breakEven = new OrderTraceRecorder();
		await RunStrategy<SelfOptimizingRsiOrMfiTraderV3Strategy>(CancellationToken, (strategy, _) =>
		{
			strategy.Security.PriceStep = 0.01m;
			strategy.OptimizingPeriods = 20;
			strategy.IndicatorPeriod = 5;
			strategy.UseAggressiveEntries = true;
			strategy.UseDynamicTargets = false;
			strategy.StaticStopLossPoints = 50_000;
			strategy.StaticTakeProfitPoints = 50_000;
			strategy.UseDynamicVolume = false;
			strategy.BaseVolume = 1m;
			strategy.UseBreakEven = true;
			strategy.BreakEvenTriggerPoints = 1;
			strategy.BreakEvenPaddingPoints = 1;
			breakEven.Attach(strategy);
		}, replayDuration: TimeSpan.FromDays(1), postTradeHorizon: TimeSpan.FromMinutes(20));

		breakEven.AssertFirstOppositeAfter(TimeSpan.FromMinutes(10));
	}

	[TestMethod]
	[TestCategory("Shard07")]
	public async Task S3623_MatrixMachineLearning()
	{
		var recorder = new OrderTraceRecorder();

		await RunStrategy<MatrixMachineLearningStrategy>(CancellationToken, (strategy, _) =>
		{
			strategy.Volume = 3m;
			strategy.HistoryDepth = 50;
			strategy.ForwardDepth = 10;
			strategy.PredictorLength = 4;
			strategy.ForecastLength = 3;
			strategy.MaxIterations = 20;
			recorder.Attach(strategy);
		});

		recorder.AssertFirstVolume(3m);
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S4006_TenpipsOppositeLastNHourTrend()
	{
		var multipliers = new[] { 4m, 2m, 5m, 5m, 1m };
		AreEqual(4m, TenPipsOppositeLastNHourTrendStrategy.ApplyLossMultipliers(1m, [5m, -10m], multipliers));
		AreEqual(2m, TenPipsOppositeLastNHourTrendStrategy.ApplyLossMultipliers(1m, [-10m, 5m], multipliers));
		AreEqual(5m, TenPipsOppositeLastNHourTrendStrategy.ApplyLossMultipliers(1m, [-10m, 5m, 5m], multipliers));
		AreEqual(1m, TenPipsOppositeLastNHourTrendStrategy.ApplyLossMultipliers(1m, [5m, 5m], multipliers));

		var openedAt = new DateTimeOffset(2024, 3, 1, 7, 0, 0, TimeSpan.Zero);
		var partialEpisode = new TenPipsTradeEpisode();
		partialEpisode.RegisterEntry(100m, 1m, Sides.Buy, openedAt);
		var firstPartialProfit = partialEpisode.RegisterExit(90m, 0.4m);
		IsFalse(firstPartialProfit.HasValue, "A partial fill must not create a closed-trade history item.");
		AreEqual(0.6m, partialEpisode.Volume);
		AreEqual(openedAt, partialEpisode.EntryTime.Value, "A partial exit must not restart OrderMaxAge.");
		var partialProfit = partialEpisode.RegisterExit(110m, 0.6m);

		var singleEpisode = new TenPipsTradeEpisode();
		singleEpisode.RegisterEntry(100m, 1m, Sides.Buy, openedAt);
		var singleProfit = singleEpisode.RegisterExit(102m, 1m);

		AreEqual(2m, partialProfit.Value);
		AreEqual(singleProfit, partialProfit, "Equivalent partial and single exits must produce one identical economic result.");

		AreEqual(1, TenPipsOppositeLastNHourTrendStrategy.DetermineDirection([110m, 100m], 1));
		AreEqual(-1, TenPipsOppositeLastNHourTrendStrategy.DetermineDirection([100m, 110m], 1));
		AreEqual(1, TenPipsOppositeLastNHourTrendStrategy.DetermineDirection([110m, 90m, 100m], 2));
		AreEqual(-1, TenPipsOppositeLastNHourTrendStrategy.DetermineDirection([100m, 120m, 110m], 2));
		AreEqual(0, TenPipsOppositeLastNHourTrendStrategy.DetermineDirection([100m], 1));

		async Task<OrderTraceRecorder> Replay(decimal priceStep, int decimals, decimal protectionPips, int hoursToCheckTrend = 3, EntrySideRecorder entryRecorder = null)
		{
			var recorder = new OrderTraceRecorder();

			await RunStrategy<TenPipsOppositeLastNHourTrendStrategy>(CancellationToken, (strategy, _) =>
			{
				strategy.Security.PriceStep = priceStep;
				strategy.Security.Decimals = decimals;
				strategy.CandleType = TimeSpan.FromMinutes(5).TimeFrame();
				strategy.TradingHour = 7;
				strategy.HoursToCheckTrend = hoursToCheckTrend;
				strategy.FixedVolume = 0.1m;
				strategy.StopLossPips = protectionPips;
				strategy.TakeProfitPips = protectionPips;
				strategy.TrailingStopPips = 0m;
				recorder.Attach(strategy);
				entryRecorder?.Attach(strategy);
			}, replayDuration: TimeSpan.FromDays(1), postTradeHorizon: TimeSpan.FromHours(1));

			return recorder;
		}

		var threeDigit = await Replay(0.001m, 3, 50_000m);
		var fiveDigit = await Replay(0.00001m, 5, 5_000_000m);

		fiveDigit.AssertSameAs(threeDigit);

		var dailyEntries = new EntrySideRecorder();
		var dailyOrders = await Replay(0.001m, 3, 1m, entryRecorder: dailyEntries);
		dailyOrders.AssertFirstOppositeWithin(TimeSpan.FromMinutes(20));
		dailyEntries.AssertMaximumEntriesPerDay(1);

		await Replay(0.01m, 2, 5_000_000m, 1);
		await Replay(0.01m, 2, 5_000_000m, 2);

		var trailing = new OrderTraceRecorder();
		await RunStrategy<TenPipsOppositeLastNHourTrendStrategy>(CancellationToken, (strategy, _) =>
		{
			strategy.Security.PriceStep = 1m;
			strategy.Security.Decimals = 2;
			strategy.CandleType = TimeSpan.FromMinutes(5).TimeFrame();
			strategy.TradingHour = 7;
			strategy.HoursToCheckTrend = 3;
			strategy.FixedVolume = 0.1m;
			strategy.StopLossPips = 0m;
			strategy.TakeProfitPips = 0m;
			strategy.TrailingStopPips = 100m;
			trailing.Attach(strategy);
		}, replayDuration: TimeSpan.FromDays(1), postTradeHorizon: TimeSpan.FromHours(1));

		trailing.AssertFirstOppositeAfter(TimeSpan.FromMinutes(10));
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S2776_Ch2010Structure()
	{
		var primaryTraded = false;
		var secondaryTraded = false;

		// README.md gives the example one slot per currency pair, each trading its own daily
		// structure, so both configured slots have to reach the market.
		await RunStrategy<Ch2010StructureStrategy>(CancellationToken, (s, sec2) =>
		{
			s.UsdChfSecurity = s.Security;
			s.GbpUsdSecurity = sec2;
			s.DailyCandleType = TimeSpan.FromHours(1).TimeFrame();
			s.IntradayCandleType = TimeSpan.FromMinutes(5).TimeFrame();
			s.MinTradeVolume = 0.001m;

			s.OrderReceived += (_, order) =>
			{
				if (order.Security?.Id == s.Security.Id)
					primaryTraded = true;
				else if (order.Security?.Id == sec2.Id)
					secondaryTraded = true;
			};
		});

		IsTrue(primaryTraded, "The first instrument slot never reached the market.");
		IsTrue(secondaryTraded, "The second instrument slot never reached the market, so the multi-instrument workflow is not exercised.");

		// MaxAggregateVolume caps the exposure summed over every traded pair. Set to a single
		// volume step it has to shrink the entry below the nominal TradeVolume of 1.
		var capped = new OrderTraceRecorder();

		await RunStrategy<Ch2010StructureStrategy>(CancellationToken, (s, _) =>
		{
			s.UsdChfSecurity = s.Security;
			s.DailyCandleType = TimeSpan.FromHours(1).TimeFrame();
			s.IntradayCandleType = TimeSpan.FromMinutes(5).TimeFrame();
			s.TradeVolume = 1m;
			s.MinTradeVolume = 0.001m;
			s.MaxTradeVolume = 5m;
			s.MaxAggregateVolume = 0.001m;
			capped.Attach(s);
		});

		capped.AssertFirstVolume(0.001m);

	}

	[TestMethod]
	[TestCategory("Shard05")]
	[DoNotParallelize]
	public async Task S0333_KeltnerSeasonalFilter()
	{
		async Task<OrderTraceRecorder> Replay(int startedMonth)
		{
			KeltnerSeasonalStartedMonthProbe.StartedMonth.Value = startedMonth;
			var recorder = new OrderTraceRecorder();

			await RunStrategy<KeltnerSeasonalStartedMonthProbe>(CancellationToken, (strategy, _) =>
			{
				recorder.Attach(strategy);
			});

			return recorder;
		}

		var januaryStart = await Replay(1);
		var septemberStart = await Replay(9);

		januaryStart.AssertSameAs(septemberStart);
	}

	private sealed class KeltnerSeasonalStartedMonthProbe : KeltnerSeasonalStrategy
	{
		public static readonly AsyncLocal<int> StartedMonth = new();

		protected override void OnStarted2(DateTime time)
		{
			var month = StartedMonth.Value is >= 1 and <= 12 ? StartedMonth.Value : time.Month;
			base.OnStarted2(new DateTime(time.Year, month, 1, time.Hour, time.Minute, time.Second, time.Kind));
		}
	}

	[TestMethod]
	[TestCategory("Shard01")]
	public void S1807_GoFormula()
	{
		var go = GoStrategy.CalculateGo(
			openEma: 10m,
			highEma: 11m,
			lowEma: 9m,
			closeEma: 12m,
			volume: 2m);

		AreEqual(12m, go, "GO must follow the README formula over EMA(O/H/L/C) multiplied by volume.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public void S3299_JbSignalUsesBollingerSmaAndForce()
	{
		AreEqual(1, JbStrategy.GetSignal(
			previousClose: 94m,
			sma: 90m,
			force: 1m,
			lowerBand: 95m,
			upperBand: 105m));

		AreEqual(-1, JbStrategy.GetSignal(
			previousClose: 106m,
			sma: 110m,
			force: -1m,
			lowerBand: 95m,
			upperBand: 105m));

		AreEqual(0, JbStrategy.GetSignal(
			previousClose: 100m,
			sma: 90m,
			force: 1m,
			lowerBand: 95m,
			upperBand: 105m));
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public void S3122_VladoWilliamsRSignal()
	{
		AreEqual(1, VladoStrategy.GetSignal(-80m, oversoldLevel: -75m, overboughtLevel: -25m));
		AreEqual(-1, VladoStrategy.GetSignal(-20m, oversoldLevel: -75m, overboughtLevel: -25m));
		AreEqual(0, VladoStrategy.GetSignal(-50m, oversoldLevel: -75m, overboughtLevel: -25m));
		AreEqual(1, VladoStrategy.GetSignal(-75m, oversoldLevel: -75m, overboughtLevel: -25m));
		AreEqual(-1, VladoStrategy.GetSignal(-25m, oversoldLevel: -75m, overboughtLevel: -25m));
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public void S1788_TimerAtrBreakoutLevels()
	{
		var (buy, sell) = TimerStrategy.CalculateLevels(
			close: 100m,
			pipDistancePoints: 10m,
			priceStep: 0.1m,
			atr: 2m);

		AreEqual(103m, buy);
		AreEqual(97m, sell);
	}

	[TestMethod]
	[TestCategory("Shard04")]
	public void S3114_LbsBreakoutLevelsUseCandleAndFreezeBuffer()
	{
		var (buy, sell) = LbsStrategy.CalculateBreakoutLevels(
			candleHigh: 105m,
			candleLow: 95m,
			bid: 100m,
			ask: 101m,
			priceStep: 0.1m);

		// Spread = 1, so 3 spreads = 3 > 10 pips (= 1 at this price step).
		AreEqual(105m, buy);
		AreEqual(95m, sell);

		(buy, sell) = LbsStrategy.CalculateBreakoutLevels(
			candleHigh: 101m,
			candleLow: 99m,
			bid: 100m,
			ask: 100.1m,
			priceStep: 0.1m);

		// Freeze buffer is at least ten pips = 1.0.
		AreEqual(101.1m, buy);
		AreEqual(99m, sell);
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public void S3121_BrunoSignalFiltersMultiplyVolume()
	{
		var (longVolume, shortVolume) = BrunoStrategy.CalculateSignalVolumes(
			baseVolume: 1m,
			multiplier: 2m,
			longDirectional: true,
			shortDirectional: false,
			longMomentum: true,
			shortMomentum: false,
			longMacd: true,
			shortMacd: false,
			longSar: true,
			shortSar: false);

		AreEqual(16m, longVolume);
		AreEqual(1m, shortVolume);

		(longVolume, shortVolume) = BrunoStrategy.CalculateSignalVolumes(
			1m, 2m,
			longDirectional: true, shortDirectional: true,
			longMomentum: false, shortMomentum: false,
			longMacd: false, shortMacd: false,
			longSar: false, shortSar: false);

		IsTrue(longVolume > 1m && shortVolume > 1m,
			"Both sides must be detectable so the caller can skip conflicting signals.");
	}

	[TestMethod]
	[TestCategory("Shard02")]
	public void S1276_RsiCrossingSignal()
	{
		AreEqual(1, RsiStrategy.GetSignal(20m, 30m, overSold: 25m, overBought: 75m));
		AreEqual(-1, RsiStrategy.GetSignal(80m, 70m, overSold: 25m, overBought: 75m));
		AreEqual(0, RsiStrategy.GetSignal(50m, 55m, overSold: 25m, overBought: 75m));
	}

	[TestMethod]
	[TestCategory("Shard02")]
	public void S1228_RciUsesRankCorrelation()
	{
		AreEqual(100m, RciStrategy.CalculateRci([1m, 2m, 3m, 4m, 5m]));
		AreEqual(-100m, RciStrategy.CalculateRci([5m, 4m, 3m, 2m, 1m]));
	}

	[TestMethod]
	[TestCategory("Shard02")]
	public void S1243_RenkoReversalSignal()
	{
		AreEqual(1, RenkoStrategy.GetSignal(previousDirection: -1, currentDirection: 1));
		AreEqual(-1, RenkoStrategy.GetSignal(previousDirection: 1, currentDirection: -1));
		AreEqual(0, RenkoStrategy.GetSignal(previousDirection: 1, currentDirection: 1));
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public void S1322_SmcUsesPremiumDiscountTrendAndOrderBlock()
	{
		AreEqual(1, SmcStrategy.GetSignal(
			price: 98m, swingLow: 90m, swingHigh: 110m, sma: 95m,
			hasSupport: true, hasResistance: false));

		AreEqual(-1, SmcStrategy.GetSignal(
			price: 102m, swingLow: 90m, swingHigh: 110m, sma: 105m,
			hasSupport: false, hasResistance: true));

		AreEqual(0, SmcStrategy.GetSignal(
			price: 98m, swingLow: 90m, swingHigh: 110m, sma: 95m,
			hasSupport: false, hasResistance: false));
	}

	[TestMethod]
	[TestCategory("Shard03")]
	public void S1437_TimeUsesTicksAboveBarOpen()
	{
		IsTrue(TimeStrategy.IsPriceConditionMet(
			open: 100m, high: 101.5m, priceStep: 0.1m, ticksFromOpen: 10));
		IsFalse(TimeStrategy.IsPriceConditionMet(
			open: 100m, high: 100.5m, priceStep: 0.1m, ticksFromOpen: 10));
		IsFalse(TimeStrategy.IsPriceConditionMet(
			open: 100m, high: 101m, priceStep: 0.1m, ticksFromOpen: 10));
		IsTrue(TimeStrategy.IsPriceConditionMet(
			open: 100m, high: 101.1m, priceStep: 0.1m, ticksFromOpen: 10));
		IsFalse(TimeStrategy.IsPriceConditionMet(
			open: 100m, high: 100m, priceStep: 0.1m, ticksFromOpen: 0));
		IsTrue(TimeStrategy.IsPriceConditionMet(
			open: 100m, high: 100.1m, priceStep: 0.1m, ticksFromOpen: 0));
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public void S1341_StochasticThresholdCrossing()
	{
		AreEqual(1, StochasticStrategy.GetSignal(40m, 60m, overSold: 50m, overBought: 50m));
		AreEqual(-1, StochasticStrategy.GetSignal(60m, 40m, overSold: 50m, overBought: 50m));
		AreEqual(0, StochasticStrategy.GetSignal(60m, 70m, overSold: 50m, overBought: 50m));
	}

	[TestMethod]
	[TestCategory("Shard04")]
	public void S1189_PriceFlipUsesMirroredRangeAndSmaCross()
	{
		AreEqual(96m, PriceFlipStrategy.CalculateInvertedPrice(
			recentHigh: 110m, recentLow: 90m, price: 104m));

		AreEqual(1, PriceFlipStrategy.GetSignal(
			previousClose: 104m, previousInverted: 96m,
			previousFast: 99m, previousSlow: 100m,
			fast: 101m, slow: 100m,
			currentClose: 102m, useTrendFilter: true));

		AreEqual(-1, PriceFlipStrategy.GetSignal(
			previousClose: 96m, previousInverted: 104m,
			previousFast: 101m, previousSlow: 100m,
			fast: 99m, slow: 100m,
			currentClose: 98m, useTrendFilter: true));
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S4104_PinballMachineRandomDrawContract()
	{
		var draw = PinballMachineRandomDrawStrategy.DrawEvaluation(
			new Random(42),
			randomMaxValue: 0,
			minStopLossPoints: 7,
			maxStopLossPoints: 7,
			minTakeProfitPoints: 11,
			maxTakeProfitPoints: 11);

		IsTrue(draw.EnterLong, "The first independently drawn pair must be able to trigger a long entry.");
		IsTrue(draw.EnterShort, "The second independently drawn pair must be able to trigger a short entry on the same candle.");
		AreEqual(7, draw.StopLossPoints);
		AreEqual(11, draw.TakeProfitPoints);

		const string path = "4101-4200/4104_Pinball_Machine_Random_Draw/CS/PinballMachineRandomDrawStrategy.cs";
		var recorder = new OrderTraceRecorder();

		await RunStrategy(path, CancellationToken, (strategy, _) =>
		{
			strategy.Security.PriceStep = 0.01m;
			strategy.Parameters["CandleType"].Value = TimeSpan.FromMinutes(5).TimeFrame();
			strategy.Parameters["TradeVolume"].Value = 2m;
			strategy.Parameters["RandomMaxValue"].Value = 0;
			strategy.Parameters["MinStopLossPoints"].Value = 0;
			strategy.Parameters["MaxStopLossPoints"].Value = 0;
			strategy.Parameters["MinTakeProfitPoints"].Value = 0;
			strategy.Parameters["MaxTakeProfitPoints"].Value = 0;
			strategy.Parameters["RandomSeed"].Value = 42;
			recorder.Attach(strategy);
		}, replayDuration: TimeSpan.FromDays(1));

		recorder.AssertContainsOppositePairAtSameTimestamp(2m, "Pinball entry");

		var protectedRecorder = new OrderTraceRecorder();
		await RunStrategy(path, CancellationToken, (strategy, _) =>
		{
			strategy.Security.PriceStep = 0.01m;
			strategy.Parameters["CandleType"].Value = TimeSpan.FromMinutes(5).TimeFrame();
			strategy.Parameters["TradeVolume"].Value = 1m;
			strategy.Parameters["RandomMaxValue"].Value = 4;
			strategy.Parameters["MinStopLossPoints"].Value = 1;
			strategy.Parameters["MaxStopLossPoints"].Value = 1;
			strategy.Parameters["MinTakeProfitPoints"].Value = 1;
			strategy.Parameters["MaxTakeProfitPoints"].Value = 1;
			strategy.Parameters["RandomSeed"].Value = 5;
			protectedRecorder.Attach(strategy);
		}, replayDuration: TimeSpan.FromDays(1));

		protectedRecorder.AssertContainsComment("Pinball protection exit");
	}

	/// <summary>
	/// Runs both versions of the example and compares their orders, so it runs once rather than once per language.
	/// </summary>
	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S0408_CSharpAndPythonSubmitIdenticalOrders()
	{
		var traces = new List<(bool IsOption, DateTime Bar, string SecurityId, Sides Side, decimal Volume, string Comment)>[2];

		foreach (var python in new[] { false, true })
		{
			VrpRecorder recorder = null;

			Action<Strategy, Security> setup = (strategy, secondary) =>
			{
				var option = VolatilityRiskPremiumOptionFixture.QuarterlyCall(secondary, strategy.Security);
				SetParam(strategy, "Option", option);
				recorder = new(strategy, option);
			};

			await (python
				? PythonTests.RunStrategy(StrategyInventory.GetFile(VolatilityRiskPremium, ".py"), CancellationToken, setup, replayDuration: TimeSpan.FromDays(31))
				: Replay(VolatilityRiskPremium, setup, TimeSpan.FromDays(31)));

			traces[python ? 1 : 0] = [.. recorder.Orders.Select(entry => (recorder.Candles[entry.Candle].IsOption, recorder.Candles[entry.Candle].OpenTime,
				entry.Order.Security.Id, entry.Order.Side, entry.Order.Volume, entry.Order.Comment))];
		}

		IsTrue(traces[0].Count > 0);
		AreEqual(traces[0], traces[1], "C# and Python must submit the same orders on the same candles.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task S1101_TrailingStopExitsTwoPercentFromTheBestPriceInBothLanguages()
	{
		var csharp = await ReplayMultiTimeframeMacdTrailing(false);
		var python = await ReplayMultiTimeframeMacdTrailing(true);

		AreEqual(csharp.Orders, python.Orders, "Python must submit the same orders as C# on the same bars.");
	}

	/// <summary>
	/// The orders of one replay with the trailing stop on, and what the README stop model found in them.
	/// </summary>
	private sealed class MultiTimeframeMacdTrailingRun
	{
		public List<(DateTime Time, Sides Side, decimal Volume)> Orders { get; } = [];
		public List<string> Violations { get; } = [];
		public int TrailingExits { get; set; }
	}

	/// <summary>
	/// Replays a week with the trailing stop on against an independent model of the README stop: while a position is open,
	/// the best price since its entry is the highest high (long) or the lowest low (short) of the finished working candles,
	/// the stop sits the published 2% below (long) or above (short) that price, and the first candle that touches the stop
	/// closes the whole position. No other order may leave the position flat.
	/// </summary>
	private async Task<MultiTimeframeMacdTrailingRun> ReplayMultiTimeframeMacdTrailing(bool python)
	{
		const decimal trailingPercent = 2m;
		var working = TimeSpan.FromMinutes(5).TimeFrame();
		var run = new MultiTimeframeMacdTrailingRun();
		(int Sign, decimal Price)? best = null;
		(Sides Side, decimal Volume)? exit = null;

		void violate(string message)
		{
			if (run.Violations.Count < 12)
				run.Violations.Add(message);
		}

		Action<Strategy, Security> setup = (strategy, _) =>
		{
			AssertMultiTimeframeMacdDefaults(strategy);

			SetParam(strategy, "UseTrailingStop", true);

			// Raised before the strategy handles the candle, so the position read here is the one the stop protects on it.
			strategy.CandleReceived += (subscription, candle) =>
			{
				if (candle.State != CandleStates.Finished || strategy.ProcessState != ProcessStates.Started)
					return;

				if (exit is { } due)
					violate($"{candle.OpenTime:O}: the previous finished candle reached the stop, but no {due.Side} {due.Volume} exit followed it.");

				exit = null;

				var position = strategy.Position;

				if (!subscription.DataType.Equals(working) || position == 0m)
					return;

				var sign = Math.Sign(position);
				var price = sign > 0 ? candle.HighPrice : candle.LowPrice;

				if (best is { } tracked && tracked.Sign == sign)
					price = sign > 0 ? Math.Max(tracked.Price, price) : Math.Min(tracked.Price, price);

				best = (sign, price);

				var reached = sign > 0
					? candle.LowPrice <= price * (1m - trailingPercent / 100m)
					: candle.HighPrice >= price * (1m + trailingPercent / 100m);

				if (reached)
					exit = (sign > 0 ? Sides.Sell : Sides.Buy, Math.Abs(position));
			};

			strategy.OrderRegistering += order =>
			{
				run.Orders.Add((strategy.CurrentTime, order.Side, order.Volume));

				var position = strategy.Position;

				if (exit is { } due)
				{
					if (order.Side == due.Side && order.Volume == due.Volume)
						run.TrailingExits++;
					else
						violate($"{strategy.CurrentTime:O}: the candle reached the stop, so its order must be the {due.Side} {due.Volume} exit, not {order.Side} {order.Volume}.");
				}
				else if (position != 0m && order.Side == (position > 0m ? Sides.Sell : Sides.Buy) && order.Volume == Math.Abs(position))
				{
					violate($"{strategy.CurrentTime:O}: {order.Side} {order.Volume} left the position flat although no finished candle reached the stop {trailingPercent}% from the best price since entry.");
				}

				// An entry or a reversal opens a position with its own best price, and an exit leaves nothing to trail.
				exit = null;
				best = null;
			};
		};

		// The test compares both languages, so the Python half runs through PythonTests.
		if (python)
			await PythonTests.RunStrategy(StrategyInventory.GetFile(MultiTimeframeMacd, ".py"), CancellationToken, setup, replayDuration: TimeSpan.FromDays(7));
		else
			await Replay(MultiTimeframeMacd, setup, TimeSpan.FromDays(7));

		if (exit is { } last)
			violate($"End of replay: the last finished candle reached the stop, but no {last.Side} {last.Volume} exit followed it.");

		var language = python ? "Python" : "C#";
		TestContext.WriteLine($"{language}: orders={run.Orders.Count}, trailing exits={run.TrailingExits}.");

		IsTrue(run.Violations.Count == 0, $"{language}: {string.Join(Environment.NewLine, run.Violations)}");
		IsTrue(run.TrailingExits > 0, $"{language}: the fixture must reach trailing exits as well as MACD agreement orders.");

		return run;
	}

	private static readonly decimal _rangeFollowerPercentStep = new(1, 0, 0, false, 27);

	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S3406_QuoteExactlyOnTheTriggerDistanceDoesNotEnter()
	{
		var session = DateTime.MinValue;
		var sessionAtr = 0m;
		var skipped = false;
		var high = 0m;
		var low = 0m;
		decimal? bid = null;
		decimal? ask = null;
		var currentPercent = 60m;
		Level1ChangeMessage tiedQuote = null;
		var tiedSessions = new HashSet<DateTime>();
		var tradedSessions = new HashSet<DateTime>();
		var entries = 0;
		var violations = new List<string>();

		await Replay(RangeFollower, (strategy, _) =>
		{
			var percent = strategy.Parameters["TriggerPercent"];
			AreEqual(60m, Convert.ToDecimal(percent.Value));
			strategy.Volume = 0.1m;

			void SetPercent(decimal value)
			{
				if (currentPercent == value)
					return;

				currentPercent = value;
				SetParam(strategy, "TriggerPercent", value);
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

				tiedQuote = null;
				SetPercent(60m);

				if (candle.OpenTime.Date == session)
				{
					high = Math.Max(high, candle.HighPrice);
					low = Math.Min(low, candle.LowPrice);
					return;
				}

				session = candle.OpenTime.Date;
				high = candle.HighPrice;
				low = candle.LowPrice;
				var atr = strategy.Indicators.OfType<AverageTrueRange>().Single();
				sessionAtr = atr.IsFormed ? atr.GetCurrentValue() : 0m;
				skipped = sessionAtr > 0m && high - low > sessionAtr * 60m / 100m;
			};

			// Once a session, the percent is set for one quote so that the trigger equals its larger distance exactly.
			// The quote reaches this handler once per Level1 subscription; the percent stays set for all of them.
			strategy.Level1Received += (_, quote) =>
			{
				if (ReferenceEquals(quote, tiedQuote))
					return;

				tiedQuote = null;
				SetPercent(60m);

				if (!RangeFollowerTakeQuote(quote, ref bid, ref ask) || bid is not decimal currentBid || ask is not decimal currentAsk)
					return;

				if (session == quote.ServerTime.Date)
				{
					high = Math.Max(high, Math.Max(currentBid, currentAsk));
					low = Math.Min(low, Math.Min(currentBid, currentAsk));
				}

				if (sessionAtr <= 0m || skipped || session != quote.ServerTime.Date || session != strategy.CurrentTime.Date
					|| tiedSessions.Contains(session) || tradedSessions.Contains(session) || strategy.Position != 0m || !strategy.IsFormedAndOnlineAndAllowTrading()
					|| strategy.Orders.Any(order => order.State is not (OrderStates.Done or OrderStates.Failed)))
					return;

				var distance = Math.Max(currentBid - low, high - currentAsk);
				if (distance >= sessionAtr * 60m / 100m || RangeFollowerTiePercent(sessionAtr, distance) is not decimal tie)
					return;

				tiedSessions.Add(session);
				tiedQuote = quote;
				SetPercent(tie);
			};

			strategy.OrderRegistering += order =>
			{
				if (strategy.Position != 0m)
					return;

				entries++;
				tradedSessions.Add(session);

				var trigger = sessionAtr * currentPercent / 100m;
				var longDistance = bid.GetValueOrDefault() - low;
				var shortDistance = high - ask.GetValueOrDefault();

				if (Math.Max(longDistance, shortDistance) <= trigger && violations.Count < 12)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} entry with bid - low = {longDistance} and high - ask = {shortDistance}, neither more than the trigger {trigger}{(currentPercent != 60m ? ", which this quote matches exactly" : string.Empty)}.");
			};
		}, TimeSpan.FromDays(31));

		TestContext.WriteLine($"ties={tiedSessions.Count}, entries={entries}");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));
		IsTrue(tiedSessions.Count > 0, "The fixture must put a live quote exactly on the trigger distance.");
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S3406_InitialRangeEqualToTheTriggerDoesNotSkipTheDay()
	{
		var session = DateTime.MinValue;
		var sessionAtr = 0m;
		var trigger = 0m;
		var skipped = false;
		var high = 0m;
		var low = 0m;
		decimal? bid = null;
		decimal? ask = null;
		var tiedSessions = new HashSet<DateTime>();
		var entriesPerSession = new Dictionary<DateTime, int>();
		var violations = new List<string>();

		await Replay(RangeFollower, (strategy, _) =>
		{
			var percent = strategy.Parameters["TriggerPercent"];
			AreEqual(60m, Convert.ToDecimal(percent.Value));
			strategy.Volume = 0.1m;
			SetParam(strategy, "CandleType", TimeSpan.FromHours(4).TimeFrame());

			strategy.ProcessStateChanged += changed =>
			{
				if (ReferenceEquals(changed, strategy) && changed.ProcessState == ProcessStates.Stopped)
					SetParam(strategy, "TriggerPercent", 60m);
			};

			// Each session's percent puts the trigger exactly on the range of its first working candle.
			strategy.CandleReceived += (subscription, candle) =>
			{
				if (candle.State != CandleStates.Finished || subscription.DataType.Arg is not TimeSpan frame || frame != TimeSpan.FromHours(4))
					return;

				if (candle.OpenTime.Date == session)
				{
					high = Math.Max(high, candle.HighPrice);
					low = Math.Min(low, candle.LowPrice);
					return;
				}

				session = candle.OpenTime.Date;
				high = candle.HighPrice;
				low = candle.LowPrice;
				var atr = strategy.Indicators.OfType<AverageTrueRange>().Single();
				sessionAtr = atr.IsFormed ? atr.GetCurrentValue() : 0m;
				var tie = sessionAtr > 0m ? RangeFollowerTiePercent(sessionAtr, high - low) : null;
				if (tie is not null)
					tiedSessions.Add(session);

				var sessionPercent = tie ?? 60m;
				SetParam(strategy, "TriggerPercent", sessionPercent);
				trigger = sessionAtr * sessionPercent / 100m;
				skipped = sessionAtr > 0m && high - low > trigger;
			};

			strategy.Level1Received += (_, quote) =>
			{
				if (!RangeFollowerTakeQuote(quote, ref bid, ref ask) || bid is not decimal currentBid || ask is not decimal currentAsk || session != quote.ServerTime.Date)
					return;

				high = Math.Max(high, Math.Max(currentBid, currentAsk));
				low = Math.Min(low, Math.Min(currentBid, currentAsk));
			};

			strategy.OrderRegistering += order =>
			{
				if (strategy.Position != 0m)
					return;

				entriesPerSession[session] = entriesPerSession.GetValueOrDefault(session) + 1;

				var longDistance = bid.GetValueOrDefault() - low;
				var shortDistance = high - ask.GetValueOrDefault();

				if ((skipped || sessionAtr <= 0m || strategy.CurrentTime.Date != session || Math.Max(longDistance, shortDistance) <= trigger) && violations.Count < 12)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} entry on {session:yyyy-MM-dd} (skipped={skipped}) with bid - low = {longDistance} and high - ask = {shortDistance} against the trigger {trigger}.");
			};
		}, TimeSpan.FromDays(31));

		var tiedEntries = tiedSessions.Count(entriesPerSession.ContainsKey);
		TestContext.WriteLine($"tiedSessions={tiedSessions.Count}, tiedSessionsTraded={tiedEntries}, sessionsTraded={entriesPerSession.Count}");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations));
		IsTrue(entriesPerSession.Values.All(count => count == 1), "Only one trade is allowed per day.");
		IsTrue(tiedSessions.Count > 0, "The fixture must open a session whose first working candle spans exactly the trigger.");
		IsTrue(tiedEntries > 0, "A day whose initial range only equals the trigger does not exceed it, so the day is not skipped and its later breakout trades.");
	}

	/// <summary>
	/// A <c>TriggerPercent</c> within 10..90 for which <c>atr * percent / 100m</c>, as the strategy computes it, equals <paramref name="distance"/> exactly.
	/// </summary>
	/// <remarks>
	/// The Python example turns its float percent into a decimal of fifteen digits, which cannot land the trigger exactly on a
	/// price distance, so the tie tests run on the C# example.
	/// </remarks>
	private static decimal? RangeFollowerTiePercent(decimal atr, decimal distance)
	{
		var guess = distance * 100m / atr;

		for (var offset = 0; offset <= 4; offset++)
		{
			foreach (var percent in new[] { guess - offset * _rangeFollowerPercentStep, guess + offset * _rangeFollowerPercentStep })
			{
				if (percent >= 10m && percent <= 90m && atr * percent / 100m == distance)
					return percent;
			}
		}

		return null;
	}

	/// <summary>
	/// Runs both versions of the example and compares their order and fill traces, so it runs once rather than once per language.
	/// </summary>
	[TestMethod]
	[TestCategory("Shard01")]
	public async Task S3801_CSharpAndPythonOrderAndFillTracesMatch()
	{
		var traces = new List<List<string>>();

		foreach (var python in new[] { false, true })
		{
			var trace = new List<string>();

			Action<Strategy, Security> setup = (strategy, _) =>
			{
				strategy.Security.StepPrice = strategy.Security.PriceStep * 2m;
				strategy.OrderRegistering += order => trace.Add($"O|{strategy.CurrentTime:O}|{order.Type}|{order.Side}|{order.Volume}|{OrderStabilizationActivation(order)}");
				strategy.OrderCanceling += order => trace.Add($"C|{strategy.CurrentTime:O}|{order.Side}|{OrderStabilizationActivation(order)}");
				strategy.Trades.TradeAdded += trade => trace.Add($"F|{strategy.CurrentTime:O}|{trade.Order.Side}|{trade.Trade.Price}|{trade.Trade.Volume}|{strategy.Position}");
			};

			await (python
				? PythonTests.RunStrategy(StrategyInventory.GetFile(OrderStabilization, ".py"), CancellationToken, setup, replayDuration: TimeSpan.FromDays(3))
				: Replay(OrderStabilization, setup, TimeSpan.FromDays(3)));

			traces.Add(trace);
		}

		IsTrue(traces[0].Count > 100, $"The default parameters must trade on the archive, but C# produced only {traces[0].Count} events.");
		IsTrue(traces[0].SequenceEqual(traces[1]),
			$"Both languages must place, cancel and fill the same orders on the same archive: {FirstOrderStabilizationTraceDifference(traces[0], traces[1])}.");
	}

	private static string FirstOrderStabilizationTraceDifference(List<string> csharp, List<string> python)
	{
		var length = Math.Min(csharp.Count, python.Count);

		for (var i = 0; i < length; i++)
		{
			if (csharp[i] != python[i])
				return $"event {i}: C# '{csharp[i]}', Python '{python[i]}'";
		}

		return $"C# {csharp.Count} events, Python {python.Count} events";
	}

	/// <summary>
	/// Replays both versions of the example and compares their traces, so it runs once rather than once per language.
	/// </summary>
	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S0040_CSharpAndPythonActualOrderAndFillTracesMatch()
	{
		var traces = new List<List<string>>();
		foreach (var python in new[] { false, true })
		{
			var trace = new List<string>();
			Action<Strategy, Security> setup = (strategy, _) =>
			{
				strategy.OrderRegistering += order => trace.Add($"O|{strategy.CurrentTime:O}|{order.Side}|{order.Volume}|{order.Comment}");
				strategy.Trades.TradeAdded += trade => trace.Add($"F|{strategy.CurrentTime:O}|{trade.Order.Side}|{trade.Trade.Price}|{trade.Trade.Volume}|{strategy.Position}");
			};
			await (python
				? PythonTests.RunStrategy(StrategyInventory.GetFile(AtrTrailing, ".py"), CancellationToken, setup, replayDuration: TimeSpan.FromDays(31))
				: Replay(AtrTrailing, setup, TimeSpan.FromDays(31)));
			traces.Add(trace);
		}
		IsTrue(traces[0].Count > 100 && traces[0].SequenceEqual(traces[1]), "Both languages must produce identical causal order and actual-fill traces on the same real archive/default parameters.");
	}

	/// <summary>
	/// Runs both versions of the example and compares their order and fill events, so it runs once rather than once per language.
	/// </summary>
	[TestMethod]
	[TestCategory("Shard04")]
	public async Task S1788_CSharpAndPythonFollowTheSameRules()
	{
		var csharp = await ReplayTimer(waitSeconds: 300, takeProfit: 100m, stopLoss: 50m, trailingStop: 50m);
		var python = await ReplayTimer((setup, duration) => PythonTests.RunStrategy(StrategyInventory.GetFile(TimerExample, ".py"), CancellationToken, setup, replayDuration: duration),
			waitSeconds: 300, takeProfit: 100m, stopLoss: 50m, trailingStop: 50m);

		IsTrue(csharp.Violations.Count == 0, string.Join(Environment.NewLine, csharp.Violations));
		IsTrue(python.Violations.Count == 0, string.Join(Environment.NewLine, python.Violations));
		IsTrue(csharp.Entries > 0 && csharp.ProtectiveExits > 0 && csharp.TrailingExits > 0,
			"Take-profit, stop-loss and trailing stop run together, so the archive must exercise entries, protective exits and trailing exits.");

		var mismatch = Enumerable.Range(0, Math.Min(csharp.Trace.Count, python.Trace.Count)).FirstOrDefault(i => csharp.Trace[i] != python.Trace[i], -1);
		IsTrue(csharp.Trace.Count > 0 && mismatch < 0 && csharp.Trace.Count == python.Trace.Count,
			mismatch < 0
				? $"C# produced {csharp.Trace.Count} order and fill events, Python {python.Trace.Count}."
				: $"First difference at event {mismatch}: C# {csharp.Trace[mismatch]}, Python {python.Trace[mismatch]}.");
	}
}

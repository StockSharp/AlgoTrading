namespace StockSharp.Tests;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Algo.Indicators;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using StockSharp.Samples.Strategies;

partial class CSharpTests
{
	[TestMethod]
	[TestCategory("Shard01")]
	public async Task S0001_MaCrossover()
	{
		var recorder = new OrderTraceRecorder();
		MaCrossoverStrategy strategy = null;

		await RunStrategy<MaCrossoverStrategy>(CancellationToken, (current, _) =>
		{
			strategy = current;
			current.FastLength = 5;
			current.SlowLength = 20;
			current.StopLossPercent = 100m;
			recorder.Attach(current);
		});

		recorder.AssertReverses(strategy.Volume);
	}

	[TestMethod]
	[TestCategory("Shard01")]
	public async Task S0201_VwapWilliamsR()
	{
		var tightStop = new OrderTraceRecorder();
		var wideStop = new OrderTraceRecorder();

		await RunStrategy<VwapWilliamsRStrategy>(CancellationToken, (strategy, _) =>
		{
			strategy.WilliamsRPeriod = 5;
			strategy.CooldownBars = 1;
			strategy.StopLossPercent = 0.5m;
			tightStop.Attach(strategy);
		}, replayDuration: TimeSpan.FromDays(7));

		await RunStrategy<VwapWilliamsRStrategy>(CancellationToken, (strategy, _) =>
		{
			strategy.WilliamsRPeriod = 5;
			strategy.CooldownBars = 1;
			strategy.StopLossPercent = 5m;
			wideStop.Attach(strategy);
		}, replayDuration: TimeSpan.FromDays(7));

		tightStop.AssertDiffersFrom(wideStop, "Changing StopLossPercent did not affect submitted orders.");
	}

	[TestMethod]
	[TestCategory("Shard01")]
	public async Task S0401_SoccerClubsArbitrage()
	{
		var recorder = new PairedOrderRecorder();
		SoccerClubsArbitrageStrategy strategy = null;
		Security secondSecurity = null;

		await RunStrategy<SoccerClubsArbitrageStrategy>(CancellationToken, (current, second) =>
		{
			strategy = current;
			secondSecurity = second;
			current.Security2Id = second.Id;
			recorder.Attach(current);
		});

		recorder.AssertBalanced(strategy.Security, secondSecurity);
	}

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
	[TestCategory("Shard05")]
	public async Task S1005_MaWithLogistic()
	{
		async Task<OrderTraceRecorder> Replay(decimal takeProfit, decimal stopLoss)
		{
			var recorder = new OrderTraceRecorder();

			await RunStrategy<MaWithLogisticStrategy>(CancellationToken, (strategy, _) =>
			{
				strategy.FastLength = 3;
				strategy.SlowLength = 8;
				strategy.TakeProfitPercent = takeProfit;
				strategy.StopLossPercent = stopLoss;
				recorder.Attach(strategy);
			}, replayDuration: TimeSpan.FromDays(7));

			return recorder;
		}

		var wideProtection = await Replay(100m, 100m);
		var tightTakeProfit = await Replay(0.5m, 100m);
		var tightStopLoss = await Replay(100m, 0.5m);

		tightTakeProfit.AssertDiffersFrom(wideProtection, "Changing TakeProfitPercent did not affect submitted orders.");
		tightStopLoss.AssertDiffersFrom(wideProtection, "Changing StopLossPercent did not affect submitted orders.");
	}

	[TestMethod]
	[TestCategory("Shard03")]
	public async Task S1507_UltimateTemplate()
	{
		async Task<OrderTraceRecorder> Replay(int fastLength, int slowLength, decimal takeProfit, decimal stopLoss)
		{
			var recorder = new OrderTraceRecorder();

			await RunStrategy<UltimateTemplateStrategy>(CancellationToken, (strategy, _) =>
			{
				strategy.FastLength = fastLength;
				strategy.SlowLength = slowLength;
				strategy.TakeProfitPercent = takeProfit;
				strategy.StopLossPercent = stopLoss;
				recorder.Attach(strategy);
			}, replayDuration: TimeSpan.FromDays(7));

			return recorder;
		}

		var baseline = await Replay(9, 21, 100m, 100m);
		var alternatePeriods = await Replay(2, 60, 100m, 100m);
		var tightTakeProfit = await Replay(9, 21, 0.5m, 100m);
		var tightStopLoss = await Replay(9, 21, 100m, 0.01m);

		alternatePeriods.AssertDiffersFrom(baseline, "Changing FastLength and SlowLength did not affect submitted orders.");
		tightTakeProfit.AssertDiffersFrom(baseline, "Changing TakeProfitPercent did not affect submitted orders.");
		tightStopLoss.AssertDiffersFrom(baseline, "Changing StopLossPercent did not affect submitted orders.");
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
	[TestCategory("Shard03")]
	public async Task S2403_ReOpenPositions()
	{
		var recorder = new OrderTraceRecorder();

		await RunStrategy<ReOpenPositionsStrategy>(CancellationToken, (strategy, _) =>
		{
			strategy.CandleType = TimeSpan.FromMinutes(5).TimeFrame();
			strategy.ProfitThreshold = -1_000_000m;
			strategy.MaxPositions = 3;
			strategy.StopLossPoints = 100m;
			strategy.TakeProfitPoints = 100m;
			recorder.Attach(strategy);
		}, replayDuration: TimeSpan.FromDays(2));

		recorder.AssertFirstSide(Sides.Buy);
		recorder.AssertBasketExitAfterEntries(3, 3m);
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S2502_21HourSessionBreakout()
	{
		var recorder = new OrderTraceRecorder();

		await RunStrategy<TwentyOneHourSessionBreakoutStrategy>(CancellationToken, (strategy, _) =>
		{
			strategy.Security.PriceStep = 0.01m;
			strategy.CandleType = TimeSpan.FromHours(4).TimeFrame();
			strategy.FirstSessionStartHour = 20;
			strategy.FirstSessionStopHour = 21;
			strategy.StepPoints = 1m;
			strategy.TakeProfitPoints = 1_000_000m;
			recorder.Attach(strategy);
		}, replayDuration: TimeSpan.FromDays(2));

		recorder.AssertFirstOrderHour(20);
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S2606_StatisticsRepeatingBehavior()
	{
		var recorder = new OrderTraceRecorder();

		await RunStrategy<StatisticsRepeatingBehaviorStrategy>(CancellationToken, (strategy, _) =>
		{
			strategy.Security.VolumeStep = 0.5m;
			strategy.Security.MinVolume = 1m;
			strategy.Security.MaxVolume = 10m;
			strategy.InitialVolume = 3.4m;
			strategy.MartingaleFactor = 2m;
			strategy.StopLossPips = 1;
			recorder.Attach(strategy);
		}, replayDuration: TimeSpan.FromDays(2));

		recorder.AssertFirstVolume(3m);
		recorder.AssertContainsVolume(6m);
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
	public async Task S3710_Rrsrandomness()
	{
		async Task<OrderTraceRecorder> Replay(
			RrsRandomnessStrategy.TradingModes mode,
			decimal minVolume,
			decimal maxVolume,
			decimal maxSpread,
			RrsRandomnessStrategy.RiskModes riskMode,
			decimal riskValue,
			decimal takeProfit = 1m,
			decimal stopLoss = 1m,
			TimeSpan? postTradeHorizon = null)
		{
			var recorder = new OrderTraceRecorder();

			await RunStrategy<RrsRandomnessStrategy>(CancellationToken, (strategy, _) =>
			{
				strategy.Mode = mode;
				strategy.MinVolume = minVolume;
				strategy.MaxVolume = maxVolume;
				strategy.MaxSpreadPoints = maxSpread;
				strategy.MoneyRiskMode = riskMode;
				strategy.RiskValue = riskValue;
				strategy.TakeProfitPoints = takeProfit;
				strategy.StopLossPoints = stopLoss;
				strategy.TrailingStartPoints = 0m;
				strategy.TrailingGapPoints = 0m;
				strategy.TradeComment = "RRS-test";
				strategy.CandleType = TimeSpan.FromMinutes(5).TimeFrame();
				recorder.Attach(strategy);
			}, replayDuration: TimeSpan.FromDays(2), postTradeHorizon: postTradeHorizon);

			return recorder;
		}

		var fixedVolume = await Replay(
			RrsRandomnessStrategy.TradingModes.DoubleSide,
			0.123m, 0.123m, 1_000_000m,
			RrsRandomnessStrategy.RiskModes.FixedMoney, 1_000_000m);

		fixedVolume.AssertFirstSide(Sides.Buy);
		fixedVolume.AssertFirstVolume(0.123m);
		fixedVolume.AssertFirstComment("RRS-test");

		var oneSide = await Replay(
			RrsRandomnessStrategy.TradingModes.OneSide,
			0.123m, 0.123m, 1_000_000m,
			RrsRandomnessStrategy.RiskModes.FixedMoney, 1_000_000m);
		oneSide.AssertDiffersFrom(fixedVolume, "Changing Mode did not affect submitted orders.");


		var horizon = TimeSpan.FromHours(12);
		var wideRisk = await Replay(
			RrsRandomnessStrategy.TradingModes.DoubleSide,
			100m, 100m, 1_000_000m,
			RrsRandomnessStrategy.RiskModes.FixedMoney, 1_000_000m,
			takeProfit: 0m, stopLoss: 0m, postTradeHorizon: horizon);
		var tightRisk = await Replay(
			RrsRandomnessStrategy.TradingModes.DoubleSide,
			100m, 100m, 1_000_000m,
			RrsRandomnessStrategy.RiskModes.FixedMoney, 0.01m,
			takeProfit: 0m, stopLoss: 0m, postTradeHorizon: horizon);
		var percentageRisk = await Replay(
			RrsRandomnessStrategy.TradingModes.DoubleSide,
			100m, 100m, 1_000_000m,
			RrsRandomnessStrategy.RiskModes.BalancePercentage, 0.01m,
			takeProfit: 0m, stopLoss: 0m, postTradeHorizon: horizon);

		tightRisk.AssertDiffersFrom(wideRisk, "Changing RiskValue did not affect submitted orders.");
		percentageRisk.AssertDiffersFrom(tightRisk, "Changing MoneyRiskMode did not affect submitted orders.");
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
	public Task S2000_HftSpreaderForForts()
		// A full month creates tens of thousands of fills. One natural day still
		// exercises hundreds of entry/exit cycles without turning CI into a load test.
		=> RunStrategy<HftSpreaderForFortsStrategy>(CancellationToken, replayDuration: TimeSpan.FromDays(1));

	[TestMethod]
	[TestCategory("Shard00")]
	public Task S3064_TwoPerbar()
		// This intentionally trades on nearly every bar. A natural one-day window
		// retains high trade coverage without generating ~16k fills per language.
		=> RunStrategy<TwoPerBarStrategy>(CancellationToken, replayDuration: TimeSpan.FromDays(1));

	[TestMethod]
	[TestCategory("Shard00")]
	public Task S4048_BurgExtrapolatorForecast()
		=> RunStrategy<BurgExtrapolatorForecastStrategy>(CancellationToken, replayDuration: TimeSpan.FromDays(1));

	[TestMethod]
	[TestCategory("Shard00")]
	public Task S2096_BreakoutBarsTrend()
		// Compact parameters make the signal reachable in the bundled history window.
		=> RunStrategy<BreakoutBarsTrendStrategy>(CancellationToken, (s, _) =>
		{
			s.Volume = 0.001m;
			s.CandleType = TimeSpan.FromMinutes(5).TimeFrame();
			s.Negatives = 0;
		});

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
	public Task S0365_DispersionTrading()
		=> RunStrategy<DispersionTradingStrategy>(CancellationToken, (s, sec2) => s.Constituents = new[] { sec2 });

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

		await RunStrategy<CointegrationPairsStrategy>(CancellationToken, (strategy, second) =>
		{
			strategy.Asset2 = second;
			strategy.Beta = 1m;
			primary = strategy.Security;
			hedge = second;
			recorder.Attach(strategy);
		});

		recorder.AssertTradesBoth(primary, hedge);
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public Task S0230_DeltaNeutralArbitrage()
		=> RunStrategy<DeltaNeutralArbitrageStrategy>(CancellationToken, (s, sec2) => { s.Asset2Security = sec2; s.Asset2Portfolio = s.Portfolio; });

	[TestMethod]
	[TestCategory("Shard07")]
	public Task S2679_MulticurrencyOverlayHedge()
		=> RunStrategy<MulticurrencyOverlayHedgeStrategy>(CancellationToken, (s, sec2) =>
		{
			s.Universe = new[] { s.Security, sec2 };
			s.CandleType = TimeSpan.FromMinutes(5).TimeFrame();
			s.CorrelationThreshold = 0.01m;
			s.CorrelationLookback = 50;
			s.RangeLength = 20;
			s.AtrLookback = 20;
			s.MaxSpread = 100000m;
			s.OverlayThreshold = 0.001m;
			s.RecalculationHour = 0;
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

		await RunStrategy<ImproveMaRsiHedgeStrategy>(CancellationToken, (strategy, second) =>
		{
			strategy.HedgeSecurity = second;
			primary = strategy.Security;
			hedge = second;
			recorder.Attach(strategy);
		});

		recorder.AssertTradesBoth(primary, hedge);
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
	public Task S1153_Pairs()
		=> RunStrategy<PairsStrategy>(CancellationToken, (s, sec2) => s.ReferenceSecurity = sec2);

	[TestMethod]
	[TestCategory("Shard01")]
	public Task S0217_PairsTrading()
		=> RunStrategy<PairsTradingStrategy>(CancellationToken, (s, sec2) => s.SecondSecurity = sec2);

	[TestMethod]
	[TestCategory("Shard06")]
	public Task S0526_SpotFuturesArbitrage()
		=> RunStrategy<SpotFuturesArbitrageStrategy>(CancellationToken, (s, sec2) => { s.Spot = s.Security; s.Future = sec2; });

	[TestMethod]
	[TestCategory("Shard01")]
	public Task S2705_Spreader2()
		=> RunStrategy<Spreader2Strategy>(CancellationToken, (s, sec2) => { s.SecondSecurity = sec2; s.DayBars = 10; s.ShiftLength = 3; s.TargetProfit = 1m; });

	[TestMethod]
	[TestCategory("Shard03")]
	public Task S0219_StatisticalArbitrage()
		=> RunStrategy<StatisticalArbitrageStrategy>(CancellationToken, (s, sec2) => s.SecondSecurity = sec2);

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

		await RunStrategy<MacdHmmStrategy>(CancellationToken, (strategy, _) =>
		{
			declaredCandleType = strategy.CandleType;
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

		async Task<OrderTraceRecorder> Replay(int hmmHistoryLength)
		{
			var recorder = new OrderTraceRecorder();

			await RunStrategy<MacdHmmStrategy>(CancellationToken, (strategy, _) =>
			{
				strategy.CandleType = TimeSpan.FromMinutes(5).TimeFrame();
				strategy.SignalCooldownBars = 1;
				strategy.HmmHistoryLength = hmmHistoryLength;
				recorder.Attach(strategy);
			}, replayDuration: TimeSpan.FromDays(3));

			return recorder;
		}

		var shortHistory = await Replay(20);
		var longHistory = await Replay(200);

		shortHistory.AssertDiffersFrom(longHistory,
			"HmmHistoryLength is published as the model history and as an optimization range, so its value must change the detected state and the orders that follow from it.");
	}

	/// <summary>
	/// The README declares an ATR stop whose width is StopLossAtr and an ATR whose period is
	/// AtrPeriod. Neither parameter reaches a submitted order today: ApplyAtrStopLoss compares the
	/// candle close with itself, and the Keltner channel is built from EmaPeriod alone. The two
	/// AssertDiffersFrom calls below state the declared behaviour and therefore fail.
	/// </summary>
	[TestMethod]
	[TestCategory("Shard07")]
	public async Task S0343_KeltnerReinforcementLearningSignal()
	{
		// Compact channel settings and a one-bar cooldown make the breakout reachable in the bundled
		// history, so a risk parameter that is actually wired has room to show in the order trace.
		async Task<OrderTraceRecorder> Replay(int atrPeriod, decimal stopLossAtr)
		{
			var recorder = new OrderTraceRecorder();

			await RunStrategy<KeltnerWithRLSignalStrategy>(CancellationToken, (strategy, _) =>
			{
				strategy.CandleType = TimeSpan.FromMinutes(5).TimeFrame();
				strategy.EmaPeriod = 5;
				strategy.AtrMultiplier = 0.2m;
				strategy.CooldownBars = 1;
				strategy.AtrPeriod = atrPeriod;
				strategy.StopLossAtr = stopLossAtr;
				recorder.Attach(strategy);
			}, replayDuration: TimeSpan.FromDays(7));

			return recorder;
		}

		// The defaults, run the way the generated row ran them.
		await RunStrategy<KeltnerWithRLSignalStrategy>(CancellationToken);

		var baseline = await Replay(atrPeriod: 14, stopLossAtr: 5m);
		var tightStop = await Replay(atrPeriod: 14, stopLossAtr: 0.01m);
		var shortAtr = await Replay(atrPeriod: 2, stopLossAtr: 5m);

		// A stop a hundredth of an ATR wide cannot close positions at the same moments as one five
		// ATR wide. The traces are identical because the stop condition compares the candle close
		// with itself and is unreachable for any positive StopLossAtr.
		tightStop.AssertDiffersFrom(baseline, "Changing StopLossAtr did not affect submitted orders: the declared ATR stop never fires.");

		// With EmaPeriod fixed, an ATR of period 2 and one of period 14 give different channel
		// widths and therefore different breakouts. The traces are identical because the channel is
		// created with Length = EmaPeriod only and AtrPeriod is applied nowhere.
		shortAtr.AssertDiffersFrom(baseline, "Changing AtrPeriod did not affect submitted orders: the declared ATR period is never applied.");
	}

	/// <summary>
	/// The README trades the differential between two front-month oil futures: long the cheaper
	/// grade, short the expensive one, both legs closed on convergence. The packaged history carries
	/// two crypto instruments, not two grades of oil. The example is still replayed, but what it
	/// declares cannot be shown on this data, so that half is reported rather than asserted.
	/// </summary>
	[TestMethod]
	[TestCategory("Shard02")]
	public async Task S0410_WtibrentSpread()
	{
		await RunStrategy<WTIBrentSpreadStrategy>(CancellationToken);

		Inconclusive("The packaged history holds two crypto instruments. This example declares a spread between two front-month oil futures, which the history does not contain, so its declared behaviour cannot be exercised here.");
	}

	/// <summary>
	/// The README declares a grid over a predefined price range (UpperLimit 48000, LowerLimit 45000,
	/// GridCount 10). Such a grid is fixed, so this pins both halves of that contract: the declared
	/// parameters themselves, and the fact that no moving-average or ATR setting may move the lines.
	/// </summary>
	[TestMethod]
	[TestCategory("Shard01")]
	public async Task S0425_GridBot()
	{
		static decimal? Declared(GridBotStrategy strategy, string name)
			=> strategy.Parameters.TryGetValue(name, out var param) ? param.Value.To<decimal>() : null;

		// The inputs of a dynamic grid. A predefined range does not read them, so moving them must
		// leave every line - and therefore every order - where the baseline run put it.
		static void Retune(GridBotStrategy strategy)
		{
			if (strategy.Parameters.TryGetValue("MALength", out var maLength))
				maLength.Value = 20;

			if (strategy.Parameters.TryGetValue("ATRLength", out var atrLength))
				atrLength.Value = 7;

			if (strategy.Parameters.TryGetValue("GridMultiplier", out var gridMultiplier))
				gridMultiplier.Value = 0.25m;
		}

		var baseline = new OrderTraceRecorder();
		var retuned = new OrderTraceRecorder();
		decimal? upperLimit = null;
		decimal? lowerLimit = null;
		decimal? gridCount = null;

		await RunStrategy<GridBotStrategy>(CancellationToken, (strategy, _) =>
		{
			upperLimit = Declared(strategy, "UpperLimit");
			lowerLimit = Declared(strategy, "LowerLimit");
			gridCount = Declared(strategy, "GridCount");
			baseline.Attach(strategy);
		});

		// The comparison run need not trade on its own: a fixed grid repeats the baseline orders,
		// and an empty trace would itself mean the lines moved with the retuned indicators.
		await RunStrategy<GridBotStrategy>(CancellationToken, (strategy, _) =>
		{
			Retune(strategy);
			retuned.Attach(strategy);
		});

		retuned.AssertSameAs(baseline);

		Assert.AreEqual<decimal?>(48000m, upperLimit, "The declared grid range has UpperLimit 48000.");
		Assert.AreEqual<decimal?>(45000m, lowerLimit, "The declared grid range has LowerLimit 45000.");
		Assert.AreEqual<decimal?>(10m, gridCount, "The declared grid splits the range into GridCount 10 levels.");
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

		await RunStrategy<CryptoRebalancingPremiumStrategy>(CancellationToken, (strategy, second) =>
		{
			strategy.SecondarySecurityId = second.Id;
			primary = strategy.Security;
			secondary = second;
			recorder.Attach(strategy);
		});

		recorder.AssertTradesBoth(primary, secondary);
	}

	/// <summary>
	/// The README builds this example on funding and lending rates quoted by two venues, with the
	/// spread between them as the signal and a liquidity check as the risk block. The packaged
	/// history carries candles for two crypto instruments and nothing else: no rates, no second
	/// venue. The example is still replayed, but what it declares cannot be shown on this data, so
	/// that half is reported rather than asserted.
	/// </summary>
	[TestMethod]
	[TestCategory("Shard02")]
	public async Task S0402_SyntheticLendingRates()
	{
		await RunStrategy<SyntheticLendingRatesStrategy>(CancellationToken);

		Inconclusive("The packaged history holds candles for two crypto instruments. This example declares funding and lending rates from two venues as its data, which the history does not contain, so its declared behaviour cannot be exercised here.");
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

		await RunStrategy<AdvancedAdaptiveGridStrategy>(CancellationToken, (strategy, _) =>
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
	/// <summary>
	/// 0601 is a session-range/retracement model. Pin the published inputs and verify that
	/// an empty entry window suppresses every order; an EMA-crossover placeholder cannot satisfy this.
	/// </summary>
	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S0806_FootprintBehavior()
	{
		const string path = "0801-0900/0806_Footprint/CS/FootprintStrategy.cs";
		var recorder = new OrderTraceRecorder();

		await RunStrategy(path, CancellationToken, (strategy, _) => recorder.Attach(strategy));

		recorder.AssertFirstSide(Sides.Buy);
	}

	[TestMethod]
	[TestCategory("Shard03")]
	public async Task S0703_QuantumSentimentFluxBehavior()
	{
		const string path = "0701-0800/0703_Quantum_Sentiment_Flux_Beginners/CS/QuantumSentimentFluxBeginnersStrategy.cs";
		var recorder = new OrderTraceRecorder();

		await RunStrategy(path, CancellationToken, (strategy, _) => recorder.Attach(strategy));

		recorder.AssertFirstVolume(1m);
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S2208_HedgeAverageProtectionStartsAfterEntryBar()
	{
		const string path = "2201-2300/2208_Hedge_Average/CS/HedgeAverageStrategy.cs";
		var recorder = new OrderTraceRecorder();

		await RunStrategy(path, CancellationToken, (strategy, _) =>
		{
			strategy.Parameters["Period1"].Value = 2;
			strategy.Parameters["Period2"].Value = 3;
			strategy.Parameters["CandleType"].Value = TimeSpan.FromMinutes(5).TimeFrame();
			strategy.Parameters["StopLoss"].Value = 0.01m;
			strategy.Parameters["TakeProfit"].Value = 0.01m;
			recorder.Attach(strategy);
		}, replayDuration: TimeSpan.FromDays(2));

		recorder.AssertFirstOppositeAfter(TimeSpan.FromTicks(1));
		recorder.AssertFirstOppositeWithin(TimeSpan.FromMinutes(10));
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S3104_MaMacdPositionAveragingProtection()
	{
		const string path = "3101-3200/3104_MA_MACD_Position_Averaging/CS/MaMacdPositionAveragingStrategy.cs";
		var recorder = new OrderTraceRecorder();

		await RunStrategy(path, CancellationToken, (strategy, _) =>
		{
			strategy.Parameters["CandleType"].Value = TimeSpan.FromMinutes(5).TimeFrame();
			strategy.Parameters["MaPeriod"].Value = 3;
			strategy.Parameters["MacdFastPeriod"].Value = 2;
			strategy.Parameters["MacdSlowPeriod"].Value = 4;
			strategy.Parameters["MacdSignalPeriod"].Value = 2;
			strategy.Parameters["IndentPips"].Value = 0;
			strategy.Parameters["MacdRatio"].Value = 0m;
			strategy.Parameters["StopLossPips"].Value = 1;
			strategy.Parameters["TakeProfitPips"].Value = 1;
			recorder.Attach(strategy);
		}, replayDuration: TimeSpan.FromDays(2));

		recorder.AssertFirstOppositeAfter(TimeSpan.FromTicks(1));
		recorder.AssertFirstOppositeWithin(TimeSpan.FromMinutes(10));
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S3206_RiskRewardRatioProtection()
	{
		const string path = "3201-3300/3206_Risk_Reward_Ratio/CS/RiskRewardRatioStrategy.cs";
		var recorder = new OrderTraceRecorder();

		await RunStrategy(path, CancellationToken, (strategy, _) =>
		{
			strategy.Parameters["CandleType"].Value = TimeSpan.FromMinutes(5).TimeFrame();
			strategy.Parameters["FastMaPeriod"].Value = 2;
			strategy.Parameters["SlowMaPeriod"].Value = 3;
			strategy.Parameters["MomentumThreshold"].Value = 0m;
			strategy.Parameters["StopLossPips"].Value = 1;
			strategy.Parameters["RewardRatio"].Value = 1m;
			strategy.Parameters["EnableTrailing"].Value = false;
			strategy.Parameters["EnableBreakEven"].Value = false;
			recorder.Attach(strategy);
		}, replayDuration: TimeSpan.FromDays(2));

		recorder.AssertFirstOppositeAfter(TimeSpan.FromTicks(1));
		recorder.AssertFirstOppositeWithin(TimeSpan.FromMinutes(10));
	}

	[TestMethod]
	[TestCategory("Shard01")]
	public async Task S3801_OrderStabilizationDoesNotLookAhead()
	{
		const string path = "3801-3900/3801_OrderStabilization/CS/OrderStabilizationStrategy.cs";
		var recorder = new OrderTraceRecorder();

		await RunStrategy(path, CancellationToken, (strategy, _) =>
		{
			strategy.Parameters["OrderDistancePoints"].Value = 1m;
			recorder.Attach(strategy);
		});

		recorder.AssertFirstOrderAfterStart(TimeSpan.FromMinutes(5));
		recorder.AssertFirstOppositeAfter(TimeSpan.FromTicks(1));
	}

	[TestMethod]
	[TestCategory("Shard01")]
	public async Task S1801_PerceptronStopEndsCurrentBar()
	{
		const string path = "1801-1900/1801_Artificial_Intelligence_Perceptron/CS/ArtificialIntelligencePerceptronStrategy.cs";
		var recorder = new OrderTraceRecorder();

		await RunStrategy(path, CancellationToken, (strategy, _) =>
		{
			strategy.Parameters["CandleType"].Value = TimeSpan.FromMinutes(5).TimeFrame();
			strategy.Parameters["StopLoss"].Value = 1m;
			recorder.Attach(strategy);
		});

		recorder.AssertAtMostOneOrderPerTimestamp();
	}

	[TestMethod]
	[TestCategory("Shard03")]
	public async Task S2907_CcfpProducesTwoLegNonUsdSignal()
	{
		const string path = "2901-3000/2907_CCFp_Currency_Strength/CS/CcfpCurrencyStrengthStrategy.cs";
		var recorder = new OrderTraceRecorder();

		await RunStrategy(path, CancellationToken, (strategy, second) =>
		{
			var firstId = strategy.Security.Id;
			var secondId = second.Id;
			foreach (var name in new[] { "EURUSD", "AUDUSD", "USDCAD", "USDJPY" })
				strategy.Parameters[name].Value = firstId;
			foreach (var name in new[] { "GBPUSD", "NZDUSD", "USDCHF" })
				strategy.Parameters[name].Value = secondId;
			strategy.Parameters["FastMa"].Value = 2;
			strategy.Parameters["SlowMa"].Value = 3;
			strategy.Parameters["StrengthStep"].Value = 0.000001m;
			strategy.Parameters["CandleType"].Value = TimeSpan.FromMinutes(5).TimeFrame();
			recorder.Attach(strategy);
		});

		recorder.AssertContainsTwoLegSignal("(TOPDOWN)");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task S1101_TrailingExitRequiresNewSignalTransition()
	{
		const string path = "1101-1200/1101_Multi_Timeframe_MACD/CS/MultiTimeframeMacdStrategy.cs";
		var recorder = new OrderTraceRecorder();

		await RunStrategy(path, CancellationToken, (strategy, _) =>
		{
			strategy.Parameters["FastLength"].Value = 2;
			strategy.Parameters["SlowLength"].Value = 3;
			strategy.Parameters["SignalLength"].Value = 2;
			strategy.Parameters["CandleType"].Value = TimeSpan.FromMinutes(5).TimeFrame();
			strategy.Parameters["HigherCandleType"].Value = TimeSpan.FromMinutes(15).TimeFrame();
			strategy.Parameters["UseTrailingStop"].Value = true;
			strategy.Parameters["TrailingStopPercent"].Value = 0.01m;
			recorder.Attach(strategy);
		}, replayDuration: TimeSpan.FromDays(3));

		recorder.AssertNoSameSideReentryWithinAfterFirstExit(TimeSpan.FromMinutes(5));
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S2808_MultiPairCloserClosesExistingPosition()
	{
		const string path = "2801-2900/2808_Multi_Pair_Closer/CS/MultiPairCloserStrategy.cs";
		var recorder = new OrderTraceRecorder();

		await RunStrategy(path, CancellationToken, (strategy, _) =>
		{
			strategy.Parameters["WatchedSymbols"].Value = string.Empty;
			strategy.Parameters["ProfitTarget"].Value = 0m;
			strategy.Parameters["MinAgeSeconds"].Value = 0;
			strategy.Parameters["CandleType"].Value = TimeSpan.FromMinutes(1).TimeFrame();
			strategy.SetPositionValue(strategy.Security, strategy.Portfolio, 1m, DateTime.MinValue);
			recorder.Attach(strategy);
		});

		recorder.AssertFirstSide(Sides.Sell);
		recorder.AssertFirstVolume(1m);
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S3008_OcoArmedLevel1Trigger()
	{
		const string path = "3001-3100/3008_OCO_Pending_Orders/CS/OcoPendingOrdersStrategy.cs";
		var recorder = new OrderTraceRecorder();
		Strategy captured = null;

		await RunStrategy(path, CancellationToken, (strategy, _) =>
		{
			captured = strategy;
			strategy.Parameters["OrderVolume"].Value = 2m;
			strategy.Parameters["BuyStopPrice"].Value = 0.01m;
			strategy.Parameters["UseOcoLink"].Value = true;
			strategy.Parameters["Armed"].Value = true;
			recorder.Attach(strategy);
		});

		recorder.AssertFirstSide(Sides.Buy);
		recorder.AssertFirstVolume(2m);
		AreEqual(false, captured.Parameters["Armed"].Value);
	}

	[TestMethod]
	[TestCategory("Shard03")]
	public async Task S3507_EconomicCalendarArmsNewsStops()
	{
		const string path = "3501-3600/3507_Sample_Detect_Economic_Calendar/CS/SampleDetectEconomicCalendarStrategy.cs";
		var recorder = new OrderTraceRecorder();
		var eventTime = Paths.HistoryBeginDate.Date.AddDays(1).AddHours(12);

		await RunStrategy(path, CancellationToken, (strategy, _) =>
		{
			strategy.Parameters["TradeNews"].Value = true;
			strategy.Parameters["OrderVolume"].Value = 2m;
			strategy.Parameters["StopLossPoints"].Value = 10;
			strategy.Parameters["TakeProfitPoints"].Value = 10;
			strategy.Parameters["TrailingStopPoints"].Value = 0;
			strategy.Parameters["BuyDistancePoints"].Value = 1;
			strategy.Parameters["SellDistancePoints"].Value = 1;
			strategy.Parameters["LeadMinutes"].Value = 60;
			strategy.Parameters["PostMinutes"].Value = 60;
			strategy.Parameters["ExpiryMinutes"].Value = 180;
			strategy.Parameters["BaseCurrency"].Value = "USD";
			strategy.Parameters["CalendarDefinition"].Value = $"{eventTime:yyyy-MM-dd HH:mm};USD;High;Behavior test";
			recorder.Attach(strategy);
		});

		recorder.AssertFirstVolume(2m);
		recorder.AssertFirstOrderAfterStart(TimeSpan.FromMinutes(1));
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S1704_MartiniStartsWithRealStopOrders()
	{
		const string path = "1701-1800/1704_Martini_Martingale/CS/MartiniMartingaleStrategy.cs";
		var recorder = new OrderTraceRecorder();

		await RunStrategy(path, CancellationToken, (strategy, _) => recorder.Attach(strategy));

		recorder.AssertFirstTwoAreOppositeConditionalStops();
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task S3301_CryptoAnalysisProtection()
	{
		const string path = "3301-3400/3301_Crypto_Analysis/CS/CryptoAnalysisStrategy.cs";
		var recorder = new OrderTraceRecorder();

		await RunStrategy(path, CancellationToken, (strategy, _) =>
		{
			strategy.Parameters["CandleType"].Value = TimeSpan.FromMinutes(5).TimeFrame();
			strategy.Parameters["MomentumCandleType"].Value = TimeSpan.FromMinutes(5).TimeFrame();
			strategy.Parameters["MacdCandleType"].Value = TimeSpan.FromMinutes(5).TimeFrame();
			strategy.Parameters["FastMaPeriod"].Value = 2;
			strategy.Parameters["SlowMaPeriod"].Value = 3;
			strategy.Parameters["MomentumPeriod"].Value = 2;
			strategy.Parameters["MomentumBuyThreshold"].Value = 0m;
			strategy.Parameters["MomentumSellThreshold"].Value = 0m;
			strategy.Parameters["MacdFastLength"].Value = 2;
			strategy.Parameters["MacdSlowLength"].Value = 3;
			strategy.Parameters["MacdSignalLength"].Value = 2;
			strategy.Parameters["StopLossPips"].Value = 1;
			strategy.Parameters["TakeProfitPips"].Value = 1;
			strategy.Parameters["TrailingStopPips"].Value = 0;
			strategy.Parameters["UseBreakEven"].Value = false;
			recorder.Attach(strategy);
		}, replayDuration: TimeSpan.FromDays(3));

		recorder.AssertFirstOppositeAfter(TimeSpan.FromTicks(1));
		recorder.AssertFirstOppositeWithin(TimeSpan.FromMinutes(10));
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

}

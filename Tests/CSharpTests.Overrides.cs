namespace StockSharp.Tests;

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
		}, replayDuration: System.TimeSpan.FromDays(7));

		await RunStrategy<VwapWilliamsRStrategy>(CancellationToken, (strategy, _) =>
		{
			strategy.WilliamsRPeriod = 5;
			strategy.CooldownBars = 1;
			strategy.StopLossPercent = 5m;
			wideStop.Attach(strategy);
		}, replayDuration: System.TimeSpan.FromDays(7));

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
		Assert.AreEqual(
			AdvancedPositionManagementStrategy.ProtectiveExitAction.Evaluate,
			AdvancedPositionManagementStrategy.ResolveProtectiveExitAction(0.6m, OrderStates.Done),
			"A terminal 0.4 partial fill must preserve protection and allow retrying the residual 0.6 position.");
		Assert.AreEqual(
			AdvancedPositionManagementStrategy.ProtectiveExitAction.Wait,
			AdvancedPositionManagementStrategy.ResolveProtectiveExitAction(0.6m, OrderStates.Active));
		Assert.AreEqual(
			AdvancedPositionManagementStrategy.ProtectiveExitAction.Reset,
			AdvancedPositionManagementStrategy.ResolveProtectiveExitAction(0m, OrderStates.Done));

		await RunStrategy<AdvancedPositionManagementStrategy>(CancellationToken, (strategy, _) =>
		{
			strategy.FastLength = 3;
			strategy.SlowLength = 8;
			strategy.StopLossPercent = 0.01m;
			strategy.TakeProfitPercent = 0.01m;
			strategy.CooldownBars = 1;
		}, replayDuration: System.TimeSpan.FromDays(7));
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
			}, replayDuration: System.TimeSpan.FromDays(7));

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
			}, replayDuration: System.TimeSpan.FromDays(7));

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
				strategy.CandleType = System.TimeSpan.FromMinutes(5).TimeFrame();
				strategy.SmaPeriod = 2;
				strategy.SleepBars = 1;
				strategy.MinStopLevel = 0.0001m;
				strategy.TrailingStep = 0.0001m;
				strategy.RandomSeed = randomSeed;
				recorder.Attach(strategy);
			}, replayDuration: System.TimeSpan.FromDays(1), postTradeHorizon: System.TimeSpan.FromHours(1));

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

		Assert.IsFalse(stopHit, "A stop tightened from the current candle must not trigger against that candle's earlier high.");
		Assert.AreEqual(100.5m, nextStop);
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task S2101_LinearRegressionSlopeV1()
	{
		var slope = LinearRegressionSlopeV1Strategy.CreateSlopeIndicator(3);
		IIndicatorValue value = null;
		var start = new System.DateTime(2024, 1, 1);
		var prices = new[] { 100m, 102m, 104m };

		for (var i = 0; i < prices.Length; i++)
			value = slope.Process(new DecimalIndicatorValue(slope, prices[i], start.AddMinutes(i)) { IsFinal = true });

		Assert.AreEqual(2m, value.GetValue<decimal>(), "The strategy indicator must output the regression coefficient, not the fitted price.");

		var recorder = new OrderTraceRecorder();
		await RunStrategy<LinearRegressionSlopeV1Strategy>(CancellationToken, (strategy, _) =>
		{
			strategy.CandleType = System.TimeSpan.FromMinutes(5).TimeFrame();
			strategy.Length = 3;
			strategy.TriggerShift = 1;
			strategy.StopLossPct = 100m;
			strategy.TakeProfitPct = 100m;
			recorder.Attach(strategy);
		}, replayDuration: System.TimeSpan.FromDays(1));

		recorder.AssertFirstVolume(1m);
	}

	[TestMethod]
	[TestCategory("Shard03")]
	public async Task S2403_ReOpenPositions()
	{
		var recorder = new OrderTraceRecorder();

		await RunStrategy<ReOpenPositionsStrategy>(CancellationToken, (strategy, _) =>
		{
			strategy.CandleType = System.TimeSpan.FromMinutes(5).TimeFrame();
			strategy.ProfitThreshold = -1_000_000m;
			strategy.MaxPositions = 3;
			strategy.StopLossPoints = 100m;
			strategy.TakeProfitPoints = 100m;
			recorder.Attach(strategy);
		}, replayDuration: System.TimeSpan.FromDays(2));

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
			strategy.CandleType = System.TimeSpan.FromHours(4).TimeFrame();
			strategy.FirstSessionStartHour = 20;
			strategy.FirstSessionStopHour = 21;
			strategy.StepPoints = 1m;
			strategy.TakeProfitPoints = 1_000_000m;
			recorder.Attach(strategy);
		}, replayDuration: System.TimeSpan.FromDays(2));

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
		}, replayDuration: System.TimeSpan.FromDays(2));

		recorder.AssertFirstVolume(3m);
		recorder.AssertContainsVolume(6m);
	}

	[TestMethod]
	[TestCategory("Shard07")]
	public async Task S2703_SelfOptimizingRsiOrMfiTraderV3()
	{
		Assert.AreEqual(
			SelfOptimizingRsiOrMfiTraderV3Strategy.ProtectiveExitAction.Evaluate,
			SelfOptimizingRsiOrMfiTraderV3Strategy.ResolveProtectiveExitAction(0.6m, OrderStates.Done),
			"A terminal 0.4 partial fill must preserve protection and allow retrying the residual 0.6 position.");
		Assert.AreEqual(
			SelfOptimizingRsiOrMfiTraderV3Strategy.ProtectiveExitAction.Wait,
			SelfOptimizingRsiOrMfiTraderV3Strategy.ResolveProtectiveExitAction(0.6m, OrderStates.Active));
		Assert.AreEqual(
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
			}, replayDuration: System.TimeSpan.FromHours(4));

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
		}, replayDuration: System.TimeSpan.FromDays(1), postTradeHorizon: System.TimeSpan.FromMinutes(20));

		breakEven.AssertFirstOppositeAfter(System.TimeSpan.FromMinutes(10));
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S3104_MaMacdPositionAveraging()
	{
		var recorder = new OrderTraceRecorder();

		await RunStrategy<MaMacdPositionAveragingStrategy>(CancellationToken, (strategy, _) =>
		{
			strategy.FastPeriod = 2;
			strategy.SlowPeriod = 3;
			strategy.StopLossPoints = 1;
			strategy.TakeProfitPoints = 1;
			recorder.Attach(strategy);
		}, replayDuration: System.TimeSpan.FromDays(2));

		recorder.AssertFirstOppositeWithin(System.TimeSpan.FromMinutes(10));
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S3206_RiskRewardRatio()
	{
		var recorder = new OrderTraceRecorder();

		await RunStrategy<RiskRewardRatioStrategy>(CancellationToken, (strategy, _) =>
		{
			strategy.FastPeriod = 2;
			strategy.SlowPeriod = 3;
			strategy.StopLossPoints = 1;
			strategy.TakeProfitPoints = 1;
			recorder.Attach(strategy);
		}, replayDuration: System.TimeSpan.FromDays(2));

		recorder.AssertFirstOppositeWithin(System.TimeSpan.FromMinutes(10));
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task S3301_CryptoAnalysis()
	{
		var recorder = new OrderTraceRecorder();

		await RunStrategy<CryptoAnalysisStrategy>(CancellationToken, (strategy, _) =>
		{
			strategy.FastPeriod = 2;
			strategy.SlowPeriod = 3;
			strategy.StopLossPoints = 1;
			strategy.TakeProfitPoints = 1;
			recorder.Attach(strategy);
		}, replayDuration: System.TimeSpan.FromDays(2));

		recorder.AssertFirstOppositeWithin(System.TimeSpan.FromMinutes(10));
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
			System.TimeSpan? postTradeHorizon = null,
			bool requireTrades = true)
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
				strategy.CandleType = System.TimeSpan.FromMinutes(5).TimeFrame();
				recorder.Attach(strategy);
			}, replayDuration: System.TimeSpan.FromDays(2), postTradeHorizon: postTradeHorizon, requireTrades: requireTrades);

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

		var spreadBlocked = await Replay(
			RrsRandomnessStrategy.TradingModes.DoubleSide,
			0.123m, 0.123m, 0m,
			RrsRandomnessStrategy.RiskModes.FixedMoney, 1_000_000m,
			requireTrades: false);
		spreadBlocked.AssertEmpty("MaxSpreadPoints=0 must block new entries.");

		var horizon = System.TimeSpan.FromHours(12);
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
		Assert.AreEqual(4m, TenPipsOppositeLastNHourTrendStrategy.ApplyLossMultipliers(1m, [5m, -10m], multipliers));
		Assert.AreEqual(2m, TenPipsOppositeLastNHourTrendStrategy.ApplyLossMultipliers(1m, [-10m, 5m], multipliers));
		Assert.AreEqual(5m, TenPipsOppositeLastNHourTrendStrategy.ApplyLossMultipliers(1m, [-10m, 5m, 5m], multipliers));
		Assert.AreEqual(1m, TenPipsOppositeLastNHourTrendStrategy.ApplyLossMultipliers(1m, [5m, 5m], multipliers));

		var openedAt = new System.DateTimeOffset(2024, 3, 1, 7, 0, 0, System.TimeSpan.Zero);
		var partialEpisode = new TenPipsTradeEpisode();
		partialEpisode.RegisterEntry(100m, 1m, Sides.Buy, openedAt);
		var firstPartialProfit = partialEpisode.RegisterExit(90m, 0.4m);
		Assert.IsFalse(firstPartialProfit.HasValue, "A partial fill must not create a closed-trade history item.");
		Assert.AreEqual(0.6m, partialEpisode.Volume);
		Assert.AreEqual(openedAt, partialEpisode.EntryTime.Value, "A partial exit must not restart OrderMaxAge.");
		var partialProfit = partialEpisode.RegisterExit(110m, 0.6m);

		var singleEpisode = new TenPipsTradeEpisode();
		singleEpisode.RegisterEntry(100m, 1m, Sides.Buy, openedAt);
		var singleProfit = singleEpisode.RegisterExit(102m, 1m);

		Assert.AreEqual(2m, partialProfit.Value);
		Assert.AreEqual(singleProfit, partialProfit, "Equivalent partial and single exits must produce one identical economic result.");

		Assert.AreEqual(1, TenPipsOppositeLastNHourTrendStrategy.DetermineDirection([110m, 100m], 1));
		Assert.AreEqual(-1, TenPipsOppositeLastNHourTrendStrategy.DetermineDirection([100m, 110m], 1));
		Assert.AreEqual(1, TenPipsOppositeLastNHourTrendStrategy.DetermineDirection([110m, 90m, 100m], 2));
		Assert.AreEqual(-1, TenPipsOppositeLastNHourTrendStrategy.DetermineDirection([100m, 120m, 110m], 2));
		Assert.AreEqual(0, TenPipsOppositeLastNHourTrendStrategy.DetermineDirection([100m], 1));

		async Task<OrderTraceRecorder> Replay(decimal priceStep, int decimals, decimal protectionPips, int hoursToCheckTrend = 3, EntrySideRecorder entryRecorder = null)
		{
			var recorder = new OrderTraceRecorder();

			await RunStrategy<TenPipsOppositeLastNHourTrendStrategy>(CancellationToken, (strategy, _) =>
			{
				strategy.Security.PriceStep = priceStep;
				strategy.Security.Decimals = decimals;
				strategy.CandleType = System.TimeSpan.FromMinutes(5).TimeFrame();
				strategy.TradingHour = 7;
				strategy.HoursToCheckTrend = hoursToCheckTrend;
				strategy.FixedVolume = 0.1m;
				strategy.StopLossPips = protectionPips;
				strategy.TakeProfitPips = protectionPips;
				strategy.TrailingStopPips = 0m;
				recorder.Attach(strategy);
				entryRecorder?.Attach(strategy);
			}, replayDuration: System.TimeSpan.FromDays(1), postTradeHorizon: System.TimeSpan.FromHours(1));

			return recorder;
		}

		var threeDigit = await Replay(0.001m, 3, 50_000m);
		var fiveDigit = await Replay(0.00001m, 5, 5_000_000m);

		fiveDigit.AssertSameAs(threeDigit);

		var dailyEntries = new EntrySideRecorder();
		var dailyOrders = await Replay(0.001m, 3, 1m, entryRecorder: dailyEntries);
		dailyOrders.AssertFirstOppositeWithin(System.TimeSpan.FromMinutes(20));
		dailyEntries.AssertMaximumEntriesPerDay(1);

		await Replay(0.01m, 2, 5_000_000m, 1);
		await Replay(0.01m, 2, 5_000_000m, 2);

		var trailing = new OrderTraceRecorder();
		await RunStrategy<TenPipsOppositeLastNHourTrendStrategy>(CancellationToken, (strategy, _) =>
		{
			strategy.Security.PriceStep = 1m;
			strategy.Security.Decimals = 2;
			strategy.CandleType = System.TimeSpan.FromMinutes(5).TimeFrame();
			strategy.TradingHour = 7;
			strategy.HoursToCheckTrend = 3;
			strategy.FixedVolume = 0.1m;
			strategy.StopLossPips = 0m;
			strategy.TakeProfitPips = 0m;
			strategy.TrailingStopPips = 100m;
			trailing.Attach(strategy);
		}, replayDuration: System.TimeSpan.FromDays(1), postTradeHorizon: System.TimeSpan.FromHours(1));

		trailing.AssertFirstOppositeAfter(System.TimeSpan.FromMinutes(10));
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public Task S2000_HftSpreaderForForts()
		// A full month creates tens of thousands of fills. One natural day still
		// exercises hundreds of entry/exit cycles without turning CI into a load test.
		=> RunStrategy<HftSpreaderForFortsStrategy>(CancellationToken, replayDuration: System.TimeSpan.FromDays(1));

	[TestMethod]
	[TestCategory("Shard00")]
	public Task S3064_TwoPerbar()
		// This intentionally trades on nearly every bar. A natural one-day window
		// retains high trade coverage without generating ~16k fills per language.
		=> RunStrategy<TwoPerBarStrategy>(CancellationToken, replayDuration: System.TimeSpan.FromDays(1));

	[TestMethod]
	[TestCategory("Shard00")]
	public Task S4048_BurgExtrapolatorForecast()
		=> RunStrategy<BurgExtrapolatorForecastStrategy>(CancellationToken, replayDuration: System.TimeSpan.FromDays(1));

	[TestMethod]
	[TestCategory("Shard00")]
	public Task S2096_BreakoutBarsTrend()
		// Compact parameters make the signal reachable in the bundled history window.
		=> RunStrategy<BreakoutBarsTrendStrategy>(CancellationToken, (s, _) =>
		{
			s.Volume = 0.001m;
			s.CandleType = System.TimeSpan.FromMinutes(5).TimeFrame();
			s.Negatives = 0;
		});

	[TestMethod]
	[TestCategory("Shard00")]
	public Task S2776_Ch2010Structure()
		=> RunStrategy<Ch2010StructureStrategy>(CancellationToken, (s, sec2) =>
		{
			s.UsdChfSecurity = s.Security;
			s.GbpUsdSecurity = sec2;
			s.DailyCandleType = System.TimeSpan.FromHours(1).TimeFrame();
			s.IntradayCandleType = System.TimeSpan.FromMinutes(5).TimeFrame();
			s.MinTradeVolume = 0.001m;
		});

	[TestMethod]
	[TestCategory("Shard05")]
	public Task S0365_DispersionTrading()
		=> RunStrategy<DispersionTradingStrategy>(CancellationToken, (s, sec2) => s.Constituents = new[] { sec2 });

	[TestMethod]
	[TestCategory("Shard06")]
	public Task S0222_CointegrationPairs()
		=> RunStrategy<CointegrationPairsStrategy>(CancellationToken, (s, sec2) => s.Asset2 = sec2);

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
			s.CandleType = System.TimeSpan.FromMinutes(5).TimeFrame();
			s.CorrelationThreshold = 0.01m;
			s.CorrelationLookback = 50;
			s.RangeLength = 20;
			s.AtrLookback = 20;
			s.MaxSpread = 100000m;
			s.OverlayThreshold = 0.001m;
			s.RecalculationHour = 0;
		});

	[TestMethod]
	[TestCategory("Shard06")]
	public Task S2798_ImproveMaRsiHedge()
		=> RunStrategy<ImproveMaRsiHedgeStrategy>(CancellationToken, (s, sec2) => s.HedgeSecurity = sec2);

	[TestMethod]
	[TestCategory("Shard05")]
	public Task S0333_KeltnerSeasonalFilter()
		// Compact periods make the signal reachable in the bundled history window.
		=> RunStrategy<KeltnerSeasonalStrategy>(CancellationToken, (s, _) =>
		{
			s.EmaPeriod = 2;
			s.AtrPeriod = 2;
			s.AtrMultiplier = 0.01m;
			s.SeasonalThreshold = 0m;
			s.CandleType = System.TimeSpan.FromMinutes(5).TimeFrame();
		});

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
}

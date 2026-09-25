namespace StockSharp.Tests;

using System;
using System.Linq;
using System.Threading.Tasks;

using Ecng.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using StockSharp.Samples.Strategies;

partial class PythonTests
{
	private static void SetParam(Strategy s, string name, object value)
	{
		if (!s.Parameters.TryGetValue(name, out var param))
			throw new InvalidOperationException($"Parameter '{name}' not found. Available: {string.Join(", ", System.Linq.Enumerable.Select(s.Parameters.CachedKeys, k => k.ToString()))}");
		param.Value = value;
	}

	[TestMethod]
	[TestCategory("Shard01")]
	public async Task S0001_MaCrossover()
	{
		var recorder = new OrderTraceRecorder();
		Strategy strategy = null;

		await RunStrategy(
			"0001-0100/0001_MA_CrossOver/PY/ma_crossover_strategy.py", CancellationToken,
			(current, _) =>
			{
				strategy = current;
				SetParam(current, "FastLength", 5);
				SetParam(current, "SlowLength", 20);
				SetParam(current, "StopLossPercent", 100.0);
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

		await RunStrategy(
			"0201-0300/0201_VWAP_Williams_R/PY/vwap_williams_r_strategy.py", CancellationToken,
			(strategy, _) =>
			{
				SetParam(strategy, "WilliamsRPeriod", 5);
				SetParam(strategy, "CooldownBars", 1);
				SetParam(strategy, "StopLossPercent", 0.5);
				tightStop.Attach(strategy);
			},
			replayDuration: TimeSpan.FromDays(7));

		await RunStrategy(
			"0201-0300/0201_VWAP_Williams_R/PY/vwap_williams_r_strategy.py", CancellationToken,
			(strategy, _) =>
			{
				SetParam(strategy, "WilliamsRPeriod", 5);
				SetParam(strategy, "CooldownBars", 1);
				SetParam(strategy, "StopLossPercent", 5.0);
				wideStop.Attach(strategy);
			},
			replayDuration: TimeSpan.FromDays(7));

		tightStop.AssertDiffersFrom(wideStop, "Changing StopLossPercent did not affect submitted orders.");
	}

	[TestMethod]
	[TestCategory("Shard01")]
	public async Task S0401_SoccerClubsArbitrage()
	{
		var recorder = new PairedOrderRecorder();
		Strategy strategy = null;
		Security secondSecurity = null;

		await RunStrategy(
			"0401-0500/0401_Soccer_Clubs_Arbitrage/PY/soccer_clubs_arbitrage_strategy.py", CancellationToken,
			(current, second) =>
			{
				strategy = current;
				secondSecurity = second;
				SetParam(current, "Security2Id", second.Id);
				recorder.Attach(current);
			});

		recorder.AssertBalanced(strategy.Security, secondSecurity);
	}

	[TestMethod]
	[TestCategory("Shard07")]
	public async Task S0503_AdvancedPositionManagement()
	{
		await RunStrategy(
			"0501-0600/0503_Advanced_Position_Management/PY/advanced_position_management_strategy.py", CancellationToken,
			(strategy, _) =>
			{
				dynamic implementation = strategy;
				AreEqual(2, (int)implementation._resolve_protective_exit_action(0.6, OrderStates.Done),
					"A terminal 0.4 partial fill must preserve protection and allow retrying the residual 0.6 position.");
				AreEqual(1, (int)implementation._resolve_protective_exit_action(0.6, OrderStates.Active));
				AreEqual(0, (int)implementation._resolve_protective_exit_action(0.0, OrderStates.Done));
				SetParam(strategy, "FastLength", 3);
				SetParam(strategy, "SlowLength", 8);
				SetParam(strategy, "StopLossPercent", 0.01);
				SetParam(strategy, "TakeProfitPercent", 0.01);
				SetParam(strategy, "CooldownBars", 1);
			},
			replayDuration: TimeSpan.FromDays(7));
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task S1005_MaWithLogistic()
	{
		async Task<OrderTraceRecorder> Replay(double takeProfit, double stopLoss)
		{
			var recorder = new OrderTraceRecorder();

			await RunStrategy(
				"1001-1100/1005_MA_With_Logistic/PY/ma_with_logistic_strategy.py", CancellationToken,
				(strategy, _) =>
				{
					SetParam(strategy, "FastLength", 3);
					SetParam(strategy, "SlowLength", 8);
					SetParam(strategy, "TakeProfitPercent", takeProfit);
					SetParam(strategy, "StopLossPercent", stopLoss);
					recorder.Attach(strategy);
				},
				replayDuration: TimeSpan.FromDays(7));

			return recorder;
		}

		var wideProtection = await Replay(100.0, 100.0);
		var tightTakeProfit = await Replay(0.5, 100.0);
		var tightStopLoss = await Replay(100.0, 0.5);

		tightTakeProfit.AssertDiffersFrom(wideProtection, "Changing TakeProfitPercent did not affect submitted orders.");
		tightStopLoss.AssertDiffersFrom(wideProtection, "Changing StopLossPercent did not affect submitted orders.");
	}

	[TestMethod]
	[TestCategory("Shard03")]
	public async Task S1507_UltimateTemplate()
	{
		async Task<OrderTraceRecorder> Replay(int fastLength, int slowLength, double takeProfit, double stopLoss, bool assertDefaults = false)
		{
			var recorder = new OrderTraceRecorder();

			await RunStrategy(
				"1501-1600/1507_Ultimate_Template/PY/ultimate_template_strategy.py", CancellationToken,
				(strategy, _) =>
				{
					if (assertDefaults)
					{
						IsTrue(strategy.Parameters.TryGetValue("TakeProfitPercent", out var parameter));
						AreEqual(typeof(double), parameter.Value.GetType(), "TakeProfitPercent must be numeric.");
						AreEqual(3.0, (double)parameter.Value, "Unexpected TakeProfitPercent default.");
					}

					SetParam(strategy, "FastLength", fastLength);
					SetParam(strategy, "SlowLength", slowLength);
					SetParam(strategy, "TakeProfitPercent", takeProfit);
					SetParam(strategy, "StopLossPercent", stopLoss);
					recorder.Attach(strategy);
				},
				replayDuration: TimeSpan.FromDays(7));

			return recorder;
		}

		var baseline = await Replay(9, 21, 100.0, 100.0, assertDefaults: true);
		var alternatePeriods = await Replay(2, 60, 100.0, 100.0);
		var tightTakeProfit = await Replay(9, 21, 0.5, 100.0);
		var tightStopLoss = await Replay(9, 21, 100.0, 0.01);

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

			await RunStrategy(
				"1901-2000/1908_Random_Trailing_Stop/PY/random_trailing_stop_strategy.py", CancellationToken,
				(strategy, _) =>
				{
					SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
					SetParam(strategy, "SmaPeriod", 2);
					SetParam(strategy, "SleepBars", 1);
					SetParam(strategy, "MinStopLevel", 0.0001);
					SetParam(strategy, "TrailingStep", 0.0001);
					SetParam(strategy, "RandomSeed", randomSeed);
					recorder.Attach(strategy);
				},
				replayDuration: TimeSpan.FromDays(1),
				postTradeHorizon: TimeSpan.FromHours(1));

			return recorder;
		}

		var expected = new EntrySideRecorder();
		await CSharpTests.RunStrategy<RandomTrailingStopStrategy>(CancellationToken, (strategy, _) =>
		{
			strategy.CandleType = TimeSpan.FromMinutes(5).TimeFrame();
			strategy.SmaPeriod = 2;
			strategy.SleepBars = 1;
			strategy.MinStopLevel = 0.0001m;
			strategy.TrailingStep = 0.0001m;
			strategy.RandomSeed = 42;
			expected.Attach(strategy);
		}, replayDuration: TimeSpan.FromDays(1), postTradeHorizon: TimeSpan.FromHours(1));

		var baseline = await Replay(42);
		var repeated = await Replay(42);
		var alternate = await Replay(43);

		baseline.AssertSupportsBothSides();
		baseline.AssertSameAs(expected);
		repeated.AssertSameAs(baseline);
		alternate.AssertDiffersFrom(baseline);
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task S2101_LinearRegressionSlopeV1()
	{
		var expected = new OrderTraceRecorder();
		await CSharpTests.RunStrategy<LinearRegressionSlopeV1Strategy>(CancellationToken, (strategy, _) =>
		{
			strategy.CandleType = TimeSpan.FromMinutes(5).TimeFrame();
			strategy.Length = 3;
			strategy.TriggerShift = 1;
			strategy.StopLossPct = 100m;
			strategy.TakeProfitPct = 100m;
			expected.Attach(strategy);
		}, replayDuration: TimeSpan.FromDays(1));

		var actual = new OrderTraceRecorder();
		await RunStrategy(
			"2101-2200/2101_Linear_Regression_Slope_V1/PY/linear_regression_slope_v1_strategy.py", CancellationToken,
			(strategy, _) =>
			{
				SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
				SetParam(strategy, "Length", 3);
				SetParam(strategy, "TriggerShift", 1);
				SetParam(strategy, "StopLossPct", 100.0);
				SetParam(strategy, "TakeProfitPct", 100.0);
				actual.Attach(strategy);
			},
			replayDuration: TimeSpan.FromDays(1));

		actual.AssertSameAs(expected);
	}

	[TestMethod]
	[TestCategory("Shard03")]
	public async Task S2403_ReOpenPositions()
	{
		var recorder = new OrderTraceRecorder();

		await RunStrategy(
			"2401-2500/2403_ReOpen_Positions/PY/re_open_positions_strategy.py", CancellationToken,
			(strategy, _) =>
			{
				SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
				SetParam(strategy, "ProfitThreshold", -1_000_000.0);
				SetParam(strategy, "MaxPositions", 3);
				SetParam(strategy, "StopLossPoints", 100.0);
				SetParam(strategy, "TakeProfitPoints", 100.0);
				recorder.Attach(strategy);
			},
			replayDuration: TimeSpan.FromDays(2));

		recorder.AssertFirstSide(Sides.Buy);
		recorder.AssertBasketExitAfterEntries(3, 3m);
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S2502_21HourSessionBreakout()
	{
		var recorder = new OrderTraceRecorder();

		await RunStrategy(
			"2501-2600/2502_21Hour_Session_Breakout/PY/twenty_one_hour_session_breakout_strategy.py", CancellationToken,
			(strategy, _) =>
			{
				strategy.Security.PriceStep = 0.01m;
				SetParam(strategy, "CandleType", TimeSpan.FromHours(4).TimeFrame());
				SetParam(strategy, "FirstSessionStartHour", 20);
				SetParam(strategy, "FirstSessionStopHour", 21);
				SetParam(strategy, "StepPoints", 1.0);
				SetParam(strategy, "TakeProfitPoints", 1_000_000.0);
				recorder.Attach(strategy);
			},
			replayDuration: TimeSpan.FromDays(2));

		recorder.AssertFirstOrderHour(20);
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S2606_StatisticsRepeatingBehavior()
	{
		var recorder = new OrderTraceRecorder();

		await RunStrategy(
			"2601-2700/2606_Statistics_Repeating_Behavior/PY/statistics_repeating_behavior_strategy.py", CancellationToken,
			(strategy, _) =>
			{
				strategy.Security.VolumeStep = 0.5m;
				strategy.Security.MinVolume = 1m;
				strategy.Security.MaxVolume = 10m;
				SetParam(strategy, "InitialVolume", 3.4);
				SetParam(strategy, "MartingaleFactor", 2.0);
				SetParam(strategy, "StopLossPips", 1);
				recorder.Attach(strategy);
			},
			replayDuration: TimeSpan.FromDays(2));

		recorder.AssertFirstVolume(3m);
		recorder.AssertContainsVolume(6m);

		var expected = new OrderTraceRecorder();
		var actual = new OrderTraceRecorder();

		await CSharpTests.RunStrategy<StatisticsRepeatingBehaviorStrategy>(CancellationToken, (strategy, _) =>
		{
			strategy.Security.PriceStep = 0.01m;
			strategy.Security.Decimals = 2;
			strategy.MinimumBodyPoints = 10_000;
			strategy.StopLossPips = 1_000;
			expected.Attach(strategy);
		}, replayDuration: TimeSpan.FromDays(3), requireTrades: false);

		await RunStrategy(
			"2601-2700/2606_Statistics_Repeating_Behavior/PY/statistics_repeating_behavior_strategy.py", CancellationToken,
			(strategy, _) =>
			{
				strategy.Security.PriceStep = 0.01m;
				strategy.Security.Decimals = 2;
				SetParam(strategy, "MinimumBodyPoints", 10_000);
				SetParam(strategy, "StopLossPips", 1_000);
				actual.Attach(strategy);
			},
			replayDuration: TimeSpan.FromDays(3),
			requireTrades: false);

		actual.AssertSameAs(expected);
	}

	[TestMethod]
	[TestCategory("Shard07")]
	public async Task S2703_SelfOptimizingRsiOrMfiTraderV3()
	{
		var exitStateChecked = false;

		async Task<OrderTraceRecorder> Replay(bool useDynamicVolume)
		{
			var recorder = new OrderTraceRecorder();

			await RunStrategy(
				"2701-2800/2703_Self_Optimizing_RSI_or_MFI_Trader_v3/PY/self_optimizing_rsi_or_mfi_trader_v3_strategy.py", CancellationToken,
				(strategy, _) =>
				{
					if (!exitStateChecked)
					{
						dynamic implementation = strategy;
						AreEqual(2, (int)implementation._resolve_protective_exit_action(0.6, OrderStates.Done),
							"A terminal 0.4 partial fill must preserve protection and allow retrying the residual 0.6 position.");
						AreEqual(1, (int)implementation._resolve_protective_exit_action(0.6, OrderStates.Active));
						AreEqual(0, (int)implementation._resolve_protective_exit_action(0.0, OrderStates.Done));
						exitStateChecked = true;
					}

					strategy.Security.PriceStep = 1m;
					strategy.Security.MaxVolume = 10m;
					SetParam(strategy, "OptimizingPeriods", 20);
					SetParam(strategy, "IndicatorPeriod", 5);
					SetParam(strategy, "UseAggressiveEntries", true);
					SetParam(strategy, "UseDynamicTargets", false);
					SetParam(strategy, "StaticStopLossPoints", 1);
					SetParam(strategy, "StaticTakeProfitPoints", 1);
					SetParam(strategy, "UseDynamicVolume", useDynamicVolume);
					SetParam(strategy, "RiskPercent", 10.0);
					SetParam(strategy, "BaseVolume", 3.0);
					recorder.Attach(strategy);
				},
				replayDuration: TimeSpan.FromHours(4));

			return recorder;
		}

		var staticVolume = await Replay(useDynamicVolume: false);
		var dynamicVolume = await Replay(useDynamicVolume: true);

		staticVolume.AssertFirstVolume(3m);
		dynamicVolume.AssertFirstVolume(10m);

		var breakEven = new OrderTraceRecorder();
		await RunStrategy(
			"2701-2800/2703_Self_Optimizing_RSI_or_MFI_Trader_v3/PY/self_optimizing_rsi_or_mfi_trader_v3_strategy.py", CancellationToken,
			(strategy, _) =>
			{
				strategy.Security.PriceStep = 0.01m;
				SetParam(strategy, "OptimizingPeriods", 20);
				SetParam(strategy, "IndicatorPeriod", 5);
				SetParam(strategy, "UseAggressiveEntries", true);
				SetParam(strategy, "UseDynamicTargets", false);
				SetParam(strategy, "StaticStopLossPoints", 50_000);
				SetParam(strategy, "StaticTakeProfitPoints", 50_000);
				SetParam(strategy, "UseDynamicVolume", false);
				SetParam(strategy, "BaseVolume", 1.0);
				SetParam(strategy, "UseBreakEven", true);
				SetParam(strategy, "BreakEvenTriggerPoints", 1);
				SetParam(strategy, "BreakEvenPaddingPoints", 1);
				breakEven.Attach(strategy);
			},
			replayDuration: TimeSpan.FromDays(1),
			postTradeHorizon: TimeSpan.FromMinutes(20));

		breakEven.AssertFirstOppositeAfter(TimeSpan.FromMinutes(10));
	}

	[TestMethod]
	[TestCategory("Shard07")]
	public async Task S3623_MatrixMachineLearning()
	{
		var expected = new OrderTraceRecorder();
		var actual = new OrderTraceRecorder();

		await CSharpTests.RunStrategy<MatrixMachineLearningStrategy>(CancellationToken, (strategy, _) =>
		{
			strategy.Volume = 3m;
			strategy.HistoryDepth = 50;
			strategy.ForwardDepth = 10;
			strategy.PredictorLength = 4;
			strategy.ForecastLength = 3;
			strategy.MaxIterations = 20;
			expected.Attach(strategy);
		});

		await RunStrategy(
			"3601-3700/3623_Matrix_Machine_Learning/PY/matrix_machine_learning_strategy.py", CancellationToken,
			(strategy, _) =>
			{
				strategy.Volume = 3m;
				SetParam(strategy, "HistoryDepth", 50);
				SetParam(strategy, "ForwardDepth", 10);
				SetParam(strategy, "PredictorLength", 4);
				SetParam(strategy, "ForecastLength", 3);
				SetParam(strategy, "MaxIterations", 20);
				actual.Attach(strategy);
			});

		actual.AssertSameAs(expected);
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S3710_Rrsrandomness()
	{
		async Task<OrderTraceRecorder> Replay(
			int mode,
			double minVolume,
			double maxVolume,
			double maxSpread,
			int riskMode,
			double riskValue,
			double takeProfit = 1.0,
			double stopLoss = 1.0,
			TimeSpan? postTradeHorizon = null,
			bool requireTrades = true,
			bool assertParameters = false)
		{
			var recorder = new OrderTraceRecorder();

			await RunStrategy(
				"3701-3800/3710_RRSRandomness/PY/rrs_randomness_strategy.py", CancellationToken,
				(strategy, _) =>
				{
					if (assertParameters)
					{
						(string name, Type type)[] expected =
						[
							("Mode", typeof(int)),
							("MinVolume", typeof(double)),
							("MaxVolume", typeof(double)),
							("TakeProfitPoints", typeof(double)),
							("StopLossPoints", typeof(double)),
							("TrailingStartPoints", typeof(double)),
							("TrailingGapPoints", typeof(double)),
							("MaxSpreadPoints", typeof(double)),
							("SlippagePoints", typeof(double)),
							("MoneyRiskMode", typeof(int)),
							("RiskValue", typeof(double)),
							("TradeComment", typeof(string)),
							("CandleType", typeof(DataType)),
						];

						foreach (var (name, type) in expected)
						{
							IsTrue(strategy.Parameters.TryGetValue(name, out var parameter), $"Python strategy has no '{name}' parameter.");
							AreEqual(type, parameter.Value.GetType(), $"Python parameter '{name}' has an unexpected type.");
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
					SetParam(strategy, "TrailingStartPoints", 0.0);
					SetParam(strategy, "TrailingGapPoints", 0.0);
					SetParam(strategy, "TradeComment", "RRS-test");
					SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
					recorder.Attach(strategy);
				},
				replayDuration: TimeSpan.FromDays(2),
				postTradeHorizon: postTradeHorizon,
				requireTrades: requireTrades);

			return recorder;
		}

		var fixedVolume = await Replay(0, 0.123, 0.123, 1_000_000.0, 0, 1_000_000.0, assertParameters: true);
		fixedVolume.AssertFirstSide(Sides.Buy);
		fixedVolume.AssertFirstVolume(0.123m);
		fixedVolume.AssertFirstComment("RRS-test");

		var oneSide = await Replay(1, 0.123, 0.123, 1_000_000.0, 0, 1_000_000.0);
		oneSide.AssertDiffersFrom(fixedVolume, "Changing Mode did not affect submitted orders.");

		var spreadBlocked = await Replay(0, 0.123, 0.123, 0.0, 0, 1_000_000.0, requireTrades: false);
		spreadBlocked.AssertEmpty("MaxSpreadPoints=0 must block new entries.");

		var horizon = TimeSpan.FromHours(12);
		var wideRisk = await Replay(0, 100.0, 100.0, 1_000_000.0, 0, 1_000_000.0, 0.0, 0.0, horizon);
		var tightRisk = await Replay(0, 100.0, 100.0, 1_000_000.0, 0, 0.01, 0.0, 0.0, horizon);
		var percentageRisk = await Replay(0, 100.0, 100.0, 1_000_000.0, 1, 0.01, 0.0, 0.0, horizon);

		tightRisk.AssertDiffersFrom(wideRisk, "Changing RiskValue did not affect submitted orders.");
		percentageRisk.AssertDiffersFrom(tightRisk, "Changing MoneyRiskMode did not affect submitted orders.");
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S4006_TenpipsOppositeLastNHourTrend()
	{
		var pythonEconomicsChecked = false;

		async Task<OrderTraceRecorder> ReplayCSharp(decimal priceStep, int decimals, decimal protectionPips, int hoursToCheckTrend = 3, EntrySideRecorder entryRecorder = null)
		{
			var recorder = new OrderTraceRecorder();

			await CSharpTests.RunStrategy<TenPipsOppositeLastNHourTrendStrategy>(CancellationToken, (strategy, _) =>
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

		async Task<OrderTraceRecorder> ReplayPython(decimal priceStep, int decimals, double protectionPips, int hoursToCheckTrend = 3, EntrySideRecorder entryRecorder = null)
		{
			var recorder = new OrderTraceRecorder();

			await RunStrategy(
				"4001-4100/4006_TenPips_Opposite_Last_N_Hour_Trend/PY/ten_pips_opposite_last_n_hour_trend_strategy.py", CancellationToken,
				(strategy, _) =>
				{
					if (!pythonEconomicsChecked)
					{
						dynamic implementation = strategy;
						implementation._closed_trade_profits.Add(-10.0);
						implementation._closed_trade_profits.Add(5.0);
						AreEqual(2.0, (double)implementation._apply_loss_multipliers(1.0));
						implementation._closed_trade_profits.Clear();

						var openedAt = new DateTimeOffset(2024, 3, 1, 7, 0, 0, TimeSpan.Zero);
						dynamic episode = implementation._episode;
						episode.register_entry(100.0, 1.0, Sides.Buy, openedAt);
						object firstPartialProfit = episode.register_exit(90.0, 0.4);
						IsNull(firstPartialProfit, "A partial fill must not create a closed-trade history item.");
						AreEqual(0.6, (double)episode.volume, 1e-9);
						AreEqual(openedAt, (DateTimeOffset)episode.entry_time, "A partial exit must not restart OrderMaxAge.");
						var partialProfit = (double)episode.register_exit(110.0, 0.6);

						episode.register_entry(100.0, 1.0, Sides.Buy, openedAt);
						var singleProfit = (double)episode.register_exit(102.0, 1.0);
						AreEqual(2.0, partialProfit, 1e-9);
						AreEqual(singleProfit, partialProfit, 1e-9, "Equivalent partial and single exits must produce one identical economic result.");
						pythonEconomicsChecked = true;
					}

					strategy.Security.PriceStep = priceStep;
					strategy.Security.Decimals = decimals;
					SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
					SetParam(strategy, "TradingHour", 7);
					SetParam(strategy, "HoursToCheckTrend", hoursToCheckTrend);
					SetParam(strategy, "FixedVolume", 0.1);
					SetParam(strategy, "StopLossPips", protectionPips);
					SetParam(strategy, "TakeProfitPips", protectionPips);
					SetParam(strategy, "TrailingStopPips", 0.0);
					recorder.Attach(strategy);
					entryRecorder?.Attach(strategy);
				},
				replayDuration: TimeSpan.FromDays(1),
				postTradeHorizon: TimeSpan.FromHours(1));

			return recorder;
		}

		var expectedThreeDigit = await ReplayCSharp(0.001m, 3, 50_000m);
		var actualThreeDigit = await ReplayPython(0.001m, 3, 50_000.0);
		actualThreeDigit.AssertSameAs(expectedThreeDigit);

		var expectedDailyEntries = new EntrySideRecorder();
		var actualDailyEntries = new EntrySideRecorder();
		var expectedDailyOrders = await ReplayCSharp(0.001m, 3, 1m, entryRecorder: expectedDailyEntries);
		var actualDailyOrders = await ReplayPython(0.001m, 3, 1.0, entryRecorder: actualDailyEntries);
		actualDailyOrders.AssertSameAs(expectedDailyOrders);
		expectedDailyOrders.AssertFirstOppositeWithin(TimeSpan.FromMinutes(20));
		actualDailyOrders.AssertFirstOppositeWithin(TimeSpan.FromMinutes(20));
		expectedDailyEntries.AssertMaximumEntriesPerDay(1);
		actualDailyEntries.AssertMaximumEntriesPerDay(1);

		var expectedFiveDigit = await ReplayCSharp(0.00001m, 5, 5_000_000m);
		var actualFiveDigit = await ReplayPython(0.00001m, 5, 5_000_000.0);
		actualFiveDigit.AssertSameAs(expectedFiveDigit);

		foreach (var hoursToCheckTrend in new[] { 1, 2 })
		{
			var expected = await ReplayCSharp(0.01m, 2, 5_000_000m, hoursToCheckTrend);
			var actual = await ReplayPython(0.01m, 2, 5_000_000.0, hoursToCheckTrend);
			actual.AssertSameAs(expected);
		}

		var expectedTrailing = new OrderTraceRecorder();
		await CSharpTests.RunStrategy<TenPipsOppositeLastNHourTrendStrategy>(CancellationToken, (strategy, _) =>
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
			expectedTrailing.Attach(strategy);
		}, replayDuration: TimeSpan.FromDays(1), postTradeHorizon: TimeSpan.FromHours(1));

		var actualTrailing = new OrderTraceRecorder();
		await RunStrategy(
			"4001-4100/4006_TenPips_Opposite_Last_N_Hour_Trend/PY/ten_pips_opposite_last_n_hour_trend_strategy.py", CancellationToken,
			(strategy, _) =>
			{
				strategy.Security.PriceStep = 1m;
				strategy.Security.Decimals = 2;
				SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
				SetParam(strategy, "TradingHour", 7);
				SetParam(strategy, "HoursToCheckTrend", 3);
				SetParam(strategy, "FixedVolume", 0.1);
				SetParam(strategy, "StopLossPips", 0.0);
				SetParam(strategy, "TakeProfitPips", 0.0);
				SetParam(strategy, "TrailingStopPips", 100.0);
				actualTrailing.Attach(strategy);
			},
			replayDuration: TimeSpan.FromDays(1),
			postTradeHorizon: TimeSpan.FromHours(1));

		actualTrailing.AssertSameAs(expectedTrailing);
		actualTrailing.AssertFirstOppositeAfter(TimeSpan.FromMinutes(10));
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public Task S2000_HftSpreaderForForts()
		// A full month creates tens of thousands of fills. One natural day still
		// exercises hundreds of entry/exit cycles without turning CI into a load test.
		=> RunStrategy(
			"1901-2000/2000_HFT_Spreader_for_FORTS/PY/hft_spreader_for_forts_strategy.py", CancellationToken,
			replayDuration: TimeSpan.FromDays(1));

	[TestMethod]
	[TestCategory("Shard00")]
	public Task S3064_TwoPerbar()
		// This intentionally trades on nearly every bar. A natural one-day window
		// retains high trade coverage without generating ~16k fills per language.
		=> RunStrategy(
			"3001-3100/3064_Two_PerBar/PY/two_per_bar_strategy.py", CancellationToken,
			replayDuration: TimeSpan.FromDays(1));

	[TestMethod]
	[TestCategory("Shard00")]
	public Task S4048_BurgExtrapolatorForecast()
		=> RunStrategy(
			"4001-4100/4048_Burg_Extrapolator_Forecast/PY/burg_extrapolator_forecast_strategy.py", CancellationToken,
			replayDuration: TimeSpan.FromDays(1));

	[TestMethod]
	[TestCategory("Shard00")]
	public Task S2096_BreakoutBarsTrend()
		// Compact parameters make the signal reachable in the bundled history window.
		=> RunStrategy("2001-2100/2096_Breakout_Bars_Trend/PY/breakout_bars_trend_strategy.py", CancellationToken, (s, _) =>
		{
			s.Volume = 0.001m;
			SetParam(s, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(s, "Negatives", 0);
		});

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S2776_Ch2010Structure()
	{
		const string path = "2701-2800/2776_CH2010_Structure/PY/ch2010_structure_strategy.py";

		Strategy strategy = null;

		await RunStrategy(path, CancellationToken, (s, _) =>
		{
			strategy = s;
			SetParam(s, "DailyCandleType", TimeSpan.FromHours(1).TimeFrame());
			SetParam(s, "IntradayCandleType", TimeSpan.FromMinutes(5).TimeFrame());
		});

		// README.md gives the example one slot per currency pair plus the limits that bound a
		// single entry and the exposure summed over every traded pair. The C# half exposes all
		// of them, so the same setup has to be reachable here.
		string[] declared =
		[
			"UsdChfSecurity",
			"GbpUsdSecurity",
			"AudUsdSecurity",
			"UsdJpySecurity",
			"EurGbpSecurity",
			"MinTradeVolume",
			"MaxTradeVolume",
			"MaxAggregateVolume",
		];

		foreach (var name in declared)
			IsTrue(strategy.Parameters.TryGetValue(name, out _), $"Python strategy has no '{name}' parameter, so the multi-instrument workflow and its volume limits cannot be configured.");

		// MaxAggregateVolume caps the exposure summed over every traded pair. Set to a single
		// volume step it has to shrink the entry below the nominal TradeVolume of 1.
		var capped = new OrderTraceRecorder();

		await RunStrategy(path, CancellationToken, (s, _) =>
		{
			SetParam(s, "UsdChfSecurity", s.Security);
			SetParam(s, "DailyCandleType", TimeSpan.FromHours(1).TimeFrame());
			SetParam(s, "IntradayCandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(s, "TradeVolume", 1.0);
			SetParam(s, "MinTradeVolume", 0.001);
			SetParam(s, "MaxTradeVolume", 5.0);
			SetParam(s, "MaxAggregateVolume", 0.001);
			capped.Attach(s);
		});

		capped.AssertFirstVolume(0.001m);

		// The session date is stored with the daily levels so the intraday side can confirm it
		// trades the same session. A one-day frame finishes only once the next date has begun,
		// so every intraday candle that follows belongs to another session and those levels
		// must not be traded at all.
		var staleSession = new OrderTraceRecorder();

		await RunStrategy(path, CancellationToken, (s, _) =>
		{
			SetParam(s, "DailyCandleType", TimeSpan.FromDays(1).TimeFrame());
			SetParam(s, "IntradayCandleType", TimeSpan.FromMinutes(5).TimeFrame());
			staleSession.Attach(s);
		}, requireTrades: false);

		staleSession.AssertEmpty("Intraday candles were traded against daily levels captured on an earlier date.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public Task S0365_DispersionTrading()
		=> RunStrategy("0301-0400/0365_Dispersion_Trading/PY/dispersion_trading_strategy.py", CancellationToken, (s, sec2) => SetParam(s, "Constituents", new[] { sec2 }));

	[TestMethod]
	[TestCategory("Shard07")]
	public Task S2679_MulticurrencyOverlayHedge()
		=> RunStrategy("2601-2700/2679_Multicurrency_Overlay_Hedge/PY/multicurrency_overlay_hedge_strategy.py", CancellationToken, (s, sec2) =>
		{
			SetParam(s, "Universe", new[] { s.Security, sec2 });
			SetParam(s, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(s, "CorrelationThreshold", 0.01);
			SetParam(s, "CorrelationLookback", 50);
			SetParam(s, "RangeLength", 20);
			SetParam(s, "AtrLookback", 20);
			SetParam(s, "MaxSpread", 100000.0);
			SetParam(s, "OverlayThreshold", 0.001);
			SetParam(s, "RecalculationHour", 0);
		});

	[TestMethod]
	[TestCategory("Shard01")]
	public Task S2705_Spreader2()
		=> RunStrategy("2701-2800/2705_Spreader_2/PY/spreader2_strategy.py", CancellationToken, (s, sec2) =>
		{
			SetParam(s, "SecondSecurity", sec2);
			SetParam(s, "DayBars", 10);
			SetParam(s, "ShiftLength", 3);
			SetParam(s, "TargetProfit", 1.0);
		});

	/// <summary>
	/// The example is accepted on Beta = 10000, which is not the ratio it documents. Beta = 1 is the
	/// declared default, and it is also the value that exposes the defect: both candle subscriptions
	/// resolve to the same instrument, so at a hedge ratio of one the residual is identically zero,
	/// no z-score ever leaves the band and nothing is ever submitted. A large Beta hides that by
	/// making the residual large again.
	/// </summary>
	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S0222_CointegrationPairs()
	{
		var recorder = new PairedOrderRecorder();
		Security primary = null;
		Security hedge = null;

		await RunStrategy(
			"0201-0300/0222_Cointegration_Pairs/PY/cointegration_pairs_strategy.py", CancellationToken,
			(strategy, second) =>
			{
				SetParam(strategy, "Asset2", second);
				SetParam(strategy, "Beta", 1.0);
				primary = strategy.Security;
				hedge = second;
				recorder.Attach(strategy);
			});

		recorder.AssertTradesBoth(primary, hedge);
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public Task S0230_DeltaNeutralArbitrage()
		=> RunStrategy("0201-0300/0230_Delta_Neutral_Arbitrage/PY/delta_neutral_arbitrage_strategy.py", CancellationToken, (s, sec2) => { SetParam(s, "Asset2Security", sec2); SetParam(s, "Asset2Portfolio", s.Portfolio); });

	/// <summary>
	/// The hedge is the example: both legs open in the same direction and are closed together when
	/// their combined open profit reaches the money target. The C# half implements that; this half
	/// has no hedge instrument at all, so it is accepted as a single-instrument strategy.
	/// </summary>
	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S2798_ImproveMaRsiHedge()
	{
		const string path = "2701-2800/2798_Improve_MA_RSI_Hedge/PY/improve_ma_rsi_hedge_strategy.py";

		var recorder = new PairedOrderRecorder();
		Strategy strategy = null;
		Security primary = null;
		Security hedge = null;

		await RunStrategy(path, CancellationToken, (s, second) =>
		{
			strategy = s;
			primary = s.Security;
			hedge = second;
			recorder.Attach(s);
		});

		IsTrue(
			strategy.Parameters.TryGetValue("HedgeSecurity", out _),
			"README makes the hedge instrument a required input, so it has to be a parameter of this half too.");

		recorder.AssertTradesBoth(primary, hedge);
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public Task S0333_KeltnerSeasonalFilter()
		// Compact periods make the signal reachable in the bundled history window.
		=> RunStrategy("0301-0400/0333_Keltner_Seasonal_Filter/PY/keltner_seasonal_strategy.py", CancellationToken, (s, _) =>
		{
			SetParam(s, "EmaPeriod", 2);
			SetParam(s, "AtrPeriod", 2);
			SetParam(s, "AtrMultiplier", 0.01);
			SetParam(s, "SeasonalThreshold", 0.0);
			SetParam(s, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
		});

	[TestMethod]
	[TestCategory("Shard01")]
	public Task S1153_Pairs()
		=> RunStrategy("1101-1200/1153_Pairs/PY/pairs_strategy.py", CancellationToken, (s, sec2) => SetParam(s, "ReferenceSecurity", sec2));

	[TestMethod]
	[TestCategory("Shard01")]
	public Task S0217_PairsTrading()
		=> RunStrategy("0201-0300/0217_Pairs_Trading/PY/pairs_trading_strategy.py", CancellationToken, (s, sec2) => SetParam(s, "SecondSecurity", sec2));

	[TestMethod]
	[TestCategory("Shard06")]
	public Task S0526_SpotFuturesArbitrage()
		=> RunStrategy("0501-0600/0526_Spot_Futures_Arbitrage/PY/spot_futures_arbitrage_strategy.py", CancellationToken, (s, sec2) => { SetParam(s, "Spot", s.Security); SetParam(s, "Future", sec2); });

	[TestMethod]
	[TestCategory("Shard03")]
	public Task S0219_StatisticalArbitrage()
		=> RunStrategy("0201-0300/0219_Statistical_Arbitrage/PY/statistical_arbitrage_strategy.py", CancellationToken, (s, sec2) => SetParam(s, "SecondSecurity", sec2));

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

		await RunStrategy(
			"0301-0400/0320_MACD_Hidden_Markov_Model/PY/macd_hidden_markov_model_strategy.py", CancellationToken,
			(strategy, _) =>
			{
				IsTrue(strategy.Parameters.TryGetValue("CandleType", out var candleType), "Python strategy has no 'CandleType' parameter.");
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

		async Task<OrderTraceRecorder> Replay(int hmmHistoryLength)
		{
			var recorder = new OrderTraceRecorder();

			await RunStrategy(
				"0301-0400/0320_MACD_Hidden_Markov_Model/PY/macd_hidden_markov_model_strategy.py", CancellationToken,
				(strategy, _) =>
				{
					SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
					SetParam(strategy, "SignalCooldownBars", 1);
					SetParam(strategy, "HmmHistoryLength", hmmHistoryLength);
					recorder.Attach(strategy);
				},
				replayDuration: TimeSpan.FromDays(3),
				requireTrades: false);

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
		async Task<OrderTraceRecorder> Replay(int atrPeriod, double stopLossAtr)
		{
			var recorder = new OrderTraceRecorder();

			await RunStrategy(
				"0301-0400/0343_Keltner_Reinforcement_Learning_Signal/PY/keltner_with_rl_signal_strategy.py", CancellationToken,
				(strategy, _) =>
				{
					SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
					SetParam(strategy, "EmaPeriod", 5);
					SetParam(strategy, "AtrMultiplier", 0.2);
					SetParam(strategy, "CooldownBars", 1);
					SetParam(strategy, "AtrPeriod", atrPeriod);
					SetParam(strategy, "StopLossAtr", stopLossAtr);
					recorder.Attach(strategy);
				},
				replayDuration: TimeSpan.FromDays(7));

			return recorder;
		}

		// The defaults, run the way the generated row ran them.
		await RunStrategy("0301-0400/0343_Keltner_Reinforcement_Learning_Signal/PY/keltner_with_rl_signal_strategy.py", CancellationToken);

		var baseline = await Replay(atrPeriod: 14, stopLossAtr: 5.0);
		var tightStop = await Replay(atrPeriod: 14, stopLossAtr: 0.01);
		var shortAtr = await Replay(atrPeriod: 2, stopLossAtr: 5.0);

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
		await RunStrategy("0401-0500/0410_WTIBrent_Spread/PY/wti_brent_spread_strategy.py", CancellationToken);

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
		const string path = "0401-0500/0425_Grid_Bot/PY/grid_bot_strategy.py";

		static decimal? Declared(Strategy strategy, string name)
			=> strategy.Parameters.TryGetValue(name, out var param) ? param.Value.To<decimal>() : null;

		// The inputs of a dynamic grid. A predefined range does not read them, so moving them must
		// leave every line - and therefore every order - where the baseline run put it.
		static void Retune(Strategy strategy)
		{
			if (strategy.Parameters.TryGetValue("MALength", out var maLength))
				maLength.Value = 20;

			if (strategy.Parameters.TryGetValue("ATRLength", out var atrLength))
				atrLength.Value = 7;

			if (strategy.Parameters.TryGetValue("GridMultiplier", out var gridMultiplier))
				gridMultiplier.Value = 0.25;
		}

		var baseline = new OrderTraceRecorder();
		var retuned = new OrderTraceRecorder();
		decimal? upperLimit = null;
		decimal? lowerLimit = null;
		decimal? gridCount = null;

		await RunStrategy(path, CancellationToken, (strategy, _) =>
		{
			upperLimit = Declared(strategy, "UpperLimit");
			lowerLimit = Declared(strategy, "LowerLimit");
			gridCount = Declared(strategy, "GridCount");
			baseline.Attach(strategy);
		});

		// The comparison run need not trade on its own: a fixed grid repeats the baseline orders,
		// and an empty trace would itself mean the lines moved with the retuned indicators.
		await RunStrategy(path, CancellationToken, (strategy, _) =>
		{
			Retune(strategy);
			retuned.Attach(strategy);
		}, requireTrades: false);

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
		const string path = "0301-0400/0362_Crypto_Rebalancing_Premium/PY/crypto_rebalancing_premium_strategy.py";

		var recorder = new PairedOrderRecorder();
		Security primary = null;
		Security secondary = null;

		await RunStrategy(path, CancellationToken, (strategy, second) =>
		{
			SetParam(strategy, "SecondarySecurityId", second.Id);
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
		await RunStrategy("0401-0500/0402_Synthetic_Lending_Rates/PY/synthetic_lending_rates_strategy.py", CancellationToken);

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
		const string path = "0401-0500/0498_Advanced_Adaptive_Grid/PY/advanced_adaptive_grid_strategy.py";

		string[] parameterIds = null;

		await RunStrategy(path, CancellationToken, (strategy, _) =>
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
	/// Python half of 0601 must expose the same session model and obey its entry window.
	/// </summary>
	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S0806_FootprintBehavior()
	{
		const string path = "0801-0900/0806_Footprint/PY/footprint_strategy.py";
		var recorder = new OrderTraceRecorder();

		await RunStrategy(path, CancellationToken, (strategy, _) => recorder.Attach(strategy));

		recorder.AssertFirstSide(Sides.Buy);
	}

	[TestMethod]
	[TestCategory("Shard03")]
	public async Task S0703_QuantumSentimentFluxBehavior()
	{
		const string path = "0701-0800/0703_Quantum_Sentiment_Flux_Beginners/PY/quantum_sentiment_flux_beginners_strategy.py";
		var recorder = new OrderTraceRecorder();

		await RunStrategy(path, CancellationToken, (strategy, _) => recorder.Attach(strategy));

		recorder.AssertFirstVolume(1m);
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S2208_HedgeAverageProtectionStartsAfterEntryBar()
	{
		const string path = "2201-2300/2208_Hedge_Average/PY/hedge_average_strategy.py";
		var recorder = new OrderTraceRecorder();

		await RunStrategy(path, CancellationToken, (strategy, _) =>
		{
			SetParam(strategy, "Period1", 2);
			SetParam(strategy, "Period2", 3);
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "StopLoss", 0.01);
			SetParam(strategy, "TakeProfit", 0.01);
			recorder.Attach(strategy);
		}, replayDuration: TimeSpan.FromDays(2));

		recorder.AssertFirstOppositeAfter(TimeSpan.FromTicks(1));
		recorder.AssertFirstOppositeWithin(TimeSpan.FromMinutes(10));
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S3104_MaMacdPositionAveragingProtection()
	{
		const string path = "3101-3200/3104_MA_MACD_Position_Averaging/PY/ma_macd_position_averaging_strategy.py";
		var recorder = new OrderTraceRecorder();

		await RunStrategy(path, CancellationToken, (strategy, _) =>
		{
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "MaPeriod", 3);
			SetParam(strategy, "MacdFastPeriod", 2);
			SetParam(strategy, "MacdSlowPeriod", 4);
			SetParam(strategy, "MacdSignalPeriod", 2);
			SetParam(strategy, "IndentPips", 0);
			SetParam(strategy, "MacdRatio", 0.0);
			SetParam(strategy, "StopLossPips", 1);
			SetParam(strategy, "TakeProfitPips", 1);
			recorder.Attach(strategy);
		}, replayDuration: TimeSpan.FromDays(2));

		recorder.AssertFirstOppositeAfter(TimeSpan.FromTicks(1));
		recorder.AssertFirstOppositeWithin(TimeSpan.FromMinutes(10));
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S3206_RiskRewardRatioProtection()
	{
		const string path = "3201-3300/3206_Risk_Reward_Ratio/PY/risk_reward_ratio_strategy.py";
		var recorder = new OrderTraceRecorder();

		await RunStrategy(path, CancellationToken, (strategy, _) =>
		{
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "FastMaPeriod", 2);
			SetParam(strategy, "SlowMaPeriod", 3);
			SetParam(strategy, "MomentumThreshold", 0.0);
			SetParam(strategy, "StopLossPips", 1);
			SetParam(strategy, "RewardRatio", 1.0);
			SetParam(strategy, "EnableTrailing", false);
			SetParam(strategy, "EnableBreakEven", false);
			recorder.Attach(strategy);
		}, replayDuration: TimeSpan.FromDays(2));

		recorder.AssertFirstOppositeAfter(TimeSpan.FromTicks(1));
		recorder.AssertFirstOppositeWithin(TimeSpan.FromMinutes(10));
	}

	[TestMethod]
	[TestCategory("Shard01")]
	public async Task S3801_OrderStabilizationDoesNotLookAhead()
	{
		const string path = "3801-3900/3801_OrderStabilization/PY/order_stabilization_strategy.py";
		var recorder = new OrderTraceRecorder();

		await RunStrategy(path, CancellationToken, (strategy, _) =>
		{
			SetParam(strategy, "OrderDistancePoints", 1.0);
			recorder.Attach(strategy);
		});

		recorder.AssertFirstOrderAfterStart(TimeSpan.FromMinutes(5));
		recorder.AssertFirstOppositeAfter(TimeSpan.FromTicks(1));
	}

	[TestMethod]
	[TestCategory("Shard01")]
	public async Task S1801_PerceptronStopEndsCurrentBar()
	{
		const string path = "1801-1900/1801_Artificial_Intelligence_Perceptron/PY/artificial_intelligence_perceptron_strategy.py";
		var recorder = new OrderTraceRecorder();

		await RunStrategy(path, CancellationToken, (strategy, _) =>
		{
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "StopLoss", 1.0);
			recorder.Attach(strategy);
		});

		recorder.AssertAtMostOneOrderPerTimestamp();
	}

	[TestMethod]
	[TestCategory("Shard03")]
	public async Task S2907_CcfpProducesTwoLegNonUsdSignal()
	{
		const string path = "2901-3000/2907_CCFp_Currency_Strength/PY/ccfp_currency_strength_strategy.py";
		var recorder = new OrderTraceRecorder();

		await RunStrategy(path, CancellationToken, (strategy, second) =>
		{
			var firstId = strategy.Security.Id;
			var secondId = second.Id;
			foreach (var name in new[] { "EURUSD", "AUDUSD", "USDCAD", "USDJPY" })
				SetParam(strategy, name, firstId);
			foreach (var name in new[] { "GBPUSD", "NZDUSD", "USDCHF" })
				SetParam(strategy, name, secondId);
			SetParam(strategy, "FastMa", 2);
			SetParam(strategy, "SlowMa", 3);
			SetParam(strategy, "StrengthStep", 0.000001);
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			recorder.Attach(strategy);
		});

		recorder.AssertContainsTwoLegSignal("(TOPDOWN)");
	}

}

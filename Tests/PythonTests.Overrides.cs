namespace StockSharp.Tests;

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
			throw new System.InvalidOperationException($"Parameter '{name}' not found. Available: {string.Join(", ", System.Linq.Enumerable.Select(s.Parameters.CachedKeys, k => k.ToString()))}");
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
			replayDuration: System.TimeSpan.FromDays(7));

		await RunStrategy(
			"0201-0300/0201_VWAP_Williams_R/PY/vwap_williams_r_strategy.py", CancellationToken,
			(strategy, _) =>
			{
				SetParam(strategy, "WilliamsRPeriod", 5);
				SetParam(strategy, "CooldownBars", 1);
				SetParam(strategy, "StopLossPercent", 5.0);
				wideStop.Attach(strategy);
			},
			replayDuration: System.TimeSpan.FromDays(7));

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
				Assert.AreEqual(2, (int)implementation._resolve_protective_exit_action(0.6, OrderStates.Done),
					"A terminal 0.4 partial fill must preserve protection and allow retrying the residual 0.6 position.");
				Assert.AreEqual(1, (int)implementation._resolve_protective_exit_action(0.6, OrderStates.Active));
				Assert.AreEqual(0, (int)implementation._resolve_protective_exit_action(0.0, OrderStates.Done));
				SetParam(strategy, "FastLength", 3);
				SetParam(strategy, "SlowLength", 8);
				SetParam(strategy, "StopLossPercent", 0.01);
				SetParam(strategy, "TakeProfitPercent", 0.01);
				SetParam(strategy, "CooldownBars", 1);
			},
			replayDuration: System.TimeSpan.FromDays(7));
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
				replayDuration: System.TimeSpan.FromDays(7));

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
						Assert.IsTrue(strategy.Parameters.TryGetValue("TakeProfitPercent", out var parameter));
						Assert.AreEqual(typeof(double), parameter.Value.GetType(), "TakeProfitPercent must be numeric.");
						Assert.AreEqual(3.0, (double)parameter.Value, "Unexpected TakeProfitPercent default.");
					}

					SetParam(strategy, "FastLength", fastLength);
					SetParam(strategy, "SlowLength", slowLength);
					SetParam(strategy, "TakeProfitPercent", takeProfit);
					SetParam(strategy, "StopLossPercent", stopLoss);
					recorder.Attach(strategy);
				},
				replayDuration: System.TimeSpan.FromDays(7));

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
					SetParam(strategy, "CandleType", System.TimeSpan.FromMinutes(5).TimeFrame());
					SetParam(strategy, "SmaPeriod", 2);
					SetParam(strategy, "SleepBars", 1);
					SetParam(strategy, "MinStopLevel", 0.0001);
					SetParam(strategy, "TrailingStep", 0.0001);
					SetParam(strategy, "RandomSeed", randomSeed);
					recorder.Attach(strategy);
				},
				replayDuration: System.TimeSpan.FromDays(1),
				postTradeHorizon: System.TimeSpan.FromHours(1));

			return recorder;
		}

		var expected = new EntrySideRecorder();
		await CSharpTests.RunStrategy<RandomTrailingStopStrategy>(CancellationToken, (strategy, _) =>
		{
			strategy.CandleType = System.TimeSpan.FromMinutes(5).TimeFrame();
			strategy.SmaPeriod = 2;
			strategy.SleepBars = 1;
			strategy.MinStopLevel = 0.0001m;
			strategy.TrailingStep = 0.0001m;
			strategy.RandomSeed = 42;
			expected.Attach(strategy);
		}, replayDuration: System.TimeSpan.FromDays(1), postTradeHorizon: System.TimeSpan.FromHours(1));

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
			strategy.CandleType = System.TimeSpan.FromMinutes(5).TimeFrame();
			strategy.Length = 3;
			strategy.TriggerShift = 1;
			strategy.StopLossPct = 100m;
			strategy.TakeProfitPct = 100m;
			expected.Attach(strategy);
		}, replayDuration: System.TimeSpan.FromDays(1));

		var actual = new OrderTraceRecorder();
		await RunStrategy(
			"2101-2200/2101_Linear_Regression_Slope_V1/PY/linear_regression_slope_v1_strategy.py", CancellationToken,
			(strategy, _) =>
			{
				SetParam(strategy, "CandleType", System.TimeSpan.FromMinutes(5).TimeFrame());
				SetParam(strategy, "Length", 3);
				SetParam(strategy, "TriggerShift", 1);
				SetParam(strategy, "StopLossPct", 100.0);
				SetParam(strategy, "TakeProfitPct", 100.0);
				actual.Attach(strategy);
			},
			replayDuration: System.TimeSpan.FromDays(1));

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
				SetParam(strategy, "CandleType", System.TimeSpan.FromMinutes(5).TimeFrame());
				SetParam(strategy, "ProfitThreshold", -1_000_000.0);
				SetParam(strategy, "MaxPositions", 3);
				SetParam(strategy, "StopLossPoints", 100.0);
				SetParam(strategy, "TakeProfitPoints", 100.0);
				recorder.Attach(strategy);
			},
			replayDuration: System.TimeSpan.FromDays(2));

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
				SetParam(strategy, "CandleType", System.TimeSpan.FromHours(4).TimeFrame());
				SetParam(strategy, "FirstSessionStartHour", 20);
				SetParam(strategy, "FirstSessionStopHour", 21);
				SetParam(strategy, "StepPoints", 1.0);
				SetParam(strategy, "TakeProfitPoints", 1_000_000.0);
				recorder.Attach(strategy);
			},
			replayDuration: System.TimeSpan.FromDays(2));

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
			replayDuration: System.TimeSpan.FromDays(2));

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
		}, replayDuration: System.TimeSpan.FromDays(3), requireTrades: false);

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
			replayDuration: System.TimeSpan.FromDays(3),
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
						Assert.AreEqual(2, (int)implementation._resolve_protective_exit_action(0.6, OrderStates.Done),
							"A terminal 0.4 partial fill must preserve protection and allow retrying the residual 0.6 position.");
						Assert.AreEqual(1, (int)implementation._resolve_protective_exit_action(0.6, OrderStates.Active));
						Assert.AreEqual(0, (int)implementation._resolve_protective_exit_action(0.0, OrderStates.Done));
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
				replayDuration: System.TimeSpan.FromHours(4));

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
			replayDuration: System.TimeSpan.FromDays(1),
			postTradeHorizon: System.TimeSpan.FromMinutes(20));

		breakEven.AssertFirstOppositeAfter(System.TimeSpan.FromMinutes(10));
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S3104_MaMacdPositionAveraging()
	{
		var recorder = new OrderTraceRecorder();

		await RunStrategy(
			"3101-3200/3104_MA_MACD_Position_Averaging/PY/ma_macd_position_averaging_strategy.py", CancellationToken,
			(strategy, _) =>
			{
				SetParam(strategy, "FastPeriod", 2);
				SetParam(strategy, "SlowPeriod", 3);
				SetParam(strategy, "StopLossPoints", 1);
				SetParam(strategy, "TakeProfitPoints", 1);
				recorder.Attach(strategy);
			},
			replayDuration: System.TimeSpan.FromDays(2));

		recorder.AssertFirstOppositeWithin(System.TimeSpan.FromMinutes(10));
	}

	[TestMethod]
	[TestCategory("Shard06")]
	public async Task S3206_RiskRewardRatio()
	{
		var recorder = new OrderTraceRecorder();

		await RunStrategy(
			"3201-3300/3206_Risk_Reward_Ratio/PY/risk_reward_ratio_strategy.py", CancellationToken,
			(strategy, _) =>
			{
				SetParam(strategy, "FastPeriod", 2);
				SetParam(strategy, "SlowPeriod", 3);
				SetParam(strategy, "StopLossPoints", 1);
				SetParam(strategy, "TakeProfitPoints", 1);
				recorder.Attach(strategy);
			},
			replayDuration: System.TimeSpan.FromDays(2));

		recorder.AssertFirstOppositeWithin(System.TimeSpan.FromMinutes(10));
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public async Task S3301_CryptoAnalysis()
	{
		var recorder = new OrderTraceRecorder();

		await RunStrategy(
			"3301-3400/3301_Crypto_Analysis/PY/crypto_analysis_strategy.py", CancellationToken,
			(strategy, _) =>
			{
				SetParam(strategy, "FastPeriod", 2);
				SetParam(strategy, "SlowPeriod", 3);
				SetParam(strategy, "StopLossPoints", 1);
				SetParam(strategy, "TakeProfitPoints", 1);
				recorder.Attach(strategy);
			},
			replayDuration: System.TimeSpan.FromDays(2));

		recorder.AssertFirstOppositeWithin(System.TimeSpan.FromMinutes(10));
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
			System.TimeSpan? postTradeHorizon = null,
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
						(string name, System.Type type)[] expected =
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
							Assert.IsTrue(strategy.Parameters.TryGetValue(name, out var parameter), $"Python strategy has no '{name}' parameter.");
							Assert.AreEqual(type, parameter.Value.GetType(), $"Python parameter '{name}' has an unexpected type.");
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
					SetParam(strategy, "CandleType", System.TimeSpan.FromMinutes(5).TimeFrame());
					recorder.Attach(strategy);
				},
				replayDuration: System.TimeSpan.FromDays(2),
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

		var horizon = System.TimeSpan.FromHours(12);
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
						Assert.AreEqual(2.0, (double)implementation._apply_loss_multipliers(1.0));
						implementation._closed_trade_profits.Clear();

						var openedAt = new System.DateTimeOffset(2024, 3, 1, 7, 0, 0, System.TimeSpan.Zero);
						dynamic episode = implementation._episode;
						episode.register_entry(100.0, 1.0, Sides.Buy, openedAt);
						object firstPartialProfit = episode.register_exit(90.0, 0.4);
						Assert.IsNull(firstPartialProfit, "A partial fill must not create a closed-trade history item.");
						Assert.AreEqual(0.6, (double)episode.volume, 1e-9);
						Assert.AreEqual(openedAt, (System.DateTimeOffset)episode.entry_time, "A partial exit must not restart OrderMaxAge.");
						var partialProfit = (double)episode.register_exit(110.0, 0.6);

						episode.register_entry(100.0, 1.0, Sides.Buy, openedAt);
						var singleProfit = (double)episode.register_exit(102.0, 1.0);
						Assert.AreEqual(2.0, partialProfit, 1e-9);
						Assert.AreEqual(singleProfit, partialProfit, 1e-9, "Equivalent partial and single exits must produce one identical economic result.");
						pythonEconomicsChecked = true;
					}

					strategy.Security.PriceStep = priceStep;
					strategy.Security.Decimals = decimals;
					SetParam(strategy, "CandleType", System.TimeSpan.FromMinutes(5).TimeFrame());
					SetParam(strategy, "TradingHour", 7);
					SetParam(strategy, "HoursToCheckTrend", hoursToCheckTrend);
					SetParam(strategy, "FixedVolume", 0.1);
					SetParam(strategy, "StopLossPips", protectionPips);
					SetParam(strategy, "TakeProfitPips", protectionPips);
					SetParam(strategy, "TrailingStopPips", 0.0);
					recorder.Attach(strategy);
					entryRecorder?.Attach(strategy);
				},
				replayDuration: System.TimeSpan.FromDays(1),
				postTradeHorizon: System.TimeSpan.FromHours(1));

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
		expectedDailyOrders.AssertFirstOppositeWithin(System.TimeSpan.FromMinutes(20));
		actualDailyOrders.AssertFirstOppositeWithin(System.TimeSpan.FromMinutes(20));
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
			strategy.CandleType = System.TimeSpan.FromMinutes(5).TimeFrame();
			strategy.TradingHour = 7;
			strategy.HoursToCheckTrend = 3;
			strategy.FixedVolume = 0.1m;
			strategy.StopLossPips = 0m;
			strategy.TakeProfitPips = 0m;
			strategy.TrailingStopPips = 100m;
			expectedTrailing.Attach(strategy);
		}, replayDuration: System.TimeSpan.FromDays(1), postTradeHorizon: System.TimeSpan.FromHours(1));

		var actualTrailing = new OrderTraceRecorder();
		await RunStrategy(
			"4001-4100/4006_TenPips_Opposite_Last_N_Hour_Trend/PY/ten_pips_opposite_last_n_hour_trend_strategy.py", CancellationToken,
			(strategy, _) =>
			{
				strategy.Security.PriceStep = 1m;
				strategy.Security.Decimals = 2;
				SetParam(strategy, "CandleType", System.TimeSpan.FromMinutes(5).TimeFrame());
				SetParam(strategy, "TradingHour", 7);
				SetParam(strategy, "HoursToCheckTrend", 3);
				SetParam(strategy, "FixedVolume", 0.1);
				SetParam(strategy, "StopLossPips", 0.0);
				SetParam(strategy, "TakeProfitPips", 0.0);
				SetParam(strategy, "TrailingStopPips", 100.0);
				actualTrailing.Attach(strategy);
			},
			replayDuration: System.TimeSpan.FromDays(1),
			postTradeHorizon: System.TimeSpan.FromHours(1));

		actualTrailing.AssertSameAs(expectedTrailing);
		actualTrailing.AssertFirstOppositeAfter(System.TimeSpan.FromMinutes(10));
	}

	[TestMethod]
	[TestCategory("Shard00")]
	public Task S2000_HftSpreaderForForts()
		// A full month creates tens of thousands of fills. One natural day still
		// exercises hundreds of entry/exit cycles without turning CI into a load test.
		=> RunStrategy(
			"1901-2000/2000_HFT_Spreader_for_FORTS/PY/hft_spreader_for_forts_strategy.py", CancellationToken,
			replayDuration: System.TimeSpan.FromDays(1));

	[TestMethod]
	[TestCategory("Shard00")]
	public Task S3064_TwoPerbar()
		// This intentionally trades on nearly every bar. A natural one-day window
		// retains high trade coverage without generating ~16k fills per language.
		=> RunStrategy(
			"3001-3100/3064_Two_PerBar/PY/two_per_bar_strategy.py", CancellationToken,
			replayDuration: System.TimeSpan.FromDays(1));

	[TestMethod]
	[TestCategory("Shard00")]
	public Task S4048_BurgExtrapolatorForecast()
		=> RunStrategy(
			"4001-4100/4048_Burg_Extrapolator_Forecast/PY/burg_extrapolator_forecast_strategy.py", CancellationToken,
			replayDuration: System.TimeSpan.FromDays(1));

	[TestMethod]
	[TestCategory("Shard00")]
	public Task S2096_BreakoutBarsTrend()
		// Compact parameters make the signal reachable in the bundled history window.
		=> RunStrategy("2001-2100/2096_Breakout_Bars_Trend/PY/breakout_bars_trend_strategy.py", CancellationToken, (s, _) =>
		{
			s.Volume = 0.001m;
			SetParam(s, "CandleType", System.TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(s, "Negatives", 0);
		});

	[TestMethod]
	[TestCategory("Shard00")]
	public Task S2776_Ch2010Structure()
		=> RunStrategy("2701-2800/2776_CH2010_Structure/PY/ch2010_structure_strategy.py", CancellationToken, (s, _) =>
		{
			SetParam(s, "DailyCandleType", System.TimeSpan.FromHours(1).TimeFrame());
			SetParam(s, "IntradayCandleType", System.TimeSpan.FromMinutes(5).TimeFrame());
		});

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
			SetParam(s, "CandleType", System.TimeSpan.FromMinutes(5).TimeFrame());
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

	[TestMethod]
	[TestCategory("Shard06")]
	public Task S0222_CointegrationPairs()
		=> RunStrategy("0201-0300/0222_Cointegration_Pairs/PY/cointegration_pairs_strategy.py", CancellationToken, (s, sec2) => { SetParam(s, "Asset2", sec2); SetParam(s, "Beta", 10000.0); });

	[TestMethod]
	[TestCategory("Shard06")]
	public Task S0230_DeltaNeutralArbitrage()
		=> RunStrategy("0201-0300/0230_Delta_Neutral_Arbitrage/PY/delta_neutral_arbitrage_strategy.py", CancellationToken, (s, sec2) => { SetParam(s, "Asset2Security", sec2); SetParam(s, "Asset2Portfolio", s.Portfolio); });

	[TestMethod]
	[TestCategory("Shard06")]
	public Task S2798_ImproveMaRsiHedge()
		=> RunStrategy("2701-2800/2798_Improve_MA_RSI_Hedge/PY/improve_ma_rsi_hedge_strategy.py", CancellationToken);

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
			SetParam(s, "CandleType", System.TimeSpan.FromMinutes(5).TimeFrame());
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
}

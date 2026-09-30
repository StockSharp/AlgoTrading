namespace StockSharp.Tests;

using System;
using System.Globalization;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;
using StockSharp.Samples.Strategies;

partial class PythonTests
{
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
	[TestCategory("Shard00")]
	public async Task S4104_PinballMachineRandomDrawContract()
	{
		const string pythonPath = "4101-4200/4104_Pinball_Machine_Random_Draw/PY/pinball_machine_random_draw_strategy.py";
		var forced = new OrderTraceRecorder();

		await RunStrategy(pythonPath, CancellationToken, (strategy, _) =>
		{
			strategy.Security.PriceStep = 0.01m;
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "TradeVolume", 2.0);
			SetParam(strategy, "RandomMaxValue", 0);
			SetParam(strategy, "MinStopLossPoints", 0);
			SetParam(strategy, "MaxStopLossPoints", 0);
			SetParam(strategy, "MinTakeProfitPoints", 0);
			SetParam(strategy, "MaxTakeProfitPoints", 0);
			SetParam(strategy, "RandomSeed", 42);
			forced.Attach(strategy);
		}, replayDuration: TimeSpan.FromDays(1));

		forced.AssertContainsOppositePairAtSameTimestamp(2m, "Pinball entry");

		var protectedRecorder = new OrderTraceRecorder();
		await RunStrategy(pythonPath, CancellationToken, (strategy, _) =>
		{
			strategy.Security.PriceStep = 0.01m;
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "TradeVolume", 1.0);
			SetParam(strategy, "RandomMaxValue", 4);
			SetParam(strategy, "MinStopLossPoints", 1);
			SetParam(strategy, "MaxStopLossPoints", 1);
			SetParam(strategy, "MinTakeProfitPoints", 1);
			SetParam(strategy, "MaxTakeProfitPoints", 1);
			SetParam(strategy, "RandomSeed", 5);
			protectedRecorder.Attach(strategy);
		}, replayDuration: TimeSpan.FromDays(1));

		protectedRecorder.AssertContainsComment("Pinball protection exit");

		var expected = new OrderTraceRecorder();
		await CSharpTests.RunStrategy<PinballMachineRandomDrawStrategy>(CancellationToken, (strategy, _) =>
		{
			strategy.CandleType = TimeSpan.FromMinutes(5).TimeFrame();
			strategy.TradeVolume = 1m;
			strategy.RandomMaxValue = 4;
			strategy.MinStopLossPoints = 0;
			strategy.MaxStopLossPoints = 0;
			strategy.MinTakeProfitPoints = 0;
			strategy.MaxTakeProfitPoints = 0;
			strategy.RandomSeed = 42;
			expected.Attach(strategy);
		}, replayDuration: TimeSpan.FromDays(1));

		var actual = new OrderTraceRecorder();
		await RunStrategy(pythonPath, CancellationToken, (strategy, _) =>
		{
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			SetParam(strategy, "TradeVolume", 1.0);
			SetParam(strategy, "RandomMaxValue", 4);
			SetParam(strategy, "MinStopLossPoints", 0);
			SetParam(strategy, "MaxStopLossPoints", 0);
			SetParam(strategy, "MinTakeProfitPoints", 0);
			SetParam(strategy, "MaxTakeProfitPoints", 0);
			SetParam(strategy, "RandomSeed", 42);
			actual.Attach(strategy);
		}, replayDuration: TimeSpan.FromDays(1));

		actual.AssertSameAs(expected);
	}

	[TestMethod]
	[TestCategory("Shard07")]
	public async Task S0343_SeededCSharpPythonDecisionParity()
	{
		const string key = "0343_Keltner_Reinforcement_Learning_Signal";
		var csharp = new OrderTraceRecorder();
		var python = new OrderTraceRecorder();
		await CSharpTests.RunStrategy(StrategyInventory.GetFile(key, ".cs"), CancellationToken,
			(strategy, _) => csharp.Attach(strategy), replayDuration: TimeSpan.FromDays(31));
		await Replay(key,
			(strategy, _) => python.Attach(strategy), TimeSpan.FromDays(31));
		python.AssertSameAs(csharp);
	}

	[TestMethod]
	[TestCategory("Shard06")]
	[DataRow("de-DE")]
	[DataRow("ru-RU")]
	public async Task S0062_PythonLevelsIgnoreThreadCulture(string cultureName)
	{
		var previousDefault = CultureInfo.DefaultThreadCurrentCulture;
		var previous = CultureInfo.CurrentCulture;
		var culture = CultureInfo.GetCultureInfo(cultureName);
		CultureInfo.DefaultThreadCurrentCulture = culture;
		CultureInfo.CurrentCulture = culture;
		try
		{
			var counts = await ReplayStochasticSignals(14, 3, 3, false);
			IsTrue(counts.LongEntries > 0 && counts.ShortEntries > 0 && counts.NeutralExits > 0);
		}
		finally
		{
			CultureInfo.DefaultThreadCurrentCulture = previousDefault;
			CultureInfo.CurrentCulture = previous;
		}
	}
}

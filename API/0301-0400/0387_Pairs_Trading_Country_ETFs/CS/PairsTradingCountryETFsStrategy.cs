using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mean-reversion pairs strategy for country ETFs that trades the primary instrument against a benchmark ETF using the ratio z-score.
/// </summary>
public class PairsTradingCountryETFsStrategy : Strategy
{
	private readonly StrategyParam<string> _security2Id;
	private readonly StrategyParam<int> _windowLength;
	private readonly StrategyParam<decimal> _entryThreshold;
	private readonly StrategyParam<decimal> _exitThreshold;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private Security _benchmark = null!;
	private SimpleMovingAverage _ratioAverage = null!;
	private StandardDeviation _ratioDeviation = null!;
	private decimal _latestPrimaryClose;
	private decimal _latestBenchmarkClose;
	private bool _primaryUpdated;
	private bool _benchmarkUpdated;
	private int _cooldownRemaining;

	/// <summary>
	/// Benchmark ETF identifier.
	/// </summary>
	public string Security2Id
	{
		get => _security2Id.Value;
		set => _security2Id.Value = value;
	}

	/// <summary>
	/// Lookback period used to estimate the ratio mean and deviation.
	/// </summary>
	public int WindowLength
	{
		get => _windowLength.Value;
		set => _windowLength.Value = value;
	}

	/// <summary>
	/// Z-score threshold required to open a paired position.
	/// </summary>
	public decimal EntryThreshold
	{
		get => _entryThreshold.Value;
		set => _entryThreshold.Value = value;
	}

	/// <summary>
	/// Z-score threshold required to close the paired position.
	/// </summary>
	public decimal ExitThreshold
	{
		get => _exitThreshold.Value;
		set => _exitThreshold.Value = value;
	}

	/// <summary>
	/// Closed candles to wait before another position change.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public PairsTradingCountryETFsStrategy()
	{
		_security2Id = Param(nameof(Security2Id), Paths.HistoryDefaultSecurity2)
			.SetDisplay("Benchmark Security Id", "Identifier of the benchmark ETF", "General");

		_windowLength = Param(nameof(WindowLength), 24)
			.SetRange(5, 120)
			.SetDisplay("Window Length", "Lookback period used to estimate the ratio mean and deviation", "Indicators");

		_entryThreshold = Param(nameof(EntryThreshold), 1.4m)
			.SetRange(0.2m, 5m)
			.SetDisplay("Entry Threshold", "Z-score threshold required to open a paired position", "Signals");

		_exitThreshold = Param(nameof(ExitThreshold), 0.35m)
			.SetRange(0m, 2m)
			.SetDisplay("Exit Threshold", "Z-score threshold required to close the paired position", "Signals");

		_cooldownBars = Param(nameof(CooldownBars), 8)
			.SetRange(0, 120)
			.SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "Risk");

		_stopLoss = Param(nameof(StopLoss), 3m)
			.SetRange(0.5m, 10m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, CandleType);

		if (!Security2Id.IsEmpty())
			yield return (new Security { Id = Security2Id }, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_benchmark = null!;
		_ratioAverage = null!;
		_ratioDeviation = null!;
		_latestPrimaryClose = 0m;
		_latestBenchmarkClose = 0m;
		_primaryUpdated = false;
		_benchmarkUpdated = false;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (Security == null)
			throw new InvalidOperationException("Primary security is not specified.");

		if (Security2Id.IsEmpty())
			throw new InvalidOperationException("Benchmark security identifier is not specified.");

		_benchmark = this.LookupById(Security2Id) ?? new Security { Id = Security2Id };
		_ratioAverage = new SimpleMovingAverage { Length = WindowLength };
		_ratioDeviation = new StandardDeviation { Length = WindowLength };

		var primarySubscription = SubscribeCandles(CandleType, security: Security);
		var benchmarkSubscription = SubscribeCandles(CandleType, security: _benchmark);

		primarySubscription
			.Bind(ProcessPrimaryCandle)
			.Start();

		benchmarkSubscription
			.Bind(ProcessBenchmarkCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, primarySubscription);
			DrawCandles(area, benchmarkSubscription);
			DrawOwnTrades(area);
		}

		StartProtection(
			new Unit(2, UnitTypes.Percent),
			new Unit(StopLoss, UnitTypes.Percent));
	}

	private void ProcessPrimaryCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_latestPrimaryClose = candle.ClosePrice;
		_primaryUpdated = true;
		TryProcessPair(candle.OpenTime);
	}

	private void ProcessBenchmarkCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_latestBenchmarkClose = candle.ClosePrice;
		_benchmarkUpdated = true;
		TryProcessPair(candle.OpenTime);
	}

	private void TryProcessPair(DateTime time)
	{
		if (!_primaryUpdated || !_benchmarkUpdated || _latestPrimaryClose <= 0m || _latestBenchmarkClose <= 0m)
			return;

		_primaryUpdated = false;
		_benchmarkUpdated = false;

		var ratio = _latestPrimaryClose / _latestBenchmarkClose;
		var mean = _ratioAverage.Process(ratio, time, true).ToDecimal();
		var deviation = _ratioDeviation.Process(ratio, time, true).ToDecimal();

		if (!_ratioAverage.IsFormed || !_ratioDeviation.IsFormed || deviation <= 0m)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var zScore = (ratio - mean) / deviation;

		if (Math.Abs(zScore) <= ExitThreshold)
		{
			FlattenPair();
			return;
		}

		if (_cooldownRemaining > 0)
			return;

		if (zScore >= EntryThreshold)
		{
			SetPairPosition(-1m);
			_cooldownRemaining = CooldownBars;
		}
		else if (zScore <= -EntryThreshold)
		{
			SetPairPosition(1m);
			_cooldownRemaining = CooldownBars;
		}
	}

	private void FlattenPair()
	{
		var primaryPosition = GetPositionValue(Security, Portfolio) ?? 0m;
		var benchmarkPosition = GetPositionValue(_benchmark, Portfolio) ?? 0m;

		if (primaryPosition > 0m)
			SellMarket(primaryPosition);
		else if (primaryPosition < 0m)
			BuyMarket(Math.Abs(primaryPosition));

		if (benchmarkPosition > 0m)
			RegisterOrder(new Order
			{
				Security = _benchmark,
				Portfolio = Portfolio,
				Side = Sides.Sell,
				Volume = benchmarkPosition,
				Type = OrderTypes.Market,
				Comment = "PairsExit"
			});
		else if (benchmarkPosition < 0m)
			RegisterOrder(new Order
			{
				Security = _benchmark,
				Portfolio = Portfolio,
				Side = Sides.Buy,
				Volume = Math.Abs(benchmarkPosition),
				Type = OrderTypes.Market,
				Comment = "PairsExit"
			});
	}

	private void SetPairPosition(decimal primaryDirection)
	{
		var primaryPosition = GetPositionValue(Security, Portfolio) ?? 0m;
		var benchmarkPosition = GetPositionValue(_benchmark, Portfolio) ?? 0m;

		var targetPrimary = primaryDirection;
		var targetBenchmark = -primaryDirection;

		MoveSecurity(Security, primaryPosition, targetPrimary);
		MoveSecurity(_benchmark, benchmarkPosition, targetBenchmark);
	}

	private void MoveSecurity(Security security, decimal currentPosition, decimal targetPosition)
	{
		var diff = targetPosition - currentPosition;
		if (diff == 0m)
			return;

		RegisterOrder(new Order
		{
			Security = security,
			Portfolio = Portfolio,
			Side = diff > 0m ? Sides.Buy : Sides.Sell,
			Volume = Math.Abs(diff),
			Type = OrderTypes.Market,
			Comment = "PairsETF"
		});
	}
}

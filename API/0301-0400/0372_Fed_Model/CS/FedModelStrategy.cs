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
/// Fed model strategy that trades the primary instrument when its synthetic earnings yield exceeds a synthetic bond yield benchmark.
/// </summary>
public class FedModelStrategy : Strategy
{
	private readonly StrategyParam<string> _security2Id;
	private readonly StrategyParam<int> _yieldLength;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _entryThreshold;
	private readonly StrategyParam<decimal> _exitThreshold;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private Security _benchmark = null!;
	private ExponentialMovingAverage _earningsYield = null!;
	private ExponentialMovingAverage _bondYield = null!;
	private SimpleMovingAverage _gapAverage = null!;
	private StandardDeviation _gapDeviation = null!;
	private decimal _latestPrimaryGap;
	private decimal _latestBenchmarkGap;
	private decimal? _previousZScore;
	private bool _primaryUpdated;
	private bool _benchmarkUpdated;
	private int _cooldownRemaining;

	/// <summary>
	/// Benchmark security identifier.
	/// </summary>
	public string Security2Id
	{
		get => _security2Id.Value;
		set => _security2Id.Value = value;
	}

	/// <summary>
	/// Smoothing length for synthetic yields.
	/// </summary>
	public int YieldLength
	{
		get => _yieldLength.Value;
		set => _yieldLength.Value = value;
	}

	/// <summary>
	/// Lookback period used to normalize the yield gap.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Z-score threshold required to open a position.
	/// </summary>
	public decimal EntryThreshold
	{
		get => _entryThreshold.Value;
		set => _entryThreshold.Value = value;
	}

	/// <summary>
	/// Z-score threshold required to close a position.
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
	/// Type of candles used for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public FedModelStrategy()
	{
		_security2Id = Param(nameof(Security2Id), Paths.HistoryDefaultSecurity2)
			.SetDisplay("Benchmark Security Id", "Identifier of the benchmark security", "General");

		_yieldLength = Param(nameof(YieldLength), 12)
			.SetRange(2, 80)
			.SetDisplay("Yield Length", "Smoothing length for synthetic yields", "Indicators");

		_lookbackPeriod = Param(nameof(LookbackPeriod), 24)
			.SetRange(5, 120)
			.SetDisplay("Lookback Period", "Lookback period used to normalize the yield gap", "Indicators");

		_entryThreshold = Param(nameof(EntryThreshold), 1.1m)
			.SetRange(0.2m, 5m)
			.SetDisplay("Entry Threshold", "Z-score threshold required to open a position", "Signals");

		_exitThreshold = Param(nameof(ExitThreshold), 0.25m)
			.SetRange(0m, 2m)
			.SetDisplay("Exit Threshold", "Z-score threshold required to close a position", "Signals");

		_cooldownBars = Param(nameof(CooldownBars), 8)
			.SetRange(0, 120)
			.SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "Risk");

		_stopLoss = Param(nameof(StopLoss), 2.5m)
			.SetRange(0.5m, 10m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
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
		_earningsYield = null!;
		_bondYield = null!;
		_gapAverage = null!;
		_gapDeviation = null!;
		_latestPrimaryGap = 0m;
		_latestBenchmarkGap = 0m;
		_previousZScore = null;
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
		_earningsYield = new ExponentialMovingAverage { Length = YieldLength };
		_bondYield = new ExponentialMovingAverage { Length = YieldLength };
		_gapAverage = new SimpleMovingAverage { Length = LookbackPeriod };
		_gapDeviation = new StandardDeviation { Length = LookbackPeriod };

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

		_latestPrimaryGap = UpdateYieldGap(_earningsYield, candle);
		_primaryUpdated = true;
		TryProcessGap(candle.OpenTime);
	}

	private void ProcessBenchmarkCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_latestBenchmarkGap = UpdateYieldGap(_bondYield, candle);
		_benchmarkUpdated = true;
		TryProcessGap(candle.OpenTime);
	}

	private decimal UpdateYieldGap(ExponentialMovingAverage average, ICandleMessage candle)
	{
		var yieldProxy = CalculateYieldProxy(candle);
		return average.Process(yieldProxy, candle.OpenTime, true).ToDecimal();
	}

	private decimal CalculateYieldProxy(ICandleMessage candle)
	{
		var priceBase = Math.Max(candle.ClosePrice, 1m);
		var range = Math.Max(candle.HighPrice - candle.LowPrice, Security?.PriceStep ?? 1m);
		var normalizedRange = range / priceBase;
		var closeLocation = (candle.ClosePrice - candle.LowPrice) / range;

		return (1m / priceBase * 100m) + closeLocation - normalizedRange;
	}

	private void TryProcessGap(DateTime time)
	{
		if (!_primaryUpdated || !_benchmarkUpdated)
			return;

		_primaryUpdated = false;
		_benchmarkUpdated = false;

		var gap = _latestPrimaryGap - _latestBenchmarkGap;
		var mean = _gapAverage.Process(gap, time, true).ToDecimal();
		var deviation = _gapDeviation.Process(gap, time, true).ToDecimal();

		if (!_gapAverage.IsFormed || !_gapDeviation.IsFormed || deviation <= 0m)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var zScore = (gap - mean) / deviation;
		var bullishEntry = _previousZScore is decimal previousBullish && previousBullish < EntryThreshold && zScore >= EntryThreshold;
		var bearishEntry = _previousZScore is decimal previousBearish && previousBearish > -EntryThreshold && zScore <= -EntryThreshold;

		if (_cooldownRemaining == 0 && Position == 0)
		{
			if (bullishEntry)
			{
				BuyMarket();
				_cooldownRemaining = CooldownBars;
			}
			else if (bearishEntry)
			{
				SellMarket();
				_cooldownRemaining = CooldownBars;
			}
		}
		else if (Position > 0 && zScore <= ExitThreshold)
		{
			SellMarket(Position);
			_cooldownRemaining = CooldownBars;
		}
		else if (Position < 0 && zScore >= -ExitThreshold)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_previousZScore = zScore;
	}
}

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
/// ESG factor momentum strategy that trades the primary instrument when its ESG-adjusted momentum is stronger or weaker than a benchmark.
/// </summary>
public class EsgFactorMomentumStrategy : Strategy
{
	private readonly StrategyParam<string> _security2Id;
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _entryThreshold;
	private readonly StrategyParam<decimal> _exitThreshold;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private Security _benchmark = null!;
	private RateOfChange _primaryMomentum = null!;
	private RateOfChange _benchmarkMomentum = null!;
	private SimpleMovingAverage _spreadAverage = null!;
	private StandardDeviation _spreadDeviation = null!;
	private decimal _latestPrimaryScore;
	private decimal _latestBenchmarkScore;
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
	/// Momentum lookback period.
	/// </summary>
	public int MomentumLength
	{
		get => _momentumLength.Value;
		set => _momentumLength.Value = value;
	}

	/// <summary>
	/// Lookback period used to normalize the ESG momentum spread.
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
	/// Candle type used for price data.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="EsgFactorMomentumStrategy"/>.
	/// </summary>
	public EsgFactorMomentumStrategy()
	{
		_security2Id = Param(nameof(Security2Id), Paths.HistoryDefaultSecurity2)
			.SetDisplay("Benchmark Security Id", "Identifier of the benchmark security", "General");

		_momentumLength = Param(nameof(MomentumLength), 40)
			.SetRange(5, 200)
			.SetDisplay("Momentum Length", "Momentum lookback period", "Indicators");

		_lookbackPeriod = Param(nameof(LookbackPeriod), 24)
			.SetRange(5, 120)
			.SetDisplay("Lookback Period", "Lookback period used to normalize the ESG momentum spread", "Indicators");

		_entryThreshold = Param(nameof(EntryThreshold), 1.2m)
			.SetRange(0.2m, 5m)
			.SetDisplay("Entry Threshold", "Z-score threshold required to open a position", "Signals");

		_exitThreshold = Param(nameof(ExitThreshold), 0.3m)
			.SetRange(0m, 2m)
			.SetDisplay("Exit Threshold", "Z-score threshold required to close a position", "Signals");

		_cooldownBars = Param(nameof(CooldownBars), 8)
			.SetRange(0, 120)
			.SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "Risk");

		_stopLoss = Param(nameof(StopLoss), 2.5m)
			.SetRange(0.5m, 10m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle TF", "Time-frame", "General");
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
		_primaryMomentum = null!;
		_benchmarkMomentum = null!;
		_spreadAverage = null!;
		_spreadDeviation = null!;
		_latestPrimaryScore = 0m;
		_latestBenchmarkScore = 0m;
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
		_primaryMomentum = new RateOfChange { Length = MomentumLength };
		_benchmarkMomentum = new RateOfChange { Length = MomentumLength };
		_spreadAverage = new SimpleMovingAverage { Length = LookbackPeriod };
		_spreadDeviation = new StandardDeviation { Length = LookbackPeriod };

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

		var momentumValue = _primaryMomentum.Process(candle);
		if (!momentumValue.IsEmpty && _primaryMomentum.IsFormed)
		{
			_latestPrimaryScore = momentumValue.ToDecimal() + CalculateEsgBias(candle);
			_primaryUpdated = true;
			TryProcessSpread(candle.OpenTime);
		}
	}

	private void ProcessBenchmarkCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var momentumValue = _benchmarkMomentum.Process(candle);
		if (!momentumValue.IsEmpty && _benchmarkMomentum.IsFormed)
		{
			_latestBenchmarkScore = momentumValue.ToDecimal() + CalculateEsgBias(candle);
			_benchmarkUpdated = true;
			TryProcessSpread(candle.OpenTime);
		}
	}

	private decimal CalculateEsgBias(ICandleMessage candle)
	{
		var priceBase = Math.Max(candle.OpenPrice, 1m);
		var range = Math.Max(candle.HighPrice - candle.LowPrice, Security?.PriceStep ?? 1m);
		var stability = 1m - Math.Min(0.2m, range / priceBase);
		var bodyBias = (candle.ClosePrice - candle.OpenPrice) / priceBase;

		return (stability * 5m) + (bodyBias * 100m);
	}

	private void TryProcessSpread(DateTime time)
	{
		if (!_primaryUpdated || !_benchmarkUpdated)
			return;

		_primaryUpdated = false;
		_benchmarkUpdated = false;

		var spread = _latestPrimaryScore - _latestBenchmarkScore;
		var mean = _spreadAverage.Process(spread, time, true).ToDecimal();
		var deviation = _spreadDeviation.Process(spread, time, true).ToDecimal();

		if (!_spreadAverage.IsFormed || !_spreadDeviation.IsFormed || deviation <= 0m)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var zScore = (spread - mean) / deviation;
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

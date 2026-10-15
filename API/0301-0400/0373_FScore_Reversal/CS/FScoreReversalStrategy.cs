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
/// F-Score reversal strategy that trades the primary instrument when a synthetic fundamental score aligns with relative short-term reversal versus a benchmark.
/// </summary>
public class FScoreReversalStrategy : Strategy
{
	private readonly StrategyParam<string> _security2Id;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<int> _scoreLength;
	private readonly StrategyParam<decimal> _entryThreshold;
	private readonly StrategyParam<decimal> _exitThreshold;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private Security _benchmark = null!;
	private RateOfChange _primaryReversal = null!;
	private RateOfChange _benchmarkReversal = null!;
	private ExponentialMovingAverage _primaryScore = null!;
	private ExponentialMovingAverage _benchmarkScore = null!;
	private SimpleMovingAverage _spreadAverage = null!;
	private StandardDeviation _spreadDeviation = null!;
	private decimal _latestPrimarySignal;
	private decimal _latestBenchmarkSignal;
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
	/// Lookback period for short-term reversal.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	/// <summary>
	/// Smoothing length for the synthetic F-Score proxy.
	/// </summary>
	public int ScoreLength
	{
		get => _scoreLength.Value;
		set => _scoreLength.Value = value;
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
	/// The type of candles to use for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="FScoreReversalStrategy"/>.
	/// </summary>
	public FScoreReversalStrategy()
	{
		_security2Id = Param(nameof(Security2Id), Paths.HistoryDefaultSecurity2)
			.SetDisplay("Benchmark Security Id", "Identifier of the benchmark security", "General");

		_lookback = Param(nameof(Lookback), 12)
			.SetRange(2, 80)
			.SetDisplay("Lookback", "Lookback period in bars", "General");

		_scoreLength = Param(nameof(ScoreLength), 8)
			.SetRange(2, 50)
			.SetDisplay("Score Length", "Smoothing length for the synthetic F-Score proxy", "General");

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
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_primaryReversal = null!;
		_benchmarkReversal = null!;
		_primaryScore = null!;
		_benchmarkScore = null!;
		_spreadAverage = null!;
		_spreadDeviation = null!;
		_latestPrimarySignal = 0m;
		_latestBenchmarkSignal = 0m;
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
		_primaryReversal = new RateOfChange { Length = Lookback };
		_benchmarkReversal = new RateOfChange { Length = Lookback };
		_primaryScore = new ExponentialMovingAverage { Length = ScoreLength };
		_benchmarkScore = new ExponentialMovingAverage { Length = ScoreLength };
		_spreadAverage = new SimpleMovingAverage { Length = 24 };
		_spreadDeviation = new StandardDeviation { Length = 24 };

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

		var reversalValue = _primaryReversal.Process(candle);
		var scoreValue = _primaryScore.Process(CalculateFScoreProxy(candle), candle.OpenTime, true);

		if (!reversalValue.IsEmpty && !scoreValue.IsEmpty && _primaryReversal.IsFormed && _primaryScore.IsFormed)
		{
			_latestPrimarySignal = scoreValue.ToDecimal() - reversalValue.ToDecimal();
			_primaryUpdated = true;
			TryProcessSpread(candle.OpenTime);
		}
	}

	private void ProcessBenchmarkCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var reversalValue = _benchmarkReversal.Process(candle);
		var scoreValue = _benchmarkScore.Process(CalculateFScoreProxy(candle), candle.OpenTime, true);

		if (!reversalValue.IsEmpty && !scoreValue.IsEmpty && _benchmarkReversal.IsFormed && _benchmarkScore.IsFormed)
		{
			_latestBenchmarkSignal = scoreValue.ToDecimal() - reversalValue.ToDecimal();
			_benchmarkUpdated = true;
			TryProcessSpread(candle.OpenTime);
		}
	}

	private decimal CalculateFScoreProxy(ICandleMessage candle)
	{
		var priceBase = Math.Max(candle.OpenPrice, 1m);
		var range = Math.Max(candle.HighPrice - candle.LowPrice, Security?.PriceStep ?? 1m);
		var closeLocation = ((candle.ClosePrice - candle.LowPrice) - (candle.HighPrice - candle.ClosePrice)) / range;
		var efficiency = (candle.ClosePrice - candle.OpenPrice) / priceBase;

		return (closeLocation * 2m) + (efficiency * 100m);
	}

	private void TryProcessSpread(DateTime time)
	{
		if (!_primaryUpdated || !_benchmarkUpdated)
			return;

		_primaryUpdated = false;
		_benchmarkUpdated = false;

		var spread = _latestPrimarySignal - _latestBenchmarkSignal;
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

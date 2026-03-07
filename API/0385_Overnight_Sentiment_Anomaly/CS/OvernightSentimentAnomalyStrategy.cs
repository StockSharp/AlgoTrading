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
/// Overnight sentiment anomaly strategy that trades the primary instrument when its opening gap diverges from benchmark sentiment.
/// </summary>
public class OvernightSentimentAnomalyStrategy : Strategy
{
	private readonly StrategyParam<string> _security2Id;
	private readonly StrategyParam<int> _sentimentPeriod;
	private readonly StrategyParam<int> _normalizationPeriod;
	private readonly StrategyParam<decimal> _entryThreshold;
	private readonly StrategyParam<decimal> _exitThreshold;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private Security _benchmark = null!;
	private RateOfChange _benchmarkSentiment = null!;
	private ExponentialMovingAverage _gapAverage = null!;
	private SimpleMovingAverage _signalAverage = null!;
	private StandardDeviation _signalDeviation = null!;
	private decimal _latestBenchmarkSentiment;
	private decimal _latestGap;
	private bool _primaryUpdated;
	private bool _benchmarkUpdated;
	private int _cooldownRemaining;

	/// <summary>
	/// Benchmark security identifier used as a sentiment proxy.
	/// </summary>
	public string Security2Id
	{
		get => _security2Id.Value;
		set => _security2Id.Value = value;
	}

	/// <summary>
	/// Lookback period used to estimate benchmark sentiment.
	/// </summary>
	public int SentimentPeriod
	{
		get => _sentimentPeriod.Value;
		set => _sentimentPeriod.Value = value;
	}

	/// <summary>
	/// Lookback period used to normalize the anomaly signal.
	/// </summary>
	public int NormalizationPeriod
	{
		get => _normalizationPeriod.Value;
		set => _normalizationPeriod.Value = value;
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
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public OvernightSentimentAnomalyStrategy()
	{
		_security2Id = Param(nameof(Security2Id), Paths.HistoryDefaultSecurity2)
			.SetDisplay("Benchmark Security Id", "Identifier of the benchmark security used as a sentiment proxy", "General");

		_sentimentPeriod = Param(nameof(SentimentPeriod), 4)
			.SetRange(2, 80)
			.SetDisplay("Sentiment Period", "Lookback period used to estimate benchmark sentiment", "Indicators");

		_normalizationPeriod = Param(nameof(NormalizationPeriod), 8)
			.SetRange(5, 120)
			.SetDisplay("Normalization Period", "Lookback period used to normalize the anomaly signal", "Indicators");

		_entryThreshold = Param(nameof(EntryThreshold), 0.4m)
			.SetRange(0.2m, 5m)
			.SetDisplay("Entry Threshold", "Z-score threshold required to open a position", "Signals");

		_exitThreshold = Param(nameof(ExitThreshold), 0.1m)
			.SetRange(0m, 2m)
			.SetDisplay("Exit Threshold", "Z-score threshold required to close a position", "Signals");

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
		_benchmarkSentiment = null!;
		_gapAverage = null!;
		_signalAverage = null!;
		_signalDeviation = null!;
		_latestBenchmarkSentiment = 0m;
		_latestGap = 0m;
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
		_benchmarkSentiment = new RateOfChange { Length = SentimentPeriod };
		_gapAverage = new ExponentialMovingAverage { Length = Math.Max(2, SentimentPeriod) };
		_signalAverage = new SimpleMovingAverage { Length = NormalizationPeriod };
		_signalDeviation = new StandardDeviation { Length = NormalizationPeriod };

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

		var gap = (candle.OpenPrice - candle.LowPrice) / Math.Max(candle.LowPrice, 1m);
		var smoothedGap = _gapAverage.Process(gap, candle.OpenTime, true).ToDecimal();

		if (!_gapAverage.IsFormed)
			return;

		_latestGap = smoothedGap;
		_primaryUpdated = true;
		TryProcessSignal(candle);
	}

	private void ProcessBenchmarkCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var sentimentValue = _benchmarkSentiment.Process(candle);
		if (sentimentValue.IsEmpty || !_benchmarkSentiment.IsFormed)
			return;

		_latestBenchmarkSentiment = sentimentValue.ToDecimal();
		_benchmarkUpdated = true;
		TryProcessSignal(candle);
	}

	private void TryProcessSignal(ICandleMessage candle)
	{
		if (!_primaryUpdated || !_benchmarkUpdated)
			return;

		_primaryUpdated = false;
		_benchmarkUpdated = false;

		var signal = _latestBenchmarkSentiment - (_latestGap * 10m);
		var mean = _signalAverage.Process(signal, candle.OpenTime, true).ToDecimal();
		var deviation = _signalDeviation.Process(signal, candle.OpenTime, true).ToDecimal();

		if (!_signalAverage.IsFormed || !_signalDeviation.IsFormed || deviation <= 0m)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var zScore = (signal - mean) / deviation;
		var bullishEntry = zScore >= EntryThreshold;
		var bearishEntry = zScore <= -EntryThreshold;

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

	}
}

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
/// Country value factor strategy that trades the primary instrument when its synthetic CAPE ratio is cheap or expensive relative to a benchmark.
/// </summary>
public class CountryValueFactorStrategy : Strategy
{
	private readonly StrategyParam<string> _security2Id;
	private readonly StrategyParam<int> _earningsLength;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _entryThreshold;
	private readonly StrategyParam<decimal> _exitThreshold;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private Security _benchmark = null!;
	private ExponentialMovingAverage _primaryEarnings = null!;
	private ExponentialMovingAverage _benchmarkEarnings = null!;
	private SimpleMovingAverage _capeSpreadAverage = null!;
	private StandardDeviation _capeSpreadDeviation = null!;
	private decimal _latestPrimaryCape;
	private decimal _latestBenchmarkCape;
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
	/// Smoothing length for the synthetic earnings proxy.
	/// </summary>
	public int EarningsLength
	{
		get => _earningsLength.Value;
		set => _earningsLength.Value = value;
	}

	/// <summary>
	/// Lookback period used to normalize CAPE spread.
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
	/// Candle type used for both instruments.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public CountryValueFactorStrategy()
	{
		_security2Id = Param(nameof(Security2Id), Paths.HistoryDefaultSecurity2)
			.SetDisplay("Benchmark Security Id", "Identifier of the benchmark security", "General");

		_earningsLength = Param(nameof(EarningsLength), 14)
			.SetRange(2, 80)
			.SetDisplay("Earnings Length", "Smoothing length for the synthetic earnings proxy", "Indicators");

		_lookbackPeriod = Param(nameof(LookbackPeriod), 28)
			.SetRange(10, 150)
			.SetDisplay("Lookback Period", "Lookback period used to normalize CAPE spread", "Indicators");

		_entryThreshold = Param(nameof(EntryThreshold), 1.35m)
			.SetRange(0.4m, 4m)
			.SetDisplay("Entry Threshold", "Z-score threshold required to open a position", "Signals");

		_exitThreshold = Param(nameof(ExitThreshold), 0.35m)
			.SetRange(0m, 2m)
			.SetDisplay("Exit Threshold", "Z-score threshold required to close a position", "Signals");

		_cooldownBars = Param(nameof(CooldownBars), 12)
			.SetRange(0, 120)
			.SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "Risk");

		_stopLoss = Param(nameof(StopLoss), 2.5m)
			.SetRange(0.5m, 10m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle series for both instruments", "General");
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
		_primaryEarnings = null!;
		_benchmarkEarnings = null!;
		_capeSpreadAverage = null!;
		_capeSpreadDeviation = null!;
		_latestPrimaryCape = 0m;
		_latestBenchmarkCape = 0m;
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
		_primaryEarnings = new ExponentialMovingAverage { Length = EarningsLength };
		_benchmarkEarnings = new ExponentialMovingAverage { Length = EarningsLength };
		_capeSpreadAverage = new SimpleMovingAverage { Length = LookbackPeriod };
		_capeSpreadDeviation = new StandardDeviation { Length = LookbackPeriod };

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

		_latestPrimaryCape = UpdateCape(_primaryEarnings, candle);
		_primaryUpdated = true;
		TryProcessSpread(candle.OpenTime);
	}

	private void ProcessBenchmarkCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_latestBenchmarkCape = UpdateCape(_benchmarkEarnings, candle);
		_benchmarkUpdated = true;
		TryProcessSpread(candle.OpenTime);
	}

	private decimal UpdateCape(ExponentialMovingAverage average, ICandleMessage candle)
	{
		var syntheticEarnings = CalculateSyntheticEarnings(candle);
		var smoothedEarnings = average.Process(syntheticEarnings, candle.OpenTime, true).ToDecimal();

		return candle.ClosePrice / Math.Max(smoothedEarnings, 1m);
	}

	private decimal CalculateSyntheticEarnings(ICandleMessage candle)
	{
		var priceBase = Math.Max(candle.OpenPrice, 1m);
		var range = Math.Max(candle.HighPrice - candle.LowPrice, Security?.PriceStep ?? 1m);
		var profitabilityProxy = priceBase * (1m + Math.Min(0.08m, (candle.ClosePrice - candle.OpenPrice) / priceBase));
		var stabilityProxy = priceBase * (1m - Math.Min(0.2m, range / priceBase));

		return (profitabilityProxy + stabilityProxy) / 2m;
	}

	private void TryProcessSpread(DateTime time)
	{
		if (!_primaryUpdated || !_benchmarkUpdated)
			return;

		_primaryUpdated = false;
		_benchmarkUpdated = false;

		if (!_primaryEarnings.IsFormed || !_benchmarkEarnings.IsFormed)
			return;

		var spread = _latestPrimaryCape - _latestBenchmarkCape;
		var mean = _capeSpreadAverage.Process(spread, time, true).ToDecimal();
		var deviation = _capeSpreadDeviation.Process(spread, time, true).ToDecimal();

		if (!_capeSpreadAverage.IsFormed || !_capeSpreadDeviation.IsFormed || deviation <= 0m)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var zScore = (spread - mean) / deviation;
		var bullishEntry = _previousZScore is decimal previousBullish && previousBullish > -EntryThreshold && zScore <= -EntryThreshold;
		var bearishEntry = _previousZScore is decimal previousBearish && previousBearish < EntryThreshold && zScore >= EntryThreshold;

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
		else if (Position > 0 && zScore >= -ExitThreshold)
		{
			SellMarket(Position);
			_cooldownRemaining = CooldownBars;
		}
		else if (Position < 0 && zScore <= ExitThreshold)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_previousZScore = zScore;
	}
}

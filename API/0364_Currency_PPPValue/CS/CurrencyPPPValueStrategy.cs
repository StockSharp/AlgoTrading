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
/// Currency PPP value strategy that trades the primary instrument when its synthetic PPP deviation becomes extreme relative to a benchmark currency.
/// </summary>
public class CurrencyPppValueStrategy : Strategy
{
	private readonly StrategyParam<string> _security2Id;
	private readonly StrategyParam<int> _pppLength;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _entryThreshold;
	private readonly StrategyParam<decimal> _exitThreshold;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private Security _benchmark = null!;
	private ExponentialMovingAverage _primaryPpp = null!;
	private ExponentialMovingAverage _benchmarkPpp = null!;
	private SimpleMovingAverage _spreadAverage = null!;
	private StandardDeviation _spreadDeviation = null!;
	private decimal _latestPrimaryDeviation;
	private decimal _latestBenchmarkDeviation;
	private decimal? _previousZScore;
	private bool _primaryUpdated;
	private bool _benchmarkUpdated;
	private int _cooldownRemaining;

	/// <summary>
	/// Benchmark currency identifier.
	/// </summary>
	public string Security2Id
	{
		get => _security2Id.Value;
		set => _security2Id.Value = value;
	}

	/// <summary>
	/// Smoothing length for the synthetic PPP anchor.
	/// </summary>
	public int PppLength
	{
		get => _pppLength.Value;
		set => _pppLength.Value = value;
	}

	/// <summary>
	/// Lookback period used to normalize the deviation spread.
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
	/// Constructor.
	/// </summary>
	public CurrencyPppValueStrategy()
	{
		_security2Id = Param(nameof(Security2Id), Paths.HistoryDefaultSecurity2)
			.SetDisplay("Benchmark Security Id", "Identifier of the benchmark currency security", "General");

		_pppLength = Param(nameof(PppLength), 14)
			.SetRange(2, 80)
			.SetDisplay("PPP Length", "Smoothing length for the synthetic PPP anchor", "Indicators");

		_lookbackPeriod = Param(nameof(LookbackPeriod), 24)
			.SetRange(5, 120)
			.SetDisplay("Lookback Period", "Lookback period used to normalize the deviation spread", "Indicators");

		_entryThreshold = Param(nameof(EntryThreshold), 1.25m)
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
			.SetDisplay("Candle Type", "Time frame of candles", "General");
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
		_primaryPpp = null!;
		_benchmarkPpp = null!;
		_spreadAverage = null!;
		_spreadDeviation = null!;
		_latestPrimaryDeviation = 0m;
		_latestBenchmarkDeviation = 0m;
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
			throw new InvalidOperationException("Primary currency security is not specified.");

		if (Security2Id.IsEmpty())
			throw new InvalidOperationException("Benchmark currency identifier is not specified.");

		_benchmark = this.LookupById(Security2Id) ?? new Security { Id = Security2Id };
		_primaryPpp = new ExponentialMovingAverage { Length = PppLength };
		_benchmarkPpp = new ExponentialMovingAverage { Length = PppLength };
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

		_latestPrimaryDeviation = UpdateDeviation(_primaryPpp, candle);
		_primaryUpdated = true;
		TryProcessSpread(candle.OpenTime);
	}

	private void ProcessBenchmarkCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_latestBenchmarkDeviation = UpdateDeviation(_benchmarkPpp, candle);
		_benchmarkUpdated = true;
		TryProcessSpread(candle.OpenTime);
	}

	private decimal UpdateDeviation(ExponentialMovingAverage average, ICandleMessage candle)
	{
		var syntheticPpp = CalculateSyntheticPpp(candle);
		var pppAnchor = average.Process(syntheticPpp, candle.OpenTime, true).ToDecimal();

		return (candle.ClosePrice - pppAnchor) / Math.Max(pppAnchor, 1m);
	}

	private decimal CalculateSyntheticPpp(ICandleMessage candle)
	{
		var priceBase = Math.Max(candle.OpenPrice, 1m);
		var range = Math.Max(candle.HighPrice - candle.LowPrice, Security?.PriceStep ?? 1m);
		var purchasingPowerAnchor = (candle.OpenPrice + candle.LowPrice + candle.ClosePrice) / 3m;
		var inflationPenalty = priceBase * Math.Min(0.12m, range / priceBase);

		return purchasingPowerAnchor - inflationPenalty;
	}

	private void TryProcessSpread(DateTime time)
	{
		if (!_primaryUpdated || !_benchmarkUpdated)
			return;

		_primaryUpdated = false;
		_benchmarkUpdated = false;

		var spread = _latestPrimaryDeviation - _latestBenchmarkDeviation;
		var mean = _spreadAverage.Process(spread, time, true).ToDecimal();
		var deviation = _spreadDeviation.Process(spread, time, true).ToDecimal();

		if (!_spreadAverage.IsFormed || !_spreadDeviation.IsFormed || deviation <= 0m)
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

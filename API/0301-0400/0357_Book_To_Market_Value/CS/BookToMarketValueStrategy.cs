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
/// Relative book-to-market factor strategy that trades the primary instrument against a benchmark using a synthetic valuation spread.
/// </summary>
public class BookToMarketValueStrategy : Strategy
{
	private readonly StrategyParam<string> _security2Id;
	private readonly StrategyParam<int> _bookLength;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _entryThreshold;
	private readonly StrategyParam<decimal> _exitThreshold;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private Security _benchmark = null!;
	private ExponentialMovingAverage _primaryBook = null!;
	private ExponentialMovingAverage _benchmarkBook = null!;
	private SimpleMovingAverage _ratioSpreadAverage = null!;
	private StandardDeviation _ratioSpreadDeviation = null!;
	private decimal _latestPrimaryRatio;
	private decimal _latestBenchmarkRatio;
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
	/// Smoothing length for the synthetic book value.
	/// </summary>
	public int BookLength
	{
		get => _bookLength.Value;
		set => _bookLength.Value = value;
	}

	/// <summary>
	/// Lookback period used to normalize valuation spread.
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
	public BookToMarketValueStrategy()
	{
		_security2Id = Param(nameof(Security2Id), Paths.HistoryDefaultSecurity2)
			.SetDisplay("Benchmark Security Id", "Identifier of the benchmark security", "General");

		_bookLength = Param(nameof(BookLength), 10)
			.SetRange(2, 50)
			.SetDisplay("Book Length", "Smoothing length for the synthetic book value", "Indicators");

		_lookbackPeriod = Param(nameof(LookbackPeriod), 28)
			.SetRange(10, 150)
			.SetDisplay("Lookback Period", "Lookback period used to normalize valuation spread", "Indicators");

		_entryThreshold = Param(nameof(EntryThreshold), 1.35m)
			.SetRange(0.5m, 4m)
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
		_primaryBook = null!;
		_benchmarkBook = null!;
		_ratioSpreadAverage = null!;
		_ratioSpreadDeviation = null!;
		_latestPrimaryRatio = 0m;
		_latestBenchmarkRatio = 0m;
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
		_primaryBook = new ExponentialMovingAverage { Length = BookLength };
		_benchmarkBook = new ExponentialMovingAverage { Length = BookLength };
		_ratioSpreadAverage = new SimpleMovingAverage { Length = LookbackPeriod };
		_ratioSpreadDeviation = new StandardDeviation { Length = LookbackPeriod };
		_cooldownRemaining = 0;

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

		_latestPrimaryRatio = UpdateRatio(_primaryBook, candle);
		_primaryUpdated = true;
		TryProcessSpread(candle.OpenTime);
	}

	private void ProcessBenchmarkCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_latestBenchmarkRatio = UpdateRatio(_benchmarkBook, candle);
		_benchmarkUpdated = true;
		TryProcessSpread(candle.OpenTime);
	}

	private decimal UpdateRatio(ExponentialMovingAverage average, ICandleMessage candle)
	{
		var syntheticBookValue = CalculateSyntheticBookValue(candle);
		var smoothedBook = average.Process(syntheticBookValue, candle.OpenTime, true).ToDecimal();

		return smoothedBook / Math.Max(candle.ClosePrice, 1m);
	}

	private decimal CalculateSyntheticBookValue(ICandleMessage candle)
	{
		var priceBase = Math.Max(candle.OpenPrice, 1m);
		var range = Math.Max(candle.HighPrice - candle.LowPrice, Security?.PriceStep ?? 1m);
		var balanceComponent = (candle.OpenPrice + candle.LowPrice + candle.ClosePrice) / 3m;
		var stabilityComponent = priceBase * (1m - Math.Min(0.2m, range / priceBase));

		return balanceComponent + stabilityComponent;
	}

	private void TryProcessSpread(DateTime time)
	{
		if (!_primaryUpdated || !_benchmarkUpdated)
			return;

		_primaryUpdated = false;
		_benchmarkUpdated = false;

		if (!_primaryBook.IsFormed || !_benchmarkBook.IsFormed)
			return;

		var ratioSpread = _latestPrimaryRatio - _latestBenchmarkRatio;
		var mean = _ratioSpreadAverage.Process(ratioSpread, time, true).ToDecimal();
		var deviation = _ratioSpreadDeviation.Process(ratioSpread, time, true).ToDecimal();

		if (!_ratioSpreadAverage.IsFormed || !_ratioSpreadDeviation.IsFormed || deviation <= 0m)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var zScore = (ratioSpread - mean) / deviation;
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

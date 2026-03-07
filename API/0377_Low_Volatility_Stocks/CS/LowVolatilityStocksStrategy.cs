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
/// Low volatility anomaly strategy that trades the primary instrument when its realized volatility diverges from a benchmark instrument.
/// </summary>
public class LowVolatilityStocksStrategy : Strategy
{
	private readonly StrategyParam<string> _security2Id;
	private readonly StrategyParam<int> _volatilityPeriod;
	private readonly StrategyParam<int> _normalizationPeriod;
	private readonly StrategyParam<int> _trendPeriod;
	private readonly StrategyParam<decimal> _entryThreshold;
	private readonly StrategyParam<decimal> _exitThreshold;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private Security _benchmark = null!;
	private StandardDeviation _primaryVolatility = null!;
	private StandardDeviation _benchmarkVolatility = null!;
	private SimpleMovingAverage _spreadAverage = null!;
	private StandardDeviation _spreadDeviation = null!;
	private SimpleMovingAverage _primaryTrend = null!;
	private decimal? _previousPrimaryClose;
	private decimal? _previousBenchmarkClose;
	private decimal _latestPrimaryVolatility;
	private decimal _latestBenchmarkVolatility;
	private decimal _latestPrimaryTrend;
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
	/// Lookback period used to estimate realized volatility.
	/// </summary>
	public int VolatilityPeriod
	{
		get => _volatilityPeriod.Value;
		set => _volatilityPeriod.Value = value;
	}

	/// <summary>
	/// Lookback period used to normalize the volatility spread.
	/// </summary>
	public int NormalizationPeriod
	{
		get => _normalizationPeriod.Value;
		set => _normalizationPeriod.Value = value;
	}

	/// <summary>
	/// Trend period used to align entries with the primary instrument direction.
	/// </summary>
	public int TrendPeriod
	{
		get => _trendPeriod.Value;
		set => _trendPeriod.Value = value;
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

	public LowVolatilityStocksStrategy()
	{
		_security2Id = Param(nameof(Security2Id), Paths.HistoryDefaultSecurity2)
			.SetDisplay("Benchmark Security Id", "Identifier of the benchmark security", "General");

		_volatilityPeriod = Param(nameof(VolatilityPeriod), 18)
			.SetRange(5, 120)
			.SetDisplay("Volatility Period", "Lookback period used to estimate realized volatility", "Indicators");

		_normalizationPeriod = Param(nameof(NormalizationPeriod), 24)
			.SetRange(5, 120)
			.SetDisplay("Normalization Period", "Lookback period used to normalize the volatility spread", "Indicators");

		_trendPeriod = Param(nameof(TrendPeriod), 30)
			.SetRange(5, 200)
			.SetDisplay("Trend Period", "Trend period used to align entries with the primary instrument direction", "Indicators");

		_entryThreshold = Param(nameof(EntryThreshold), 1.1m)
			.SetRange(0.2m, 5m)
			.SetDisplay("Entry Threshold", "Z-score threshold required to open a position", "Signals");

		_exitThreshold = Param(nameof(ExitThreshold), 0.25m)
			.SetRange(0m, 2m)
			.SetDisplay("Exit Threshold", "Z-score threshold required to close a position", "Signals");

		_cooldownBars = Param(nameof(CooldownBars), 6)
			.SetRange(0, 120)
			.SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "Risk");

		_stopLoss = Param(nameof(StopLoss), 2.5m)
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
		_primaryVolatility = null!;
		_benchmarkVolatility = null!;
		_spreadAverage = null!;
		_spreadDeviation = null!;
		_primaryTrend = null!;
		_previousPrimaryClose = null;
		_previousBenchmarkClose = null;
		_latestPrimaryVolatility = 0m;
		_latestBenchmarkVolatility = 0m;
		_latestPrimaryTrend = 0m;
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
		_primaryVolatility = new StandardDeviation { Length = VolatilityPeriod };
		_benchmarkVolatility = new StandardDeviation { Length = VolatilityPeriod };
		_spreadAverage = new SimpleMovingAverage { Length = NormalizationPeriod };
		_spreadDeviation = new StandardDeviation { Length = NormalizationPeriod };
		_primaryTrend = new SimpleMovingAverage { Length = TrendPeriod };

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

		var trendValue = _primaryTrend.Process(candle);
		if (!trendValue.IsEmpty && _primaryTrend.IsFormed)
			_latestPrimaryTrend = trendValue.ToDecimal();

		var ret = CalculateReturn(candle.ClosePrice, ref _previousPrimaryClose);
		if (ret is null)
			return;

		var volatilityValue = _primaryVolatility.Process(Math.Abs(ret.Value), candle.OpenTime, true);
		if (!volatilityValue.IsEmpty && _primaryVolatility.IsFormed)
		{
			_latestPrimaryVolatility = volatilityValue.ToDecimal();
			_primaryUpdated = true;
			TryProcessSpread(candle);
		}
	}

	private void ProcessBenchmarkCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var ret = CalculateReturn(candle.ClosePrice, ref _previousBenchmarkClose);
		if (ret is null)
			return;

		var volatilityValue = _benchmarkVolatility.Process(Math.Abs(ret.Value), candle.OpenTime, true);
		if (!volatilityValue.IsEmpty && _benchmarkVolatility.IsFormed)
		{
			_latestBenchmarkVolatility = volatilityValue.ToDecimal();
			_benchmarkUpdated = true;
			TryProcessSpread(candle);
		}
	}

	private static decimal? CalculateReturn(decimal closePrice, ref decimal? previousClose)
	{
		if (previousClose is not decimal previous || previous <= 0m)
		{
			previousClose = closePrice;
			return null;
		}

		var ret = (closePrice - previous) / previous;
		previousClose = closePrice;
		return ret;
	}

	private void TryProcessSpread(ICandleMessage candle)
	{
		if (!_primaryUpdated || !_benchmarkUpdated)
			return;

		_primaryUpdated = false;
		_benchmarkUpdated = false;

		var spread = _latestBenchmarkVolatility - _latestPrimaryVolatility;
		var mean = _spreadAverage.Process(spread, candle.OpenTime, true).ToDecimal();
		var deviation = _spreadDeviation.Process(spread, candle.OpenTime, true).ToDecimal();

		if (!_spreadAverage.IsFormed || !_spreadDeviation.IsFormed || deviation <= 0m || !_primaryTrend.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var zScore = (spread - mean) / deviation;
		var bullishTrend = candle.ClosePrice >= _latestPrimaryTrend;
		var bearishTrend = candle.ClosePrice <= _latestPrimaryTrend;
		var bullishEntry = zScore >= EntryThreshold && bullishTrend;
		var bearishEntry = zScore <= -EntryThreshold && bearishTrend;

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
		else if (Position > 0 && (zScore <= ExitThreshold || bearishEntry))
		{
			SellMarket(Position);
			_cooldownRemaining = CooldownBars;
		}
		else if (Position < 0 && (zScore >= -ExitThreshold || bullishEntry))
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

	}
}

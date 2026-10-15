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
/// Momentum, reversal, and volatility composite strategy that trades the primary instrument when its composite score diverges from a benchmark instrument.
/// </summary>
public class MomentumRevVolStrategy : Strategy
{
	private readonly StrategyParam<string> _security2Id;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<int> _reversalPeriod;
	private readonly StrategyParam<int> _volatilityPeriod;
	private readonly StrategyParam<int> _normalizationPeriod;
	private readonly StrategyParam<decimal> _momentumWeight;
	private readonly StrategyParam<decimal> _reversalWeight;
	private readonly StrategyParam<decimal> _volatilityWeight;
	private readonly StrategyParam<decimal> _entryThreshold;
	private readonly StrategyParam<decimal> _exitThreshold;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private Security _benchmark = null!;
	private RateOfChange _primaryMomentum = null!;
	private RateOfChange _benchmarkMomentum = null!;
	private RateOfChange _primaryReversal = null!;
	private RateOfChange _benchmarkReversal = null!;
	private StandardDeviation _primaryVolatility = null!;
	private StandardDeviation _benchmarkVolatility = null!;
	private SimpleMovingAverage _spreadAverage = null!;
	private StandardDeviation _spreadDeviation = null!;
	private decimal? _previousPrimaryClose;
	private decimal? _previousBenchmarkClose;
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
	/// Lookback period for medium-term momentum.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Lookback period for short-term reversal.
	/// </summary>
	public int ReversalPeriod
	{
		get => _reversalPeriod.Value;
		set => _reversalPeriod.Value = value;
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
	/// Lookback period used to normalize the relative composite spread.
	/// </summary>
	public int NormalizationPeriod
	{
		get => _normalizationPeriod.Value;
		set => _normalizationPeriod.Value = value;
	}

	/// <summary>
	/// Weight applied to the momentum component.
	/// </summary>
	public decimal MomentumWeight
	{
		get => _momentumWeight.Value;
		set => _momentumWeight.Value = value;
	}

	/// <summary>
	/// Weight applied to the reversal component.
	/// </summary>
	public decimal ReversalWeight
	{
		get => _reversalWeight.Value;
		set => _reversalWeight.Value = value;
	}

	/// <summary>
	/// Weight applied to the volatility component.
	/// </summary>
	public decimal VolatilityWeight
	{
		get => _volatilityWeight.Value;
		set => _volatilityWeight.Value = value;
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

	public MomentumRevVolStrategy()
	{
		_security2Id = Param(nameof(Security2Id), Paths.HistoryDefaultSecurity2)
			.SetDisplay("Benchmark Security Id", "Identifier of the benchmark security", "General");

		_momentumPeriod = Param(nameof(MomentumPeriod), 36)
			.SetRange(8, 200)
			.SetDisplay("Momentum Period", "Lookback period for medium-term momentum", "Indicators");

		_reversalPeriod = Param(nameof(ReversalPeriod), 8)
			.SetRange(2, 60)
			.SetDisplay("Reversal Period", "Lookback period for short-term reversal", "Indicators");

		_volatilityPeriod = Param(nameof(VolatilityPeriod), 18)
			.SetRange(5, 120)
			.SetDisplay("Volatility Period", "Lookback period used to estimate realized volatility", "Indicators");

		_normalizationPeriod = Param(nameof(NormalizationPeriod), 24)
			.SetRange(5, 120)
			.SetDisplay("Normalization Period", "Lookback period used to normalize the relative composite spread", "Indicators");

		_momentumWeight = Param(nameof(MomentumWeight), 1m)
			.SetRange(0.1m, 10m)
			.SetDisplay("Momentum Weight", "Weight applied to the momentum component", "Signals");

		_reversalWeight = Param(nameof(ReversalWeight), 0.8m)
			.SetRange(0.1m, 10m)
			.SetDisplay("Reversal Weight", "Weight applied to the reversal component", "Signals");

		_volatilityWeight = Param(nameof(VolatilityWeight), 1.5m)
			.SetRange(0.1m, 10m)
			.SetDisplay("Volatility Weight", "Weight applied to the volatility component", "Signals");

		_entryThreshold = Param(nameof(EntryThreshold), 1.1m)
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
		_primaryMomentum = null!;
		_benchmarkMomentum = null!;
		_primaryReversal = null!;
		_benchmarkReversal = null!;
		_primaryVolatility = null!;
		_benchmarkVolatility = null!;
		_spreadAverage = null!;
		_spreadDeviation = null!;
		_previousPrimaryClose = null;
		_previousBenchmarkClose = null;
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
		_primaryMomentum = new RateOfChange { Length = MomentumPeriod };
		_benchmarkMomentum = new RateOfChange { Length = MomentumPeriod };
		_primaryReversal = new RateOfChange { Length = ReversalPeriod };
		_benchmarkReversal = new RateOfChange { Length = ReversalPeriod };
		_primaryVolatility = new StandardDeviation { Length = VolatilityPeriod };
		_benchmarkVolatility = new StandardDeviation { Length = VolatilityPeriod };
		_spreadAverage = new SimpleMovingAverage { Length = NormalizationPeriod };
		_spreadDeviation = new StandardDeviation { Length = NormalizationPeriod };

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
		var reversalValue = _primaryReversal.Process(candle);
		var ret = CalculateReturn(candle.ClosePrice, ref _previousPrimaryClose);

		if (momentumValue.IsEmpty || reversalValue.IsEmpty || ret is null || !_primaryMomentum.IsFormed || !_primaryReversal.IsFormed)
			return;

		var volatilityValue = _primaryVolatility.Process(Math.Abs(ret.Value), candle.OpenTime, true);
		if (volatilityValue.IsEmpty || !_primaryVolatility.IsFormed)
			return;

		_latestPrimarySignal =
			(MomentumWeight * momentumValue.ToDecimal()) -
			(ReversalWeight * reversalValue.ToDecimal()) -
			(VolatilityWeight * volatilityValue.ToDecimal());

		_primaryUpdated = true;
		TryProcessSpread(candle.OpenTime);
	}

	private void ProcessBenchmarkCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var momentumValue = _benchmarkMomentum.Process(candle);
		var reversalValue = _benchmarkReversal.Process(candle);
		var ret = CalculateReturn(candle.ClosePrice, ref _previousBenchmarkClose);

		if (momentumValue.IsEmpty || reversalValue.IsEmpty || ret is null || !_benchmarkMomentum.IsFormed || !_benchmarkReversal.IsFormed)
			return;

		var volatilityValue = _benchmarkVolatility.Process(Math.Abs(ret.Value), candle.OpenTime, true);
		if (volatilityValue.IsEmpty || !_benchmarkVolatility.IsFormed)
			return;

		_latestBenchmarkSignal =
			(MomentumWeight * momentumValue.ToDecimal()) -
			(ReversalWeight * reversalValue.ToDecimal()) -
			(VolatilityWeight * volatilityValue.ToDecimal());

		_benchmarkUpdated = true;
		TryProcessSpread(candle.OpenTime);
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

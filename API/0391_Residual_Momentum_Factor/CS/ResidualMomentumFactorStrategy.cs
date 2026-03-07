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
/// Residual momentum factor strategy that trades the primary instrument when its benchmark-adjusted momentum diverges from the market proxy.
/// </summary>
public class ResidualMomentumFactorStrategy : Strategy
{
	private readonly StrategyParam<string> _security2Id;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<int> _betaLength;
	private readonly StrategyParam<int> _normalizationPeriod;
	private readonly StrategyParam<decimal> _entryThreshold;
	private readonly StrategyParam<decimal> _exitThreshold;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private Security _benchmark = null!;
	private RateOfChange _primaryMomentum = null!;
	private RateOfChange _benchmarkMomentum = null!;
	private ExponentialMovingAverage _betaAverage = null!;
	private SimpleMovingAverage _spreadAverage = null!;
	private StandardDeviation _spreadDeviation = null!;
	private decimal? _previousPrimaryClose;
	private decimal? _previousBenchmarkClose;
	private decimal _latestPrimaryMomentum;
	private decimal _latestBenchmarkMomentum;
	private decimal _latestBeta;
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
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing length used for the rolling beta proxy.
	/// </summary>
	public int BetaLength
	{
		get => _betaLength.Value;
		set => _betaLength.Value = value;
	}

	/// <summary>
	/// Lookback period used to normalize residual momentum.
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

	public ResidualMomentumFactorStrategy()
	{
		_security2Id = Param(nameof(Security2Id), Paths.HistoryDefaultSecurity2)
			.SetDisplay("Benchmark Security Id", "Identifier of the benchmark security", "General");

		_momentumPeriod = Param(nameof(MomentumPeriod), 32)
			.SetRange(5, 200)
			.SetDisplay("Momentum Period", "Momentum lookback period", "Indicators");

		_betaLength = Param(nameof(BetaLength), 8)
			.SetRange(2, 80)
			.SetDisplay("Beta Length", "Smoothing length used for the rolling beta proxy", "Indicators");

		_normalizationPeriod = Param(nameof(NormalizationPeriod), 24)
			.SetRange(5, 120)
			.SetDisplay("Normalization Period", "Lookback period used to normalize residual momentum", "Indicators");

		_entryThreshold = Param(nameof(EntryThreshold), 1.1m)
			.SetRange(0.2m, 5m)
			.SetDisplay("Entry Threshold", "Z-score threshold required to open a position", "Signals");

		_exitThreshold = Param(nameof(ExitThreshold), 0.25m)
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
		_primaryMomentum = null!;
		_benchmarkMomentum = null!;
		_betaAverage = null!;
		_spreadAverage = null!;
		_spreadDeviation = null!;
		_previousPrimaryClose = null;
		_previousBenchmarkClose = null;
		_latestPrimaryMomentum = 0m;
		_latestBenchmarkMomentum = 0m;
		_latestBeta = 1m;
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
		_betaAverage = new ExponentialMovingAverage { Length = BetaLength };
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
		if (momentumValue.IsEmpty || !_primaryMomentum.IsFormed)
			return;

		latestBetaFromReturns(candle.ClosePrice, true);
		_latestPrimaryMomentum = momentumValue.ToDecimal();
		_primaryUpdated = true;
		TryProcessResidualMomentum(candle.OpenTime);
	}

	private void ProcessBenchmarkCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var momentumValue = _benchmarkMomentum.Process(candle);
		if (momentumValue.IsEmpty || !_benchmarkMomentum.IsFormed)
			return;

		latestBetaFromReturns(candle.ClosePrice, false);
		_latestBenchmarkMomentum = momentumValue.ToDecimal();
		_benchmarkUpdated = true;
		TryProcessResidualMomentum(candle.OpenTime);
	}

	private void latestBetaFromReturns(decimal closePrice, bool isPrimary)
	{
		ref var previousClose = ref isPrimary ? ref _previousPrimaryClose : ref _previousBenchmarkClose;

		if (previousClose is not decimal previous || previous <= 0m)
		{
			previousClose = closePrice;
			return;
		}

		var ret = (closePrice - previous) / previous;
		previousClose = closePrice;

		if (isPrimary)
			_latestPrimaryMomentum = ret;
		else
			_latestBeta = _betaAverage.Process(ret.Abs(), CurrentTime, true).ToDecimal();
	}

	private void TryProcessResidualMomentum(DateTime time)
	{
		if (!_primaryUpdated || !_benchmarkUpdated)
			return;

		_primaryUpdated = false;
		_benchmarkUpdated = false;

		var betaAdjustedBenchmark = _latestBenchmarkMomentum * Math.Max(_latestBeta, 0.2m);
		var residualMomentum = _latestPrimaryMomentum - betaAdjustedBenchmark;
		var mean = _spreadAverage.Process(residualMomentum, time, true).ToDecimal();
		var deviation = _spreadDeviation.Process(residualMomentum, time, true).ToDecimal();

		if (!_spreadAverage.IsFormed || !_spreadDeviation.IsFormed || deviation <= 0m)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var zScore = (residualMomentum - mean) / deviation;
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

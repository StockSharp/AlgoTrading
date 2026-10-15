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
/// Momentum plus asset-growth strategy that trades the primary instrument when its risk-adjusted momentum outperforms a benchmark while synthetic asset growth remains contained.
/// </summary>
public class MomentumAssetGrowthStrategy : Strategy
{
	private readonly StrategyParam<string> _security2Id;
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<int> _assetLength;
	private readonly StrategyParam<int> _normalizationPeriod;
	private readonly StrategyParam<decimal> _growthPenalty;
	private readonly StrategyParam<decimal> _entryThreshold;
	private readonly StrategyParam<decimal> _exitThreshold;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private Security _benchmark = null!;
	private RateOfChange _primaryMomentum = null!;
	private RateOfChange _benchmarkMomentum = null!;
	private ExponentialMovingAverage _primaryAssetBase = null!;
	private ExponentialMovingAverage _benchmarkAssetBase = null!;
	private SimpleMovingAverage _signalAverage = null!;
	private StandardDeviation _signalDeviation = null!;
	private decimal _previousPrimaryAssetBase;
	private decimal _previousBenchmarkAssetBase;
	private decimal _latestPrimaryMomentum;
	private decimal _latestBenchmarkMomentum;
	private decimal _latestPrimaryGrowth;
	private decimal _latestBenchmarkGrowth;
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
	/// Smoothing length for the synthetic asset base.
	/// </summary>
	public int AssetLength
	{
		get => _assetLength.Value;
		set => _assetLength.Value = value;
	}

	/// <summary>
	/// Lookback period used to normalize the composite signal.
	/// </summary>
	public int NormalizationPeriod
	{
		get => _normalizationPeriod.Value;
		set => _normalizationPeriod.Value = value;
	}

	/// <summary>
	/// Penalty applied to relative asset growth inside the composite score.
	/// </summary>
	public decimal GrowthPenalty
	{
		get => _growthPenalty.Value;
		set => _growthPenalty.Value = value;
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

	public MomentumAssetGrowthStrategy()
	{
		_security2Id = Param(nameof(Security2Id), Paths.HistoryDefaultSecurity2)
			.SetDisplay("Benchmark Security Id", "Identifier of the benchmark security", "General");

		_momentumLength = Param(nameof(MomentumLength), 28)
			.SetRange(5, 150)
			.SetDisplay("Momentum Length", "Momentum lookback period", "Indicators");

		_assetLength = Param(nameof(AssetLength), 8)
			.SetRange(2, 40)
			.SetDisplay("Asset Length", "Smoothing length for the synthetic asset base", "Indicators");

		_normalizationPeriod = Param(nameof(NormalizationPeriod), 24)
			.SetRange(5, 120)
			.SetDisplay("Normalization Period", "Lookback period used to normalize the composite signal", "Indicators");

		_growthPenalty = Param(nameof(GrowthPenalty), 1.8m)
			.SetRange(0.1m, 10m)
			.SetDisplay("Growth Penalty", "Penalty applied to relative asset growth inside the composite score", "Signals");

		_entryThreshold = Param(nameof(EntryThreshold), 1.15m)
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
		_primaryAssetBase = null!;
		_benchmarkAssetBase = null!;
		_signalAverage = null!;
		_signalDeviation = null!;
		_previousPrimaryAssetBase = 0m;
		_previousBenchmarkAssetBase = 0m;
		_latestPrimaryMomentum = 0m;
		_latestBenchmarkMomentum = 0m;
		_latestPrimaryGrowth = 0m;
		_latestBenchmarkGrowth = 0m;
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
		_primaryAssetBase = new ExponentialMovingAverage { Length = AssetLength };
		_benchmarkAssetBase = new ExponentialMovingAverage { Length = AssetLength };
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

		var momentumValue = _primaryMomentum.Process(candle);
		if (momentumValue.IsEmpty || !_primaryMomentum.IsFormed)
			return;

		_latestPrimaryMomentum = momentumValue.ToDecimal();
		_latestPrimaryGrowth = UpdateGrowth(_primaryAssetBase, candle, ref _previousPrimaryAssetBase);
		_primaryUpdated = true;
		TryProcessSignal(candle.OpenTime);
	}

	private void ProcessBenchmarkCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var momentumValue = _benchmarkMomentum.Process(candle);
		if (momentumValue.IsEmpty || !_benchmarkMomentum.IsFormed)
			return;

		_latestBenchmarkMomentum = momentumValue.ToDecimal();
		_latestBenchmarkGrowth = UpdateGrowth(_benchmarkAssetBase, candle, ref _previousBenchmarkAssetBase);
		_benchmarkUpdated = true;
		TryProcessSignal(candle.OpenTime);
	}

	private decimal UpdateGrowth(ExponentialMovingAverage average, ICandleMessage candle, ref decimal previousBase)
	{
		var assetBase = CalculateSyntheticAssetBase(candle);
		var smoothedBase = average.Process(assetBase, candle.OpenTime, true).ToDecimal();

		if (previousBase == 0m)
		{
			previousBase = smoothedBase;
			return 0m;
		}

		var growth = (smoothedBase - previousBase) / Math.Max(Math.Abs(previousBase), 1m);
		previousBase = smoothedBase;
		return growth;
	}

	private static decimal CalculateSyntheticAssetBase(ICandleMessage candle)
	{
		var priceBase = Math.Max(candle.OpenPrice, 1m);
		var rangeRatio = (candle.HighPrice - candle.LowPrice) / priceBase;
		var turnoverProxy = candle.ClosePrice * (1m + (rangeRatio * 6m));
		var balanceProxy = (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m;

		return turnoverProxy + balanceProxy;
	}

	private void TryProcessSignal(DateTime time)
	{
		if (!_primaryUpdated || !_benchmarkUpdated)
			return;

		_primaryUpdated = false;
		_benchmarkUpdated = false;

		if (!_primaryAssetBase.IsFormed || !_benchmarkAssetBase.IsFormed)
			return;

		var signal = (_latestPrimaryMomentum - _latestBenchmarkMomentum) - (GrowthPenalty * (_latestPrimaryGrowth - _latestBenchmarkGrowth));
		var mean = _signalAverage.Process(signal, time, true).ToDecimal();
		var deviation = _signalDeviation.Process(signal, time, true).ToDecimal();

		if (!_signalAverage.IsFormed || !_signalDeviation.IsFormed || deviation <= 0m)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var zScore = (signal - mean) / deviation;
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

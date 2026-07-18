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
/// Smart-factors momentum market strategy that trades the primary instrument when its blended fast and slow momentum outperforms a benchmark market proxy.
/// </summary>
public class SmartFactorsMomentumMarketStrategy : Strategy
{
	private readonly StrategyParam<string> _security2Id;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _normalizationPeriod;
	private readonly StrategyParam<decimal> _entryThreshold;
	private readonly StrategyParam<decimal> _exitThreshold;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private Security _benchmark = null!;
	private RateOfChange _primaryFastMomentum = null!;
	private RateOfChange _primarySlowMomentum = null!;
	private RateOfChange _benchmarkFastMomentum = null!;
	private RateOfChange _benchmarkSlowMomentum = null!;
	private SimpleMovingAverage _spreadAverage = null!;
	private StandardDeviation _spreadDeviation = null!;
	private decimal _latestPrimarySignal;
	private decimal _latestBenchmarkSignal;
	private decimal? _previousZScore;
	private bool _primaryUpdated;
	private bool _benchmarkUpdated;
	private int _cooldownRemaining;

	public string Security2Id
	{
		get => _security2Id.Value;
		set => _security2Id.Value = value;
	}

	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	public int NormalizationPeriod
	{
		get => _normalizationPeriod.Value;
		set => _normalizationPeriod.Value = value;
	}

	public decimal EntryThreshold
	{
		get => _entryThreshold.Value;
		set => _entryThreshold.Value = value;
	}

	public decimal ExitThreshold
	{
		get => _exitThreshold.Value;
		set => _exitThreshold.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public SmartFactorsMomentumMarketStrategy()
	{
		_security2Id = Param(nameof(Security2Id), Paths.HistoryDefaultSecurity2)
			.SetDisplay("Benchmark Security Id", "Identifier of the benchmark market proxy", "General");

		_fastPeriod = Param(nameof(FastPeriod), 8)
			.SetRange(2, 80)
			.SetDisplay("Fast Period", "Fast momentum lookback period", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 24)
			.SetRange(5, 200)
			.SetDisplay("Slow Period", "Slow momentum lookback period", "Indicators");

		_normalizationPeriod = Param(nameof(NormalizationPeriod), 20)
			.SetRange(5, 120)
			.SetDisplay("Normalization Period", "Lookback period used to normalize the blended momentum spread", "Indicators");

		_entryThreshold = Param(nameof(EntryThreshold), 1m)
			.SetRange(0.2m, 5m)
			.SetDisplay("Entry Threshold", "Z-score threshold required to open a position", "Signals");

		_exitThreshold = Param(nameof(ExitThreshold), 0.2m)
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
		_primaryFastMomentum = null!;
		_primarySlowMomentum = null!;
		_benchmarkFastMomentum = null!;
		_benchmarkSlowMomentum = null!;
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
		_primaryFastMomentum = new RateOfChange { Length = FastPeriod };
		_primarySlowMomentum = new RateOfChange { Length = SlowPeriod };
		_benchmarkFastMomentum = new RateOfChange { Length = FastPeriod };
		_benchmarkSlowMomentum = new RateOfChange { Length = SlowPeriod };
		_spreadAverage = new SimpleMovingAverage { Length = NormalizationPeriod };
		_spreadDeviation = new StandardDeviation { Length = NormalizationPeriod };

		var primarySubscription = SubscribeCandles(CandleType, security: Security);
		var benchmarkSubscription = SubscribeCandles(CandleType, security: _benchmark);

		primarySubscription.Bind(ProcessPrimaryCandle).Start();
		benchmarkSubscription.Bind(ProcessBenchmarkCandle).Start();

		StartProtection(new Unit(2, UnitTypes.Percent), new Unit(StopLoss, UnitTypes.Percent));
	}

	private void ProcessPrimaryCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var fast = _primaryFastMomentum.Process(candle);
		var slow = _primarySlowMomentum.Process(candle);
		if (fast.IsEmpty || slow.IsEmpty || !_primaryFastMomentum.IsFormed || !_primarySlowMomentum.IsFormed)
			return;

		_latestPrimarySignal = (fast.ToDecimal() * 0.65m) + (slow.ToDecimal() * 0.35m);
		_primaryUpdated = true;
		TryProcessSpread(candle.OpenTime);
	}

	private void ProcessBenchmarkCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var fast = _benchmarkFastMomentum.Process(candle);
		var slow = _benchmarkSlowMomentum.Process(candle);
		if (fast.IsEmpty || slow.IsEmpty || !_benchmarkFastMomentum.IsFormed || !_benchmarkSlowMomentum.IsFormed)
			return;

		_latestBenchmarkSignal = (fast.ToDecimal() * 0.65m) + (slow.ToDecimal() * 0.35m);
		_benchmarkUpdated = true;
		TryProcessSpread(candle.OpenTime);
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

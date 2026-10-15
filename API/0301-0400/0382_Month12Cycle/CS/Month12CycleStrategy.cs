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
/// 12-month cycle strategy that trades the primary instrument when its 12-month minus 1-month seasonal return outperforms a benchmark.
/// </summary>
public class Month12CycleStrategy : Strategy
{
	private readonly StrategyParam<string> _security2Id;
	private readonly StrategyParam<int> _annualPeriod;
	private readonly StrategyParam<int> _recentPeriod;
	private readonly StrategyParam<int> _normalizationPeriod;
	private readonly StrategyParam<decimal> _entryThreshold;
	private readonly StrategyParam<decimal> _exitThreshold;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private Security _benchmark = null!;
	private RateOfChange _primaryAnnualMomentum = null!;
	private RateOfChange _benchmarkAnnualMomentum = null!;
	private RateOfChange _primaryRecentMomentum = null!;
	private RateOfChange _benchmarkRecentMomentum = null!;
	private SimpleMovingAverage _spreadAverage = null!;
	private StandardDeviation _spreadDeviation = null!;
	private decimal _latestPrimarySignal;
	private decimal _latestBenchmarkSignal;
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
	/// Long lookback period used to approximate the prior 12-month cycle.
	/// </summary>
	public int AnnualPeriod
	{
		get => _annualPeriod.Value;
		set => _annualPeriod.Value = value;
	}

	/// <summary>
	/// Short lookback period used to remove the most recent month.
	/// </summary>
	public int RecentPeriod
	{
		get => _recentPeriod.Value;
		set => _recentPeriod.Value = value;
	}

	/// <summary>
	/// Lookback period used to normalize the seasonal spread.
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

	public Month12CycleStrategy()
	{
		_security2Id = Param(nameof(Security2Id), Paths.HistoryDefaultSecurity2)
			.SetDisplay("Benchmark Security Id", "Identifier of the benchmark security", "General");

		_annualPeriod = Param(nameof(AnnualPeriod), 90)
			.SetRange(30, 400)
			.SetDisplay("Annual Period", "Long lookback period used to approximate the prior 12-month cycle", "Indicators");

		_recentPeriod = Param(nameof(RecentPeriod), 10)
			.SetRange(2, 60)
			.SetDisplay("Recent Period", "Short lookback period used to remove the most recent month", "Indicators");

		_normalizationPeriod = Param(nameof(NormalizationPeriod), 12)
			.SetRange(5, 120)
			.SetDisplay("Normalization Period", "Lookback period used to normalize the seasonal spread", "Indicators");

		_entryThreshold = Param(nameof(EntryThreshold), 0.65m)
			.SetRange(0.1m, 5m)
			.SetDisplay("Entry Threshold", "Z-score threshold required to open a position", "Signals");

		_exitThreshold = Param(nameof(ExitThreshold), 0.15m)
			.SetRange(0m, 2m)
			.SetDisplay("Exit Threshold", "Z-score threshold required to close a position", "Signals");

		_cooldownBars = Param(nameof(CooldownBars), 8)
			.SetRange(0, 120)
			.SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "Risk");

		_stopLoss = Param(nameof(StopLoss), 4m)
			.SetRange(0.5m, 15m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for calculations", "General");
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
		_primaryAnnualMomentum = null!;
		_benchmarkAnnualMomentum = null!;
		_primaryRecentMomentum = null!;
		_benchmarkRecentMomentum = null!;
		_spreadAverage = null!;
		_spreadDeviation = null!;
		_latestPrimarySignal = 0m;
		_latestBenchmarkSignal = 0m;
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
		_primaryAnnualMomentum = new RateOfChange { Length = AnnualPeriod };
		_benchmarkAnnualMomentum = new RateOfChange { Length = AnnualPeriod };
		_primaryRecentMomentum = new RateOfChange { Length = RecentPeriod };
		_benchmarkRecentMomentum = new RateOfChange { Length = RecentPeriod };
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

		var annualMomentum = _primaryAnnualMomentum.Process(candle);
		var recentMomentum = _primaryRecentMomentum.Process(candle);

		if (annualMomentum.IsEmpty || recentMomentum.IsEmpty || !_primaryAnnualMomentum.IsFormed || !_primaryRecentMomentum.IsFormed)
			return;

		_latestPrimarySignal = annualMomentum.ToDecimal() - recentMomentum.ToDecimal();
		_primaryUpdated = true;
		TryProcessSpread(candle.OpenTime);
	}

	private void ProcessBenchmarkCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var annualMomentum = _benchmarkAnnualMomentum.Process(candle);
		var recentMomentum = _benchmarkRecentMomentum.Process(candle);

		if (annualMomentum.IsEmpty || recentMomentum.IsEmpty || !_benchmarkAnnualMomentum.IsFormed || !_benchmarkRecentMomentum.IsFormed)
			return;

		_latestBenchmarkSignal = annualMomentum.ToDecimal() - recentMomentum.ToDecimal();
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

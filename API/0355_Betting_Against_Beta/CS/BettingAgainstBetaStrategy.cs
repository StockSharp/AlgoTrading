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
/// Betting-against-beta factor strategy that trades the primary instrument against a benchmark using its rolling beta regime.
/// </summary>
public class BettingAgainstBetaStrategy : Strategy
{
	private readonly StrategyParam<string> _security2Id;
	private readonly StrategyParam<int> _betaLength;
	private readonly StrategyParam<decimal> _lowBetaThreshold;
	private readonly StrategyParam<decimal> _highBetaThreshold;
	private readonly StrategyParam<decimal> _exitBetaThreshold;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private Security _benchmark = null!;
	private Correlation _correlation = null!;
	private StandardDeviation _primaryDeviation = null!;
	private StandardDeviation _benchmarkDeviation = null!;
	private decimal _latestPrimaryPrice;
	private decimal _latestBenchmarkPrice;
	private decimal _previousPrimaryPrice;
	private decimal _previousBenchmarkPrice;
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
	/// Rolling beta lookback length.
	/// </summary>
	public int BetaLength
	{
		get => _betaLength.Value;
		set => _betaLength.Value = value;
	}

	/// <summary>
	/// Maximum beta required to open a long position.
	/// </summary>
	public decimal LowBetaThreshold
	{
		get => _lowBetaThreshold.Value;
		set => _lowBetaThreshold.Value = value;
	}

	/// <summary>
	/// Minimum beta required to open a short position.
	/// </summary>
	public decimal HighBetaThreshold
	{
		get => _highBetaThreshold.Value;
		set => _highBetaThreshold.Value = value;
	}

	/// <summary>
	/// Neutral beta threshold used to close positions.
	/// </summary>
	public decimal ExitBetaThreshold
	{
		get => _exitBetaThreshold.Value;
		set => _exitBetaThreshold.Value = value;
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
	public BettingAgainstBetaStrategy()
	{
		_security2Id = Param(nameof(Security2Id), Paths.HistoryDefaultSecurity2)
			.SetDisplay("Benchmark Security Id", "Identifier of the benchmark security", "General");

		_betaLength = Param(nameof(BetaLength), 16)
			.SetRange(10, 150)
			.SetDisplay("Beta Length", "Rolling beta lookback length", "Indicators");

		_lowBetaThreshold = Param(nameof(LowBetaThreshold), 0.95m)
			.SetRange(0.2m, 1.2m)
			.SetDisplay("Low Beta Threshold", "Maximum beta required to open a long position", "Signals");

		_highBetaThreshold = Param(nameof(HighBetaThreshold), 1.05m)
			.SetRange(0.8m, 2.5m)
			.SetDisplay("High Beta Threshold", "Minimum beta required to open a short position", "Signals");

		_exitBetaThreshold = Param(nameof(ExitBetaThreshold), 1m)
			.SetRange(0.5m, 1.5m)
			.SetDisplay("Exit Beta Threshold", "Neutral beta threshold used to close positions", "Signals");

		_cooldownBars = Param(nameof(CooldownBars), 8)
			.SetRange(0, 100)
			.SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "Risk");

		_stopLoss = Param(nameof(StopLoss), 2m)
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
		_correlation = null!;
		_primaryDeviation = null!;
		_benchmarkDeviation = null!;
		_latestPrimaryPrice = 0m;
		_latestBenchmarkPrice = 0m;
		_previousPrimaryPrice = 0m;
		_previousBenchmarkPrice = 0m;
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
		_correlation = new Correlation { Length = BetaLength };
		_primaryDeviation = new StandardDeviation { Length = BetaLength };
		_benchmarkDeviation = new StandardDeviation { Length = BetaLength };
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

		_latestPrimaryPrice = candle.ClosePrice;
		_primaryUpdated = true;
		TryProcessBeta(candle.OpenTime);
	}

	private void ProcessBenchmarkCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_latestBenchmarkPrice = candle.ClosePrice;
		_benchmarkUpdated = true;
		TryProcessBeta(candle.OpenTime);
	}

	private void TryProcessBeta(DateTime time)
	{
		if (!_primaryUpdated || !_benchmarkUpdated)
			return;

		_primaryUpdated = false;
		_benchmarkUpdated = false;

		if (_previousPrimaryPrice <= 0m || _previousBenchmarkPrice <= 0m)
		{
			_previousPrimaryPrice = _latestPrimaryPrice;
			_previousBenchmarkPrice = _latestBenchmarkPrice;
			return;
		}

		var primaryReturn = (_latestPrimaryPrice - _previousPrimaryPrice) / Math.Max(_previousPrimaryPrice, 1m);
		var benchmarkReturn = (_latestBenchmarkPrice - _previousBenchmarkPrice) / Math.Max(_previousBenchmarkPrice, 1m);

		_previousPrimaryPrice = _latestPrimaryPrice;
		_previousBenchmarkPrice = _latestBenchmarkPrice;

		var correlationInput = new PairIndicatorValue<decimal>(_correlation, (primaryReturn, benchmarkReturn), time)
		{
			IsFinal = true
		};

		var correlation = _correlation.Process(correlationInput).ToDecimal();
		var primaryDeviation = _primaryDeviation.Process(primaryReturn, time, true).ToDecimal();
		var benchmarkDeviation = _benchmarkDeviation.Process(benchmarkReturn, time, true).ToDecimal();

		if (!_correlation.IsFormed || !_primaryDeviation.IsFormed || !_benchmarkDeviation.IsFormed || benchmarkDeviation <= 0m)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var beta = correlation * (primaryDeviation / benchmarkDeviation);
		var bullishEntry = beta <= LowBetaThreshold;
		var bearishEntry = beta >= HighBetaThreshold;

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
		else if (Position > 0 && beta >= ExitBetaThreshold)
		{
			SellMarket(Position);
			_cooldownRemaining = CooldownBars;
		}
		else if (Position < 0 && beta <= ExitBetaThreshold)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

	}
}

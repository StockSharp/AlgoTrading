using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mean-reversion strategy that uses rolling inter-market correlation as a regime filter.
/// Trades the primary security when a low-correlation regime coincides with short-term divergence versus the secondary security.
/// </summary>
public class CorrelationMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<string> _security2Id;
	private readonly StrategyParam<int> _correlationPeriod;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _deviationThreshold;
	private readonly StrategyParam<decimal> _exitThreshold;
	private readonly StrategyParam<decimal> _divergenceThreshold;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private Security _security2;
	private Correlation _correlation;
	private SimpleMovingAverage _correlationSma;
	private StandardDeviation _correlationStdDev;
	private decimal _latestPrice1;
	private decimal _latestPrice2;
	private decimal _previousPrice1;
	private decimal _previousPrice2;
	private bool _primaryUpdated;
	private bool _secondaryUpdated;
	private int _cooldown;

	/// <summary>
	/// Secondary security identifier.
	/// </summary>
	public string Security2Id
	{
		get => _security2Id.Value;
		set => _security2Id.Value = value;
	}

	/// <summary>
	/// Rolling period for the correlation indicator.
	/// </summary>
	public int CorrelationPeriod
	{
		get => _correlationPeriod.Value;
		set => _correlationPeriod.Value = value;
	}

	/// <summary>
	/// Lookback period for correlation mean and deviation.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Absolute Z-score required to recognize a low-correlation dislocation.
	/// </summary>
	public decimal DeviationThreshold
	{
		get => _deviationThreshold.Value;
		set => _deviationThreshold.Value = value;
	}

	/// <summary>
	/// Exit Z-score threshold as correlation normalizes.
	/// </summary>
	public decimal ExitThreshold
	{
		get => _exitThreshold.Value;
		set => _exitThreshold.Value = value;
	}

	/// <summary>
	/// Minimum one-bar relative performance spread required for entry.
	/// </summary>
	public decimal DivergenceThreshold
	{
		get => _divergenceThreshold.Value;
		set => _divergenceThreshold.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Bars to wait after each order.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Candle type for both instruments.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public CorrelationMeanReversionStrategy()
	{
		_security2Id = Param(nameof(Security2Id), Paths.HistoryDefaultSecurity2)
			.SetDisplay("Second Security Id", "Identifier of the secondary security", "General");

		_correlationPeriod = Param(nameof(CorrelationPeriod), 20)
			.SetRange(5, 100)
			.SetDisplay("Correlation Period", "Rolling period for the correlation indicator", "Indicators");

		_lookbackPeriod = Param(nameof(LookbackPeriod), 30)
			.SetRange(10, 150)
			.SetDisplay("Lookback Period", "Lookback period for correlation statistics", "Indicators");

		_deviationThreshold = Param(nameof(DeviationThreshold), 1.1m)
			.SetRange(0.25m, 3m)
			.SetDisplay("Deviation Threshold", "Absolute Z-score required for entry", "Signals");

		_exitThreshold = Param(nameof(ExitThreshold), 0.15m)
			.SetRange(0m, 2m)
			.SetDisplay("Exit Threshold", "Z-score threshold used for exit", "Signals");

		_divergenceThreshold = Param(nameof(DivergenceThreshold), 0.003m)
			.SetRange(0.0005m, 0.05m)
			.SetDisplay("Divergence Threshold", "Minimum one-bar divergence between the two instruments", "Signals");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetRange(0.5m, 10m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_cooldownBars = Param(nameof(CooldownBars), 120)
			.SetRange(1, 500)
			.SetDisplay("Cooldown Bars", "Bars to wait after each order", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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

		_security2 = null;
		_correlation = null;
		_correlationSma = null;
		_correlationStdDev = null;
		_latestPrice1 = 0m;
		_latestPrice2 = 0m;
		_previousPrice1 = 0m;
		_previousPrice2 = 0m;
		_primaryUpdated = false;
		_secondaryUpdated = false;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (Security == null)
			throw new InvalidOperationException("Primary security is not specified.");

		if (Security2Id.IsEmpty())
			throw new InvalidOperationException("Secondary security identifier is not specified.");

		_security2 = this.LookupById(Security2Id) ?? new Security { Id = Security2Id };
		_correlation = new Correlation { Length = CorrelationPeriod };
		_correlationSma = new SimpleMovingAverage { Length = LookbackPeriod };
		_correlationStdDev = new StandardDeviation { Length = LookbackPeriod };
		_cooldown = 0;

		var primarySubscription = SubscribeCandles(CandleType, security: Security);
		var secondarySubscription = SubscribeCandles(CandleType, security: _security2);

		primarySubscription
			.Bind(ProcessPrimaryCandle)
			.Start();

		secondarySubscription
			.Bind(ProcessSecondaryCandle)
			.Start();

		var area = CreateChartArea();

		if (area != null)
		{
			DrawCandles(area, primarySubscription);
			DrawCandles(area, secondarySubscription);
			DrawOwnTrades(area);
		}

		StartProtection(new Unit(0, UnitTypes.Absolute), new Unit(StopLossPercent, UnitTypes.Percent), false);
	}

	private void ProcessPrimaryCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_latestPrice1 = candle.ClosePrice;
		_primaryUpdated = true;

		TryProcessPair(candle.OpenTime);
	}

	private void ProcessSecondaryCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_latestPrice2 = candle.ClosePrice;
		_secondaryUpdated = true;

		TryProcessPair(candle.OpenTime);
	}

	private void TryProcessPair(DateTimeOffset time)
	{
		if (!_primaryUpdated || !_secondaryUpdated)
			return;

		_primaryUpdated = false;
		_secondaryUpdated = false;

		if (_latestPrice1 <= 0 || _latestPrice2 <= 0)
			return;

		if (_previousPrice1 <= 0 || _previousPrice2 <= 0)
		{
			_previousPrice1 = _latestPrice1;
			_previousPrice2 = _latestPrice2;
			return;
		}

		var correlationValue = _correlation.Process((_latestPrice1, _latestPrice2), time.UtcDateTime, true).ToDecimal();
		var averageCorrelation = _correlationSma.Process(correlationValue, time.UtcDateTime, true).ToDecimal();
		var stdCorrelation = _correlationStdDev.Process(correlationValue, time.UtcDateTime, true).ToDecimal();

		var primaryReturn = (_latestPrice1 - _previousPrice1) / _previousPrice1;
		var secondaryReturn = (_latestPrice2 - _previousPrice2) / _previousPrice2;
		var divergence = primaryReturn - secondaryReturn;

		_previousPrice1 = _latestPrice1;
		_previousPrice2 = _latestPrice2;

		if (!_correlation.IsFormed || !_correlationSma.IsFormed || !_correlationStdDev.IsFormed)
			return;

		if (ProcessState != ProcessStates.Started)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		if (stdCorrelation <= 0)
			return;

		var zScore = (correlationValue - averageCorrelation) / stdCorrelation;
		var isLowCorrelation = zScore <= -DeviationThreshold;

		if (Position == 0)
		{
			if (!isLowCorrelation)
				return;

			if (divergence <= -DivergenceThreshold)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (divergence >= DivergenceThreshold)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}

			return;
		}

		var correlationRecovered = zScore >= -ExitThreshold;

		if (Position > 0 && (correlationRecovered || divergence >= -DivergenceThreshold * 0.5m))
		{
			SellMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && (correlationRecovered || divergence <= DivergenceThreshold * 0.5m))
		{
			BuyMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}
	}
}

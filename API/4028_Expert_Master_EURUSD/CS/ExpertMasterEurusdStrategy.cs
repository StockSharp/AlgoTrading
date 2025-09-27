using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD-based reversal breakout strategy converted from the Expert Master EURUSD MQL4 expert.
/// It observes four-bar patterns on the MACD main and signal lines to detect momentum shifts and enter trades.
/// </summary>
public class ExpertMasterEurusdStrategy : Strategy
{
	private readonly StrategyParam<int> _trailingPoints;
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<decimal> _upperMacdThreshold;
	private readonly StrategyParam<decimal> _lowerMacdThreshold;
	private readonly StrategyParam<decimal> _shortCurrentThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private decimal? _macdMain0;
	private decimal? _macdMain1;
	private decimal? _macdMain2;
	private decimal? _macdMain3;
	private decimal? _signal0;
	private decimal? _signal1;
	private decimal? _signal2;
	private decimal? _signal3;
	private decimal _longEntryPrice;
	private decimal _shortEntryPrice;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;

	/// <summary>
	/// Trailing stop distance expressed in price points.
	/// </summary>
	public int TrailingPoints
	{
		get => _trailingPoints.Value;
		set => _trailingPoints.Value = value;
	}

	/// <summary>
	/// Fallback trade volume used when risk sizing returns zero.
	/// </summary>
	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	/// <summary>
	/// Percentage of portfolio value used to size positions.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Fast EMA period for the MACD main line.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period for the MACD main line.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal line period for the MACD indicator.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Positive MACD threshold required for entries.
	/// </summary>
	public decimal UpperMacdThreshold
	{
		get => _upperMacdThreshold.Value;
		set => _upperMacdThreshold.Value = value;
	}

	/// <summary>
	/// Negative MACD threshold used when building long signals.
	/// </summary>
	public decimal LowerMacdThreshold
	{
		get => _lowerMacdThreshold.Value;
		set => _lowerMacdThreshold.Value = value;
	}

	/// <summary>
	/// Negative MACD threshold applied to the current value for short entries.
	/// </summary>
	public decimal ShortCurrentThreshold
	{
		get => _shortCurrentThreshold.Value;
		set => _shortCurrentThreshold.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public ExpertMasterEurusdStrategy()
	{
		_trailingPoints = Param(nameof(TrailingPoints), 25)
			.SetDisplay("Trailing", "Trailing stop distance in points", "Risk")
			.SetCanOptimize(true)
			.SetRange(0, 1000);
		_fixedVolume = Param(nameof(FixedVolume), 1m)
			.SetDisplay("Fixed Volume", "Fallback trade volume", "Risk")
			.SetCanOptimize(true)
			.SetRange(0.01m, 100m);
		_riskPercent = Param(nameof(RiskPercent), 0.01m)
			.SetDisplay("Risk Percent", "Portfolio percentage used to size positions", "Risk")
			.SetRange(0m, 100m);
		_macdFastPeriod = Param(nameof(MacdFastPeriod), 5)
			.SetDisplay("MACD Fast", "Fast EMA period", "Indicators")
			.SetGreaterThanZero();
		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 15)
			.SetDisplay("MACD Slow", "Slow EMA period", "Indicators")
			.SetGreaterThanZero();
		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 3)
			.SetDisplay("MACD Signal", "Signal EMA period", "Indicators")
			.SetGreaterThanZero();
		_upperMacdThreshold = Param(nameof(UpperMacdThreshold), 0.00020m)
			.SetDisplay("Upper MACD", "Positive MACD threshold", "Logic");
		_lowerMacdThreshold = Param(nameof(LowerMacdThreshold), -0.00020m)
			.SetDisplay("Lower MACD", "Negative MACD threshold for longs", "Logic");
		_shortCurrentThreshold = Param(nameof(ShortCurrentThreshold), -0.00035m)
			.SetDisplay("Short MACD", "Negative MACD threshold for shorts", "Logic");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for MACD", "Data");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		ResetState();
		base.OnReseted();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Fast = MacdFastPeriod,
			Slow = MacdSlowPeriod,
			Signal = MacdSignalPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (indicatorValue is not MovingAverageConvergenceDivergenceSignalValue macdValue)
			return;

		if (macdValue.Macd is not decimal macdMain || macdValue.Signal is not decimal macdSignal)
			return;

		// Cache MACD main and signal values to reproduce the MQL shift logic.
		ShiftBuffer(ref _macdMain3, ref _macdMain2, ref _macdMain1, ref _macdMain0, macdMain);
		ShiftBuffer(ref _signal3, ref _signal2, ref _signal1, ref _signal0, macdSignal);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (ManageTrailing(candle))
			return;

		if (_macdMain3 is null || _macdMain2 is null || _macdMain1 is null || _macdMain0 is null ||
			_signal3 is null || _signal2 is null || _signal1 is null || _signal0 is null)
		{
			return;
		}

		var mac1 = _macdMain0.Value;
		var mac2 = _macdMain1.Value;
		var mac3 = _macdMain2.Value;
		var mac4 = _macdMain3.Value;
		var sig1 = _signal0.Value;
		var sig2 = _signal1.Value;
		var sig3 = _signal2.Value;
		var sig4 = _signal3.Value;

		// Long signal replicates the original MACD pattern.
		var longSignal = sig4 > sig3 &&
			sig3 > sig2 &&
			sig2 < sig1 &&
			mac4 > mac3 &&
			mac3 < mac2 &&
			mac2 < mac1 &&
			mac2 < LowerMacdThreshold &&
			mac4 < 0m &&
			mac1 > UpperMacdThreshold;

		// Short signal mirrors the MQL condition set.
		var shortSignal = sig4 < sig3 &&
			sig3 < sig2 &&
			sig2 > sig1 &&
			mac4 < mac3 &&
			mac3 > mac2 &&
			mac2 > mac1 &&
			mac2 > UpperMacdThreshold &&
			mac4 > 0m &&
			mac1 < ShortCurrentThreshold;

		if (Position == 0)
		{
			if (longSignal)
			{
				var volume = GetTradeVolume();
				if (volume > 0m)
				{
					BuyMarket(volume);
					_longEntryPrice = candle.ClosePrice;
					_longTrailingStop = null;
					ResetShort();
				}
			}
			else if (shortSignal)
			{
				var volume = GetTradeVolume();
				if (volume > 0m)
				{
					SellMarket(volume);
					_shortEntryPrice = candle.ClosePrice;
					_shortTrailingStop = null;
					ResetLong();
				}
			}
		}
		else if (Position > 0)
		{
			if (mac1 < mac2)
			{
				SellMarket(Position);
				ResetLong();
			}
		}
		else if (Position < 0)
		{
			if (mac1 > mac2)
			{
				BuyMarket(-Position);
				ResetShort();
			}
		}
	}

	private void ShiftBuffer(ref decimal? oldest, ref decimal? older, ref decimal? previous, ref decimal? current, decimal value)
	{
		oldest = older;
		older = previous;
		previous = current;
		current = value;
	}

	private bool ManageTrailing(ICandleMessage candle)
	{
		var trailingDistance = GetPriceByPoints(TrailingPoints);
		if (TrailingPoints <= 0 || trailingDistance <= 0m)
			return false;

		if (Position > 0 && _longEntryPrice > 0m)
		{
			if (candle.HighPrice >= _longEntryPrice + trailingDistance)
			{
				var newStop = candle.ClosePrice - trailingDistance;
				if (!_longTrailingStop.HasValue || newStop > _longTrailingStop.Value)
					_longTrailingStop = newStop;
			}

			if (_longTrailingStop.HasValue && candle.LowPrice <= _longTrailingStop.Value)
			{
				SellMarket(Position);
				ResetLong();
				return true;
			}
		}
		else if (Position < 0 && _shortEntryPrice > 0m)
		{
			if (candle.LowPrice <= _shortEntryPrice - trailingDistance)
			{
				var newStop = candle.ClosePrice + trailingDistance;
				if (!_shortTrailingStop.HasValue || newStop < _shortTrailingStop.Value)
					_shortTrailingStop = newStop;
			}

			if (_shortTrailingStop.HasValue && candle.HighPrice >= _shortTrailingStop.Value)
			{
				BuyMarket(-Position);
				ResetShort();
				return true;
			}
		}

		return false;
	}

	private decimal GetPriceByPoints(int points)
	{
		if (points <= 0)
			return 0m;

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			step = 1m;

		return points * step;
	}

	private decimal GetTradeVolume()
	{
		var volume = FixedVolume;

		if (RiskPercent > 0m && Portfolio is not null)
		{
			var equity = Portfolio.CurrentValue;
			if (equity > 0m)
			{
				var riskVolume = equity * (RiskPercent / 100m);
				volume = Math.Round(riskVolume, 1, MidpointRounding.AwayFromZero);
			}
		}

		if (volume <= 0m)
			volume = FixedVolume;

		return Math.Max(volume, 0m);
	}

	private void ResetLong()
	{
		_longEntryPrice = 0m;
		_longTrailingStop = null;
	}

	private void ResetShort()
	{
		_shortEntryPrice = 0m;
		_shortTrailingStop = null;
	}

	private void ResetState()
	{
		_macdMain0 = null;
		_macdMain1 = null;
		_macdMain2 = null;
		_macdMain3 = null;
		_signal0 = null;
		_signal1 = null;
		_signal2 = null;
		_signal3 = null;
		ResetLong();
		ResetShort();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		ResetState();
		base.OnStopped();
	}
}

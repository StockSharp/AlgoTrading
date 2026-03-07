using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volatility pivot strategy using ATR-based trailing stop.
/// Follows trend by flipping long/short when price crosses the ATR-based pivot line.
/// </summary>
public class VolatilityPivotStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _emaPeriod;

	private AverageTrueRange _atr;
	private ExponentialMovingAverage _ema;

	private decimal _pivotStop;
	private decimal _prevClose;
	private bool _isLong;
	private bool _initialized;
	private int _cooldown;

	/// <summary>
	/// ATR calculation period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier applied to ATR for pivot distance.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// EMA period for trend confirmation.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public VolatilityPivotStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR calculation period", "Indicator");

		_atrMultiplier = Param(nameof(AtrMultiplier), 5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "Multiplier for pivot distance", "Indicator");

		_emaPeriod = Param(nameof(EmaPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA trend filter period", "Indicator");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, TimeSpan.FromMinutes(5).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_atr = null;
		_ema = null;
		_pivotStop = 0;
		_prevClose = 0;
		_isLong = true;
		_initialized = false;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_atr = new AverageTrueRange { Length = AtrPeriod };
		_ema = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(TimeSpan.FromMinutes(5).TimeFrame());
		subscription.Bind(_atr, _ema, ProcessCandle);
		subscription.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_atr.IsFormed || !_ema.IsFormed)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevClose = candle.ClosePrice;
			return;
		}

		var close = candle.ClosePrice;
		var delta = atrValue * AtrMultiplier;

		if (!_initialized)
		{
			_pivotStop = close > emaValue ? close - delta : close + delta;
			_isLong = close > emaValue;
			_initialized = true;
			_prevClose = close;
			return;
		}

		// Update pivot stop
		if (_isLong)
		{
			var newStop = close - delta;
			if (newStop > _pivotStop)
				_pivotStop = newStop;

			// Check for reversal
			if (close < _pivotStop)
			{
				_isLong = false;
				_pivotStop = close + delta;

				if (Position > 0)
					SellMarket();

				SellMarket();
				_cooldown = 60;
			}
		}
		else
		{
			var newStop = close + delta;
			if (newStop < _pivotStop)
				_pivotStop = newStop;

			// Check for reversal
			if (close > _pivotStop)
			{
				_isLong = true;
				_pivotStop = close - delta;

				if (Position < 0)
					BuyMarket();

				BuyMarket();
				_cooldown = 60;
			}
		}

		_prevClose = close;
	}
}

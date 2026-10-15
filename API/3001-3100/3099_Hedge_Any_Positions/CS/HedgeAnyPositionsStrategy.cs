using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hedge Any Positions strategy using ATR-based mean reversion.
/// Enters long when price drops below EMA by ATR threshold, enters short on rally above.
/// Uses stop-loss and take-profit for risk management.
/// </summary>
public class HedgeAnyPositionsStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;

	private ExponentialMovingAverage _ema;
	private AverageTrueRange _atr;

	private decimal _entryPrice;
	private int _cooldown;

	/// <summary>
	/// EMA period.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier applied to ATR for entry threshold.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="HedgeAnyPositionsStrategy"/> class.
	/// </summary>
	public HedgeAnyPositionsStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period for mean price", "Indicator");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR calculation period", "Indicator");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "Multiplier for entry distance", "Indicator");

		_stopLossPoints = Param(nameof(StopLossPoints), 200)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop-loss distance in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 300)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take-profit distance in price steps", "Risk");
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

		_ema = null;
		_atr = null;
		_entryPrice = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ema = new ExponentialMovingAverage { Length = EmaPeriod };
		_atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(TimeSpan.FromMinutes(5).TimeFrame());
		subscription.Bind(_ema, _atr, ProcessCandle);
		subscription.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ema.IsFormed || !_atr.IsFormed)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		var close = candle.ClosePrice;
		var step = Security?.PriceStep ?? 1m;
		var threshold = atrValue * AtrMultiplier;

		// Check SL/TP
		if (Position > 0 && _entryPrice > 0)
		{
			if (StopLossPoints > 0 && close <= _entryPrice - StopLossPoints * step)
			{
				SellMarket();
				_entryPrice = 0;
				_cooldown = 80;
				return;
			}

			if (TakeProfitPoints > 0 && close >= _entryPrice + TakeProfitPoints * step)
			{
				SellMarket();
				_entryPrice = 0;
				_cooldown = 80;
				return;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			if (StopLossPoints > 0 && close >= _entryPrice + StopLossPoints * step)
			{
				BuyMarket();
				_entryPrice = 0;
				_cooldown = 80;
				return;
			}

			if (TakeProfitPoints > 0 && close <= _entryPrice - TakeProfitPoints * step)
			{
				BuyMarket();
				_entryPrice = 0;
				_cooldown = 80;
				return;
			}
		}

		// Mean reversion: buy when price drops below EMA by threshold
		if (close < emaValue - threshold && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();

			BuyMarket();
			_entryPrice = close;
			_cooldown = 80;
		}
		// Mean reversion: sell when price rallies above EMA by threshold
		else if (close > emaValue + threshold && Position >= 0)
		{
			if (Position > 0)
				SellMarket();

			SellMarket();
			_entryPrice = close;
			_cooldown = 80;
		}
	}
}

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Opens positions based on SMA trend and closes when floating PnL reaches a profit threshold.
/// Simplified from the "Close all positions" utility expert.
/// </summary>
public class CloseAllPositionsStrategy : Strategy
{
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;

	private SimpleMovingAverage _sma;

	private decimal _entryPrice;
	private decimal _prevSma;
	private int _cooldown;

	/// <summary>
	/// SMA period for entry signals.
	/// </summary>
	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
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
	/// Initializes strategy parameters.
	/// </summary>
	public CloseAllPositionsStrategy()
	{
		_smaPeriod = Param(nameof(SmaPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "Moving average period for entry signals", "Indicators");

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

		_sma = null;
		_entryPrice = 0;
		_prevSma = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_sma = new SimpleMovingAverage { Length = SmaPeriod };

		var subscription = SubscribeCandles(TimeSpan.FromMinutes(5).TimeFrame());
		subscription.Bind(_sma, ProcessCandle);
		subscription.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_sma.IsFormed)
		{
			_prevSma = smaValue;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevSma = smaValue;
			return;
		}

		var close = candle.ClosePrice;
		var step = Security?.PriceStep ?? 1m;

		// Check SL/TP
		if (Position > 0 && _entryPrice > 0)
		{
			if (StopLossPoints > 0 && close <= _entryPrice - StopLossPoints * step)
			{
				SellMarket();
				_entryPrice = 0;
				_cooldown = 60;
				_prevSma = smaValue;
				return;
			}

			if (TakeProfitPoints > 0 && close >= _entryPrice + TakeProfitPoints * step)
			{
				SellMarket();
				_entryPrice = 0;
				_cooldown = 60;
				_prevSma = smaValue;
				return;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			if (StopLossPoints > 0 && close >= _entryPrice + StopLossPoints * step)
			{
				BuyMarket();
				_entryPrice = 0;
				_cooldown = 60;
				_prevSma = smaValue;
				return;
			}

			if (TakeProfitPoints > 0 && close <= _entryPrice - TakeProfitPoints * step)
			{
				BuyMarket();
				_entryPrice = 0;
				_cooldown = 60;
				_prevSma = smaValue;
				return;
			}
		}

		// Crossover entry: price crosses above SMA -> buy
		if (close > smaValue && candle.OpenPrice <= _prevSma && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();

			BuyMarket();
			_entryPrice = close;
			_cooldown = 60;
		}
		// Price crosses below SMA -> sell
		else if (close < smaValue && candle.OpenPrice >= _prevSma && Position >= 0)
		{
			if (Position > 0)
				SellMarket();

			SellMarket();
			_entryPrice = close;
			_cooldown = 60;
		}

		_prevSma = smaValue;
	}
}

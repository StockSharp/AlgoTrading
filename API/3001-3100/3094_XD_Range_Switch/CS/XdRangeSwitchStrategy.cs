using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// XD-RangeSwitch strategy using Highest/Lowest channel breakouts.
/// Buys on breakout above channel high, sells on breakout below channel low.
/// Uses stop-loss and take-profit for risk management.
/// </summary>
public class XdRangeSwitchStrategy : Strategy
{
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;

	private Highest _highest;
	private Lowest _lowest;

	private decimal _prevHigh;
	private decimal _prevLow;
	private decimal _entryPrice;
	private int _cooldown;

	/// <summary>
	/// Channel lookback period.
	/// </summary>
	public int ChannelPeriod
	{
		get => _channelPeriod.Value;
		set => _channelPeriod.Value = value;
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
	public XdRangeSwitchStrategy()
	{
		_channelPeriod = Param(nameof(ChannelPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("Channel Period", "Lookback for highest/lowest channel", "Indicator");

		_stopLossPoints = Param(nameof(StopLossPoints), 200)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop-loss distance in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 400)
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

		_highest = null;
		_lowest = null;
		_prevHigh = 0;
		_prevLow = 0;
		_entryPrice = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highest = new Highest { Length = ChannelPeriod };
		_lowest = new Lowest { Length = ChannelPeriod };

		var subscription = SubscribeCandles(TimeSpan.FromMinutes(5).TimeFrame());
		subscription.Bind(_highest, _lowest, ProcessCandle);
		subscription.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal highValue, decimal lowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_highest.IsFormed || !_lowest.IsFormed)
		{
			_prevHigh = highValue;
			_prevLow = lowValue;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevHigh = highValue;
			_prevLow = lowValue;
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
				_cooldown = 30;
				_prevHigh = highValue;
				_prevLow = lowValue;
				return;
			}

			if (TakeProfitPoints > 0 && close >= _entryPrice + TakeProfitPoints * step)
			{
				SellMarket();
				_entryPrice = 0;
				_cooldown = 30;
				_prevHigh = highValue;
				_prevLow = lowValue;
				return;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			if (StopLossPoints > 0 && close >= _entryPrice + StopLossPoints * step)
			{
				BuyMarket();
				_entryPrice = 0;
				_cooldown = 30;
				_prevHigh = highValue;
				_prevLow = lowValue;
				return;
			}

			if (TakeProfitPoints > 0 && close <= _entryPrice - TakeProfitPoints * step)
			{
				BuyMarket();
				_entryPrice = 0;
				_cooldown = 30;
				_prevHigh = highValue;
				_prevLow = lowValue;
				return;
			}
		}

		// Breakout above previous channel high
		if (close > _prevHigh && _prevHigh > 0 && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();

			BuyMarket();
			_entryPrice = close;
			_cooldown = 30;
		}
		// Breakout below previous channel low
		else if (close < _prevLow && _prevLow > 0 && Position >= 0)
		{
			if (Position > 0)
				SellMarket();

			SellMarket();
			_entryPrice = close;
			_cooldown = 30;
		}

		_prevHigh = highValue;
		_prevLow = lowValue;
	}
}

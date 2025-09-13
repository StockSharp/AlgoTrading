using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy that places stop orders above and below the day's range at a specific time.
/// Pending orders include optional stop loss, take profit, break-even and trailing stop management.
/// </summary>
public class BreakdownLevelDayStrategy : Strategy
{
	private readonly StrategyParam<TimeSpan> _orderTime;
	private readonly StrategyParam<decimal> _delta;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _breakEven;
	private readonly StrategyParam<decimal> _trailing;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _dayHigh;
	private decimal _dayLow;
	private DateTime _currentDay;
	private bool _ordersPlaced;
	private Order? _buyStopOrder;
	private Order? _sellStopOrder;
	private Order? _stopOrder;
	private Order? _profitOrder;
	private decimal _entryPrice;
	private bool _isLong;
	private decimal _stopPrice;
	private decimal _tickSize;

	/// <summary>
	/// Time of day to place pending orders.
	/// </summary>
	public TimeSpan OrderTime
	{
		get => _orderTime.Value;
		set => _orderTime.Value = value;
	}

	/// <summary>
	/// Offset from high/low in ticks.
	/// </summary>
	public decimal Delta
	{
		get => _delta.Value;
		set => _delta.Value = value;
	}

	/// <summary>
	/// Stop loss distance in ticks.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit distance in ticks.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Move stop to entry after this profit in ticks.
	/// </summary>
	public decimal BreakEven
	{
		get => _breakEven.Value;
		set => _breakEven.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in ticks.
	/// </summary>
	public decimal Trailing
	{
		get => _trailing.Value;
		set => _trailing.Value = value;
	}

	/// <summary>
	/// The type of candles to use for time and range calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public BreakdownLevelDayStrategy()
	{
		_orderTime = Param(nameof(OrderTime), new TimeSpan(7, 32, 0))
			.SetDisplay("Order Time", "Time of day to place orders", "General");

		_delta = Param(nameof(Delta), 6m)
			.SetGreaterThanZero()
			.SetDisplay("Delta", "Offset from range boundaries in ticks", "Orders");

		_stopLoss = Param(nameof(StopLoss), 120m)
			.SetDisplay("Stop Loss", "Stop loss in ticks", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 90m)
			.SetDisplay("Take Profit", "Take profit in ticks", "Risk");

		_breakEven = Param(nameof(BreakEven), 0m)
			.SetDisplay("Break Even", "Move stop to entry after profit in ticks", "Risk");

		_trailing = Param(nameof(Trailing), 0m)
			.SetDisplay("Trailing", "Trailing stop distance in ticks", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculations", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_dayHigh = 0m;
		_dayLow = 0m;
		_currentDay = default;
		_ordersPlaced = false;
		_buyStopOrder = null;
		_sellStopOrder = null;
		_stopOrder = null;
		_profitOrder = null;
		_entryPrice = 0m;
		_isLong = false;
		_stopPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_tickSize = Security?.PriceStep ?? 1m;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var date = candle.OpenTime.UtcDateTime.Date;

		if (date != _currentDay)
		{
			ResetDay(candle);
		}
		else
		{
			if (candle.HighPrice > _dayHigh)
				_dayHigh = candle.HighPrice;
			if (candle.LowPrice < _dayLow)
				_dayLow = candle.LowPrice;
		}

		ManagePosition(candle);

		if (!_ordersPlaced && candle.OpenTime.TimeOfDay >= OrderTime)
			PlacePendingOrders();
	}

	private void ResetDay(ICandleMessage candle)
	{
		_currentDay = candle.OpenTime.UtcDateTime.Date;
		_dayHigh = candle.HighPrice;
		_dayLow = candle.LowPrice;
		_ordersPlaced = false;
		_entryPrice = 0m;
		_isLong = false;
		_stopPrice = 0m;

		if (_buyStopOrder != null)
			CancelOrder(_buyStopOrder);
		if (_sellStopOrder != null)
			CancelOrder(_sellStopOrder);
		if (_stopOrder != null)
			CancelOrder(_stopOrder);
		if (_profitOrder != null)
			CancelOrder(_profitOrder);

		_buyStopOrder = null;
		_sellStopOrder = null;
		_stopOrder = null;
		_profitOrder = null;
	}

	private void PlacePendingOrders()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var upPrice = _dayHigh + Delta * _tickSize;
		var downPrice = _dayLow - Delta * _tickSize;

		if (Position <= 0)
			_buyStopOrder = BuyStop(Volume, upPrice);

		if (Position >= 0)
			_sellStopOrder = SellStop(Volume, downPrice);

		_ordersPlaced = true;
	}

	private void ManagePosition(ICandleMessage candle)
	{
		if (Position == 0)
		{
			_entryPrice = 0m;
			_stopPrice = 0m;

			if (_stopOrder != null)
			{
				CancelOrder(_stopOrder);
				_stopOrder = null;
			}

			if (_profitOrder != null)
			{
				CancelOrder(_profitOrder);
				_profitOrder = null;
			}

			return;
		}

		if (_entryPrice == 0m)
		{
			_entryPrice = candle.ClosePrice;
			_isLong = Position > 0;

			if (_isLong)
			{
				if (StopLoss > 0m)
				{
					_stopPrice = _entryPrice - StopLoss * _tickSize;
					_stopOrder = SellStop(Math.Abs(Position), _stopPrice);
				}

				if (TakeProfit > 0m)
				{
					var tp = _entryPrice + TakeProfit * _tickSize;
					_profitOrder = SellLimit(Math.Abs(Position), tp);
				}

				if (_sellStopOrder != null)
				{
					CancelOrder(_sellStopOrder);
					_sellStopOrder = null;
				}
			}
			else
			{
				if (StopLoss > 0m)
				{
					_stopPrice = _entryPrice + StopLoss * _tickSize;
					_stopOrder = BuyStop(Math.Abs(Position), _stopPrice);
				}

				if (TakeProfit > 0m)
				{
					var tp = _entryPrice - TakeProfit * _tickSize;
					_profitOrder = BuyLimit(Math.Abs(Position), tp);
				}

				if (_buyStopOrder != null)
				{
					CancelOrder(_buyStopOrder);
					_buyStopOrder = null;
				}
			}
		}

		var currentPrice = candle.ClosePrice;

		if (_isLong)
		{
			if (BreakEven > 0m && currentPrice - _entryPrice >= BreakEven * _tickSize && _stopPrice < _entryPrice)
			{
				_stopPrice = _entryPrice;
				if (_stopOrder != null)
					CancelOrder(_stopOrder);
				_stopOrder = SellStop(Math.Abs(Position), _stopPrice);
			}

			if (Trailing > 0m)
			{
				var newStop = currentPrice - Trailing * _tickSize;
				if (newStop > _stopPrice)
				{
					_stopPrice = newStop;
					if (_stopOrder != null)
						CancelOrder(_stopOrder);
					_stopOrder = SellStop(Math.Abs(Position), _stopPrice);
				}
			}
		}
		else
		{
			if (BreakEven > 0m && _entryPrice - currentPrice >= BreakEven * _tickSize && _stopPrice > _entryPrice)
			{
				_stopPrice = _entryPrice;
				if (_stopOrder != null)
					CancelOrder(_stopOrder);
				_stopOrder = BuyStop(Math.Abs(Position), _stopPrice);
			}

			if (Trailing > 0m)
			{
				var newStop = currentPrice + Trailing * _tickSize;
				if (newStop < _stopPrice || _stopPrice == 0m)
				{
					_stopPrice = newStop;
					if (_stopOrder != null)
						CancelOrder(_stopOrder);
					_stopOrder = BuyStop(Math.Abs(Position), _stopPrice);
				}
			}
		}
	}
}

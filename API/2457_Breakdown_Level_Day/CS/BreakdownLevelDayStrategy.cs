namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Breakout strategy that trades previous day's high/low levels.
/// </summary>
public class BreakdownLevelDayStrategy : Strategy
{
	private readonly StrategyParam<TimeSpan> _orderTime;
	private readonly StrategyParam<int> _delta;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _noLoss;
	private readonly StrategyParam<int> _trailing;
	private readonly StrategyParam<decimal> _volume;

	private decimal _prevHigh;
	private decimal _prevLow;
	private decimal _buyLevel;
	private decimal _sellLevel;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	private DateTime _tradeDay;
	private bool _levelsPlaced;

	public TimeSpan OrderTime
	{
		get => _orderTime.Value;
		set => _orderTime.Value = value;
	}

	public int Delta
	{
		get => _delta.Value;
		set => _delta.Value = value;
	}

	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	public int NoLoss
	{
		get => _noLoss.Value;
		set => _noLoss.Value = value;
	}

	public int Trailing
	{
		get => _trailing.Value;
		set => _trailing.Value = value;
	}

	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	public BreakdownLevelDayStrategy()
	{
		_orderTime = Param(nameof(OrderTime), TimeSpan.Zero)
			.SetDisplay("Order Time", "Time to place breakout levels", "General");

		_delta = Param(nameof(Delta), 6)
			.SetDisplay("Delta (points)", "Shift from previous high/low in points", "Parameters");

		_stopLoss = Param(nameof(StopLoss), 120)
			.SetDisplay("Stop Loss (points)", "Protective stop in points", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 90)
			.SetDisplay("Take Profit (points)", "Take profit in points", "Risk");

		_noLoss = Param(nameof(NoLoss), 0)
			.SetDisplay("Break-even (points)", "Move stop after profit in points", "Risk");

		_trailing = Param(nameof(Trailing), 0)
			.SetDisplay("Trailing (points)", "Trailing stop distance in points", "Risk");

		_volume = Param(nameof(Volume), 1m)
			.SetDisplay("Volume", "Order volume", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> new[] { (Security, DataType.Ticks), (Security, TimeSpan.FromDays(1).TimeFrame()) };

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevHigh = _prevLow = _buyLevel = _sellLevel = 0m;
		_entryPrice = _stopPrice = _takePrice = 0m;
		_tradeDay = default;
		_levelsPlaced = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeTrades().Bind(ProcessTrade).Start();
		SubscribeCandles(TimeSpan.FromDays(1).TimeFrame()).Bind(ProcessDaily).Start();
	}

	private void ProcessDaily(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_tradeDay = candle.OpenTime.Date.AddDays(1);
		_levelsPlaced = false;
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		var price = trade.TradePrice ?? 0m;
		var time = trade.ServerTime;

		if (time.Date > _tradeDay)
			_levelsPlaced = false;

		if (!_levelsPlaced && time.Date == _tradeDay && time.TimeOfDay >= OrderTime)
		{
			var step = Security.PriceStep ?? 1m;
			_buyLevel = _prevHigh + Delta * step;
			_sellLevel = _prevLow - Delta * step;
			_levelsPlaced = true;
		}

		if (Position == 0)
		{
			if (!_levelsPlaced)
				return;

			if (price >= _buyLevel)
			{
				BuyMarket(Volume);
				_entryPrice = price;
				SetInitialStops(true);
				_levelsPlaced = false;
			}
			else if (price <= _sellLevel)
			{
				SellMarket(Volume);
				_entryPrice = price;
				SetInitialStops(false);
				_levelsPlaced = false;
			}
		}
		else
		{
			ManagePosition(price);
		}
	}

	private void SetInitialStops(bool isLong)
	{
		var step = Security.PriceStep ?? 1m;

		_stopPrice = StopLoss > 0
			? (isLong ? _entryPrice - StopLoss * step : _entryPrice + StopLoss * step)
			: 0m;

		_takePrice = TakeProfit > 0
			? (isLong ? _entryPrice + TakeProfit * step : _entryPrice - TakeProfit * step)
			: 0m;
	}

	private void ManagePosition(decimal price)
	{
		var step = Security.PriceStep ?? 1m;

		if (Position > 0)
		{
			if ((_takePrice != 0m && price >= _takePrice) || (_stopPrice != 0m && price <= _stopPrice))
			{
				ClosePosition();
				return;
			}

			if (Trailing > 0)
			{
				var newStop = price - Trailing * step;
				if (newStop > _stopPrice && newStop > _entryPrice)
					_stopPrice = newStop;
			}

			if (NoLoss > 0 && _stopPrice < _entryPrice)
			{
				var breakEven = price - NoLoss * step;
				if (breakEven > _entryPrice)
					_stopPrice = breakEven;
			}
		}
		else
		{
			if ((_takePrice != 0m && price <= _takePrice) || (_stopPrice != 0m && price >= _stopPrice))
			{
				ClosePosition();
				return;
			}

			if (Trailing > 0)
			{
				var newStop = price + Trailing * step;
				if (newStop < _stopPrice && newStop < _entryPrice)
					_stopPrice = newStop;
			}

			if (NoLoss > 0 && _stopPrice > _entryPrice)
			{
				var breakEven = price + NoLoss * step;
				if (breakEven < _entryPrice)
					_stopPrice = breakEven;
			}
		}
	}
}

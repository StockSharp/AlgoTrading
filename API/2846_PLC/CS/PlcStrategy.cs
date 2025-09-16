using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// PLC strategy converted from MetaTrader.
/// Places stop orders above and below the latest candle with configurable shifts.
/// Adjusts lot size based on fractal levels from M5 and H1 timeframes and closes positions after reaching a profit target.
/// </summary>
public class PlcStrategy : Strategy
{
	private readonly StrategyParam<int> _shiftOhlcPips;
	private readonly StrategyParam<decimal> _minimumProfit;
	private readonly StrategyParam<int> _shiftPositionPips;
	private readonly StrategyParam<decimal> _buyVolume;
	private readonly StrategyParam<decimal> _sellVolume;
	private readonly StrategyParam<int> _lotIncreaseM5;
	private readonly StrategyParam<int> _lotIncreaseH1;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _fractalsM5Type;
	private readonly StrategyParam<DataType> _fractalsH1Type;

	private readonly List<Order> _buyStopOrders = new();
	private readonly List<Order> _sellStopOrders = new();
	private readonly List<PositionLot> _longPositions = new();
	private readonly List<PositionLot> _shortPositions = new();

	private readonly FractalTracker _fractalsM5 = new();
	private readonly FractalTracker _fractalsH1 = new();

	private bool _deleteOrders;
	private bool _closePositions;

	/// <summary>
	/// Initializes a new instance of <see cref="PlcStrategy"/>.
	/// </summary>
	public PlcStrategy()
	{
		_shiftOhlcPips = Param(nameof(ShiftOhlcPips), 15)
			.SetGreaterThanZero()
			.SetDisplay("Shift OHLC", "Offset added to candle high/low (in pips)", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(5, 50, 5);

		_minimumProfit = Param(nameof(MinimumProfit), 7m)
			.SetGreaterThanZero()
			.SetDisplay("Minimum Profit", "Profit threshold that triggers closing all positions", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(3m, 20m, 1m);

		_shiftPositionPips = Param(nameof(ShiftPositionPips), 43)
			.SetGreaterThanZero()
			.SetDisplay("Shift Position", "Required distance from the highest/lowest position before placing a new stop", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 5);

		_buyVolume = Param(nameof(BuyVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Buy Volume", "Base volume for buy stop orders", "Trading");

		_sellVolume = Param(nameof(SellVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Sell Volume", "Base volume for sell stop orders", "Trading");

		_lotIncreaseM5 = Param(nameof(LotIncreaseRateM5), 2)
			.SetDisplay("M5 Multiplier", "Multiplier applied when the stop is above the latest M5 fractal", "Scaling");

		_lotIncreaseH1 = Param(nameof(LotIncreaseRateH1), 4)
			.SetDisplay("H1 Multiplier", "Multiplier applied when the stop is above the latest H1 fractal", "Scaling");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Entry Timeframe", "Primary candle timeframe used for signals", "Data");

		_fractalsM5Type = Param(nameof(FractalsM5Type), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("M5 Fractal Timeframe", "Timeframe used to track M5 fractal levels", "Data");

		_fractalsH1Type = Param(nameof(FractalsH1Type), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("H1 Fractal Timeframe", "Timeframe used to track H1 fractal levels", "Data");
	}

	/// <summary>
	/// Offset added to previous candle high and low before placing stop orders.
	/// </summary>
	public int ShiftOhlcPips
	{
		get => _shiftOhlcPips.Value;
		set => _shiftOhlcPips.Value = value;
	}

	/// <summary>
	/// Profit target that triggers closing all open positions.
	/// </summary>
	public decimal MinimumProfit
	{
		get => _minimumProfit.Value;
		set => _minimumProfit.Value = value;
	}

	/// <summary>
	/// Minimum distance between the newest stop order and the highest/lowest position price.
	/// </summary>
	public int ShiftPositionPips
	{
		get => _shiftPositionPips.Value;
		set => _shiftPositionPips.Value = value;
	}

	/// <summary>
	/// Base volume for buy stop orders.
	/// </summary>
	public decimal BuyVolume
	{
		get => _buyVolume.Value;
		set => _buyVolume.Value = value;
	}

	/// <summary>
	/// Base volume for sell stop orders.
	/// </summary>
	public decimal SellVolume
	{
		get => _sellVolume.Value;
		set => _sellVolume.Value = value;
	}

	/// <summary>
	/// Multiplier applied when the stop price is above the latest M5 fractal.
	/// </summary>
	public int LotIncreaseRateM5
	{
		get => _lotIncreaseM5.Value;
		set => _lotIncreaseM5.Value = value;
	}

	/// <summary>
	/// Multiplier applied when the stop price is above the latest H1 fractal.
	/// </summary>
	public int LotIncreaseRateH1
	{
		get => _lotIncreaseH1.Value;
		set => _lotIncreaseH1.Value = value;
	}

	/// <summary>
	/// Primary candle timeframe used for signal generation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Timeframe used to track M5 fractal levels.
	/// </summary>
	public DataType FractalsM5Type
	{
		get => _fractalsM5Type.Value;
		set => _fractalsM5Type.Value = value;
	}

	/// <summary>
	/// Timeframe used to track H1 fractal levels.
	/// </summary>
	public DataType FractalsH1Type
	{
		get => _fractalsH1Type.Value;
		set => _fractalsH1Type.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, FractalsM5Type), (Security, FractalsH1Type)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_buyStopOrders.Clear();
		_sellStopOrders.Clear();
		_longPositions.Clear();
		_shortPositions.Clear();
		_fractalsM5.Reset();
		_fractalsH1.Reset();
		_deleteOrders = false;
		_closePositions = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription.Bind(ProcessMainCandle).Start();

		var m5Subscription = SubscribeCandles(FractalsM5Type);
		m5Subscription.Bind(ProcessFractalM5).Start();

		var h1Subscription = SubscribeCandles(FractalsH1Type);
		h1Subscription.Bind(ProcessFractalH1).Start();
	}

	private void ProcessFractalM5(ICandleMessage candle)
	{
		_fractalsM5.Update(candle);
	}

	private void ProcessFractalH1(ICandleMessage candle)
	{
		_fractalsH1.Update(candle);
	}

	private void ProcessMainCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		CleanupInactiveOrders();

		if (_deleteOrders && TryCancelPendingOrders())
		{
			return;
		}

		if (_closePositions && TryClosePositions())
		{
			return;
		}

		var adjustedPoint = GetAdjustedPoint();
		var shiftOhlc = adjustedPoint * ShiftOhlcPips;
		var shiftPosition = adjustedPoint * ShiftPositionPips;

		var highWithShift = candle.HighPrice + shiftOhlc;
		var lowWithShift = candle.LowPrice - shiftOhlc;

		var highestBuy = GetHighestBuyPrice();
		var lowestSell = GetLowestSellPrice();

		var buyStopCount = GetActiveOrderCount(_buyStopOrders);
		var sellStopCount = GetActiveOrderCount(_sellStopOrders);

		if (ShouldPlaceBuyStop(highestBuy, highWithShift, shiftPosition, buyStopCount))
		{
			var volume = CalculateBuyVolume(highWithShift);
			if (volume > 0m)
			{
				var order = BuyStop(volume, highWithShift);
				if (order != null)
				{
					_buyStopOrders.Add(order);
				}
			}
		}

		if (ShouldPlaceSellStop(lowestSell, lowWithShift, shiftPosition, sellStopCount))
		{
			var volume = CalculateSellVolume(lowWithShift);
			if (volume > 0m)
			{
				var order = SellStop(volume, lowWithShift);
				if (order != null)
				{
					_sellStopOrders.Add(order);
				}
			}
		}

		var profit = CalculateOpenProfit(candle.ClosePrice);
		if (profit > MinimumProfit)
		{
			_closePositions = true;
		}
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade?.Order == null)
		{
			return;
		}

		var order = trade.Order;
		var volume = trade.Trade.Volume;
		var price = trade.Trade.Price;

		if (volume <= 0m)
		{
			return;
		}

		if (order.Side == Sides.Buy)
		{
			var remaining = ReducePositions(_shortPositions, volume);
			if (remaining > 0m)
			{
				_longPositions.Add(new PositionLot(price, remaining));
			}
		}
		else if (order.Side == Sides.Sell)
		{
			var remaining = ReducePositions(_longPositions, volume);
			if (remaining > 0m)
			{
				_shortPositions.Add(new PositionLot(price, remaining));
			}
		}

		if (_buyStopOrders.Contains(order) || _sellStopOrders.Contains(order))
		{
			_deleteOrders = true;
		}
	}

	private void CleanupInactiveOrders()
	{
		RemoveInactiveOrders(_buyStopOrders);
		RemoveInactiveOrders(_sellStopOrders);

		if (_deleteOrders && _buyStopOrders.Count == 0 && _sellStopOrders.Count == 0)
		{
			_deleteOrders = false;
		}
	}

	private static void RemoveInactiveOrders(List<Order> orders)
	{
		for (var i = orders.Count - 1; i >= 0; i--)
		{
			var order = orders[i];
			if (order == null)
			{
				orders.RemoveAt(i);
				continue;
			}

			if (order.State != OrderStates.Active)
			{
				orders.RemoveAt(i);
			}
		}
	}

	private bool TryCancelPendingOrders()
	{
		var hasActive = false;

		foreach (var order in _buyStopOrders)
		{
			if (order.State == OrderStates.Active)
			{
				CancelOrder(order);
				hasActive = true;
			}
		}

		foreach (var order in _sellStopOrders)
		{
			if (order.State == OrderStates.Active)
			{
				CancelOrder(order);
				hasActive = true;
			}
		}

		return hasActive;
	}

	private bool TryClosePositions()
	{
		var position = Position;
		if (position == 0m)
		{
			_closePositions = false;
			return false;
		}

		var volume = Math.Abs(position);
		if (volume <= 0m)
		{
			_closePositions = false;
			return false;
		}

		if (position > 0m)
		{
			SellMarket(volume);
		}
		else
		{
			BuyMarket(volume);
		}

		return true;
	}

	private decimal GetAdjustedPoint()
	{
		var security = Security;
		if (security == null)
		{
			return 0m;
		}

		var priceStep = security.PriceStep ?? 0m;
		if (priceStep == 0m)
		{
			return 0m;
		}

		var decimals = security.Decimals;
		var adjust = decimals == 3 || decimals == 5 ? 10m : 1m;
		return priceStep * adjust;
	}

	private bool ShouldPlaceBuyStop(decimal highestBuy, decimal stopPrice, decimal shiftPosition, int pendingCount)
	{
		if (highestBuy != 0m)
		{
			return stopPrice - highestBuy > shiftPosition;
		}

		return pendingCount == 0;
	}

	private bool ShouldPlaceSellStop(decimal lowestSell, decimal stopPrice, decimal shiftPosition, int pendingCount)
	{
		if (lowestSell != 0m)
		{
			return lowestSell - stopPrice > shiftPosition;
		}

		return pendingCount == 0;
	}

	private decimal CalculateBuyVolume(decimal stopPrice)
	{
		var volume = BuyVolume;

		var caseId = 0;
		var upM5 = _fractalsM5.Up;
		var upH1 = _fractalsH1.Up;

		if (stopPrice - upM5 > 0m)
		{
			caseId = 1;
		}
		if (stopPrice - upH1 > 0m)
		{
			caseId = 2;
		}

		switch (caseId)
		{
			case 1:
			{
				if (LotIncreaseRateM5 > 0)
				{
					volume *= LotIncreaseRateM5;
				}
				break;
			}
			case 2:
			{
				if (LotIncreaseRateH1 > 0)
				{
					volume *= LotIncreaseRateH1;
				}
				break;
			}
		}

		return AdjustVolume(volume);
	}

	private decimal CalculateSellVolume(decimal stopPrice)
	{
		var volume = SellVolume;

		var caseId = 0;
		var downM5 = _fractalsM5.Down;
		var downH1 = _fractalsH1.Down;

		if (downM5 - stopPrice > 0m)
		{
			caseId = 1;
		}
		if (downH1 - stopPrice > 0m)
		{
			caseId = 2;
		}

		switch (caseId)
		{
			case 1:
			{
				if (LotIncreaseRateM5 > 0)
				{
					volume *= LotIncreaseRateM5;
				}
				break;
			}
			case 2:
			{
				if (LotIncreaseRateH1 > 0)
				{
					volume *= LotIncreaseRateH1;
				}
				break;
			}
		}

		return AdjustVolume(volume);
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (volume <= 0m)
		{
			return 0m;
		}

		var security = Security;
		var step = security?.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Floor(volume / step);
			volume = steps * step;
		}

		return volume > 0m ? volume : 0m;
	}

	private static int GetActiveOrderCount(List<Order> orders)
	{
		var count = 0;

		foreach (var order in orders)
		{
			if (order != null && order.State == OrderStates.Active)
			{
				count++;
			}
		}

		return count;
	}

	private decimal GetHighestBuyPrice()
	{
		if (_longPositions.Count == 0)
		{
			return 0m;
		}

		var hasValue = false;
		var highest = 0m;

		foreach (var lot in _longPositions)
		{
			if (!hasValue || lot.Price > highest)
			{
				highest = lot.Price;
				hasValue = true;
			}
		}

		return hasValue ? highest : 0m;
	}

	private decimal GetLowestSellPrice()
	{
		if (_shortPositions.Count == 0)
		{
			return 0m;
		}

		var hasValue = false;
		var lowest = 0m;

		foreach (var lot in _shortPositions)
		{
			if (!hasValue || lot.Price < lowest)
			{
				lowest = lot.Price;
				hasValue = true;
			}
		}

		return hasValue ? lowest : 0m;
	}

	private decimal ReducePositions(List<PositionLot> positions, decimal volume)
	{
		var index = 0;
		while (index < positions.Count && volume > 0m)
		{
			var lot = positions[index];
			if (lot.Volume > volume)
			{
				lot.Volume -= volume;
				volume = 0m;
				break;
			}
			volume -= lot.Volume;
			positions.RemoveAt(index);
		}
		return volume;
	}

	private decimal CalculateOpenProfit(decimal currentPrice)
	{
		var security = Security;
		if (security == null)
		{
			return 0m;
		}

		var priceStep = security.PriceStep ?? 0m;
		var stepPrice = security.StepPrice ?? priceStep;
		if (priceStep == 0m)
		{
			return 0m;
		}

		var multiplier = stepPrice / priceStep;
		var profit = 0m;

		foreach (var lot in _longPositions)
		{
			profit += (currentPrice - lot.Price) * multiplier * lot.Volume;
		}

		foreach (var lot in _shortPositions)
		{
			profit += (lot.Price - currentPrice) * multiplier * lot.Volume;
		}

		return profit;
	}

	private sealed class PositionLot
	{
		public PositionLot(decimal price, decimal volume)
		{
			Price = price;
			Volume = volume;
		}

		public decimal Price { get; }
		public decimal Volume { get; set; }
	}

	private sealed class FractalTracker
	{
		private readonly decimal[] _highs = new decimal[5];
		private readonly decimal[] _lows = new decimal[5];
		private int _count;

		public decimal Up { get; private set; }
		public decimal Down { get; private set; }

		public void Reset()
		{
			Array.Clear(_highs);
			Array.Clear(_lows);
			_count = 0;
			Up = 0m;
			Down = 0m;
		}

		public void Update(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
			{
				return;
			}

			if (_count < 5)
			{
				_highs[_count] = candle.HighPrice;
				_lows[_count] = candle.LowPrice;
				_count += 1;

				if (_count < 5)
				{
					return;
				}
			}
			else
			{
				for (var i = 0; i < 4; i++)
				{
					_highs[i] = _highs[i + 1];
					_lows[i] = _lows[i + 1];
				}

				_highs[4] = candle.HighPrice;
				_lows[4] = candle.LowPrice;
			}

			var midHigh = _highs[2];
			if (midHigh > _highs[0] && midHigh > _highs[1] && midHigh > _highs[3] && midHigh > _highs[4])
			{
				Up = midHigh;
			}

			var midLow = _lows[2];
			if (midLow < _lows[0] && midLow < _lows[1] && midLow < _lows[3] && midLow < _lows[4])
			{
				Down = midLow;
			}
		}
	}
}

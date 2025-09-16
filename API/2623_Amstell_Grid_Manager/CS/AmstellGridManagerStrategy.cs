using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Amstell averaging grid strategy that opens new entries when price drifts away
/// from the last fill and closes exposure once profit or loss thresholds are reached.
/// </summary>
public class AmstellGridManagerStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _buyDistancePips;
	private readonly StrategyParam<decimal> _sellDistanceMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _longVolume;
	private decimal _shortVolume;
	private decimal? _averageLongPrice;
	private decimal? _averageShortPrice;
	private decimal? _lastBuyPrice;
	private decimal? _lastSellPrice;
	private decimal _pipValue;
	private decimal _takeProfitOffset;
	private decimal _stopLossOffset;
	private decimal _buyDistanceOffset;
	private decimal _sellDistanceOffset;
	private bool _closingLong;
	private bool _closingShort;

	/// <summary>
	/// Quantity per market order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Profit target in pips for each grid leg.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Maximum tolerated loss in pips for each grid leg.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Price distance in pips required to add another long position.
	/// </summary>
	public int BuyDistancePips
	{
		get => _buyDistancePips.Value;
		set => _buyDistancePips.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the long distance when stacking short entries.
	/// </summary>
	public decimal SellDistanceMultiplier
	{
		get => _sellDistanceMultiplier.Value;
		set => _sellDistanceMultiplier.Value = value;
	}

	/// <summary>
	/// Candle data type used for decision making.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes the strategy parameters.
	/// </summary>
	public AmstellGridManagerStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Quantity submitted with each grid order", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 0.1m, 0.01m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 30)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Profit target distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10, 150, 10);

		_stopLossPips = Param(nameof(StopLossPips), 30)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10, 150, 10);

		_buyDistancePips = Param(nameof(BuyDistancePips), 10)
			.SetGreaterThanZero()
			.SetDisplay("Buy Distance (pips)", "Distance before adding another long", "Entries")
			.SetCanOptimize(true)
			.SetOptimize(5, 60, 5);

		_sellDistanceMultiplier = Param(nameof(SellDistanceMultiplier), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Sell Distance Multiplier", "Multiplier applied to long distance when adding shorts", "Entries")
			.SetCanOptimize(true)
			.SetOptimize(2m, 15m, 1m);

		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(1)))
			.SetDisplay("Candle Type", "Time frame for processing", "General");
	}

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_longVolume = 0m;
		_shortVolume = 0m;
		_averageLongPrice = null;
		_averageShortPrice = null;
		_lastBuyPrice = null;
		_lastSellPrice = null;
		_pipValue = 0m;
		_takeProfitOffset = 0m;
		_stopLossOffset = 0m;
		_buyDistanceOffset = 0m;
		_sellDistanceOffset = 0m;
		_closingLong = false;
		_closingShort = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (BuyDistancePips >= TakeProfitPips || BuyDistancePips >= StopLossPips)
			throw new InvalidOperationException("Buy distance must be less than take profit and stop loss distances.");

		UpdatePriceOffsets();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (!_closingLong && _longVolume > 0m && _averageLongPrice is decimal longAvg)
		{
			var profit = close - longAvg;
			if (profit >= _takeProfitOffset || -profit >= _stopLossOffset)
			{
				SellMarket(_longVolume);
				_closingLong = true;
				return;
			}
		}

		if (!_closingShort && _shortVolume > 0m && _averageShortPrice is decimal shortAvg)
		{
			var profit = shortAvg - close;
			if (profit >= _takeProfitOffset || -profit >= _stopLossOffset)
			{
				BuyMarket(_shortVolume);
				_closingShort = true;
				return;
			}
		}

		var openedLong = false;

		if (!_closingLong && Position >= 0m)
		{
			if (_longVolume <= 0m)
			{
				BuyMarket(OrderVolume);
				openedLong = true;
			}
			else if (_lastBuyPrice is decimal lastBuy && lastBuy - close >= _buyDistanceOffset)
			{
				BuyMarket(OrderVolume);
				openedLong = true;
			}
		}

		if (openedLong)
			return;

		if (!_closingShort && Position <= 0m)
		{
			if (_shortVolume <= 0m)
			{
				SellMarket(OrderVolume);
			}
			else if (_lastSellPrice is decimal lastSell && close - lastSell >= _sellDistanceOffset)
			{
				SellMarket(OrderVolume);
			}
		}
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null)
			return;

		var tradeVolume = trade.Trade.Volume;
		var price = trade.Trade.Price;

		if (trade.Order.Side == Sides.Buy)
		{
			if (_shortVolume > 0m)
			{
				var closingVolume = Math.Min(tradeVolume, _shortVolume);
				_shortVolume -= closingVolume;
				tradeVolume -= closingVolume;
				if (_shortVolume <= 0m)
				{
					_shortVolume = 0m;
					_averageShortPrice = null;
					_lastSellPrice = null;
				}
			}

			if (tradeVolume > 0m)
			{
				var newVolume = _longVolume + tradeVolume;
				var totalCost = (_averageLongPrice ?? 0m) * _longVolume + price * tradeVolume;
				_longVolume = newVolume;
				_averageLongPrice = totalCost / newVolume;
				_lastBuyPrice = price;
				_closingLong = false;
			}
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			if (_longVolume > 0m)
			{
				var closingVolume = Math.Min(tradeVolume, _longVolume);
				_longVolume -= closingVolume;
				tradeVolume -= closingVolume;
				if (_longVolume <= 0m)
				{
					_longVolume = 0m;
					_averageLongPrice = null;
					_lastBuyPrice = null;
				}
			}

			if (tradeVolume > 0m)
			{
				var newVolume = _shortVolume + tradeVolume;
				var totalCost = (_averageShortPrice ?? 0m) * _shortVolume + price * tradeVolume;
				_shortVolume = newVolume;
				_averageShortPrice = totalCost / newVolume;
				_lastSellPrice = price;
				_closingShort = false;
			}
		}

		if (_longVolume <= 0m && Position <= 0m)
			_closingLong = false;

		if (_shortVolume <= 0m && Position >= 0m)
			_closingShort = false;

		if (Position == 0m)
		{
			_longVolume = 0m;
			_shortVolume = 0m;
			_averageLongPrice = null;
			_averageShortPrice = null;
			_lastBuyPrice = null;
			_lastSellPrice = null;
			_closingLong = false;
			_closingShort = false;
		}
	}

	private void UpdatePriceOffsets()
	{
		var step = Security?.PriceStep ?? 1m;
		if (step <= 0m)
			step = 1m;

		var decimals = GetDecimalPlaces(step);
		_pipValue = decimals == 3 || decimals == 5 ? step * 10m : step;

		_takeProfitOffset = TakeProfitPips * _pipValue;
		_stopLossOffset = StopLossPips * _pipValue;
		_buyDistanceOffset = BuyDistancePips * _pipValue;
		_sellDistanceOffset = _buyDistanceOffset * SellDistanceMultiplier;
	}

	private static int GetDecimalPlaces(decimal value)
	{
		if (value == 0m)
			return 0;

		var bits = decimal.GetBits(value);
		return (bits[3] >> 16) & 0x7F;
	}
}

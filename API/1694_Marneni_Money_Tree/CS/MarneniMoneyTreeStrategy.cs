using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy inspired by the Marneni Money Tree expert advisor.
/// Places a market order and a series of limit orders when a shifted SMA condition is met.
/// </summary>
public class MarneniMoneyTreeStrategy : Strategy
{
	private readonly StrategyParam<decimal> _order2Pips;
	private readonly StrategyParam<decimal> _order3Pips;
	private readonly StrategyParam<decimal> _order4Pips;
	private readonly StrategyParam<decimal> _order5Pips;
	private readonly StrategyParam<decimal> _order6Pips;
	private readonly StrategyParam<decimal> _order7Pips;
	private readonly StrategyParam<decimal> _order8Pips;
	private readonly StrategyParam<decimal> _order9Pips;
	private readonly StrategyParam<DataType> _candleType;

	private readonly decimal[] _smaBuffer = new decimal[31];
	private int _bufferIndex;
	private int _valuesCount;
	private decimal _step;

	/// <summary>
	/// Distance for the second order in pips.
	/// </summary>
	public decimal Order2Pips { get => _order2Pips.Value; set => _order2Pips.Value = value; }

	/// <summary>
	/// Distance for the third order in pips.
	/// </summary>
	public decimal Order3Pips { get => _order3Pips.Value; set => _order3Pips.Value = value; }

	/// <summary>
	/// Distance for the fourth order in pips.
	/// </summary>
	public decimal Order4Pips { get => _order4Pips.Value; set => _order4Pips.Value = value; }

	/// <summary>
	/// Distance for the fifth order in pips.
	/// </summary>
	public decimal Order5Pips { get => _order5Pips.Value; set => _order5Pips.Value = value; }

	/// <summary>
	/// Distance for the sixth order in pips.
	/// </summary>
	public decimal Order6Pips { get => _order6Pips.Value; set => _order6Pips.Value = value; }

	/// <summary>
	/// Distance for the seventh order in pips.
	/// </summary>
	public decimal Order7Pips { get => _order7Pips.Value; set => _order7Pips.Value = value; }

	/// <summary>
	/// Distance for the eighth order in pips.
	/// </summary>
	public decimal Order8Pips { get => _order8Pips.Value; set => _order8Pips.Value = value; }

	/// <summary>
	/// Distance for the ninth order in pips.
	/// </summary>
	public decimal Order9Pips { get => _order9Pips.Value; set => _order9Pips.Value = value; }

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the strategy with default parameters.
	/// </summary>
	public MarneniMoneyTreeStrategy()
	{
		_order2Pips = Param(nameof(Order2Pips), 4m).SetDisplay("Order 2 Distance", "Second limit order distance", "Trading");
		_order3Pips = Param(nameof(Order3Pips), 8m).SetDisplay("Order 3 Distance", "Third limit order distance", "Trading");
		_order4Pips = Param(nameof(Order4Pips), 12m).SetDisplay("Order 4 Distance", "Fourth limit order distance", "Trading");
		_order5Pips = Param(nameof(Order5Pips), 20m).SetDisplay("Order 5 Distance", "Fifth limit order distance", "Trading");
		_order6Pips = Param(nameof(Order6Pips), 30m).SetDisplay("Order 6 Distance", "Sixth limit order distance", "Trading");
		_order7Pips = Param(nameof(Order7Pips), 40m).SetDisplay("Order 7 Distance", "Seventh limit order distance", "Trading");
		_order8Pips = Param(nameof(Order8Pips), 50m).SetDisplay("Order 8 Distance", "Eighth limit order distance", "Trading");
		_order9Pips = Param(nameof(Order9Pips), 60m).SetDisplay("Order 9 Distance", "Ninth limit order distance", "Trading");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame()).SetDisplay("Candle Type", "Timeframe for strategy", "General");

		Volume = 2m;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_step = Security.PriceStep ?? 1m;

		var sma = new Sma { Length = 40 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_smaBuffer[_bufferIndex] = smaValue;
		_bufferIndex = (_bufferIndex + 1) % _smaBuffer.Length;
		if (_valuesCount < _smaBuffer.Length)
			_valuesCount++;

		if (_valuesCount < _smaBuffer.Length)
			return;

		var idxCurrent = (_bufferIndex - 1 + _smaBuffer.Length) % _smaBuffer.Length;
		var idxShift4 = (_bufferIndex - 5 + _smaBuffer.Length) % _smaBuffer.Length;
		var idxShift30 = _bufferIndex % _smaBuffer.Length;

		var ma = _smaBuffer[idxShift4];
		var ma1 = _smaBuffer[idxCurrent];
		var ma2 = _smaBuffer[idxShift30];

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position == 0 && !HasActiveOrders())
		{
			if (ma > ma1 && ma < ma2)
				PlaceSellOrders(candle.ClosePrice);
			else if (ma < ma1 && ma > ma2)
				PlaceBuyOrders(candle.ClosePrice);
		}
		else
		{
			if (Position > 0 && ma > ma1)
			{
				SellMarket(Position);
				CancelActiveOrders();
			}
			else if (Position < 0 && ma < ma1)
			{
				BuyMarket(Math.Abs(Position));
				CancelActiveOrders();
			}
		}
	}

	private bool HasActiveOrders()
	{
		foreach (var order in Orders)
			if (order.State.IsActive())
				return true;
		return false;
	}

	private void PlaceSellOrders(decimal basePrice)
	{
		SellMarket(Volume);
		SellLimit(Volume, basePrice + Order2Pips * _step);
		SellLimit(Volume, basePrice + Order3Pips * _step);
		SellLimit(Volume, basePrice + Order4Pips * _step);
		SellLimit(Volume, basePrice + Order5Pips * _step);
		SellLimit(Volume, basePrice + Order6Pips * _step);
		SellLimit(Volume, basePrice + Order7Pips * _step);
		SellLimit(Volume, basePrice + Order8Pips * _step);
		SellLimit(Volume, basePrice + Order9Pips * _step);
	}

	private void PlaceBuyOrders(decimal basePrice)
	{
		BuyMarket(Volume);
		BuyLimit(Volume, basePrice - Order2Pips * _step);
		BuyLimit(Volume, basePrice - Order3Pips * _step);
		BuyLimit(Volume, basePrice - Order4Pips * _step);
		BuyLimit(Volume, basePrice - Order5Pips * _step);
		BuyLimit(Volume, basePrice - Order6Pips * _step);
		BuyLimit(Volume, basePrice - Order7Pips * _step);
		BuyLimit(Volume, basePrice - Order8Pips * _step);
		BuyLimit(Volume, basePrice - Order9Pips * _step);
	}
}

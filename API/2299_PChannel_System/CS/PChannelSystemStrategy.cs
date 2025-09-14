using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Price Channel breakout system with delayed confirmation.
/// Opens a long position when price breaks above the channel and then returns inside.
/// Opens a short position on a breakout below the channel followed by a return inside.
/// Supports optional stop loss and take profit levels.
/// </summary>
public class PChannelSystemStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<int> _shift;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private bool _isLongPosition;
	private bool _prevAbove;
	private bool _prevBelow;

	/// <summary>
	/// Lookback period for channel calculation.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Number of bars used to shift the channel.
	/// </summary>
	public int Shift
	{
		get => _shift.Value;
		set => _shift.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit distance in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public PChannelSystemStrategy()
	{
		_period = Param(nameof(Period), 20)
		.SetGreaterThanZero()
		.SetDisplay("Period", "Channel calculation period", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(10, 40, 5);

		_shift = Param(nameof(Shift), 2)
		.SetGreaterOrEqualZero()
		.SetDisplay("Shift", "Bars shift for channel", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(0, 5, 1);

		_stopLoss = Param(nameof(StopLoss), 1000m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Stop Loss", "Stop loss distance in price", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(100m, 2000m, 100m);

		_takeProfit = Param(nameof(TakeProfit), 2000m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Take Profit", "Take profit distance in price", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(200m, 4000m, 200m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles for strategy", "General");
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

		_entryPrice = 0m;
		_isLongPosition = false;
		_prevAbove = false;
		_prevBelow = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var highest = new Highest { Length = Period };
		var lowest = new Lowest { Length = Period };

		var subscription = SubscribeCandles(CandleType);

		var upperQueue = new Queue<decimal>();
		var lowerQueue = new Queue<decimal>();

		subscription
		.Bind(highest, lowest, (candle, highVal, lowVal) =>
		{
			if (candle.State != CandleStates.Finished)
			return;

			if (!IsFormedAndOnlineAndAllowTrading())
			return;

			upperQueue.Enqueue(highVal);
			lowerQueue.Enqueue(lowVal);

			if (upperQueue.Count <= Shift || lowerQueue.Count <= Shift)
			return;

			var upper = upperQueue.Dequeue();
			var lower = lowerQueue.Dequeue();

			ProcessCandle(candle, upper, lower);
		})
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal upper, decimal lower)
	{
		var isAbove = candle.ClosePrice > upper;
		var isBelow = candle.ClosePrice < lower;

		if (_prevAbove && !isAbove)
		{
			if (Position < 0)
			BuyMarket(Math.Abs(Position));

			if (Position <= 0)
			{
				_entryPrice = candle.ClosePrice;
				_isLongPosition = true;
				BuyMarket(Volume + Math.Abs(Position));
			}
		}
		else if (_prevBelow && !isBelow)
		{
			if (Position > 0)
			SellMarket(Position);

			if (Position >= 0)
			{
				_entryPrice = candle.ClosePrice;
				_isLongPosition = false;
				SellMarket(Volume + Math.Abs(Position));
			}
		}

		if (Position != 0 && _entryPrice != 0m)
		CheckStops(candle.ClosePrice);

		_prevAbove = isAbove;
		_prevBelow = isBelow;
	}

	private void CheckStops(decimal price)
	{
		if (_isLongPosition)
		{
			if (StopLoss > 0m && price <= _entryPrice - StopLoss)
			SellMarket(Position);

			if (TakeProfit > 0m && price >= _entryPrice + TakeProfit)
			SellMarket(Position);
		}
		else
		{
			if (StopLoss > 0m && price >= _entryPrice + StopLoss)
			BuyMarket(Math.Abs(Position));

			if (TakeProfit > 0m && price <= _entryPrice - TakeProfit)
			BuyMarket(Math.Abs(Position));
		}
	}
}

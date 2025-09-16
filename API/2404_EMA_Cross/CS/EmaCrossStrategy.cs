using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover strategy with optional reversal and trailing stop.
/// Buys when fast EMA crosses above slow EMA, sells on opposite cross.
/// </summary>
public class EmaCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<bool> _reverse;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _entryPrice;
	private decimal _trailPrice;
	private bool _isLong;
	private int _lastDirection;
	
	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int ShortLength { get => _shortLength.Value; set => _shortLength.Value = value; }
	
	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int LongLength { get => _longLength.Value; set => _longLength.Value = value; }
	
	/// <summary>
	/// Take profit distance in price units.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	
	/// <summary>
	/// Stop loss distance in price units.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	
	/// <summary>
	/// Trailing stop distance in price units.
	/// </summary>
	public decimal TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }
	
	/// <summary>
	/// Reverse cross direction.
	/// </summary>
	public bool Reverse { get => _reverse.Value; set => _reverse.Value = value; }
	
	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Constructor.
	/// </summary>
	public EmaCrossStrategy()
	{
		_shortLength = Param(nameof(ShortLength), 9)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA Length", "Period of the fast EMA", "EMA");
		
		_longLength = Param(nameof(LongLength), 45)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA Length", "Period of the slow EMA", "EMA");
		
		_takeProfit = Param(nameof(TakeProfit), 25m)
		.SetDisplay("Take Profit", "Profit target in price units", "Risk");
		
		_stopLoss = Param(nameof(StopLoss), 105m)
		.SetDisplay("Stop Loss", "Stop loss in price units", "Risk");
		
		_trailingStop = Param(nameof(TrailingStop), 20m)
		.SetDisplay("Trailing Stop", "Trailing stop distance", "Risk");
		
		_reverse = Param(nameof(Reverse), true)
		.SetDisplay("Reverse", "Swap EMA lines", "General");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
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
		_trailPrice = 0m;
		_isLong = false;
		_lastDirection = 0;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var fastEma = new ExponentialMovingAverage
		{
			Length = ShortLength,
			CandlePrice = CandlePrice.Close,
		};
		
		var slowEma = new ExponentialMovingAverage
		{
			Length = LongLength,
			CandlePrice = CandlePrice.Close,
		};
		
		var subscription = SubscribeCandles(CandleType);
		
		subscription
		.Bind(fastEma, slowEma, ProcessCandle)
		.Start();
		
		StartProtection();
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var cross = Reverse ? GetCross(fast, slow) : GetCross(slow, fast);
		
		if (Position <= 0 && cross == 1)
		{
			_entryPrice = candle.ClosePrice;
			_trailPrice = 0m;
			_isLong = true;
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (Position >= 0 && cross == 2)
		{
			_entryPrice = candle.ClosePrice;
			_trailPrice = 0m;
			_isLong = false;
			SellMarket(Volume + Math.Abs(Position));
		}
		
		if (Position != 0)
		ManagePosition(candle);
	}
	
	private int GetCross(decimal line1, decimal line2)
	{
		var dir = line1 > line2 ? 1 : 2;
		
		if (_lastDirection == 0)
		{
			_lastDirection = dir;
			return 0;
		}
		
		if (dir != _lastDirection)
		{
			_lastDirection = dir;
			return dir;
		}
		
		return 0;
	}
	
	private void ManagePosition(ICandleMessage candle)
	{
		if (_isLong)
		{
			// calculate take profit and stop loss
			if (TakeProfit > 0m && candle.ClosePrice >= _entryPrice + TakeProfit)
			{
				SellMarket(Position);
				return;
			}
			
			if (StopLoss > 0m && candle.ClosePrice <= _entryPrice - StopLoss)
			{
				SellMarket(Position);
				return;
			}
			
			if (TrailingStop > 0m)
			{
				var newStop = candle.ClosePrice - TrailingStop;
				if (_trailPrice < newStop)
				_trailPrice = newStop;
				
				if (_trailPrice > 0m && candle.ClosePrice <= _trailPrice)
				SellMarket(Position);
			}
		}
		else
		{
			// short position management
			if (TakeProfit > 0m && candle.ClosePrice <= _entryPrice - TakeProfit)
			{
				BuyMarket(-Position);
				return;
			}
			
			if (StopLoss > 0m && candle.ClosePrice >= _entryPrice + StopLoss)
			{
				BuyMarket(-Position);
				return;
			}
			
			if (TrailingStop > 0m)
			{
				var newStop = candle.ClosePrice + TrailingStop;
				if (_trailPrice == 0m || _trailPrice > newStop)
				_trailPrice = newStop;
				
				if (candle.ClosePrice >= _trailPrice)
				BuyMarket(-Position);
			}
		}
	}
}

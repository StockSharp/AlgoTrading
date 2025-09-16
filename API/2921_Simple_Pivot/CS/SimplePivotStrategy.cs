using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple pivot strategy that flips position based on the previous bar pivot.
/// </summary>
public class SimplePivotStrategy : Strategy
{
	private enum TradeDirection
	{
		None,
		Long,
		Short,
	}

	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private TradeDirection _lastDirection = TradeDirection.None;
	private decimal _previousHigh;
	private decimal _previousLow;
	private bool _hasPreviousCandle;

	public decimal Volume { get => _volume.Value; set => _volume.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public SimplePivotStrategy()
	{
		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Data");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_lastDirection = TradeDirection.None;
		_previousHigh = 0m;
		_previousLow = 0m;
		_hasPreviousCandle = false;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPreviousCandle)
		{
			// Collect the very first completed candle as the seed for the pivot calculation.
			_previousHigh = candle.HighPrice;
			_previousLow = candle.LowPrice;
			_hasPreviousCandle = true;
			return;
		}

		var pivot = (_previousHigh + _previousLow) / 2m;
		var desiredDirection = TradeDirection.Long;

		// When the new open sits between the previous high and pivot we switch to a short bias.
		if (candle.OpenPrice < _previousHigh && candle.OpenPrice > pivot)
			desiredDirection = TradeDirection.Short;

		if (desiredDirection == _lastDirection && _lastDirection != TradeDirection.None)
		{
			// Keep the existing position when direction has not changed.
			_previousHigh = candle.HighPrice;
			_previousLow = candle.LowPrice;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousHigh = candle.HighPrice;
			_previousLow = candle.LowPrice;
			return;
		}

		CloseExistingPosition();

		if (desiredDirection == TradeDirection.Long)
		{
			// Enter a long position when the open is below the pivot.
			BuyMarket(Volume);
		}
		else
		{
			// Enter a short position when the open is above the pivot zone.
			SellMarket(Volume);
		}

		_lastDirection = desiredDirection;
		_previousHigh = candle.HighPrice;
		_previousLow = candle.LowPrice;
	}

	private void CloseExistingPosition()
	{
		if (Position > 0)
		{
			// Flip from long to flat before opening the opposite direction.
			SellMarket(Math.Abs(Position));
			_lastDirection = TradeDirection.None;
		}
		else if (Position < 0)
		{
			// Flip from short to flat before opening the opposite direction.
			BuyMarket(Math.Abs(Position));
			_lastDirection = TradeDirection.None;
		}
	}
}

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy based on consolidation zones.
/// </summary>
public class MaximusVxLiteStrategy : Strategy
{
	private readonly StrategyParam<decimal> _distance;
	private readonly StrategyParam<decimal> _maxRange;
	private readonly StrategyParam<decimal> _trailStop;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _zoneHigh;
	private decimal _zoneLow;
	private bool _zoneSet;
	private decimal _stopPrice;

	public decimal Distance { get => _distance.Value; set => _distance.Value = value; }
	public decimal MaxRange { get => _maxRange.Value; set => _maxRange.Value = value; }
	public decimal TrailStop { get => _trailStop.Value; set => _trailStop.Value = value; }
	public decimal StopLossVal { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MaximusVxLiteStrategy()
	{
		_distance = Param(nameof(Distance), 50m)
			.SetDisplay("Distance", "Breakout distance beyond zone", "General");

		_maxRange = Param(nameof(MaxRange), 2000m)
			.SetDisplay("Max Range", "Maximum consolidation zone size", "General");

		_trailStop = Param(nameof(TrailStop), 300m)
			.SetDisplay("Trail Stop", "Trailing stop distance", "Risk");

		_stopLoss = Param(nameof(StopLossVal), 500m)
			.SetDisplay("Stop Loss", "Initial stop loss distance", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for candles", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var highest = new Highest { Length = 40 };
		var lowest = new Lowest { Length = 40 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(highest, lowest, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var rangeSize = highest - lowest;

		// Update consolidation zone when range is within bounds
		if (rangeSize <= MaxRange && rangeSize > 0)
		{
			_zoneHigh = highest;
			_zoneLow = lowest;
			_zoneSet = true;
		}

		if (!_zoneSet)
			return;

		if (Position == 0)
		{
			if (candle.ClosePrice > _zoneHigh + Distance)
			{
				BuyMarket();
				_stopPrice = candle.ClosePrice - StopLossVal;
			}
			else if (candle.ClosePrice < _zoneLow - Distance)
			{
				SellMarket();
				_stopPrice = candle.ClosePrice + StopLossVal;
			}
		}
		else if (Position > 0)
		{
			var newStop = candle.ClosePrice - TrailStop;
			if (newStop > _stopPrice)
				_stopPrice = newStop;

			if (candle.ClosePrice <= _stopPrice)
				SellMarket();
		}
		else if (Position < 0)
		{
			var newStop = candle.ClosePrice + TrailStop;
			if (newStop < _stopPrice)
				_stopPrice = newStop;

			if (candle.ClosePrice >= _stopPrice)
				BuyMarket();
		}
	}
}

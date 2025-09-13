using System;
using System.Collections.Generic;

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
	private readonly StrategyParam<int> _delayOpen;
	private readonly StrategyParam<int> _distance;
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<int> _range;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _trail;
	private readonly StrategyParam<DataType> _candleType;

	private DateTimeOffset _lastTradeTime;
	private decimal _upperMax;
	private decimal _upperMin;
	private decimal _lowerMax;
	private decimal _lowerMin;
	private decimal _stopPrice;

	public int DelayOpen { get => _delayOpen.Value; set => _delayOpen.Value = value; }
	public int Distance { get => _distance.Value; set => _distance.Value = value; }
	public int Period { get => _period.Value; set => _period.Value = value; }
	public int Range { get => _range.Value; set => _range.Value = value; }
	public int StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public int Trail { get => _trail.Value; set => _trail.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MaximusVxLiteStrategy()
	{
		_delayOpen = Param(nameof(DelayOpen), 2)
			.SetDisplay("Delay Open", "Hours before new trade", "General");

		_distance = Param(nameof(Distance), 50)
			.SetDisplay("Distance", "Breakout distance in points", "General");

		_period = Param(nameof(Period), 20)
			.SetDisplay("Period", "Hours to reset consolidation", "General");

		_range = Param(nameof(Range), 500)
			.SetDisplay("Range", "Maximum consolidation size in points", "General");

		_stopLoss = Param(nameof(StopLoss), 20000)
			.SetDisplay("Stop Loss", "Initial stop loss in points", "Risk");

		_trail = Param(nameof(Trail), 500)
			.SetDisplay("Trail", "Trailing stop distance in points", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for candles", "General");
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

		var highest = new Highest { Length = 40 };
		var lowest = new Lowest { Length = 40 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(highest, lowest, ProcessCandle).Start();

		_lastTradeTime = time;
		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var step = Security.PriceStep ?? 1m;
		var rangeSize = (highest - lowest) / step;

		// Update consolidation zone if its size is below the Range parameter
		if (rangeSize <= Range)
		{
			_upperMax = highest;
			_upperMin = lowest;
			_lowerMax = highest;
			_lowerMin = lowest;
		}

		// Do not open new trades before the delay expires
		if (candle.OpenTime - _lastTradeTime < TimeSpan.FromHours(DelayOpen))
			return;

		if (Position == 0)
		{
			// Open long position on breakout above the upper boundary
			if (candle.ClosePrice > _upperMax + Distance * step)
			{
				BuyMarket();
				_lastTradeTime = candle.OpenTime;
				_stopPrice = candle.ClosePrice - StopLoss * step;
			}
			// Open short position on breakout below the lower boundary
			else if (candle.ClosePrice < _lowerMin - Distance * step)
			{
				SellMarket();
				_lastTradeTime = candle.OpenTime;
				_stopPrice = candle.ClosePrice + StopLoss * step;
			}
		}
		else if (Position > 0)
		{
			var newStop = candle.ClosePrice - Trail * step;
			if (newStop > _stopPrice)
				_stopPrice = newStop;

			if (candle.LowPrice <= _stopPrice)
				SellMarket();
		}
		else if (Position < 0)
		{
			var newStop = candle.ClosePrice + Trail * step;
			if (newStop < _stopPrice)
				_stopPrice = newStop;

			if (candle.HighPrice >= _stopPrice)
				BuyMarket();
		}

		// Reset consolidation after the Period
		if (candle.OpenTime - _lastTradeTime >= TimeSpan.FromHours(Period))
		{
			_upperMax = 0m;
			_upperMin = 0m;
			_lowerMax = 0m;
			_lowerMin = 0m;
		}
	}
}

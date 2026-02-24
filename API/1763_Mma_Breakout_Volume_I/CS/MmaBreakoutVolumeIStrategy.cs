using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on smoothed moving average breakout with EMA exit.
/// </summary>
public class MmaBreakoutVolumeIStrategy : Strategy
{
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _exitPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevPrice;
	private decimal _prevSlow;

	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public int ExitPeriod { get => _exitPeriod.Value; set => _exitPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MmaBreakoutVolumeIStrategy()
	{
		_slowPeriod = Param(nameof(SlowPeriod), 200)
			.SetDisplay("Slow SMMA Period", "Period for long smoothed moving average", "Indicators");

		_exitPeriod = Param(nameof(ExitPeriod), 5)
			.SetDisplay("Exit EMA Period", "Period for exit EMA", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var slowSmma = new SmoothedMovingAverage { Length = SlowPeriod };
		var exitEma = new EMA { Length = ExitPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(slowSmma, exitEma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal slowValue, decimal exitValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevPrice == 0m || _prevSlow == 0m)
		{
			_prevPrice = candle.ClosePrice;
			_prevSlow = slowValue;
			return;
		}

		var isCrossAbove = _prevPrice <= _prevSlow && candle.ClosePrice > slowValue;
		var isCrossBelow = _prevPrice >= _prevSlow && candle.ClosePrice < slowValue;

		if (isCrossAbove && Position <= 0)
			BuyMarket();
		else if (isCrossBelow && Position >= 0)
			SellMarket();
		else if (Position > 0 && candle.ClosePrice < exitValue)
			SellMarket();
		else if (Position < 0 && candle.ClosePrice > exitValue)
			BuyMarket();

		_prevPrice = candle.ClosePrice;
		_prevSlow = slowValue;
	}
}

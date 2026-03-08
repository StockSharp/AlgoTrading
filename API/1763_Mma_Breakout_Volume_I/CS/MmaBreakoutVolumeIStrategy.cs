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
	private bool _hasPrev;

	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public int ExitPeriod { get => _exitPeriod.Value; set => _exitPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MmaBreakoutVolumeIStrategy()
	{
		_slowPeriod = Param(nameof(SlowPeriod), 50)
			.SetDisplay("Slow EMA Period", "Period for long moving average", "Indicators");

		_exitPeriod = Param(nameof(ExitPeriod), 10)
			.SetDisplay("Exit EMA Period", "Period for exit EMA", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevPrice = 0;
		_prevSlow = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var slowEma = new ExponentialMovingAverage { Length = SlowPeriod };
		var exitEma = new ExponentialMovingAverage { Length = ExitPeriod };

		SubscribeCandles(CandleType)
			.Bind(slowEma, exitEma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal slowValue, decimal exitValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (!_hasPrev)
		{
			_prevPrice = candle.ClosePrice;
			_prevSlow = slowValue;
			_hasPrev = true;
			return;
		}

		var isCrossAbove = _prevPrice <= _prevSlow && candle.ClosePrice > slowValue;
		var isCrossBelow = _prevPrice >= _prevSlow && candle.ClosePrice < slowValue;

		if (isCrossAbove && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (isCrossBelow && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}
		else if (Position > 0 && candle.ClosePrice < exitValue)
			SellMarket();
		else if (Position < 0 && candle.ClosePrice > exitValue)
			BuyMarket();

		_prevPrice = candle.ClosePrice;
		_prevSlow = slowValue;
	}
}

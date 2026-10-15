using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// X Trader Strategy - contrarian moving average cross.
/// </summary>
public class XTraderStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _ma1Period;
	private readonly StrategyParam<int> _ma2Period;

	private decimal _ma1Prev;
	private decimal _ma1Prev2;
	private decimal _ma2Prev;
	private decimal _ma2Prev2;
	private bool _hasPrev2;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Ma1Period { get => _ma1Period.Value; set => _ma1Period.Value = value; }
	public int Ma2Period { get => _ma2Period.Value; set => _ma2Period.Value = value; }

	public XTraderStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_ma1Period = Param(nameof(Ma1Period), 16)
			.SetGreaterThanZero()
			.SetDisplay("MA1 Period", "Period of the first moving average", "Parameters");

		_ma2Period = Param(nameof(Ma2Period), 10)
			.SetGreaterThanZero()
			.SetDisplay("MA2 Period", "Period of the second moving average", "Parameters");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_ma1Prev = 0;
		_ma1Prev2 = 0;
		_ma2Prev = 0;
		_ma2Prev2 = 0;
		_hasPrev2 = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ma1 = new ExponentialMovingAverage { Length = Ma1Period };
		var ma2 = new ExponentialMovingAverage { Length = Ma2Period };

		SubscribeCandles(CandleType)
			.Bind(ma1, ma2, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma1Value, decimal ma2Value)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_ma1Prev == 0)
		{
			_ma1Prev = ma1Value;
			_ma2Prev = ma2Value;
			return;
		}

		if (!_hasPrev2)
		{
			_ma1Prev2 = _ma1Prev;
			_ma2Prev2 = _ma2Prev;
			_ma1Prev = ma1Value;
			_ma2Prev = ma2Value;
			_hasPrev2 = true;
			return;
		}

		// Contrarian: sell when MA1 crosses above MA2, buy when MA1 crosses below
		var sellSignal = ma1Value > ma2Value && _ma1Prev > _ma2Prev && _ma1Prev2 < _ma2Prev2;
		var buySignal = ma1Value < ma2Value && _ma1Prev < _ma2Prev && _ma1Prev2 > _ma2Prev2;

		if (sellSignal && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}
		else if (buySignal && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}

		_ma1Prev2 = _ma1Prev;
		_ma2Prev2 = _ma2Prev;
		_ma1Prev = ma1Value;
		_ma2Prev = ma2Value;
	}
}

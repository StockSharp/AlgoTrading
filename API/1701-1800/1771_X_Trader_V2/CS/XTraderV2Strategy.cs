using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Contrarian moving average crossover strategy (X trader v2).
/// </summary>
public class XTraderV2Strategy : Strategy
{
	private readonly StrategyParam<int> _ma1Period;
	private readonly StrategyParam<int> _ma2Period;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _ma1Prev;
	private decimal _ma1Prev2;
	private decimal _ma2Prev;
	private decimal _ma2Prev2;
	private bool _hasPrev2;

	public int Ma1Period { get => _ma1Period.Value; set => _ma1Period.Value = value; }
	public int Ma2Period { get => _ma2Period.Value; set => _ma2Period.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public XTraderV2Strategy()
	{
		_ma1Period = Param(nameof(Ma1Period), 16)
			.SetGreaterThanZero()
			.SetDisplay("MA1 Period", "Period for the first moving average", "Indicators");

		_ma2Period = Param(nameof(Ma2Period), 10)
			.SetGreaterThanZero()
			.SetDisplay("MA2 Period", "Period for the second moving average", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

	private void ProcessCandle(ICandleMessage candle, decimal ma1, decimal ma2)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_ma1Prev == 0)
		{
			_ma1Prev = ma1;
			_ma2Prev = ma2;
			return;
		}

		if (!_hasPrev2)
		{
			_ma1Prev2 = _ma1Prev;
			_ma2Prev2 = _ma2Prev;
			_ma1Prev = ma1;
			_ma2Prev = ma2;
			_hasPrev2 = true;
			return;
		}

		// Contrarian: sell when MA1 crosses above MA2, buy when crosses below
		var sellSignal = ma1 > ma2 && _ma1Prev > _ma2Prev && _ma1Prev2 < _ma2Prev2;
		var buySignal = ma1 < ma2 && _ma1Prev < _ma2Prev && _ma1Prev2 > _ma2Prev2;

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
		_ma1Prev = ma1;
		_ma2Prev = ma2;
	}
}

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on crossover of two moving averages.
/// Buys when fast MA crosses above slow MA, sells when crosses below.
/// </summary>
public class XAlert3Strategy : Strategy
{
	private readonly StrategyParam<int> _ma1Period;
	private readonly StrategyParam<int> _ma2Period;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;

	public int Ma1Period { get => _ma1Period.Value; set => _ma1Period.Value = value; }
	public int Ma2Period { get => _ma2Period.Value; set => _ma2Period.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public XAlert3Strategy()
	{
		_ma1Period = Param(nameof(Ma1Period), 10)
			.SetGreaterThanZero()
			.SetDisplay("MA1 Period", "Fast moving average period", "Indicators");

		_ma2Period = Param(nameof(Ma2Period), 30)
			.SetGreaterThanZero()
			.SetDisplay("MA2 Period", "Slow moving average period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = 0;
		_prevSlow = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast = new ExponentialMovingAverage { Length = Ma1Period };
		var slow = new ExponentialMovingAverage { Length = Ma2Period };

		SubscribeCandles(CandleType)
			.Bind(fast, slow, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_hasPrev = true;
			return;
		}

		if (_prevFast <= _prevSlow && fast > slow)
		{
			if (Position < 0) BuyMarket();
			if (Position <= 0) BuyMarket();
		}
		else if (_prevFast >= _prevSlow && fast < slow)
		{
			if (Position > 0) SellMarket();
			if (Position >= 0) SellMarket();
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}

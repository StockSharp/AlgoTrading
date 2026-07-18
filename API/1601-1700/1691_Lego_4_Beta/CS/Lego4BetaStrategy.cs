using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Modular strategy combining EMA crossover with RSI filter.
/// Enters on EMA cross with RSI confirmation, exits on RSI extremes or opposite cross.
/// </summary>
public class Lego4BetaStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;

	public int FastMaLength { get => _fastMaLength.Value; set => _fastMaLength.Value = value; }
	public int SlowMaLength { get => _slowMaLength.Value; set => _slowMaLength.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Lego4BetaStrategy()
	{
		_fastMaLength = Param(nameof(FastMaLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA length", "Indicators");
		_slowMaLength = Param(nameof(SlowMaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA length", "Indicators");
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = 0; _prevSlow = 0; _hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast = new ExponentialMovingAverage { Length = FastMaLength };
		var slow = new ExponentialMovingAverage { Length = SlowMaLength };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		SubscribeCandles(CandleType).Bind(fast, slow, rsi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal rsi)
	{
		if (candle.State != CandleStates.Finished) return;

		if (!_hasPrev) { _prevFast = fast; _prevSlow = slow; _hasPrev = true; return; }

		// EMA cross up + RSI not overbought => long
		if (_prevFast <= _prevSlow && fast > slow && rsi < 70)
		{
			if (Position < 0) BuyMarket();
			if (Position <= 0) BuyMarket();
		}
		// EMA cross down + RSI not oversold => short
		else if (_prevFast >= _prevSlow && fast < slow && rsi > 30)
		{
			if (Position > 0) SellMarket();
			if (Position >= 0) SellMarket();
		}
		// RSI exit: overbought close long
		else if (Position > 0 && rsi > 75)
		{
			SellMarket();
		}
		// RSI exit: oversold close short
		else if (Position < 0 && rsi < 25)
		{
			BuyMarket();
		}

		_prevFast = fast; _prevSlow = slow;
	}
}

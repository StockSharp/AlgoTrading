using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA slope reversal strategy.
/// Enters long when EMA turns up, short when EMA turns down.
/// </summary>
public class ExpMovingAverageFnStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevEma;
	private decimal _prevPrevEma;
	private int _count;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ExpMovingAverageFnStrategy()
	{
		_length = Param(nameof(Length), 12)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevEma = 0;
		_prevPrevEma = 0;
		_count = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = Length };

		SubscribeCandles(CandleType)
			.Bind(ema, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_count++;

		if (_count < 3)
		{
			_prevPrevEma = _prevEma;
			_prevEma = emaValue;
			return;
		}

		// Buy when EMA turns up
		var turnUp = _prevEma < _prevPrevEma && emaValue > _prevEma;
		// Sell when EMA turns down
		var turnDown = _prevEma > _prevPrevEma && emaValue < _prevEma;

		if (turnUp && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (turnDown && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevPrevEma = _prevEma;
		_prevEma = emaValue;
	}
}

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that follows ZLEMA direction changes for trend signals.
/// </summary>
public class ColorZeroLagMaStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevZlma;
	private decimal _prevPrevZlma;
	private int _count;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ColorZeroLagMaStrategy()
	{
		_length = Param(nameof(Length), 12)
			.SetGreaterThanZero()
			.SetDisplay("Length", "ZLEMA length", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevZlma = 0;
		_prevPrevZlma = 0;
		_count = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var zlma = new ZeroLagExponentialMovingAverage { Length = Length };

		SubscribeCandles(CandleType)
			.Bind(zlma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal zlmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_count++;

		if (_count < 3)
		{
			_prevPrevZlma = _prevZlma;
			_prevZlma = zlmaValue;
			return;
		}

		// Buy when ZLEMA turns up
		var turnUp = _prevZlma < _prevPrevZlma && zlmaValue > _prevZlma;
		// Sell when ZLEMA turns down
		var turnDown = _prevZlma > _prevPrevZlma && zlmaValue < _prevZlma;

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

		_prevPrevZlma = _prevZlma;
		_prevZlma = zlmaValue;
	}
}

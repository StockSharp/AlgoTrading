using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Center of Gravity OSMA strategy.
/// Uses SMA vs WMA difference (center of gravity concept) direction changes for signals.
/// </summary>
public class CenterOfGravityOsmaStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevOsma;
	private decimal _prevPrevOsma;
	private int _count;

	public int Period { get => _period.Value; set => _period.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public CenterOfGravityOsmaStrategy()
	{
		_period = Param(nameof(Period), 10)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Calculation period", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevOsma = 0;
		_prevPrevOsma = 0;
		_count = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = Period };
		var wma = new WeightedMovingAverage { Length = Period };

		SubscribeCandles(CandleType)
			.Bind(sma, wma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal wmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var osma = smaValue - wmaValue;
		_count++;

		if (_count < 3)
		{
			_prevPrevOsma = _prevOsma;
			_prevOsma = osma;
			return;
		}

		// Buy when OSMA turns up
		var turnUp = _prevOsma < _prevPrevOsma && osma > _prevOsma;
		// Sell when OSMA turns down
		var turnDown = _prevOsma > _prevPrevOsma && osma < _prevOsma;

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

		_prevPrevOsma = _prevOsma;
		_prevOsma = osma;
	}
}

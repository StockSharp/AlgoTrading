using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Adaptive market level strategy using SMA slope direction changes.
/// </summary>
public class AdaptiveMarketLevelStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevSma;
	private decimal _prevPrevSma;
	private int _count;

	public int Period { get => _period.Value; set => _period.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public AdaptiveMarketLevelStrategy()
	{
		_period = Param(nameof(Period), 14)
			.SetGreaterThanZero()
			.SetDisplay("Period", "SMA period", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevSma = 0;
		_prevPrevSma = 0;
		_count = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = Period };

		SubscribeCandles(CandleType)
			.Bind(sma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_count++;

		if (_count < 3)
		{
			_prevPrevSma = _prevSma;
			_prevSma = smaValue;
			return;
		}

		var turnUp = _prevSma < _prevPrevSma && smaValue > _prevSma;
		var turnDown = _prevSma > _prevPrevSma && smaValue < _prevSma;

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

		_prevPrevSma = _prevSma;
		_prevSma = smaValue;
	}
}

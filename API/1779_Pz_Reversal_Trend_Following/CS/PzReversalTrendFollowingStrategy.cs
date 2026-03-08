using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy using Highest/Lowest channels.
/// </summary>
public class PzReversalTrendFollowingStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevHighest;
	private decimal _prevLowest;
	private bool _hasPrev;

	public int Period { get => _period.Value; set => _period.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public PzReversalTrendFollowingStrategy()
	{
		_period = Param(nameof(Period), 30)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Lookback period for breakout", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevHighest = 0;
		_prevLowest = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var highest = new Highest { Length = Period };
		var lowest = new Lowest { Length = Period };

		SubscribeCandles(CandleType)
			.Bind(highest, lowest, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestValue, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (!_hasPrev)
		{
			_prevHighest = highestValue;
			_prevLowest = lowestValue;
			_hasPrev = true;
			return;
		}

		if (candle.ClosePrice > _prevHighest && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (candle.ClosePrice < _prevLowest && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevHighest = highestValue;
		_prevLowest = lowestValue;
	}
}

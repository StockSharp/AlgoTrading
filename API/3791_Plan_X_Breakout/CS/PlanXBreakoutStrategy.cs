using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Plan X Breakout strategy using highest high / lowest low channel breakout.
/// Buy when price breaks above the highest high of the lookback period.
/// Sell when price breaks below the lowest low of the lookback period.
/// </summary>
public class PlanXBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _hasPrev;

	public int Lookback { get => _lookback.Value; set => _lookback.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public PlanXBreakoutStrategy()
	{
		_lookback = Param(nameof(Lookback), 20)
			.SetDisplay("Lookback", "Channel lookback period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevHigh = 0m;
		_prevLow = 0m;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var highestHigh = new Highest { Length = Lookback };
		var lowestLow = new Lowest { Length = Lookback };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highestHigh, lowestLow, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevHigh = highest;
			_prevLow = lowest;
			_hasPrev = true;
			return;
		}

		// Breakout above previous highest high
		if (Position <= 0 && candle.ClosePrice > _prevHigh)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Breakout below previous lowest low
		else if (Position >= 0 && candle.ClosePrice < _prevLow)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevHigh = highest;
		_prevLow = lowest;
	}
}

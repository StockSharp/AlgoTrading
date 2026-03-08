using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy using Highest/Lowest channels (converted from grid).
/// </summary>
public class CollectorV10Strategy : Strategy
{
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _hasPrev;

	public int Lookback { get => _lookback.Value; set => _lookback.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public CollectorV10Strategy()
	{
		_lookback = Param(nameof(Lookback), 20)
			.SetDisplay("Lookback", "Channel lookback period", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevHigh = 0;
		_prevLow = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var highest = new Highest { Length = Lookback };
		var lowest = new Lowest { Length = Lookback };

		SubscribeCandles(CandleType).Bind(highest, lowest, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished) return;

		if (!_hasPrev)
		{
			_prevHigh = highest;
			_prevLow = lowest;
			_hasPrev = true;
			return;
		}

		var close = candle.ClosePrice;

		if (close > _prevHigh && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (close < _prevLow && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevHigh = highest;
		_prevLow = lowest;
	}
}

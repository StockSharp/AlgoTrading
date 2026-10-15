using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Gap fill strategy using Highest/Lowest channel breakout.
/// </summary>
public class GapFillStrategy : Strategy
{
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _hasPrev;

	public int ChannelPeriod { get => _channelPeriod.Value; set => _channelPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public GapFillStrategy()
	{
		_channelPeriod = Param(nameof(ChannelPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("Channel Period", "Highest/Lowest period", "Parameters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "Data");
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

		var highest = new Highest { Length = ChannelPeriod };
		var lowest = new Lowest { Length = ChannelPeriod };

		SubscribeCandles(CandleType)
			.Bind(highest, lowest, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal highVal, decimal lowVal)
	{
		if (candle.State != CandleStates.Finished) return;

		if (!_hasPrev)
		{
			_prevHigh = highVal;
			_prevLow = lowVal;
			_hasPrev = true;
			return;
		}

		if (candle.ClosePrice > _prevHigh && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (candle.ClosePrice < _prevLow && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevHigh = highVal;
		_prevLow = lowVal;
	}
}

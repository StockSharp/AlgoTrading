using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Donchian counter-channel system.
/// Counter-trend: buys when lower channel turns up (reversal from support).
/// Sells when upper channel turns down (reversal from resistance).
/// </summary>
public class DonchainCounterChannelSystemStrategy : Strategy
{
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevHigh;
	private decimal _prevLow;
	private decimal _prevPrevHigh;
	private decimal _prevPrevLow;
	private int _barCount;

	public int ChannelPeriod { get => _channelPeriod.Value; set => _channelPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public DonchainCounterChannelSystemStrategy()
	{
		_channelPeriod = Param(nameof(ChannelPeriod), 20)
			.SetDisplay("Channel Period", "Donchian channel lookback", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_barCount = 0;

		var highest = new Highest { Length = ChannelPeriod };
		var lowest = new Lowest { Length = ChannelPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barCount++;

		if (_barCount < 3)
		{
			_prevPrevHigh = _prevHigh;
			_prevPrevLow = _prevLow;
			_prevHigh = highest;
			_prevLow = lowest;
			return;
		}

		// Lower band turning up = support holding = buy signal
		var lowerTurningUp = lowest > _prevLow && _prevLow <= _prevPrevLow;
		// Upper band turning down = resistance holding = sell signal
		var upperTurningDown = highest < _prevHigh && _prevHigh >= _prevPrevHigh;

		if (lowerTurningUp && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		else if (upperTurningDown && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevPrevHigh = _prevHigh;
		_prevPrevLow = _prevLow;
		_prevHigh = highest;
		_prevLow = lowest;
	}
}

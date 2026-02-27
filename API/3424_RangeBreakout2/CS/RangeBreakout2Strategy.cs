namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Range Breakout 2 strategy: Donchian channel breakout.
/// Buys on new high breakout, sells on new low breakout.
/// </summary>
public class RangeBreakout2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _channelPeriod;

	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int ChannelPeriod { get => _channelPeriod.Value; set => _channelPeriod.Value = value; }

	public RangeBreakout2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_channelPeriod = Param(nameof(ChannelPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("Channel Period", "Highest/Lowest period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;
		var highest = new Highest { Length = ChannelPeriod };
		var lowest = new Lowest { Length = ChannelPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(highest, lowest, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal highValue, decimal lowValue)
	{
		if (candle.State != CandleStates.Finished) return;

		var close = candle.ClosePrice;

		if (_hasPrev)
		{
			if (close > _prevHigh && Position <= 0)
				BuyMarket();
			else if (close < _prevLow && Position >= 0)
				SellMarket();
		}

		_prevHigh = highValue;
		_prevLow = lowValue;
		_hasPrev = true;
	}
}

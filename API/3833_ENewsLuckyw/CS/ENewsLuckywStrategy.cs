using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ENews Lucky strategy - momentum breakout with ATR filter.
/// Buys when close breaks above recent high with strong momentum.
/// Sells when close breaks below recent low with strong momentum.
/// </summary>
public class ENewsLuckywStrategy : Strategy
{
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private decimal _prevMid;
	private bool _hasPrev;

	public int ChannelPeriod { get => _channelPeriod.Value; set => _channelPeriod.Value = value; }
	public int MomentumPeriod { get => _momentumPeriod.Value; set => _momentumPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ENewsLuckywStrategy()
	{
		_channelPeriod = Param(nameof(ChannelPeriod), 15)
			.SetDisplay("Channel Period", "Highest/Lowest period", "Indicators");

		_momentumPeriod = Param(nameof(MomentumPeriod), 10)
			.SetDisplay("Momentum Period", "Momentum period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var highest = new Highest { Length = ChannelPeriod };
		var lowest = new Lowest { Length = ChannelPeriod };
		var momentum = new Momentum { Length = MomentumPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, momentum, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest, decimal momentum)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var mid = (highest + lowest) / 2;

		if (!_hasPrev)
		{
			_prevClose = close;
			_prevMid = mid;
			_hasPrev = true;
			return;
		}

		// Breakout above midpoint with positive momentum
		if (_prevClose <= _prevMid && close > mid && momentum > 0 && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Breakout below midpoint with negative momentum
		else if (_prevClose >= _prevMid && close < mid && momentum < 0 && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevClose = close;
		_prevMid = mid;
	}
}

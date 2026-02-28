using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Contrarian MA strategy - mean reversion around SMA.
/// Buys when price crosses below SMA (contrarian dip buy).
/// Sells when price crosses above SMA (contrarian top sell).
/// Uses Highest/Lowest as extreme confirmation.
/// </summary>
public class ContrarianTradeMaWeeklyStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private decimal _prevSma;
	private bool _hasPrev;

	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public int ChannelPeriod { get => _channelPeriod.Value; set => _channelPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ContrarianTradeMaWeeklyStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 14)
			.SetDisplay("SMA Period", "SMA period", "Indicators");

		_channelPeriod = Param(nameof(ChannelPeriod), 10)
			.SetDisplay("Channel Period", "Highest/Lowest lookback", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var sma = new SimpleMovingAverage { Length = MaPeriod };
		var highest = new Highest { Length = ChannelPeriod };
		var lowest = new Lowest { Length = ChannelPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, highest, lowest, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal sma, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		if (!_hasPrev)
		{
			_prevClose = close;
			_prevSma = sma;
			_hasPrev = true;
			return;
		}

		var mid = (highest + lowest) / 2;

		// Contrarian buy: price crosses below SMA and is near the low
		if (_prevClose >= _prevSma && close < sma && close < mid && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Contrarian sell: price crosses above SMA and is near the high
		else if (_prevClose <= _prevSma && close > sma && close > mid && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevClose = close;
		_prevSma = sma;
	}
}

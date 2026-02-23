using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class DonchianScalperStrategy : Strategy
{
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema;
	private decimal _prevUpper;
	private decimal _prevLower;
	private decimal _prevEma;
	private bool _hasPrev;
	private int _barCount;

	public int ChannelPeriod
	{
		get => _channelPeriod.Value;
		set => _channelPeriod.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public DonchianScalperStrategy()
	{
		_channelPeriod = Param(nameof(ChannelPeriod), 20);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ema = new ExponentialMovingAverage { Length = ChannelPeriod };
		var donchian = new DonchianChannels { Length = ChannelPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(donchian, _ema, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue donchianValue, IIndicatorValue emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!donchianValue.IsFinal || !emaValue.IsFinal)
			return;

		if (donchianValue is not DonchianChannelsValue dcValue)
			return;

		if (dcValue.UpperBand is not decimal upper ||
			dcValue.LowerBand is not decimal lower ||
			dcValue.Middle is not decimal middle)
			return;

		var ema = emaValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		_barCount++;
		if (_barCount < ChannelPeriod + 2)
		{
			_prevUpper = upper;
			_prevLower = lower;
			_prevEma = ema;
			_hasPrev = true;
			return;
		}

		if (!_hasPrev)
		{
			_prevUpper = upper;
			_prevLower = lower;
			_prevEma = ema;
			_hasPrev = true;
			return;
		}

		// Long: close breaks above upper Donchian and is above EMA
		if (Position <= 0 && close >= upper && close > ema)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Short: close breaks below lower Donchian and is below EMA
		else if (Position >= 0 && close <= lower && close < ema)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}
		// Exit long at middle band
		else if (Position > 0 && close < middle)
		{
			SellMarket();
		}
		// Exit short at middle band
		else if (Position < 0 && close > middle)
		{
			BuyMarket();
		}

		_prevUpper = upper;
		_prevLower = lower;
		_prevEma = ema;
	}
}

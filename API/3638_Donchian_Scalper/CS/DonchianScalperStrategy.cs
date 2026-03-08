using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Donchian Channel scalper with EMA filter.
/// Buys on upper channel breakout above EMA, sells on lower breakout below EMA.
/// Exits at middle band.
/// </summary>
public class DonchianScalperStrategy : Strategy
{
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<DataType> _candleType;

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
		_channelPeriod = Param(nameof(ChannelPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Channel Period", "Donchian channel lookback", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var donchian = new DonchianChannels { Length = ChannelPeriod };
		var ema = new ExponentialMovingAverage { Length = ChannelPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(donchian, ema, (candle, donchianVal, emaVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (donchianVal is not DonchianChannelsValue dcValue)
					return;

				if (dcValue.UpperBand is not decimal upper ||
					dcValue.LowerBand is not decimal lower ||
					dcValue.Middle is not decimal middle)
					return;

				if (emaVal.IsEmpty)
					return;

				var emaValue = emaVal.GetValue<decimal>();
				var close = candle.ClosePrice;

				// Long: close breaks above upper Donchian and is above EMA
				if (Position <= 0 && close >= upper && close > emaValue)
					BuyMarket();
				// Short: close breaks below lower Donchian and is below EMA
				else if (Position >= 0 && close <= lower && close < emaValue)
					SellMarket();
				// Exit long at middle band
				else if (Position > 0 && close < middle)
					SellMarket();
				// Exit short at middle band
				else if (Position < 0 && close > middle)
					BuyMarket();
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, donchian);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}
}

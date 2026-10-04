using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Donchian Channel.
/// It enters long when the close breaks above the upper band of the previous candles and short below the lower band,
/// and exits when the close returns to the channel midpoint.
/// </summary>
public class DonchianChannelStrategy : Strategy
{
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<DataType> _candleType;

	// Current state
	// Channel of the candles before the current one.
	private decimal? _prevUpperBand;
	private decimal? _prevLowerBand;
	private decimal? _prevMiddle;

	/// <summary>
	/// Period for Donchian Channel.
	/// </summary>
	public int ChannelPeriod
	{
		get => _channelPeriod.Value;
		set => _channelPeriod.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize the Donchian Channel strategy.
	/// </summary>
	public DonchianChannelStrategy()
	{
		_channelPeriod = Param(nameof(ChannelPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Channel Period", "Period for Donchian Channel calculation", "Indicators")
			
			.SetOptimize(10, 50, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevUpperBand = null;
		_prevLowerBand = null;
		_prevMiddle = null;

	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Create indicators
		var donchian = new DonchianChannels { Length = ChannelPeriod };

		// Create subscription and bind indicators
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(donchian, ProcessCandle)
			.Start();

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, donchian);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue donchianValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var donchianTyped = (DonchianChannelsValue)donchianValue;

		if (donchianTyped.UpperBand is not decimal upperValue ||
			donchianTyped.LowerBand is not decimal lowerValue ||
			donchianTyped.Middle is not decimal midValue)
		{
			return;
		}

		// A close can only break out of the channel the candles before it formed.
		var upper = _prevUpperBand;
		var lower = _prevLowerBand;
		var middle = _prevMiddle;

		_prevUpperBand = upperValue;
		_prevLowerBand = lowerValue;
		_prevMiddle = midValue;

		if (upper is not decimal up || lower is not decimal down || middle is not decimal mid)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (close > up && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (close < down && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
		else if (Position > 0 && close <= mid)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && close >= mid)
		{
			BuyMarket(-Position);
		}
	}
}

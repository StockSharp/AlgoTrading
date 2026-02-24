using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Price Channel Signal v2 strategy that reacts to Donchian channel breakouts.
/// </summary>
public class PriceChannelSignalV2Strategy : Strategy
{
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private DonchianChannels _donchian;
	private int _previousTrend;
	private decimal? _previousClose;
	private decimal? _previousUpper;
	private decimal? _previousLower;

	/// <summary>
	/// Channel lookback length.
	/// </summary>
	public int ChannelPeriod
	{
		get => _channelPeriod.Value;
		set => _channelPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize a new instance of <see cref="PriceChannelSignalV2Strategy"/>.
	/// </summary>
	public PriceChannelSignalV2Strategy()
	{
		_channelPeriod = Param(nameof(ChannelPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Channel Period", "Donchian lookback used for Price Channel", "Price Channel");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for Price Channel", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_previousTrend = 0;
		_previousClose = null;
		_previousUpper = null;
		_previousLower = null;

		_donchian = new DonchianChannels { Length = ChannelPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_donchian, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _donchian);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue donchianValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_donchian.IsFormed)
		{
			_previousClose = candle.ClosePrice;
			return;
		}

		var channel = (IDonchianChannelsValue)donchianValue;
		if (channel.UpperBand is not decimal channelHigh || channel.LowerBand is not decimal channelLow)
		{
			_previousClose = candle.ClosePrice;
			return;
		}

		var range = channelHigh - channelLow;
		if (range <= 0m)
		{
			_previousClose = candle.ClosePrice;
			return;
		}

		var mid = (channelHigh + channelLow) / 2m;

		// Update trend state based on channel breakout
		var trend = _previousTrend;
		if (candle.ClosePrice > channelHigh - range * 0.1m)
			trend = 1;
		else if (candle.ClosePrice < channelLow + range * 0.1m)
			trend = -1;

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		// Trend reversal signals
		if (trend > 0 && _previousTrend <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			if (Position <= 0)
				BuyMarket(volume);
		}
		else if (trend < 0 && _previousTrend >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));

			if (Position >= 0)
				SellMarket(volume);
		}

		// Exit on mid-line cross
		if (Position > 0 && _previousClose is decimal pc1 && pc1 >= mid && candle.ClosePrice < mid)
		{
			SellMarket(Math.Abs(Position));
		}
		else if (Position < 0 && _previousClose is decimal pc2 && pc2 <= mid && candle.ClosePrice > mid)
		{
			BuyMarket(Math.Abs(Position));
		}

		_previousTrend = trend;
		_previousClose = candle.ClosePrice;
		_previousUpper = channelHigh;
		_previousLower = channelLow;
	}
}

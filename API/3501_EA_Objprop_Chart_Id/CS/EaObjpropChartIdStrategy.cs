using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Visualization focused strategy inspired by the EA_OBJPROP_CHART_ID example.
/// The strategy hosts Donchian Channels on the primary timeframe together with
/// helper panels for H4 and Daily context to mimic the original chart setup.
/// </summary>
public class EaObjpropChartIdStrategy : Strategy
{
	private readonly StrategyParam<int> _channelLength;
	private readonly StrategyParam<DataType> _primaryCandleType;
	private readonly StrategyParam<DataType> _h4CandleType;
	private readonly StrategyParam<DataType> _dailyCandleType;

	private DonchianChannels _primaryChannel = null!;
	private DonchianChannels _h4Channel = null!;
	private DonchianChannels _dailyChannel = null!;

	private decimal _primaryUpper;
	private decimal _primaryLower;
	private decimal _h4Upper;
	private decimal _h4Lower;
	private decimal _dailyUpper;
	private decimal _dailyLower;

	/// <summary>
	/// Donchian Channel length applied to all timeframes.
	/// </summary>
	public int ChannelLength
	{
		get => _channelLength.Value;
		set => _channelLength.Value = value;
	}

	/// <summary>
	/// Main timeframe used for trading and charting.
	/// </summary>
	public DataType PrimaryCandleType
	{
		get => _primaryCandleType.Value;
		set => _primaryCandleType.Value = value;
	}

	/// <summary>
	/// Auxiliary H4 timeframe used for context.
	/// </summary>
	public DataType H4CandleType
	{
		get => _h4CandleType.Value;
		set => _h4CandleType.Value = value;
	}

	/// <summary>
	/// Auxiliary Daily timeframe used for context.
	/// </summary>
	public DataType DailyCandleType
	{
		get => _dailyCandleType.Value;
		set => _dailyCandleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="EaObjpropChartIdStrategy"/> class.
	/// </summary>
	public EaObjpropChartIdStrategy()
	{
		_channelLength = Param(nameof(ChannelLength), 22)
			.SetGreaterThanZero()
			.SetDisplay("Channel Length", "Length of Donchian Channel", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 2);

		_primaryCandleType = Param(nameof(PrimaryCandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Primary Candle", "Primary timeframe for the strategy", "General");

		_h4CandleType = Param(nameof(H4CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("H4 Candle", "H4 context timeframe", "General");

		_dailyCandleType = Param(nameof(DailyCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Daily Candle", "Daily context timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, PrimaryCandleType);
		yield return (Security, H4CandleType);
		yield return (Security, DailyCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_primaryChannel?.Reset();
		_h4Channel?.Reset();
		_dailyChannel?.Reset();

		_primaryUpper = _primaryLower = 0m;
		_h4Upper = _h4Lower = 0m;
		_dailyUpper = _dailyLower = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Prepare Donchian Channels for each timeframe to replicate the original chart layout.
		_primaryChannel = new DonchianChannels { Length = ChannelLength };
		_h4Channel = new DonchianChannels { Length = ChannelLength };
		_dailyChannel = new DonchianChannels { Length = ChannelLength };

		var primarySubscription = SubscribeCandles(PrimaryCandleType);
		var h4Subscription = SubscribeCandles(H4CandleType);
		var dailySubscription = SubscribeCandles(DailyCandleType);

		primarySubscription
			.BindEx(_primaryChannel, ProcessPrimary)
			.Start();

		h4Subscription
			.BindEx(_h4Channel, ProcessH4)
			.Start();

		dailySubscription
			.BindEx(_dailyChannel, ProcessDaily)
			.Start();

		var mainArea = CreateChartArea("Primary Price Channel");
		if (mainArea != null)
		{
			// Display main candles with their Donchian Channel boundaries.
			DrawCandles(mainArea, primarySubscription);
			DrawIndicator(mainArea, _primaryChannel);
			DrawOwnTrades(mainArea);
		}

		var h4Area = CreateChartArea("H4 Context");
		if (h4Area != null)
		{
			// Provide the H4 overview in a separate panel similar to the MQL subwindow.
			DrawCandles(h4Area, h4Subscription);
			DrawIndicator(h4Area, _h4Channel);
		}

		var dailyArea = CreateChartArea("Daily Context");
		if (dailyArea != null)
		{
			// Provide the daily overview in another dedicated panel.
			DrawCandles(dailyArea, dailySubscription);
			DrawIndicator(dailyArea, _dailyChannel);
		}
	}

	private void ProcessPrimary(ICandleMessage candle, IIndicatorValue channelValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var donchian = (DonchianChannelsValue)channelValue;

		if (donchian.Upper is not decimal upper || donchian.Lower is not decimal lower)
			return;

		// Store the latest channel boundaries for potential trade logic extensions.
		_primaryUpper = upper;
		_primaryLower = lower;
	}

	private void ProcessH4(ICandleMessage candle, IIndicatorValue channelValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var donchian = (DonchianChannelsValue)channelValue;

		if (donchian.Upper is not decimal upper || donchian.Lower is not decimal lower)
			return;

		// Keep H4 channel levels for comparison with the primary timeframe.
		_h4Upper = upper;
		_h4Lower = lower;
	}

	private void ProcessDaily(ICandleMessage candle, IIndicatorValue channelValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var donchian = (DonchianChannelsValue)channelValue;

		if (donchian.Upper is not decimal upper || donchian.Lower is not decimal lower)
			return;

		// Keep Daily channel levels to mirror the MQL dashboard behavior.
		_dailyUpper = upper;
		_dailyLower = lower;
	}
}

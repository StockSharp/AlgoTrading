using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Support and resistance breakout detector based on Donchian channels.
/// Generates informational logs when the candle close crosses above resistance or below support.
/// </summary>
public class SrBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackLength;
	private readonly StrategyParam<DataType> _hour1CandleType;
	private readonly StrategyParam<DataType> _hour4CandleType;

	private DonchianChannels _hour1Donchian = null!;
	private DonchianChannels _hour4Donchian = null!;

	private decimal? _previousHour1Close;
	private decimal? _previousHour4Close;

	/// <summary>
	/// Number of candles used to calculate support and resistance bounds.
	/// </summary>
	public int LookbackLength
	{
		get => _lookbackLength.Value;
		set => _lookbackLength.Value = value;
	}

	/// <summary>
	/// Candle type used for the one-hour analysis.
	/// </summary>
	public DataType Hour1CandleType
	{
		get => _hour1CandleType.Value;
		set => _hour1CandleType.Value = value;
	}

	/// <summary>
	/// Candle type used for the four-hour analysis.
	/// </summary>
	public DataType Hour4CandleType
	{
		get => _hour4CandleType.Value;
		set => _hour4CandleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="SrBreakoutStrategy"/>.
	/// </summary>
	public SrBreakoutStrategy()
	{
		_lookbackLength = Param(nameof(LookbackLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Length", "Number of candles for support/resistance detection", "General")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 5);

		_hour1CandleType = Param(nameof(Hour1CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("H1 Candle Type", "Candle type for 1-hour support/resistance", "Data");

		_hour4CandleType = Param(nameof(Hour4CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("H4 Candle Type", "Candle type for 4-hour support/resistance", "Data");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, Hour1CandleType);
		yield return (Security, Hour4CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousHour1Close = null;
		_previousHour4Close = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_hour1Donchian = new DonchianChannels { Length = LookbackLength };
		_hour4Donchian = new DonchianChannels { Length = LookbackLength };

		var hour1Subscription = SubscribeCandles(Hour1CandleType);
		var hour4Subscription = SubscribeCandles(Hour4CandleType);

		hour1Subscription
			.BindEx(_hour1Donchian, ProcessHour1Candle)
			.Start();

		hour4Subscription
			.BindEx(_hour4Donchian, ProcessHour4Candle)
			.Start();
	}

	private void ProcessHour1Candle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		ProcessBreakout(candle, _hour1Donchian, indicatorValue, ref _previousHour1Close, "H1");
	}

	private void ProcessHour4Candle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		ProcessBreakout(candle, _hour4Donchian, indicatorValue, ref _previousHour4Close, "H4");
	}

	private void ProcessBreakout(ICandleMessage candle, DonchianChannels indicator, IIndicatorValue indicatorValue, ref decimal? previousClose, string label)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!indicator.IsFormed)
			return;

		if (!indicatorValue.IsFinal)
			return;

		var channel = (DonchianChannelsValue)indicatorValue;

		if (channel.UpBand is not decimal resistance || channel.LowBand is not decimal support)
			return;

		var currentClose = candle.ClosePrice;

		if (previousClose is not decimal previous)
		{
			// Store the first close to enable crossover checks on subsequent candles.
			previousClose = currentClose;
			LogInfo($"{label} channel ready. Support: {support:F5}, Resistance: {resistance:F5}.");
			return;
		}

		var candleTime = candle.CloseTime ?? candle.OpenTime;

		if (previous <= resistance && currentClose > resistance)
		{
			LogInfo($"{label} close crossed above resistance {resistance:F5} at {currentClose:F5} ({candleTime:O}).");
		}
		else if (previous >= support && currentClose < support)
		{
			LogInfo($"{label} close crossed below support {support:F5} at {currentClose:F5} ({candleTime:O}).");
		}

		previousClose = currentClose;
	}
}

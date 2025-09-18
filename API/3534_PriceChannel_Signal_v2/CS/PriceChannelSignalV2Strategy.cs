using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Price Channel Signal v2 strategy that reacts to trend reversals, re-entries and exit levels of the channel.
/// </summary>
public class PriceChannelSignalV2Strategy : Strategy
{
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<decimal> _riskFactor;
	private readonly StrategyParam<decimal> _exitLevel;
	private readonly StrategyParam<bool> _useReEntry;
	private readonly StrategyParam<bool> _useExitSignals;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _useTimeControl;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _endMinute;

	private DonchianChannels _donchian = null!;
	private AverageTrueRange _atr = null!;

	private int _previousTrend;
	private decimal? _previousUpBand;
	private decimal? _previousLowBand;
	private decimal? _previousUpExit;
	private decimal? _previousLowExit;
	private decimal? _previousClose;
	private DateTimeOffset? _lastLongEntryBarTime;
	private DateTimeOffset? _lastShortEntryBarTime;

	/// <summary>
	/// Channel lookback length.
	/// </summary>
	public int ChannelPeriod
	{
		get => _channelPeriod.Value;
		set => _channelPeriod.Value = value;
	}

	/// <summary>
	/// Risk factor that shifts the entry channel boundaries.
	/// </summary>
	public decimal RiskFactor
	{
		get => _riskFactor.Value;
		set => _riskFactor.Value = value;
	}

	/// <summary>
	/// Exit level factor relative to the Donchian range.
	/// </summary>
	public decimal ExitLevel
	{
		get => _exitLevel.Value;
		set => _exitLevel.Value = value;
	}

	/// <summary>
	/// Enable Price Channel re-entry signals.
	/// </summary>
	public bool UseReEntry
	{
		get => _useReEntry.Value;
		set => _useReEntry.Value = value;
	}

	/// <summary>
	/// Enable Price Channel exit signals.
	/// </summary>
	public bool UseExitSignals
	{
		get => _useExitSignals.Value;
		set => _useExitSignals.Value = value;
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
	/// Toggle intraday time control.
	/// </summary>
	public bool UseTimeControl
	{
		get => _useTimeControl.Value;
		set => _useTimeControl.Value = value;
	}

	/// <summary>
	/// Inclusive start hour of the trading window (0-23).
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Inclusive start minute of the trading window (0-59).
	/// </summary>
	public int StartMinute
	{
		get => _startMinute.Value;
		set => _startMinute.Value = value;
	}

	/// <summary>
	/// Exclusive end hour of the trading window (0-23).
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Exclusive end minute of the trading window (0-59).
	/// </summary>
	public int EndMinute
	{
		get => _endMinute.Value;
		set => _endMinute.Value = value;
	}

	/// <summary>
	/// Initialize a new instance of <see cref="PriceChannelSignalV2Strategy"/>.
	/// </summary>
	public PriceChannelSignalV2Strategy()
	{
		_channelPeriod = Param(nameof(ChannelPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Channel Period", "Donchian lookback used for Price Channel", "Price Channel")
			.SetCanOptimize(true);

		_riskFactor = Param(nameof(RiskFactor), 3m)
			.SetRange(0m, 10m)
			.SetDisplay("Risk Factor", "Risk factor shifting entry bands", "Price Channel")
			.SetCanOptimize(true);

		_exitLevel = Param(nameof(ExitLevel), 5m)
			.SetRange(0m, 33m)
			.SetDisplay("Exit Level", "Exit factor shifting protective bands", "Price Channel")
			.SetCanOptimize(true);

		_useReEntry = Param(nameof(UseReEntry), true)
			.SetDisplay("Enable Re-Entry", "Allow channel re-entry trades", "Signals");

		_useExitSignals = Param(nameof(UseExitSignals), true)
			.SetDisplay("Enable Exits", "Allow channel exit signals", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for Price Channel", "General");

		_useTimeControl = Param(nameof(UseTimeControl), false)
			.SetDisplay("Use Time Control", "Restrict trading to a daily window", "Timing");

		_startHour = Param(nameof(StartHour), 10)
			.SetRange(0, 23)
			.SetDisplay("Start Hour", "Inclusive window start hour", "Timing");

		_startMinute = Param(nameof(StartMinute), 1)
			.SetRange(0, 59)
			.SetDisplay("Start Minute", "Inclusive window start minute", "Timing");

		_endHour = Param(nameof(EndHour), 15)
			.SetRange(0, 23)
			.SetDisplay("End Hour", "Exclusive window end hour", "Timing");

		_endMinute = Param(nameof(EndMinute), 2)
			.SetRange(0, 59)
			.SetDisplay("End Minute", "Exclusive window end minute", "Timing");
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

		_previousTrend = 0;
		_previousUpBand = null;
		_previousLowBand = null;
		_previousUpExit = null;
		_previousLowExit = null;
		_previousClose = null;
		_lastLongEntryBarTime = null;
		_lastShortEntryBarTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (ExitLevel <= RiskFactor)
			throw new InvalidOperationException("Exit level must be greater than risk factor to create meaningful exit bands.");

		_donchian = new DonchianChannels { Length = ChannelPeriod };
		_atr = new AverageTrueRange { Length = ChannelPeriod };

		if (Volume <= 0m)
			Volume = 1m;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_donchian, _atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _donchian);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue donchianValue, IIndicatorValue atrValue)
	{
		// Only analyze fully completed candles to avoid premature decisions.
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure indicators are fully formed before producing trading logic.
		if (!_donchian.IsFormed || !_atr.IsFormed)
		{
			_previousClose = candle.ClosePrice;
			return;
		}

		var channel = (DonchianChannelsValue)donchianValue;
		if (channel.UpBand is not decimal channelHigh || channel.LowBand is not decimal channelLow)
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

		var riskShift = (33m - RiskFactor) * range / 100m;
		var exitShift = (33m - ExitLevel) * range / 100m;

		var upBand = channelHigh - riskShift;
		var lowBand = channelLow + riskShift;
		var upExit = channelHigh - exitShift;
		var lowExit = channelLow + exitShift;

		var previousTrend = _previousTrend;
		var trend = previousTrend;

		// Update trend state whenever price leaves the adjusted channel.
		if (candle.ClosePrice > upBand)
			trend = 1;
		else if (candle.ClosePrice < lowBand)
			trend = -1;

		var barTime = candle.OpenTime;
		var insideWindow = IsWithinTradingWindow(barTime);
		var canTrade = insideWindow && IsFormedAndOnlineAndAllowTrading();

		var longSignal = insideWindow && trend > 0 && previousTrend < 0;
		var shortSignal = insideWindow && trend < 0 && previousTrend > 0;

		var hasPreviousUp = _previousUpBand is decimal prevUpBand && _previousClose is decimal prevClose;
		var hasPreviousLow = _previousLowBand is decimal prevLowBand && _previousClose is decimal prevClose2;
		var hasPreviousUpExit = _previousUpExit is decimal prevUpExit && _previousClose is decimal prevCloseExitUp;
		var hasPreviousLowExit = _previousLowExit is decimal prevLowExit && _previousClose is decimal prevCloseExitLow;

		var longReEntry = insideWindow && UseReEntry && trend > 0 && hasPreviousUp && prevClose <= prevUpBand && candle.ClosePrice > upBand;
		var shortReEntry = insideWindow && UseReEntry && trend < 0 && hasPreviousLow && prevClose2 >= prevLowBand && candle.ClosePrice < lowBand;

		var longExit = insideWindow && UseExitSignals && hasPreviousUpExit && prevCloseExitUp >= prevUpExit && candle.ClosePrice < upExit;
		var shortExit = insideWindow && UseExitSignals && hasPreviousLowExit && prevCloseExitLow <= prevLowExit && candle.ClosePrice > lowExit;

		// Execute long exits before evaluating new entries.
		if (longExit && Position > 0m)
		{
			// Close all long exposure when the price breaks below the exit band.
			SellMarket(Position);
		}

		if (shortExit && Position < 0m)
		{
			// Close all short exposure when the price breaks above the exit band.
			BuyMarket(Math.Abs(Position));
		}

		if ((longSignal || longReEntry) && Position == 0m && canTrade)
		{
			// Allow only one buy order per bar to mirror the MQL implementation.
			if (_lastLongEntryBarTime != barTime)
			{
				var volume = Volume > 0m ? Volume : 1m;
				BuyMarket(volume);
				_lastLongEntryBarTime = barTime;
			}
		}
		else if ((shortSignal || shortReEntry) && Position == 0m && canTrade)
		{
			// Allow only one sell order per bar to mirror the MQL implementation.
			if (_lastShortEntryBarTime != barTime)
			{
				var volume = Volume > 0m ? Volume : 1m;
				SellMarket(volume);
				_lastShortEntryBarTime = barTime;
			}
		}

		_previousTrend = trend;
		_previousUpBand = upBand;
		_previousLowBand = lowBand;
		_previousUpExit = upExit;
		_previousLowExit = lowExit;
		_previousClose = candle.ClosePrice;
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		if (!UseTimeControl)
			return true;

		var startMinutes = StartHour * 60 + StartMinute;
		var endMinutes = EndHour * 60 + EndMinute;
		var currentMinutes = time.Hour * 60 + time.Minute;

		if (startMinutes == endMinutes)
			return false;

		if (startMinutes < endMinutes)
			return currentMinutes >= startMinutes && currentMinutes < endMinutes;

		return currentMinutes >= startMinutes || currentMinutes < endMinutes;
	}
}

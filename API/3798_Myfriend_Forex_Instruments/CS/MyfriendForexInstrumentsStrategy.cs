namespace StockSharp.Samples.Strategies;

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

/// <summary>
/// Recreates the "MyFriend" MetaTrader expert using high-level StockSharp API.
/// The strategy combines daily pivot levels, Donchian channel expansion and a short-vs-long close momentum spread.
/// </summary>
public class MyfriendForexInstrumentsStrategy : Strategy
{
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossBufferPoints;
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<int> _trailingStartPoints;
	private readonly StrategyParam<int> _trailingBufferPoints;
	private readonly StrategyParam<bool> _useTimeClose;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _dailyCandleType;

	private DonchianChannels _donchian = null!;
	private SimpleMovingAverage _shortSma = null!;
	private SimpleMovingAverage _longSma = null!;

	private decimal? _prevUpper;
	private decimal? _prevLower;
	private CandleSnapshot? _previousCandle;

	private decimal? _prevDailyHigh;
	private decimal? _prevDailyLow;
	private decimal? _prevDailyClose;
	private decimal? _lastDailyHigh;
	private decimal? _lastDailyLow;
	private decimal? _lastDailyClose;

	private decimal? _pivot;
	private decimal? _r1;
	private decimal? _s1;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private DateTimeOffset? _longEntryTime;
	private DateTimeOffset? _shortEntryTime;

	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;

	private decimal? _pendingLongStop;
	private decimal? _pendingLongTake;
	private decimal? _pendingShortStop;
	private decimal? _pendingShortTake;

	/// <summary>
	/// Base volume used when opening new positions.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in MetaTrader points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Additional buffer for stop losses in MetaTrader points.
	/// </summary>
	public int StopLossBufferPoints
	{
		get => _stopLossBufferPoints.Value;
		set => _stopLossBufferPoints.Value = value;
	}

	/// <summary>
	/// Period of the Donchian channel used for breadth checks and trailing stops.
	/// </summary>
	public int ChannelPeriod
	{
		get => _channelPeriod.Value;
		set => _channelPeriod.Value = value;
	}

	/// <summary>
	/// Enables the Donchian-based trailing logic.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Minimal open profit (in points) required before tightening stops.
	/// </summary>
	public int TrailingStartPoints
	{
		get => _trailingStartPoints.Value;
		set => _trailingStartPoints.Value = value;
	}

	/// <summary>
	/// Buffer added to Donchian boundaries when trailing positions.
	/// </summary>
	public int TrailingBufferPoints
	{
		get => _trailingBufferPoints.Value;
		set => _trailingBufferPoints.Value = value;
	}

	/// <summary>
	/// Enables the time-based exit window inherited from the original expert.
	/// </summary>
	public bool UseTimeClose
	{
		get => _useTimeClose.Value;
		set => _useTimeClose.Value = value;
	}

	/// <summary>
	/// Primary candle type used for intraday processing (defaults to M30).
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Daily candle type used to rebuild pivot levels from the previous session.
	/// </summary>
	public DataType DailyCandleType
	{
		get => _dailyCandleType.Value;
		set => _dailyCandleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="MyfriendForexInstrumentsStrategy"/>.
	/// </summary>
	public MyfriendForexInstrumentsStrategy()
	{
		_baseVolume = Param(nameof(BaseVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Base Volume", "Default order volume", "Trading");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 70)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit (points)", "Take profit distance in MetaTrader points", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(20, 150, 5);

		_stopLossBufferPoints = Param(nameof(StopLossBufferPoints), 13)
		.SetGreaterThanZero()
		.SetDisplay("Stop Buffer (points)", "Extra stop buffer in MetaTrader points", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(5, 40, 1);

		_channelPeriod = Param(nameof(ChannelPeriod), 16)
		.SetGreaterThanZero()
		.SetDisplay("Channel Period", "Donchian channel period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 40, 1);

		_useTrailingStop = Param(nameof(UseTrailingStop), true)
		.SetDisplay("Use Trailing", "Activate Donchian trailing stop", "Risk Management");

		_trailingStartPoints = Param(nameof(TrailingStartPoints), 5)
		.SetGreaterThanZero()
		.SetDisplay("Trailing Start", "Minimal profit before trailing in points", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(3, 20, 1);

		_trailingBufferPoints = Param(nameof(TrailingBufferPoints), 1)
		.SetGreaterThanZero()
		.SetDisplay("Trailing Buffer", "Distance from Donchian boundary for the trailing stop", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(1, 10, 1);

		_useTimeClose = Param(nameof(UseTimeClose), true)
		.SetDisplay("Use Time Close", "Close within the 3-4 candle window if price rejects the entry", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("Primary Candle", "Intraday candle type (default M30)", "General");

		_dailyCandleType = Param(nameof(DailyCandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Daily Candle", "Daily candle type used for pivots", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, DailyCandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevUpper = null;
		_prevLower = null;
		_previousCandle = null;

		_prevDailyHigh = null;
		_prevDailyLow = null;
		_prevDailyClose = null;
		_lastDailyHigh = null;
		_lastDailyLow = null;
		_lastDailyClose = null;

		_pivot = null;
		_r1 = null;
		_s1 = null;

		ResetPositionState();

		_pendingLongStop = null;
		_pendingLongTake = null;
		_pendingShortStop = null;
		_pendingShortTake = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = BaseVolume;

		_donchian = new DonchianChannels
		{
			Length = ChannelPeriod
		};

		_shortSma = new SimpleMovingAverage
		{
			Length = 3
		};

		_longSma = new SimpleMovingAverage
		{
			Length = 9
		};

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription
		.BindEx(_donchian, ProcessCandle)
		.Start();

		var dailySubscription = SubscribeCandles(DailyCandleType);
		dailySubscription
		.Bind(ProcessDaily)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawIndicator(area, _donchian);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position > 0m)
		{
			_longEntryPrice = PositionPrice;
			_longEntryTime = CurrentTime;
			_shortEntryPrice = null;
			_shortEntryTime = null;
			_shortStop = null;
			_shortTake = null;
			_pendingShortStop = null;
			_pendingShortTake = null;

			_longStop = _pendingLongStop;
			_longTake = _pendingLongTake;
			_pendingLongStop = null;
			_pendingLongTake = null;
		}
		else if (Position < 0m)
		{
			_shortEntryPrice = PositionPrice;
			_shortEntryTime = CurrentTime;
			_longEntryPrice = null;
			_longEntryTime = null;
			_longStop = null;
			_longTake = null;
			_pendingLongStop = null;
			_pendingLongTake = null;

			_shortStop = _pendingShortStop;
			_shortTake = _pendingShortTake;
			_pendingShortStop = null;
			_pendingShortTake = null;
		}
		else
		{
			ResetPositionState();
		}
	}

	private void ProcessDaily(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_prevDailyHigh = _lastDailyHigh;
		_prevDailyLow = _lastDailyLow;
		_prevDailyClose = _lastDailyClose;

		_lastDailyHigh = candle.HighPrice;
		_lastDailyLow = candle.LowPrice;
		_lastDailyClose = candle.ClosePrice;

		if (_prevDailyHigh is decimal high && _prevDailyLow is decimal low && _prevDailyClose is decimal close)
		{
			var pivot = (high + low + close) / 3m;
			var s1 = 2m * pivot - high;
			var r1 = 2m * pivot - low;

			_pivot = pivot;
			_s1 = s1;
			_r1 = r1;
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue donchianValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!donchianValue.IsFinal)
		return;

		var donchian = (DonchianChannelsValue)donchianValue;
		if (donchian.Upper is not decimal upper || donchian.Lower is not decimal lower)
		return;

		var shortValue = _shortSma.Process(candle.ClosePrice);
		var longValue = _longSma.Process(candle.ClosePrice);

		var step = GetPriceStep();
		var timeFrame = GetMainTimeFrame();

		ManageOpenPositions(candle, upper, lower, step, timeFrame);

		if (!_shortSma.IsFormed || !_longSma.IsFormed)
		{
			StoreState(candle, upper, lower);
			return;
		}

		if (_pivot is not decimal pivot || _r1 is not decimal r1 || _s1 is not decimal s1)
		{
			StoreState(candle, upper, lower);
			return;
		}

		var mp = (shortValue.ToDecimal() - longValue.ToDecimal()) * 1000m;
		var prevCandle = _previousCandle;
		var prevUpper = _prevUpper;
		var prevLower = _prevLower;

		var signal = 0;

		if (prevCandle.HasValue)
		{
			var prev = prevCandle.Value;

			if (prev.Open < pivot
			&& prev.Close > pivot
			&& prev.Close - prev.Open > 12m * step
			&& candle.ClosePrice > prev.Close
			&& mp > 0m
			&& candle.ClosePrice < candle.HighPrice)
			{
				signal = 1;
			}

			if (prev.Open > pivot
			&& prev.Close < pivot
			&& prev.Open - prev.Close > 12m * step
			&& candle.ClosePrice < prev.Close
			&& mp < 0m
			&& candle.ClosePrice > candle.LowPrice)
			{
				signal = -1;
			}
		}

		var rangeThreshold = r1 - s1;

		if (prevUpper is decimal lastUpper && prevLower is decimal lastLower)
		{
			var widthCurrent = upper - lower;
			var widthPrevious = lastUpper - lastLower;

			if (widthCurrent > rangeThreshold
			&& widthPrevious < rangeThreshold
			&& upper > lastUpper
			&& lower >= lastLower
			&& mp > 0m
			&& candle.ClosePrice < candle.HighPrice - 7m * step)
			{
				signal = 1;
			}

			if (widthCurrent > rangeThreshold
			&& widthPrevious < rangeThreshold
			&& lower < lastLower
			&& upper >= lastUpper
			&& mp > 0m
			&& candle.ClosePrice > candle.LowPrice + 7m * step)
			{
				signal = -1;
			}
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			StoreState(candle, upper, lower);
			return;
		}

		if (Position == 0m && signal != 0 && prevCandle.HasValue)
		{
			if (signal > 0)
			TryOpenLong(candle, prevCandle.Value, lower, step);
			else
			TryOpenShort(candle, prevCandle.Value, upper, step);
		}

		StoreState(candle, upper, lower);
	}

	private void ManageOpenPositions(ICandleMessage candle, decimal upper, decimal lower, decimal step, TimeSpan timeFrame)
	{
		if (Position > 0m && _longEntryPrice is decimal entry && PositionPrice is decimal)
		{
			var volume = Position;

			if (_longTake is decimal take && candle.HighPrice >= take)
			{
				SellMarket(volume);
				return;
			}

			if (_longStop is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(volume);
				return;
			}

			if (UseTimeClose && _longEntryTime is DateTimeOffset openTime && _previousCandle is CandleSnapshot prev)
			{
				var elapsed = candle.CloseTime - openTime;
				var minWindow = TimeSpan.FromTicks(timeFrame.Ticks * 3);
				var maxWindow = TimeSpan.FromTicks(timeFrame.Ticks * 4);

				if (elapsed >= minWindow && elapsed < maxWindow && prev.Close < entry - 3m * step)
				{
					SellMarket(volume);
					return;
				}
			}

			if (UseTrailingStop && _prevLower is decimal lastLower)
			{
				var profit = candle.ClosePrice - entry;
				if (profit > TrailingStartPoints * step && lower > lastLower)
				{
					var candidate = lower - TrailingBufferPoints * step;
					if (!_longStop.HasValue || candidate > _longStop.Value)
					_longStop = candidate;
				}
			}
		}
		else if (Position < 0m && _shortEntryPrice is decimal entryPrice && PositionPrice is decimal)
		{
			var volume = Math.Abs(Position);

			if (_shortTake is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(volume);
				return;
			}

			if (_shortStop is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(volume);
				return;
			}

			if (UseTimeClose && _shortEntryTime is DateTimeOffset openTime && _previousCandle is CandleSnapshot prev)
			{
				var elapsed = candle.CloseTime - openTime;
				var minWindow = TimeSpan.FromTicks(timeFrame.Ticks * 3);
				var maxWindow = TimeSpan.FromTicks(timeFrame.Ticks * 4);

				if (elapsed >= minWindow && elapsed < maxWindow && prev.Close > entryPrice + 3m * step)
				{
					BuyMarket(volume);
					return;
				}
			}

			if (UseTrailingStop && _prevUpper is decimal lastUpper)
			{
				var profit = entryPrice - candle.ClosePrice;
				if (profit > TrailingStartPoints * step && upper < lastUpper)
				{
					var candidate = upper + TrailingBufferPoints * step;
					if (!_shortStop.HasValue || candidate < _shortStop.Value)
					_shortStop = candidate;
				}
			}
		}
	}

	private void TryOpenLong(ICandleMessage candle, CandleSnapshot previous, decimal lower, decimal step)
	{
		var volume = Volume + Math.Abs(Position);
		var stop = previous.Low - StopLossBufferPoints * step;
		var take = candle.ClosePrice + TakeProfitPoints * step;

		_pendingLongStop = stop;
		_pendingLongTake = take;

		BuyMarket(volume);
	}

	private void TryOpenShort(ICandleMessage candle, CandleSnapshot previous, decimal upper, decimal step)
	{
		var volume = Volume + Math.Abs(Position);
		var stop = previous.High + StopLossBufferPoints * step;
		var take = candle.ClosePrice - TakeProfitPoints * step;

		_pendingShortStop = stop;
		_pendingShortTake = take;

		SellMarket(volume);
	}

	private void StoreState(ICandleMessage candle, decimal upper, decimal lower)
	{
		_prevUpper = upper;
		_prevLower = lower;
		_previousCandle = new CandleSnapshot(candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice, candle.OpenTime, candle.CloseTime);
	}

	private TimeSpan GetMainTimeFrame()
	{
		return CandleType.TimeFrame ?? (CandleType.Arg as TimeSpan? ?? TimeSpan.FromMinutes(1));
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep;
		return step is > 0m ? step.Value : 1m;
	}

	private void ResetPositionState()
	{
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longEntryTime = null;
		_shortEntryTime = null;
		_longStop = null;
		_longTake = null;
		_shortStop = null;
		_shortTake = null;
	}

	private readonly record struct CandleSnapshot(decimal Open, decimal High, decimal Low, decimal Close, DateTimeOffset OpenTime, DateTimeOffset CloseTime);
}


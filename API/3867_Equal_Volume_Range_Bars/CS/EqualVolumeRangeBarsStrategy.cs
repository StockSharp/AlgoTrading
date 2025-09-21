using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader script equalvolumebars.mq4 that generated offline equal volume or range charts.
/// Builds synthetic candles from tick data (and optionally from minute candles) and logs every completed bar.
/// </summary>
public class EqualVolumeRangeBarsStrategy : Strategy
{
	private readonly StrategyParam<EqualVolumeBarsMode> _workMode;
	private readonly StrategyParam<int> _ticksInBar;
	private readonly StrategyParam<bool> _fromMinuteHistory;
	private readonly StrategyParam<DataType> _minuteCandleType;

	private decimal _tickSize;
	private decimal _rangeThreshold;
	private decimal _currentVolume;
	private decimal _currentOpen;
	private decimal _currentHigh;
	private decimal _currentLow;
	private decimal _currentClose;
	private DateTimeOffset _currentOpenTime;
	private DateTimeOffset _lastUpdateTime;
	private DateTimeOffset _lastPublishedTime;
	private bool _hasActiveBar;
	private string _seriesName = string.Empty;
	private int _barCounter;

	/// <summary>
	/// Candle construction mode that matches the MT4 offline chart options.
	/// </summary>
	public EqualVolumeBarsMode WorkMode
	{
		get => _workMode.Value;
		set => _workMode.Value = value;
	}

	/// <summary>
	/// Number of ticks per bar in equal volume mode or points per bar in range mode.
	/// </summary>
	public int TicksInBar
	{
		get => _ticksInBar.Value;
		set => _ticksInBar.Value = value;
	}

	/// <summary>
	/// Enables rebuilding history from a one-minute candle subscription before live ticks arrive.
	/// </summary>
	public bool FromMinuteHistory
	{
		get => _fromMinuteHistory.Value;
		set => _fromMinuteHistory.Value = value;
	}

	/// <summary>
	/// Candle type used when <see cref="FromMinuteHistory"/> is enabled (defaults to M1).
	/// </summary>
	public DataType MinuteCandleType
	{
		get => _minuteCandleType.Value;
		set => _minuteCandleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="EqualVolumeRangeBarsStrategy"/> class.
	/// </summary>
	public EqualVolumeRangeBarsStrategy()
	{
		_workMode = Param(nameof(WorkMode), EqualVolumeBarsMode.EqualVolumeBars)
			.SetDisplay("Work Mode", "Choose equal volume or range bars", "General");

		_ticksInBar = Param(nameof(TicksInBar), 100)
			.SetDisplay("Ticks In Bar", "Tick count or point range per synthetic candle", "General")
			.SetCanOptimize();

		_fromMinuteHistory = Param(nameof(FromMinuteHistory), true)
			.SetDisplay("Use Minute History", "Seed the builder with historical M1 candles", "General");

		_minuteCandleType = Param(nameof(MinuteCandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Minute Candle Type", "Candle type for historical seeding", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, DataType.Ticks);

		if (FromMinuteHistory)
			yield return (Security, MinuteCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_currentVolume = 0m;
		_currentOpen = 0m;
		_currentHigh = 0m;
		_currentLow = 0m;
		_currentClose = 0m;
		_currentOpenTime = default;
		_lastUpdateTime = default;
		_lastPublishedTime = default;
		_hasActiveBar = false;
		_seriesName = string.Empty;
		_barCounter = 0;
		_tickSize = 0m;
		_rangeThreshold = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_tickSize = Security?.PriceStep ?? Security?.MinPriceStep ?? 0m;
		if (_tickSize <= 0m)
		{
			_tickSize = 0.0001m;
			LogWarning("PriceStep is not configured for the security. Using 0.0001 as a fallback tick size.");
		}

		_rangeThreshold = _tickSize * TicksInBar;
		_seriesName = BuildSeriesName();
		LogInfo($"{_seriesName}: starting in {WorkMode} mode with threshold {TicksInBar} {(WorkMode == EqualVolumeBarsMode.EqualVolumeBars ? "ticks" : "points")}");

		if (FromMinuteHistory)
		{
			var candleSubscription = SubscribeCandles(MinuteCandleType);
			candleSubscription
				.Bind(ProcessMinuteCandle)
				.Start();
		}

		SubscribeTrades()
			.Bind(ProcessTrade)
			.Start();
	}

	private void ProcessMinuteCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var totalVolume = candle.TotalVolume ?? 1m;
		if (totalVolume <= 0m)
			totalVolume = 1m;

		var directionUp = candle.ClosePrice >= candle.OpenPrice;
		var step = TimeSpan.FromSeconds(1);
		var time = candle.OpenTime;

		AppendSyntheticTick(candle.OpenPrice, time, 1m);
		totalVolume -= 1m;

		if (directionUp)
		{
			if (candle.LowPrice < candle.OpenPrice && totalVolume > 0m)
			{
				AppendSyntheticTick(candle.LowPrice, time + step, 1m);
				totalVolume -= 1m;
			}

			if (candle.HighPrice > candle.OpenPrice && totalVolume > 0m)
			{
				AppendSyntheticTick(candle.HighPrice, time + step + step, 1m);
				totalVolume -= 1m;
			}
		}
		else
		{
			if (candle.HighPrice > candle.OpenPrice && totalVolume > 0m)
			{
				AppendSyntheticTick(candle.HighPrice, time + step, 1m);
				totalVolume -= 1m;
			}

			if (candle.LowPrice < candle.OpenPrice && totalVolume > 0m)
			{
				AppendSyntheticTick(candle.LowPrice, time + step + step, 1m);
				totalVolume -= 1m;
			}
		}

		if (totalVolume > 0m)
		{
			AppendSyntheticTick(candle.ClosePrice, time + step + step + step, totalVolume);
		}
	}

	private void AppendSyntheticTick(decimal price, DateTimeOffset time, decimal volume)
	{
		ProcessTick(price, volume, time);
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		if (trade.TradePrice is not decimal price)
			return;

		var volume = trade.TradeVolume ?? 1m;
		if (volume <= 0m)
			volume = 1m;

		ProcessTick(price, volume, trade.ServerTime);
	}

	private void ProcessTick(decimal price, decimal volume, DateTimeOffset time)
	{
		if (volume <= 0m)
			return;

		if (!_hasActiveBar)
		{
			StartNewBar(price, volume, time);
			return;
		}

		var prospectiveHigh = Math.Max(_currentHigh, price);
		var prospectiveLow = Math.Min(_currentLow, price);
		var prospectiveVolume = _currentVolume + volume;

		var shouldStartNew = ShouldStartNewBar(prospectiveVolume, prospectiveHigh, prospectiveLow);
		if (shouldStartNew)
		{
			PublishBar(_lastUpdateTime);
			StartNewBar(price, volume, time);
			return;
		}

		_currentHigh = prospectiveHigh;
		_currentLow = prospectiveLow;
		_currentClose = price;
		_currentVolume = prospectiveVolume;
		_lastUpdateTime = time;
	}

	private void StartNewBar(decimal price, decimal volume, DateTimeOffset time)
	{
		_currentOpen = price;
		_currentHigh = price;
		_currentLow = price;
		_currentClose = price;
		_currentVolume = volume;
		_currentOpenTime = EnsureForwardTime(time);
		_lastUpdateTime = _currentOpenTime;
		_hasActiveBar = true;
	}

	private bool ShouldStartNewBar(decimal volume, decimal high, decimal low)
	{
		return WorkMode switch
		{
			EqualVolumeBarsMode.EqualVolumeBars => volume > TicksInBar,
			EqualVolumeBarsMode.RangeBars => high - low > _rangeThreshold,
			_ => false,
		};
	}

	private void PublishBar(DateTimeOffset time)
	{
		if (!_hasActiveBar || _currentVolume <= 0m)
			return;

		var closeTime = EnsureForwardTime(time);
		_barCounter++;

		LogInfo($"{_seriesName}: bar #{_barCounter} open={_currentOpen}, high={_currentHigh}, low={_currentLow}, close={_currentClose}, volume={_currentVolume}, time={closeTime:O}.");

		_hasActiveBar = false;
		_currentVolume = 0m;
	}

	private DateTimeOffset EnsureForwardTime(DateTimeOffset time)
	{
		if (time <= _lastPublishedTime)
			time = _lastPublishedTime + TimeSpan.FromMilliseconds(1);

		_lastPublishedTime = time;
		return time;
	}

	private string BuildSeriesName()
	{
		var id = Security?.Id ?? "Unknown";
		return $"EqvRng-{id}";
	}

	/// <summary>
	/// Operation mode copied from the MT4 script.
	/// </summary>
	public enum EqualVolumeBarsMode
	{
		/// <summary>
		/// Create bars once the configured number of ticks has been accumulated.
		/// </summary>
		EqualVolumeBars,

		/// <summary>
		/// Create bars once price covers the configured range expressed in points.
		/// </summary>
		RangeBars,
	}
}

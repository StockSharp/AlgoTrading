using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that replicates the MCM control panel behaviour by logging multi-timeframe events.
/// The strategy listens to several candle streams and optionally ticks, forwarding their events to the log.
/// </summary>
public class McmControlPanelStrategy : Strategy
{
	private readonly StrategyParam<DataType> _primaryCandleType;
	private readonly StrategyParam<DataType> _secondaryCandleType;
	private readonly StrategyParam<DataType> _tertiaryCandleType;
	private readonly StrategyParam<bool> _useSecondaryCandle;
	private readonly StrategyParam<bool> _useTertiaryCandle;
	private readonly StrategyParam<bool> _trackTicks;
	private readonly StrategyParam<bool> _logUnfinishedCandles;

	private DataType _activePrimaryType;
	private DataType _activeSecondaryType;
	private DataType _activeTertiaryType;

	private DateTimeOffset? _lastPrimaryEvent;
	private DateTimeOffset? _lastSecondaryEvent;
	private DateTimeOffset? _lastTertiaryEvent;
	private DateTimeOffset? _lastTickEvent;

	/// <summary>
	/// Primary candle type that is always tracked.
	/// </summary>
	public DataType PrimaryCandleType
	{
		get => _primaryCandleType.Value;
		set => _primaryCandleType.Value = value;
	}

	/// <summary>
	/// Optional secondary candle type that can be enabled for extra logging.
	/// </summary>
	public DataType SecondaryCandleType
	{
		get => _secondaryCandleType.Value;
		set => _secondaryCandleType.Value = value;
	}

	/// <summary>
	/// Optional tertiary candle type that can be enabled for extra logging.
	/// </summary>
	public DataType TertiaryCandleType
	{
		get => _tertiaryCandleType.Value;
		set => _tertiaryCandleType.Value = value;
	}

	/// <summary>
	/// Enables processing for the secondary candle stream.
	/// </summary>
	public bool UseSecondaryCandle
	{
		get => _useSecondaryCandle.Value;
		set => _useSecondaryCandle.Value = value;
	}

	/// <summary>
	/// Enables processing for the tertiary candle stream.
	/// </summary>
	public bool UseTertiaryCandle
	{
		get => _useTertiaryCandle.Value;
		set => _useTertiaryCandle.Value = value;
	}

	/// <summary>
	/// Enables tick monitoring in addition to candle events.
	/// </summary>
	public bool TrackTicks
	{
		get => _trackTicks.Value;
		set => _trackTicks.Value = value;
	}

	/// <summary>
	/// When enabled, candle updates are logged even before they close.
	/// </summary>
	public bool LogUnfinishedCandles
	{
		get => _logUnfinishedCandles.Value;
		set => _logUnfinishedCandles.Value = value;
	}

	public McmControlPanelStrategy()
	{
		_primaryCandleType = Param(nameof(PrimaryCandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Primary Timeframe", "Main candle timeframe to monitor", "General");

		_useSecondaryCandle = Param(nameof(UseSecondaryCandle), false)
			.SetDisplay("Use Secondary Timeframe", "Enable logging for the secondary timeframe", "General");

		_secondaryCandleType = Param(nameof(SecondaryCandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Secondary Timeframe", "Optional secondary candle timeframe", "General");

		_useTertiaryCandle = Param(nameof(UseTertiaryCandle), false)
			.SetDisplay("Use Tertiary Timeframe", "Enable logging for the tertiary timeframe", "General");

		_tertiaryCandleType = Param(nameof(TertiaryCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Tertiary Timeframe", "Optional tertiary candle timeframe", "General");

		_trackTicks = Param(nameof(TrackTicks), true)
			.SetDisplay("Track Ticks", "Log incoming ticks alongside candles", "General");

		_logUnfinishedCandles = Param(nameof(LogUnfinishedCandles), false)
			.SetDisplay("Log Unfinished Candles", "Log candle updates before they are finished", "Advanced");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security is null)
			yield break;

		yield return (Security, PrimaryCandleType);

		if (UseSecondaryCandle)
			yield return (Security, SecondaryCandleType);

		if (UseTertiaryCandle)
			yield return (Security, TertiaryCandleType);

		if (TrackTicks)
			yield return (Security, DataType.Ticks);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_lastPrimaryEvent = null;
		_lastSecondaryEvent = null;
		_lastTertiaryEvent = null;
		_lastTickEvent = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_activePrimaryType = PrimaryCandleType;
		SubscribeCandles(_activePrimaryType)
			.Bind(ProcessPrimaryCandle)
			.Start();

		if (UseSecondaryCandle)
		{
			_activeSecondaryType = SecondaryCandleType;
			SubscribeCandles(_activeSecondaryType)
				.Bind(ProcessSecondaryCandle)
				.Start();
		}

		if (UseTertiaryCandle)
		{
			_activeTertiaryType = TertiaryCandleType;
			SubscribeCandles(_activeTertiaryType)
				.Bind(ProcessTertiaryCandle)
				.Start();
		}

		if (TrackTicks)
		{
			SubscribeTicks()
				.Bind(ProcessTick)
				.Start();
		}

		LogInfo("MCM control panel monitor started.");
	}

	private void ProcessPrimaryCandle(ICandleMessage candle)
	{
		LogCandleEvent(candle, _activePrimaryType, ref _lastPrimaryEvent);
	}

	private void ProcessSecondaryCandle(ICandleMessage candle)
	{
		LogCandleEvent(candle, _activeSecondaryType, ref _lastSecondaryEvent);
	}

	private void ProcessTertiaryCandle(ICandleMessage candle)
	{
		LogCandleEvent(candle, _activeTertiaryType, ref _lastTertiaryEvent);
	}

	private void ProcessTick(ITickTradeMessage trade)
	{
		var eventTime = trade.ServerTime;
		if (_lastTickEvent == eventTime)
			return;

		_lastTickEvent = eventTime;

		var symbol = Security?.Id ?? string.Empty;
		LogInfo($"[{symbol}] Tick price={trade.Price} volume={trade.Volume} time={eventTime:O}");
	}

	private void LogCandleEvent(ICandleMessage candle, DataType candleType, ref DateTimeOffset? lastEvent)
	{
		if (!LogUnfinishedCandles && candle.State != CandleStates.Finished)
			return;

		var eventTime = candle.State == CandleStates.Finished ? candle.CloseTime : candle.OpenTime;
		if (lastEvent == eventTime)
			return;

		lastEvent = eventTime;

		var symbol = Security?.Id ?? string.Empty;
		var period = DescribeDataType(candleType);
		var status = candle.State == CandleStates.Finished ? "closed candle" : "candle update";
		var price = candle.ClosePrice;
		var volume = candle.TotalVolume;

		LogInfo($"[{symbol}] {period} {status} price={price} volume={volume} time={eventTime:O}");
	}

	private static string DescribeDataType(DataType dataType)
	{
		if (dataType.MessageType == typeof(TimeFrameCandleMessage) && dataType.Arg is TimeSpan timeFrame)
		{
			var minutes = timeFrame.TotalMinutes;
			switch (minutes)
			{
				case 1: return "M1";
				case 2: return "M2";
				case 3: return "M3";
				case 4: return "M4";
				case 5: return "M5";
				case 6: return "M6";
				case 10: return "M10";
				case 12: return "M12";
				case 15: return "M15";
				case 20: return "M20";
				case 30: return "M30";
			}

			var hours = timeFrame.TotalHours;
			switch (hours)
			{
				case 1: return "H1";
				case 2: return "H2";
				case 3: return "H3";
				case 4: return "H4";
				case 6: return "H6";
				case 8: return "H8";
				case 12: return "H12";
			}

			var days = timeFrame.TotalDays;
			switch (days)
			{
				case 1: return "D1";
				case 7: return "W1";
				case 30: return "MN1";
			}

			if (minutes >= 1 && minutes < 60)
				return $"M{minutes}";

			if (hours >= 1 && hours < 24)
				return $"H{hours}";

			if (days >= 1)
				return $"D{days}";
		}

		return dataType.ToString() ?? "Unknown";
	}
}

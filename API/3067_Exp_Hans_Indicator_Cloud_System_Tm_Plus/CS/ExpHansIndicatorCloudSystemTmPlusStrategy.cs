namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Port of the Exp_Hans_Indicator_Cloud_System_Tm_Plus MQL5 expert advisor.
/// Replicates the Hans indicator breakout logic, including time-based exits and pip-based risk limits.
/// </summary>
public class ExpHansIndicatorCloudSystemTmPlusStrategy : Strategy
{
	private const int MaxHistory = 1024;

	private static readonly TimeSpan Session1Start = TimeSpan.FromHours(4);
	private static readonly TimeSpan Session1End = TimeSpan.FromHours(8);
	private static readonly TimeSpan Session2End = TimeSpan.FromHours(12);

	private readonly StrategyParam<decimal> _moneyManagement;
	private readonly StrategyParam<MoneyManagementMode> _moneyMode;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _deviationPoints;
	private readonly StrategyParam<bool> _allowBuyEntries;
	private readonly StrategyParam<bool> _allowSellEntries;
	private readonly StrategyParam<bool> _allowBuyExits;
	private readonly StrategyParam<bool> _allowSellExits;
	private readonly StrategyParam<bool> _useTimeExit;
	private readonly StrategyParam<int> _holdingMinutes;
	private readonly StrategyParam<int> _pipsForEntry;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<int> _localTimeZone;
	private readonly StrategyParam<int> _destinationTimeZone;
	private readonly StrategyParam<DataType> _candleType;

	private DailySessionState? _dayState;
	private readonly List<int> _colorHistory = new();
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private DateTimeOffset? _entryTime;

	/// <summary>
	/// Enumeration matching the money management modes of the original expert.
	/// Currently only the Lot mode is applied; other options are reserved for future extensions.
	/// </summary>
	public enum MoneyManagementMode
	{
		FreeMargin,
		Balance,
		LossFreeMargin,
		LossBalance,
		Lot,
	}

	/// <summary>
	/// Portion of the base strategy volume used for each order.
	/// </summary>
	public decimal MoneyManagement
	{
		get => _moneyManagement.Value;
		set => _moneyManagement.Value = value;
	}

	/// <summary>
	/// Selected money management interpretation.
	/// </summary>
	public MoneyManagementMode MoneyMode
	{
		get => _moneyMode.Value;
		set => _moneyMode.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in instrument points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in instrument points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Allowed execution deviation in points (kept for compatibility).
	/// </summary>
	public int DeviationPoints
	{
		get => _deviationPoints.Value;
		set => _deviationPoints.Value = value;
	}

	/// <summary>
	/// Enables long entries when the bullish breakout sequence completes.
	/// </summary>
	public bool AllowBuyEntries
	{
		get => _allowBuyEntries.Value;
		set => _allowBuyEntries.Value = value;
	}

	/// <summary>
	/// Enables short entries when the bearish breakout sequence completes.
	/// </summary>
	public bool AllowSellEntries
	{
		get => _allowSellEntries.Value;
		set => _allowSellEntries.Value = value;
	}

	/// <summary>
	/// Allows closing long positions on bearish Hans colors.
	/// </summary>
	public bool AllowBuyExits
	{
		get => _allowBuyExits.Value;
		set => _allowBuyExits.Value = value;
	}

	/// <summary>
	/// Allows closing short positions on bullish Hans colors.
	/// </summary>
	public bool AllowSellExits
	{
		get => _allowSellExits.Value;
		set => _allowSellExits.Value = value;
	}

	/// <summary>
	/// Enables the time-based exit filter.
	/// </summary>
	public bool UseTimeExit
	{
		get => _useTimeExit.Value;
		set => _useTimeExit.Value = value;
	}

	/// <summary>
	/// Maximum holding time in minutes before the position is liquidated.
	/// </summary>
	public int HoldingMinutes
	{
		get => _holdingMinutes.Value;
		set => _holdingMinutes.Value = value;
	}

	/// <summary>
	/// Number of pips added to the breakout range.
	/// </summary>
	public int PipsForEntry
	{
		get => _pipsForEntry.Value;
		set => _pipsForEntry.Value = value;
	}

	/// <summary>
	/// Number of closed candles used as signal offset.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Broker/server time zone in hours.
	/// </summary>
	public int LocalTimeZone
	{
		get => _localTimeZone.Value;
		set => _localTimeZone.Value = value;
	}

	/// <summary>
	/// Destination time zone defining the Hans breakout sessions.
	/// </summary>
	public int DestinationTimeZone
	{
		get => _destinationTimeZone.Value;
		set => _destinationTimeZone.Value = value;
	}

	/// <summary>
	/// Candle series used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize the strategy parameters with defaults matching the MQL5 inputs.
	/// </summary>
	public ExpHansIndicatorCloudSystemTmPlusStrategy()
	{
		_moneyManagement = Param(nameof(MoneyManagement), 0.1m)
		.SetDisplay("Money Management", "Portion of the base volume traded per entry", "Risk");

		_moneyMode = Param(nameof(MoneyMode), MoneyManagementMode.Lot)
		.SetDisplay("Money Mode", "Interpretation of the money management value", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
		.SetDisplay("Stop Loss (points)", "Distance to the protective stop in points", "Risk")
		.SetGreaterOrEqual(0);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
		.SetDisplay("Take Profit (points)", "Distance to the profit target in points", "Risk")
		.SetGreaterOrEqual(0);

		_deviationPoints = Param(nameof(DeviationPoints), 10)
		.SetDisplay("Execution Deviation", "Maximum acceptable slippage in points", "Orders")
		.SetGreaterOrEqual(0);

		_allowBuyEntries = Param(nameof(AllowBuyEntries), true)
		.SetDisplay("Enable Long Entries", "Allow opening long positions", "Signals");

		_allowSellEntries = Param(nameof(AllowSellEntries), true)
		.SetDisplay("Enable Short Entries", "Allow opening short positions", "Signals");

		_allowBuyExits = Param(nameof(AllowBuyExits), true)
		.SetDisplay("Enable Long Exits", "Allow automated long exits", "Signals");

		_allowSellExits = Param(nameof(AllowSellExits), true)
		.SetDisplay("Enable Short Exits", "Allow automated short exits", "Signals");

		_useTimeExit = Param(nameof(UseTimeExit), true)
		.SetDisplay("Use Time Exit", "Close positions after the holding period", "Risk");

		_holdingMinutes = Param(nameof(HoldingMinutes), 1500)
		.SetDisplay("Holding Minutes", "Maximum position lifetime in minutes", "Risk")
		.SetGreaterOrEqual(0);

		_pipsForEntry = Param(nameof(PipsForEntry), 100)
		.SetDisplay("Pips For Entry", "Offset added above/below the breakout range", "Indicator")
		.SetGreaterOrEqual(0);

		_signalBar = Param(nameof(SignalBar), 1)
		.SetDisplay("Signal Bar", "Closed candle offset used for signals", "Indicator")
		.SetGreaterOrEqual(0);

		_localTimeZone = Param(nameof(LocalTimeZone), 0)
		.SetDisplay("Local Time Zone", "Broker/server time zone", "Indicator");

		_destinationTimeZone = Param(nameof(DestinationTimeZone), 4)
		.SetDisplay("Destination Time Zone", "Target time zone for sessions", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("Candle Type", "Time frame used for Hans calculations", "Data");
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

		_colorHistory.Clear();
		_dayState = null;
		ResetPositionState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (Position == 0 && (_entryTime.HasValue || _stopPrice.HasValue || _takePrice.HasValue))
		ResetPositionState();

		UpdateDailyState(candle);

		var color = CalculateColor(candle);
		_colorHistory.Add(color);
		TrimHistory();

		var offset = Math.Max(1, SignalBar);
		if (_colorHistory.Count <= offset)
		return;

		var currentIndex = _colorHistory.Count - offset;
		if (currentIndex >= _colorHistory.Count)
		return;

		var previousIndex = currentIndex - 1;
		if (previousIndex < 0)
		return;

		var currentColor = _colorHistory[currentIndex];
		var previousColor = _colorHistory[previousIndex];

		var buyEntrySignal = AllowBuyEntries && IsUpperBreakout(previousColor) && !IsUpperBreakout(currentColor);
		var sellEntrySignal = AllowSellEntries && IsLowerBreakout(previousColor) && !IsLowerBreakout(currentColor);
		var buyExitSignal = AllowBuyExits && IsLowerBreakout(previousColor);
		var sellExitSignal = AllowSellExits && IsUpperBreakout(previousColor);

		if (Position > 0)
		{
			var exitByTime = UseTimeExit && HoldingMinutes > 0 && _entryTime.HasValue && candle.CloseTime - _entryTime.Value >= TimeSpan.FromMinutes(HoldingMinutes);
			var exitByStop = _stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value;
			var exitByTarget = _takePrice.HasValue && candle.HighPrice >= _takePrice.Value;
			if (exitByTime || buyExitSignal || exitByStop || exitByTarget)
			{
				CloseLong();
			}
		}
		else if (Position < 0)
		{
			var exitByTime = UseTimeExit && HoldingMinutes > 0 && _entryTime.HasValue && candle.CloseTime - _entryTime.Value >= TimeSpan.FromMinutes(HoldingMinutes);
			var exitByStop = _stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value;
			var exitByTarget = _takePrice.HasValue && candle.LowPrice <= _takePrice.Value;
			if (exitByTime || sellExitSignal || exitByStop || exitByTarget)
			{
				CloseShort();
			}
		}

		if (buyEntrySignal && Position <= 0)
		{
			EnterLong(candle);
		}
		else if (sellEntrySignal && Position >= 0)
		{
			EnterShort(candle);
		}
	}

	private void EnterLong(ICandleMessage candle)
	{
		var volume = GetOrderVolume();
		if (volume <= 0)
		return;

		var existingShort = Position < 0 ? Math.Abs(Position) : 0m;
		var totalVolume = volume + existingShort;
		if (totalVolume <= 0)
		return;

		BuyMarket(totalVolume);

		_entryTime = candle.CloseTime != default ? candle.CloseTime : candle.OpenTime;

		var pipSize = GetPipSize();
		if (pipSize <= 0)
		{
			_stopPrice = null;
			_takePrice = null;
			return;
		}

		_stopPrice = StopLossPoints > 0 ? candle.ClosePrice - pipSize * StopLossPoints : null;
		_takePrice = TakeProfitPoints > 0 ? candle.ClosePrice + pipSize * TakeProfitPoints : null;
	}

	private void EnterShort(ICandleMessage candle)
	{
		var volume = GetOrderVolume();
		if (volume <= 0)
		return;

		var existingLong = Position > 0 ? Math.Abs(Position) : 0m;
		var totalVolume = volume + existingLong;
		if (totalVolume <= 0)
		return;

		SellMarket(totalVolume);

		_entryTime = candle.CloseTime != default ? candle.CloseTime : candle.OpenTime;

		var pipSize = GetPipSize();
		if (pipSize <= 0)
		{
			_stopPrice = null;
			_takePrice = null;
			return;
		}

		_stopPrice = StopLossPoints > 0 ? candle.ClosePrice + pipSize * StopLossPoints : null;
		_takePrice = TakeProfitPoints > 0 ? candle.ClosePrice - pipSize * TakeProfitPoints : null;
	}

	private void CloseLong()
	{
		var volume = Math.Abs(Position);
		if (volume <= 0)
		return;

		SellMarket(volume);
		ResetPositionState();
	}

	private void CloseShort()
	{
		var volume = Math.Abs(Position);
		if (volume <= 0)
		return;

		BuyMarket(volume);
		ResetPositionState();
	}

	private void UpdateDailyState(ICandleMessage candle)
	{
		var destOpen = ToDestinationTime(candle.OpenTime);
		var date = destOpen.Date;

		if (_dayState == null || _dayState.Date != date)
		{
			_dayState = new DailySessionState { Date = date };
		}

		var state = _dayState;
		var timeOfDay = destOpen.TimeOfDay;

		if (timeOfDay >= Session1Start && timeOfDay < Session1End)
		{
			UpdateSessionRange(state, candle.HighPrice, candle.LowPrice, true);
			state.Session1Completed = false;
		}
		else if (timeOfDay >= Session1End && timeOfDay < Session2End)
		{
			if (!state.Session1Completed && state.Session1High.HasValue && state.Session1Low.HasValue)
			state.Session1Completed = true;

			UpdateSessionRange(state, candle.HighPrice, candle.LowPrice, false);
			state.Session2Completed = false;
		}
		else
		{
			if (!state.Session1Completed && state.Session1High.HasValue && state.Session1Low.HasValue)
			state.Session1Completed = true;

			if (!state.Session2Completed && state.Session2High.HasValue && state.Session2Low.HasValue)
			state.Session2Completed = true;
		}
	}

	private int CalculateColor(ICandleMessage candle)
	{
		if (!TryGetActiveBands(out var upper, out var lower))
		return 2;

		if (candle.ClosePrice > upper)
		return candle.ClosePrice >= candle.OpenPrice ? 0 : 1;

		if (candle.ClosePrice < lower)
		return candle.ClosePrice <= candle.OpenPrice ? 4 : 3;

		return 2;
	}

	private bool TryGetActiveBands(out decimal upper, out decimal lower)
	{
		upper = 0m;
		lower = 0m;

		var pipSize = GetPipSize();
		if (pipSize <= 0)
		return false;

		if (_dayState == null)
		return false;

		if (_dayState.Session2Completed && _dayState.Session2High.HasValue && _dayState.Session2Low.HasValue)
		{
			upper = _dayState.Session2High.Value + pipSize * PipsForEntry;
			lower = _dayState.Session2Low.Value - pipSize * PipsForEntry;
			return true;
		}

		if (_dayState.Session1Completed && _dayState.Session1High.HasValue && _dayState.Session1Low.HasValue)
		{
			upper = _dayState.Session1High.Value + pipSize * PipsForEntry;
			lower = _dayState.Session1Low.Value - pipSize * PipsForEntry;
			return true;
		}

		return false;
	}

	private void UpdateSessionRange(DailySessionState state, decimal high, decimal low, bool isFirstSession)
	{
		if (isFirstSession)
		{
			state.Session1High = state.Session1High.HasValue ? Math.Max(state.Session1High.Value, high) : high;
			state.Session1Low = state.Session1Low.HasValue ? Math.Min(state.Session1Low.Value, low) : low;
		}
		else
		{
			state.Session2High = state.Session2High.HasValue ? Math.Max(state.Session2High.Value, high) : high;
			state.Session2Low = state.Session2Low.HasValue ? Math.Min(state.Session2Low.Value, low) : low;
		}
	}

	private decimal GetOrderVolume()
	{
		var step = Security?.VolumeStep ?? 1m;
		if (step <= 0)
		step = 1m;

		var baseVolume = Volume * MoneyManagement;
		if (baseVolume <= 0)
		baseVolume = Volume;

		var normalized = Math.Round(baseVolume / step) * step;
		if (normalized <= 0)
		normalized = step;

		return normalized;
	}

	private decimal GetPipSize()
	{
		if (Security?.PriceStep is decimal step && step > 0m)
		{
			var decimals = Security.Decimals;
			if (decimals == 3 || decimals == 5)
			return step * 10m;

			return step;
		}

		return 0m;
	}

	private void ResetPositionState()
	{
		_entryTime = null;
		_stopPrice = null;
		_takePrice = null;
	}

	private void TrimHistory()
	{
		if (_colorHistory.Count <= MaxHistory)
		return;

		var excess = _colorHistory.Count - MaxHistory;
		_colorHistory.RemoveRange(0, excess);
	}

	private DateTimeOffset ToDestinationTime(DateTimeOffset time)
	{
		var shift = TimeSpan.FromHours(LocalTimeZone - DestinationTimeZone);
		return time - shift;
	}

	private static bool IsUpperBreakout(int? color) => color is 0 or 1;

	private static bool IsLowerBreakout(int? color) => color is 3 or 4;

	private sealed class DailySessionState
	{
		public DateTime Date { get; set; }
		public decimal? Session1High { get; set; }
		public decimal? Session1Low { get; set; }
		public decimal? Session2High { get; set; }
		public decimal? Session2Low { get; set; }
		public bool Session1Completed { get; set; }
		public bool Session2Completed { get; set; }
	}
}

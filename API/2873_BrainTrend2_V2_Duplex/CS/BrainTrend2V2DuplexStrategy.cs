using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// BrainTrend2 V2 Duplex strategy converted from the MQL5 version.
/// </summary>
public class BrainTrend2V2DuplexStrategy : Strategy
{
	private readonly StrategyParam<DataType> _longCandleType;
	private readonly StrategyParam<int> _longAtrPeriod;
	private readonly StrategyParam<int> _longSignalBar;
	private readonly StrategyParam<bool> _longOpenEnabled;
	private readonly StrategyParam<bool> _longCloseEnabled;
	private readonly StrategyParam<decimal> _longVolume;
	private readonly StrategyParam<decimal> _longStopLossPoints;
	private readonly StrategyParam<decimal> _longTakeProfitPoints;
	private readonly StrategyParam<DataType> _shortCandleType;
	private readonly StrategyParam<int> _shortAtrPeriod;
	private readonly StrategyParam<int> _shortSignalBar;
	private readonly StrategyParam<bool> _shortOpenEnabled;
	private readonly StrategyParam<bool> _shortCloseEnabled;
	private readonly StrategyParam<decimal> _shortVolume;
	private readonly StrategyParam<decimal> _shortStopLossPoints;
	private readonly StrategyParam<decimal> _shortTakeProfitPoints;

	private BrainTrend2V2Indicator _longIndicator;
	private BrainTrend2V2Indicator _shortIndicator;

	private readonly List<int> _longColors = new();
	private readonly List<int> _shortColors = new();

	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;

	/// <summary>
	/// Data type for the long-side BrainTrend candles.
	/// </summary>
	public DataType LongCandleType
	{
		get => _longCandleType.Value;
		set => _longCandleType.Value = value;
	}

	/// <summary>
	/// ATR length used by the long BrainTrend indicator.
	/// </summary>
	public int LongAtrPeriod
	{
		get => _longAtrPeriod.Value;
		set => _longAtrPeriod.Value = value;
	}

	/// <summary>
	/// Historical shift (in bars) evaluated for long entries.
	/// </summary>
	public int LongSignalBar
	{
		get => _longSignalBar.Value;
		set => _longSignalBar.Value = value;
	}

	/// <summary>
	/// Enables long entries.
	/// </summary>
	public bool LongOpenEnabled
	{
		get => _longOpenEnabled.Value;
		set => _longOpenEnabled.Value = value;
	}

	/// <summary>
	/// Enables long exits.
	/// </summary>
	public bool LongCloseEnabled
	{
		get => _longCloseEnabled.Value;
		set => _longCloseEnabled.Value = value;
	}

	/// <summary>
	/// Order size for opening long trades.
	/// </summary>
	public decimal LongVolume
	{
		get => _longVolume.Value;
		set => _longVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance (in points) for long positions.
	/// </summary>
	public decimal LongStopLossPoints
	{
		get => _longStopLossPoints.Value;
		set => _longStopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance (in points) for long positions.
	/// </summary>
	public decimal LongTakeProfitPoints
	{
		get => _longTakeProfitPoints.Value;
		set => _longTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Data type for the short-side BrainTrend candles.
	/// </summary>
	public DataType ShortCandleType
	{
		get => _shortCandleType.Value;
		set => _shortCandleType.Value = value;
	}

	/// <summary>
	/// ATR length used by the short BrainTrend indicator.
	/// </summary>
	public int ShortAtrPeriod
	{
		get => _shortAtrPeriod.Value;
		set => _shortAtrPeriod.Value = value;
	}

	/// <summary>
	/// Historical shift (in bars) evaluated for short entries.
	/// </summary>
	public int ShortSignalBar
	{
		get => _shortSignalBar.Value;
		set => _shortSignalBar.Value = value;
	}

	/// <summary>
	/// Enables short entries.
	/// </summary>
	public bool ShortOpenEnabled
	{
		get => _shortOpenEnabled.Value;
		set => _shortOpenEnabled.Value = value;
	}

	/// <summary>
	/// Enables short exits.
	/// </summary>
	public bool ShortCloseEnabled
	{
		get => _shortCloseEnabled.Value;
		set => _shortCloseEnabled.Value = value;
	}

	/// <summary>
	/// Order size for opening short trades.
	/// </summary>
	public decimal ShortVolume
	{
		get => _shortVolume.Value;
		set => _shortVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance (in points) for short positions.
	/// </summary>
	public decimal ShortStopLossPoints
	{
		get => _shortStopLossPoints.Value;
		set => _shortStopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance (in points) for short positions.
	/// </summary>
	public decimal ShortTakeProfitPoints
	{
		get => _shortTakeProfitPoints.Value;
		set => _shortTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public BrainTrend2V2DuplexStrategy()
	{
		_longCandleType = Param(nameof(LongCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Long Candle Type", "Time frame for the long BrainTrend2 indicator", "Long Side");

		_longAtrPeriod = Param(nameof(LongAtrPeriod), 7)
			.SetGreaterThanZero()
			.SetDisplay("Long ATR Period", "ATR length used by the long BrainTrend2 calculation", "Long Side");

		_longSignalBar = Param(nameof(LongSignalBar), 1)
			.SetDisplay("Long Signal Bar", "Shift in bars used to evaluate long signals", "Long Side");

		_longOpenEnabled = Param(nameof(LongOpenEnabled), true)
			.SetDisplay("Enable Long Entries", "Allow the strategy to open long trades", "Long Side");

		_longCloseEnabled = Param(nameof(LongCloseEnabled), true)
			.SetDisplay("Enable Long Exits", "Allow the strategy to close long trades on indicator signals", "Long Side");

		_longVolume = Param(nameof(LongVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Long Volume", "Base order size used for long entries", "Long Side");

		_longStopLossPoints = Param(nameof(LongStopLossPoints), 1000m)
			.SetDisplay("Long Stop Loss", "Stop loss distance in points for long trades (0 disables)", "Risk Management");

		_longTakeProfitPoints = Param(nameof(LongTakeProfitPoints), 2000m)
			.SetDisplay("Long Take Profit", "Take profit distance in points for long trades (0 disables)", "Risk Management");

		_shortCandleType = Param(nameof(ShortCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Short Candle Type", "Time frame for the short BrainTrend2 indicator", "Short Side");

		_shortAtrPeriod = Param(nameof(ShortAtrPeriod), 7)
			.SetGreaterThanZero()
			.SetDisplay("Short ATR Period", "ATR length used by the short BrainTrend2 calculation", "Short Side");

		_shortSignalBar = Param(nameof(ShortSignalBar), 1)
			.SetDisplay("Short Signal Bar", "Shift in bars used to evaluate short signals", "Short Side");

		_shortOpenEnabled = Param(nameof(ShortOpenEnabled), true)
			.SetDisplay("Enable Short Entries", "Allow the strategy to open short trades", "Short Side");

		_shortCloseEnabled = Param(nameof(ShortCloseEnabled), true)
			.SetDisplay("Enable Short Exits", "Allow the strategy to close short trades on indicator signals", "Short Side");

		_shortVolume = Param(nameof(ShortVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Short Volume", "Base order size used for short entries", "Short Side");

		_shortStopLossPoints = Param(nameof(ShortStopLossPoints), 1000m)
			.SetDisplay("Short Stop Loss", "Stop loss distance in points for short trades (0 disables)", "Risk Management");

		_shortTakeProfitPoints = Param(nameof(ShortTakeProfitPoints), 2000m)
			.SetDisplay("Short Take Profit", "Take profit distance in points for short trades (0 disables)", "Risk Management");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new[]
		{
			(Security, LongCandleType),
			(Security, ShortCandleType)
		};
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_longColors.Clear();
		_shortColors.Clear();
		_longStopPrice = null;
		_longTakePrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
	}

	/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
// Initialize indicator instances for both long and short signal streams.
_longIndicator = new BrainTrend2V2Indicator
{
AtrPeriod = LongAtrPeriod
};

		_shortIndicator = new BrainTrend2V2Indicator
		{
			AtrPeriod = ShortAtrPeriod
		};

// Subscribe to candles for the long side and bind the indicator callback.
var longSubscription = SubscribeCandles(LongCandleType);
longSubscription
.BindEx(_longIndicator, ProcessLongSignal)
.Start();

// Subscribe to candles for the short side and bind the indicator callback.
var shortSubscription = SubscribeCandles(ShortCandleType);
shortSubscription
.BindEx(_shortIndicator, ProcessShortSignal)
.Start();

// Enable built-in position protection helpers.
StartProtection();

base.OnStarted(time);
}

private void ProcessLongSignal(ICandleMessage candle, IIndicatorValue indicatorValue)
{
// Only process fully formed candles.
if (candle.State != CandleStates.Finished)
return;

// Wait for the indicator to produce a stable value.
if (!indicatorValue.IsFormed)
return;

var colorValue = indicatorValue.GetValue<decimal>();
var color = (int)Math.Round(colorValue, MidpointRounding.AwayFromZero);

// Store the color history so we can look back by SignalBar.
UpdateColorHistory(_longColors, color, LongSignalBar + 3);

if (!IsFormedAndOnlineAndAllowTrading())
return;

// Handle stop-loss or take-profit exits before generating new orders.
if (CheckRiskManagement(candle))
return;

if (!TryGetSignalColors(_longColors, LongSignalBar, out var currentColor, out var previousColor))
return;

if (LongOpenEnabled && previousColor < 2 && currentColor > 1)
TryOpenLong(candle);

if (LongCloseEnabled && previousColor > 2)
TryCloseLong();
}

private void ProcessShortSignal(ICandleMessage candle, IIndicatorValue indicatorValue)
{
// Only process fully formed candles.
if (candle.State != CandleStates.Finished)
return;

// Wait for the indicator to produce a stable value.
if (!indicatorValue.IsFormed)
return;

var colorValue = indicatorValue.GetValue<decimal>();
var color = (int)Math.Round(colorValue, MidpointRounding.AwayFromZero);

// Store the color history so we can look back by SignalBar.
UpdateColorHistory(_shortColors, color, ShortSignalBar + 3);

if (!IsFormedAndOnlineAndAllowTrading())
return;

// Handle stop-loss or take-profit exits before generating new orders.
if (CheckRiskManagement(candle))
return;

		if (!TryGetSignalColors(_shortColors, ShortSignalBar, out var currentColor, out var previousColor))
			return;

		if (ShortOpenEnabled && previousColor > 2 && currentColor > 0)
			TryOpenShort(candle);

		if (ShortCloseEnabled && previousColor < 2)
			TryCloseShort();
	}

	private void UpdateColorHistory(List<int> history, int color, int capacity)
	{
		history.Add(color);
		var maxCapacity = Math.Max(capacity, 2);
		while (history.Count > maxCapacity)
		{
			history.RemoveAt(0);
		}
	}

	private bool TryGetSignalColors(List<int> history, int signalBar, out int currentColor, out int previousColor)
	{
		currentColor = 0;
		previousColor = 0;

		var currentIndex = history.Count - 1 - signalBar;
		var previousIndex = currentIndex - 1;

		if (currentIndex < 0 || previousIndex < 0)
			return false;

		currentColor = history[currentIndex];
		previousColor = history[previousIndex];
		return true;
	}

private bool CheckRiskManagement(ICandleMessage candle)
{
// Exit long positions when stop-loss or take-profit thresholds are breached.
if (Position > 0)
{
if (_longStopPrice.HasValue && candle.LowPrice <= _longStopPrice.Value)
{
SellMarket(Math.Abs(Position));
				_longStopPrice = null;
				_longTakePrice = null;
				return true;
			}

			if (_longTakePrice.HasValue && candle.HighPrice >= _longTakePrice.Value)
			{
				SellMarket(Math.Abs(Position));
				_longStopPrice = null;
				_longTakePrice = null;
				return true;
			}
}
else if (Position < 0)
{
// Exit short positions when stop-loss or take-profit thresholds are breached.
if (_shortStopPrice.HasValue && candle.HighPrice >= _shortStopPrice.Value)
{
BuyMarket(Math.Abs(Position));
_shortStopPrice = null;
				_shortTakePrice = null;
				return true;
			}

			if (_shortTakePrice.HasValue && candle.LowPrice <= _shortTakePrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				_shortStopPrice = null;
				_shortTakePrice = null;
				return true;
			}
		}

		return false;
	}

private void TryOpenLong(ICandleMessage candle)
{
// Do not proceed if volume is not positive.
if (LongVolume <= 0)
return;

var volume = LongVolume;

// Flip the position if we are currently short.
if (Position < 0)
{
volume += Math.Abs(Position);
}
else if (Position > 0)
		{
			return;
		}

		BuyMarket(volume);
		_shortStopPrice = null;
		_shortTakePrice = null;
		SetLongProtection(candle.ClosePrice);
	}

private void TryCloseLong()
{
// Close long positions via a market sell order.
if (Position <= 0)
return;

SellMarket(Math.Abs(Position));
		_longStopPrice = null;
		_longTakePrice = null;
	}

private void TryOpenShort(ICandleMessage candle)
{
// Do not proceed if volume is not positive.
if (ShortVolume <= 0)
return;

var volume = ShortVolume;

// Flip the position if we are currently long.
if (Position > 0)
{
volume += Math.Abs(Position);
}
else if (Position < 0)
		{
			return;
		}

		SellMarket(volume);
		_longStopPrice = null;
		_longTakePrice = null;
		SetShortProtection(candle.ClosePrice);
	}

private void TryCloseShort()
{
// Close short positions via a market buy order.
if (Position >= 0)
return;

BuyMarket(Math.Abs(Position));
		_shortStopPrice = null;
		_shortTakePrice = null;
	}

private void SetLongProtection(decimal entryPrice)
{
// Convert point-based parameters into absolute prices.
var step = GetPriceStep();

if (LongStopLossPoints > 0)
_longStopPrice = entryPrice - LongStopLossPoints * step;
		else
			_longStopPrice = null;

		if (LongTakeProfitPoints > 0)
			_longTakePrice = entryPrice + LongTakeProfitPoints * step;
		else
			_longTakePrice = null;
	}

private void SetShortProtection(decimal entryPrice)
{
// Convert point-based parameters into absolute prices.
var step = GetPriceStep();

if (ShortStopLossPoints > 0)
_shortStopPrice = entryPrice + ShortStopLossPoints * step;
		else
			_shortStopPrice = null;

		if (ShortTakeProfitPoints > 0)
			_shortTakePrice = entryPrice - ShortTakeProfitPoints * step;
		else
			_shortTakePrice = null;
	}

private decimal GetPriceStep()
{
// Use the security price step when available, otherwise fallback to 1.
var step = Security?.PriceStep;
if (step == null || step == 0)
return 1m;

		return step.Value;
	}
}

/// <summary>
/// BrainTrend2 V2 indicator implementation used by the duplex strategy.
/// </summary>
public class BrainTrend2V2Indicator : BaseIndicator<decimal>
{
	private const decimal Dartp = 7m;
	private const decimal Cecf = 0.7m;

	private readonly List<decimal> _trBuffer = new();
	private int _bufferIndex;
	private bool _hasPreviousClose;
	private decimal _previousClose;
	private bool _river = true;
	private bool _riverInitialized;
	private decimal _emaxtra;

	/// <summary>
	/// ATR period used by the indicator.
	/// </summary>
	public int AtrPeriod { get; set; } = 7;

	/// <inheritdoc />
protected override IIndicatorValue OnProcess(IIndicatorValue input)
{
// The indicator only reacts to completed candles.
if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
return new DecimalIndicatorValue(this, default, input.Time);

if (!_hasPreviousClose)
{
// Capture the very first closing price to initialize internal state.
_previousClose = candle.ClosePrice;
_emaxtra = candle.ClosePrice;
_hasPreviousClose = true;
IsFormed = false;
			return new DecimalIndicatorValue(this, default, input.Time);
		}

		var prevClose = _previousClose;
		var high = candle.HighPrice;
		var low = candle.LowPrice;

var tr = high - low;
var highDiff = Math.Abs(high - prevClose);
if (highDiff > tr)
tr = highDiff;

var lowDiff = Math.Abs(low - prevClose);
if (lowDiff > tr)
tr = lowDiff;

if (_trBuffer.Count < AtrPeriod)
{
// Fill the circular buffer until it reaches the ATR length.
_trBuffer.Add(tr);
_bufferIndex = _trBuffer.Count % Math.Max(AtrPeriod, 1);
}
else
{
// Replace the oldest value with the newest true range.
_trBuffer[_bufferIndex] = tr;
_bufferIndex = (_bufferIndex + 1) % AtrPeriod;
}

		_previousClose = candle.ClosePrice;

		if (_trBuffer.Count < AtrPeriod || AtrPeriod <= 0)
		{
			IsFormed = false;
			return new DecimalIndicatorValue(this, default, input.Time);
		}

if (!_riverInitialized)
{
// Determine the initial river direction from the last two closes.
_river = candle.ClosePrice >= prevClose;
_emaxtra = prevClose;
_riverInitialized = true;
}

		var atr = CalculateAtr();
		var widcha = Cecf * atr;

		if (_river && low < _emaxtra - widcha)
		{
			_river = false;
			_emaxtra = high;
		}
		else if (!_river && high > _emaxtra + widcha)
		{
			_river = true;
			_emaxtra = low;
		}

		if (_river && low > _emaxtra)
			_emaxtra = low;
		else if (!_river && high < _emaxtra)
			_emaxtra = high;

var color = _river
? (candle.OpenPrice <= candle.ClosePrice ? 0 : 1)
: (candle.OpenPrice >= candle.ClosePrice ? 4 : 3);

IsFormed = true;
return new DecimalIndicatorValue(this, color, input.Time);
}

/// <inheritdoc />
public override void Reset()
{
// Clear buffers and reset the internal state machine.
base.Reset();
_trBuffer.Clear();
_bufferIndex = 0;
_hasPreviousClose = false;
_river = true;
		_riverInitialized = false;
		_emaxtra = 0m;
	}

private decimal CalculateAtr()
{
// Weighted ATR calculation replicating the original BrainTrend logic.
var length = Math.Max(AtrPeriod, 1);
var weight = (decimal)length;
var index = _bufferIndex - 1;
if (index < 0)
index = length - 1;

decimal sum = 0m;
for (var i = 0; i < length; i++)
{
sum += _trBuffer[index] * weight;
weight -= 1m;
index--;
if (index < 0)
index = length - 1;
}

		var denominator = Dartp * (Dartp + 1m);
		if (denominator == 0m)
			return 0m;

		return 2m * sum / denominator;
	}
}

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD pattern strategy converted from the FORTRADER "MacdPatternTraderv01" expert advisor.
/// </summary>
public class MacdPatternTraderV01Strategy : Strategy
{
	private const int HistoryLimit = 1000;

	private readonly StrategyParam<int> _stopLossBars;
	private readonly StrategyParam<int> _takeProfitBars;
	private readonly StrategyParam<int> _offsetPoints;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<decimal> _bearishThreshold;
	private readonly StrategyParam<decimal> _bullishThreshold;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _emaShortPeriod;
	private readonly StrategyParam<int> _emaMediumPeriod;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _emaLongPeriod;
	private readonly StrategyParam<decimal> _profitThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private MACD _macd = null!;
	private ExponentialMovingAverage _emaShort = null!;
	private ExponentialMovingAverage _emaMedium = null!;
	private SimpleMovingAverage _sma = null!;
	private ExponentialMovingAverage _emaLong = null!;

	private decimal? _macdPrev1;
	private decimal? _macdPrev2;
	private decimal? _macdPrev3;

	private bool _bearishArmed;
	private bool _bullishArmed;
	private bool _pendingSell;
	private bool _pendingBuy;

	private decimal? _stopPrice;
	private decimal? _takePrice;
	private decimal? _entryPrice;
	private decimal? _initialVolume;

	private int _buyPartialStage;
	private int _sellPartialStage;

	private readonly List<decimal> _recentLows = new();
	private readonly List<decimal> _recentHighs = new();

	/// <summary>
	/// Number of finished candles used to determine the stop-loss reference high/low.
	/// </summary>
	public int StopLossBars { get => _stopLossBars.Value; set => _stopLossBars.Value = value; }

	/// <summary>
	/// Number of candles used by the recursive take-profit search.
	/// </summary>
	public int TakeProfitBars { get => _takeProfitBars.Value; set => _takeProfitBars.Value = value; }

	/// <summary>
	/// Offset in points added to the calculated stop level.
	/// </summary>
	public int OffsetPoints { get => _offsetPoints.Value; set => _offsetPoints.Value = value; }

	/// <summary>
	/// Fast EMA period for the MACD indicator.
	/// </summary>
	public int MacdFastPeriod { get => _macdFastPeriod.Value; set => _macdFastPeriod.Value = value; }

	/// <summary>
	/// Slow EMA period for the MACD indicator.
	/// </summary>
	public int MacdSlowPeriod { get => _macdSlowPeriod.Value; set => _macdSlowPeriod.Value = value; }

	/// <summary>
	/// Signal EMA period for the MACD indicator.
	/// </summary>
	public int MacdSignalPeriod { get => _macdSignalPeriod.Value; set => _macdSignalPeriod.Value = value; }

	/// <summary>
	/// Positive MACD level that arms the bearish hook setup.
	/// </summary>
	public decimal BearishThreshold { get => _bearishThreshold.Value; set => _bearishThreshold.Value = value; }

	/// <summary>
	/// Negative MACD level that arms the bullish hook setup.
	/// </summary>
	public decimal BullishThreshold { get => _bullishThreshold.Value; set => _bullishThreshold.Value = value; }

	/// <summary>
	/// Volume for every market order.
	/// </summary>
	public decimal OrderVolume { get => _orderVolume.Value; set => _orderVolume.Value = value; }

	/// <summary>
	/// EMA period used by the first partial exit condition.
	/// </summary>
	public int EmaShortPeriod { get => _emaShortPeriod.Value; set => _emaShortPeriod.Value = value; }

	/// <summary>
	/// EMA period used by the crossover filter and the the first partial exit.
	/// </summary>
	public int EmaMediumPeriod { get => _emaMediumPeriod.Value; set => _emaMediumPeriod.Value = value; }

	/// <summary>
	/// SMA period participating in the second partial exit.
	/// </summary>
	public int SmaPeriod { get => _smaPeriod.Value; set => _smaPeriod.Value = value; }

	/// <summary>
	/// Long-term EMA period used inside the composite exit average.
	/// </summary>
	public int EmaLongPeriod { get => _emaLongPeriod.Value; set => _emaLongPeriod.Value = value; }

	/// <summary>
	/// Minimal floating profit (in currency) required before partial exits are allowed.
	/// </summary>
	public decimal ProfitThreshold { get => _profitThreshold.Value; set => _profitThreshold.Value = value; }

	/// <summary>
	/// Candle data type used by the strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="MacdPatternTraderV01Strategy"/>.
	/// </summary>
	public MacdPatternTraderV01Strategy()
	{
		_stopLossBars = Param(nameof(StopLossBars), 6)
		.SetGreaterThanZero()
		.SetDisplay("Stop-Loss Bars", "Bars used to determine the stop-loss swing", "Risk")
		.SetCanOptimize(true);

		_takeProfitBars = Param(nameof(TakeProfitBars), 20)
		.SetGreaterThanZero()
		.SetDisplay("Take-Profit Bars", "Bars per block for the recursive take-profit", "Risk")
		.SetCanOptimize(true);

		_offsetPoints = Param(nameof(OffsetPoints), 10)
		.SetGreaterThanZero()
		.SetDisplay("Stop Offset", "Additional points added to the stop-loss", "Risk");

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "Fast EMA length for MACD", "Indicators");

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 13)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "Slow EMA length for MACD", "Indicators");

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 1)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "Signal EMA length for MACD", "Indicators");

		_bearishThreshold = Param(nameof(BearishThreshold), 0.0045m)
		.SetDisplay("Bearish Threshold", "Positive MACD level that arms short trades", "Signals");

		_bullishThreshold = Param(nameof(BullishThreshold), -0.0045m)
		.SetDisplay("Bullish Threshold", "Negative MACD level that arms long trades", "Signals");

		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Trade volume for every market order", "General");

		_emaShortPeriod = Param(nameof(EmaShortPeriod), 7)
		.SetGreaterThanZero()
		.SetDisplay("EMA Short", "Short EMA period for position management", "Indicators");

		_emaMediumPeriod = Param(nameof(EmaMediumPeriod), 21)
		.SetGreaterThanZero()
		.SetDisplay("EMA Medium", "Medium EMA period for filters", "Indicators");

		_smaPeriod = Param(nameof(SmaPeriod), 98)
		.SetGreaterThanZero()
		.SetDisplay("SMA Period", "SMA period used in the composite exit", "Indicators");

		_emaLongPeriod = Param(nameof(EmaLongPeriod), 365)
		.SetGreaterThanZero()
		.SetDisplay("EMA Long", "Long EMA period used in the composite exit", "Indicators");

		_profitThreshold = Param(nameof(ProfitThreshold), 5m)
		.SetDisplay("Profit Threshold", "Minimum floating profit before scaling out", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Source series for the strategy", "General");
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

	_macdPrev1 = null;
	_macdPrev2 = null;
	_macdPrev3 = null;
	_bearishArmed = false;
	_bullishArmed = false;
	_pendingSell = false;
	_pendingBuy = false;
	_stopPrice = null;
	_takePrice = null;
	_entryPrice = null;
	_initialVolume = null;
	_buyPartialStage = 0;
	_sellPartialStage = 0;
	_recentLows.Clear();
	_recentHighs.Clear();
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
	base.OnStarted(time);

	Volume = OrderVolume;

	_macd = new MACD
	{
		ShortPeriod = MacdFastPeriod,
		LongPeriod = MacdSlowPeriod,
		SignalPeriod = MacdSignalPeriod
	};

_emaShort = new ExponentialMovingAverage { Length = EmaShortPeriod };
_emaMedium = new ExponentialMovingAverage { Length = EmaMediumPeriod };
_sma = new SimpleMovingAverage { Length = SmaPeriod };
_emaLong = new ExponentialMovingAverage { Length = EmaLongPeriod };

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(_macd, _emaShort, _emaMedium, _sma, _emaLong, ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
	DrawCandles(area, subscription);
	DrawIndicator(area, _macd);
	DrawIndicator(area, _emaMedium);
	DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, decimal macdLine, decimal macdSignal, decimal emaShortValue, decimal emaMediumValue, decimal smaValue, decimal emaLongValue)
{
	if (candle.State != CandleStates.Finished)
	return;

	_ = macdSignal;

		// Check protective targets before generating new signals.
	HandleProtectiveExits(candle);
		// Try scaling out according to the EMA/SMA management rules.
	HandlePartialExits(candle, emaMediumValue, smaValue, emaLongValue);

	if (!_macd.IsFormed)
	{
		// Keep collecting MACD values while the indicator is not formed yet.
		UpdateMacdHistory(macdLine);
		StoreCandleExtremes(candle);
		return;
	}

	// Store the freshly calculated MACD value for pattern detection.
UpdateMacdHistory(macdLine);

if (_macdPrev1 is null || _macdPrev2 is null || _macdPrev3 is null)
{
	StoreCandleExtremes(candle);
	return;
}

var macdCurr = _macdPrev1.Value;
var macdLast = _macdPrev2.Value;
var macdLast3 = _macdPrev3.Value;

	// A new bullish MACD extreme arms the potential bearish hook sequence.
if (macdCurr > BearishThreshold)
_bearishArmed = true;

if (macdCurr < 0m)
_bearishArmed = false;

if (macdCurr < BearishThreshold && macdCurr < macdLast && macdLast > macdLast3 && _bearishArmed && macdCurr > 0m && macdLast3 < BearishThreshold)
{
	_pendingSell = true;
}

	// Execute a short entry once all bearish requirements are met.
if (_pendingSell)
{
	TryEnterShort(candle);
}

	// A deep negative MACD swing arms the bullish hook setup.
if (macdCurr < BullishThreshold)
_bullishArmed = true;

if (macdCurr > 0m)
_bullishArmed = false;

if (macdCurr > BullishThreshold && macdCurr < 0m && macdCurr > macdLast && macdLast < macdLast3 && _bullishArmed && macdLast3 > BullishThreshold)
{
	_pendingBuy = true;
}

	// Execute a long entry once all bullish requirements are satisfied.
if (_pendingBuy)
{
	TryEnterLong(candle);
}

StoreCandleExtremes(candle);
}

private void HandleProtectiveExits(ICandleMessage candle)
{
	if (Position > 0)
	{
		if (_stopPrice is decimal stop && candle.LowPrice <= stop)
		{
			SellMarket(Position);
			ResetPositionState();
			return;
		}

	if (_takePrice is decimal take && candle.HighPrice >= take)
	{
		SellMarket(Position);
		ResetPositionState();
		return;
	}
}
else if (Position < 0)
{
	var volume = Math.Abs(Position);

	if (_stopPrice is decimal stop && candle.HighPrice >= stop)
	{
		BuyMarket(volume);
		ResetPositionState();
		return;
	}

if (_takePrice is decimal take && candle.LowPrice <= take)
{
	BuyMarket(volume);
	ResetPositionState();
}
}
}

private void HandlePartialExits(ICandleMessage candle, decimal emaMediumValue, decimal smaValue, decimal emaLongValue)
{
	if (Position == 0 || _entryPrice is null)
	return;

	// Evaluate the floating PnL to decide whether partial exits are allowed.
	var profit = CalculateFloatingProfit(candle.ClosePrice);
	if (profit < ProfitThreshold)
	return;

	if (Position > 0)
	{
		if (_buyPartialStage == 0 && candle.ClosePrice > emaMediumValue)
		{
			var volume = GetInitialPortionVolume(3m);
			if (volume > 0m)
			{
				SellMarket(Math.Min(volume, Position));
				_buyPartialStage = 1;
			}
	}
else if (_buyPartialStage == 1)
{
		// Combine the slow averages to reproduce the MQL composite threshold.
	var composite = (smaValue + emaLongValue) / 2m;
	if (candle.HighPrice > composite)
	{
		var volume = Math.Abs(Position) / 2m;
		if (volume > 0m)
		{
			SellMarket(volume);
			_buyPartialStage = 2;
		}
}
}
}
else if (Position < 0)
{
	var absPosition = Math.Abs(Position);

	if (_sellPartialStage == 0 && candle.ClosePrice < emaMediumValue)
	{
		var volume = GetInitialPortionVolume(3m);
		if (volume > 0m)
		{
			BuyMarket(Math.Min(volume, absPosition));
			_sellPartialStage = 1;
		}
}
else if (_sellPartialStage == 1)
{
	var composite = (smaValue + emaLongValue) / 2m;
	if (candle.LowPrice < composite)
	{
		var volume = absPosition / 2m;
		if (volume > 0m)
		{
			BuyMarket(volume);
			_sellPartialStage = 2;
		}
}
}
}
}

private decimal GetInitialPortionVolume(decimal divider)
{
	if (_initialVolume is null || _initialVolume.Value <= 0m)
	return 0m;

	return _initialVolume.Value / divider;
}

private decimal CalculateFloatingProfit(decimal price)
{
	if (_entryPrice is null || Security is null)
	return 0m;

	var positionVolume = Math.Abs(Position);
	if (positionVolume == 0m)
	return 0m;

	var priceStep = Security.PriceStep ?? 0m;
	var stepPrice = Security.StepPrice ?? priceStep;

	if (priceStep <= 0m || stepPrice <= 0m)
	return 0m;

	var diff = price - _entryPrice.Value;
	var steps = diff / priceStep;
	var money = steps * stepPrice * positionVolume;

	return Position > 0 ? money : -money;
}

private void TryEnterShort(ICandleMessage candle)
{
	_pendingSell = false;
	_bearishArmed = false;

	if (Position < 0)
	return;

	if (OrderVolume <= 0m)
	return;

	// Replicate the MQL stop-loss that scans past highs plus the configured offset.
	var stop = CalculateStopPrice(false);
	// Compute the recursive take-profit using the finished candle lows.
	var take = CalculateTakePrice(false, candle);

	if (stop is null || take is null)
	return;

	SellMarket(OrderVolume);

	_stopPrice = stop;
	_takePrice = take;
	_entryPrice = candle.ClosePrice;
	_initialVolume = OrderVolume;
	_sellPartialStage = 0;
	_buyPartialStage = 0;
}

private void TryEnterLong(ICandleMessage candle)
{
	_pendingBuy = false;
	_bullishArmed = false;

	if (Position > 0)
	return;

	if (OrderVolume <= 0m)
	return;

	// Derive the long stop-loss from the recent swing lows.
	var stop = CalculateStopPrice(true);
	// Search for the layered bullish target via the recursive high scan.
	var take = CalculateTakePrice(true, candle);

	if (stop is null || take is null)
	return;

	BuyMarket(OrderVolume);

	_stopPrice = stop;
	_takePrice = take;
	_entryPrice = candle.ClosePrice;
	_initialVolume = OrderVolume;
	_buyPartialStage = 0;
	_sellPartialStage = 0;
}

private decimal? CalculateStopPrice(bool isLong)
{
	var length = StopLossBars;
	// Invalid configuration means the level cannot be evaluated.
	if (length <= 0)
	return null;

	if (isLong)
	{
		// Not enough historical lows to reproduce the original lookback.
		if (_recentLows.Count < length)
		return null;

			// Manual iteration avoids allocating additional buffers for extrema search.
		var min = decimal.MaxValue;
		for (var i = _recentLows.Count - length; i < _recentLows.Count; i++)
		{
			var value = _recentLows[i];
			if (value < min)
			min = value;
		}

	var offset = GetOffsetPrice();
	return min - offset;
}
else
{
	if (_recentHighs.Count < length)
	return null;

			// Mirror the same manual search for the highest value.
	var max = decimal.MinValue;
	for (var i = _recentHighs.Count - length; i < _recentHighs.Count; i++)
	{
		var value = _recentHighs[i];
		if (value > max)
		max = value;
	}

var offset = GetOffsetPrice();
return max + offset;
}
}

private decimal? CalculateTakePrice(bool isLong, ICandleMessage candle)
{
	var length = TakeProfitBars;
	if (length <= 0)
	return null;

	var totalWithCurrent = _recentHighs.Count + 1;
	if (totalWithCurrent < length)
	return null;
		// Iterate over consecutive blocks until the extrema stop improving.
		decimal? best = null;
	var segment = 0;
	while (true)
	{
			// Evaluate the next block by combining historical data with the current candle.
		var extreme = isLong
		? GetSegmentExtreme(_recentHighs, candle.HighPrice, length, segment, false)
		: GetSegmentExtreme(_recentLows, candle.LowPrice, length, segment, true);

		if (extreme is null)
		break;

		if (best is null)
		{
			best = extreme;
			segment++;
			continue;
		}

	if (isLong)
	{
		if (extreme.Value > best.Value)
		{
			best = extreme;
			segment++;
			continue;
		}
}
else
{
	if (extreme.Value < best.Value)
	{
		best = extreme;
		segment++;
		continue;
	}
}

break;
}

return best;
}

private decimal? GetSegmentExtreme(List<decimal> source, decimal currentValue, int length, int segmentIndex, bool isMin)
{
	// Treat the finished candle as an extra sample appended to the stored history.
	var total = source.Count + 1;
	var end = total - 1 - segmentIndex * length;
	var start = end - length + 1;

	if (start < 0)
	return null;

	decimal extreme = isMin ? decimal.MaxValue : decimal.MinValue;

	for (var i = start; i <= end; i++)
	{
		var value = i == source.Count ? currentValue : source[i];

		if (isMin)
		{
			if (value < extreme)
			extreme = value;
		}
	else
	{
		if (value > extreme)
		extreme = value;
	}
}

return extreme;
}

private decimal GetOffsetPrice()
{
	// Convert the configured offset from points into an absolute price distance.
	var priceStep = Security?.PriceStep ?? 0m;
	if (priceStep <= 0m)
	return 0m;

	return OffsetPoints * priceStep;
}

private void StoreCandleExtremes(ICandleMessage candle)
{
	_recentLows.Add(candle.LowPrice);
	_recentHighs.Add(candle.HighPrice);

	if (_recentLows.Count > HistoryLimit)
	_recentLows.RemoveAt(0);

	if (_recentHighs.Count > HistoryLimit)
	_recentHighs.RemoveAt(0);
}

private void ResetPositionState()
{
	_stopPrice = null;
	_takePrice = null;
	_entryPrice = null;
	_initialVolume = null;
	_buyPartialStage = 0;
	_sellPartialStage = 0;
}

private void UpdateMacdHistory(decimal macdLine)
{
	_macdPrev3 = _macdPrev2;
	_macdPrev2 = _macdPrev1;
	_macdPrev1 = macdLine;
}
}

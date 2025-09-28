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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bill Williams Alligator strategy with optional fractal filter, martingale grid, and trailing exit.
/// Automatically mirrors the behavior of the "Alligator vol 1.1" MetaTrader expert.
/// </summary>
public class AlligatorVolatilityStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _manualMode;
	private readonly StrategyParam<bool> _useAlligatorEntry;
	private readonly StrategyParam<bool> _useFractalFilter;
	private readonly StrategyParam<bool> _useAlligatorExit;
	private readonly StrategyParam<bool> _onlyOnePosition;
	private readonly StrategyParam<bool> _enableMartingale;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<decimal> _riskPerThousand;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<decimal> _minVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingActivationPips;
	private readonly StrategyParam<decimal> _entryGap;
	private readonly StrategyParam<decimal> _exitGap;
	private readonly StrategyParam<int> _jawPeriod;
	private readonly StrategyParam<int> _jawShift;
	private readonly StrategyParam<int> _teethPeriod;
	private readonly StrategyParam<int> _teethShift;
	private readonly StrategyParam<int> _lipsPeriod;
	private readonly StrategyParam<int> _lipsShift;
	private readonly StrategyParam<int> _fractalBars;
	private readonly StrategyParam<int> _fractalDistancePips;
	private readonly StrategyParam<int> _martingaleDepth;
	private readonly StrategyParam<decimal> _martingaleMultiplier;
	private readonly StrategyParam<int> _gridSpreadPips;

	private SmoothedMovingAverage _jaw = null!;
	private SmoothedMovingAverage _teeth = null!;
	private SmoothedMovingAverage _lips = null!;

	private decimal?[] _jawHistory = Array.Empty<decimal?>();
	private decimal?[] _teethHistory = Array.Empty<decimal?>();
	private decimal?[] _lipsHistory = Array.Empty<decimal?>();

	private decimal[] _recentHighs = Array.Empty<decimal>();
	private decimal[] _recentLows = Array.Empty<decimal>();

	private bool _canBuyState;
	private bool _canSellState;
	private bool _longExitRequested;
	private bool _shortExitRequested;

	private decimal _pipSize;

	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;

	private readonly List<Order> _pendingLongLimits = new();
	private readonly List<Order> _pendingShortLimits = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="AlligatorVolatilityStrategy"/> class.
	/// </summary>
	public AlligatorVolatilityStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for candle subscription.", "General");

		_manualMode = Param(nameof(ManualMode), false)
			.SetDisplay("Manual Mode", "Disable automatic entries when enabled.", "Trading");

		_useAlligatorEntry = Param(nameof(UseAlligatorEntry), true)
			.SetDisplay("Use Alligator Entry", "Require Alligator expansion for entries.", "Alligator");

		_useFractalFilter = Param(nameof(UseFractalFilter), false)
			.SetDisplay("Use Fractal Filter", "Require fractal breakout confirmation.", "Fractals");

		_useAlligatorExit = Param(nameof(UseAlligatorExit), false)
			.SetDisplay("Use Alligator Exit", "Close positions when the Alligator closes.", "Alligator");

		_onlyOnePosition = Param(nameof(OnlyOnePosition), true)
			.SetDisplay("Single Position", "Allow only one open position at a time.", "Trading");

		_enableMartingale = Param(nameof(EnableMartingale), true)
			.SetDisplay("Enable Martingale", "Place averaging limit orders after entry.", "Martingale");

		_enableTrailing = Param(nameof(EnableTrailing), true)
			.SetDisplay("Enable Trailing", "Activate trailing stop management.", "Risk");

		_riskPerThousand = Param(nameof(RiskPerThousand), 0.04m)
			.SetDisplay("Risk per 1000", "Volume multiplier based on account equity.", "Risk");

		_maxVolume = Param(nameof(MaxVolume), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("Maximum Volume", "Upper limit for calculated volume.", "Risk");

		_minVolume = Param(nameof(MinVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Minimum Volume", "Fallback volume when equity is unavailable.", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 80)
			.SetNotNegative()
			.SetDisplay("Stop-Loss (pips)", "Distance to initial protective stop.", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 80)
			.SetNotNegative()
			.SetDisplay("Take-Profit (pips)", "Distance to initial profit target.", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 30)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance when enabled.", "Risk");

		_trailingActivationPips = Param(nameof(TrailingActivationPips), 20)
			.SetNotNegative()
			.SetDisplay("Trailing Activation (pips)", "Minimum profit before trailing adjusts.", "Risk");

		_entryGap = Param(nameof(EntryGap), 0.0005m)
			.SetNotNegative()
			.SetDisplay("Entry Gap", "Minimum distance between Lips and Jaw for entries.", "Alligator");

		_exitGap = Param(nameof(ExitGap), 0.0001m)
			.SetNotNegative()
			.SetDisplay("Exit Gap", "Minimum distance between lines to keep the position.", "Alligator");

		_jawPeriod = Param(nameof(JawPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("Jaw Period", "Alligator jaw smoothing length.", "Alligator");

		_jawShift = Param(nameof(JawShift), 8)
			.SetNotNegative()
			.SetDisplay("Jaw Shift", "Jaw line shift in bars.", "Alligator");

		_teethPeriod = Param(nameof(TeethPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Teeth Period", "Alligator teeth smoothing length.", "Alligator");

		_teethShift = Param(nameof(TeethShift), 5)
			.SetNotNegative()
			.SetDisplay("Teeth Shift", "Teeth line shift in bars.", "Alligator");

		_lipsPeriod = Param(nameof(LipsPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lips Period", "Alligator lips smoothing length.", "Alligator");

		_lipsShift = Param(nameof(LipsShift), 3)
			.SetNotNegative()
			.SetDisplay("Lips Shift", "Lips line shift in bars.", "Alligator");

		_fractalBars = Param(nameof(FractalBars), 10)
			.SetNotNegative()
			.SetDisplay("Fractal Bars", "Number of completed candles to scan for fractals.", "Fractals");

		_fractalDistancePips = Param(nameof(FractalDistancePips), 30)
			.SetNotNegative()
			.SetDisplay("Fractal Distance (pips)", "Minimum distance between price and fractal.", "Fractals");

		_martingaleDepth = Param(nameof(MartingaleDepth), 10)
			.SetNotNegative()
			.SetDisplay("Martingale Depth", "Number of averaging limit orders.", "Martingale");

		_martingaleMultiplier = Param(nameof(MartingaleMultiplier), 1.3m)
			.SetGreaterThanZero()
			.SetDisplay("Martingale Multiplier", "Additional multiplier applied to double-up volumes.", "Martingale");

		_gridSpreadPips = Param(nameof(GridSpreadPips), 10)
			.SetNotNegative()
			.SetDisplay("Grid Spread (pips)", "Offset applied to martingale limit orders.", "Martingale");
	}

	/// <summary>
	/// Candle type for subscriptions.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Switch to disable automated entries.
	/// </summary>
	public bool ManualMode { get => _manualMode.Value; set => _manualMode.Value = value; }

	/// <summary>
	/// Require Alligator expansion for entries.
	/// </summary>
	public bool UseAlligatorEntry { get => _useAlligatorEntry.Value; set => _useAlligatorEntry.Value = value; }

	/// <summary>
	/// Require fractal breakout confirmation.
	/// </summary>
	public bool UseFractalFilter { get => _useFractalFilter.Value; set => _useFractalFilter.Value = value; }

	/// <summary>
	/// Enable Alligator-based exit rules.
	/// </summary>
	public bool UseAlligatorExit { get => _useAlligatorExit.Value; set => _useAlligatorExit.Value = value; }

	/// <summary>
	/// Allow only one position at a time.
	/// </summary>
	public bool OnlyOnePosition { get => _onlyOnePosition.Value; set => _onlyOnePosition.Value = value; }

	/// <summary>
	/// Enable martingale averaging grid.
	/// </summary>
	public bool EnableMartingale { get => _enableMartingale.Value; set => _enableMartingale.Value = value; }

	/// <summary>
	/// Enable trailing stop management.
	/// </summary>
	public bool EnableTrailing { get => _enableTrailing.Value; set => _enableTrailing.Value = value; }

	/// <summary>
	/// Risk multiplier per 1000 units of equity.
	/// </summary>
	public decimal RiskPerThousand { get => _riskPerThousand.Value; set => _riskPerThousand.Value = value; }

	/// <summary>
	/// Maximum allowed order volume.
	/// </summary>
	public decimal MaxVolume { get => _maxVolume.Value; set => _maxVolume.Value = value; }

	/// <summary>
	/// Minimum fallback order volume.
	/// </summary>
	public decimal MinVolume { get => _minVolume.Value; set => _minVolume.Value = value; }

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public int StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public int TakeProfitPips { get => _takeProfitPips.Value; set => _takeProfitPips.Value = value; }

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public int TrailingStopPips { get => _trailingStopPips.Value; set => _trailingStopPips.Value = value; }

	/// <summary>
	/// Minimum profit in pips before trailing activates.
	/// </summary>
	public int TrailingActivationPips { get => _trailingActivationPips.Value; set => _trailingActivationPips.Value = value; }

	/// <summary>
	/// Minimum distance between Lips and Jaw for entries.
	/// </summary>
	public decimal EntryGap { get => _entryGap.Value; set => _entryGap.Value = value; }

	/// <summary>
	/// Minimum distance required to keep the position open.
	/// </summary>
	public decimal ExitGap { get => _exitGap.Value; set => _exitGap.Value = value; }

	/// <summary>
	/// Alligator jaw period.
	/// </summary>
	public int JawPeriod { get => _jawPeriod.Value; set => _jawPeriod.Value = value; }

	/// <summary>
	/// Jaw line shift in bars.
	/// </summary>
	public int JawShift { get => _jawShift.Value; set => _jawShift.Value = value; }

	/// <summary>
	/// Alligator teeth period.
	/// </summary>
	public int TeethPeriod { get => _teethPeriod.Value; set => _teethPeriod.Value = value; }

	/// <summary>
	/// Teeth line shift in bars.
	/// </summary>
	public int TeethShift { get => _teethShift.Value; set => _teethShift.Value = value; }

	/// <summary>
	/// Alligator lips period.
	/// </summary>
	public int LipsPeriod { get => _lipsPeriod.Value; set => _lipsPeriod.Value = value; }

	/// <summary>
	/// Lips line shift in bars.
	/// </summary>
	public int LipsShift { get => _lipsShift.Value; set => _lipsShift.Value = value; }

	/// <summary>
	/// Number of candles to inspect for fractal confirmation.
	/// </summary>
	public int FractalBars { get => _fractalBars.Value; set => _fractalBars.Value = value; }

	/// <summary>
	/// Minimum fractal distance from price in pips.
	/// </summary>
	public int FractalDistancePips { get => _fractalDistancePips.Value; set => _fractalDistancePips.Value = value; }

	/// <summary>
	/// Number of martingale averaging orders.
	/// </summary>
	public int MartingaleDepth { get => _martingaleDepth.Value; set => _martingaleDepth.Value = value; }

	/// <summary>
	/// Martingale multiplier applied after doubling volume.
	/// </summary>
	public decimal MartingaleMultiplier { get => _martingaleMultiplier.Value; set => _martingaleMultiplier.Value = value; }

	/// <summary>
	/// Spread offset for martingale grid in pips.
	/// </summary>
	public int GridSpreadPips { get => _gridSpreadPips.Value; set => _gridSpreadPips.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_jawHistory = Array.Empty<decimal?>();
		_teethHistory = Array.Empty<decimal?>();
		_lipsHistory = Array.Empty<decimal?>();

		_recentHighs = Array.Empty<decimal>();
		_recentLows = Array.Empty<decimal>();

		_canBuyState = false;
		_canSellState = false;
		_longExitRequested = false;
		_shortExitRequested = false;

		_pipSize = 0m;

		_longStopPrice = null;
		_longTakePrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;

		_pendingLongLimits.Clear();
		_pendingShortLimits.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_jaw = new SmoothedMovingAverage { Length = JawPeriod };
		_teeth = new SmoothedMovingAverage { Length = TeethPeriod };
		_lips = new SmoothedMovingAverage { Length = LipsPeriod };

		_jawHistory = CreateHistoryBuffer(JawShift);
		_teethHistory = CreateHistoryBuffer(TeethShift);
		_lipsHistory = CreateHistoryBuffer(LipsShift);

		_recentHighs = new decimal[Math.Max(FractalBars, 5)];
		_recentLows = new decimal[Math.Max(FractalBars, 5)];

		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _jaw);
			DrawIndicator(area, _teeth);
			DrawIndicator(area, _lips);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position == 0m)
		{
			_longStopPrice = null;
			_longTakePrice = null;
			_shortStopPrice = null;
			_shortTakePrice = null;
			_longExitRequested = false;
			_shortExitRequested = false;
			return;
		}

		var entryPrice = PositionPrice;

		if (Position > 0m && delta > 0m)
		{
			_longStopPrice = ConvertPipsToPrice(StopLossPips) > 0m ? entryPrice - ConvertPipsToPrice(StopLossPips) : (decimal?)null;
			_longTakePrice = ConvertPipsToPrice(TakeProfitPips) > 0m ? entryPrice + ConvertPipsToPrice(TakeProfitPips) : (decimal?)null;
			_longExitRequested = false;
		}
		else if (Position < 0m && delta < 0m)
		{
			_shortStopPrice = ConvertPipsToPrice(StopLossPips) > 0m ? entryPrice + ConvertPipsToPrice(StopLossPips) : (decimal?)null;
			_shortTakePrice = ConvertPipsToPrice(TakeProfitPips) > 0m ? entryPrice - ConvertPipsToPrice(TakeProfitPips) : (decimal?)null;
			_shortExitRequested = false;
		}
	}

	/// <inheritdoc />
	protected override void OnOrderReceived(Order order)
	{
		base.OnOrderReceived(order);

		if (order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
		{
			_pendingLongLimits.Remove(order);
			_pendingShortLimits.Remove(order);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateFractalHistory(candle);
		ManageOpenPositions(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var median = (candle.HighPrice + candle.LowPrice) / 2m;

		var jawValue = _jaw.Process(median, candle.OpenTime, true);
		var teethValue = _teeth.Process(median, candle.OpenTime, true);
		var lipsValue = _lips.Process(median, candle.OpenTime, true);

		if (!jawValue.IsFinal || !teethValue.IsFinal || !lipsValue.IsFinal)
			return;

		var jaw = jawValue.ToDecimal();
		var teeth = teethValue.ToDecimal();
		var lips = lipsValue.ToDecimal();

		UpdateHistory(_jawHistory, jaw);
		UpdateHistory(_teethHistory, teeth);
		UpdateHistory(_lipsHistory, lips);

		if (!TryGetShiftedValue(_jawHistory, JawShift, out var jawPrev) ||
			!TryGetShiftedValue(_teethHistory, TeethShift, out var teethPrev) ||
			!TryGetShiftedValue(_lipsHistory, LipsShift, out var lipsPrev))
		{
			return;
		}

		var longState = !UseAlligatorEntry || EvaluateLongState(lipsPrev, jawPrev, teethPrev);
		var shortState = !UseAlligatorEntry || EvaluateShortState(lipsPrev, jawPrev, teethPrev);

		var previousLong = _canBuyState;
		var previousShort = _canSellState;

		_canBuyState = longState;
		_canSellState = shortState;

		if (UseAlligatorExit)
		{
			if (previousLong && !longState && Position > 0m)
				TryCloseLong();

			if (previousShort && !shortState && Position < 0m)
				TryCloseShort();
		}

		if (ManualMode)
			return;

		if (Position >= 0m)
		{
			var longSignal = (!UseAlligatorEntry || (!previousLong && longState)) &&
				(!OnlyOnePosition || Position <= 0m) &&
				(!UseFractalFilter || HasFractalBreakout(true, candle.ClosePrice));

			if (longSignal)
				EnterLong(candle);
		}

		if (Position <= 0m)
		{
			var shortSignal = (!UseAlligatorEntry || (!previousShort && shortState)) &&
				(!OnlyOnePosition || Position >= 0m) &&
				(!UseFractalFilter || HasFractalBreakout(false, candle.ClosePrice));

			if (shortSignal)
				EnterShort(candle);
		}
	}

	private void EnterLong(ICandleMessage candle)
	{
		CancelPendingOrders(_pendingLongLimits);

		var volume = CalculateOrderVolume();
		if (volume <= 0m)
			return;

		BuyMarket(volume: volume);

		if (!EnableMartingale)
			return;

		PlaceMartingaleOrders(true, candle.ClosePrice, volume);
	}

	private void EnterShort(ICandleMessage candle)
	{
		CancelPendingOrders(_pendingShortLimits);

		var volume = CalculateOrderVolume();
		if (volume <= 0m)
			return;

		SellMarket(volume: volume);

		if (!EnableMartingale)
			return;

		PlaceMartingaleOrders(false, candle.ClosePrice, volume);
	}

	private void ManageOpenPositions(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			if (_longTakePrice is decimal take && candle.HighPrice >= take)
			{
				TryCloseLong();
				return;
			}

			if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
			{
				TryCloseLong();
				return;
			}

			if (EnableTrailing && TrailingStopPips > 0)
				UpdateTrailingForLong(candle);
		}
		else if (Position < 0m)
		{
			if (_shortTakePrice is decimal take && candle.LowPrice <= take)
			{
				TryCloseShort();
				return;
			}

			if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
			{
				TryCloseShort();
				return;
			}

			if (EnableTrailing && TrailingStopPips > 0)
				UpdateTrailingForShort(candle);
		}
	}

	private void UpdateTrailingForLong(ICandleMessage candle)
	{
		var activationDistance = ConvertPipsToPrice(TrailingActivationPips);
		var trailDistance = ConvertPipsToPrice(TrailingStopPips);
		if (trailDistance <= 0m)
			return;

		var referencePrice = Math.Max(candle.HighPrice, candle.ClosePrice);
		if (referencePrice - PositionPrice < activationDistance)
			return;

		var desiredStop = referencePrice - trailDistance;
		if (_longStopPrice is not decimal currentStop || desiredStop > currentStop)
			_longStopPrice = desiredStop;
	}

	private void UpdateTrailingForShort(ICandleMessage candle)
	{
		var activationDistance = ConvertPipsToPrice(TrailingActivationPips);
		var trailDistance = ConvertPipsToPrice(TrailingStopPips);
		if (trailDistance <= 0m)
			return;

		var referencePrice = Math.Min(candle.LowPrice, candle.ClosePrice);
		if (PositionPrice - referencePrice < activationDistance)
			return;

		var desiredStop = referencePrice + trailDistance;
		if (_shortStopPrice is not decimal currentStop || desiredStop < currentStop)
			_shortStopPrice = desiredStop;
	}

	private void TryCloseLong()
	{
		if (_longExitRequested)
			return;

		_longExitRequested = true;
		CancelPendingOrders(_pendingLongLimits);
		SellMarket(volume: Math.Abs(Position));
	}

	private void TryCloseShort()
	{
		if (_shortExitRequested)
			return;

		_shortExitRequested = true;
		CancelPendingOrders(_pendingShortLimits);
		BuyMarket(volume: Math.Abs(Position));
	}

	private void PlaceMartingaleOrders(bool isLong, decimal referencePrice, decimal initialVolume)
	{
		var depth = MartingaleDepth;
		if (depth <= 0)
			return;

		var step = ConvertPipsToPrice(Math.Max(StopLossPips, 1));
		if (step <= 0m)
			return;

		var spreadOffset = ConvertPipsToPrice(GridSpreadPips);

		var volume = initialVolume;
		for (var i = 1; i <= depth; i++)
		{
			volume = Math.Min(MaxVolume, volume * 2m * MartingaleMultiplier);
			if (volume <= 0m)
				break;

			var price = isLong
				? referencePrice - step * i + spreadOffset
				: referencePrice + step * i - spreadOffset;

			price = RoundPrice(price);

			Order order = isLong
				? BuyLimit(price, volume)
				: SellLimit(price, volume);

			if (order == null)
				continue;

			if (isLong)
				_pendingLongLimits.Add(order);
			else
				_pendingShortLimits.Add(order);
		}
	}

	private bool HasFractalBreakout(bool isLong, decimal currentPrice)
	{
		if (_recentHighs.Length < 5)
			return false;

		var minDistance = ConvertPipsToPrice(FractalDistancePips);
		if (minDistance < 0m)
			minDistance = 0m;

		if (isLong)
		{
			var fractalHigh = FindFractalHigh();
			return fractalHigh.HasValue && fractalHigh.Value >= currentPrice + minDistance;
		}

		var fractalLow = FindFractalLow();
		return fractalLow.HasValue && fractalLow.Value <= currentPrice - minDistance;
	}

	private decimal? FindFractalHigh()
	{
		decimal? best = null;
		for (var i = 2; i < _recentHighs.Length - 2; i++)
		{
			var center = _recentHighs[i];
			if (center == 0m)
				continue;

			if (center > _recentHighs[i - 1] && center > _recentHighs[i - 2] &&
				center >= _recentHighs[i + 1] && center >= _recentHighs[i + 2])
			{
				if (best == null || center > best.Value)
					best = center;
			}
		}

		return best;
	}

	private decimal? FindFractalLow()
	{
		decimal? best = null;
		for (var i = 2; i < _recentLows.Length - 2; i++)
		{
			var center = _recentLows[i];
			if (center == 0m)
				continue;

			if (center < _recentLows[i - 1] && center < _recentLows[i - 2] &&
				center <= _recentLows[i + 1] && center <= _recentLows[i + 2])
			{
				if (best == null || center < best.Value)
					best = center;
			}
		}

		return best;
	}

	private bool EvaluateLongState(decimal lips, decimal jaw, decimal teeth)
	{
		if (lips <= jaw + EntryGap)
			return false;

		if (lips + ExitGap < teeth)
			return false;

		return true;
	}

	private bool EvaluateShortState(decimal lips, decimal jaw, decimal teeth)
	{
		if (jaw <= lips + EntryGap)
			return false;

		if (jaw + ExitGap < teeth)
			return false;

		return true;
	}

	private void UpdateFractalHistory(ICandleMessage candle)
	{
		if (_recentHighs.Length == 0 || _recentLows.Length == 0)
			return;

		ShiftArray(_recentHighs, candle.HighPrice);
		ShiftArray(_recentLows, candle.LowPrice);
	}

	private static void ShiftArray(decimal[] buffer, decimal value)
	{
		Array.Copy(buffer, 1, buffer, 0, buffer.Length - 1);
		buffer[^1] = value;
	}

	private void CancelPendingOrders(List<Order> orders)
	{
		foreach (var order in orders.ToList())
		{
			if (order.State is OrderStates.Pending or OrderStates.Active)
				CancelOrder(order);
		}

		orders.Clear();
	}

	private decimal CalculateOrderVolume()
	{
		var minVolume = MinVolume;
		var maxVolume = MaxVolume;

		var portfolio = Portfolio;
		decimal volume = minVolume;

		if (portfolio?.CurrentValue is decimal equity && equity > 0m)
		{
			var riskVolume = equity / 1000m * RiskPerThousand;
			if (riskVolume > 0m)
				volume = Math.Max(volume, riskVolume);
		}

		if (maxVolume > 0m)
			volume = Math.Min(volume, maxVolume);

		return Math.Max(volume, minVolume);
	}

	private decimal ConvertPipsToPrice(int pips)
	{
		if (pips <= 0)
			return 0m;

		return pips * _pipSize;
	}

	private decimal RoundPrice(decimal price)
	{
		var security = Security;
		if (security == null)
			return price;

		var step = security.PriceStep;
		if (step == null || step.Value <= 0m)
			return price;

		return Math.Round(price / step.Value, 0, MidpointRounding.AwayFromZero) * step.Value;
	}

	private static decimal?[] CreateHistoryBuffer(int shift)
	{
		var size = Math.Max(shift + 3, 3);
		return new decimal?[size];
	}

	private static void UpdateHistory(decimal?[] buffer, decimal value)
	{
		if (buffer.Length == 0)
			return;

		Array.Copy(buffer, 1, buffer, 0, buffer.Length - 1);
		buffer[^1] = value;
	}

	private static bool TryGetShiftedValue(decimal?[] buffer, int shift, out decimal value)
	{
		value = 0m;
		if (buffer.Length == 0)
			return false;

		var offset = shift + 2;
		if (offset <= 0)
			offset = 2;

		if (buffer.Length < offset)
			return false;

		var index = buffer.Length - offset;
		if (index < 0)
			return false;

		if (buffer[index] is not decimal stored)
			return false;

		value = stored;
		return true;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 0.0001m;

		var decimals = Security?.Decimals ?? 0;
		if (decimals == 3 || decimals == 5)
			return step * 10m;

		return step;
	}
}

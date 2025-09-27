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
/// Translation of the "FT Bill Williams Trader" MetaTrader expert advisor.
/// The strategy trades fractal breakouts filtered by Bill Williams Alligator alignment and distance constraints.
/// It optionally applies trailing stops based on the Alligator lips slope and closes positions on opposite signals.
/// </summary>
public class FTBillWillamsTraderStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _fractalPeriod;
	private readonly StrategyParam<int> _indentPoints;
	private readonly StrategyParam<EntryMode> _entryMode;
	private readonly StrategyParam<bool> _useTeethFilter;
	private readonly StrategyParam<int> _maxDistancePoints;
	private readonly StrategyParam<bool> _useTrendAlignment;
	private readonly StrategyParam<int> _jawTeethDistancePoints;
	private readonly StrategyParam<int> _teethLipsDistancePoints;
	private readonly StrategyParam<JawExitMode> _jawExitMode;
	private readonly StrategyParam<ReverseExitMode> _reverseExitMode;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<int> _slopeSmaPeriod;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<int> _jawPeriod;
	private readonly StrategyParam<int> _jawShift;
	private readonly StrategyParam<int> _teethPeriod;
	private readonly StrategyParam<int> _teethShift;
	private readonly StrategyParam<int> _lipsPeriod;
	private readonly StrategyParam<int> _lipsShift;
	private readonly StrategyParam<MovingAverageMethod> _maMethod;
	private readonly StrategyParam<CandlePrice> _appliedPrice;
	private readonly StrategyParam<DataType> _candleType;

	private LengthIndicator<decimal> _jaw = null!;
	private LengthIndicator<decimal> _teeth = null!;
	private LengthIndicator<decimal> _lips = null!;
	private SimpleMovingAverage _slopeSma = null!;

	private decimal?[] _jawHistory = Array.Empty<decimal?>();
	private decimal?[] _teethHistory = Array.Empty<decimal?>();
	private decimal?[] _lipsHistory = Array.Empty<decimal?>();
	private decimal?[] _smaHistory = Array.Empty<decimal?>();

	private decimal[] _highBuffer = Array.Empty<decimal>();
	private decimal[] _lowBuffer = Array.Empty<decimal>();
	private int _fractalCount;

	private decimal? _pendingBuyLevel;
	private decimal? _pendingSellLevel;
	private decimal? _triggeredBuyLevel;
	private decimal? _triggeredSellLevel;
	private bool _newUpFractal;
	private bool _newDownFractal;

	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortTakePrice;

	private decimal? _previousClose;

	private decimal _point;
	private decimal _indentDistance;
	private decimal _maxDistance;

	/// <summary>
	/// Order volume expressed in lots or contracts.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Number of bars used to confirm Bill Williams fractals.
	/// </summary>
	public int FractalPeriod
	{
		get => _fractalPeriod.Value;
		set => _fractalPeriod.Value = value;
	}

	/// <summary>
	/// Offset added above/below the fractal level in price points.
	/// </summary>
	public int IndentPoints
	{
		get => _indentPoints.Value;
		set => _indentPoints.Value = value;
	}

	/// <summary>
	/// Breakout confirmation method.
	/// </summary>
	public EntryMode EntryConfirmation
	{
		get => _entryMode.Value;
		set => _entryMode.Value = value;
	}

	/// <summary>
	/// Require the previous close to be on the same side of the Alligator teeth.
	/// </summary>
	public bool UseTeethFilter
	{
		get => _useTeethFilter.Value;
		set => _useTeethFilter.Value = value;
	}

	/// <summary>
	/// Maximum allowed distance between the entry price and the Alligator lips, measured in points.
	/// </summary>
	public int MaxDistancePoints
	{
		get => _maxDistancePoints.Value;
		set => _maxDistancePoints.Value = value;
	}

	/// <summary>
	/// Enforce Alligator alignment before trading.
	/// </summary>
	public bool UseTrendAlignment
	{
		get => _useTrendAlignment.Value;
		set => _useTrendAlignment.Value = value;
	}

	/// <summary>
	/// Minimum separation between the teeth and jaw lines (in points).
	/// </summary>
	public int JawTeethDistancePoints
	{
		get => _jawTeethDistancePoints.Value;
		set => _jawTeethDistancePoints.Value = value;
	}

	/// <summary>
	/// Minimum separation between the lips and teeth lines (in points).
	/// </summary>
	public int TeethLipsDistancePoints
	{
		get => _teethLipsDistancePoints.Value;
		set => _teethLipsDistancePoints.Value = value;
	}

	/// <summary>
	/// Exit mode when price falls back to the Alligator jaw.
	/// </summary>
	public JawExitMode JawExit
	{
		get => _jawExitMode.Value;
		set => _jawExitMode.Value = value;
	}

	/// <summary>
	/// Exit mode triggered by opposite signals.
	/// </summary>
	public ReverseExitMode ReverseExit
	{
		get => _reverseExitMode.Value;
		set => _reverseExitMode.Value = value;
	}

	/// <summary>
	/// Enables trailing stop management based on the Alligator lips slope.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Period of the smoothing SMA used to compare with the lips slope.
	/// </summary>
	public int SlopeSmaPeriod
	{
		get => _slopeSmaPeriod.Value;
		set => _slopeSmaPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss distance measured in points (zero disables the stop).
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance measured in points (zero disables the target).
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Alligator jaw period.
	/// </summary>
	public int JawPeriod
	{
		get => _jawPeriod.Value;
		set => _jawPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the jaw line.
	/// </summary>
	public int JawShift
	{
		get => _jawShift.Value;
		set => _jawShift.Value = value;
	}

	/// <summary>
	/// Alligator teeth period.
	/// </summary>
	public int TeethPeriod
	{
		get => _teethPeriod.Value;
		set => _teethPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the teeth line.
	/// </summary>
	public int TeethShift
	{
		get => _teethShift.Value;
		set => _teethShift.Value = value;
	}

	/// <summary>
	/// Alligator lips period.
	/// </summary>
	public int LipsPeriod
	{
		get => _lipsPeriod.Value;
		set => _lipsPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the lips line.
	/// </summary>
	public int LipsShift
	{
		get => _lipsShift.Value;
		set => _lipsShift.Value = value;
	}

	/// <summary>
	/// Moving-average algorithm used for all Alligator lines.
	/// </summary>
	public MovingAverageMethod MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	/// <summary>
	/// Price source supplied to the moving averages.
	/// </summary>
	public CandlePrice AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// Candle type consumed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters with defaults matching the original expert advisor.
	/// </summary>
	public FTBillWillamsTraderStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Trade volume used for market orders", "Trading");

		_fractalPeriod = Param(nameof(FractalPeriod), 5)
		.SetRange(3, 21)
		.SetDisplay("Fractal Period", "Number of candles used for fractal detection", "Signals");

		_indentPoints = Param(nameof(IndentPoints), 1)
		.SetRange(0, 1000)
		.SetDisplay("Indent", "Points added to the breakout price", "Signals");

		_entryMode = Param(nameof(EntryConfirmation), EntryMode.CloseBreakout)
		.SetDisplay("Entry Mode", "Breakout confirmation method", "Trading");

		_useTeethFilter = Param(nameof(UseTeethFilter), true)
		.SetDisplay("Teeth Filter", "Require the previous close to be above/below the teeth", "Filters");

		_maxDistancePoints = Param(nameof(MaxDistancePoints), 1000)
		.SetRange(0, 100000)
		.SetDisplay("Max Distance", "Maximum distance between entry and lips (points)", "Filters");

		_useTrendAlignment = Param(nameof(UseTrendAlignment), false)
		.SetDisplay("Trend Alignment", "Require Alligator lines to expand in trend direction", "Filters");

		_jawTeethDistancePoints = Param(nameof(JawTeethDistancePoints), 10)
		.SetRange(0, 1000)
		.SetDisplay("Jaw-Teeth Distance", "Minimum gap between jaw and teeth", "Filters");

		_teethLipsDistancePoints = Param(nameof(TeethLipsDistancePoints), 10)
		.SetRange(0, 1000)
		.SetDisplay("Teeth-Lips Distance", "Minimum gap between teeth and lips", "Filters");

		_jawExitMode = Param(nameof(JawExit), JawExitMode.CloseCross)
		.SetDisplay("Jaw Exit", "Exit condition when price crosses the jaw", "Risk Management");

		_reverseExitMode = Param(nameof(ReverseExit), ReverseExitMode.OppositePosition)
		.SetDisplay("Reverse Exit", "Exit behavior on opposite signals", "Risk Management");

		_enableTrailing = Param(nameof(EnableTrailing), true)
		.SetDisplay("Trailing", "Enable Alligator-based trailing stop", "Risk Management");

		_slopeSmaPeriod = Param(nameof(SlopeSmaPeriod), 5)
		.SetRange(1, 100)
		.SetDisplay("Slope SMA", "Period of the slope comparison SMA", "Risk Management");

		_stopLossPoints = Param(nameof(StopLossPoints), 50m)
		.SetNotNegative()
		.SetDisplay("Stop Loss", "Stop-loss distance in points", "Risk Management");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50m)
		.SetNotNegative()
		.SetDisplay("Take Profit", "Take-profit distance in points", "Risk Management");

		_jawPeriod = Param(nameof(JawPeriod), 13)
		.SetGreaterThanZero()
		.SetDisplay("Jaw Period", "Alligator jaw period", "Alligator");

		_jawShift = Param(nameof(JawShift), 8)
		.SetRange(0, 30)
		.SetDisplay("Jaw Shift", "Forward shift of the jaw line", "Alligator");

		_teethPeriod = Param(nameof(TeethPeriod), 8)
		.SetGreaterThanZero()
		.SetDisplay("Teeth Period", "Alligator teeth period", "Alligator");

		_teethShift = Param(nameof(TeethShift), 5)
		.SetRange(0, 30)
		.SetDisplay("Teeth Shift", "Forward shift of the teeth line", "Alligator");

		_lipsPeriod = Param(nameof(LipsPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("Lips Period", "Alligator lips period", "Alligator");

		_lipsShift = Param(nameof(LipsShift), 3)
		.SetRange(0, 30)
		.SetDisplay("Lips Shift", "Forward shift of the lips line", "Alligator");

		_maMethod = Param(nameof(MaMethod), MovingAverageMethod.Simple)
		.SetDisplay("MA Method", "Moving-average type used for Alligator lines", "Alligator");

		_appliedPrice = Param(nameof(AppliedPrice), CandlePrice.Median)
		.SetDisplay("Applied Price", "Price source for the moving averages", "Alligator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Market data type consumed by the strategy", "General");
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

		_jawHistory = Array.Empty<decimal?>();
		_teethHistory = Array.Empty<decimal?>();
		_lipsHistory = Array.Empty<decimal?>();
		_smaHistory = Array.Empty<decimal?>();
		_highBuffer = Array.Empty<decimal>();
		_lowBuffer = Array.Empty<decimal>();
		_fractalCount = 0;
		_pendingBuyLevel = null;
		_pendingSellLevel = null;
		_triggeredBuyLevel = null;
		_triggeredSellLevel = null;
		_newUpFractal = false;
		_newDownFractal = false;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longTakePrice = null;
		_shortTakePrice = null;
		_previousClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_point = Security?.PriceStep ?? 0m;
		if (_point <= 0m)
		_point = 1m;

		_indentDistance = IndentPoints * _point;
		_maxDistance = MaxDistancePoints * _point;

		_jaw = CreateMovingAverage(MaMethod, JawPeriod);
		_teeth = CreateMovingAverage(MaMethod, TeethPeriod);
		_lips = CreateMovingAverage(MaMethod, LipsPeriod);
		_slopeSma = new SimpleMovingAverage { Length = SlopeSmaPeriod };

		_jawHistory = CreateHistoryBuffer(JawShift);
		_teethHistory = CreateHistoryBuffer(TeethShift);
		_lipsHistory = CreateHistoryBuffer(LipsShift);
		_smaHistory = CreateHistoryBuffer(0);

		_highBuffer = new decimal[Math.Max(FractalPeriod, 3)];
		_lowBuffer = new decimal[Math.Max(FractalPeriod, 3)];
		_fractalCount = 0;
		_pendingBuyLevel = null;
		_pendingSellLevel = null;
		_triggeredBuyLevel = null;
		_triggeredSellLevel = null;
		_newUpFractal = false;
		_newDownFractal = false;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longTakePrice = null;
		_shortTakePrice = null;
		_previousClose = null;

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
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
		{
			_longStopPrice = null;
			_longTakePrice = null;
			_shortStopPrice = null;
			_shortTakePrice = null;
			return;
		}

		var entryPrice = PositionPrice;
		var stopDistance = StopLossPoints * _point;
		var takeDistance = TakeProfitPoints * _point;

		if (Position > 0 && delta > 0)
		{
			_longStopPrice = stopDistance > 0m ? entryPrice - stopDistance : null;
			_longTakePrice = takeDistance > 0m ? entryPrice + takeDistance : null;
			_shortStopPrice = null;
			_shortTakePrice = null;
		}
		else if (Position < 0 && delta < 0)
		{
			_shortStopPrice = stopDistance > 0m ? entryPrice + stopDistance : null;
			_shortTakePrice = takeDistance > 0m ? entryPrice - takeDistance : null;
			_longStopPrice = null;
			_longTakePrice = null;
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateFractalBuffers(candle);

		var price = GetAppliedPrice(candle);

		var jawValue = _jaw.Process(price, candle.OpenTime, true);
		var teethValue = _teeth.Process(price, candle.OpenTime, true);
		var lipsValue = _lips.Process(price, candle.OpenTime, true);
		var slopeValue = _slopeSma.Process(candle.ClosePrice, candle.OpenTime, true);

		if (!jawValue.IsFinal || !teethValue.IsFinal || !lipsValue.IsFinal || !slopeValue.IsFinal)
		{
			_previousClose = candle.ClosePrice;
			_newUpFractal = false;
			_newDownFractal = false;
			return;
		}

		var jaw = jawValue.ToDecimal();
		var teeth = teethValue.ToDecimal();
		var lips = lipsValue.ToDecimal();
		var slopeSma = slopeValue.ToDecimal();

		UpdateHistory(_jawHistory, jaw);
		UpdateHistory(_teethHistory, teeth);
		UpdateHistory(_lipsHistory, lips);
		UpdateHistory(_smaHistory, slopeSma);

		ManagePositions(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousClose = candle.ClosePrice;
			_newUpFractal = false;
			_newDownFractal = false;
			return;
		}

		TryEnterLong(candle);
		TryEnterShort(candle);

		_previousClose = candle.ClosePrice;
		_newUpFractal = false;
		_newDownFractal = false;
	}

	private void ManagePositions(ICandleMessage candle)
	{
		var hasJaw = TryGetShiftedValue(_jawHistory, JawShift, 1, out var jawPrev);
		var hasTeeth = TryGetShiftedValue(_teethHistory, TeethShift, 1, out var teethPrev);
		var hasLips = TryGetShiftedValue(_lipsHistory, LipsShift, 1, out var lipsPrev);
		var hasLipsPrevPrev = TryGetShiftedValue(_lipsHistory, LipsShift, 2, out var lipsPrevPrev);
		var hasSlopePrev = TryGetHistoryValue(_smaHistory, 1, out var slopePrev);
		var hasSlopePrevPrev = TryGetHistoryValue(_smaHistory, 2, out var slopePrevPrev);

		if (Position > 0)
		{
			if (_longTakePrice is decimal take && candle.HighPrice >= take)
			{
				CloseLong();
				return;
			}

			if (JawExit != JawExitMode.Disabled && hasJaw)
			{
				var threshold = JawExit == JawExitMode.PriceCross ? candle.LowPrice : candle.ClosePrice;
				if (threshold <= jawPrev)
				{
					CloseLong();
					return;
				}
			}

			if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
			{
				CloseLong();
				return;
			}

			if (EnableTrailing && hasLips && hasLipsPrevPrev && hasSlopePrev && hasSlopePrevPrev)
			{
				var profit = candle.ClosePrice - PositionPrice;
				if (profit > 0m)
				{
					var lipsSlope = lipsPrev - lipsPrevPrev;
					var smaSlope = slopePrev - slopePrevPrev;
					decimal? desiredStop = null;

					if (lipsSlope > smaSlope)
					desiredStop = lipsPrev;
					else if (hasTeeth)
					desiredStop = teethPrev;

					if (desiredStop is decimal target && (_longStopPrice is not decimal current || target > current))
					_longStopPrice = target;
				}
			}

			if (ReverseExit == ReverseExitMode.OppositeFractal && _newDownFractal)
			{
				CloseLong();
				_newDownFractal = false;
				return;
			}
		}
		else if (Position < 0)
		{
			if (_shortTakePrice is decimal take && candle.LowPrice <= take)
			{
				CloseShort();
				return;
			}

			if (JawExit != JawExitMode.Disabled && hasJaw)
			{
				var threshold = JawExit == JawExitMode.PriceCross ? candle.HighPrice : candle.ClosePrice;
				if (threshold >= jawPrev)
				{
					CloseShort();
					return;
				}
			}

			if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
			{
				CloseShort();
				return;
			}

			if (EnableTrailing && hasLips && hasLipsPrevPrev && hasSlopePrev && hasSlopePrevPrev)
			{
				var profit = PositionPrice - candle.ClosePrice;
				if (profit > 0m)
				{
					var lipsSlope = lipsPrevPrev - lipsPrev;
					var smaSlope = slopePrevPrev - slopePrev;
					decimal? desiredStop = null;

					if (lipsSlope > smaSlope)
					desiredStop = lipsPrev;
					else if (hasTeeth)
					desiredStop = teethPrev;

					if (desiredStop is decimal target && (_shortStopPrice is not decimal current || target < current))
					_shortStopPrice = target;
				}
			}

			if (ReverseExit == ReverseExitMode.OppositeFractal && _newUpFractal)
			{
				CloseShort();
				_newUpFractal = false;
				return;
			}
		}
	}

	private void TryEnterLong(ICandleMessage candle)
	{
		if (_pendingBuyLevel is not decimal buyLevel)
		return;

		if (_triggeredBuyLevel is decimal triggered && AreClose(triggered, buyLevel))
		return;

		if (!TryGetShiftedValue(_lipsHistory, LipsShift, 1, out var lipsPrev))
		return;

		if (MaxDistancePoints > 0 && Math.Abs(buyLevel - lipsPrev) > _maxDistance)
		return;

		var breakout = EntryConfirmation switch
		{
			EntryMode.PriceBreakout => candle.HighPrice > buyLevel,
			EntryMode.CloseBreakout => _previousClose is decimal prev && prev > buyLevel,
			_ => false,
		};

		if (!breakout)
		return;

		if (UseTeethFilter)
		{
			if (!TryGetShiftedValue(_teethHistory, TeethShift, 1, out var teethPrev))
			return;

			if (!(_previousClose is decimal prevClose && prevClose > teethPrev))
			return;
		}

		if (UseTrendAlignment)
		{
			if (!TryGetShiftedValue(_teethHistory, TeethShift, 1, out var teethPrev) ||
			!TryGetShiftedValue(_jawHistory, JawShift, 1, out var jawPrev))
			return;

			if (!(lipsPrev - teethPrev > TeethLipsDistancePoints * _point &&
			teethPrev - jawPrev > JawTeethDistancePoints * _point))
			return;
		}

		if (Position < 0 && ReverseExit == ReverseExitMode.OppositePosition)
		{
			CloseShort();
		}

		if (Position > 0)
		return;

		if (OrderVolume <= 0m)
		return;

		BuyMarket(OrderVolume);
		_triggeredBuyLevel = buyLevel;
	}

	private void TryEnterShort(ICandleMessage candle)
	{
		if (_pendingSellLevel is not decimal sellLevel)
		return;

		if (_triggeredSellLevel is decimal triggered && AreClose(triggered, sellLevel))
		return;

		if (!TryGetShiftedValue(_lipsHistory, LipsShift, 1, out var lipsPrev))
		return;

		if (MaxDistancePoints > 0 && Math.Abs(sellLevel - lipsPrev) > _maxDistance)
		return;

		var breakout = EntryConfirmation switch
		{
			EntryMode.PriceBreakout => candle.LowPrice < sellLevel,
			EntryMode.CloseBreakout => _previousClose is decimal prev && prev < sellLevel,
			_ => false,
		};

		if (!breakout)
		return;

		if (UseTeethFilter)
		{
			if (!TryGetShiftedValue(_teethHistory, TeethShift, 1, out var teethPrev))
			return;

			if (!(_previousClose is decimal prevClose && prevClose < teethPrev))
			return;
		}

		if (UseTrendAlignment)
		{
			if (!TryGetShiftedValue(_teethHistory, TeethShift, 1, out var teethPrev) ||
			!TryGetShiftedValue(_jawHistory, JawShift, 1, out var jawPrev))
			return;

			if (!(teethPrev - lipsPrev > TeethLipsDistancePoints * _point &&
			jawPrev - teethPrev > JawTeethDistancePoints * _point))
			return;
		}

		if (Position > 0 && ReverseExit == ReverseExitMode.OppositePosition)
		{
			CloseLong();
		}

		if (Position < 0)
		return;

		if (OrderVolume <= 0m)
		return;

		SellMarket(OrderVolume);
		_triggeredSellLevel = sellLevel;
	}

	private void UpdateFractalBuffers(ICandleMessage candle)
	{
		if (_highBuffer.Length != Math.Max(FractalPeriod, 3))
		{
			_highBuffer = new decimal[Math.Max(FractalPeriod, 3)];
			_lowBuffer = new decimal[Math.Max(FractalPeriod, 3)];
			_fractalCount = 0;
			_pendingBuyLevel = null;
			_pendingSellLevel = null;
			_triggeredBuyLevel = null;
			_triggeredSellLevel = null;
		}

		ShiftBuffer(_highBuffer, candle.HighPrice);
		ShiftBuffer(_lowBuffer, candle.LowPrice);

		if (_fractalCount < _highBuffer.Length)
		{
			_fractalCount++;
			return;
		}

		if (_highBuffer.Length < 3)
		return;

		var wing = (_highBuffer.Length - 1) / 2;
		var centerIndex = _highBuffer.Length - 1 - wing;

		var centerHigh = _highBuffer[centerIndex];
		var isUpFractal = true;
		for (var i = 0; i < _highBuffer.Length; i++)
		{
			if (i == centerIndex)
			continue;

			if (!(centerHigh > _highBuffer[i]))
			{
				isUpFractal = false;
				break;
			}
		}

		if (isUpFractal)
		{
			var level = centerHigh + _indentDistance;
			if (_pendingBuyLevel is not decimal existing || !AreClose(existing, level))
			{
				_pendingBuyLevel = level;
				_triggeredBuyLevel = null;
				_newUpFractal = true;
			}
		}

		var centerLow = _lowBuffer[centerIndex];
		var isDownFractal = true;
		for (var i = 0; i < _lowBuffer.Length; i++)
		{
			if (i == centerIndex)
			continue;

			if (!(centerLow < _lowBuffer[i]))
			{
				isDownFractal = false;
				break;
			}
		}

		if (isDownFractal)
		{
			var level = centerLow - _indentDistance;
			if (_pendingSellLevel is not decimal existing || !AreClose(existing, level))
			{
				_pendingSellLevel = level;
				_triggeredSellLevel = null;
				_newDownFractal = true;
			}
		}
	}

	private void CloseLong()
	{
		if (Position <= 0)
		return;

		SellMarket(Position);
		_longStopPrice = null;
		_longTakePrice = null;
	}

	private void CloseShort()
	{
		if (Position >= 0)
		return;

		BuyMarket(Math.Abs(Position));
		_shortStopPrice = null;
		_shortTakePrice = null;
	}

	private decimal GetAppliedPrice(ICandleMessage candle)
	{
		return AppliedPrice switch
		{
			CandlePrice.Open => candle.OpenPrice,
			CandlePrice.High => candle.HighPrice,
			CandlePrice.Low => candle.LowPrice,
			CandlePrice.Close => candle.ClosePrice,
			CandlePrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			CandlePrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			CandlePrice.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			_ => candle.ClosePrice,
		};
	}

	private static void ShiftBuffer(decimal[] buffer, decimal value)
	{
		if (buffer.Length == 0)
		return;

		Array.Copy(buffer, 1, buffer, 0, buffer.Length - 1);
		buffer[^1] = value;
	}

	private static void UpdateHistory(decimal?[] buffer, decimal value)
	{
		if (buffer.Length == 0)
		return;

		Array.Copy(buffer, 1, buffer, 0, buffer.Length - 1);
		buffer[^1] = value;
	}

	private static decimal?[] CreateHistoryBuffer(int shift)
	{
		var size = Math.Max(shift + 4, 4);
		return new decimal?[size];
	}

	private static bool TryGetShiftedValue(decimal?[] buffer, int shift, int offset, out decimal value)
	{
		var barsAgo = shift + offset;
		return TryGetHistoryValue(buffer, barsAgo, out value);
	}

	private static bool TryGetHistoryValue(decimal?[] buffer, int barsAgo, out decimal value)
	{
		value = 0m;
		if (buffer.Length == 0)
		return false;

		var index = buffer.Length - 1 - barsAgo;
		if (index < 0 || index >= buffer.Length)
		return false;

		if (buffer[index] is not decimal stored)
		return false;

		value = stored;
		return true;
	}

	private bool AreClose(decimal first, decimal second)
	{
		return Math.Abs(first - second) <= _point / 2m;
	}

	private LengthIndicator<decimal> CreateMovingAverage(MovingAverageMethod method, int length)
	{
		LengthIndicator<decimal> indicator = method switch
		{
			MovingAverageMethod.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageMethod.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageMethod.Weighted => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};

		return indicator;
	}

	/// <summary>
	/// Breakout confirmation methods.
	/// </summary>
	public enum EntryMode
	{
		/// <summary>
		/// Confirm using intrabar high/low penetration.
		/// </summary>
		PriceBreakout = 1,

		/// <summary>
		/// Confirm using the previous candle close.
		/// </summary>
		CloseBreakout = 2,
	}

	/// <summary>
	/// Jaw exit options.
	/// </summary>
	public enum JawExitMode
	{
		/// <summary>
		/// Do not close positions when price crosses the jaw.
		/// </summary>
		Disabled = 0,

		/// <summary>
		/// Use intrabar price extremes relative to the jaw.
		/// </summary>
		PriceCross = 1,

		/// <summary>
		/// Use candle close relative to the jaw.
		/// </summary>
		CloseCross = 2,
	}

	/// <summary>
	/// Reverse signal exit options.
	/// </summary>
	public enum ReverseExitMode
	{
		/// <summary>
		/// Ignore opposite signals.
		/// </summary>
		Disabled = 0,

		/// <summary>
		/// Close when a new opposite fractal appears.
		/// </summary>
		OppositeFractal = 1,

		/// <summary>
		/// Close before opening an opposite position.
		/// </summary>
		OppositePosition = 2,
	}

	/// <summary>
	/// Moving-average types supported by the original expert advisor.
	/// </summary>
	public enum MovingAverageMethod
	{
		/// <summary>
		/// Simple moving average.
		/// </summary>
		Simple = 0,

		/// <summary>
		/// Exponential moving average.
		/// </summary>
		Exponential = 1,

		/// <summary>
		/// Smoothed moving average.
		/// </summary>
		Smoothed = 2,

		/// <summary>
		/// Linear weighted moving average.
		/// </summary>
		Weighted = 3,
	}
}

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

using StockSharp.Algo;

/// <summary>
/// Port of the "MA MACD Position averaging v2" MetaTrader expert advisor.
/// Combines a weighted moving average filter with MACD confirmation and position averaging.
/// </summary>
public class MaMacdPositionAveragingV2Strategy : Strategy
{
	private sealed class PositionLeg
	{
		public bool IsLong;
		public decimal Volume;
		public decimal EntryPrice;
		public decimal? StopPrice;
		public decimal? TakePrice;
	}

	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _stepLossPips;
	private readonly StrategyParam<decimal> _lotCoefficient;
	private readonly StrategyParam<int> _barOffset;
	private readonly StrategyParam<bool> _reverseSignals;

	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<MovingAverageMethod> _maMethod;
	private readonly StrategyParam<CandlePrice> _maPrice;
	private readonly StrategyParam<decimal> _maIndentPips;

	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<CandlePrice> _macdPrice;
	private readonly StrategyParam<decimal> _macdRatio;

	private readonly StrategyParam<DataType> _candleType;

	private MovingAverage _movingAverage = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private readonly Queue<decimal> _maBuffer = new();
	private readonly Queue<decimal> _macdMainBuffer = new();
	private readonly Queue<decimal> _macdSignalBuffer = new();

	private readonly List<PositionLeg> _longLegs = new();
	private readonly List<PositionLeg> _shortLegs = new();

	private decimal _pipSize;
	private decimal _stopLossOffset;
	private decimal _takeProfitOffset;
	private decimal _stepLossOffset;
	private decimal _indentOffset;
	private decimal _trailingStopOffset;
	private decimal _trailingStepOffset;

	private int _maOffset;
	private int _macdOffset;

	/// <summary>
	/// Initializes a new instance of the <see cref="MaMacdPositionAveragingV2Strategy"/> class.
	/// </summary>
	public MaMacdPositionAveragingV2Strategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Base volume for the first position", "Trading")
			.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Distance of the protective stop in pips", "Risk Management")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Distance of the profit target in pips", "Risk Management")
			.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk Management");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetNotNegative()
			.SetDisplay("Trailing Step (pips)", "Additional move required before trailing adjusts", "Risk Management");

		_stepLossPips = Param(nameof(StepLossPips), 30m)
			.SetNotNegative()
			.SetDisplay("Averaging Step (pips)", "Minimal adverse move before adding to the position", "Averaging")
			.SetCanOptimize(true);

		_lotCoefficient = Param(nameof(LotCoefficient), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Lot Coefficient", "Multiplier applied to the losing leg volume", "Averaging")
			.SetCanOptimize(true);

		_barOffset = Param(nameof(BarOffset), 0)
			.SetRange(0, 1000)
			.SetDisplay("Bar Offset", "Number of bars to look back when reading indicators", "Signal")
			.SetCanOptimize(true);

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse Signals", "Invert long and short entry directions", "Signal");

		_maPeriod = Param(nameof(MaPeriod), 15)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average length", "Moving Average")
			.SetCanOptimize(true);

		_maShift = Param(nameof(MaShift), 0)
			.SetRange(0, 1000)
			.SetDisplay("MA Shift", "Forward shift applied to the moving average", "Moving Average");

		_maMethod = Param(nameof(MaMethod), MovingAverageMethod.Weighted)
			.SetDisplay("MA Method", "Smoothing method for the moving average", "Moving Average");

		_maPrice = Param(nameof(MaPrice), CandlePrice.Weighted)
			.SetDisplay("MA Price", "Applied price used for the moving average", "Moving Average");

		_maIndentPips = Param(nameof(MaIndentPips), 4m)
			.SetNotNegative()
			.SetDisplay("MA Indent (pips)", "Minimal distance between price and MA before entering", "Moving Average");

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA period for MACD", "MACD")
			.SetCanOptimize(true);

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA period for MACD", "MACD")
			.SetCanOptimize(true);

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal EMA period for MACD", "MACD")
			.SetCanOptimize(true);

		_macdPrice = Param(nameof(MacdPrice), CandlePrice.Weighted)
			.SetDisplay("MACD Price", "Applied price fed into MACD", "MACD");

		_macdRatio = Param(nameof(MacdRatio), 0.9m)
			.SetGreaterThanZero()
			.SetDisplay("MACD Ratio", "Required ratio between MACD main and signal", "MACD")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle type for calculations", "General");
	}

	/// <summary>
	/// Base volume for the first trade in a series.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Additional distance that price must cover before the trailing stop is advanced.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Minimal loss in pips required before a new averaging order is added.
	/// </summary>
	public decimal StepLossPips
	{
		get => _stepLossPips.Value;
		set => _stepLossPips.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the losing leg volume when averaging.
	/// </summary>
	public decimal LotCoefficient
	{
		get => _lotCoefficient.Value;
		set => _lotCoefficient.Value = value;
	}

	/// <summary>
	/// Number of bars to look back when reading indicator values.
	/// </summary>
	public int BarOffset
	{
		get => _barOffset.Value;
		set => _barOffset.Value = value;
	}

	/// <summary>
	/// Inverts long and short entry directions when true.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the moving average.
	/// </summary>
	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Moving average smoothing method.
	/// </summary>
	public MovingAverageMethod MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	/// <summary>
	/// Price source used by the moving average.
	/// </summary>
	public CandlePrice MaPrice
	{
		get => _maPrice.Value;
		set => _maPrice.Value = value;
	}

	/// <summary>
	/// Minimal distance between price and the moving average before entering.
	/// </summary>
	public decimal MaIndentPips
	{
		get => _maIndentPips.Value;
		set => _maIndentPips.Value = value;
	}

	/// <summary>
	/// Fast EMA period for MACD.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period for MACD.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal EMA period for MACD.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Applied price fed into the MACD calculation.
	/// </summary>
	public CandlePrice MacdPrice
	{
		get => _macdPrice.Value;
		set => _macdPrice.Value = value;
	}

	/// <summary>
	/// Required ratio between MACD main and signal lines.
	/// </summary>
	public decimal MacdRatio
	{
		get => _macdRatio.Value;
		set => _macdRatio.Value = value;
	}

	/// <summary>
	/// Candle type used for all indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_maBuffer.Clear();
		_macdMainBuffer.Clear();
		_macdSignalBuffer.Clear();
		_longLegs.Clear();
		_shortLegs.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		_stopLossOffset = StopLossPips > 0m ? StopLossPips * _pipSize : 0m;
		_takeProfitOffset = TakeProfitPips > 0m ? TakeProfitPips * _pipSize : 0m;
		_stepLossOffset = StepLossPips > 0m ? StepLossPips * _pipSize : 0m;
		_indentOffset = MaIndentPips > 0m ? MaIndentPips * _pipSize : 0m;
		_trailingStopOffset = TrailingStopPips > 0m ? TrailingStopPips * _pipSize : 0m;
		_trailingStepOffset = TrailingStepPips > 0m ? TrailingStepPips * _pipSize : 0m;

		_maOffset = Math.Max(0, MaShift + BarOffset);
		_macdOffset = Math.Max(0, BarOffset);

		_maBuffer.Clear();
		_macdMainBuffer.Clear();
		_macdSignalBuffer.Clear();

		_movingAverage = CreateMovingAverage(MaMethod, MaPeriod, MaPrice);
		_macd = CreateMacd();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, _movingAverage, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _movingAverage);
			DrawOwnTrades(area);

			var macdArea = CreateChartArea();
			if (macdArea != null)
				DrawIndicator(macdArea, _macd);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateTrailing(candle.ClosePrice);
		ManageLegs(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!macdValue.IsFinal || !maValue.IsFinal)
			return;

		var maCurrent = maValue.ToDecimal();
		var macd = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (macd.Macd is not decimal macdMainRaw ||
			macd.Signal is not decimal macdSignalRaw)
		{
			return;
		}

		var maShifted = UpdateBuffer(_maBuffer, maCurrent, _maOffset);
		var macdMain = UpdateBuffer(_macdMainBuffer, macdMainRaw, _macdOffset);
		var macdSignal = UpdateBuffer(_macdSignalBuffer, macdSignalRaw, _macdOffset);

		if (maShifted is null || macdMain is null || macdSignal is null)
			return;

		var price = candle.ClosePrice;

		if (_longLegs.Count > 0 || _shortLegs.Count > 0)
		{
			if (_longLegs.Count > 0 && _shortLegs.Count > 0)
			{
				CloseAllPositions();
				return;
			}

			if (_longLegs.Count > 0)
			{
				TryAddLongAveraging(price);
			}
			else
			{
				TryAddShortAveraging(price);
			}

			return;
		}

		var ratioOk = macdSignal.Value != 0m && macdMain.Value / macdSignal.Value >= MacdRatio;
		if (!ratioOk)
			return;

		var priceAboveMa = price > maShifted.Value;
		var priceBelowMa = price < maShifted.Value;

		var distanceAbove = price - maShifted.Value;
		var distanceBelow = maShifted.Value - price;

		var canBuy = macdMain.Value < 0m && macdSignal.Value < 0m && priceAboveMa && distanceAbove >= _indentOffset;
		var canSell = macdMain.Value > 0m && macdSignal.Value > 0m && priceBelowMa && distanceBelow >= _indentOffset;

		if (ReverseSignals)
		{
			if (canBuy)
				TryOpenShort(OrderVolume, price);
			else if (canSell)
				TryOpenLong(OrderVolume, price);
		}
		else
		{
			if (canBuy)
				TryOpenLong(OrderVolume, price);
			else if (canSell)
				TryOpenShort(OrderVolume, price);
		}
	}

	private void TryAddLongAveraging(decimal price)
	{
		if (_stepLossOffset <= 0m)
			return;

		PositionLeg? candidate = null;

		foreach (var leg in _longLegs)
		{
			var loss = leg.EntryPrice - price;
			if (loss < _stepLossOffset)
				continue;

			if (candidate is null || leg.EntryPrice < candidate.EntryPrice)
				candidate = leg;
		}

		if (candidate is null)
			return;

		var volume = candidate.Volume * LotCoefficient;
		TryOpenLong(volume, price);
	}

	private void TryAddShortAveraging(decimal price)
	{
		if (_stepLossOffset <= 0m)
			return;

		PositionLeg? candidate = null;

		foreach (var leg in _shortLegs)
		{
			var loss = price - leg.EntryPrice;
			if (loss < _stepLossOffset)
				continue;

			if (candidate is null || leg.EntryPrice > candidate.EntryPrice)
				candidate = leg;
		}

		if (candidate is null)
			return;

		var volume = candidate.Volume * LotCoefficient;
		TryOpenShort(volume, price);
	}

	private void UpdateTrailing(decimal currentPrice)
	{
		if (_trailingStopOffset <= 0m || _trailingStepOffset <= 0m)
			return;

		foreach (var leg in _longLegs)
		{
			var gain = currentPrice - leg.EntryPrice;
			if (gain <= _trailingStopOffset + _trailingStepOffset)
				continue;

			var trigger = currentPrice - (_trailingStopOffset + _trailingStepOffset);
			if (leg.StopPrice.HasValue && leg.StopPrice.Value >= trigger)
				continue;

			leg.StopPrice = NormalizePrice(currentPrice - _trailingStopOffset);
		}

		foreach (var leg in _shortLegs)
		{
			var gain = leg.EntryPrice - currentPrice;
			if (gain <= _trailingStopOffset + _trailingStepOffset)
				continue;

			var trigger = currentPrice + (_trailingStopOffset + _trailingStepOffset);
			if (leg.StopPrice.HasValue && leg.StopPrice.Value <= trigger && leg.StopPrice.Value != 0m)
				continue;

			leg.StopPrice = NormalizePrice(currentPrice + _trailingStopOffset);
		}
	}

	private void ManageLegs(ICandleMessage candle)
	{
		for (var i = 0; i < _longLegs.Count;)
		{
			var leg = _longLegs[i];

			if (leg.StopPrice.HasValue && candle.LowPrice <= leg.StopPrice.Value)
			{
				if (SellMarket(leg.Volume) != null)
				{
					_longLegs.RemoveAt(i);
					continue;
				}
			}
			else if (leg.TakePrice.HasValue && candle.HighPrice >= leg.TakePrice.Value)
			{
				if (SellMarket(leg.Volume) != null)
				{
					_longLegs.RemoveAt(i);
					continue;
				}
			}

			i++;
		}

		for (var i = 0; i < _shortLegs.Count;)
		{
			var leg = _shortLegs[i];

			if (leg.StopPrice.HasValue && candle.HighPrice >= leg.StopPrice.Value)
			{
				if (BuyMarket(leg.Volume) != null)
				{
					_shortLegs.RemoveAt(i);
					continue;
				}
			}
			else if (leg.TakePrice.HasValue && candle.LowPrice <= leg.TakePrice.Value)
			{
				if (BuyMarket(leg.Volume) != null)
				{
					_shortLegs.RemoveAt(i);
					continue;
				}
			}

			i++;
		}
	}

	private bool TryOpenLong(decimal volume, decimal price)
	{
		var adjusted = AdjustVolume(volume);
		if (adjusted <= 0m)
			return false;

		if (BuyMarket(adjusted) is null)
			return false;

		var leg = new PositionLeg
		{
			IsLong = true,
			Volume = adjusted,
			EntryPrice = price,
			StopPrice = _stopLossOffset > 0m ? NormalizePrice(price - _stopLossOffset) : null,
			TakePrice = _takeProfitOffset > 0m ? NormalizePrice(price + _takeProfitOffset) : null
		};

		_longLegs.Add(leg);
		return true;
	}

	private bool TryOpenShort(decimal volume, decimal price)
	{
		var adjusted = AdjustVolume(volume);
		if (adjusted <= 0m)
			return false;

		if (SellMarket(adjusted) is null)
			return false;

		var leg = new PositionLeg
		{
			IsLong = false,
			Volume = adjusted,
			EntryPrice = price,
			StopPrice = _stopLossOffset > 0m ? NormalizePrice(price + _stopLossOffset) : null,
			TakePrice = _takeProfitOffset > 0m ? NormalizePrice(price - _takeProfitOffset) : null
		};

		_shortLegs.Add(leg);
		return true;
	}

	private void CloseAllPositions()
	{
		var net = Position;

		if (net > 0m)
			SellMarket(net);
		else if (net < 0m)
			BuyMarket(Math.Abs(net));

		_longLegs.Clear();
		_shortLegs.Clear();
	}

	private MovingAverage CreateMovingAverage(MovingAverageMethod method, int period, CandlePrice price)
	{
		var length = Math.Max(1, period);

		MovingAverage indicator = method switch
		{
			MovingAverageMethod.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageMethod.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageMethod.Weighted => new WeightedMovingAverage { Length = length },
			_ => new WeightedMovingAverage { Length = length }
		};

		indicator.CandlePrice = price;
		return indicator;
	}

	private MovingAverageConvergenceDivergenceSignal CreateMacd()
	{
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = Math.Max(1, MacdFastPeriod), CandlePrice = MacdPrice },
				LongMa = { Length = Math.Max(1, MacdSlowPeriod), CandlePrice = MacdPrice }
			},
			SignalMa = { Length = Math.Max(1, MacdSignalPeriod) }
		};

		return macd;
	}

	private decimal? UpdateBuffer(Queue<decimal> buffer, decimal value, int offset)
	{
		buffer.Enqueue(value);

		var required = offset + 1;
		while (buffer.Count > required)
			buffer.Dequeue();

		if (buffer.Count < required)
			return null;

		return buffer.Peek();
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (Security is null)
			return volume;

		var step = Security.VolumeStep ?? 0m;
		if (step > 0m)
			volume = step * Math.Floor(volume / step);

		var minVolume = Security.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
			return 0m;

		var maxVolume = Security.MaxVolume;
		if (maxVolume != null && volume > maxVolume.Value)
			volume = maxVolume.Value;

		return volume;
	}

	private decimal NormalizePrice(decimal price)
	{
		var step = Security?.PriceStep;
		if (step is null or 0m)
			return price;

		return Math.Round(price / step.Value, MidpointRounding.AwayFromZero) * step.Value;
	}

	private decimal CalculatePipSize()
	{
		var priceStep = Security?.PriceStep ?? 1m;
		if (priceStep <= 0m)
			return 1m;

		var step = priceStep;
		var decimals = 0;
		while (step < 1m && decimals < 10)
		{
			step *= 10m;
			decimals++;
		}

		return decimals is 3 or 5 ? priceStep * 10m : priceStep;
	}
}

/// <summary>
/// Moving average smoothing modes mirroring the MetaTrader enumeration.
/// </summary>
public enum MovingAverageMethod
{
	/// <summary>
	/// Simple moving average.
	/// </summary>
	Simple,

	/// <summary>
	/// Exponential moving average.
	/// </summary>
	Exponential,

	/// <summary>
	/// Smoothed moving average.
	/// </summary>
	Smoothed,

	/// <summary>
	/// Linear weighted moving average.
	/// </summary>
	Weighted
}


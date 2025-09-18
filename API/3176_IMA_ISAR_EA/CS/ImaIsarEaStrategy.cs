namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Trend-following strategy that combines weighted moving averages with dual Parabolic SAR confirmation.
/// </summary>
public class ImaIsarEaStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<bool> _useTrailing;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _fastMaShift;
	private readonly StrategyParam<int> _normalMaPeriod;
	private readonly StrategyParam<int> _normalMaShift;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _slowMaShift;
	private readonly StrategyParam<decimal> _fastSarStep;
	private readonly StrategyParam<decimal> _fastSarMax;
	private readonly StrategyParam<decimal> _normalSarStep;
	private readonly StrategyParam<decimal> _normalSarMax;

	private WeightedMovingAverage? _fastMa;
	private WeightedMovingAverage? _normalMa;
	private WeightedMovingAverage? _slowMa;
	private ParabolicSar? _fastSar;
	private ParabolicSar? _normalSar;
	private Shift? _fastShiftIndicator;
	private Shift? _normalShiftIndicator;
	private Shift? _slowShiftIndicator;

	private decimal _pipSize;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortTakePrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="ImaIsarEaStrategy"/> class.
	/// </summary>
	public ImaIsarEaStrategy()
	{
		_volume = Param(nameof(Volume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Base order volume", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
		.SetDisplay("Stop Loss (pips)", "Protective stop distance", "Risk")
		.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
		.SetDisplay("Take Profit (pips)", "Target distance", "Risk")
		.SetCanOptimize(true);

		_useTrailing = Param(nameof(UseTrailing), true)
		.SetDisplay("Use Trailing", "Enable trailing stop", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 25m)
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk")
		.SetCanOptimize(true);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
		.SetDisplay("Trailing Step (pips)", "Minimum move before trailing update", "Risk")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle series", "General");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 10)
		.SetGreaterThanZero()
		.SetDisplay("Fast MA Period", "Length of the fast weighted MA", "Indicators");

		_fastMaShift = Param(nameof(FastMaShift), 0)
		.SetDisplay("Fast MA Shift", "Completed bars to shift the fast MA", "Indicators");

		_normalMaPeriod = Param(nameof(NormalMaPeriod), 30)
		.SetGreaterThanZero()
		.SetDisplay("Normal MA Period", "Length of the middle weighted MA", "Indicators");

		_normalMaShift = Param(nameof(NormalMaShift), 3)
		.SetDisplay("Normal MA Shift", "Completed bars to shift the middle MA", "Indicators");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 60)
		.SetGreaterThanZero()
		.SetDisplay("Slow MA Period", "Length of the slow weighted MA", "Indicators");

		_slowMaShift = Param(nameof(SlowMaShift), 6)
		.SetDisplay("Slow MA Shift", "Completed bars to shift the slow MA", "Indicators");

		_fastSarStep = Param(nameof(FastSarStep), 0.02m)
		.SetGreaterThanZero()
		.SetDisplay("Fast SAR Step", "Acceleration factor for the fast SAR", "Indicators");

		_fastSarMax = Param(nameof(FastSarMax), 0.2m)
		.SetGreaterThanZero()
		.SetDisplay("Fast SAR Max", "Maximum acceleration for the fast SAR", "Indicators");

		_normalSarStep = Param(nameof(NormalSarStep), 0.02m)
		.SetGreaterThanZero()
		.SetDisplay("Normal SAR Step", "Acceleration factor for the normal SAR", "Indicators");

		_normalSarMax = Param(nameof(NormalSarMax), 0.2m)
		.SetGreaterThanZero()
		.SetDisplay("Normal SAR Max", "Maximum acceleration for the normal SAR", "Indicators");
	}

	/// <summary>
	/// Base order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
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
	/// Enable trailing stop management.
	/// </summary>
	public bool UseTrailing
	{
		get => _useTrailing.Value;
		set => _useTrailing.Value = value;
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
	/// Minimal price movement (in pips) before the trailing stop advances.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Candle series used for all indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast weighted moving average length.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Completed bars to shift the fast moving average.
	/// </summary>
	public int FastMaShift
	{
		get => _fastMaShift.Value;
		set => _fastMaShift.Value = value;
	}

	/// <summary>
	/// Normal weighted moving average length.
	/// </summary>
	public int NormalMaPeriod
	{
		get => _normalMaPeriod.Value;
		set => _normalMaPeriod.Value = value;
	}

	/// <summary>
	/// Completed bars to shift the normal moving average.
	/// </summary>
	public int NormalMaShift
	{
		get => _normalMaShift.Value;
		set => _normalMaShift.Value = value;
	}

	/// <summary>
	/// Slow weighted moving average length.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Completed bars to shift the slow moving average.
	/// </summary>
	public int SlowMaShift
	{
		get => _slowMaShift.Value;
		set => _slowMaShift.Value = value;
	}

	/// <summary>
	/// Acceleration step for the fast Parabolic SAR.
	/// </summary>
	public decimal FastSarStep
	{
		get => _fastSarStep.Value;
		set => _fastSarStep.Value = value;
	}

	/// <summary>
	/// Maximum acceleration for the fast Parabolic SAR.
	/// </summary>
	public decimal FastSarMax
	{
		get => _fastSarMax.Value;
		set => _fastSarMax.Value = value;
	}

	/// <summary>
	/// Acceleration step for the normal Parabolic SAR.
	/// </summary>
	public decimal NormalSarStep
	{
		get => _normalSarStep.Value;
		set => _normalSarStep.Value = value;
	}

	/// <summary>
	/// Maximum acceleration for the normal Parabolic SAR.
	/// </summary>
	public decimal NormalSarMax
	{
		get => _normalSarMax.Value;
		set => _normalSarMax.Value = value;
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

		_fastMa = null;
		_normalMa = null;
		_slowMa = null;
		_fastSar = null;
		_normalSar = null;
		_fastShiftIndicator = null;
		_normalShiftIndicator = null;
		_slowShiftIndicator = null;

		ResetProtection();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new() { Length = FastMaPeriod };
		_normalMa = new() { Length = NormalMaPeriod };
		_slowMa = new() { Length = SlowMaPeriod };

		_fastSar = new ParabolicSar
		{
			AccelerationStep = FastSarStep,
			AccelerationMax = FastSarMax
		};

		_normalSar = new ParabolicSar
		{
			AccelerationStep = NormalSarStep,
			AccelerationMax = NormalSarMax
		};

		_fastShiftIndicator = FastMaShift > 0 ? new Shift { Length = FastMaShift } : null;
		_normalShiftIndicator = NormalMaShift > 0 ? new Shift { Length = NormalMaShift } : null;
		_slowShiftIndicator = SlowMaShift > 0 ? new Shift { Length = SlowMaShift } : null;

		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_fastMa, _normalMa, _slowMa, _fastSar, _normalSar, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _normalMa);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _fastSar);
			DrawIndicator(area, _normalSar);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastMaValue, decimal normalMaValue, decimal slowMaValue, decimal fastSarValue, decimal normalSarValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_fastMa is null || _normalMa is null || _slowMa is null || _fastSar is null || _normalSar is null)
		return;

		var fastValue = ApplyShift(_fastShiftIndicator, fastMaValue, candle);
		var normalValue = ApplyShift(_normalShiftIndicator, normalMaValue, candle);
		var slowValue = ApplyShift(_slowShiftIndicator, slowMaValue, candle);

		if (fastValue is null || normalValue is null || slowValue is null)
		return;

		if (!_fastMa.IsFormed || !_normalMa.IsFormed || !_slowMa.IsFormed || !_fastSar.IsFormed || !_normalSar.IsFormed)
		return;

		if ((_fastShiftIndicator != null && !_fastShiftIndicator.IsFormed) ||
		(_normalShiftIndicator != null && !_normalShiftIndicator.IsFormed) ||
		(_slowShiftIndicator != null && !_slowShiftIndicator.IsFormed))
		{
			return;
		}

		ManageActivePosition(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var close = candle.ClosePrice;
		var shouldBuy = fastValue > normalValue && fastValue > slowValue && fastSarValue < close && normalSarValue < close;
		var shouldSell = fastValue < normalValue && fastValue < slowValue && fastSarValue > close && normalSarValue > close;

		if (shouldBuy && Position <= 0m)
		{
			var volume = PrepareOrderVolume(true);
			if (volume > 0m)
			{
				BuyMarket(volume);
				InitializeLongProtection(close);
			}
		}
		else if (shouldSell && Position >= 0m)
		{
			var volume = PrepareOrderVolume(false);
			if (volume > 0m)
			{
				SellMarket(volume);
				InitializeShortProtection(close);
			}
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			ResetProtection();
		}
		else if (Position > 0m)
		{
			_shortEntryPrice = null;
			_shortTrailingStop = null;
			_shortStopPrice = null;
			_shortTakePrice = null;
		}
		else
		{
			_longEntryPrice = null;
			_longTrailingStop = null;
			_longStopPrice = null;
			_longTakePrice = null;
		}
	}

	private void ManageActivePosition(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			HandleLongPosition(candle);
		}
		else if (Position < 0m)
		{
			HandleShortPosition(candle);
		}
	}

	private void HandleLongPosition(ICandleMessage candle)
	{
		if (_longEntryPrice is not decimal entry)
		return;

		var stopDistance = StopLossPips > 0m ? StopLossPips * _pipSize : 0m;
		var takeDistance = TakeProfitPips > 0m ? TakeProfitPips * _pipSize : 0m;
		var trailingDistance = UseTrailing && TrailingStopPips > 0m ? TrailingStopPips * _pipSize : 0m;
		var trailingStep = UseTrailing && TrailingStepPips > 0m ? TrailingStepPips * _pipSize : 0m;

		var stopPrice = stopDistance > 0m ? entry - stopDistance : (decimal?)null;

		if (stopPrice.HasValue && candle.LowPrice <= stopPrice.Value)
		{
			SellMarket(Position);
			ResetLongProtection();
			return;
		}

		if (takeDistance > 0m)
		{
			var target = entry + takeDistance;
			if (candle.HighPrice >= target)
			{
				SellMarket(Position);
				ResetLongProtection();
				return;
			}
			_longTakePrice = target;
		}
		else
		{
			_longTakePrice = null;
		}

		if (trailingDistance > 0m)
		{
			var desired = candle.ClosePrice - trailingDistance;
			if (_longTrailingStop is null)
			{
				_longTrailingStop = desired;
			}
			else if (desired > _longTrailingStop.Value + trailingStep)
			{
				_longTrailingStop = desired;
			}

			if (_longTrailingStop.HasValue && candle.LowPrice <= _longTrailingStop.Value)
			{
				SellMarket(Position);
				ResetLongProtection();
				return;
			}

			if (stopPrice.HasValue)
			{
				stopPrice = Math.Max(stopPrice.Value, _longTrailingStop ?? stopPrice.Value);
			}
			else
			{
				stopPrice = _longTrailingStop;
			}
		}
		else
		{
			_longTrailingStop = null;
		}

		_longStopPrice = stopPrice;
	}

	private void HandleShortPosition(ICandleMessage candle)
	{
		if (_shortEntryPrice is not decimal entry)
		return;

		var stopDistance = StopLossPips > 0m ? StopLossPips * _pipSize : 0m;
		var takeDistance = TakeProfitPips > 0m ? TakeProfitPips * _pipSize : 0m;
		var trailingDistance = UseTrailing && TrailingStopPips > 0m ? TrailingStopPips * _pipSize : 0m;
		var trailingStep = UseTrailing && TrailingStepPips > 0m ? TrailingStepPips * _pipSize : 0m;

		var stopPrice = stopDistance > 0m ? entry + stopDistance : (decimal?)null;

		if (stopPrice.HasValue && candle.HighPrice >= stopPrice.Value)
		{
			BuyMarket(Math.Abs(Position));
			ResetShortProtection();
			return;
		}

		if (takeDistance > 0m)
		{
			var target = entry - takeDistance;
			if (candle.LowPrice <= target)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortProtection();
				return;
			}
			_shortTakePrice = target;
		}
		else
		{
			_shortTakePrice = null;
		}

		if (trailingDistance > 0m)
		{
			var desired = candle.ClosePrice + trailingDistance;
			if (_shortTrailingStop is null)
			{
				_shortTrailingStop = desired;
			}
			else if (desired < _shortTrailingStop.Value - trailingStep)
			{
				_shortTrailingStop = desired;
			}

			if (_shortTrailingStop.HasValue && candle.HighPrice >= _shortTrailingStop.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortProtection();
				return;
			}

			if (stopPrice.HasValue)
			{
				stopPrice = Math.Min(stopPrice.Value, _shortTrailingStop ?? stopPrice.Value);
			}
			else
			{
				stopPrice = _shortTrailingStop;
			}
		}
		else
		{
			_shortTrailingStop = null;
		}

		_shortStopPrice = stopPrice;
	}

	private void InitializeLongProtection(decimal entryPrice)
	{
		_longEntryPrice = entryPrice;
		_shortEntryPrice = null;
		_shortTrailingStop = null;
		_shortStopPrice = null;
		_shortTakePrice = null;

		var stopDistance = StopLossPips > 0m ? StopLossPips * _pipSize : 0m;
		_longStopPrice = stopDistance > 0m ? entryPrice - stopDistance : (decimal?)null;

		var takeDistance = TakeProfitPips > 0m ? TakeProfitPips * _pipSize : 0m;
		_longTakePrice = takeDistance > 0m ? entryPrice + takeDistance : (decimal?)null;

		var trailingDistance = UseTrailing && TrailingStopPips > 0m ? TrailingStopPips * _pipSize : 0m;
		_longTrailingStop = trailingDistance > 0m ? entryPrice - trailingDistance : null;
	}

	private void InitializeShortProtection(decimal entryPrice)
	{
		_shortEntryPrice = entryPrice;
		_longEntryPrice = null;
		_longTrailingStop = null;
		_longStopPrice = null;
		_longTakePrice = null;

		var stopDistance = StopLossPips > 0m ? StopLossPips * _pipSize : 0m;
		_shortStopPrice = stopDistance > 0m ? entryPrice + stopDistance : (decimal?)null;

		var takeDistance = TakeProfitPips > 0m ? TakeProfitPips * _pipSize : 0m;
		_shortTakePrice = takeDistance > 0m ? entryPrice - takeDistance : (decimal?)null;

		var trailingDistance = UseTrailing && TrailingStopPips > 0m ? TrailingStopPips * _pipSize : 0m;
		_shortTrailingStop = trailingDistance > 0m ? entryPrice + trailingDistance : null;
	}

	private void ResetProtection()
	{
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longTrailingStop = null;
		_shortTrailingStop = null;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longTakePrice = null;
		_shortTakePrice = null;
	}

	private void ResetLongProtection()
	{
		_longEntryPrice = null;
		_longTrailingStop = null;
		_longStopPrice = null;
		_longTakePrice = null;
	}

	private void ResetShortProtection()
	{
		_shortEntryPrice = null;
		_shortTrailingStop = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
	}

	private decimal? ApplyShift(Shift? shift, decimal value, ICandleMessage candle)
	{
		if (shift is null)
		return value;

		var shifted = shift.Process(value, candle.OpenTime, true);
		return shift.IsFormed ? shifted.ToDecimal() : null;
	}

	private decimal PrepareOrderVolume(bool isLong)
	{
		var volume = Volume;

		if (isLong && Position < 0m)
		{
			volume += Math.Abs(Position);
		}
		else if (!isLong && Position > 0m)
		{
			volume += Math.Abs(Position);
		}

		var adjusted = AdjustVolume(volume);
		return adjusted > 0m ? adjusted : volume;
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (Security is null)
		return volume;

		var step = Security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var rounded = step * Math.Floor(volume / step);
			if (rounded > 0m)
			{
				volume = rounded;
			}
			else
			{
				volume = step;
			}
		}

		var minVolume = Security.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
		volume = minVolume;

		var maxVolume = Security.MaxVolume;
		if (maxVolume != null && maxVolume.Value > 0m && volume > maxVolume.Value)
		volume = maxVolume.Value;

		return volume;
	}

	private decimal CalculatePipSize()
	{
		if (Security?.PriceStep is decimal step && step > 0m)
		return step;

		return 0.0001m;
	}
}

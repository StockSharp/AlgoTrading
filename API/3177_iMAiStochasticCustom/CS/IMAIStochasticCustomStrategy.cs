namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class IMAIStochasticCustomStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<VolumeMode> _volumeMode;
	private readonly StrategyParam<decimal> _volumeValue;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<MaMethod> _maMethod;
	private readonly StrategyParam<int> _levelUpPips;
	private readonly StrategyParam<int> _levelDownPips;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<int> _stochasticSlowing;
	private readonly StrategyParam<decimal> _stochasticLevel1;
	private readonly StrategyParam<decimal> _stochasticLevel2;
	private readonly StrategyParam<bool> _reverseSignals;

	private LengthIndicator<decimal> _movingAverage = null!;
	private StochasticOscillator _stochastic = null!;
	private readonly List<decimal> _maHistory = new();
	private decimal _pipSize;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakeProfitPrice;
	private decimal? _shortTakeProfitPrice;

	public IMAIStochasticCustomStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used to evaluate signals.", "General");

		_stopLossPips = Param(nameof(StopLossPips), 50)
		.SetGreaterOrEqualZero()
		.SetDisplay("Stop Loss (pips)", "Protective stop distance expressed in pips.", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10, 150, 10);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
		.SetGreaterOrEqualZero()
		.SetDisplay("Take Profit (pips)", "Take-profit distance expressed in pips.", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10, 200, 10);

		_trailingStopPips = Param(nameof(TrailingStopPips), 25)
		.SetGreaterOrEqualZero()
		.SetDisplay("Trailing Stop (pips)", "Distance that activates the trailing stop.", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0, 150, 5);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
		.SetGreaterOrEqualZero()
		.SetDisplay("Trailing Step (pips)", "Minimum price improvement before moving the trailing stop.", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0, 50, 5);

		_volumeMode = Param(nameof(ManagementMode), VolumeMode.RiskPercent)
		.SetDisplay("Money Management", "Volume sizing rule applied to new orders.", "Risk");

		_volumeValue = Param(nameof(VolumeValue), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Lot / Risk", "Fixed lot when ManagementMode is FixedLot or percent risk when ManagementMode is RiskPercent.", "Risk");

		_maPeriod = Param(nameof(MaPeriod), 13)
		.SetGreaterThanZero()
		.SetDisplay("MA Period", "Length of the moving average envelope.", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 60, 1);

		_maShift = Param(nameof(MaShift), 0)
		.SetGreaterOrEqualZero()
		.SetDisplay("MA Shift", "Number of completed bars used to shift the moving average.", "Indicators");

		_maMethod = Param(nameof(MaMethod), MaMethod.Smoothed)
		.SetDisplay("MA Method", "Calculation method for the moving average envelope.", "Indicators");

		_levelUpPips = Param(nameof(LevelUpPips), 80)
		.SetDisplay("Upper Level (pips)", "Offset added to the moving average to build the resistance band.", "Indicators");

		_levelDownPips = Param(nameof(LevelDownPips), -80)
		.SetDisplay("Lower Level (pips)", "Offset added to the moving average to build the support band.", "Indicators");

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic %K", "Lookback period for the stochastic oscillator.", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(3, 20, 1);

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 3)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic %D", "Signal smoothing period for the stochastic oscillator.", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(1, 10, 1);

		_stochasticSlowing = Param(nameof(StochasticSlowing), 3)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic Slowing", "Smoothing applied to the %K line.", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(1, 10, 1);

		_stochasticLevel1 = Param(nameof(StochasticLevel1), 25m)
		.SetDisplay("Level #1", "Threshold used for bullish confirmations.", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10m, 40m, 5m);

		_stochasticLevel2 = Param(nameof(StochasticLevel2), 75m)
		.SetDisplay("Level #2", "Threshold used for bearish confirmations.", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(60m, 90m, 5m);

		_reverseSignals = Param(nameof(ReverseSignals), false)
		.SetDisplay("Reverse", "Invert long and short signals.", "General");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	public VolumeMode ManagementMode
	{
		get => _volumeMode.Value;
		set => _volumeMode.Value = value;
	}

	public decimal VolumeValue
	{
		get => _volumeValue.Value;
		set => _volumeValue.Value = value;
	}

	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	public MaMethod MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	public int LevelUpPips
	{
		get => _levelUpPips.Value;
		set => _levelUpPips.Value = value;
	}

	public int LevelDownPips
	{
		get => _levelDownPips.Value;
		set => _levelDownPips.Value = value;
	}

	public int StochasticKPeriod
	{
		get => _stochasticKPeriod.Value;
		set => _stochasticKPeriod.Value = value;
	}

	public int StochasticDPeriod
	{
		get => _stochasticDPeriod.Value;
		set => _stochasticDPeriod.Value = value;
	}

	public int StochasticSlowing
	{
		get => _stochasticSlowing.Value;
		set => _stochasticSlowing.Value = value;
	}

	public decimal StochasticLevel1
	{
		get => _stochasticLevel1.Value;
		set => _stochasticLevel1.Value = value;
	}

	public decimal StochasticLevel2
	{
		get => _stochasticLevel2.Value;
		set => _stochasticLevel2.Value = value;
	}

	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
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

		ResetState();
		_maHistory.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = GetPipSize();

		_movingAverage = CreateMovingAverage(MaMethod, Math.Max(1, MaPeriod));
		_stochastic = new StochasticOscillator
		{
			Length = Math.Max(1, StochasticKPeriod),
			KPeriod = Math.Max(1, StochasticKPeriod),
			DPeriod = Math.Max(1, StochasticDPeriod),
			Smooth = Math.Max(1, StochasticSlowing)
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_movingAverage, _stochastic, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _movingAverage);
			DrawIndicator(area, _stochastic);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position > 0m)
		{
			ResetShortState();

			if (PositionPrice is decimal entry)
			{
				_longEntryPrice = entry;
				_longStopPrice ??= StopLossPips > 0 ? entry - StopLossPips * _pipSize : null;
				_longTakeProfitPrice ??= TakeProfitPips > 0 ? entry + TakeProfitPips * _pipSize : null;
			}
		}
		else if (Position < 0m)
		{
			ResetLongState();

			if (PositionPrice is decimal entry)
			{
				_shortEntryPrice = entry;
				_shortStopPrice ??= StopLossPips > 0 ? entry + StopLossPips * _pipSize : null;
				_shortTakeProfitPrice ??= TakeProfitPips > 0 ? entry - TakeProfitPips * _pipSize : null;
			}
		}
		else
		{
			ResetState();
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue maValue, IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!maValue.IsFinal || !stochasticValue.IsFinal)
		return;

		var maRaw = maValue.ToDecimal();
		if (!TryGetShiftedMa(maRaw, out var ma))
		return;

		if (_pipSize <= 0m)
		_pipSize = GetPipSize();

		if (_pipSize <= 0m)
		return;

		var upperBand = ma + LevelUpPips * _pipSize;
		var lowerBand = ma + LevelDownPips * _pipSize;

		var stoch = (StochasticOscillatorValue)stochasticValue;
		if (stoch.K is not decimal kValue || stoch.D is not decimal dValue)
		return;

		ManageOpenPositions(candle);
		UpdateTrailingStops(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var shouldBuy = candle.ClosePrice > upperBand && (kValue > StochasticLevel1 || dValue > StochasticLevel1);
		var shouldSell = candle.ClosePrice < lowerBand && (kValue < StochasticLevel2 || dValue < StochasticLevel2);

		if (ReverseSignals)
		{
			(shouldBuy, shouldSell) = (shouldSell, shouldBuy);
		}

		if (shouldBuy && Position <= 0m)
		{
			var volume = CalculateOrderVolume();
			if (volume > 0m)
			{
				var total = volume + (Position < 0m ? Math.Abs(Position) : 0m);
				if (total > 0m)
				BuyMarket(total);
			}
		}
		else if (shouldSell && Position >= 0m)
		{
			var volume = CalculateOrderVolume();
			if (volume > 0m)
			{
				var total = volume + (Position > 0m ? Math.Abs(Position) : 0m);
				if (total > 0m)
				SellMarket(total);
			}
		}
	}

	private void ManageOpenPositions(ICandleMessage candle)
	{
		if (Position > 0m && _longEntryPrice is decimal entry)
		{
			if (_longTakeProfitPrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Math.Abs(Position));
				ResetLongState();
				return;
			}

			if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Math.Abs(Position));
				ResetLongState();
			}
		}
		else if (Position < 0m && _shortEntryPrice is decimal entry)
		{
			if (_shortTakeProfitPrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				return;
			}

			if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
			}
		}
	}

	private void UpdateTrailingStops(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0 || TrailingStepPips <= 0 || _pipSize <= 0m)
		return;

		var trailingDistance = TrailingStopPips * _pipSize;
		var trailingStep = TrailingStepPips * _pipSize;

		if (Position > 0m && _longEntryPrice is decimal entry)
		{
			var profitDistance = candle.HighPrice - entry;
			if (profitDistance > trailingDistance)
			{
				var candidate = candle.HighPrice - trailingDistance;
				if (_longStopPrice is decimal current)
				{
					if (candidate > current + trailingStep)
					_longStopPrice = candidate;
				}
				else if (candidate > entry)
				{
					_longStopPrice = candidate;
				}
			}

			if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Math.Abs(Position));
				ResetLongState();
			}
		}
		else if (Position < 0m && _shortEntryPrice is decimal entry)
		{
			var profitDistance = entry - candle.LowPrice;
			if (profitDistance > trailingDistance)
			{
				var candidate = candle.LowPrice + trailingDistance;
				if (_shortStopPrice is decimal current)
				{
					if (candidate < current - trailingStep)
					_shortStopPrice = candidate;
				}
				else if (candidate < entry)
				{
					_shortStopPrice = candidate;
				}
			}

			if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
			}
		}
	}

	private decimal CalculateOrderVolume()
	{
		if (ManagementMode == VolumeMode.FixedLot)
		return VolumeValue;

		if (ManagementMode != VolumeMode.RiskPercent)
		return 0m;

		if (VolumeValue <= 0m)
		return 0m;

		var stopOffset = StopLossPips * _pipSize;
		if (stopOffset <= 0m)
		return 0m;

		var portfolio = Portfolio;
		if (portfolio is null)
		return 0m;

		var equity = portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;
		if (equity <= 0m)
		return 0m;

		var priceStep = Security?.PriceStep ?? 0m;
		var stepPrice = Security?.StepPrice ?? 0m;
		if (priceStep <= 0m || stepPrice <= 0m)
		return 0m;

		var perUnitRisk = stopOffset / priceStep * stepPrice;
		if (perUnitRisk <= 0m)
		return 0m;

		var riskAmount = equity * VolumeValue / 100m;
		var rawVolume = riskAmount / perUnitRisk;

		var volumeStep = Security?.VolumeStep ?? 0m;
		if (volumeStep > 0m)
		{
			var steps = Math.Max(1m, Math.Floor(rawVolume / volumeStep));
			return steps * volumeStep;
		}

		return Math.Max(rawVolume, 0m);
	}

	private bool TryGetShiftedMa(decimal currentValue, out decimal shifted)
	{
		_maHistory.Add(currentValue);

		var shift = Math.Max(0, MaShift);
		var maxCapacity = Math.Max(shift + 1, 10);
		if (_maHistory.Count > maxCapacity)
		_maHistory.RemoveRange(0, _maHistory.Count - maxCapacity);

		if (_maHistory.Count <= shift)
		{
			shifted = 0m;
			return false;
		}

		shifted = _maHistory[_maHistory.Count - 1 - shift];
		return true;
	}

	private void ResetState()
	{
		ResetLongState();
		ResetShortState();
	}

	private void ResetLongState()
	{
		_longEntryPrice = null;
		_longStopPrice = null;
		_longTakeProfitPrice = null;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = null;
		_shortStopPrice = null;
		_shortTakeProfitPrice = null;
	}

	private decimal GetPipSize()
	{
		var security = Security;
		if (security == null)
		return 0m;

		var step = security.PriceStep ?? 0m;
		if (step <= 0m)
		return 0m;

		var decimals = security.Decimals ?? 0;
		return decimals >= 3 ? step * 10m : step;
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MaMethod method, int period)
	{
		return method switch
		{
			MaMethod.Simple => new SimpleMovingAverage { Length = period },
			MaMethod.Exponential => new ExponentialMovingAverage { Length = period },
			MaMethod.LinearWeighted => new LinearWeightedMovingAverage { Length = period },
			_ => new SmoothedMovingAverage { Length = period }
		};
	}

	public enum VolumeMode
	{
		FixedLot,
		RiskPercent
	}

	public enum MaMethod
	{
		Simple,
		Exponential,
		Smoothed,
		LinearWeighted
	}
}

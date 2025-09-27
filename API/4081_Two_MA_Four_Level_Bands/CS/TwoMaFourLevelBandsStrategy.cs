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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "ytg_2MA_4Level" MetaTrader strategy.
/// Executes trades when a fast moving average crosses a slow moving average within configurable offset bands.
/// Symmetric stop-loss and take-profit distances are preserved via StockSharp's protective module.
/// </summary>
public class TwoMaFourLevelBandsStrategy : Strategy
{
	public enum CandlePrices
	{
		Open = 0,
		High = 1,
		Low = 2,
		Close = 3,
		Median = 4,
		Typical = 5,
		Weighted = 6
	}

	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _calculationBar;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<MovingAverageMethods> _fastMethod;
	private readonly StrategyParam<CandlePrices> _fastPrice;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<MovingAverageMethods> _slowMethod;
	private readonly StrategyParam<CandlePrices> _slowPrice;
	private readonly StrategyParam<int> _upperLevel1;
	private readonly StrategyParam<int> _upperLevel2;
	private readonly StrategyParam<int> _lowerLevel1;
	private readonly StrategyParam<int> _lowerLevel2;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _fastHistory = new();
	private readonly Queue<decimal> _slowHistory = new();

	private decimal _pipSize;

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public TwoMaFourLevelBandsStrategy()
	{
		_takeProfitPips = Param(nameof(TakeProfitPips), 130)
			.SetNotNegative()
			.SetDisplay("Take-profit (pips)", "Distance in pips for the take-profit order.", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(50, 300, 50);

		_stopLossPips = Param(nameof(StopLossPips), 1000)
			.SetNotNegative()
			.SetDisplay("Stop-loss (pips)", "Distance in pips for the protective stop.", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(200, 1500, 100);

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade volume", "Base lot size for new positions.", "Execution");

		_calculationBar = Param(nameof(CalculationBar), 1)
			.SetNotNegative()
			.SetDisplay("Calculation bar", "Shift used when evaluating the moving averages.", "Indicators");

		_fastPeriod = Param(nameof(FastPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA period", "Period of the fast moving average.", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 40, 5);

		_fastMethod = Param(nameof(FastMethod), MovingAverageMethods.Smoothed)
			.SetDisplay("Fast MA method", "Type of moving average used for the fast line.", "Indicators");

		_fastPrice = Param(nameof(FastPrice), CandlePrices.Median)
			.SetDisplay("Fast MA price", "Applied price used by the fast moving average.", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 180)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA period", "Period of the slow moving average.", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(60, 300, 20);

		_slowMethod = Param(nameof(SlowMethod), MovingAverageMethods.Smoothed)
			.SetDisplay("Slow MA method", "Type of moving average used for the slow line.", "Indicators");

		_slowPrice = Param(nameof(SlowPrice), CandlePrices.Median)
			.SetDisplay("Slow MA price", "Applied price used by the slow moving average.", "Indicators");

		_upperLevel1 = Param(nameof(UpperLevel1), 500)
			.SetNotNegative()
			.SetDisplay("Upper level #1", "Positive offset (in pips) added to the slow MA.", "Levels")
			.SetCanOptimize(true)
			.SetOptimize(100, 800, 100);

		_upperLevel2 = Param(nameof(UpperLevel2), 250)
			.SetNotNegative()
			.SetDisplay("Upper level #2", "Secondary positive offset added to the slow MA.", "Levels")
			.SetCanOptimize(true)
			.SetOptimize(50, 500, 50);

		_lowerLevel1 = Param(nameof(LowerLevel1), 500)
			.SetNotNegative()
			.SetDisplay("Lower level #1", "Negative offset (in pips) subtracted from the slow MA.", "Levels")
			.SetCanOptimize(true)
			.SetOptimize(100, 800, 100);

		_lowerLevel2 = Param(nameof(LowerLevel2), 250)
			.SetNotNegative()
			.SetDisplay("Lower level #2", "Secondary negative offset subtracted from the slow MA.", "Levels")
			.SetCanOptimize(true)
			.SetOptimize(50, 500, 50);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle type", "Time frame used for signal calculations.", "General");
	}

	/// <summary>
	/// Distance of the take-profit order in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Distance of the stop-loss order in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Base lot size for new positions.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Shift applied when sampling moving averages.
	/// </summary>
	public int CalculationBar
	{
		get => _calculationBar.Value;
		set => _calculationBar.Value = value;
	}

	/// <summary>
	/// Period of the fast moving average.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Moving average method for the fast line.
	/// </summary>
	public MovingAverageMethods FastMethod
	{
		get => _fastMethod.Value;
		set => _fastMethod.Value = value;
	}

	/// <summary>
	/// Applied price for the fast moving average.
	/// </summary>
	public CandlePrices FastPrice
	{
		get => _fastPrice.Value;
		set => _fastPrice.Value = value;
	}

	/// <summary>
	/// Period of the slow moving average.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Moving average method for the slow line.
	/// </summary>
	public MovingAverageMethods SlowMethod
	{
		get => _slowMethod.Value;
		set => _slowMethod.Value = value;
	}

	/// <summary>
	/// Applied price for the slow moving average.
	/// </summary>
	public CandlePrices SlowPrice
	{
		get => _slowPrice.Value;
		set => _slowPrice.Value = value;
	}

	/// <summary>
	/// Upper offset expressed in pips.
	/// </summary>
	public int UpperLevel1
	{
		get => _upperLevel1.Value;
		set => _upperLevel1.Value = value;
	}

	/// <summary>
	/// Secondary upper offset expressed in pips.
	/// </summary>
	public int UpperLevel2
	{
		get => _upperLevel2.Value;
		set => _upperLevel2.Value = value;
	}

	/// <summary>
	/// Lower offset expressed in pips.
	/// </summary>
	public int LowerLevel1
	{
		get => _lowerLevel1.Value;
		set => _lowerLevel1.Value = value;
	}

	/// <summary>
	/// Secondary lower offset expressed in pips.
	/// </summary>
	public int LowerLevel2
	{
		get => _lowerLevel2.Value;
		set => _lowerLevel2.Value = value;
	}

	/// <summary>
	/// Type of candles used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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
		_fastHistory.Clear();
		_slowHistory.Clear();
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastHistory.Clear();
		_slowHistory.Clear();
		_pipSize = CalculatePipSize();

		var fastMa = CreateMovingAverage(FastMethod, FastPeriod, FastPrice);
		var slowMa = CreateMovingAverage(SlowMethod, SlowPeriod, SlowPrice);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastMa, slowMa, ProcessCandle)
			.Start();

		var takeUnit = TakeProfitPips > 0 ? new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute) : null;
		var stopUnit = StopLossPips > 0 ? new Unit(StopLossPips * _pipSize, UnitTypes.Absolute) : null;

		if (takeUnit != null || stopUnit != null)
		{
			// Replicate MetaTrader's protective orders with StockSharp's engine.
			StartProtection(takeProfit: takeUnit, stopLoss: stopUnit, useMarketOrders: true);
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		// Only work with completed candles.
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!TryGetShiftedValues(_fastHistory, fastValue, CalculationBar, out var fastCurrent, out var fastPrevious))
			return;

		if (!TryGetShiftedValues(_slowHistory, slowValue, CalculationBar, out var slowCurrent, out var slowPrevious))
			return;

		var pipSize = _pipSize > 0m ? _pipSize : CalculatePipSize();
		if (_pipSize <= 0m)
			_pipSize = pipSize;

		var upper1Offset = UpperLevel1 * pipSize;
		var upper2Offset = UpperLevel2 * pipSize;
		var lower1Offset = LowerLevel1 * pipSize;
		var lower2Offset = LowerLevel2 * pipSize;

		var bullish = IsCrossUp(fastPrevious, fastCurrent, slowPrevious, slowCurrent);

		if (!bullish && UpperLevel1 > 0)
			bullish = IsCrossUp(fastPrevious, fastCurrent, slowPrevious + upper1Offset, slowCurrent + upper1Offset);
		if (!bullish && UpperLevel2 > 0)
			bullish = IsCrossUp(fastPrevious, fastCurrent, slowPrevious + upper2Offset, slowCurrent + upper2Offset);
		if (!bullish && LowerLevel1 > 0)
			bullish = IsCrossUp(fastPrevious, fastCurrent, slowPrevious - lower1Offset, slowCurrent - lower1Offset);
		if (!bullish && LowerLevel2 > 0)
			bullish = IsCrossUp(fastPrevious, fastCurrent, slowPrevious - lower2Offset, slowCurrent - lower2Offset);

		var bearish = IsCrossDown(fastPrevious, fastCurrent, slowPrevious, slowCurrent);

		if (!bearish && UpperLevel1 > 0)
			bearish = IsCrossDown(fastPrevious, fastCurrent, slowPrevious + upper1Offset, slowCurrent + upper1Offset);
		if (!bearish && UpperLevel2 > 0)
			bearish = IsCrossDown(fastPrevious, fastCurrent, slowPrevious + upper2Offset, slowCurrent + upper2Offset);
		if (!bearish && LowerLevel1 > 0)
			bearish = IsCrossDown(fastPrevious, fastCurrent, slowPrevious - lower1Offset, slowCurrent - lower1Offset);
		if (!bearish && LowerLevel2 > 0)
			bearish = IsCrossDown(fastPrevious, fastCurrent, slowPrevious - lower2Offset, slowCurrent - lower2Offset);

		if (Position == 0m && !HasActiveOrders())
		{
			var volume = NormalizeVolume(TradeVolume);
			if (volume <= 0m)
				return;

			Volume = volume;

			if (bullish)
			{
				// Fast MA crossed above the slow MA (possibly within an offset band).
				BuyMarket(volume);
			}
			else if (bearish)
			{
				// Fast MA crossed below the slow MA (possibly within an offset band).
				SellMarket(volume);
			}
		}
	}

	private static bool IsCrossUp(decimal prevFast, decimal currentFast, decimal prevSlow, decimal currentSlow)
	{
		return prevFast <= prevSlow && currentFast > currentSlow;
	}

	private static bool IsCrossDown(decimal prevFast, decimal currentFast, decimal prevSlow, decimal currentSlow)
	{
		return prevFast >= prevSlow && currentFast < currentSlow;
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageMethods method, int period, CandlePrices price)
	{
		var indicator = method switch
		{
			MovingAverageMethods.Simple => new SimpleMovingAverage(),
			MovingAverageMethods.Exponential => new ExponentialMovingAverage(),
			MovingAverageMethods.Smoothed => new SmoothedMovingAverage(),
			MovingAverageMethods.LinearWeighted => new WeightedMovingAverage(),
			_ => new SimpleMovingAverage(),
		};

		indicator.Length = Math.Max(1, period);
		indicator.CandlePrice = price;

		return indicator;
	}

	private bool TryGetShiftedValues(Queue<decimal> buffer, decimal currentValue, int shift, out decimal current, out decimal previous)
	{
		buffer.Enqueue(currentValue);

		var required = Math.Max(shift + 2, 2);
		while (buffer.Count > required)
			buffer.Dequeue();

		if (buffer.Count < shift + 2)
		{
			current = 0m;
			previous = 0m;
			return false;
		}

		var items = buffer.ToArray();
		var currentIndex = items.Length - 1 - shift;
		var previousIndex = items.Length - 2 - shift;

		if (currentIndex < 0 || previousIndex < 0)
		{
			current = 0m;
			previous = 0m;
			return false;
		}

		current = items[currentIndex];
		previous = items[previousIndex];
		return true;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
			return volume;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Max(1m, Math.Round(volume / step, MidpointRounding.AwayFromZero));
			volume = steps * step;
		}

		var minVolume = security.MinVolume;
		if (minVolume > 0m && volume < minVolume)
			volume = minVolume.Value;

		var maxVolume = security.MaxVolume;
		if (maxVolume > 0m && volume > maxVolume)
			volume = maxVolume.Value;

		return volume;
	}

	private bool HasActiveOrders()
	{
		return Orders.Any(o => o.State.IsActive());
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;

		if (step <= 0m)
			return 1m;

		if (step < 0.001m)
			return step * 10m;

		return step;
	}

	/// <summary>
	/// Moving average methods available in MetaTrader.
	/// </summary>
	public enum MovingAverageMethods
	{
		Simple = 0,
		Exponential = 1,
		Smoothed = 2,
		LinearWeighted = 3,
	}
}

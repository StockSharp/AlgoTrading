using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Reimplementation of the YTG ADX threshold breakout expert using high level StockSharp API.
/// The strategy waits for the +DI or -DI line to break above configurable levels and opens
/// a position in the corresponding direction with protective stop-loss and take-profit.
/// </summary>
public class YtgAdxLevelCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<int> _levelPlus;
	private readonly StrategyParam<int> _levelMinus;
	private readonly StrategyParam<int> _shift;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;

	private AverageDirectionalIndex _adx;

	private readonly List<decimal> _plusDiHistory = [];
	private readonly List<decimal> _minusDiHistory = [];

	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	public int LevelPlus
	{
		get => _levelPlus.Value;
		set => _levelPlus.Value = value;
	}

	public int LevelMinus
	{
		get => _levelMinus.Value;
		set => _levelMinus.Value = value;
	}

	public int Shift
	{
		get => _shift.Value;
		set => _shift.Value = value;
	}

	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public YtgAdxLevelCrossStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 28)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Period for the Average Directional Index", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 2);

		_levelPlus = Param(nameof(LevelPlus), 5)
			.SetGreaterOrEqualZero()
			.SetDisplay("+DI Level", "Threshold that the +DI line must break", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(5, 40, 5);

		_levelMinus = Param(nameof(LevelMinus), 5)
			.SetGreaterOrEqualZero()
			.SetDisplay("-DI Level", "Threshold that the -DI line must break", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(5, 40, 5);

		_shift = Param(nameof(Shift), 1)
			.SetGreaterOrEqualZero()
			.SetDisplay("Signal Shift", "Number of closed candles to look back", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(0, 3, 1);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 500m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit (points)", "Distance to take profit in price points", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 500m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss (points)", "Distance to stop loss in price points", "Risk");

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Base volume for market orders", "Orders");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for the strategy", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_plusDiHistory.Clear();
		_minusDiHistory.Clear();
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_adx = new AverageDirectionalIndex
		{
			Length = AdxPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_adx, ProcessAdx)
			.Start();

		var step = Security.PriceStep ?? 1m;
		Unit? takeProfit = null;
		Unit? stopLoss = null;

		if (TakeProfitPoints > 0)
			takeProfit = new Unit(TakeProfitPoints * step, UnitTypes.Point);

		if (StopLossPoints > 0)
			stopLoss = new Unit(StopLossPoints * step, UnitTypes.Point);

		if (takeProfit != null || stopLoss != null)
		{
			StartProtection(takeProfit: takeProfit, stopLoss: stopLoss);
		}
	}

	private void ProcessAdx(ICandleMessage candle, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!adxValue.IsFinal)
			return;

		if (adxValue is not AverageDirectionalIndexValue typed)
			return;

		if (typed.Dx.Plus is not decimal plusDi || typed.Dx.Minus is not decimal minusDi)
			return;

		UpdateHistory(_plusDiHistory, plusDi);
		UpdateHistory(_minusDiHistory, minusDi);

		var currentShift = Shift;
		var minCount = currentShift + 2;

		if (_plusDiHistory.Count < minCount || _minusDiHistory.Count < minCount)
			return;

		var currentIndex = _plusDiHistory.Count - 1 - currentShift;
		var previousIndex = currentIndex - 1;

		if (previousIndex < 0)
			return;

		var shiftedPlus = _plusDiHistory[currentIndex];
		var shiftedPlusPrev = _plusDiHistory[previousIndex];
		var shiftedMinus = _minusDiHistory[currentIndex];
		var shiftedMinusPrev = _minusDiHistory[previousIndex];

		var longSignal = shiftedPlus > LevelPlus && shiftedPlusPrev < LevelPlus;
		var shortSignal = shiftedMinus > LevelMinus && shiftedMinusPrev < LevelMinus;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position == 0)
		{
			if (longSignal)
			{
				// Enter a long position when +DI breaks above the configured level.
				BuyMarket();
			}
			else if (shortSignal)
			{
				// Enter a short position when -DI breaks above the configured level.
				SellMarket();
			}
		}
	}

	private void UpdateHistory(List<decimal> history, decimal value)
	{
		history.Add(value);

		var maxLength = Shift + 2;

		while (history.Count > maxLength)
		{
			// Keep only the amount of history required for the configured shift.
			history.RemoveAt(0);
		}
	}
}

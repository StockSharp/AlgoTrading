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
/// Port of the MetaTrader advisor MacdPatternTrader.
/// Implements the MACD histogram double-top/double-bottom pattern with adaptive arming logic.
/// </summary>
public class MacdPatternTraderTriggerStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<decimal> _bullishTrigger;
	private readonly StrategyParam<decimal> _bullishReset;
	private readonly StrategyParam<decimal> _bearishTrigger;
	private readonly StrategyParam<decimal> _bearishReset;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergence _macd = null!;

	private decimal? _macdPrev1;
	private decimal? _macdPrev2;
	private decimal? _macdPrev3;

	private bool _bullishArmed;
	private bool _bullishWindow;
	private bool _bullishReady;

	private bool _bearishArmed;
	private bool _bearishWindow;
	private bool _bearishReady;

	private decimal _priceStep = 1m;

	/// <summary>
	/// Trade volume used for market orders.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Fast EMA length of the MACD indicator.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA length of the MACD indicator.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Signal smoothing period of the MACD indicator.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Positive MACD value that arms the bullish pattern.
	/// </summary>
	public decimal BullishTrigger
	{
		get => _bullishTrigger.Value;
		set => _bullishTrigger.Value = value;
	}

	/// <summary>
	/// Threshold that marks the bullish pullback zone.
	/// </summary>
	public decimal BullishReset
	{
		get => _bullishReset.Value;
		set => _bullishReset.Value = value;
	}

	/// <summary>
	/// Negative MACD value that arms the bearish pattern.
	/// </summary>
	public decimal BearishTrigger
	{
		get => _bearishTrigger.Value;
		set => _bearishTrigger.Value = value;
	}

	/// <summary>
	/// Threshold that marks the bearish pullback zone.
	/// </summary>
	public decimal BearishReset
	{
		get => _bearishReset.Value;
		set => _bearishReset.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in instrument points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in instrument points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Candle type that drives the MACD calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public MacdPatternTraderTriggerStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Order volume for entries", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.05m, 0.3m, 0.05m);

		_fastPeriod = Param(nameof(FastPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA length for MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(8, 18, 1);

		_slowPeriod = Param(nameof(SlowPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA length for MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(4, 15, 1);

		_signalPeriod = Param(nameof(SignalPeriod), 1)
			.SetGreaterThanZero()
			.SetDisplay("Signal EMA", "Signal EMA length for MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1, 5, 1);

		_bullishTrigger = Param(nameof(BullishTrigger), 0.0015m)
			.SetGreaterThanZero()
			.SetDisplay("Bullish Trigger", "MACD level that arms the bullish pattern", "Logic");

		_bullishReset = Param(nameof(BullishReset), 0.0005m)
			.SetGreaterThanZero()
			.SetDisplay("Bullish Reset", "MACD pullback threshold for bullish setup", "Logic");

		_bearishTrigger = Param(nameof(BearishTrigger), 0.0015m)
			.SetGreaterThanZero()
			.SetDisplay("Bearish Trigger", "Absolute MACD level that arms the bearish pattern", "Logic");

		_bearishReset = Param(nameof(BearishReset), 0.0005m)
			.SetGreaterThanZero()
			.SetDisplay("Bearish Reset", "MACD pullback threshold for bearish setup", "Logic");

		_stopLossPoints = Param(nameof(StopLossPoints), 100m)
			.SetGreaterThanOrEqual(0m)
			.SetDisplay("Stop Loss", "Stop-loss distance in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(50m, 200m, 25m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 300m)
			.SetGreaterThanOrEqual(0m)
			.SetDisplay("Take Profit", "Take-profit distance in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(100m, 400m, 50m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for indicator calculations", "Data");
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
		_bullishArmed = false;
		_bullishWindow = false;
		_bullishReady = false;
		_bearishArmed = false;
		_bearishWindow = false;
		_bearishReady = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 1m;
		if (_priceStep <= 0m)
			_priceStep = 1m;

		var takeProfit = TakeProfitPoints > 0m ? new Unit(TakeProfitPoints * _priceStep, UnitTypes.Point) : null;
		var stopLoss = StopLossPoints > 0m ? new Unit(StopLossPoints * _priceStep, UnitTypes.Point) : null;

		StartProtection(takeProfit, stopLoss);

		_macd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = FastPeriod,
			LongPeriod = SlowPeriod,
			SignalPeriod = SignalPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!macdValue.IsFinal || macdValue is not MovingAverageConvergenceDivergenceValue macdLineValue)
			return;

		if (macdLineValue.Macd is not decimal currentMacd)
			return;

		if (_macdPrev1 is null || _macdPrev2 is null || _macdPrev3 is null)
		{
			ShiftHistory(currentMacd);
			return;
		}

		var macdCurr = _macdPrev1.Value;
		var macdLast = _macdPrev2.Value;
		var macdLast3 = _macdPrev3.Value;

		EvaluateBearishPattern(macdCurr, macdLast, macdLast3);
		EvaluateBullishPattern(macdCurr, macdLast, macdLast3);

		ShiftHistory(currentMacd);
	}

	private void EvaluateBullishPattern(decimal macdCurr, decimal macdLast, decimal macdLast3)
	{
		if (macdCurr < 0m)
		{
			_bullishArmed = false;
			_bullishWindow = false;
			_bullishReady = false;
		}
		else
		{
			if (!_bullishArmed && macdCurr > BullishTrigger)
				_bullishArmed = true;

			if (_bullishArmed && macdCurr < BullishReset)
			{
				_bullishArmed = false;
				_bullishWindow = true;
			}
		}

		if (_bullishWindow && macdCurr > macdLast && macdLast < macdLast3 && macdCurr > BullishReset && macdLast < BullishReset)
		{
			_bullishReady = true;
			_bullishWindow = false;
		}

		if (!_bullishReady)
			return;

		var volumeToBuy = TradeVolume + Math.Max(0m, -Position);
		if (volumeToBuy > 0m)
			BuyMarket(volumeToBuy);

		_bullishReady = false;
		_bullishArmed = false;
		_bullishWindow = false;
	}

	private void EvaluateBearishPattern(decimal macdCurr, decimal macdLast, decimal macdLast3)
	{
		if (macdCurr > 0m)
		{
			_bearishArmed = false;
			_bearishWindow = false;
			_bearishReady = false;
		}
		else
		{
			if (!_bearishArmed && macdCurr < -BearishTrigger)
				_bearishArmed = true;

			if (_bearishArmed && macdCurr > -BearishReset)
			{
				_bearishArmed = false;
				_bearishWindow = true;
			}
		}

		if (_bearishWindow && macdCurr < macdLast && macdLast > macdLast3 && macdCurr < -BearishReset && macdLast > -BearishReset)
		{
			_bearishReady = true;
			_bearishWindow = false;
		}

		if (!_bearishReady)
			return;

		var volumeToSell = TradeVolume + Math.Max(0m, Position);
		if (volumeToSell > 0m)
			SellMarket(volumeToSell);

		_bearishReady = false;
		_bearishArmed = false;
		_bearishWindow = false;
	}

	private void ShiftHistory(decimal current)
	{
		_macdPrev3 = _macdPrev2;
		_macdPrev2 = _macdPrev1;
		_macdPrev1 = current;
	}
}


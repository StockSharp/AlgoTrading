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
/// Port of the Auto RXD v1.67 MetaTrader expert advisor.
/// Implements the perceptron-driven supervisor with optional indicator filters.
/// </summary>
public class AutoRXDV167Strategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<bool> _useAtrTargets;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrTakeProfitFactor;
	private readonly StrategyParam<decimal> _atrStopLossFactor;
	private readonly StrategyParam<decimal> _longTakeProfitPoints;
	private readonly StrategyParam<decimal> _longStopLossPoints;
	private readonly StrategyParam<decimal> _shortTakeProfitPoints;
	private readonly StrategyParam<decimal> _shortStopLossPoints;
	private readonly StrategyParam<bool> _useIndicatorFilters;
	private readonly StrategyParam<bool> _useAdxFilter;
	private readonly StrategyParam<bool> _useMacdFilter;
	private readonly StrategyParam<bool> _useRsiFilter;
	private readonly StrategyParam<bool> _useCciFilter;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _shortMaPeriod;
	private readonly StrategyParam<int> _shortStep;
	private readonly StrategyParam<decimal> _shortX1;
	private readonly StrategyParam<decimal> _shortX2;
	private readonly StrategyParam<decimal> _shortX3;
	private readonly StrategyParam<decimal> _shortX4;
	private readonly StrategyParam<decimal> _shortThreshold;
	private readonly StrategyParam<int> _longMaPeriod;
	private readonly StrategyParam<int> _longStep;
	private readonly StrategyParam<decimal> _longX1;
	private readonly StrategyParam<decimal> _longX2;
	private readonly StrategyParam<decimal> _longX3;
	private readonly StrategyParam<decimal> _longX4;
	private readonly StrategyParam<decimal> _longThreshold;
	private readonly StrategyParam<int> _supervisorMaPeriod;
	private readonly StrategyParam<int> _supervisorStep;
	private readonly StrategyParam<decimal> _supervisorX1;
	private readonly StrategyParam<decimal> _supervisorX2;
	private readonly StrategyParam<decimal> _supervisorX3;
	private readonly StrategyParam<decimal> _supervisorX4;
	private readonly StrategyParam<decimal> _supervisorThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _shortCloseBuffer = new();
	private readonly List<decimal> _shortWeightedBuffer = new();
	private readonly List<decimal> _shortWeightedHistory = new();
	private readonly List<decimal> _longCloseBuffer = new();
	private readonly List<decimal> _longWeightedBuffer = new();
	private readonly List<decimal> _longWeightedHistory = new();
	private readonly List<decimal> _supervisorCloseBuffer = new();
	private readonly List<decimal> _supervisorWeightedBuffer = new();
	private readonly List<decimal> _supervisorWeightedHistory = new();

	private AverageTrueRange _atr = null!;
	private RelativeStrengthIndex _rsi = null!;
	private CommodityChannelIndex _cci = null!;
	private MovingAverageConvergenceDivergence _macd = null!;
	private AverageDirectionalIndex _adx = null!;

	/// <summary>
	/// Initializes the strategy parameters.
	/// </summary>
	public AutoRXDV167Strategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
		.SetDisplay("Order Volume", "Volume for new entries", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(0.01m, 2m, 0.01m);

		_useAtrTargets = Param(nameof(UseAtrTargets), true)
		.SetDisplay("Use ATR Targets", "Switch between ATR-based and fixed pip targets", "Risk");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
		.SetDisplay("ATR Period", "Average True Range period", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(5, 50, 1);

		_atrTakeProfitFactor = Param(nameof(AtrTakeProfitFactor), 4m)
		.SetDisplay("ATR TP Factor", "Multiplier applied to ATR for take profit", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1m, 6m, 0.5m);

		_atrStopLossFactor = Param(nameof(AtrStopLossFactor), 3m)
		.SetDisplay("ATR SL Factor", "Multiplier applied to ATR for stop loss", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1m, 6m, 0.5m);

		_longTakeProfitPoints = Param(nameof(LongTakeProfitPoints), 1000m)
		.SetDisplay("Long TP Points", "Take profit points for long trades", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(100m, 2000m, 50m);

		_longStopLossPoints = Param(nameof(LongStopLossPoints), 1000m)
		.SetDisplay("Long SL Points", "Stop loss points for long trades", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(100m, 2000m, 50m);

		_shortTakeProfitPoints = Param(nameof(ShortTakeProfitPoints), 1000m)
		.SetDisplay("Short TP Points", "Take profit points for short trades", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(100m, 2000m, 50m);

		_shortStopLossPoints = Param(nameof(ShortStopLossPoints), 1000m)
		.SetDisplay("Short SL Points", "Stop loss points for short trades", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(100m, 2000m, 50m);

		_useIndicatorFilters = Param(nameof(UseIndicatorFilters), true)
		.SetDisplay("Use Indicator Filters", "Enable confirmation filters", "Filters");

		_useAdxFilter = Param(nameof(UseAdxFilter), false)
		.SetDisplay("Use ADX Filter", "Require ADX agreement", "Filters");

		_useMacdFilter = Param(nameof(UseMacdFilter), false)
		.SetDisplay("Use MACD Filter", "Require MACD confirmation", "Filters");

		_useRsiFilter = Param(nameof(UseRsiFilter), false)
		.SetDisplay("Use RSI Filter", "Require RSI confirmation", "Filters");

		_useCciFilter = Param(nameof(UseCciFilter), false)
		.SetDisplay("Use CCI Filter", "Require CCI confirmation", "Filters");
		_adxPeriod = Param(nameof(AdxPeriod), 14)
		.SetDisplay("ADX Period", "ADX calculation period", "Filters")
		.SetCanOptimize(true)
		.SetOptimize(7, 40, 1);

		_adxThreshold = Param(nameof(AdxThreshold), 21m)
		.SetDisplay("ADX Threshold", "Minimum ADX value", "Filters")
		.SetCanOptimize(true)
		.SetOptimize(10m, 40m, 1m);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
		.SetDisplay("RSI Period", "RSI calculation period", "Filters")
		.SetCanOptimize(true)
		.SetOptimize(5, 40, 1);

		_cciPeriod = Param(nameof(CciPeriod), 14)
		.SetDisplay("CCI Period", "CCI calculation period", "Filters")
		.SetCanOptimize(true)
		.SetOptimize(5, 40, 1);

		_macdFast = Param(nameof(MacdFast), 12)
		.SetDisplay("MACD Fast", "Fast EMA length", "Filters")
		.SetCanOptimize(true)
		.SetOptimize(5, 18, 1);

		_macdSlow = Param(nameof(MacdSlow), 26)
		.SetDisplay("MACD Slow", "Slow EMA length", "Filters")
		.SetCanOptimize(true)
		.SetOptimize(10, 40, 1);

		_macdSignal = Param(nameof(MacdSignal), 9)
		.SetDisplay("MACD Signal", "Signal SMA length", "Filters")
		.SetCanOptimize(true)
		.SetOptimize(5, 18, 1);

		_shortMaPeriod = Param(nameof(ShortMaPeriod), 21)
		.SetDisplay("Short MA Period", "Perceptron short MA period", "Perceptron")
		.SetCanOptimize(true)
		.SetOptimize(5, 60, 1);

		_shortStep = Param(nameof(ShortStep), 5)
		.SetDisplay("Short Step", "Lag in candles for short perceptron", "Perceptron")
		.SetCanOptimize(true)
		.SetOptimize(1, 40, 1);

		_shortX1 = Param(nameof(ShortX1), 100m)
		.SetDisplay("Short Weight 1", "First perceptron weight", "Perceptron");

		_shortX2 = Param(nameof(ShortX2), 100m)
		.SetDisplay("Short Weight 2", "Second perceptron weight", "Perceptron");

		_shortX3 = Param(nameof(ShortX3), 100m)
		.SetDisplay("Short Weight 3", "Third perceptron weight", "Perceptron");

		_shortX4 = Param(nameof(ShortX4), 100m)
		.SetDisplay("Short Weight 4", "Fourth perceptron weight", "Perceptron");

		_shortThreshold = Param(nameof(ShortThreshold), 100m)
		.SetDisplay("Short Threshold", "Bias for the short perceptron", "Perceptron");

		_longMaPeriod = Param(nameof(LongMaPeriod), 21)
		.SetDisplay("Long MA Period", "Perceptron long MA period", "Perceptron")
		.SetCanOptimize(true)
		.SetOptimize(5, 60, 1);

		_longStep = Param(nameof(LongStep), 5)
		.SetDisplay("Long Step", "Lag in candles for long perceptron", "Perceptron")
		.SetCanOptimize(true)
		.SetOptimize(1, 40, 1);

		_longX1 = Param(nameof(LongX1), 100m)
		.SetDisplay("Long Weight 1", "First perceptron weight", "Perceptron");

		_longX2 = Param(nameof(LongX2), 100m)
		.SetDisplay("Long Weight 2", "Second perceptron weight", "Perceptron");

		_longX3 = Param(nameof(LongX3), 100m)
		.SetDisplay("Long Weight 3", "Third perceptron weight", "Perceptron");

		_longX4 = Param(nameof(LongX4), 100m)
		.SetDisplay("Long Weight 4", "Fourth perceptron weight", "Perceptron");

		_longThreshold = Param(nameof(LongThreshold), 100m)
		.SetDisplay("Long Threshold", "Bias for the long perceptron", "Perceptron");

		_supervisorMaPeriod = Param(nameof(SupervisorMaPeriod), 21)
		.SetDisplay("Supervisor MA Period", "Supervisor MA period", "Perceptron")
		.SetCanOptimize(true)
		.SetOptimize(5, 60, 1);

		_supervisorStep = Param(nameof(SupervisorStep), 5)
		.SetDisplay("Supervisor Step", "Lag for supervisor perceptron", "Perceptron")
		.SetCanOptimize(true)
		.SetOptimize(1, 40, 1);

		_supervisorX1 = Param(nameof(SupervisorX1), 100m)
		.SetDisplay("Supervisor Weight 1", "First supervisor weight", "Perceptron");

		_supervisorX2 = Param(nameof(SupervisorX2), 100m)
		.SetDisplay("Supervisor Weight 2", "Second supervisor weight", "Perceptron");

		_supervisorX3 = Param(nameof(SupervisorX3), 100m)
		.SetDisplay("Supervisor Weight 3", "Third supervisor weight", "Perceptron");

		_supervisorX4 = Param(nameof(SupervisorX4), 100m)
		.SetDisplay("Supervisor Weight 4", "Fourth supervisor weight", "Perceptron");

		_supervisorThreshold = Param(nameof(SupervisorThreshold), 100m)
		.SetDisplay("Supervisor Threshold", "Bias for the supervisor perceptron", "Perceptron");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle series", "Trading");
	}
	/// <summary>
	/// Base order volume.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Enable ATR-based protective targets.
	/// </summary>
	public bool UseAtrTargets
	{
		get => _useAtrTargets.Value;
		set => _useAtrTargets.Value = value;
	}

	/// <summary>
	/// ATR indicator period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR take profit multiplier.
	/// </summary>
	public decimal AtrTakeProfitFactor
	{
		get => _atrTakeProfitFactor.Value;
		set => _atrTakeProfitFactor.Value = value;
	}

	/// <summary>
	/// ATR stop loss multiplier.
	/// </summary>
	public decimal AtrStopLossFactor
	{
		get => _atrStopLossFactor.Value;
		set => _atrStopLossFactor.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
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
		_shortCloseBuffer.Clear();
		_shortWeightedBuffer.Clear();
		_shortWeightedHistory.Clear();
		_longCloseBuffer.Clear();
		_longWeightedBuffer.Clear();
		_longWeightedHistory.Clear();
		_supervisorCloseBuffer.Clear();
		_supervisorWeightedBuffer.Clear();
		_supervisorWeightedHistory.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		_atr = new AverageTrueRange { Length = AtrPeriod };
		_rsi = new RelativeStrengthIndex { Length = _rsiPeriod.Value };
		_cci = new CommodityChannelIndex { Length = _cciPeriod.Value };
		_macd = new()
		{
			Macd =
{
ShortMa = { Length = _macdFast.Value },
LongMa = { Length = _macdSlow.Value }
},
			SignalMa = { Length = _macdSignal.Value }
		};
		_adx = new AverageDirectionalIndex { Length = _adxPeriod.Value };

		var subscription = SubscribeCandles(CandleType);
		subscription
		// Bind the indicator group to ProcessCandle while keeping access to the raw values.
		.BindEx(_atr, _rsi, _cci, _macd, _adx, ProcessCandle)
		.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _atr);
		}
	}
	private void ProcessCandle(
	ICandleMessage candle,
	IIndicatorValue atrValue,
	IIndicatorValue rsiValue,
	IIndicatorValue cciValue,
	IIndicatorValue macdValue,
	IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			return;
		}

		var weightedPrice = GetWeightedPrice(candle);

		// Update the short perceptron caches and wait until the window is filled.
		if (!TryUpdatePerceptron(_shortCloseBuffer, _shortWeightedBuffer, _shortWeightedHistory, _shortMaPeriod.Value, _shortStep.Value, candle.ClosePrice, weightedPrice))
		{
			return;
		}

		// Update the long perceptron caches for the bullish entry model.
		if (!TryUpdatePerceptron(_longCloseBuffer, _longWeightedBuffer, _longWeightedHistory, _longMaPeriod.Value, _longStep.Value, candle.ClosePrice, weightedPrice))
		{
			return;
		}

		// Update the supervisor perceptron which toggles between bullish and bearish specialists.
		if (!TryUpdatePerceptron(_supervisorCloseBuffer, _supervisorWeightedBuffer, _supervisorWeightedHistory, _supervisorMaPeriod.Value, _supervisorStep.Value, candle.ClosePrice, weightedPrice))
		{
			return;
		}

		var supervisorScore = EvaluatePerceptron(
		_supervisorWeightedHistory,
		_supervisorCloseBuffer,
		_supervisorMaPeriod.Value,
		_supervisorStep.Value,
		_supervisorX1.Value,
		_supervisorX2.Value,
		_supervisorX3.Value,
		_supervisorX4.Value,
		_supervisorThreshold.Value);

		if (supervisorScore is null)
		{
			return;
		}

		// Convert indicator snapshots only when they provide final values for the candle.
		var atr = atrValue.IsFinal ? atrValue.TryGetFinalDecimal() : null;
		var rsi = rsiValue.IsFinal ? rsiValue.TryGetFinalDecimal() : null;
		var cci = cciValue.IsFinal ? cciValue.TryGetFinalDecimal() : null;
		var macdData = macdValue.IsFinal ? macdValue as MovingAverageConvergenceDivergenceValue : null;
		var adxData = adxValue.IsFinal ? adxValue as AverageDirectionalIndexValue : null;

		if (supervisorScore > 0m)
		{
			var longScore = EvaluatePerceptron(
			_longWeightedHistory,
			_longCloseBuffer,
			_longMaPeriod.Value,
			_longStep.Value,
			_longX1.Value,
			_longX2.Value,
			_longX3.Value,
			_longX4.Value,
			_longThreshold.Value);

			if (longScore is null || longScore <= 0m)
			{
				return;
			}

			if (UseIndicatorFilters && !AreFiltersSatisfied(Sides.Buy, rsi, cci, macdData, adxData))
			{
				return;
			}

			TryEnterLong(candle, atr);
		}
		else
		{
			var shortScore = EvaluatePerceptron(
			_shortWeightedHistory,
			_shortCloseBuffer,
			_shortMaPeriod.Value,
			_shortStep.Value,
			_shortX1.Value,
			_shortX2.Value,
			_shortX3.Value,
			_shortX4.Value,
			_shortThreshold.Value);

			if (shortScore is null || shortScore >= 0m)
			{
				return;
			}

			if (UseIndicatorFilters && !AreFiltersSatisfied(Sides.Sell, rsi, cci, macdData, adxData))
			{
				return;
			}

			TryEnterShort(candle, atr);
		}
	}
	private void TryEnterLong(ICandleMessage candle, decimal? atr)
	{
		if (HasActiveOrders())
		{
			return;
		}

		var priceStep = GetPriceStep();
		if (priceStep <= 0m)
		{
			return;
		}

		if (Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
		}

		if (Position > 0m)
		{
			return;
		}

		var entryPrice = Security?.BestAskPrice ?? candle.ClosePrice;

		var resultingPosition = Position + OrderVolume;

		BuyMarket(OrderVolume);

		// Compute take profit and stop loss distances using either ATR or fixed thresholds.
		var protective = CalculateProtectiveDistances(Sides.Buy, priceStep, atr);
		if (protective is null)
		{
			return;
		}

		var (takeDistance, stopDistance) = protective.Value;
		SetProtectiveOrders(entryPrice, takeDistance, stopDistance, resultingPosition, Sides.Buy);
	}

	private void TryEnterShort(ICandleMessage candle, decimal? atr)
	{
		if (HasActiveOrders())
		{
			return;
		}

		var priceStep = GetPriceStep();
		if (priceStep <= 0m)
		{
			return;
		}

		if (Position > 0m)
		{
			SellMarket(Position);
		}

		if (Position < 0m)
		{
			return;
		}

		var entryPrice = Security?.BestBidPrice ?? candle.ClosePrice;

		var resultingPosition = Position - OrderVolume;

		SellMarket(OrderVolume);

		// Compute take profit and stop loss distances using either ATR or fixed thresholds.
		var protective = CalculateProtectiveDistances(Sides.Sell, priceStep, atr);
		if (protective is null)
		{
			return;
		}

		var (takeDistance, stopDistance) = protective.Value;
		SetProtectiveOrders(entryPrice, takeDistance, stopDistance, resultingPosition, Sides.Sell);
	}

	private (decimal takeDistance, decimal stopDistance)? CalculateProtectiveDistances(Sides side, decimal priceStep, decimal? atr)
	{
		decimal takePoints;
		decimal stopPoints;

		if (UseAtrTargets)
		{
			if (atr is null || atr <= 0m)
			{
				return null;
			}

			takePoints = side == Sides.Buy ? _longTakeProfitPoints.Value : _shortTakeProfitPoints.Value;
			stopPoints = side == Sides.Buy ? _longStopLossPoints.Value : _shortStopLossPoints.Value;

			var takeDistanceAtr = atr.Value * (takePoints / 100m) * AtrTakeProfitFactor;
			var stopDistanceAtr = atr.Value * (stopPoints / 100m) * AtrStopLossFactor;
			return (takeDistanceAtr, stopDistanceAtr);
		}

		takePoints = side == Sides.Buy ? _longTakeProfitPoints.Value : _shortTakeProfitPoints.Value;
		stopPoints = side == Sides.Buy ? _longStopLossPoints.Value : _shortStopLossPoints.Value;

		return (takePoints * priceStep, stopPoints * priceStep);
	}

	private void SetProtectiveOrders(decimal entryPrice, decimal takeDistance, decimal stopDistance, decimal resultingPosition, Sides side)
	{
		var priceStep = GetPriceStep();
		if (priceStep <= 0m)
		{
			return;
		}

		var stopPrice = side == Sides.Buy ? entryPrice - stopDistance : entryPrice + stopDistance;
		var takePrice = side == Sides.Buy ? entryPrice + takeDistance : entryPrice - takeDistance;

		var stopSteps = GetDistanceInSteps(entryPrice, stopPrice, priceStep);
		var takeSteps = GetDistanceInSteps(entryPrice, takePrice, priceStep);

		if (stopSteps > 0)
		{
			SetStopLoss(stopSteps, entryPrice, resultingPosition);
		}

		if (takeSteps > 0)
		{
			SetTakeProfit(takeSteps, entryPrice, resultingPosition);
		}
	}

	private static int GetDistanceInSteps(decimal fromPrice, decimal toPrice, decimal priceStep)
	{
		if (priceStep <= 0m)
		{
			return 0;
		}

		var distance = Math.Abs(fromPrice - toPrice);
		if (distance <= 0m)
		{
			return 0;
		}

		var steps = decimal.Divide(distance, priceStep);
		return (int)Math.Round(steps, MidpointRounding.AwayFromZero);
	}
	private bool AreFiltersSatisfied(Sides direction, decimal? rsi, decimal? cci, MovingAverageConvergenceDivergenceValue macd, AverageDirectionalIndexValue adx)
	{
		if (_useRsiFilter.Value)
		{
			if (rsi is null)
			{
				return false;
			}

			if (direction == Sides.Buy && rsi <= 50m)
			{
				return false;
			}

			if (direction == Sides.Sell && rsi >= 50m)
			{
				return false;
			}
		}

		if (_useCciFilter.Value)
		{
			if (cci is null)
			{
				return false;
			}

			if (direction == Sides.Buy && cci <= 100m)
			{
				return false;
			}

			if (direction == Sides.Sell && cci >= -100m)
			{
				return false;
			}
		}

		if (_useMacdFilter.Value)
		{
			if (macd is null || macd.Macd is not decimal macdLine || macd.Signal is not decimal signalLine)
			{
				return false;
			}

			if (direction == Sides.Buy && macdLine <= signalLine)
			{
				return false;
			}

			if (direction == Sides.Sell && macdLine >= signalLine)
			{
				return false;
			}
		}

		if (_useAdxFilter.Value)
		{
			if (adx is null || adx.MovingAverage is not decimal adxMain)
			{
				return false;
			}

			if (adxMain < _adxThreshold.Value)
			{
				return false;
			}

			var plusDi = adx.Dx.Plus;
			var minusDi = adx.Dx.Minus;

			if (direction == Sides.Buy && plusDi <= minusDi)
			{
				return false;
			}

			if (direction == Sides.Sell && plusDi >= minusDi)
			{
				return false;
			}
		}

		return true;
	}

	private static decimal GetWeightedPrice(ICandleMessage candle)
	{
		return (candle.HighPrice + candle.LowPrice + (candle.ClosePrice * 2m)) / 4m;
	}

	private bool TryUpdatePerceptron(
	List<decimal> closeBuffer,
	List<decimal> weightedBuffer,
	List<decimal> history,
	int length,
	int step,
	decimal closePrice,
	decimal weightedPrice)
	{
		var closeMa = CalculateLinearWeightedAverage(closeBuffer, length, closePrice);
		var weightedMa = CalculateLinearWeightedAverage(weightedBuffer, length, weightedPrice);

		if (weightedMa is null || closeMa is null)
		{
			return false;
		}

		AddToHistory(history, weightedMa.Value, step);
		return true;
	}

	private static decimal? EvaluatePerceptron(
	List<decimal> history,
	List<decimal> closeBuffer,
	int length,
	int step,
	decimal weight1,
	decimal weight2,
	decimal weight3,
	decimal weight4,
	decimal threshold)
	{
		if (closeBuffer.Count < length)
		{
			return null;
		}

		var currentCloseMa = CalculateLinearWeightedAverageSnapshot(closeBuffer, length);
		if (currentCloseMa is null)
		{
			return null;
		}

		if (!TryGetShifted(history, step, out var w1))
		{
			return null;
		}

		if (!TryGetShifted(history, step * 2, out var w2))
		{
			return null;
		}

		if (!TryGetShifted(history, step * 3, out var w3))
		{
			return null;
		}

		if (!TryGetShifted(history, step * 4, out var w4))
		{
			return null;
		}

		var feature1 = currentCloseMa.Value - w1;
		var feature2 = w1 - w2;
		var feature3 = w2 - w3;
		var feature4 = w3 - w4;

		var sum = (weight1 - 100m) * feature1
		+ (weight2 - 100m) * feature2
		+ (weight3 - 100m) * feature3
		+ (weight4 - 100m) * feature4
		+ (threshold - 100m);

		return sum;
	}

	private static decimal? CalculateLinearWeightedAverage(List<decimal> buffer, int length, decimal newValue)
	{
		buffer.Add(newValue);
		if (buffer.Count > length)
		{
			buffer.RemoveAt(0);
		}

		if (buffer.Count < length)
		{
			return null;
		}

		return CalculateLinearWeightedAverageSnapshot(buffer, length);
	}

	private static decimal? CalculateLinearWeightedAverageSnapshot(IReadOnlyList<decimal> buffer, int length)
	{
		if (buffer.Count < length)
		{
			return null;
		}

		var numerator = 0m;
		var denominator = 0m;
		for (var i = 0; i < length; i++)
		{
			var weight = length - i;
			numerator += buffer[buffer.Count - length + i] * weight;
			denominator += weight;
		}

		return denominator == 0m ? null : numerator / denominator;
	}

	private static void AddToHistory(List<decimal> history, decimal value, int step)
	{
		history.Add(value);
		var required = (step * 4) + 5;
		if (history.Count > required)
		{
			history.RemoveRange(0, history.Count - required);
		}
	}

	private static bool TryGetShifted(IReadOnlyList<decimal> history, int shift, out decimal value)
	{
		if (shift <= 0 || history.Count <= shift)
		{
			value = default;
			return false;
		}

		value = history[history.Count - 1 - shift];
		return true;
	}

	private decimal GetPriceStep()
	{
		return Security?.PriceStep ?? Security?.MinPriceStep ?? 0m;
	}

	private bool HasActiveOrders()
	{
		foreach (var order in Orders)
		{
			if (order.State.IsActive())
			{
				return true;
			}
		}

		return false;
	}
}

internal static class IndicatorExtensions
{
	public static decimal? TryGetFinalDecimal(this IIndicatorValue value)
	{
		if (!value.IsFinal)
		{
			return null;
		}

		return value.GetValue<decimal>();
	}
}
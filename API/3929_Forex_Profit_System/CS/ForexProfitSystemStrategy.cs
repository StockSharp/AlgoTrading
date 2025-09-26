using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend-following strategy that replicates the MetaTrader "Forex Profit System" expert.
/// Combines three exponential moving averages with a Parabolic SAR filter and layered trade management.
/// </summary>
public class ForexProfitSystemStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _mediumEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMax;
	private readonly StrategyParam<int> _longTakeProfitPoints;
	private readonly StrategyParam<int> _shortTakeProfitPoints;
	private readonly StrategyParam<int> _longStopLossPoints;
	private readonly StrategyParam<int> _shortStopLossPoints;
	private readonly StrategyParam<int> _longTrailingStopPoints;
	private readonly StrategyParam<int> _shortTrailingStopPoints;
	private readonly StrategyParam<int> _longProfitTriggerPoints;
	private readonly StrategyParam<int> _shortProfitTriggerPoints;

	private ExponentialMovingAverage _fastEma = null!;
	private ExponentialMovingAverage _mediumEma = null!;
	private ExponentialMovingAverage _slowEma = null!;
	private ParabolicSar _parabolicSar = null!;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrevValues;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;

	/// <summary>
	/// Type of candles to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period for the fast exponential moving average.
	/// </summary>
	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	/// <summary>
	/// Period for the medium exponential moving average.
	/// </summary>
	public int MediumEmaLength
	{
		get => _mediumEmaLength.Value;
		set => _mediumEmaLength.Value = value;
	}

	/// <summary>
	/// Period for the slow exponential moving average.
	/// </summary>
	public int SlowEmaLength
	{
		get => _slowEmaLength.Value;
		set => _slowEmaLength.Value = value;
	}

	/// <summary>
	/// Initial acceleration step for Parabolic SAR.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	/// <summary>
	/// Maximum acceleration factor for Parabolic SAR.
	/// </summary>
	public decimal SarMax
	{
		get => _sarMax.Value;
		set => _sarMax.Value = value;
	}


	/// <summary>
	/// Take-profit distance for long positions expressed in points.
	/// </summary>
	public int LongTakeProfitPoints
	{
		get => _longTakeProfitPoints.Value;
		set => _longTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance for short positions expressed in points.
	/// </summary>
	public int ShortTakeProfitPoints
	{
		get => _shortTakeProfitPoints.Value;
		set => _shortTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for long positions expressed in points.
	/// </summary>
	public int LongStopLossPoints
	{
		get => _longStopLossPoints.Value;
		set => _longStopLossPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for short positions expressed in points.
	/// </summary>
	public int ShortStopLossPoints
	{
		get => _shortStopLossPoints.Value;
		set => _shortStopLossPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance for long positions expressed in points.
	/// </summary>
	public int LongTrailingStopPoints
	{
		get => _longTrailingStopPoints.Value;
		set => _longTrailingStopPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance for short positions expressed in points.
	/// </summary>
	public int ShortTrailingStopPoints
	{
		get => _shortTrailingStopPoints.Value;
		set => _shortTrailingStopPoints.Value = value;
	}

	/// <summary>
	/// Minimum profit in points required before exiting long trades on EMA reversal.
	/// </summary>
	public int LongProfitTriggerPoints
	{
		get => _longProfitTriggerPoints.Value;
		set => _longProfitTriggerPoints.Value = value;
	}

	/// <summary>
	/// Minimum profit in points required before exiting short trades on EMA reversal.
	/// </summary>
	public int ShortProfitTriggerPoints
	{
		get => _shortProfitTriggerPoints.Value;
		set => _shortProfitTriggerPoints.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ForexProfitSystemStrategy"/> class.
	/// </summary>
	public ForexProfitSystemStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");

		_fastEmaLength = Param(nameof(FastEmaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Period of the fast EMA", "Indicators");

		_mediumEmaLength = Param(nameof(MediumEmaLength), 25)
			.SetGreaterThanZero()
			.SetDisplay("Medium EMA", "Period of the medium EMA", "Indicators");

		_slowEmaLength = Param(nameof(SlowEmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Period of the slow EMA", "Indicators");

		_sarStep = Param(nameof(SarStep), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Step", "Initial acceleration factor for Parabolic SAR", "Indicators");

		_sarMax = Param(nameof(SarMax), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Max", "Maximum acceleration factor for Parabolic SAR", "Indicators");


		_longTakeProfitPoints = Param(nameof(LongTakeProfitPoints), 50)
			.SetDisplay("Long TP", "Take-profit distance for long trades (points)", "Risk");

		_shortTakeProfitPoints = Param(nameof(ShortTakeProfitPoints), 50)
			.SetDisplay("Short TP", "Take-profit distance for short trades (points)", "Risk");

		_longStopLossPoints = Param(nameof(LongStopLossPoints), 30)
			.SetDisplay("Long SL", "Stop-loss distance for long trades (points)", "Risk");

		_shortStopLossPoints = Param(nameof(ShortStopLossPoints), 30)
			.SetDisplay("Short SL", "Stop-loss distance for short trades (points)", "Risk");

		_longTrailingStopPoints = Param(nameof(LongTrailingStopPoints), 10)
			.SetDisplay("Long Trailing", "Trailing stop distance for long trades (points)", "Risk");

		_shortTrailingStopPoints = Param(nameof(ShortTrailingStopPoints), 10)
			.SetDisplay("Short Trailing", "Trailing stop distance for short trades (points)", "Risk");

		_longProfitTriggerPoints = Param(nameof(LongProfitTriggerPoints), 10)
			.SetDisplay("Long Profit Trigger", "Minimum profit before long exit on EMA reversal", "Risk");

		_shortProfitTriggerPoints = Param(nameof(ShortProfitTriggerPoints), 5)
			.SetDisplay("Short Profit Trigger", "Minimum profit before short exit on EMA reversal", "Risk");
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

		_prevFast = 0m;
		_prevSlow = 0m;
		_hasPrevValues = false;
		_longEntryPrice = null;
		_shortEntryPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Prepare indicators that mimic the original MetaTrader setup.
		_fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		_mediumEma = new ExponentialMovingAverage { Length = MediumEmaLength };
		_slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };
		_parabolicSar = new ParabolicSar
		{
			Acceleration = SarStep,
			AccelerationMax = SarMax
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_parabolicSar, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastEma);
			DrawIndicator(area, _mediumEma);
			DrawIndicator(area, _slowEma);
			DrawIndicator(area, _parabolicSar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sarValue)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		// Calculate median price to replicate PRICE_MEDIAN input from MetaTrader.
		var medianPrice = (candle.HighPrice + candle.LowPrice) / 2m;

		// Update EMA values on the closed bar.
		var fastValue = _fastEma.Process(medianPrice, candle.OpenTime, true).ToDecimal();
		var mediumValue = _mediumEma.Process(medianPrice, candle.OpenTime, true).ToDecimal();
		var slowValue = _slowEma.Process(medianPrice, candle.OpenTime, true).ToDecimal();

		if (!_fastEma.IsFormed || !_mediumEma.IsFormed || !_slowEma.IsFormed)
		{
			return;
		}

		var hadPrev = _hasPrevValues;
		var prevFast = _prevFast;
		var prevSlow = _prevSlow;
		var closePrice = candle.ClosePrice;

		if (hadPrev)
		{
			TryOpenPositions(closePrice, fastValue, mediumValue, slowValue, prevFast, prevSlow, sarValue);
			ManageOpenPositions(closePrice, fastValue, prevFast);
		}

		_prevFast = fastValue;
		_prevSlow = slowValue;
		_hasPrevValues = true;
	}

	private void TryOpenPositions(decimal closePrice, decimal fastValue, decimal mediumValue, decimal slowValue, decimal prevFast, decimal prevSlow, decimal sarValue)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		{
			return;
		}

		if (Volume <= 0m || Position != 0m)
		{
			return;
		}

		// Detect bullish crossover with Parabolic SAR confirmation.
		var bullishSignal = fastValue > mediumValue && fastValue > slowValue && prevFast <= prevSlow && sarValue < closePrice;
		if (bullishSignal)
		{
			var resultingPosition = Position + Volume;
			BuyMarket(Volume);
			_longEntryPrice = closePrice;
			_shortEntryPrice = null;
			ApplyRiskManagement(closePrice, resultingPosition, true);
			return;
		}

		// Detect bearish crossover with Parabolic SAR confirmation.
		var bearishSignal = fastValue < mediumValue && fastValue < slowValue && prevFast >= prevSlow && sarValue > closePrice;
		if (bearishSignal)
		{
			var resultingPosition = Position - Volume;
			SellMarket(Volume);
			_shortEntryPrice = closePrice;
			_longEntryPrice = null;
			ApplyRiskManagement(closePrice, resultingPosition, false);
		}
	}

	private void ManageOpenPositions(decimal closePrice, decimal fastValue, decimal prevFast)
	{
		if (Position > 0m)
		{
			var entryPrice = _longEntryPrice;
			if (entryPrice == null)
			{
				return;
			}

			var profitDistance = closePrice - entryPrice.Value;
			var exitThreshold = ToPriceDistance(LongProfitTriggerPoints);

			// Close longs on EMA reversal if profit requirement is met.
			if (exitThreshold > 0m && profitDistance >= exitThreshold && fastValue < prevFast)
			{
				SellMarket(Position);
				_longEntryPrice = null;
				return;
			}

			var trailingDistance = ToPriceDistance(LongTrailingStopPoints);
			if (trailingDistance > 0m && profitDistance > trailingDistance)
			{
				// Lock profits by moving the stop closer to current price.
				SetStopLoss(trailingDistance, closePrice, Position);
			}
		}
		else if (Position < 0m)
		{
			var entryPrice = _shortEntryPrice;
			if (entryPrice == null)
			{
				return;
			}

			var profitDistance = entryPrice.Value - closePrice;
			var exitThreshold = ToPriceDistance(ShortProfitTriggerPoints);

			// Close shorts on EMA reversal if profit requirement is met.
			if (exitThreshold > 0m && profitDistance >= exitThreshold && fastValue > prevFast)
			{
				BuyMarket(-Position);
				_shortEntryPrice = null;
				return;
			}

			var trailingDistance = ToPriceDistance(ShortTrailingStopPoints);
			if (trailingDistance > 0m && profitDistance > trailingDistance)
			{
				// Tighten the protective stop once price moves far enough.
				SetStopLoss(trailingDistance, closePrice, Position);
			}
		}
	}

	private void ApplyRiskManagement(decimal referencePrice, decimal resultingPosition, bool isLong)
	{
		var takeProfitPoints = isLong ? LongTakeProfitPoints : ShortTakeProfitPoints;
		var stopLossPoints = isLong ? LongStopLossPoints : ShortStopLossPoints;

		var takeProfitDistance = ToPriceDistance(takeProfitPoints);
		if (takeProfitDistance > 0m)
		{
			SetTakeProfit(takeProfitDistance, referencePrice, resultingPosition);
		}

		var stopLossDistance = ToPriceDistance(stopLossPoints);
		if (stopLossDistance > 0m)
		{
			SetStopLoss(stopLossDistance, referencePrice, resultingPosition);
		}
	}

	private decimal ToPriceDistance(int points)
	{
		if (points <= 0)
		{
			return 0m;
		}

		var step = Security?.PriceStep;
		if (step == null || step.Value <= 0m)
		{
			return points;
		}

		return points * step.Value;
	}
}

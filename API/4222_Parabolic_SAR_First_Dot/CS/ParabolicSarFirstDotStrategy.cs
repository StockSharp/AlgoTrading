using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR first dot reversal strategy.
/// Opens a position when Parabolic SAR flips relative to the close and protects it with classic stops.
/// </summary>
public class ParabolicSarFirstDotStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<bool> _useStopMultiplier;
	private readonly StrategyParam<decimal> _sarAccelerationStep;
	private readonly StrategyParam<decimal> _sarAccelerationMax;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;
	private bool? _prevIsSarAbovePrice;
	private decimal _priceStep;

	/// <summary>
	/// Trading volume in lots.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set
		{
			_tradeVolume.Value = value;
			Volume = value;
		}
	}

	/// <summary>
	/// Stop-loss distance expressed in Parabolic SAR points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in Parabolic SAR points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Multiply stop distances by ten to mirror the MetaTrader implementation.
	/// </summary>
	public bool UseStopMultiplier
	{
		get => _useStopMultiplier.Value;
		set => _useStopMultiplier.Value = value;
	}

	/// <summary>
	/// Initial acceleration factor for Parabolic SAR.
	/// </summary>
	public decimal SarAccelerationStep
	{
		get => _sarAccelerationStep.Value;
		set => _sarAccelerationStep.Value = value;
	}

	/// <summary>
	/// Maximum acceleration factor for Parabolic SAR.
	/// </summary>
	public decimal SarAccelerationMax
	{
		get => _sarAccelerationMax.Value;
		set => _sarAccelerationMax.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ParabolicSarFirstDotStrategy"/>.
	/// </summary>
	public ParabolicSarFirstDotStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetDisplay("Volume", "Order volume in lots", "General")
			.SetGreaterThanZero();

		_stopLossPoints = Param(nameof(StopLossPoints), 90)
			.SetDisplay("Stop-Loss Points", "Stop-loss distance converted through the instrument price step", "Risk Management")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(30, 150, 10);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 20)
			.SetDisplay("Take-Profit Points", "Take-profit distance converted through the instrument price step", "Risk Management")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(10, 80, 10);

		_useStopMultiplier = Param(nameof(UseStopMultiplier), true)
			.SetDisplay("Use Stop Multiplier", "Multiply distances by ten to reproduce MetaTrader stop handling", "Risk Management");

		_sarAccelerationStep = Param(nameof(SarAccelerationStep), 0.02m)
			.SetDisplay("SAR Step", "Initial acceleration factor for Parabolic SAR", "Indicator")
			.SetRange(0.01m, 0.05m)
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 0.05m, 0.01m);

		_sarAccelerationMax = Param(nameof(SarAccelerationMax), 0.2m)
			.SetDisplay("SAR Max", "Maximum acceleration factor for Parabolic SAR", "Indicator")
			.SetRange(0.1m, 0.4m)
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 0.4m, 0.05m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for calculations", "General");

		Volume = _tradeVolume.Value;
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

		_prevIsSarAbovePrice = null;
		_longStop = null;
		_longTake = null;
		_shortStop = null;
		_shortTake = null;
		Volume = _tradeVolume.Value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = GetPriceStep();

		var parabolicSar = new ParabolicSar
		{
			Acceleration = SarAccelerationStep,
			AccelerationMax = SarAccelerationMax
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(parabolicSar, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, parabolicSar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sarValue)
	{
		// Work only with completed candles to match MetaTrader logic.
		if (candle.State != CandleStates.Finished)
			return;

		// Verify that the strategy is allowed to trade and subscriptions are active.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Check whether existing positions should be closed by protective levels.
		CheckProtectiveLevels(candle);

		var isSarAbovePrice = sarValue > candle.ClosePrice;

		// Initialize state on the first value.
		if (_prevIsSarAbovePrice == null)
		{
			_prevIsSarAbovePrice = isSarAbovePrice;
			return;
		}

		var sarSwitchedBelow = _prevIsSarAbovePrice.Value && !isSarAbovePrice;
		var sarSwitchedAbove = !_prevIsSarAbovePrice.Value && isSarAbovePrice;

		if (sarSwitchedBelow)
			TryEnterLong(candle, sarValue);
		else if (sarSwitchedAbove)
			TryEnterShort(candle, sarValue);

		_prevIsSarAbovePrice = isSarAbovePrice;
	}

	private void TryEnterLong(ICandleMessage candle, decimal sarValue)
	{
		// Prevent duplicate long entries.
		if (Position > 0m)
			return;

		var volume = Volume + Math.Abs(Position);
		if (volume <= 0m)
			return;

		BuyMarket(volume);

		var entryPrice = candle.ClosePrice;
		var stopDistance = GetDistance(StopLossPoints);
		var takeDistance = GetDistance(TakeProfitPoints);

		_longStop = entryPrice - stopDistance;
		_longTake = entryPrice + takeDistance;
		_shortStop = null;
		_shortTake = null;

		LogInfo($"Long entry after SAR flip. Close={entryPrice}, SAR={sarValue}, Stop={_longStop}, Take={_longTake}");
	}

	private void TryEnterShort(ICandleMessage candle, decimal sarValue)
	{
		// Prevent duplicate short entries.
		if (Position < 0m)
			return;

		var volume = Volume + Math.Abs(Position);
		if (volume <= 0m)
			return;

		SellMarket(volume);

		var entryPrice = candle.ClosePrice;
		var stopDistance = GetDistance(StopLossPoints);
		var takeDistance = GetDistance(TakeProfitPoints);

		_shortStop = entryPrice + stopDistance;
		_shortTake = entryPrice - takeDistance;
		_longStop = null;
		_longTake = null;

		LogInfo($"Short entry after SAR flip. Close={entryPrice}, SAR={sarValue}, Stop={_shortStop}, Take={_shortTake}");
	}

	private void CheckProtectiveLevels(ICandleMessage candle)
	{
		var position = Position;

		if (position > 0m)
		{
			if (_longStop is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Math.Abs(position));
				LogInfo($"Long stop-loss triggered at {stop}.");
				ResetLongTargets();
			}
			else if (_longTake is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Math.Abs(position));
				LogInfo($"Long take-profit triggered at {take}.");
				ResetLongTargets();
			}
		}
		else if (position < 0m)
		{
			if (_shortStop is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(position));
				LogInfo($"Short stop-loss triggered at {stop}.");
				ResetShortTargets();
			}
			else if (_shortTake is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(Math.Abs(position));
				LogInfo($"Short take-profit triggered at {take}.");
				ResetShortTargets();
			}
		}
	}

	private void ResetLongTargets()
	{
		_longStop = null;
		_longTake = null;
	}

	private void ResetShortTargets()
	{
		_shortStop = null;
		_shortTake = null;
	}

	private decimal GetDistance(int basePoints)
	{
		var multiplier = UseStopMultiplier ? 10 : 1;
		return basePoints * multiplier * _priceStep;
	}

	private decimal GetPriceStep()
	{
		// Use security price step when available, otherwise fall back to a minimal tick.
		var step = Security?.PriceStep ?? 0.0001m;
		return step > 0m ? step : 0.0001m;
	}
}

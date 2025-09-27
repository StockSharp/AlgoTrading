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
/// Parabolic SAR flip strategy converted from the MetaTrader expert <c>pSAR_bug_3</c>.
/// Closes the current position when the Parabolic SAR changes side and immediately opens the opposite trade.
/// Protective stop-loss and take-profit levels replicate the original point-based configuration.
/// </summary>
public class ParabolicSarBug3Strategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopMultiplier;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMaximum;
	private readonly StrategyParam<DataType> _candleType;

	private bool? _previousSarAbovePrice;
	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;
	private decimal _priceStep;

	/// <summary>
	/// Volume used for every new entry.
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
	/// Multiplier applied to the MetaTrader style stop distances.
	/// </summary>
	public int StopMultiplier
	{
		get => _stopMultiplier.Value;
		set => _stopMultiplier.Value = value;
	}

	/// <summary>
	/// Initial acceleration step for the Parabolic SAR indicator.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	/// <summary>
	/// Maximum acceleration for the Parabolic SAR indicator.
	/// </summary>
	public decimal SarMaximum
	{
		get => _sarMaximum.Value;
		set => _sarMaximum.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ParabolicSarBug3Strategy"/> class.
	/// </summary>
	public ParabolicSarBug3Strategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetDisplay("Volume", "Order volume in lots", "General")
			.SetGreaterThanZero();

		_stopLossPoints = Param(nameof(StopLossPoints), 90)
			.SetDisplay("Stop-Loss Points", "Distance converted through the instrument price step", "Risk Management")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(30, 150, 10);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 20)
			.SetDisplay("Take-Profit Points", "Distance converted through the instrument price step", "Risk Management")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(10, 80, 10);

		_stopMultiplier = Param(nameof(StopMultiplier), 10)
			.SetDisplay("Stop Multiplier", "Factor applied to MetaTrader-style stop distances", "Risk Management")
			.SetRange(1, 20)
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_sarStep = Param(nameof(SarStep), 0.02m)
			.SetDisplay("SAR Step", "Initial acceleration for Parabolic SAR", "Indicator")
			.SetRange(0.01m, 0.05m)
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 0.05m, 0.01m);

		_sarMaximum = Param(nameof(SarMaximum), 0.2m)
			.SetDisplay("SAR Max", "Maximum acceleration for Parabolic SAR", "Indicator")
			.SetRange(0.1m, 0.4m)
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 0.4m, 0.05m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Market data source for the indicator", "General");

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

		_previousSarAbovePrice = null;
		ResetLongTargets();
		ResetShortTargets();
		Volume = _tradeVolume.Value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = GetPriceStep();

		var parabolicSar = new ParabolicSar
		{
			Acceleration = SarStep,
			AccelerationMax = SarMaximum
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
		// Only completed candles match the MetaTrader signal timing.
		if (candle.State != CandleStates.Finished)
			return;

		// Skip processing until subscriptions are active and trading is permitted.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Check if protective targets must close existing positions.
		CheckProtectiveLevels(candle);

		var isSarAbovePrice = sarValue > candle.ClosePrice;

		// Initialise the state on the very first candle.
		if (_previousSarAbovePrice == null)
		{
			_previousSarAbovePrice = isSarAbovePrice;
			return;
		}

		var sarFlippedBelow = _previousSarAbovePrice.Value && !isSarAbovePrice;
		var sarFlippedAbove = !_previousSarAbovePrice.Value && isSarAbovePrice;

		if (sarFlippedBelow)
			TryEnterLong(candle, sarValue);
		else if (sarFlippedAbove)
			TryEnterShort(candle, sarValue);

		_previousSarAbovePrice = isSarAbovePrice;
	}

	private void TryEnterLong(ICandleMessage candle, decimal sarValue)
	{
		// Prevent stacking multiple long positions.
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
		ResetShortTargets();

		LogInfo($"Opened long after SAR flip. Close={entryPrice}, SAR={sarValue}, Stop={_longStop}, Take={_longTake}");
	}

	private void TryEnterShort(ICandleMessage candle, decimal sarValue)
	{
		// Prevent stacking multiple short positions.
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
		ResetLongTargets();

		LogInfo($"Opened short after SAR flip. Close={entryPrice}, SAR={sarValue}, Stop={_shortStop}, Take={_shortTake}");
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
		var multiplier = StopMultiplier;
		if (multiplier < 1)
			multiplier = 1;

		var step = _priceStep;
		if (step <= 0m)
			step = 0.0001m;

		return basePoints * multiplier * step;
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep ?? 0.0001m;
		return step > 0m ? step : 0.0001m;
	}
}

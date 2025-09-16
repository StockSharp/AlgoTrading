using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR reversal strategy ported from the MetaTrader expert "pSAR_bug2".
/// Opens a position on the very first Parabolic SAR flip and manages classic stop-loss and take-profit levels.
/// </summary>
public class ParabolicSarBug2Strategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMaximum;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;
	private bool? _wasSarAboveClose;
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
	/// Stop-loss distance expressed in price points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Initial acceleration factor for the Parabolic SAR indicator.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	/// <summary>
	/// Maximum acceleration factor for the Parabolic SAR indicator.
	/// </summary>
	public decimal SarMaximum
	{
		get => _sarMaximum.Value;
		set => _sarMaximum.Value = value;
	}

	/// <summary>
	/// Candle type used to evaluate the indicator and trading logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ParabolicSarBug2Strategy"/> class.
	/// </summary>
	public ParabolicSarBug2Strategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetDisplay("Volume", "Order volume in lots", "General")
			.SetGreaterThanZero();

		_stopLossPoints = Param(nameof(StopLossPoints), 90)
			.SetDisplay("Stop-Loss Points", "Stop-loss distance measured in price points", "Risk Management")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(30, 150, 10);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 20)
			.SetDisplay("Take-Profit Points", "Take-profit distance measured in price points", "Risk Management")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(10, 80, 10);

		_sarStep = Param(nameof(SarStep), 0.001m)
			.SetDisplay("SAR Step", "Initial acceleration factor for Parabolic SAR", "Indicator")
			.SetRange(0.001m, 0.05m)
			.SetCanOptimize(true)
			.SetOptimize(0.001m, 0.02m, 0.001m);

		_sarMaximum = Param(nameof(SarMaximum), 0.2m)
			.SetDisplay("SAR Maximum", "Maximum acceleration factor for Parabolic SAR", "Indicator")
			.SetRange(0.1m, 0.6m)
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

		_wasSarAboveClose = null;
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
		// Work only with completed candles to reproduce the MetaTrader behaviour.
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure that the strategy is allowed to trade and data is online.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Apply protective stops on every new candle.
		CheckProtectiveLevels(candle);

		var isSarAboveClose = sarValue > candle.ClosePrice;

		// Initialise the state when the very first SAR value arrives.
		if (_wasSarAboveClose == null)
		{
			_wasSarAboveClose = isSarAboveClose;
			return;
		}

		var sarFlippedBelow = _wasSarAboveClose.Value && !isSarAboveClose;
		var sarFlippedAbove = !_wasSarAboveClose.Value && isSarAboveClose;

		if (sarFlippedBelow)
		{
			HandleLongSignal(candle, sarValue);
		}
		else if (sarFlippedAbove)
		{
			HandleShortSignal(candle, sarValue);
		}

		_wasSarAboveClose = isSarAboveClose;
	}

	private void HandleLongSignal(ICandleMessage candle, decimal sarValue)
	{
		// Avoid stacking long positions.
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

	private void HandleShortSignal(ICandleMessage candle, decimal sarValue)
	{
		// Avoid stacking short positions.
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
		return basePoints * _priceStep;
	}

	private decimal GetPriceStep()
	{
		// Use the security price step when available, otherwise fall back to a small default tick.
		var step = Security?.PriceStep ?? 0.0001m;
		return step > 0m ? step : 0.0001m;
	}
}

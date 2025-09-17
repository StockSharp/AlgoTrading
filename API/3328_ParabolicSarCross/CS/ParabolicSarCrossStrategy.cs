using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy converted from the MetaTrader "PSAR Trader EA" expert advisor.
/// It trades Parabolic SAR crossovers while applying fixed stop loss, take profit, and trailing stop management.
/// </summary>
public class ParabolicSarCrossStrategy : Strategy
{
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMaximum;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _useAutoLot;
	private readonly StrategyParam<decimal> _fixedLot;
	private readonly StrategyParam<decimal> _lotsPerThousand;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStartPoints;
	private readonly StrategyParam<decimal> _trailingDistancePoints;

	private bool _hasPreviousSar;
	private bool _wasDotBelowPrice;
	private decimal _previousPosition;

	private decimal? _longStopLoss;
	private decimal? _longTakeProfit;
	private decimal? _shortStopLoss;
	private decimal? _shortTakeProfit;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;

	/// <summary>
	/// Parabolic SAR acceleration step.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	/// <summary>
	/// Parabolic SAR maximum acceleration.
	/// </summary>
	public decimal SarMaximum
	{
		get => _sarMaximum.Value;
		set => _sarMaximum.Value = value;
	}

	/// <summary>
	/// Candle data series used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Enables balance-dependent volume calculation.
	/// </summary>
	public bool UseAutoLot
	{
		get => _useAutoLot.Value;
		set => _useAutoLot.Value = value;
	}

	/// <summary>
	/// Fixed trade volume in lots when auto lot sizing is disabled.
	/// </summary>
	public decimal FixedLot
	{
		get => _fixedLot.Value;
		set => _fixedLot.Value = value;
	}

	/// <summary>
	/// Lots allocated per each 1000 units of account balance.
	/// </summary>
	public decimal LotsPerThousand
	{
		get => _lotsPerThousand.Value;
		set => _lotsPerThousand.Value = value;
	}

	/// <summary>
	/// Stop loss distance in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Profit distance required before the trailing stop activates (points).
	/// </summary>
	public decimal TrailingStartPoints
	{
		get => _trailingStartPoints.Value;
		set => _trailingStartPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance once activated (points).
	/// </summary>
	public decimal TrailingDistancePoints
	{
		get => _trailingDistancePoints.Value;
		set => _trailingDistancePoints.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ParabolicSarCrossStrategy"/> class.
	/// </summary>
	public ParabolicSarCrossStrategy()
	{
		_sarStep = Param(nameof(SarStep), 0.02m)
		.SetDisplay("SAR Step", "Acceleration factor used by Parabolic SAR", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(0.01m, 0.05m, 0.01m);

		_sarMaximum = Param(nameof(SarMaximum), 0.2m)
		.SetDisplay("SAR Maximum", "Maximum acceleration factor for Parabolic SAR", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 0.5m, 0.1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("Candle Type", "Source candle series", "General");

		_useAutoLot = Param(nameof(UseAutoLot), false)
		.SetDisplay("Use Auto Lot", "Enable balance based lot sizing", "Trading");

		_fixedLot = Param(nameof(FixedLot), 0.1m)
		.SetDisplay("Fixed Lot", "Volume used when auto lot is disabled", "Trading")
		.SetGreaterThanZero();

		_lotsPerThousand = Param(nameof(LotsPerThousand), 0.05m)
		.SetDisplay("Lots per 1000", "Volume multiplier per 1000 balance units", "Trading")
		.SetGreaterThanZero();

		_stopLossPoints = Param(nameof(StopLossPoints), 500m)
		.SetDisplay("Stop Loss (points)", "Stop loss distance in points", "Risk")
		.SetNotNegative();

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 1000m)
		.SetDisplay("Take Profit (points)", "Take profit distance in points", "Risk")
		.SetNotNegative();

		_trailingStartPoints = Param(nameof(TrailingStartPoints), 500m)
		.SetDisplay("Trailing Start (points)", "Profit required before trailing activates", "Risk")
		.SetNotNegative();

		_trailingDistancePoints = Param(nameof(TrailingDistancePoints), 100m)
		.SetDisplay("Trailing Distance (points)", "Trailing stop offset once active", "Risk")
		.SetNotNegative();

		Volume = FixedLot;
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

		_hasPreviousSar = false;
		_wasDotBelowPrice = false;
		_previousPosition = 0m;

		ResetProtection();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

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
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		UpdatePositionState(candle);

		if (ManageOpenPosition(candle))
			return;

		var dotBelowPrice = candle.ClosePrice > sarValue;

		if (_hasPreviousSar && dotBelowPrice != _wasDotBelowPrice)
		{
			if (dotBelowPrice)
			{
				if (Position < 0m)
				{
					ClosePosition();
					LogInfo($"Closing short at SAR bullish flip. Close={candle.ClosePrice:0.#####}, SAR={sarValue:0.#####}");
					return;
				}

				if (Position == 0m)
				{
					var volume = GetTradeVolume();
					if (volume > 0m)
					{
						BuyMarket(volume);
						LogInfo($"Opening long on SAR bullish flip. Close={candle.ClosePrice:0.#####}, SAR={sarValue:0.#####}");
					}
				}
			}
			else
			{
				if (Position > 0m)
				{
					ClosePosition();
					LogInfo($"Closing long at SAR bearish flip. Close={candle.ClosePrice:0.#####}, SAR={sarValue:0.#####}");
					return;
				}

				if (Position == 0m)
				{
					var volume = GetTradeVolume();
					if (volume > 0m)
					{
						SellMarket(volume);
						LogInfo($"Opening short on SAR bearish flip. Close={candle.ClosePrice:0.#####}, SAR={sarValue:0.#####}");
					}
				}
			}
		}

		_wasDotBelowPrice = dotBelowPrice;
		_hasPreviousSar = true;
		_previousPosition = Position;
	}

	private void UpdatePositionState(ICandleMessage candle)
	{
		if (Position > 0m && _previousPosition <= 0m)
		{
			InitializeLongProtection(candle);
		}
		else if (Position < 0m && _previousPosition >= 0m)
		{
			InitializeShortProtection(candle);
		}
		else if (Position == 0m && _previousPosition != 0m)
		{
			ResetProtection();
		}
	}

	private bool ManageOpenPosition(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			UpdateLongTrailing(candle);

			var stopPrice = GetLongStopPrice();
			if (stopPrice is decimal longStop && candle.LowPrice <= longStop)
			{
				SellMarket(Position);
				LogInfo($"Long stop triggered at {longStop:0.#####}");
				ResetLongProtection();
				return true;
			}

			if (_longTakeProfit is decimal longTake && candle.HighPrice >= longTake)
			{
				SellMarket(Position);
				LogInfo($"Long take profit reached at {longTake:0.#####}");
				ResetLongProtection();
				return true;
			}
		}
		else if (Position < 0m)
		{
			UpdateShortTrailing(candle);

			var stopPrice = GetShortStopPrice();
			if (stopPrice is decimal shortStop && candle.HighPrice >= shortStop)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Short stop triggered at {shortStop:0.#####}");
				ResetShortProtection();
				return true;
			}

			if (_shortTakeProfit is decimal shortTake && candle.LowPrice <= shortTake)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Short take profit reached at {shortTake:0.#####}");
				ResetShortProtection();
				return true;
			}
		}

		return false;
	}

	private void InitializeLongProtection(ICandleMessage candle)
	{
		var entry = PositionPrice ?? candle.ClosePrice;
		var stopOffset = PointsToPrice(StopLossPoints);
		var takeOffset = PointsToPrice(TakeProfitPoints);

		_longStopLoss = stopOffset > 0m ? entry - stopOffset : null;
		_longTakeProfit = takeOffset > 0m ? entry + takeOffset : null;
		_longTrailingStop = null;

		_shortStopLoss = null;
		_shortTakeProfit = null;
		_shortTrailingStop = null;
	}

	private void InitializeShortProtection(ICandleMessage candle)
	{
		var entry = PositionPrice ?? candle.ClosePrice;
		var stopOffset = PointsToPrice(StopLossPoints);
		var takeOffset = PointsToPrice(TakeProfitPoints);

		_shortStopLoss = stopOffset > 0m ? entry + stopOffset : null;
		_shortTakeProfit = takeOffset > 0m ? entry - takeOffset : null;
		_shortTrailingStop = null;

		_longStopLoss = null;
		_longTakeProfit = null;
		_longTrailingStop = null;
	}

	private void UpdateLongTrailing(ICandleMessage candle)
	{
		if (TrailingStartPoints <= 0m || TrailingDistancePoints <= 0m)
			return;

		var entry = PositionPrice ?? candle.ClosePrice;
		var start = PointsToPrice(TrailingStartPoints);
		var distance = PointsToPrice(TrailingDistancePoints);

		if (distance <= 0m)
			return;

		var profit = candle.HighPrice - entry;
		if (profit >= start)
		{
			var candidate = candle.HighPrice - distance;
			if (_longTrailingStop is decimal trailing)
			{
				if (candidate > trailing)
					_longTrailingStop = candidate;
			}
			else
			{
				_longTrailingStop = candidate;
			}
		}
	}

	private void UpdateShortTrailing(ICandleMessage candle)
	{
		if (TrailingStartPoints <= 0m || TrailingDistancePoints <= 0m)
			return;

		var entry = PositionPrice ?? candle.ClosePrice;
		var start = PointsToPrice(TrailingStartPoints);
		var distance = PointsToPrice(TrailingDistancePoints);

		if (distance <= 0m)
			return;

		var profit = entry - candle.LowPrice;
		if (profit >= start)
		{
			var candidate = candle.LowPrice + distance;
			if (_shortTrailingStop is decimal trailing)
			{
				if (candidate < trailing)
					_shortTrailingStop = candidate;
			}
			else
			{
				_shortTrailingStop = candidate;
			}
		}
	}

	private decimal? GetLongStopPrice()
	{
		if (_longStopLoss is not decimal stop && _longTrailingStop is not decimal trailing)
			return null;

		if (_longStopLoss is not decimal fixedStop)
			return _longTrailingStop;

		if (_longTrailingStop is not decimal trailStop)
			return _longStopLoss;

		return Math.Max(fixedStop, trailStop);
	}

	private decimal? GetShortStopPrice()
	{
		if (_shortStopLoss is not decimal stop && _shortTrailingStop is not decimal trailing)
			return null;

		if (_shortStopLoss is not decimal fixedStop)
			return _shortTrailingStop;

		if (_shortTrailingStop is not decimal trailStop)
			return _shortStopLoss;

		return Math.Min(fixedStop, trailStop);
	}

	private void ResetProtection()
	{
		ResetLongProtection();
		ResetShortProtection();
	}

	private void ResetLongProtection()
	{
		_longStopLoss = null;
		_longTakeProfit = null;
		_longTrailingStop = null;
	}

	private void ResetShortProtection()
	{
		_shortStopLoss = null;
		_shortTakeProfit = null;
		_shortTrailingStop = null;
	}

	private decimal GetTradeVolume()
	{
		var volume = FixedLot;

		if (UseAutoLot)
		{
			var balance = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
			var autoVolume = (balance / 1000m) * LotsPerThousand;

			if (autoVolume > 0m)
				volume = autoVolume;
		}

		return AlignVolume(volume);
	}

	private decimal PointsToPrice(decimal points)
	{
		if (points <= 0m)
			return 0m;

		var step = Security?.PriceStep;
		var priceStep = step is null or <= 0m ? 1m : step.Value;
		return points * priceStep;
	}

	private decimal AlignVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var minVolume = Security?.MinVolume;
		if (minVolume is decimal min && min > 0m && volume < min)
			volume = min;

		var step = Security?.VolumeStep;
		if (step is decimal s && s > 0m)
		{
			var steps = Math.Max(1m, Math.Floor(volume / s));
			volume = steps * s;
		}

		return volume;
	}
}

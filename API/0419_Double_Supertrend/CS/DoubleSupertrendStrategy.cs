using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Drawing;

using StockSharp.Messages;
using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Indicators;
using StockSharp.BusinessEntities;
using StockSharp.Localization;
using StockSharp.Charting;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Double Supertrend Strategy (using Moving Averages as substitute for SuperTrend)
/// </summary>
public class DoubleSupertrendStrategy : Strategy
{
	private bool _prevDirection1;
	private bool _prevDirection2;

	public DoubleSupertrendStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_atrPeriod1 = Param(nameof(ATRPeriod1), 10)
			.SetGreaterThanZero()
			.SetDisplay("MA1 Period", "First Moving Average period", "Moving Averages");

		_factor1 = Param(nameof(Factor1), 3.0m)
			.SetDisplay("MA1 Factor", "First Moving Average factor", "Moving Averages");

		_atrPeriod2 = Param(nameof(ATRPeriod2), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA2 Period", "Second Moving Average period", "Moving Averages");

		_factor2 = Param(nameof(Factor2), 5.0m)
			.SetDisplay("MA2 Factor", "Second Moving Average factor", "Moving Averages");

		_direction = Param(nameof(Direction), "Long")
			.SetDisplay("Direction", "Trading direction (Long/Short)", "Strategy");

		_takeProfit = Param(nameof(TakeProfit), 1.5m.Percents())
			.SetDisplay("TP", "Take profit", "Take Profit");

		_stopLoss = Param(nameof(StopLoss), 10m.Percents())
			.SetDisplay("SL", "Stop loss", "Stop Loss");
	}

	private readonly StrategyParam<DataType> _candleTypeParam;
	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	private readonly StrategyParam<int> _atrPeriod1;
	public int ATRPeriod1
	{
		get => _atrPeriod1.Value;
		set => _atrPeriod1.Value = value;
	}

	private readonly StrategyParam<decimal> _factor1;
	public decimal Factor1
	{
		get => _factor1.Value;
		set => _factor1.Value = value;
	}

	private readonly StrategyParam<int> _atrPeriod2;
	public int ATRPeriod2
	{
		get => _atrPeriod2.Value;
		set => _atrPeriod2.Value = value;
	}

	private readonly StrategyParam<decimal> _factor2;
	public decimal Factor2
	{
		get => _factor2.Value;
		set => _factor2.Value = value;
	}

	private readonly StrategyParam<string> _direction;
	public string Direction
	{
		get => _direction.Value;
		set => _direction.Value = value;
	}

	private readonly StrategyParam<Unit> _takeProfit;
	public Unit TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	private readonly StrategyParam<Unit> _stopLoss;
	public Unit StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevDirection1 = false;
		_prevDirection2 = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create Moving Average indicators as SuperTrend substitute
		var ma1 = new ExponentialMovingAverage
		{
			Length = ATRPeriod1
		};

		var ma2 = new ExponentialMovingAverage
		{
			Length = ATRPeriod2
		};

		// Create ATR for volatility-based signals
		var atr1 = new AverageTrueRange
		{
			Length = ATRPeriod1
		};

		var atr2 = new AverageTrueRange
		{
			Length = ATRPeriod2
		};

		// Subscribe to candles
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ma1, ma2, atr1, atr2, OnProcess)
			.Start();

		// Configure chart
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma1, System.Drawing.Color.Green);
			DrawIndicator(area, ma2, System.Drawing.Color.Red);
			DrawOwnTrades(area);
		}

		StartProtection(TakeProfit, StopLoss);
	}

	private void OnProcess(ICandleMessage candle, 
		IIndicatorValue ma1Value, IIndicatorValue ma2Value, 
		IIndicatorValue atr1Value, IIndicatorValue atr2Value)
	{
		// Only process finished candles
		if (candle.State != CandleStates.Finished)
			return;

		var closePrice = candle.ClosePrice;
		var ma1Price = ma1Value.ToDecimal();
		var ma2Price = ma2Value.ToDecimal();
		var atr1Val = atr1Value.ToDecimal();
		var atr2Val = atr2Value.ToDecimal();

		// Calculate trend direction based on price vs moving average + ATR
		var upperBand1 = ma1Price + (atr1Val * Factor1);
		var lowerBand1 = ma1Price - (atr1Val * Factor1);
		var upperBand2 = ma2Price + (atr2Val * Factor2);
		var lowerBand2 = ma2Price - (atr2Val * Factor2);

		// Determine trend direction (simulating SuperTrend logic)
		var inLong1 = closePrice > lowerBand1;
		var inLong2 = closePrice > lowerBand2;

		// Check for direction changes
		var exitLong = _prevDirection2 && !inLong2;
		var exitShort = !_prevDirection2 && inLong2;

		var isLongMode = Direction == "Long";
		var isShortMode = Direction == "Short";

		// Entry conditions
		var entryLong = inLong1 && closePrice > upperBand1;
		var entryShort = !inLong1 && closePrice < lowerBand1;

		// Execute trades
		if (isLongMode)
		{
			if (entryLong && Position == 0)
			{
				BuyMarket(Volume);
			}
			else if (exitLong && Position > 0)
			{
				SellMarket(Position);
			}
		}
		else if (isShortMode)
		{
			if (entryShort && Position == 0)
			{
				SellMarket(Volume);
			}
			else if ((exitShort || inLong1) && Position < 0)
			{
				BuyMarket(Position.Abs());
			}
		}

		// Update previous directions
		_prevDirection1 = inLong1;
		_prevDirection2 = inLong2;
	}
}
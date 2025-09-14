using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades breakouts of the previous candle's high or low.
/// The strategy uses a trailing stop and take profit for risk management.
/// </summary>
public class PreviousHighLowBreakoutStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _previousHigh;
	private decimal _previousLow;
	private bool _isFirstCandle = true;

	/// <summary>
	/// Stop loss in price points.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit in price points.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public PreviousHighLowBreakoutStrategy()
	{
		_stopLoss = Param(nameof(StopLoss), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in price points", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(20m, 100m, 10m);

		_takeProfit = Param(nameof(TakeProfit), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in price points", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(100m, 2000m, 100m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Subscribe to candles and process them.
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		// Enable trailing stop and take profit protection.
		StartProtection(
			new Unit(TakeProfit, UnitTypes.Absolute),
			new Unit(StopLoss, UnitTypes.Absolute),
			isStopTrailing: true,
			useMarketOrders: true);

		// Setup chart visualization if available.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Work only with finished candles.
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure strategy is ready for trading.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Store the first candle values and wait for the next one.
		if (_isFirstCandle)
		{
			_previousHigh = candle.HighPrice;
			_previousLow = candle.LowPrice;
			_isFirstCandle = false;
			return;
		}

		var price = candle.ClosePrice;

		// Breakout above previous high.
		if (price > _previousHigh && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		// Breakout below previous low.
		else if (price < _previousLow && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}

		// Update previous candle data for next iteration.
		_previousHigh = candle.HighPrice;
		_previousLow = candle.LowPrice;
	}
}


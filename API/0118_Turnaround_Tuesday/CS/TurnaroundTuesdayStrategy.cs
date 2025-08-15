using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Algo.Indicators;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Implementation of Turnaround Tuesday trading strategy.
/// The strategy enters long position on Tuesday after a price decline on Monday.
/// </summary>
public class TurnaroundTuesdayStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _prevClosePrice;
	private bool _isPriceLowerOnMonday;

	/// <summary>
	/// Stop loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TurnaroundTuesdayStrategy"/>.
	/// </summary>
	public TurnaroundTuesdayStrategy()
	{
		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Protection");
		
		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average period for trend confirmation", "Strategy");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for strategy", "Strategy");
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

		_prevClosePrice = 0;
		_isPriceLowerOnMonday = false;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		// Create a simple moving average indicator
		var sma = new SimpleMovingAverage { Length = MaPeriod };
		
		// Create subscription and bind indicator
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();
		
		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
		
		// Start position protection
		StartProtection(
			takeProfit: new Unit(0), // No take profit
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
		);
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;
		
		// Skip if strategy is not ready
		if (!IsFormedAndOnlineAndAllowTrading())
			return;
		
		var currentDay = candle.OpenTime.DayOfWeek;
		
		// Record Monday's price action
		if (currentDay == DayOfWeek.Monday)
		{
			// Check if Monday's close is lower than the previous candle's close
			_isPriceLowerOnMonday = candle.ClosePrice < _prevClosePrice;
			LogInfo($"Monday candle: Close={candle.ClosePrice}, Prev Close={_prevClosePrice}, Lower={_isPriceLowerOnMonday}");
		}
		// Tuesday - BUY signal if Monday's close was lower
		else if (currentDay == DayOfWeek.Tuesday && _isPriceLowerOnMonday && candle.ClosePrice > maValue && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			
			LogInfo($"Buy signal on Tuesday after Monday decline: Price={candle.ClosePrice}, MA={maValue}, Volume={volume}");
		}
		// Closing conditions - close long position on Friday
		else if (currentDay == DayOfWeek.Friday && Position > 0)
		{
			ClosePosition();
			LogInfo($"Closing position on Friday: Position={Position}");
		}
		
		// Store current close price for next candle
		_prevClosePrice = candle.ClosePrice;
	}
}

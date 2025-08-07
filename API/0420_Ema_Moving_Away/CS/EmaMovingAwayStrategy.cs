namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// EMA Moving Away Strategy
/// </summary>
public class EmaMovingAwayStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _movingAwayPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	
	private ExponentialMovingAverage _ema;
	private decimal _lastCandleBodyPercent;
	private int _last4GreenCount;

	public EmaMovingAwayStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_emaLength = Param(nameof(EmaLength), 55)
			.SetDisplay("EMA Length", "EMA period", "Moving Average");

		_movingAwayPercent = Param(nameof(MovingAwayPercent), 2m)
			.SetDisplay("Moving away (%)", "Required percentage that price moves away from EMA", "Strategy");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetDisplay("Stop Loss (%)", "Stop loss percentage", "Stop Loss");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	public decimal MovingAwayPercent
	{
		get => _movingAwayPercent.Value;
		set => _movingAwayPercent.Value = value;
	}

	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_lastCandleBodyPercent = default;
		_last4GreenCount = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize EMA indicator
		_ema = new ExponentialMovingAverage
		{
			Length = EmaLength
		};

		// Subscribe to candles using high-level API
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, OnProcess)
			.Start();

		// Setup chart
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema, System.Drawing.Color.Orange);
			DrawOwnTrades(area);
		}

		// Enable protection with stop loss
		StartProtection(new(), new Unit(StopLossPercent, UnitTypes.Percent));
	}

	private void OnProcess(ICandleMessage candle, decimal emaValue)
	{
		// Process only finished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Wait for indicator to form
		if (!_ema.IsFormed)
			return;

		// Calculate last candle body percentage
		_lastCandleBodyPercent = Math.Abs((candle.ClosePrice - candle.OpenPrice) / candle.OpenPrice * 100);

		// Track last 4 green candles
		UpdateLast4GreenCount(candle);

		// Calculate entry zones
		var longEntryLevel = emaValue * (1 - MovingAwayPercent / 100);
		var shortEntryLevel = emaValue * (1 + MovingAwayPercent / 100);

		// Check entry conditions
		var longEntryCondition = candle.ClosePrice <= longEntryLevel && 
								  _lastCandleBodyPercent < StopLossPercent;

		var shortEntryCondition = candle.ClosePrice >= shortEntryLevel && 
								   _lastCandleBodyPercent < StopLossPercent && 
								   _last4GreenCount != 4;

		// Exit conditions
		var exitLong = Position > 0 && candle.ClosePrice >= emaValue;
		var exitShort = Position < 0 && candle.ClosePrice <= emaValue;

		// Execute trades
		if (exitLong)
		{
			ClosePosition();
		}
		else if (exitShort)
		{
			ClosePosition();
		}
		else if (longEntryCondition && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (shortEntryCondition && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
	}

	private void UpdateLast4GreenCount(ICandleMessage candle)
	{
		// Simple tracking: increase counter if green candle
		if (candle.ClosePrice > candle.OpenPrice)
		{
			_last4GreenCount = Math.Min(_last4GreenCount + 1, 4);
		}
		else
		{
			_last4GreenCount = 0;
		}
	}
}
namespace StockSharp.Samples.Strategies;

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

/// <summary>
/// Reimplementation of the MetaTrader "CrossoverMA" expert advisor.
/// Buys when the candle crosses above the moving average while the average slopes upward.
/// Sells when the candle crosses below the moving average while the average slopes downward.
/// </summary>
public class CrossoverMaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _movingAverageLength;
	private readonly StrategyParam<decimal> _tradeVolume;

	private SimpleMovingAverage _movingAverage;
	private decimal? _previousAverageValue;

	public CrossoverMaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame that feeds the moving average.", "General");

		_movingAverageLength = Param(nameof(MovingAverageLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Number of completed candles used by the moving average.", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(5, 40, 5);

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Default order size for market entries.", "Trading");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int MovingAverageLength
	{
		get => _movingAverageLength.Value;
		set => _movingAverageLength.Value = value;
	}

	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_movingAverage = null;
		_previousAverageValue = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_movingAverage = new SimpleMovingAverage { Length = MovingAverageLength };
		_previousAverageValue = null;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_movingAverage, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			if (_movingAverage != null)
			{
				DrawIndicator(area, _movingAverage);
			}
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal averageValue)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		if (_movingAverage == null || !_movingAverage.IsFormed)
		{
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			return;
		}

		var previousAverageValue = _previousAverageValue;
		_previousAverageValue = averageValue;

		if (previousAverageValue is null)
		{
			return; // Need at least two completed averages to evaluate the slope.
		}

		var openOffset = candle.OpenPrice - averageValue;
		var closeOffset = candle.ClosePrice - averageValue;
		var slope = averageValue - previousAverageValue.Value;

		var bullishCross = openOffset < 0m && closeOffset > 0m && slope > 0m;
		var bearishCross = openOffset > 0m && closeOffset < 0m && slope < 0m;

		if (!bullishCross && !bearishCross)
		{
			return;
		}

		var tradeVolume = TradeVolume;
		if (tradeVolume <= 0m)
		{
			return;
		}

		if (bullishCross && Position <= 0m)
		{
			if (Position < 0m)
			{
				BuyMarket(Math.Abs(Position)); // Close existing short exposure before reversing.
			}

			BuyMarket(tradeVolume); // Enter or extend the long position.
		}
		else if (bearishCross && Position >= 0m)
		{
			if (Position > 0m)
			{
				SellMarket(Position); // Close existing long exposure before reversing.
			}

			SellMarket(tradeVolume); // Enter or extend the short position.
		}
	}
}


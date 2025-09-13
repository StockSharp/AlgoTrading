using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Liquidex V1 strategy based on weighted moving average and range filter.
/// Enters when price crosses the WMA after wide candles and uses stop loss for protection.
/// </summary>
public class LiquidexV1Strategy : Strategy
{
	private readonly StrategyParam<decimal> _rangeFilter;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<Unit> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Minimum candle range to allow trading.
	/// </summary>
	public decimal RangeFilter
	{
		get => _rangeFilter.Value;
		set => _rangeFilter.Value = value;
	}

	/// <summary>
	/// Weighted moving average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss value.
	/// </summary>
	public Unit StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="LiquidexV1Strategy"/>.
	/// </summary>
	public LiquidexV1Strategy()
	{
		_rangeFilter = Param(nameof(RangeFilter), 20m)
			.SetDisplay("Range Filter", "Minimum candle range to enable trading", "General")
			.SetGreaterThanZero();

		_maPeriod = Param(nameof(MaPeriod), 3)
			.SetDisplay("MA Period", "Period of weighted moving average", "Indicators")
			.SetGreaterThanZero();

		_stopLoss = Param(nameof(StopLoss), new Unit(30, UnitTypes.Point))
			.SetDisplay("Stop Loss", "Stop loss size in points", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");
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

		StartProtection(new(), StopLoss);

		var wma = new WeightedMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(wma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, wma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal wmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var range = candle.HighPrice - candle.LowPrice;

		if (range < RangeFilter)
			return;

		var crossAbove = candle.OpenPrice < wmaValue && candle.ClosePrice > wmaValue;
		var crossBelow = candle.OpenPrice > wmaValue && candle.ClosePrice < wmaValue;

		if (crossAbove && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			LogInfo($"Buy signal: price crossed above WMA at {wmaValue}");
		}
		else if (crossBelow && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			LogInfo($"Sell signal: price crossed below WMA at {wmaValue}");
		}
	}
}

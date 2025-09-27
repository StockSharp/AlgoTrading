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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple pivot-based strategy converted from the MetaTrader expert advisor in MQL/7610.
/// The strategy compares the current candle open with the previous candle range to decide
/// whether the next trade should be long or short.
/// </summary>
public class SimplePivotStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _previousHigh;
	private decimal _previousLow;
	private bool _hasPreviousCandle;

	/// <summary>
	/// Order volume used for market entries.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Candle type used for price subscription.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SimplePivotStrategy"/> class.
	/// </summary>
	public SimplePivotStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Market order volume used for entries.", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for pivot calculation.", "Data");
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

		_previousHigh = 0m;
		_previousLow = 0m;
		_hasPreviousCandle = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_hasPreviousCandle)
		{
			// Store the first completed candle to build the reference range.
			_previousHigh = candle.HighPrice;
			_previousLow = candle.LowPrice;
			_hasPreviousCandle = true;
			return;
		}

		// Calculate the pivot as the midpoint of the previous candle range.
		var pivot = (_previousHigh + _previousLow) / 2m;
		var desiredSide = Sides.Buy;

		// If the new candle opens inside the upper half of the previous range we go short.
		if (candle.OpenPrice < _previousHigh && candle.OpenPrice > pivot)
			desiredSide = Sides.Sell;

		// Skip re-entry if we already hold a position in the desired direction.
		if ((desiredSide == Sides.Buy && Position > 0) || (desiredSide == Sides.Sell && Position < 0))
		{
			_previousHigh = candle.HighPrice;
			_previousLow = candle.LowPrice;
			return;
		}

		if (Position != 0)
		{
			// Close the current exposure before opening a new trade.
			ClosePosition();
		}

		if (desiredSide == Sides.Buy)
		{
			BuyMarket(OrderVolume);
		}
		else
		{
			SellMarket(OrderVolume);
		}

		// Update reference range for the next candle.
		_previousHigh = candle.HighPrice;
		_previousLow = candle.LowPrice;
	}
}


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
/// Strategy based on comparing current price with prices from short and long lookback periods.
/// Buys when the short-term change crosses above the long-term change and sells on the opposite cross.
/// </summary>
public class ExchangePriceStrategy : Strategy
{
	private readonly StrategyParam<int> _shortPeriod;
	private readonly StrategyParam<int> _longPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _prices = new();
	private decimal? _prevUpDiff;
	private decimal? _prevDownDiff;

	public int ShortPeriod { get => _shortPeriod.Value; set => _shortPeriod.Value = value; }
	public int LongPeriod { get => _longPeriod.Value; set => _longPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ExchangePriceStrategy()
	{
		_shortPeriod = Param(nameof(ShortPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("Short Period", "Bars for short lookback", "General");

		_longPeriod = Param(nameof(LongPeriod), 48)
			.SetGreaterThanZero()
			.SetDisplay("Long Period", "Bars for long lookback", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_prices.Clear();
		_prevUpDiff = null;
		_prevDownDiff = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

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

		_prices.Add(candle.ClosePrice);

		if (_prices.Count > LongPeriod + 1)
			_prices.RemoveAt(0);

		if (_prices.Count <= LongPeriod)
			return;

		var priceShort = _prices[_prices.Count - 1 - ShortPeriod];
		var priceLong = _prices[_prices.Count - 1 - LongPeriod];

		var upDiff = candle.ClosePrice - priceShort;
		var downDiff = candle.ClosePrice - priceLong;

		if (_prevUpDiff is decimal prevUp && _prevDownDiff is decimal prevDown)
		{
			var crossUp = prevUp <= prevDown && upDiff > downDiff;
			var crossDown = prevUp >= prevDown && upDiff < downDiff;

			if (crossUp && Position <= 0)
			{
				if (Position < 0)
					BuyMarket();
				BuyMarket();
			}
			else if (crossDown && Position >= 0)
			{
				if (Position > 0)
					SellMarket();
				SellMarket();
			}
		}

		_prevUpDiff = upDiff;
		_prevDownDiff = downDiff;
	}
}

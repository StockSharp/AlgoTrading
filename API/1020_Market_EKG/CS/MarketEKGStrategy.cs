using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Market EKG strategy.
/// Compares previous period OHLC with averages of earlier two periods.
/// Buys when the average close of periods 2 and 3 is above period 1 close and sells when below.
/// </summary>
public class MarketEKGStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage _prev1;
	private ICandleMessage _prev2;
	private ICandleMessage _prev3;

	/// <summary>
	/// The type of candles to use for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="MarketEKGStrategy"/>.
	/// </summary>
	public MarketEKGStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		_prev1 = null;
		_prev2 = null;
		_prev3 = null;
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

		if (_prev1 != null && _prev2 != null && _prev3 != null)
		{
			var avgClose = (_prev3.ClosePrice + _prev2.ClosePrice) / 2m;
			var diffClose = avgClose - _prev1.ClosePrice;

			if (diffClose > 0 && Position <= 0)
				BuyMarket();
			else if (diffClose < 0 && Position >= 0)
				SellMarket();
		}

		_prev3 = _prev2;
		_prev2 = _prev1;
		_prev1 = candle;
	}
}

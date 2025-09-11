using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades new intraday highs that close near the low.
/// </summary>
public class NewIntradayHighWithWeakBarStrategy : Strategy
{
	private readonly StrategyParam<int> _highestLength;
	private readonly StrategyParam<decimal> _weakRatio;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest = null!;
	private decimal _prevHigh;

	public int HighestLength
	{
		get => _highestLength.Value;
		set => _highestLength.Value = value;
	}

	public decimal WeakRatio
	{
		get => _weakRatio.Value;
		set => _weakRatio.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public NewIntradayHighWithWeakBarStrategy()
	{
		_highestLength = Param(nameof(HighestLength), 10)
			.SetDisplay("Highest Length", "Bars to look back for high", "General")
			.SetGreaterThanZero()
			.SetCanOptimize();

		_weakRatio = Param(nameof(WeakRatio), 0.15m)
			.SetDisplay("Weak Bar Ratio", "Close-low to range ratio", "General")
			.SetGreaterThanZero()
			.SetCanOptimize();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Working candle timeframe", "General");
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
		_prevHigh = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest { Length = HighestLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_highest, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _highest, "Highest High");
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var close = candle.ClosePrice;

		if (!_highest.IsFormed)
		{
			_prevHigh = high;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevHigh = high;
			return;
		}

		var range = high - low;
		if (range <= 0)
		{
			_prevHigh = high;
			return;
		}

		var ratio = (close - low) / range;

		if (Position == 0 && high == highestValue && ratio < WeakRatio)
		{
			BuyMarket();
		}
		else if (Position > 0 && close > _prevHigh)
		{
			SellMarket();
		}

		_prevHigh = high;
	}
}


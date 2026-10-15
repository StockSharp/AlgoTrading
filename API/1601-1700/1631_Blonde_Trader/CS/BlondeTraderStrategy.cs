using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Blonde Trader strategy. Buys when price pulls back from recent high,
/// sells when price bounces from recent low. Uses Highest/Lowest indicators.
/// </summary>
public class BlondeTraderStrategy : Strategy
{
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;

	public int Lookback { get => _lookback.Value; set => _lookback.Value = value; }
	public decimal Threshold { get => _threshold.Value; set => _threshold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BlondeTraderStrategy()
	{
		_lookback = Param(nameof(Lookback), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Period for Highest/Lowest", "General");

		_threshold = Param(nameof(Threshold), 0.002m)
			.SetDisplay("Threshold", "Min distance ratio from extreme", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var highest = new Highest { Length = Lookback };
		var lowest = new Lowest { Length = Lookback };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal high, decimal low)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;
		var range = high - low;

		if (range <= 0 || high == 0)
			return;

		var distFromHigh = (high - price) / high;
		var distFromLow = (price - low) / price;

		// Buy signal: price pulled back from high by at least threshold
		if (distFromHigh > Threshold && Position <= 0)
		{
			BuyMarket();
			_entryPrice = price;
		}
		// Sell signal: price bounced up from low by at least threshold
		else if (distFromLow > Threshold && Position >= 0)
		{
			SellMarket();
			_entryPrice = price;
		}
	}
}

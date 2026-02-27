namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// ABH BH MFI strategy: Harami pattern with MFI confirmation.
/// </summary>
public class AbhBhMfiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _mfiPeriod;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<decimal> _overbought;

	private readonly List<ICandleMessage> _candles = new();

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int MfiPeriod { get => _mfiPeriod.Value; set => _mfiPeriod.Value = value; }
	public decimal Oversold { get => _oversold.Value; set => _oversold.Value = value; }
	public decimal Overbought { get => _overbought.Value; set => _overbought.Value = value; }

	public AbhBhMfiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_mfiPeriod = Param(nameof(MfiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("MFI Period", "Money Flow Index period", "Indicators");
		_oversold = Param(nameof(Oversold), 40m)
			.SetDisplay("Oversold", "MFI oversold level", "Signals");
		_overbought = Param(nameof(Overbought), 60m)
			.SetDisplay("Overbought", "MFI overbought level", "Signals");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_candles.Clear();
		var mfi = new MoneyFlowIndex { Length = MfiPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(mfi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal mfiValue)
	{
		if (candle.State != CandleStates.Finished) return;

		_candles.Add(candle);
		if (_candles.Count > 5) _candles.RemoveAt(0);

		if (_candles.Count >= 2)
		{
			var curr = _candles[^1];
			var prev = _candles[^2];

			var bullishHarami = prev.OpenPrice > prev.ClosePrice
				&& curr.ClosePrice > curr.OpenPrice
				&& curr.OpenPrice > prev.ClosePrice
				&& curr.ClosePrice < prev.OpenPrice;

			var bearishHarami = prev.ClosePrice > prev.OpenPrice
				&& curr.OpenPrice > curr.ClosePrice
				&& curr.ClosePrice > prev.OpenPrice
				&& curr.OpenPrice < prev.ClosePrice;

			if (bullishHarami && mfiValue < Oversold && Position <= 0)
				BuyMarket();
			else if (bearishHarami && mfiValue > Overbought && Position >= 0)
				SellMarket();
		}
	}
}

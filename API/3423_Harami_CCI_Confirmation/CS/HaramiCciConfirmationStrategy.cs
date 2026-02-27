namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Harami CCI Confirmation strategy: Harami pattern with CCI confirmation.
/// </summary>
public class HaramiCciConfirmationStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _entryLevel;

	private readonly List<ICandleMessage> _candles = new();

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }
	public decimal EntryLevel { get => _entryLevel.Value; set => _entryLevel.Value = value; }

	public HaramiCciConfirmationStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "CCI period", "Indicators");
		_entryLevel = Param(nameof(EntryLevel), 0m)
			.SetDisplay("Entry Level", "CCI threshold", "Signals");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_candles.Clear();
		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(cci, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
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

			if (bullishHarami && cciValue < -EntryLevel && Position <= 0)
				BuyMarket();
			else if (bearishHarami && cciValue > EntryLevel && Position >= 0)
				SellMarket();
		}
	}
}

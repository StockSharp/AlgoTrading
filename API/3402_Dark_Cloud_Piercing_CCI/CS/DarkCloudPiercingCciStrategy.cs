namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Dark Cloud Piercing CCI strategy: trades Dark Cloud Cover and Piercing Line
/// candlestick patterns confirmed by CCI indicator levels.
/// </summary>
public class DarkCloudPiercingCciStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _entryLevel;

	private readonly List<ICandleMessage> _candles = new();
	private decimal _prevCci;
	private bool _hasPrevCci;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }
	public decimal EntryLevel { get => _entryLevel.Value; set => _entryLevel.Value = value; }

	public DarkCloudPiercingCciStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "CCI period", "Indicators");
		_entryLevel = Param(nameof(EntryLevel), 50m)
			.SetDisplay("Entry Level", "CCI level for confirmation", "Signals");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_candles.Clear();
		_hasPrevCci = false;
		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(cci, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished) return;

		_candles.Add(candle);
		if (_candles.Count > 5)
			_candles.RemoveAt(0);

		if (_candles.Count >= 2 && _hasPrevCci)
		{
			var curr = _candles[^1];
			var prev = _candles[^2];

			// Piercing Line: prev bearish, curr bullish, curr opens below prev low, closes above midpoint
			var isPiercing = prev.OpenPrice > prev.ClosePrice
				&& curr.ClosePrice > curr.OpenPrice
				&& curr.OpenPrice < prev.LowPrice
				&& curr.ClosePrice > (prev.OpenPrice + prev.ClosePrice) / 2m;

			// Dark Cloud Cover: prev bullish, curr bearish, curr opens above prev high, closes below midpoint
			var isDarkCloud = prev.ClosePrice > prev.OpenPrice
				&& curr.OpenPrice > curr.ClosePrice
				&& curr.OpenPrice > prev.HighPrice
				&& curr.ClosePrice < (prev.OpenPrice + prev.ClosePrice) / 2m;

			if (isPiercing && cciValue < -EntryLevel && Position <= 0)
				BuyMarket();
			else if (isDarkCloud && cciValue > EntryLevel && Position >= 0)
				SellMarket();

			// Exit on CCI crossing back
			if (Position > 0 && _prevCci > EntryLevel && cciValue < EntryLevel)
				SellMarket();
			else if (Position < 0 && _prevCci < -EntryLevel && cciValue > -EntryLevel)
				BuyMarket();
		}

		_prevCci = cciValue;
		_hasPrevCci = true;
	}
}

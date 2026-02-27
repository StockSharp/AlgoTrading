namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// CDC PL RSI strategy: Dark Cloud Cover and Piercing Line candlestick patterns
/// confirmed by RSI levels.
/// </summary>
public class CdcPlRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _overboughtLevel;

	private readonly List<ICandleMessage> _candles = new();
	private decimal _prevRsi;
	private bool _hasPrevRsi;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public decimal OversoldLevel { get => _oversoldLevel.Value; set => _oversoldLevel.Value = value; }
	public decimal OverboughtLevel { get => _overboughtLevel.Value; set => _overboughtLevel.Value = value; }

	public CdcPlRsiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");
		_oversoldLevel = Param(nameof(OversoldLevel), 40m)
			.SetDisplay("Oversold Level", "RSI below this for long entry", "Signals");
		_overboughtLevel = Param(nameof(OverboughtLevel), 60m)
			.SetDisplay("Overbought Level", "RSI above this for short entry", "Signals");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_candles.Clear();
		_hasPrevRsi = false;
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished) return;

		_candles.Add(candle);
		if (_candles.Count > 5)
			_candles.RemoveAt(0);

		if (_candles.Count >= 2 && _hasPrevRsi)
		{
			var curr = _candles[^1];
			var prev = _candles[^2];

			// Piercing Line
			var isPiercing = prev.OpenPrice > prev.ClosePrice
				&& curr.ClosePrice > curr.OpenPrice
				&& curr.OpenPrice < prev.LowPrice
				&& curr.ClosePrice > (prev.OpenPrice + prev.ClosePrice) / 2m;

			// Dark Cloud Cover
			var isDarkCloud = prev.ClosePrice > prev.OpenPrice
				&& curr.OpenPrice > curr.ClosePrice
				&& curr.OpenPrice > prev.HighPrice
				&& curr.ClosePrice < (prev.OpenPrice + prev.ClosePrice) / 2m;

			if (isPiercing && rsiValue < OversoldLevel && Position <= 0)
				BuyMarket();
			else if (isDarkCloud && rsiValue > OverboughtLevel && Position >= 0)
				SellMarket();

			// Exit on RSI crossing
			if (Position > 0 && _prevRsi >= OverboughtLevel && rsiValue < OverboughtLevel)
				SellMarket();
			else if (Position < 0 && _prevRsi <= OversoldLevel && rsiValue > OversoldLevel)
				BuyMarket();
		}

		_prevRsi = rsiValue;
		_hasPrevRsi = true;
	}
}

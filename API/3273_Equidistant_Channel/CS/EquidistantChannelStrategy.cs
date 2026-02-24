using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Equidistant Channel strategy: Bollinger Band mean reversion.
/// Buys when close crosses below lower band then returns inside.
/// Sells when close crosses above upper band then returns inside.
/// </summary>
public class EquidistantChannelStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bbPeriod;
	private readonly StrategyParam<decimal> _bbWidth;

	private decimal? _prevClose;
	private decimal? _prevUpper;
	private decimal? _prevLower;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int BbPeriod
	{
		get => _bbPeriod.Value;
		set => _bbPeriod.Value = value;
	}

	public decimal BbWidth
	{
		get => _bbWidth.Value;
		set => _bbWidth.Value = value;
	}

	public EquidistantChannelStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_bbPeriod = Param(nameof(BbPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Band period", "Indicators");

		_bbWidth = Param(nameof(BbWidth), 2m)
			.SetGreaterThanZero()
			.SetDisplay("BB Width", "Bollinger Band deviation", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = null;
		_prevUpper = null;
		_prevLower = null;

		var bb = new BollingerBands { Length = BbPeriod, Width = BbWidth };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bb, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bb);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var bbTyped = (BollingerBandsValue)bbValue;
		if (bbTyped.UpBand is not decimal upper || bbTyped.LowBand is not decimal lower)
			return;

		var close = candle.ClosePrice;
		var mid = (upper + lower) / 2m;

		if (_prevClose.HasValue && _prevUpper.HasValue && _prevLower.HasValue)
		{
			// Buy: previously below lower band, now back inside
			if (_prevClose.Value <= _prevLower.Value && close > lower && Position <= 0)
			{
				BuyMarket();
			}
			// Sell: previously above upper band, now back inside
			else if (_prevClose.Value >= _prevUpper.Value && close < upper && Position >= 0)
			{
				SellMarket();
			}
		}

		_prevClose = close;
		_prevUpper = upper;
		_prevLower = lower;
	}
}

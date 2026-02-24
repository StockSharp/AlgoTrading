using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Explosion strategy: Bollinger Band squeeze breakout.
/// Detects when BB narrows (squeeze) and trades the breakout direction.
/// </summary>
public class ExplosionStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bbPeriod;
	private readonly StrategyParam<decimal> _bbWidth;
	private readonly StrategyParam<int> _smaPeriod;

	private decimal? _prevBandWidth;

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

	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	public ExplosionStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_bbPeriod = Param(nameof(BbPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Band period", "Indicators");

		_bbWidth = Param(nameof(BbWidth), 2m)
			.SetGreaterThanZero()
			.SetDisplay("BB Width", "Bollinger Band deviation", "Indicators");

		_smaPeriod = Param(nameof(SmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "Trend SMA period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevBandWidth = null;

		var bb = new BollingerBands { Length = BbPeriod, Width = BbWidth };
		var sma = new SimpleMovingAverage { Length = SmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bb, sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bb);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbValue, IIndicatorValue smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var bbTyped = (BollingerBandsValue)bbValue;
		if (bbTyped.UpBand is not decimal upper || bbTyped.LowBand is not decimal lower)
			return;

		var sma = smaValue.ToDecimal();
		var close = candle.ClosePrice;
		var mid = (upper + lower) / 2m;
		var bandWidth = upper - lower;

		if (_prevBandWidth.HasValue && mid > 0)
		{
			// Squeeze expanding: band width increasing
			var expanding = bandWidth > _prevBandWidth.Value;

			if (expanding)
			{
				// Buy: breakout above upper band with uptrend
				if (close > upper && close > sma && Position <= 0)
				{
					BuyMarket();
				}
				// Sell: breakout below lower band with downtrend
				else if (close < lower && close < sma && Position >= 0)
				{
					SellMarket();
				}
			}
		}

		_prevBandWidth = bandWidth;
	}
}

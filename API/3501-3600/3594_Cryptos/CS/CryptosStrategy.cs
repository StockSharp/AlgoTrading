using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified from "Cryptos" MetaTrader expert.
/// Uses Bollinger Bands with a WMA trend filter. Buys when price is below WMA
/// and touches lower band; sells when price is above WMA and touches upper band.
/// </summary>
public class CryptosStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _wmaPeriod;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerWidth;

	private ExponentialMovingAverage _bandEma;
	private ExponentialMovingAverage _trendEma;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int WmaPeriod
	{
		get => _wmaPeriod.Value;
		set => _wmaPeriod.Value = value;
	}

	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	public decimal BollingerWidth
	{
		get => _bollingerWidth.Value;
		set => _bollingerWidth.Value = value;
	}

	public CryptosStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for signals", "General");

		_wmaPeriod = Param(nameof(WmaPeriod), 55)
			.SetGreaterThanZero()
			.SetDisplay("WMA Period", "Weighted moving average period", "Indicators");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Bands period", "Indicators");

		_bollingerWidth = Param(nameof(BollingerWidth), 2m)
			.SetGreaterThanZero()
			.SetDisplay("BB Width", "Bollinger Bands deviation", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_bandEma = new ExponentialMovingAverage { Length = BollingerPeriod };
		_trendEma = new ExponentialMovingAverage { Length = WmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_bandEma, _trendEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bandEma);
			DrawIndicator(area, _trendEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal bandValue, decimal trendValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_bandEma.IsFormed || !_trendEma.IsFormed)
			return;

		var close = candle.ClosePrice;
		var bandOffset = bandValue * (BollingerWidth / 100m);
		var upperBand = bandValue + bandOffset;
		var lowerBand = bandValue - bandOffset;

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		// Buy: price below WMA, touches lower band
		if (close < trendValue && close <= lowerBand)
		{
			if (Position <= 0)
				BuyMarket(Position < 0 ? Math.Abs(Position) + volume : volume);
		}
		// Sell: price above WMA, touches upper band
		else if (close > trendValue && close >= upperBand)
		{
			if (Position >= 0)
				SellMarket(Position > 0 ? Math.Abs(Position) + volume : volume);
		}

		// Exit at WMA cross
		if (Position > 0 && close >= upperBand)
			SellMarket(Position);
		else if (Position < 0 && close <= lowerBand)
			BuyMarket(Math.Abs(Position));
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		_bandEma = null;
		_trendEma = null;

		base.OnReseted();
	}
}

namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Alli Heik strategy.
/// Uses Heikin Ashi candle patterns with EMA filter for trend following.
/// Buys on bullish HA candles when above EMA, sells on bearish HA candles when below EMA.
/// </summary>
public class AlliHeikStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;

	private decimal _prevHaOpen;
	private decimal _prevHaClose;
	private bool _initialized;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	public AlliHeikStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Source candles for the strategy", "General");

		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Trend filter EMA period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevHaOpen = 0;
		_prevHaClose = 0;
		_initialized = false;

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Calculate Heikin Ashi values
		decimal haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		decimal haOpen;

		if (!_initialized)
		{
			haOpen = (candle.OpenPrice + candle.ClosePrice) / 2m;
			_initialized = true;
		}
		else
		{
			haOpen = (_prevHaOpen + _prevHaClose) / 2m;
		}

		var haBullish = haClose > haOpen;
		var haBearish = haClose < haOpen;

		// Buy on bullish HA candle above EMA
		if (haBullish && candle.ClosePrice > emaValue && Position <= 0)
		{
			BuyMarket();
		}
		// Sell on bearish HA candle below EMA
		else if (haBearish && candle.ClosePrice < emaValue && Position >= 0)
		{
			SellMarket();
		}

		_prevHaOpen = haOpen;
		_prevHaClose = haClose;
	}
}

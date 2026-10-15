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
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Source candles for the strategy", "General");

		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Trend filter EMA period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevHaOpen = 0m;
		_prevHaClose = 0m;
		_initialized = false;
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
		var prevHaBullish = _prevHaClose > _prevHaOpen;
		var prevHaBearish = _prevHaClose < _prevHaOpen;

		// Buy only on a bearish-to-bullish HA flip confirmed by the EMA filter.
		if (haBullish && prevHaBearish && candle.ClosePrice > emaValue && Position <= 0)
		{
			BuyMarket();
		}
		// Sell only on a bullish-to-bearish HA flip confirmed by the EMA filter.
		else if (haBearish && prevHaBullish && candle.ClosePrice < emaValue && Position >= 0)
		{
			SellMarket();
		}

		_prevHaOpen = haOpen;
		_prevHaClose = haClose;
	}
}

namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Average Change Candle strategy (simplified).
/// Compares smoothed open vs close ratios to detect bullish/bearish candle patterns.
/// Uses EMA of open and close to determine trend direction.
/// </summary>
public class AverageChangeCandleStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;

	private decimal _prevSmoothedOpen;
	private decimal _prevSmoothedClose;
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

	public AverageChangeCandleStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Source candles", "General");

		_emaPeriod = Param(nameof(EmaPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA smoothing period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevSmoothedOpen = 0;
		_prevSmoothedClose = 0;
		_initialized = false;

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, (ICandleMessage candle, decimal emaValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				// Simple exponential smoothing of open and close
				var alpha = 2m / (EmaPeriod + 1m);

				if (!_initialized)
				{
					_prevSmoothedOpen = candle.OpenPrice;
					_prevSmoothedClose = candle.ClosePrice;
					_initialized = true;
					return;
				}

				var smoothedOpen = alpha * candle.OpenPrice + (1m - alpha) * _prevSmoothedOpen;
				var smoothedClose = alpha * candle.ClosePrice + (1m - alpha) * _prevSmoothedClose;

				var prevBullish = _prevSmoothedClose > _prevSmoothedOpen;
				var currBullish = smoothedClose > smoothedOpen;

				if (!IsFormedAndOnlineAndAllowTrading())
				{
					_prevSmoothedOpen = smoothedOpen;
					_prevSmoothedClose = smoothedClose;
					return;
				}

				// Buy on transition to bullish smoothed candle
				if (currBullish && !prevBullish && Position <= 0)
				{
					BuyMarket();
				}
				// Sell on transition to bearish smoothed candle
				else if (!currBullish && prevBullish && Position >= 0)
				{
					SellMarket();
				}

				_prevSmoothedOpen = smoothedOpen;
				_prevSmoothedClose = smoothedClose;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}
}

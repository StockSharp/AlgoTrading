using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Heikin-Ashi strategy with EMA trend filter.
/// </summary>
public class OptimizedHeikinAshiBuySellStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;

	private decimal _prevHaOpen;
	private decimal _prevHaClose;
	private bool _haInit;
	private bool _prevBullish;
	private bool _prevBearish;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }

	public OptimizedHeikinAshiBuySellStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
		_emaLength = Param(nameof(EmaLength), 50).SetGreaterThanZero();
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevHaOpen = 0;
		_prevHaClose = 0;
		_haInit = false;
		_prevBullish = false;
		_prevBearish = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevHaOpen = 0;
		_prevHaClose = 0;
		_haInit = false;
		_prevBullish = false;
		_prevBearish = false;

		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var lastSignal = DateTimeOffset.MinValue;
		var cooldown = TimeSpan.FromMinutes(600);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, (candle, emaVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!ema.IsFormed)
					return;

				decimal haOpen;
				decimal haClose;

				if (!_haInit)
				{
					haOpen = (candle.OpenPrice + candle.ClosePrice) / 2m;
					haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
					_prevHaOpen = haOpen;
					_prevHaClose = haClose;
					_haInit = true;
					_prevBullish = haClose > haOpen;
					_prevBearish = haClose < haOpen;
					return;
				}

				haOpen = (_prevHaOpen + _prevHaClose) / 2m;
				haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;

				var isBullish = haClose > haOpen;
				var isBearish = haClose < haOpen;

				if (candle.OpenTime - lastSignal >= cooldown)
				{
					// Transition from bearish to bullish + above EMA
					if (!_prevBullish && isBullish && candle.ClosePrice > emaVal && Position <= 0)
					{
						BuyMarket();
						lastSignal = candle.OpenTime;
					}
					// Transition from bullish to bearish + below EMA
					else if (!_prevBearish && isBearish && candle.ClosePrice < emaVal && Position >= 0)
					{
						SellMarket();
						lastSignal = candle.OpenTime;
					}
				}

				_prevHaOpen = haOpen;
				_prevHaClose = haClose;
				_prevBullish = isBullish;
				_prevBearish = isBearish;
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

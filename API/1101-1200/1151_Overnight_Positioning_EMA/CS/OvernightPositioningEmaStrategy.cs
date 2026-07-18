using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class OvernightPositioningEmaStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLen;
	private readonly StrategyParam<DataType> _candle;

	private DateTime _currentDay;
	private bool _tradeTakenToday;

	public int EmaLength { get => _emaLen.Value; set => _emaLen.Value = value; }
	public DataType CandleType { get => _candle.Value; set => _candle.Value = value; }

	public OvernightPositioningEmaStrategy()
	{
		_emaLen = Param(nameof(EmaLength), 100).SetGreaterThanZero();
		_candle = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_currentDay = default;
		_tradeTakenToday = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_currentDay = default;
		_tradeTakenToday = false;

		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var sub = SubscribeCandles(CandleType);
		sub.Bind(ema, (candle, emaVal) =>
		{
			if (candle.State != CandleStates.Finished)
				return;

			if (!ema.IsFormed)
				return;

			var day = candle.OpenTime.Date;
			if (_currentDay != day)
			{
				_currentDay = day;
				_tradeTakenToday = false;
			}

			if (_tradeTakenToday)
				return;

			var hour = candle.OpenTime.Hour;

			// Buy near end of day if above EMA
			if (hour >= 15 && hour < 16 && candle.ClosePrice > emaVal && Position <= 0)
			{
				BuyMarket();
				_tradeTakenToday = true;
			}
			// Sell near start of day
			else if (hour >= 9 && hour < 10 && Position > 0)
			{
				SellMarket();
				_tradeTakenToday = true;
			}
		}).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}
}

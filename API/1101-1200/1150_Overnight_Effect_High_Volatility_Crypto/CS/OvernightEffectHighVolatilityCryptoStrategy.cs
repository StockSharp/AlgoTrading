using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class OvernightEffectHighVolatilityCryptoStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private DateTime _currentDay;
	private bool _tradeTakenToday;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public OvernightEffectHighVolatilityCryptoStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
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

		var sma = new SimpleMovingAverage { Length = 20 };
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, (candle, smaVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!sma.IsFormed)
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

				// Buy at 20:00, only once per day
				if (hour == 20 && Position <= 0 && candle.ClosePrice > smaVal)
				{
					BuyMarket();
					_tradeTakenToday = true;
				}
				// Sell at 8:00, only once per day
				else if (hour == 8 && Position >= 0 && candle.ClosePrice < smaVal)
				{
					SellMarket();
					_tradeTakenToday = true;
				}
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}
}

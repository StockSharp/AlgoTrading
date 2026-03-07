using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class OutsideBarStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public OutsideBarStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevHigh = 0;
		_prevLow = 0;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevHigh = 0;
		_prevLow = 0;
		_hasPrev = false;

		var sma = new SimpleMovingAverage { Length = 20 };
		var lastSignal = DateTimeOffset.MinValue;
		var cooldown = TimeSpan.FromMinutes(360);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, (candle, smaVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!sma.IsFormed)
					return;

				if (!_hasPrev)
				{
					_prevHigh = candle.HighPrice;
					_prevLow = candle.LowPrice;
					_hasPrev = true;
					return;
				}

				var isOutsideBar = candle.HighPrice > _prevHigh && candle.LowPrice < _prevLow;

				if (isOutsideBar && candle.OpenTime - lastSignal >= cooldown)
				{
					var isBullish = candle.ClosePrice > candle.OpenPrice;

					if (isBullish && candle.ClosePrice > smaVal && Position <= 0)
					{
						BuyMarket();
						lastSignal = candle.OpenTime;
					}
					else if (!isBullish && candle.ClosePrice < smaVal && Position >= 0)
					{
						SellMarket();
						lastSignal = candle.OpenTime;
					}
				}

				_prevHigh = candle.HighPrice;
				_prevLow = candle.LowPrice;
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

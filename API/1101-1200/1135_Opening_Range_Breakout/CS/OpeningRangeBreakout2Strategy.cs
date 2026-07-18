using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class OpeningRangeBreakout2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal _orHigh;
	private decimal _orLow;
	private bool _tradeTakenToday;
	private bool _wasInOr;
	private DateTime _currentDay;
	private bool _orEstablished;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public OpeningRangeBreakout2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_orHigh = 0;
		_orLow = 0;
		_tradeTakenToday = false;
		_wasInOr = false;
		_currentDay = default;
		_orEstablished = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_orHigh = 0;
		_orLow = 0;
		_tradeTakenToday = false;
		_wasInOr = false;
		_currentDay = default;
		_orEstablished = false;

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
					_orHigh = 0;
					_orLow = 0;
					_tradeTakenToday = false;
					_orEstablished = false;
				}

				var hour = candle.OpenTime.TimeOfDay.TotalHours;
				var inOr = hour >= 0 && hour < 1;

				if (inOr)
				{
					_orHigh = _orHigh > 0 ? Math.Max(_orHigh, candle.HighPrice) : candle.HighPrice;
					_orLow = _orLow > 0 ? Math.Min(_orLow, candle.LowPrice) : candle.LowPrice;
				}

				if (_wasInOr && !inOr && _orHigh > 0 && _orLow > 0)
				{
					var range = _orHigh - _orLow;
					if (range > 0)
						_orEstablished = true;
				}

				// Only one entry per day, no exit logic (just entry)
				if (!_tradeTakenToday && _orEstablished && !inOr)
				{
					if (candle.ClosePrice > _orHigh && candle.ClosePrice > smaVal && Position <= 0)
					{
						BuyMarket();
						_tradeTakenToday = true;
					}
					else if (candle.ClosePrice < _orLow && candle.ClosePrice < smaVal && Position >= 0)
					{
						SellMarket();
						_tradeTakenToday = true;
					}
				}

				_wasInOr = inOr;
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

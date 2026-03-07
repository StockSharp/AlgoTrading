using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class OrbVwapBraidFilterStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal _orHigh;
	private decimal _orLow;
	private bool _tradeTakenToday;
	private bool _wasInOr;
	private DateTime _currentDay;
	private bool _orEstablished;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public OrbVwapBraidFilterStrategy()
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

		var ema1 = new ExponentialMovingAverage { Length = 3 };
		var ema2 = new ExponentialMovingAverage { Length = 7 };
		var ema3 = new ExponentialMovingAverage { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema1, ema2, ema3, (candle, e1, e2, e3) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!ema1.IsFormed || !ema2.IsFormed || !ema3.IsFormed)
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
				var inOr = hour < 1;

				if (inOr)
				{
					_orHigh = _orHigh > 0 ? Math.Max(_orHigh, candle.HighPrice) : candle.HighPrice;
					_orLow = _orLow > 0 ? Math.Min(_orLow, candle.LowPrice) : candle.LowPrice;
				}

				if (_wasInOr && !inOr && _orHigh > 0 && _orLow > 0 && _orHigh - _orLow > 0)
					_orEstablished = true;

				var bullBraid = e1 > e2 && e2 > e3;
				var bearBraid = e1 < e2 && e2 < e3;

				if (!_tradeTakenToday && _orEstablished && !inOr)
				{
					if (candle.ClosePrice > _orHigh && bullBraid && Position <= 0)
					{
						BuyMarket();
						_tradeTakenToday = true;
					}
					else if (candle.ClosePrice < _orLow && bearBraid && Position >= 0)
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
			DrawIndicator(area, ema1);
			DrawOwnTrades(area);
		}
	}
}

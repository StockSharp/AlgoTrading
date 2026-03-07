using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades when price crosses above the top Central Pivot Range level.
/// Places take profit at a fixed target and stop loss at the pivot level.
/// </summary>
public class PavanCprStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitTarget;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _todayPivot;
	private decimal _todayTop;
	private decimal _lastClose;
	private decimal _takeProfitPrice;
	private decimal _stopLossPrice;
	private decimal _prevSessionHigh;
	private decimal _prevSessionLow;
	private decimal _prevSessionClose;
	private DateTimeOffset _currentDay;

	/// <summary>
	/// Take profit distance in price points.
	/// </summary>
	public decimal TakeProfitTarget
	{
		get => _takeProfitTarget.Value;
		set => _takeProfitTarget.Value = value;
	}

	/// <summary>
	/// Candle type used for trading.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public PavanCprStrategy()
	{
		_takeProfitTarget = Param(nameof(TakeProfitTarget), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit distance in price points", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles for entry logic", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_todayPivot = 0m;
		_todayTop = 0m;
		_lastClose = 0m;
		_takeProfitPrice = 0m;
		_stopLossPrice = 0m;
		_prevSessionHigh = 0m;
		_prevSessionLow = 0m;
		_prevSessionClose = 0m;
		_currentDay = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sessionHigh = 0m;
		var sessionLow = 0m;
		var sessionClose = 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(candle =>
		{
			if (candle.State != CandleStates.Finished)
				return;

			var day = candle.OpenTime.Date;

			if (_currentDay != day)
			{
				if (sessionHigh > 0)
				{
					_prevSessionHigh = sessionHigh;
					_prevSessionLow = sessionLow;
					_prevSessionClose = sessionClose;

					var pivot = (_prevSessionHigh + _prevSessionLow + _prevSessionClose) / 3m;
					var top = (_prevSessionHigh + _prevSessionLow) / 2m;

					_todayPivot = pivot;
					_todayTop = top;
				}

				sessionHigh = candle.HighPrice;
				sessionLow = candle.LowPrice;
				_currentDay = day;
			}
			else
			{
				sessionHigh = Math.Max(sessionHigh, candle.HighPrice);
				sessionLow = Math.Min(sessionLow, candle.LowPrice);
			}

			sessionClose = candle.ClosePrice;

			if (_todayTop == 0m)
			{
				_lastClose = candle.ClosePrice;
				return;
			}

			if (Position == 0 && _lastClose > 0 && _lastClose < _todayTop && candle.ClosePrice > _todayTop)
			{
				BuyMarket();
				_takeProfitPrice = candle.ClosePrice + TakeProfitTarget;
				_stopLossPrice = _todayPivot;
			}
			else if (Position > 0 && _stopLossPrice > 0)
			{
				if (candle.LowPrice <= _stopLossPrice || candle.HighPrice >= _takeProfitPrice)
				{
					SellMarket();
					_stopLossPrice = 0m;
					_takeProfitPrice = 0m;
				}
			}
			else if (Position < 0)
			{
				BuyMarket();
			}

			_lastClose = candle.ClosePrice;
		}).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}
}

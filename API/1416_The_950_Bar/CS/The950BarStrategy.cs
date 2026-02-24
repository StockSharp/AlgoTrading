using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades a specific time bar each day with fixed target and stop.
/// Buys when the candle is bullish, sells when bearish, then manages TP/SL.
/// </summary>
public class The950BarStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _targetPercent;
	private readonly StrategyParam<decimal> _stopPercent;
	private readonly StrategyParam<int> _tradeHour;

	private DateTime? _tradeDate;
	private decimal _targetPrice;
	private decimal _stopPrice;

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Profit target percent.
	/// </summary>
	public decimal TargetPercent { get => _targetPercent.Value; set => _targetPercent.Value = value; }

	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal StopPercent { get => _stopPercent.Value; set => _stopPercent.Value = value; }

	/// <summary>
	/// Hour of day (UTC) to enter trade.
	/// </summary>
	public int TradeHour { get => _tradeHour.Value; set => _tradeHour.Value = value; }

	public The950BarStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_targetPercent = Param(nameof(TargetPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Target %", "Profit target percent", "Parameters");

		_stopPercent = Param(nameof(StopPercent), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Stop %", "Stop loss percent", "Parameters");

		_tradeHour = Param(nameof(TradeHour), 14)
			.SetDisplay("Trade Hour", "UTC hour to enter trade", "Parameters");
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
		_tradeDate = null;
		_targetPrice = 0m;
		_stopPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = 10 };
		var sub = SubscribeCandles(CandleType);
		sub.Bind(sma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Check exits first
		if (Position > 0)
		{
			if (candle.HighPrice >= _targetPrice || candle.LowPrice <= _stopPrice)
			{
				SellMarket();
				return;
			}
		}
		else if (Position < 0)
		{
			if (candle.LowPrice <= _targetPrice || candle.HighPrice >= _stopPrice)
			{
				BuyMarket();
				return;
			}
		}

		// Entry: only when flat and at the designated hour, once per day
		if (Position == 0)
		{
			var utcTime = candle.OpenTime;
			if (_tradeDate != utcTime.Date && utcTime.Hour == TradeHour && utcTime.Minute == 50)
			{
				_tradeDate = utcTime.Date;
				var isLong = candle.ClosePrice > candle.OpenPrice;

				if (isLong)
				{
					BuyMarket();
					_targetPrice = candle.ClosePrice * (1 + TargetPercent / 100m);
					_stopPrice = candle.ClosePrice * (1 - StopPercent / 100m);
				}
				else
				{
					SellMarket();
					_targetPrice = candle.ClosePrice * (1 - TargetPercent / 100m);
					_stopPrice = candle.ClosePrice * (1 + StopPercent / 100m);
				}
			}
		}
	}
}

using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MNQ strategy based on multiple EMA levels and dynamic exits.
/// </summary>
public class MNQEMAStrategy : Strategy
{
	private readonly StrategyParam<int> _ema5Length;
	private readonly StrategyParam<int> _ema13Length;
	private readonly StrategyParam<int> _ema30Length;
	private readonly StrategyParam<int> _ema200Length;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _highestSinceEntry;
	private decimal? _lowestSinceEntry;

	public int Ema5Length { get => _ema5Length.Value; set => _ema5Length.Value = value; }
	public int Ema13Length { get => _ema13Length.Value; set => _ema13Length.Value = value; }
	public int Ema30Length { get => _ema30Length.Value; set => _ema30Length.Value = value; }
	public int Ema200Length { get => _ema200Length.Value; set => _ema200Length.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MNQEMAStrategy()
	{
		_ema5Length = Param(nameof(Ema5Length), 5);
		_ema13Length = Param(nameof(Ema13Length), 13);
		_ema30Length = Param(nameof(Ema30Length), 30);
		_ema200Length = Param(nameof(Ema200Length), 50);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highestSinceEntry = null;
		_lowestSinceEntry = null;

		var ema5 = new EMA { Length = Ema5Length };
		var ema13 = new EMA { Length = Ema13Length };
		var ema30 = new EMA { Length = Ema30Length };
		var ema200 = new EMA { Length = Ema200Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema5, ema13, ema30, ema200, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema5, decimal ema13, decimal ema30, decimal ema200)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var candleIsGreen = close > candle.OpenPrice;
		var candleIsBearish = close < candle.OpenPrice;

		if (Position == 0)
		{
			if (close > ema200 && close > ema30 && close > ema5 && candleIsGreen)
			{
				BuyMarket();
				_highestSinceEntry = close;
			}
			else if (close < ema200 && close < ema30 && close < ema5 && candleIsBearish)
			{
				SellMarket();
				_lowestSinceEntry = close;
			}
		}
		else if (Position > 0)
		{
			_highestSinceEntry = _highestSinceEntry == null ? close : Math.Max(_highestSinceEntry.Value, candle.HighPrice);

			// Exit long: close below ema13
			if (close < ema13)
			{
				SellMarket();
				_highestSinceEntry = null;
			}
		}
		else if (Position < 0)
		{
			_lowestSinceEntry = _lowestSinceEntry == null ? close : Math.Min(_lowestSinceEntry.Value, candle.LowPrice);

			// Exit short: close above ema13
			if (close > ema13)
			{
				BuyMarket();
				_lowestSinceEntry = null;
			}
		}
	}
}

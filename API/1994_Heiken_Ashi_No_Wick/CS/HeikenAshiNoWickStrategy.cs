namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy trading opposite Heiken Ashi candles without wicks.
/// A bullish HA candle with no lower wick and a larger body than the previous one opens a short.
/// A bearish HA candle with no upper wick and a larger body than the previous one opens a long.
/// Positions close on the first opposite colored candle without the respective wick.
/// </summary>
public class HeikenAshiNoWickStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevHaOpen;
	private decimal _prevHaClose;
	private decimal _prevBody;

	/// <summary>
	/// Candle type to subscribe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="HeikenAshiNoWickStrategy"/>.
	/// </summary>
	public HeikenAshiNoWickStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevHaOpen = 0m;
		_prevHaClose = 0m;
		_prevBody = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		decimal haOpen;
		decimal haClose;
		decimal haHigh;
		decimal haLow;

		if (_prevHaOpen == 0m && _prevHaClose == 0m)
		{
			haOpen = (candle.OpenPrice + candle.ClosePrice) / 2m;
			haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		}
		else
		{
			haOpen = (_prevHaOpen + _prevHaClose) / 2m;
			haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		}

		haHigh = Math.Max(candle.HighPrice, Math.Max(haOpen, haClose));
		haLow = Math.Min(candle.LowPrice, Math.Min(haOpen, haClose));

		var body = Math.Abs(haClose - haOpen);
		var prevIsBull = _prevHaClose > _prevHaOpen;
		var prevIsBear = _prevHaClose < _prevHaOpen;
		var isBull = haClose > haOpen;
		var isBear = haClose < haOpen;

		var step = Security.PriceStep ?? 1m;
		var threshold = step * 5m;
		var noLowerWick = Math.Abs(Math.Min(haOpen, haClose) - haLow) <= threshold;
		var noUpperWick = Math.Abs(haHigh - Math.Max(haOpen, haClose)) <= threshold;

		var sellSignal = isBull && noLowerWick && prevIsBull && body > _prevBody;
		var buySignal = isBear && noUpperWick && prevIsBear && body > _prevBody;
		var exitLong = isBull && noLowerWick && prevIsBull;
		var exitShort = isBear && noUpperWick && prevIsBear;

		if (Position > 0 && exitLong)
		SellMarket(Position);
		else if (Position < 0 && exitShort)
		BuyMarket(Math.Abs(Position));
		else if (sellSignal && Position >= 0)
		SellMarket(Volume + (Position > 0 ? Position : 0m));
		else if (buySignal && Position <= 0)
		BuyMarket(Volume + (Position < 0 ? Math.Abs(Position) : 0m));

		_prevHaOpen = haOpen;
		_prevHaClose = haClose;
		_prevBody = body;
	}
}

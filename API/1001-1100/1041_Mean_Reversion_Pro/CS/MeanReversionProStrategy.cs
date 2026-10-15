using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mean Reversion Pro strategy.
/// When flat, a long opens on a close below the fast SMA, inside the lowest 20% of the candle range and above the slow SMA;
/// a short opens on a close above the fast SMA, inside the highest 20% of the range (above the 80% level) and below the slow SMA.
/// A long closes when the close crosses above the fast SMA and a short when it crosses below. Direction selects
/// "Long only", "Short only" or "Both".
/// </summary>
public class MeanReversionProStrategy : Strategy
{
	private const string LongOnlyValue = "Long only";
	private const string ShortOnlyValue = "Short only";

	private readonly StrategyParam<int> _fastSma;
	private readonly StrategyParam<int> _slowSma;
	private readonly StrategyParam<string> _direction;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevClose;
	private decimal? _prevFast;

	/// <summary>
	/// Fast SMA length.
	/// </summary>
	public int FastSma
	{
		get => _fastSma.Value;
		set => _fastSma.Value = value;
	}

	/// <summary>
	/// Slow SMA length.
	/// </summary>
	public int SlowSma
	{
		get => _slowSma.Value;
		set => _slowSma.Value = value;
	}

	/// <summary>
	/// Allowed trade direction: "Long only", "Short only" or "Both".
	/// </summary>
	public string Direction
	{
		get => _direction.Value;
		set => _direction.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public MeanReversionProStrategy()
	{
		_fastSma = Param(nameof(FastSma), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA", "Fast SMA length", "Indicators");

		_slowSma = Param(nameof(SlowSma), 100)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA", "Slow SMA length", "Indicators");

		_direction = Param(nameof(Direction), LongOnlyValue)
			.SetDisplay("Direction", "Allowed trade direction: Long only, Short only or Both", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevClose = null;
		_prevFast = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = null;
		_prevFast = null;

		var fast = new SimpleMovingAverage { Length = FastSma };
		var slow = new SimpleMovingAverage { Length = SlowSma };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fast, slow, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!fastValue.IsFormed || !slowValue.IsFormed)
			return;

		var fast = fastValue.GetValue<decimal>();
		var slow = slowValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		var prevClose = _prevClose;
		var prevFast = _prevFast;
		_prevClose = close;
		_prevFast = fast;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var crossedUp = prevClose is decimal pc1 && prevFast is decimal pf1 && pc1 <= pf1 && close > fast;
		var crossedDown = prevClose is decimal pc2 && prevFast is decimal pf2 && pc2 >= pf2 && close < fast;

		if (Position > 0)
		{
			if (crossedUp)
				SellMarket(Position);

			return;
		}

		if (Position < 0)
		{
			if (crossedDown)
				BuyMarket(-Position);

			return;
		}

		var range = candle.HighPrice - candle.LowPrice;
		var lowLevel = candle.LowPrice + 0.2m * range;
		var highLevel = candle.LowPrice + 0.8m * range;

		var allowLong = !Direction.EqualsIgnoreCase(ShortOnlyValue);
		var allowShort = !Direction.EqualsIgnoreCase(LongOnlyValue);

		if (allowLong && close < fast && close < lowLevel && close > slow)
			BuyMarket(Volume);
		else if (allowShort && close > fast && close > highLevel && close < slow)
			SellMarket(Volume);
	}
}

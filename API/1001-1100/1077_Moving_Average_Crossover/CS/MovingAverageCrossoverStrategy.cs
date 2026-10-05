using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average crossover strategy.
/// Goes long when the short SMA crosses above the long SMA and short when it crosses below,
/// reversing the position on the opposite crossover.
/// </summary>
public class MovingAverageCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevShort;
	private decimal? _prevLong;

	/// <summary>
	/// Short SMA length.
	/// </summary>
	public int ShortLength
	{
		get => _shortLength.Value;
		set => _shortLength.Value = value;
	}

	/// <summary>
	/// Long SMA length.
	/// </summary>
	public int LongLength
	{
		get => _longLength.Value;
		set => _longLength.Value = value;
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
	public MovingAverageCrossoverStrategy()
	{
		_shortLength = Param(nameof(ShortLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Short Length", "Short SMA length", "Indicators");

		_longLength = Param(nameof(LongLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Long Length", "Long SMA length", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_prevShort = null;
		_prevLong = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevShort = null;
		_prevLong = null;

		var shortSma = new SimpleMovingAverage { Length = ShortLength };
		var longSma = new SimpleMovingAverage { Length = LongLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(shortSma, longSma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, shortSma);
			DrawIndicator(area, longSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue shortValue, IIndicatorValue longValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!shortValue.IsFormed || !longValue.IsFormed)
			return;

		var shortMa = shortValue.GetValue<decimal>();
		var longMa = longValue.GetValue<decimal>();

		var prevShort = _prevShort;
		var prevLong = _prevLong;
		_prevShort = shortMa;
		_prevLong = longMa;

		if (prevShort is not decimal ps || prevLong is not decimal pl)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var crossUp = ps <= pl && shortMa > longMa;
		var crossDown = ps >= pl && shortMa < longMa;

		if (crossUp && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossDown && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover trailing stop strategy.
/// The short EMA crossing above the long EMA goes long and crossing below goes short, reversing an opposite position.
/// A percent trailing stop follows the best price since entry and closes the position when price reverses by TrailStopPercent.
/// </summary>
public class EmaCrossoverTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<decimal> _trailStopPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevShort;
	private decimal? _prevLong;

	/// <summary>
	/// Short EMA length.
	/// </summary>
	public int ShortLength
	{
		get => _shortLength.Value;
		set => _shortLength.Value = value;
	}

	/// <summary>
	/// Long EMA length.
	/// </summary>
	public int LongLength
	{
		get => _longLength.Value;
		set => _longLength.Value = value;
	}

	/// <summary>
	/// Trailing stop percentage.
	/// </summary>
	public decimal TrailStopPercent
	{
		get => _trailStopPercent.Value;
		set => _trailStopPercent.Value = value;
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
	public EmaCrossoverTrailingStopStrategy()
	{
		_shortLength = Param(nameof(ShortLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Short EMA", "Short EMA length", "Indicators");

		_longLength = Param(nameof(LongLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Long EMA", "Long EMA length", "Indicators");

		_trailStopPercent = Param(nameof(TrailStopPercent), 1m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop %", "Trailing stop percentage from the best price", "Risk");

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

		var shortEma = new ExponentialMovingAverage { Length = ShortLength };
		var longEma = new ExponentialMovingAverage { Length = LongLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(shortEma, longEma, ProcessCandle)
			.Start();

		if (TrailStopPercent > 0m)
			StartProtection(new Unit(), new Unit(TrailStopPercent, UnitTypes.Percent), isStopTrailing: true, useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, shortEma);
			DrawIndicator(area, longEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal shortValue, decimal longValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prevShort = _prevShort;
		var prevLong = _prevLong;
		_prevShort = shortValue;
		_prevLong = longValue;

		if (prevShort is not decimal ps || prevLong is not decimal pl)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (ps <= pl && shortValue > longValue && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (ps >= pl && shortValue < longValue && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}

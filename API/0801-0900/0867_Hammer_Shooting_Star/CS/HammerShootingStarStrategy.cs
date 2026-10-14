using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hammer and Shooting Star strategy.
/// A candle whose body is at least MinBodyRangePct of its range is a hammer when its lower wick is at least WickFactor times the
/// body and its upper wick at most MaxOppositeWickFactor times the body; a shooting star mirrors this. After a hammer closes the
/// strategy buys with the stop at the hammer's low and the target at its high; after a shooting star it sells with the stop at
/// its high and the target at its low. An opposite pattern reverses the position.
/// </summary>
public class HammerShootingStarStrategy : Strategy
{
	private readonly StrategyParam<decimal> _wickFactor;
	private readonly StrategyParam<decimal> _maxOppositeWickFactor;
	private readonly StrategyParam<decimal> _minBodyRangePct;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// Minimum main wick length as a multiple of the body.
	/// </summary>
	public decimal WickFactor
	{
		get => _wickFactor.Value;
		set => _wickFactor.Value = value;
	}

	/// <summary>
	/// Maximum opposite wick length as a multiple of the body.
	/// </summary>
	public decimal MaxOppositeWickFactor
	{
		get => _maxOppositeWickFactor.Value;
		set => _maxOppositeWickFactor.Value = value;
	}

	/// <summary>
	/// Minimum body size as a fraction of the candle range.
	/// </summary>
	public decimal MinBodyRangePct
	{
		get => _minBodyRangePct.Value;
		set => _minBodyRangePct.Value = value;
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
	public HammerShootingStarStrategy()
	{
		_wickFactor = Param(nameof(WickFactor), 0.9m)
			.SetGreaterThanZero()
			.SetDisplay("Wick Factor", "Minimum main wick length as a multiple of the body", "Pattern");

		_maxOppositeWickFactor = Param(nameof(MaxOppositeWickFactor), 0.45m)
			.SetNotNegative()
			.SetDisplay("Max Opposite Wick", "Maximum opposite wick length as a multiple of the body", "Pattern");

		_minBodyRangePct = Param(nameof(MinBodyRangePct), 0.2m)
			.SetNotNegative()
			.SetDisplay("Min Body/Range", "Minimum body size as a fraction of the candle range", "Pattern");

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
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_stopPrice = 0m;
		_takePrice = 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

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

		// Exits of the open position against the signal candle levels; the stop is checked first.
		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
			{
				SellMarket(Position);
				return;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
			{
				BuyMarket(-Position);
				return;
			}
		}

		var range = candle.HighPrice - candle.LowPrice;
		if (range <= 0)
			return;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		if (body <= 0 || body < MinBodyRangePct * range)
			return;

		var upperWick = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice);
		var lowerWick = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice;

		var isHammer = lowerWick >= WickFactor * body && upperWick <= MaxOppositeWickFactor * body;
		var isShootingStar = upperWick >= WickFactor * body && lowerWick <= MaxOppositeWickFactor * body;

		if (isHammer && !isShootingStar && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = candle.LowPrice;
			_takePrice = candle.HighPrice;
		}
		else if (isShootingStar && !isHammer && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = candle.HighPrice;
			_takePrice = candle.LowPrice;
		}
	}
}

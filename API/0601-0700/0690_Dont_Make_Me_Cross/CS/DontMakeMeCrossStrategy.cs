using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dont Make Me Cross strategy.
/// Both EMAs are shifted vertically by ShiftAmount. A cross of the shifted short EMA above the shifted long EMA goes long,
/// a cross below goes short, and the opposite cross reverses the position.
/// </summary>
public class DontMakeMeCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _shortEmaLength;
	private readonly StrategyParam<int> _longEmaLength;
	private readonly StrategyParam<decimal> _shiftAmount;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevShort;
	private decimal? _prevLong;

	/// <summary>
	/// Short EMA period.
	/// </summary>
	public int ShortEmaLength
	{
		get => _shortEmaLength.Value;
		set => _shortEmaLength.Value = value;
	}

	/// <summary>
	/// Long EMA period.
	/// </summary>
	public int LongEmaLength
	{
		get => _longEmaLength.Value;
		set => _longEmaLength.Value = value;
	}

	/// <summary>
	/// Vertical shift added to both EMAs.
	/// </summary>
	public decimal ShiftAmount
	{
		get => _shiftAmount.Value;
		set => _shiftAmount.Value = value;
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
	public DontMakeMeCrossStrategy()
	{
		_shortEmaLength = Param(nameof(ShortEmaLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Short EMA", "Short EMA period", "Indicators");

		_longEmaLength = Param(nameof(LongEmaLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Long EMA", "Long EMA period", "Indicators");

		_shiftAmount = Param(nameof(ShiftAmount), -50m)
			.SetDisplay("Shift Amount", "Vertical shift added to both EMAs", "Indicators");

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

		var shortEma = new ExponentialMovingAverage { Length = ShortEmaLength };
		var longEma = new ExponentialMovingAverage { Length = LongEmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(shortEma, longEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, shortEma);
			DrawIndicator(area, longEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue shortValue, IIndicatorValue longValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!shortValue.IsFormed || !longValue.IsFormed)
			return;

		var shortEma = shortValue.GetValue<decimal>() + ShiftAmount;
		var longEma = longValue.GetValue<decimal>() + ShiftAmount;

		var prevShort = _prevShort;
		var prevLong = _prevLong;

		_prevShort = shortEma;
		_prevLong = longEma;

		if (prevShort is not decimal ps || prevLong is not decimal pl)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var crossUp = ps <= pl && shortEma > longEma;
		var crossDown = ps >= pl && shortEma < longEma;

		if (crossUp && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossDown && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// New intraday high with weak bar strategy.
/// When flat, a candle whose high is the highest high of the last HighestLength bars but which closes in the lower WeakRatio part of
/// its range opens a long. The long closes when a candle closes above the previous candle's high.
/// </summary>
public class NewIntradayHighWithWeakBarStrategy : Strategy
{
	private readonly StrategyParam<int> _highestLength;
	private readonly StrategyParam<decimal> _weakRatio;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private decimal? _prevHigh;

	/// <summary>
	/// Bars the highest high is taken over.
	/// </summary>
	public int HighestLength
	{
		get => _highestLength.Value;
		set => _highestLength.Value = value;
	}

	/// <summary>
	/// Maximum (close - low) / (high - low) of a weak bar.
	/// </summary>
	public decimal WeakRatio
	{
		get => _weakRatio.Value;
		set => _weakRatio.Value = value;
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
	public NewIntradayHighWithWeakBarStrategy()
	{
		_highestLength = Param(nameof(HighestLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Highest Length", "Bars the highest high is taken over", "Indicators");

		_weakRatio = Param(nameof(WeakRatio), 0.15m)
			.SetRange(0m, 1m)
			.SetDisplay("Weak Ratio", "Maximum (close - low) / (high - low) of a weak bar", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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
		_highest = null;
		_prevHigh = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevHigh = null;
		_highest = new Highest { Length = HighestLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _highest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// The highest high is taken over candle highs, the current one included.
		var highestValue = _highest.Process(candle.HighPrice, candle.ServerTime, true);
		var prevHigh = _prevHigh;
		_prevHigh = candle.HighPrice;

		if (!_highest.IsFormed || prevHigh is not decimal lastHigh)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			if (candle.ClosePrice > lastHigh)
				SellMarket(Position);
			return;
		}

		if (Position != 0)
			return;

		var range = candle.HighPrice - candle.LowPrice;
		if (range <= 0)
			return;

		var isNewHigh = candle.HighPrice >= highestValue.ToDecimal();
		var isWeak = (candle.ClosePrice - candle.LowPrice) / range < WeakRatio;

		if (isNewHigh && isWeak)
			BuyMarket(Volume);
	}
}

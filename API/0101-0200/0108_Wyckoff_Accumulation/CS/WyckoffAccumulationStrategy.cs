using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Wyckoff Accumulation strategy.
/// The accumulation range is the highest high and lowest low of the previous RangePeriod candles. A close above the range buys while flat.
/// The stop lies StopLossPercent below the base of the range, and a close below it closes the position.
/// </summary>
public class WyckoffAccumulationStrategy : Strategy
{
	private readonly StrategyParam<int> _rangePeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<(decimal High, decimal Low)> _candles = [];
	private decimal _stopPrice;

	/// <summary>
	/// Number of previous candles that form the level.
	/// </summary>
	public int RangePeriod
	{
		get => _rangePeriod.Value;
		set => _rangePeriod.Value = value;
	}

	/// <summary>
	/// Distance of the stop beyond the level, in percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
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
	public WyckoffAccumulationStrategy()
	{
		_rangePeriod = Param(nameof(RangePeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("RangePeriod", "Number of previous candles that form the level", "Pattern");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Distance of the stop beyond the level, in percent", "Risk");

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
		_candles.Clear();
		_stopPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_candles.Clear();
		_stopPrice = default;

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

		// The level is formed by the candles before this one.
		var ready = _candles.Count == RangePeriod;
		var high = ready ? _candles.Max(c => c.High) : 0m;
		var low = ready ? _candles.Min(c => c.Low) : 0m;

		_candles.Add((candle.HighPrice, candle.LowPrice));

		if (_candles.Count > RangePeriod)
			_candles.RemoveAt(0);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (Position > 0)
		{
			if (close <= _stopPrice)
				SellMarket(Position);

			return;
		}

		if (Position != 0 || !ready || !(close > high))
			return;

		BuyMarket(Volume);
		_stopPrice = low * (1 - StopLossPercent / 100m);
	}
}

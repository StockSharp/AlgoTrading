namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Liquidity Swings Strategy.
/// The latest pivot high (Lookback bars on each side) is resistance and the latest pivot low is support. A long opens when
/// the low crosses above support with the close below resistance; a short opens when the high crosses below resistance with
/// the close above support. The stop sits StopLossBuffer beyond the level and the target is twice the risk from the entry.
/// </summary>
public class LiquiditySwingsStrategy : Strategy
{
	private const decimal RewardMultiplier = 2m;

	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<decimal> _stopLossBuffer;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<ICandleMessage> _window = [];
	private decimal? _support;
	private decimal? _resistance;
	private decimal? _prevLow;
	private decimal? _prevHigh;
	private decimal? _stopPrice;
	private decimal? _targetPrice;

	/// <summary>
	/// Bars on each side of a pivot.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	/// <summary>
	/// Price distance of the stop beyond the level.
	/// </summary>
	public decimal StopLossBuffer
	{
		get => _stopLossBuffer.Value;
		set => _stopLossBuffer.Value = value;
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
	public LiquiditySwingsStrategy()
	{
		_lookback = Param(nameof(Lookback), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Bars on each side of a pivot", "Pivots");

		_stopLossBuffer = Param(nameof(StopLossBuffer), 0.5m)
			.SetNotNegative()
			.SetDisplay("Stop Loss Buffer", "Price distance of the stop beyond the level", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	private void ResetState()
	{
		_window.Clear();
		_support = null;
		_resistance = null;
		_prevLow = null;
		_prevHigh = null;
		_stopPrice = null;
		_targetPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

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

		UpdatePivots(candle);

		var prevLow = _prevLow;
		var prevHigh = _prevHigh;
		_prevLow = candle.LowPrice;
		_prevHigh = candle.HighPrice;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			if ((_stopPrice is decimal stop && candle.LowPrice <= stop) || (_targetPrice is decimal target && candle.HighPrice >= target))
				SellMarket(Position);

			return;
		}

		if (Position < 0)
		{
			if ((_stopPrice is decimal stop && candle.HighPrice >= stop) || (_targetPrice is decimal target && candle.LowPrice <= target))
				BuyMarket(-Position);

			return;
		}

		if (_support is not decimal support || _resistance is not decimal resistance || prevLow is not decimal lastLow || prevHigh is not decimal lastHigh)
			return;

		var close = candle.ClosePrice;

		if (lastLow <= support && candle.LowPrice > support && close < resistance)
		{
			var stop = support - StopLossBuffer;
			var risk = close - stop;
			if (risk <= 0m)
				return;

			BuyMarket(Volume);
			_stopPrice = stop;
			_targetPrice = close + RewardMultiplier * risk;
		}
		else if (lastHigh >= resistance && candle.HighPrice < resistance && close > support)
		{
			var stop = resistance + StopLossBuffer;
			var risk = stop - close;
			if (risk <= 0m)
				return;

			SellMarket(Volume);
			_stopPrice = stop;
			_targetPrice = close - RewardMultiplier * risk;
		}
	}

	private void UpdatePivots(ICandleMessage candle)
	{
		_window.Add(candle);

		var size = Lookback * 2 + 1;
		if (_window.Count > size)
			_window.RemoveAt(0);

		if (_window.Count < size)
			return;

		// The middle candle is a pivot once Lookback candles on each side confirm it.
		var center = _window[Lookback];
		var others = _window.Where((_, i) => i != Lookback).ToArray();

		if (others.All(c => c.HighPrice < center.HighPrice))
			_resistance = center.HighPrice;

		if (others.All(c => c.LowPrice > center.LowPrice))
			_support = center.LowPrice;
	}
}

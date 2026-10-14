using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Narrow range breakout strategy.
/// A setup appears on an inside bar whose range is narrower than the range of the reference bar Length periods ago.
/// The reference bar's high and low become breakout levels: a close above the high buys, a close below the low sells.
/// The take profit is the reference range away from the entry level and the stop loss is StopLossPercent of that range.
/// </summary>
public class NarrowRangeStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<ICandleMessage> _history = [];
	private decimal? _breakoutHigh;
	private decimal? _breakoutLow;
	private decimal _setupRange;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// Bars back to the reference bar.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Stop loss as a fraction of the reference range.
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
	public NarrowRangeStrategy()
	{
		_length = Param(nameof(Length), 4)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Bars back to the reference bar", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 0.35m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop loss as a fraction of the reference range", "Risk");

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
		ResetState();
	}

	private void ResetState()
	{
		_history.Clear();
		_breakoutHigh = null;
		_breakoutLow = null;
		_setupRange = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
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

		if (IsFormedAndOnlineAndAllowTrading())
			Trade(candle);

		_history.Add(candle);
		if (_history.Count > Length + 1)
			_history.RemoveAt(0);

		if (Position != 0 || _history.Count <= Length)
			return;

		var previous = _history[^2];
		var reference = _history[0];
		var range = candle.HighPrice - candle.LowPrice;
		var referenceRange = reference.HighPrice - reference.LowPrice;
		var insideBar = candle.HighPrice < previous.HighPrice && candle.LowPrice > previous.LowPrice;

		if (insideBar && range < referenceRange && referenceRange > 0)
		{
			_breakoutHigh = reference.HighPrice;
			_breakoutLow = reference.LowPrice;
			_setupRange = referenceRange;
		}
	}

	private void Trade(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if ((StopLossPercent > 0 && candle.LowPrice <= _stopPrice) || candle.HighPrice >= _takePrice)
				SellMarket(Position);

			return;
		}

		if (Position < 0)
		{
			if ((StopLossPercent > 0 && candle.HighPrice >= _stopPrice) || candle.LowPrice <= _takePrice)
				BuyMarket(-Position);

			return;
		}

		if (_breakoutHigh is not decimal high || _breakoutLow is not decimal low)
			return;

		if (candle.ClosePrice > high)
		{
			BuyMarket(Volume);
			_takePrice = high + _setupRange;
			_stopPrice = high - _setupRange * StopLossPercent;
			_breakoutHigh = _breakoutLow = null;
		}
		else if (candle.ClosePrice < low)
		{
			SellMarket(Volume);
			_takePrice = low - _setupRange;
			_stopPrice = low + _setupRange * StopLossPercent;
			_breakoutHigh = _breakoutLow = null;
		}
	}
}

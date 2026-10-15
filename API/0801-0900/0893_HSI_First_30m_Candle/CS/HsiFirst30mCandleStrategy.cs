using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// HSI First 30m Candle strategy.
/// Records the high and low of the first 30 minutes after the Hong Kong session opens (09:30 HKT, 01:30 UTC). During the rest of
/// the session a close above that high goes long and a close below that low goes short, at most one trade per day. The stop sits
/// at the opposite side of the range and the target at the range size multiplied by RiskReward from the entry.
/// </summary>
public class HsiFirst30mCandleStrategy : Strategy
{
	private static readonly TimeSpan _sessionOpen = new(1, 30, 0);
	private static readonly TimeSpan _rangeEnd = new(2, 0, 0);
	private static readonly TimeSpan _sessionClose = new(8, 0, 0);

	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime _currentDay;
	private decimal? _rangeHigh;
	private decimal? _rangeLow;
	private bool _tradedToday;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// Target distance as a multiple of the range size.
	/// </summary>
	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
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
	public HsiFirst30mCandleStrategy()
	{
		_riskReward = Param(nameof(RiskReward), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Reward", "Target distance as a multiple of the range size", "Risk");

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
		_currentDay = default;
		_rangeHigh = null;
		_rangeLow = null;
		_tradedToday = false;
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

		var openTime = candle.OpenTime;
		var day = openTime.Date;
		var timeOfDay = openTime.TimeOfDay;

		if (day != _currentDay)
		{
			_currentDay = day;
			_rangeHigh = null;
			_rangeLow = null;
			_tradedToday = false;
		}

		if (timeOfDay >= _sessionOpen && timeOfDay < _rangeEnd)
		{
			_rangeHigh = _rangeHigh is decimal h ? Math.Max(h, candle.HighPrice) : candle.HighPrice;
			_rangeLow = _rangeLow is decimal l ? Math.Min(l, candle.LowPrice) : candle.LowPrice;
			return;
		}

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
				SellMarket(Position);
			return;
		}

		if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
				BuyMarket(-Position);
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_tradedToday || timeOfDay < _rangeEnd || timeOfDay >= _sessionClose)
			return;

		if (_rangeHigh is not decimal high || _rangeLow is not decimal low)
			return;

		var range = high - low;
		if (range <= 0)
			return;

		var close = candle.ClosePrice;

		if (close > high)
		{
			BuyMarket(Volume);
			_stopPrice = low;
			_takePrice = close + range * RiskReward;
			_tradedToday = true;
		}
		else if (close < low)
		{
			SellMarket(Volume);
			_stopPrice = high;
			_takePrice = close - range * RiskReward;
			_tradedToday = true;
		}
	}
}

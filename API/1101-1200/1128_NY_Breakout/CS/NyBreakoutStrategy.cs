using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// NY breakout strategy.
/// The high and low of the candles opening between 13:00 and 13:30 UTC form the session range. The first candle after 13:30 goes
/// long if it closes above the range high and short if it closes below the range low. The stop is the opposite range boundary
/// and the target lies RewardRisk times the range height from the entry.
/// </summary>
public class NyBreakoutStrategy : Strategy
{
	private static readonly TimeSpan _rangeStart = new(13, 0, 0);
	private static readonly TimeSpan _rangeEnd = new(13, 30, 0);

	private readonly StrategyParam<decimal> _rewardRisk;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime _currentDay;
	private decimal? _rangeHigh;
	private decimal? _rangeLow;
	private bool _checked;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	/// <summary>
	/// Target distance as a multiple of the range height.
	/// </summary>
	public decimal RewardRisk
	{
		get => _rewardRisk.Value;
		set => _rewardRisk.Value = value;
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
	public NyBreakoutStrategy()
	{
		_rewardRisk = Param(nameof(RewardRisk), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Reward/Risk", "Target distance as a multiple of the range height", "Risk");

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
		ResetState();
	}

	private void ResetState()
	{
		_currentDay = default;
		_rangeHigh = null;
		_rangeLow = null;
		_checked = false;
		_stopPrice = null;
		_takePrice = null;
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

		var day = candle.OpenTime.Date;
		var tod = candle.OpenTime.TimeOfDay;

		if (day != _currentDay)
		{
			_currentDay = day;
			_rangeHigh = null;
			_rangeLow = null;
			_checked = false;
		}

		if (tod >= _rangeStart && tod < _rangeEnd)
		{
			_rangeHigh = _rangeHigh is decimal h ? Math.Max(h, candle.HighPrice) : candle.HighPrice;
			_rangeLow = _rangeLow is decimal l ? Math.Min(l, candle.LowPrice) : candle.LowPrice;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0 && _stopPrice is decimal longStop && _takePrice is decimal longTake)
		{
			if (candle.LowPrice <= longStop || candle.HighPrice >= longTake)
			{
				SellMarket(Position);
				_stopPrice = null;
				_takePrice = null;
				return;
			}
		}
		else if (Position < 0 && _stopPrice is decimal shortStop && _takePrice is decimal shortTake)
		{
			if (candle.HighPrice >= shortStop || candle.LowPrice <= shortTake)
			{
				BuyMarket(-Position);
				_stopPrice = null;
				_takePrice = null;
				return;
			}
		}

		if (_checked || tod < _rangeEnd || _rangeHigh is not decimal high || _rangeLow is not decimal low)
			return;

		// Only the first candle after the window is checked for a breakout.
		_checked = true;

		var close = candle.ClosePrice;
		var range = high - low;

		if (close > high && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = low;
			_takePrice = close + range * RewardRisk;
		}
		else if (close < low && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = high;
			_takePrice = close - range * RewardRisk;
		}
	}
}

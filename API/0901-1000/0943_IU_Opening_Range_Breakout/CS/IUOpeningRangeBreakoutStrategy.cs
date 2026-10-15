using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// IU opening range breakout strategy.
/// The first candle of each UTC day sets the opening range. Before EndTime a close crossing above its high goes long and a close
/// crossing below its low goes short, reversing an opposite position, with at most MaxTrades entries a day. The stop sits at the
/// previous candle's low (long) or high (short), the target RiskReward times the stop distance away, and any open position is
/// closed at EndTime.
/// </summary>
public class IUOpeningRangeBreakoutStrategy : Strategy
{
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<TimeSpan> _endTime;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime _currentDay;
	private decimal? _rangeHigh;
	private decimal? _rangeLow;
	private decimal? _prevHigh;
	private decimal? _prevLow;
	private decimal? _prevClose;
	private decimal? _stopPrice;
	private decimal? _targetPrice;
	private int _tradesToday;

	/// <summary>
	/// Target distance as a multiple of the stop distance.
	/// </summary>
	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
	}

	/// <summary>
	/// Maximum entries per day.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Time of day (UTC) when positions are closed and entries stop.
	/// </summary>
	public TimeSpan EndTime
	{
		get => _endTime.Value;
		set => _endTime.Value = value;
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
	public IUOpeningRangeBreakoutStrategy()
	{
		_riskReward = Param(nameof(RiskReward), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Risk/Reward", "Target distance as a multiple of the stop distance", "Risk")
			.SetOptimize(1m, 3m, 0.5m);

		_maxTrades = Param(nameof(MaxTrades), 2)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades", "Maximum entries per day", "General");

		_endTime = Param(nameof(EndTime), new TimeSpan(15, 0, 0))
			.SetDisplay("End Time", "Time of day (UTC) when positions are closed", "General");

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
		_rangeHigh = _rangeLow = null;
		_prevHigh = _prevLow = _prevClose = null;
		_stopPrice = _targetPrice = null;
		_tradesToday = 0;
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

		var prevHigh = _prevHigh;
		var prevLow = _prevLow;
		var prevClose = _prevClose;
		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_prevClose = candle.ClosePrice;

		var day = candle.OpenTime.Date;
		if (_currentDay != day)
		{
			// The first candle of the day is the opening range.
			_currentDay = day;
			_rangeHigh = candle.HighPrice;
			_rangeLow = candle.LowPrice;
			_tradesToday = 0;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (candle.OpenTime.TimeOfDay >= EndTime)
		{
			if (Position > 0)
				SellMarket(Position);
			else if (Position < 0)
				BuyMarket(-Position);

			_stopPrice = _targetPrice = null;
			return;
		}

		if (Position > 0 && _stopPrice is decimal longStop && _targetPrice is decimal longTarget)
		{
			if (candle.LowPrice <= longStop || candle.HighPrice >= longTarget)
			{
				SellMarket(Position);
				_stopPrice = _targetPrice = null;
				return;
			}
		}
		else if (Position < 0 && _stopPrice is decimal shortStop && _targetPrice is decimal shortTarget)
		{
			if (candle.HighPrice >= shortStop || candle.LowPrice <= shortTarget)
			{
				BuyMarket(-Position);
				_stopPrice = _targetPrice = null;
				return;
			}
		}

		if (_tradesToday >= MaxTrades || _rangeHigh is not decimal high || _rangeLow is not decimal low ||
			prevClose is not decimal lastClose || prevHigh is not decimal lastHigh || prevLow is not decimal lastLow)
			return;

		var close = candle.ClosePrice;

		if (lastClose <= high && close > high && Position <= 0 && lastLow < close)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_tradesToday++;
			_stopPrice = lastLow;
			_targetPrice = close + (close - lastLow) * RiskReward;
		}
		else if (lastClose >= low && close < low && Position >= 0 && lastHigh > close)
		{
			SellMarket(Volume + Math.Abs(Position));
			_tradesToday++;
			_stopPrice = lastHigh;
			_targetPrice = close - (lastHigh - close) * RiskReward;
		}
	}
}

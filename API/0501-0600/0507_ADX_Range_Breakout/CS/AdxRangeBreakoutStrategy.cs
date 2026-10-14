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
/// ADX Range Breakout strategy.
/// Buys when the close reaches the highest close of the previous HighestPeriod candles while ADX is below AdxThreshold, at most
/// MaxTradesPerDay times per trading day (UTC). A fixed StopLoss in price units protects the position and it is closed on the last
/// candle of the day.
/// </summary>
public class AdxRangeBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<int> _highestPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<int> _maxTradesPerDay;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _closes = [];
	private DateTime _currentDay;
	private int _tradesToday;

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Previous candles whose highest close must be reached.
	/// </summary>
	public int HighestPeriod
	{
		get => _highestPeriod.Value;
		set => _highestPeriod.Value = value;
	}

	/// <summary>
	/// ADX level the market must stay below.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Maximum entries per trading day.
	/// </summary>
	public int MaxTradesPerDay
	{
		get => _maxTradesPerDay.Value;
		set => _maxTradesPerDay.Value = value;
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
	public AdxRangeBreakoutStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "ADX period", "Indicators");

		_highestPeriod = Param(nameof(HighestPeriod), 34)
			.SetGreaterThanZero()
			.SetDisplay("Highest Period", "Previous candles whose highest close must be reached", "Indicators");

		_adxThreshold = Param(nameof(AdxThreshold), 17.5m)
			.SetDisplay("ADX Threshold", "ADX level the market must stay below", "Indicators");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop loss distance in price units", "Risk");

		_maxTradesPerDay = Param(nameof(MaxTradesPerDay), 3)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades Per Day", "Maximum entries per trading day", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
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
		_closes.Clear();
		_currentDay = default;
		_tradesToday = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_closes.Clear();
		_currentDay = default;
		_tradesToday = 0;

		var adx = new AverageDirectionalIndex { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(adx, ProcessCandle)
			.Start();

		StartProtection(new Unit(), new Unit(StopLoss, UnitTypes.Absolute), useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, adx);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		decimal? previousHighest = _closes.Count >= HighestPeriod ? _closes.Max() : null;

		_closes.Add(close);
		if (_closes.Count > HighestPeriod)
			_closes.RemoveAt(0);

		var day = candle.OpenTime.Date;
		if (day != _currentDay)
		{
			_currentDay = day;
			_tradesToday = 0;
		}

		if (!adxValue.IsFormed || ((IAverageDirectionalIndexValue)adxValue).MovingAverage is not decimal adx)
			return;

		if (previousHighest is not decimal highest || !IsFormedAndOnlineAndAllowTrading())
			return;

		var frame = CandleType.Arg is TimeSpan tf ? tf : TimeSpan.Zero;
		var lastOfDay = (candle.OpenTime + frame).Date > day;

		if (Position > 0)
		{
			if (lastOfDay)
				SellMarket(Position);

			return;
		}

		if (!lastOfDay && Position == 0 && _tradesToday < MaxTradesPerDay && close >= highest && adx < AdxThreshold)
		{
			BuyMarket(Volume);
			_tradesToday++;
		}
	}
}

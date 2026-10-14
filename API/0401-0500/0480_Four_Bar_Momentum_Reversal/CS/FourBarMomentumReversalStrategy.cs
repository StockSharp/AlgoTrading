using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Four Bar Momentum Reversal strategy.
/// Counts consecutive candles whose close is below the close from Lookback bars ago. Once the count reaches BuyThreshold inside the
/// StartTime..EndTime window the strategy buys, and it closes the long when the close breaks above the previous candle high.
/// </summary>
public class FourBarMomentumReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _buyThreshold;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<DateTimeOffset> _startTime;
	private readonly StrategyParam<DateTimeOffset> _endTime;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _closes = [];
	private int _belowCount;
	private decimal? _prevHigh;

	/// <summary>
	/// Consecutive closes below the reference close required to buy.
	/// </summary>
	public int BuyThreshold
	{
		get => _buyThreshold.Value;
		set => _buyThreshold.Value = value;
	}

	/// <summary>
	/// How many bars back the reference close is taken.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	/// <summary>
	/// Beginning of the trading window.
	/// </summary>
	public DateTimeOffset StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// End of the trading window.
	/// </summary>
	public DateTimeOffset EndTime
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
	public FourBarMomentumReversalStrategy()
	{
		_buyThreshold = Param(nameof(BuyThreshold), 4)
			.SetGreaterThanZero()
			.SetDisplay("Buy Threshold", "Consecutive closes below the reference close required to buy", "Strategy");

		_lookback = Param(nameof(Lookback), 4)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "How many bars back the reference close is taken", "Strategy");

		_startTime = Param(nameof(StartTime), new DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Time", "Beginning of the trading window", "General");

		_endTime = Param(nameof(EndTime), new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("End Time", "End of the trading window", "General");

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
		_belowCount = 0;
		_prevHigh = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_closes.Clear();
		_belowCount = 0;
		_prevHigh = null;

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

		var close = candle.ClosePrice;
		var prevHigh = _prevHigh;
		_prevHigh = candle.HighPrice;

		// _closes holds the previous Lookback closes, so its first element is the close from Lookback bars ago.
		var ready = _closes.Count >= Lookback;
		if (ready)
			_belowCount = close < _closes[0] ? _belowCount + 1 : 0;

		_closes.Add(close);
		if (_closes.Count > Lookback)
			_closes.RemoveAt(0);

		if (!ready || !IsFormedAndOnlineAndAllowTrading())
			return;

		var inWindow = candle.OpenTime >= StartTime.UtcDateTime && candle.OpenTime <= EndTime.UtcDateTime;

		if (Position == 0 && inWindow && _belowCount >= BuyThreshold)
			BuyMarket(Volume);
		else if (Position > 0 && prevHigh is decimal high && close > high)
			SellMarket(Position);
	}
}

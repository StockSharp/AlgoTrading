namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Short-only strategy based on EMA trend filters and Fibonacci levels.
/// </summary>
public class CCTrendStrategy2DowntrendShortStrategy : Strategy
{
	private readonly StrategyParam<int> _fibLength;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema9;
	private ExponentialMovingAverage _ema21;
	private ExponentialMovingAverage _ema55;
	private ExponentialMovingAverage _ema200;
	private Highest _highest;
	private Lowest _lowest;

	private decimal _prevClose;
	private decimal _prevEma200;
	private bool _initialized;

	/// <summary>
	/// Constructor.
	/// </summary>
	public CCTrendStrategy2DowntrendShortStrategy()
	{
		_fibLength = Param(nameof(FibLength), 100)
		.SetGreaterThanZero()
		.SetDisplay("Fibonacci Length", "Length for Fibonacci levels", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <summary>
	/// Length for Fibonacci calculations.
	/// </summary>
	public int FibLength
	{
		get => _fibLength.Value;
		set => _fibLength.Value = value;
	}

	/// <summary>
	/// Type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema9 = new ExponentialMovingAverage { Length = 9 };
		_ema21 = new ExponentialMovingAverage { Length = 21 };
		_ema55 = new ExponentialMovingAverage { Length = 55 };
		_ema200 = new ExponentialMovingAverage { Length = 200 };

		_highest = new Highest { Length = FibLength };
		_lowest = new Lowest { Length = FibLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_ema9, _ema21, _ema55, _ema200, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema9);
			DrawIndicator(area, _ema21);
			DrawIndicator(area, _ema55);
			DrawIndicator(area, _ema200);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema9Value, decimal ema21Value, decimal ema55Value, decimal ema200Value)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_ema9.IsFormed || !_ema21.IsFormed || !_ema55.IsFormed || !_ema200.IsFormed)
		return;

		var maxEma = Math.Max(ema55Value, ema9Value);
		var minEma = Math.Min(ema55Value, ema9Value);

		var highF = _highest.Process(maxEma, candle.OpenTime, true).ToDecimal();
		var lowF = _lowest.Process(minEma, candle.OpenTime, true).ToDecimal();

		if (!_highest.IsFormed || !_lowest.IsFormed)
		return;

		var avgFib = highF - lowF;
		var l236 = highF - 0.236m * avgFib;

		var close = candle.ClosePrice;

		if (!_initialized)
		{
			_prevClose = close;
			_prevEma200 = ema200Value;
			_initialized = true;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevClose = close;
			_prevEma200 = ema200Value;
			return;
		}

		var shortCondition = _prevClose < highF && ema21Value < ema55Value;

		if (shortCondition && Position >= 0)
		SellMarket();

		if (Position < 0)
		{
			var shortTp = _prevClose <= _prevEma200 && close > ema200Value && PnL >= 0m;
			var shortClose2 = _prevClose > l236 && !shortCondition;

			if (shortTp || shortClose2)
			BuyMarket();
		}

		_prevClose = close;
		_prevEma200 = ema200Value;
	}
}

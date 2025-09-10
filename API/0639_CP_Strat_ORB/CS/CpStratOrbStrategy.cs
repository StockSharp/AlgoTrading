using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// New York opening range breakout with retest.
/// </summary>
public class CpStratOrbStrategy : Strategy
{
	private readonly StrategyParam<decimal> _minRangePoints;
	private readonly StrategyParam<decimal> _stopPoints;
	private readonly StrategyParam<decimal> _takePoints;
	private readonly StrategyParam<int> _maxTradesPerSession;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _nyHigh;
	private decimal? _nyLow;
	private bool _nyRangeDone;
	private int _nyTradeCount;
	private DateTime _currentDay;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	private readonly TimeZoneInfo _nyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

	/// <summary>
	/// Minimum range in points.
	/// </summary>
	public decimal MinRangePoints
	{
		get => _minRangePoints.Value;
		set => _minRangePoints.Value = value;
	}

	/// <summary>
	/// Stop loss in points.
	/// </summary>
	public decimal StopPoints
	{
		get => _stopPoints.Value;
		set => _stopPoints.Value = value;
	}

	/// <summary>
	/// Take profit in points.
	/// </summary>
	public decimal TakePoints
	{
		get => _takePoints.Value;
		set => _takePoints.Value = value;
	}

	/// <summary>
	/// Maximum trades per NY session.
	/// </summary>
	public int MaxTradesPerSession
	{
		get => _maxTradesPerSession.Value;
		set => _maxTradesPerSession.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public CpStratOrbStrategy()
	{
		_minRangePoints = Param(nameof(MinRangePoints), 60m)
			.SetGreaterThanZero()
			.SetDisplay("Min Range", "Minimum NY range in points", "General")
			.SetCanOptimize(true)
			.SetOptimize(10m, 100m, 10m);

		_stopPoints = Param(nameof(StopPoints), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 40m, 5m);

		_takePoints = Param(nameof(TakePoints), 60m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20m, 120m, 10m);

		_maxTradesPerSession = Param(nameof(MaxTradesPerSession), 3)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades", "Max trades per NY session", "General")
			.SetCanOptimize(false);

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
		_nyHigh = null;
		_nyLow = null;
		_nyRangeDone = false;
		_nyTradeCount = 0;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
		_currentDay = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

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

		var nyTime = TimeZoneInfo.ConvertTime(candle.OpenTime.UtcDateTime, _nyTimeZone);

		if (_currentDay != nyTime.Date)
		{
			_currentDay = nyTime.Date;
			_nyHigh = null;
			_nyLow = null;
			_nyRangeDone = false;
			_nyTradeCount = 0;
		}

		if (nyTime.Hour == 9 && nyTime.Minute >= 30 && nyTime.Minute < 45)
		{
			_nyHigh = _nyHigh.HasValue ? Math.Max(_nyHigh.Value, candle.HighPrice) : candle.HighPrice;
			_nyLow = _nyLow.HasValue ? Math.Min(_nyLow.Value, candle.LowPrice) : candle.LowPrice;
		}

		if (nyTime.Hour == 9 && nyTime.Minute == 45 && !_nyRangeDone && _nyHigh.HasValue && _nyLow.HasValue)
			_nyRangeDone = true;

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice)
			{
				SellMarket(Math.Abs(Position));
				ResetPosition();
				return;
			}

			if (candle.HighPrice >= _takePrice)
			{
				SellMarket(Math.Abs(Position));
				ResetPosition();
				return;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice)
			{
				BuyMarket(Math.Abs(Position));
				ResetPosition();
				return;
			}

			if (candle.LowPrice <= _takePrice)
			{
				BuyMarket(Math.Abs(Position));
				ResetPosition();
				return;
			}
		}

		if (!_nyRangeDone || _nyHigh is null || _nyLow is null)
			return;

		var range = _nyHigh.Value - _nyLow.Value;
		if (range < MinRangePoints)
			return;

		if (_nyTradeCount >= MaxTradesPerSession)
			return;

		var longBreakout = candle.HighPrice > _nyHigh.Value;
		var longRetest = longBreakout && candle.LowPrice <= _nyHigh.Value && candle.ClosePrice > _nyHigh.Value;

		if (longRetest && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
			_stopPrice = _entryPrice - StopPoints;
			_takePrice = _entryPrice + TakePoints;
			_nyTradeCount++;
			return;
		}

		var shortBreakout = candle.LowPrice < _nyLow.Value;
		var shortRetest = shortBreakout && candle.HighPrice >= _nyLow.Value && candle.ClosePrice < _nyLow.Value;

		if (shortRetest && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
			_stopPrice = _entryPrice + StopPoints;
			_takePrice = _entryPrice - TakePoints;
			_nyTradeCount++;
		}
	}

	private void ResetPosition()
	{
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
	}
}

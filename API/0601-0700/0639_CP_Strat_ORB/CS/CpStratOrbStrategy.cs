using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// CP Strat ORB strategy.
/// The New York opening range spans 9:30-9:45 local exchange time and is traded only when it is at least MinRangePoints wide.
/// After a close above the range high, a later candle that dips back to the high and closes above it buys; after a close below
/// the range low, a candle that rallies back to the low and closes below it sells short. At most MaxTradesPerSession entries
/// are taken per day and every trade has a fixed StopPoints stop and TakePoints target.
/// </summary>
public class CpStratOrbStrategy : Strategy
{
	private static readonly TimeSpan _rangeStart = new(9, 30, 0);
	private static readonly TimeSpan _rangeEnd = new(9, 45, 0);
	private static readonly TimeZoneInfo _newYork = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

	private readonly StrategyParam<decimal> _minRangePoints;
	private readonly StrategyParam<decimal> _stopPoints;
	private readonly StrategyParam<decimal> _takePoints;
	private readonly StrategyParam<int> _maxTradesPerSession;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime? _sessionDate;
	private decimal? _rangeHigh;
	private decimal? _rangeLow;
	private bool _brokeUp;
	private bool _brokeDown;
	private int _tradesToday;

	/// <summary>
	/// Minimum opening range width in points.
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
	/// Maximum entries per session.
	/// </summary>
	public int MaxTradesPerSession
	{
		get => _maxTradesPerSession.Value;
		set => _maxTradesPerSession.Value = value;
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
	public CpStratOrbStrategy()
	{
		_minRangePoints = Param(nameof(MinRangePoints), 60m)
			.SetNotNegative()
			.SetDisplay("Min Range", "Minimum opening range width in points", "Range");

		_stopPoints = Param(nameof(StopPoints), 20m)
			.SetNotNegative()
			.SetDisplay("Stop Points", "Stop loss in points", "Risk");

		_takePoints = Param(nameof(TakePoints), 60m)
			.SetNotNegative()
			.SetDisplay("Take Points", "Take profit in points", "Risk");

		_maxTradesPerSession = Param(nameof(MaxTradesPerSession), 3)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades", "Maximum entries per session", "Risk");

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
		_sessionDate = null;
		ResetSession();
	}

	private void ResetSession()
	{
		_rangeHigh = null;
		_rangeLow = null;
		_brokeUp = false;
		_brokeDown = false;
		_tradesToday = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_sessionDate = null;
		ResetSession();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var step = Security?.PriceStep ?? 1m;
		StartProtection(
			TakePoints > 0 ? new Unit(TakePoints * step, UnitTypes.Absolute) : new Unit(),
			StopPoints > 0 ? new Unit(StopPoints * step, UnitTypes.Absolute) : new Unit(),
			useMarketOrders: true);

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

		var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(candle.OpenTime, DateTimeKind.Utc), _newYork);

		if (_sessionDate != local.Date)
		{
			_sessionDate = local.Date;
			ResetSession();
		}

		var timeOfDay = local.TimeOfDay;

		if (timeOfDay < _rangeStart)
			return;

		if (timeOfDay < _rangeEnd)
		{
			_rangeHigh = _rangeHigh is decimal high ? Math.Max(high, candle.HighPrice) : candle.HighPrice;
			_rangeLow = _rangeLow is decimal low ? Math.Min(low, candle.LowPrice) : candle.LowPrice;
			return;
		}

		if (_rangeHigh is not decimal rangeHigh || _rangeLow is not decimal rangeLow)
			return;

		var step = Security?.PriceStep ?? 1m;
		if (rangeHigh - rangeLow < MinRangePoints * step)
			return;

		var close = candle.ClosePrice;

		var longSignal = _brokeUp && candle.LowPrice <= rangeHigh && close > rangeHigh;
		var shortSignal = _brokeDown && candle.HighPrice >= rangeLow && close < rangeLow;

		if (close > rangeHigh)
			_brokeUp = true;

		if (close < rangeLow)
			_brokeDown = true;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0 || _tradesToday >= MaxTradesPerSession)
			return;

		if (longSignal)
		{
			BuyMarket(Volume);
			_tradesToday++;
			_brokeUp = false;
		}
		else if (shortSignal)
		{
			SellMarket(Volume);
			_tradesToday++;
			_brokeDown = false;
		}
	}
}

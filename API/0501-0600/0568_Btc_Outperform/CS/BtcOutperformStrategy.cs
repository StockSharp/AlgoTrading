namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>Compares completed fixed-duration weekly and quarterly closes.</summary>
public class BtcOutperformStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _weeklyDays;
	private readonly StrategyParam<int> _quarterlyDays;
	private DateTime _week, _quarter, _lastTime;
	private int _weekCount, _quarterCount;
	private decimal? _weeklyClose, _quarterlyClose;
	private Order _pending;

	/// <summary>Finished source candle interval.</summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>UTC Monday-anchored weekly period in days.</summary>
	public int WeeklyDays
	{
		get => _weeklyDays.Value;
		set => _weeklyDays.Value = value;
	}

	/// <summary>UTC epoch-anchored long period in days.</summary>
	public int QuarterlyDays
	{
		get => _quarterlyDays.Value;
		set => _quarterlyDays.Value = value;
	}

	/// <summary>Initializes the seven-day and ninety-day periods.</summary>
	public BtcOutperformStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("CandleType", "Finished source candle interval", "Parameters");
		_weeklyDays = Param(nameof(WeeklyDays), 7).SetGreaterThanZero()
			.SetDisplay("WeeklyDays", "UTC Monday-anchored weekly period in days", "Parameters");
		_quarterlyDays = Param(nameof(QuarterlyDays), 90).SetGreaterThanZero()
			.SetDisplay("QuarterlyDays", "UTC epoch-anchored long period in days", "Parameters");
		OrderRegistering += order => _pending = order;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_week = _quarter = _lastTime = default;
		_weekCount = _quarterCount = 0;
		_weeklyClose = _quarterlyClose = null;
		_pending = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		if (!CandleType.Equals(TimeSpan.FromDays(1).TimeFrame()) || WeeklyDays >= QuarterlyDays)
			throw new InvalidOperationException("Daily source candles and a weekly period shorter than the quarterly period are required.");
		base.OnStarted2(time);
		var candles = SubscribeCandles(CandleType);
		candles.Bind(ProcessCandle).Start();
		var area = CreateChartArea();
		if (area is not null)
		{
			DrawCandles(area, candles);
			DrawOwnTrades(area);
		}
	}

	private static DateTime Bucket(DateTime date, DateTime epoch, int days)
		=> epoch.AddDays(Math.Floor((date - epoch).TotalDays / days) * days);

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || candle.OpenTime <= _lastTime)
			return;
		var date = candle.OpenTime.Date;
		var contiguous = _lastTime == default || date == _lastTime.AddDays(1);
		var week = Bucket(date, new DateTime(1970, 1, 5, 0, 0, 0, DateTimeKind.Utc), WeeklyDays);
		var quarter = Bucket(date, DateTime.UnixEpoch, QuarterlyDays);
		_weekCount = week != _week ? date == week ? 1 : 0 : contiguous && _weekCount > 0 ? _weekCount + 1 : 0;
		_quarterCount = quarter != _quarter ? date == quarter ? 1 : 0 : contiguous && _quarterCount > 0 ? _quarterCount + 1 : 0;
		_week = week;
		_quarter = quarter;
		_lastTime = date;
		if (_weekCount == WeeklyDays && date.AddDays(1) == week.AddDays(WeeklyDays))
			_weeklyClose = candle.ClosePrice;
		if (_quarterCount == QuarterlyDays && date.AddDays(1) == quarter.AddDays(QuarterlyDays))
			_quarterlyClose = candle.ClosePrice;
		if (_weeklyClose is null || _quarterlyClose is null || !IsFormedAndOnlineAndAllowTrading()
			|| _pending is not null && _pending.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		if (_weeklyClose > _quarterlyClose && Position <= 0m)
			BuyMarket(Volume + Math.Abs(Position));
		else if (_weeklyClose < _quarterlyClose && Position >= 0m)
			SellMarket(Volume + Math.Abs(Position));
	}
}

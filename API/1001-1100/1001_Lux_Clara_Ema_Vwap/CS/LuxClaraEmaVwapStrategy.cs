using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Lux Clara EMA + VWAP strategy.
/// Goes long when the fast EMA crosses above the slow EMA while the slow EMA is above the session VWAP, and short on the opposite
/// cross with the slow EMA below VWAP. Entries are taken only between StartTime and EndTime (UTC). A position closes on the opposite
/// EMA cross, which reverses it when the opposite entry conditions are also met.
/// </summary>
public class LuxClaraEmaVwapStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<TimeSpan> _endTime;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevFast;
	private decimal? _prevSlow;
	private DateTime? _vwapDay;
	private decimal _cumPriceVolume;
	private decimal _cumVolume;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowEmaLength
	{
		get => _slowEmaLength.Value;
		set => _slowEmaLength.Value = value;
	}

	/// <summary>
	/// Session start time (UTC).
	/// </summary>
	public TimeSpan StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// Session end time (UTC).
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
	public LuxClaraEmaVwapStrategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA Length", "Period of the fast EMA", "Indicators");

		_slowEmaLength = Param(nameof(SlowEmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA Length", "Period of the slow EMA", "Indicators");

		_startTime = Param(nameof(StartTime), new TimeSpan(7, 30, 0))
			.SetDisplay("Start Time", "Session start time (UTC)", "Session");

		_endTime = Param(nameof(EndTime), new TimeSpan(14, 30, 0))
			.SetDisplay("End Time", "Session end time (UTC)", "Session");

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
		_prevFast = null;
		_prevSlow = null;
		_vwapDay = null;
		_cumPriceVolume = 0m;
		_cumVolume = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fastEma, slowEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// VWAP is anchored to the start of each UTC day.
		var day = candle.OpenTime.Date;
		if (_vwapDay != day)
		{
			_vwapDay = day;
			_cumPriceVolume = 0m;
			_cumVolume = 0m;
		}

		var typical = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
		_cumPriceVolume += typical * candle.TotalVolume;
		_cumVolume += candle.TotalVolume;

		if (!fastValue.IsFormed || !slowValue.IsFormed)
			return;

		var fast = fastValue.GetValue<decimal>();
		var slow = slowValue.GetValue<decimal>();

		var prevFast = _prevFast;
		var prevSlow = _prevSlow;
		_prevFast = fast;
		_prevSlow = slow;

		if (prevFast is not decimal pf || prevSlow is not decimal ps || _cumVolume <= 0m)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var vwap = _cumPriceVolume / _cumVolume;
		var crossUp = pf <= ps && fast > slow;
		var crossDown = pf >= ps && fast < slow;

		var timeOfDay = candle.OpenTime.TimeOfDay;
		var inSession = timeOfDay >= StartTime && timeOfDay < EndTime;

		if (crossUp)
		{
			if (inSession && slow > vwap && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (Position < 0)
				BuyMarket(-Position);
		}
		else if (crossDown)
		{
			if (inSession && slow < vwap && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
			else if (Position > 0)
				SellMarket(Position);
		}
	}
}

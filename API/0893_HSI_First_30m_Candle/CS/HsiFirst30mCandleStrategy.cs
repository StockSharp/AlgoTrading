using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades breakouts of the first 30-minute range on a 5-minute chart.
/// </summary>
public class HsiFirst30mCandleStrategy : Strategy
{
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<DataType> _candleType;
	private readonly TimeZoneInfo _hkTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Hong_Kong");

	private decimal? _firstHigh;
	private decimal? _firstLow;
	private bool _rangeLocked;
	private DateTime _currentDay;
	private bool _tradedToday;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// Risk/reward ratio.
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
	/// Initializes a new instance of the <see cref="HsiFirst30mCandleStrategy"/> class.
	/// </summary>
	public HsiFirst30mCandleStrategy()
	{
		_riskReward = Param(nameof(RiskReward), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Risk/Reward", "Take profit to stop ratio", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 3m, 0.5m);

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
		_firstHigh = null;
		_firstLow = null;
		_rangeLocked = false;
		_tradedToday = false;
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
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var hkTime = TimeZoneInfo.ConvertTime(candle.OpenTime.UtcDateTime, _hkTimeZone);

		if (_currentDay != hkTime.Date)
		{
			_currentDay = hkTime.Date;
			_firstHigh = null;
			_firstLow = null;
			_rangeLocked = false;
			_tradedToday = false;
		}

		if (hkTime.Hour == 9 && hkTime.Minute >= 15 && hkTime.Minute < 45)
		{
			_firstHigh = _firstHigh.HasValue ? Math.Max(_firstHigh.Value, candle.HighPrice) : candle.HighPrice;
			_firstLow = _firstLow.HasValue ? Math.Min(_firstLow.Value, candle.LowPrice) : candle.LowPrice;
		}

		if (!_rangeLocked && hkTime.Hour == 9 && hkTime.Minute >= 45 && _firstHigh.HasValue && _firstLow.HasValue)
		_rangeLocked = true;

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
			SellMarket(Math.Abs(Position));
			return;
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
			BuyMarket(Math.Abs(Position));
			return;
		}

		if (!_rangeLocked || _tradedToday || _firstHigh is null || _firstLow is null)
		return;

		var time = hkTime.TimeOfDay;
		var inSession = (time >= new TimeSpan(9, 15, 0) && time <= new TimeSpan(12, 0, 0)) ||
		(time >= new TimeSpan(13, 0, 0) && time <= new TimeSpan(16, 0, 0));

		if (!inSession)
		return;

		var range = _firstHigh.Value - _firstLow.Value;

		if (candle.HighPrice >= _firstHigh.Value)
		{
			BuyStop(Volume, _firstHigh.Value);
			_stopPrice = _firstLow.Value;
			_takePrice = _firstHigh.Value + range * RiskReward;
			_tradedToday = true;
		}
		else if (candle.LowPrice <= _firstLow.Value)
		{
			SellStop(Volume, _firstLow.Value);
			_stopPrice = _firstHigh.Value;
			_takePrice = _firstLow.Value - range * RiskReward;
			_tradedToday = true;
		}
	}
}


using System;
using System.Collections.Generic;

using Ecng.Common;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Enters when the first candle's open equals its high or low.
/// </summary>
public class IuOpenEqualToHighLowStrategy : Strategy
{
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<int> _cooldownDays;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime _currentDay;
	private DateTime _nextEntryDate;
	private decimal _stopPrice;
	private decimal _takePrice;
	private ICandleMessage _prevCandle;

	/// <summary>
	/// Risk/reward ratio.
	/// </summary>
	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
	}

	/// <summary>
	/// Minimum days between entries.
	/// </summary>
	public int CooldownDays
	{
		get => _cooldownDays.Value;
		set => _cooldownDays.Value = value;
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
	/// Initializes a new instance of the <see cref="IuOpenEqualToHighLowStrategy"/> class.
	/// </summary>
	public IuOpenEqualToHighLowStrategy()
	{
		_riskReward = Param(nameof(RiskReward), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Risk/Reward", "Take profit to stop ratio", "Risk")
			
			.SetOptimize(1m, 5m, 1m);

		_cooldownDays = Param(nameof(CooldownDays), 1)
			.SetDisplay("Cooldown Days", "Minimum number of days between new entries", "Risk");

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
		_currentDay = default;
		_nextEntryDate = DateTime.MinValue;
		_stopPrice = 0m;
		_takePrice = 0m;
		_prevCandle = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var dummyEma1 = new StockSharp.Algo.Indicators.ExponentialMovingAverage { Length = 10 };
		var dummyEma2 = new StockSharp.Algo.Indicators.ExponentialMovingAverage { Length = 20 };
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(dummyEma1, dummyEma2, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal d1, decimal d2)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var day = candle.OpenTime.Date;

		if (_currentDay != day)
		{
			_currentDay = day;

			if (Position == 0 && _prevCandle != null && day >= _nextEntryDate)
			{
				var entryPrice = candle.OpenPrice;
				var tolerance = candle.OpenPrice * 0.005m;
				var isOpenNearLow = candle.OpenPrice - candle.LowPrice <= tolerance;
				var isOpenNearHigh = candle.HighPrice - candle.OpenPrice <= tolerance;

				if (isOpenNearLow)
				{
					_stopPrice = _prevCandle.LowPrice;
					_takePrice = entryPrice + (entryPrice - _stopPrice) * RiskReward;
					BuyMarket();
					_nextEntryDate = day.AddDays(CooldownDays);
				}
				else if (isOpenNearHigh)
				{
					_stopPrice = _prevCandle.HighPrice;
					_takePrice = entryPrice - (_stopPrice - entryPrice) * RiskReward;
					SellMarket();
					_nextEntryDate = day.AddDays(CooldownDays);
				}
			}
		}

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
			{
				SellMarket();
				_stopPrice = 0m;
				_takePrice = 0m;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
			{
				BuyMarket();
				_stopPrice = 0m;
				_takePrice = 0m;
			}
		}

		_prevCandle = candle;
	}
}

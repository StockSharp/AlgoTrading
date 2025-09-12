
using System;
using System.Collections.Generic;

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
	private readonly StrategyParam<DataType> _candleType;

	private DateTime _currentDay;
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
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);

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
		_currentDay = default;
		_stopPrice = 0m;
		_takePrice = 0m;
		_prevCandle = null;
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

		var day = candle.OpenTime.UtcDateTime.Date;

		if (_currentDay != day)
		{
			_currentDay = day;

			if (Position == 0 && _prevCandle != null)
			{
				var entryPrice = candle.OpenPrice;

				if (candle.OpenPrice == candle.LowPrice)
				{
					_stopPrice = _prevCandle.LowPrice;
					_takePrice = entryPrice + (entryPrice - _stopPrice) * RiskReward;
					BuyMarket();
				}
				else if (candle.OpenPrice == candle.HighPrice)
				{
					_stopPrice = _prevCandle.HighPrice;
					_takePrice = entryPrice - (_stopPrice - entryPrice) * RiskReward;
					SellMarket();
				}
			}
		}

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
			{
				SellMarket(Math.Abs(Position));
				_stopPrice = 0m;
				_takePrice = 0m;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
			{
				BuyMarket(Math.Abs(Position));
				_stopPrice = 0m;
				_takePrice = 0m;
			}
		}

		_prevCandle = candle;
	}
}

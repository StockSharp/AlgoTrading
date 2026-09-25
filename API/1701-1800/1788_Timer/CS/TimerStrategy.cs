using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Timer ATR breakout strategy.
/// </summary>
public class TimerStrategy : Strategy
{
	private readonly StrategyParam<int> _waitSeconds;
	private readonly StrategyParam<decimal> _pipDistance;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _useTradingHours;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<TimeSpan> _stopTime;

	private DateTimeOffset? _lastLevelTime;
	private decimal? _buyLevel;
	private decimal? _sellLevel;

	public int WaitSeconds { get => _waitSeconds.Value; set => _waitSeconds.Value = value; }
	public decimal PipDistance { get => _pipDistance.Value; set => _pipDistance.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }
	public decimal TradeVolume { get => _tradeVolume.Value; set => _tradeVolume.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public bool UseTradingHours { get => _useTradingHours.Value; set => _useTradingHours.Value = value; }
	public TimeSpan StartTime { get => _startTime.Value; set => _startTime.Value = value; }
	public TimeSpan StopTime { get => _stopTime.Value; set => _stopTime.Value = value; }

	public TimerStrategy()
	{
		_waitSeconds = Param(nameof(WaitSeconds), 60).SetGreaterThanZero();
		_pipDistance = Param(nameof(PipDistance), 10m).SetNotNegative();
		_atrPeriod = Param(nameof(AtrPeriod), 14).SetGreaterThanZero();
		_takeProfit = Param(nameof(TakeProfit), 100m).SetNotNegative();
		_stopLoss = Param(nameof(StopLoss), 50m).SetNotNegative();
		_trailingStop = Param(nameof(TrailingStop), 0m).SetNotNegative();
		_tradeVolume = Param(nameof(TradeVolume), 1m).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame());
		_useTradingHours = Param(nameof(UseTradingHours), false);
		_startTime = Param(nameof(StartTime), TimeSpan.Zero);
		_stopTime = Param(nameof(StopTime), new TimeSpan(23, 59, 59));
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_lastLevelTime = null;
		_buyLevel = null;
		_sellLevel = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var point = Security?.PriceStep ?? 1m;
		if (point <= 0m)
			point = 1m;

		var take = TakeProfit > 0m ? new Unit(TakeProfit * point, UnitTypes.Absolute) : null;

		// StockSharp exposes one stop offset. When TrailingStop is enabled it becomes
		// the trailing stop distance; otherwise StopLoss is the fixed stop distance.
		var stopPoints = TrailingStop > 0m ? TrailingStop : StopLoss;
		var stop = stopPoints > 0m ? new Unit(stopPoints * point, UnitTypes.Absolute) : null;

		if (take is not null || stop is not null)
			StartProtection(take, stop, isStopTrailing: TrailingStop > 0m, useMarketOrders: true);

		var atr = new AverageTrueRange { Length = AtrPeriod };
		SubscribeCandles(CandleType)
			.Bind(atr, (candle, atrValue) =>
			{
				if (candle.State != CandleStates.Finished || !atr.IsFormed || atrValue <= 0m)
					return;

				ProcessCandle(candle, atrValue);
			})
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		var now = candle.CloseTime;

		if (IsTradingTime(now.TimeOfDay))
		{
			if (_buyLevel is decimal buy && candle.ClosePrice >= buy && Position <= 0m)
			{
				BuyMarket(TradeVolume + Math.Abs(Position));
				_buyLevel = null;
				_sellLevel = null;
			}
			else if (_sellLevel is decimal sell && candle.ClosePrice <= sell && Position >= 0m)
			{
				SellMarket(TradeVolume + Math.Abs(Position));
				_buyLevel = null;
				_sellLevel = null;
			}
		}

		if (_lastLevelTime is null || now - _lastLevelTime.Value >= TimeSpan.FromSeconds(WaitSeconds))
		{
			var point = Security?.PriceStep ?? 1m;
			if (point <= 0m)
				point = 1m;

			(_buyLevel, _sellLevel) = CalculateLevels(candle.ClosePrice, PipDistance, point, atr);
			_lastLevelTime = now;
		}
	}

	private bool IsTradingTime(TimeSpan time)
	{
		if (!UseTradingHours)
			return true;

		return StartTime <= StopTime
			? time >= StartTime && time <= StopTime
			: time >= StartTime || time <= StopTime;
	}

	internal static (decimal buy, decimal sell) CalculateLevels(
		decimal close,
		decimal pipDistancePoints,
		decimal priceStep,
		decimal atr)
	{
		var distance = pipDistancePoints * priceStep + atr;
		return (close + distance, close - distance);
	}
}

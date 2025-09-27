using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader strategy up3x1_Investor.
/// </summary>
public class Up3x1InvestorRangeFilterStrategy : Strategy
{
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _maximumRisk;
	private readonly StrategyParam<decimal> _decreaseFactor;
	private readonly StrategyParam<decimal> _minimumVolume;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<bool> _skipMondays;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _sma24;
	private SimpleMovingAverage _sma60;

	private ICandleMessage _previousCandle;
	private decimal _priceStep;
	private decimal _entryPrice;
	private int _positionDirection;
	private int _lossStreak;
	private decimal? _longStop;
	private decimal? _longTarget;
	private decimal? _shortStop;
	private decimal? _shortTarget;

	/// <summary>
/// Initializes a new instance of the <see cref="Up3x1InvestorRangeFilterStrategy"/> class.
/// </summary>
public Up3x1InvestorRangeFilterStrategy()
	{
		_baseVolume = Param(nameof(BaseVolume), 0.1m);
		_maximumRisk = Param(nameof(MaximumRisk), 0.2m);
		_decreaseFactor = Param(nameof(DecreaseFactor), 3m);
		_minimumVolume = Param(nameof(MinimumVolume), 0.1m);
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 20m);
		_stopLossPoints = Param(nameof(StopLossPoints), 50m);
		_trailingStopPoints = Param(nameof(TrailingStopPoints), 10m);
		_skipMondays = Param(nameof(SkipMondays), true);
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame());
	}

	public decimal BaseVolume { get => _baseVolume.Value; set => _baseVolume.Value = value; }
	public decimal MaximumRisk { get => _maximumRisk.Value; set => _maximumRisk.Value = value; }
	public decimal DecreaseFactor { get => _decreaseFactor.Value; set => _decreaseFactor.Value = value; }
	public decimal MinimumVolume { get => _minimumVolume.Value; set => _minimumVolume.Value = value; }
	public decimal TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }
	public decimal StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }
	public decimal TrailingStopPoints { get => _trailingStopPoints.Value; set => _trailingStopPoints.Value = value; }
	public bool SkipMondays { get => _skipMondays.Value; set => _skipMondays.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 0.0001m;
		_sma24 = new SimpleMovingAverage { Length = 24 };
		_sma60 = new SimpleMovingAverage { Length = 60 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_sma24, _sma60, OnProcessCandle).Start();
	}

	private void OnProcessCandle(ICandleMessage candle, decimal ma24, decimal ma60)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (SkipMondays && candle.CloseTime.DayOfWeek == DayOfWeek.Monday)
		{
			_previousCandle = candle;
			return;
		}

		if (!_sma24.IsFormed || !_sma60.IsFormed)
		{
			_previousCandle = candle;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousCandle = candle;
			return;
		}

		if (Position == 0m)
			TryOpenPosition();
		else
			ManageOpenPosition(candle, ma24, ma60);

		_previousCandle = candle;
	}

	private void TryOpenPosition()
	{
		if (_previousCandle is null)
			return;

		var range = _previousCandle.HighPrice - _previousCandle.LowPrice;
		var body = Math.Abs(_previousCandle.OpenPrice - _previousCandle.ClosePrice);

		if (range <= 0m || body <= 0m)
			return;

		var volume = CalculateTradeVolume();
		if (volume <= 0m)
			return;

		var rangeThreshold = 0.0060m;
		var bodyThreshold = 0.0050m;

		if (range > rangeThreshold && _previousCandle.ClosePrice > _previousCandle.OpenPrice && body > bodyThreshold)
		{
			BuyMarket(volume);
			InitializeLongState(_previousCandle.ClosePrice);
		}
		else if (range > rangeThreshold && _previousCandle.ClosePrice < _previousCandle.OpenPrice && body > bodyThreshold)
		{
			SellMarket(volume);
			InitializeShortState(_previousCandle.ClosePrice);
		}
	}

	private void ManageOpenPosition(ICandleMessage candle, decimal ma24, decimal ma60)
	{
		if (Position > 0m)
		{
			var exitDueToMa = AreMovingAveragesEqual(ma24, ma60);
			if (exitDueToMa)
			{
				ClosePosition(candle.ClosePrice);
				return;
			}

			UpdateTrailingStopForLong(candle);

			if (_longTarget is decimal target && candle.HighPrice >= target)
			{
				ClosePosition(target);
				return;
			}

			if (_longStop is decimal stop && candle.LowPrice <= stop)
			{
				ClosePosition(stop);
				return;
			}
		}
		else if (Position < 0m)
		{
			var exitDueToMa = AreMovingAveragesEqual(ma24, ma60);
			if (exitDueToMa)
			{
				ClosePosition(candle.ClosePrice);
				return;
			}

			UpdateTrailingStopForShort(candle);

			if (_shortTarget is decimal target && candle.LowPrice <= target)
			{
				ClosePosition(target);
				return;
			}

			if (_shortStop is decimal stop && candle.HighPrice >= stop)
			{
				ClosePosition(stop);
				return;
			}
		}
	}

	private void InitializeLongState(decimal price)
	{
		_entryPrice = price;
		_positionDirection = 1;

		var takeProfit = ConvertPointsToPrice(TakeProfitPoints);
		var stopLoss = ConvertPointsToPrice(StopLossPoints);
		var trailingStop = ConvertPointsToPrice(TrailingStopPoints);

		_longTarget = takeProfit > 0m ? price + takeProfit : null;
		_longStop = stopLoss > 0m ? price - stopLoss : null;

		if (trailingStop <= 0m)
			return;

		var candidate = price - trailingStop;
		if (_longStop is null || candidate > _longStop)
			_longStop = candidate;
	}

	private void InitializeShortState(decimal price)
	{
		_entryPrice = price;
		_positionDirection = -1;

		var takeProfit = ConvertPointsToPrice(TakeProfitPoints);
		var stopLoss = ConvertPointsToPrice(StopLossPoints);
		var trailingStop = ConvertPointsToPrice(TrailingStopPoints);

		_shortTarget = takeProfit > 0m ? price - takeProfit : null;
		_shortStop = stopLoss > 0m ? price + stopLoss : null;

		if (trailingStop <= 0m)
			return;

		var candidate = price + trailingStop;
		if (_shortStop is null || candidate < _shortStop)
			_shortStop = candidate;
	}

	private void UpdateTrailingStopForLong(ICandleMessage candle)
	{
		var trailingDistance = ConvertPointsToPrice(TrailingStopPoints);
		if (trailingDistance <= 0m || _entryPrice <= 0m)
			return;

		if (candle.ClosePrice - _entryPrice <= trailingDistance)
			return;

		var candidate = candle.ClosePrice - trailingDistance;
		if (_longStop is null || candidate > _longStop)
			_longStop = candidate;
	}

	private void UpdateTrailingStopForShort(ICandleMessage candle)
	{
		var trailingDistance = ConvertPointsToPrice(TrailingStopPoints);
		if (trailingDistance <= 0m || _entryPrice <= 0m)
			return;

		if (_entryPrice - candle.ClosePrice <= trailingDistance)
			return;

		var candidate = candle.ClosePrice + trailingDistance;
		if (_shortStop is null || candidate < _shortStop)
			_shortStop = candidate;
	}

	private bool AreMovingAveragesEqual(decimal ma24, decimal ma60)
	{
		var tolerance = _priceStep;
		return Math.Abs(ma24 - ma60) <= tolerance;
	}

	private void ClosePosition(decimal exitPrice)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
		{
			ResetPositionState();
			return;
		}

		if (Position > 0m)
			SellMarket(volume);
		else
			BuyMarket(volume);

		if (_entryPrice > 0m)
		{
			var profit = _positionDirection > 0 ? exitPrice - _entryPrice : _entryPrice - exitPrice;
			if (profit < 0m)
				_lossStreak++;
			else if (profit > 0m)
				_lossStreak = 0;
		}

		ResetPositionState();
	}

	private void ResetPositionState()
	{
		_entryPrice = 0m;
		_positionDirection = 0;
		_longStop = null;
		_longTarget = null;
		_shortStop = null;
		_shortTarget = null;
	}

	private decimal CalculateTradeVolume()
	{
		var volume = BaseVolume;
		var accountValue = Portfolio?.CurrentValue;

		if (accountValue is decimal value && value > 0m && MaximumRisk > 0m)
		{
			var riskVolume = Math.Round(value * MaximumRisk / 1000m, 1, MidpointRounding.AwayFromZero);
			if (riskVolume > 0m)
				volume = riskVolume;
		}

		if (DecreaseFactor > 0m && _lossStreak > 1)
		{
			var reduction = volume * _lossStreak / DecreaseFactor;
			volume = Math.Max(volume - reduction, MinimumVolume);
		}

		if (volume < MinimumVolume)
			volume = MinimumVolume;

		return volume;
	}

	private decimal ConvertPointsToPrice(decimal points)
		=> points > 0m ? points * _priceStep : 0m;
}


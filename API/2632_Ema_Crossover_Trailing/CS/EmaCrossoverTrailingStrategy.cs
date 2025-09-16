using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover strategy with trailing stop logic converted from the MQL5 "Intersection 2 iMA" expert advisor.
/// The strategy opens trades on moving average crossovers and maintains a stepped trailing stop.
/// </summary>
public class EmaCrossoverTrailingStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _trailingStepPoints;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _tradeVolume;

	private ExponentialMovingAverage _fastEma = null!;
	private ExponentialMovingAverage _slowEma = null!;

	private decimal? _previousFastValue;
	private decimal? _previousSlowValue;

	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;

	private decimal _stopDistance;
	private decimal _stepDistance;

	/// <summary>
	/// Initializes <see cref="EmaCrossoverTrailingStrategy"/>.
	/// </summary>
	public EmaCrossoverTrailingStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 4)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Period of the fast exponential moving average", "Moving Averages")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 1);

		_slowPeriod = Param(nameof(SlowPeriod), 18)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Period of the slow exponential moving average", "Moving Averages")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 2);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 20m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (points)", "Distance from price to trailing stop expressed in price steps", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(5m, 40m, 5m);

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 5m)
			.SetNotNegative()
			.SetDisplay("Trailing Step (points)", "Minimum price advancement before the trailing stop is moved", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 10m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for calculations", "General");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume used for entries", "General");
	}

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in price steps.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Minimum move required before shifting the trailing stop.
	/// </summary>
	public decimal TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Volume used for market orders.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
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

		Volume = TradeVolume;
		_previousFastValue = null;
		_previousSlowValue = null;
		_longStopPrice = null;
		_shortStopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_fastEma = new ExponentialMovingAverage { Length = FastPeriod };
		_slowEma = new ExponentialMovingAverage { Length = SlowPeriod };

		_stopDistance = CalculateDistance(TrailingStopPoints);
		_stepDistance = CalculateDistance(TrailingStepPoints);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastEma, _slowEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastEma);
			DrawIndicator(area, _slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_stopDistance = CalculateDistance(TrailingStopPoints);
		_stepDistance = CalculateDistance(TrailingStepPoints);

		UpdateTrailingStops(candle);

		if (!_fastEma.IsFormed || !_slowEma.IsFormed)
		{
			_previousFastValue = fastValue;
			_previousSlowValue = slowValue;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousFastValue = fastValue;
			_previousSlowValue = slowValue;
			return;
		}

		if (_previousFastValue is null || _previousSlowValue is null)
		{
			_previousFastValue = fastValue;
			_previousSlowValue = slowValue;
			return;
		}

		var fastPrev = _previousFastValue.Value;
		var slowPrev = _previousSlowValue.Value;

		var crossedUp = fastPrev <= slowPrev && fastValue > slowValue;
		var crossedDown = fastPrev >= slowPrev && fastValue < slowValue;

		if (crossedUp && Position <= 0)
		{
			var volumeToBuy = TradeVolume;

			if (Position < 0)
				volumeToBuy += Math.Abs(Position);

			if (volumeToBuy > 0)
			{
				BuyMarket(volumeToBuy);
				InitializeLongTrailing(candle.ClosePrice);
			}
		}
		else if (crossedDown && Position >= 0)
		{
			var volumeToSell = TradeVolume;

			if (Position > 0)
				volumeToSell += Math.Abs(Position);

			if (volumeToSell > 0)
			{
				SellMarket(volumeToSell);
				InitializeShortTrailing(candle.ClosePrice);
			}
		}

		_previousFastValue = fastValue;
		_previousSlowValue = slowValue;
	}

	private decimal CalculateDistance(decimal points)
	{
		if (points <= 0m)
			return 0m;

		var priceStep = Security?.PriceStep ?? 0m;

		if (priceStep <= 0m)
			priceStep = 1m;

		return points * priceStep;
	}

	private void InitializeLongTrailing(decimal price)
	{
		if (_stopDistance <= 0m)
		{
			_longStopPrice = null;
			return;
		}

		_longStopPrice = price - _stopDistance;
		_shortStopPrice = null;
	}

	private void InitializeShortTrailing(decimal price)
	{
		if (_stopDistance <= 0m)
		{
			_shortStopPrice = null;
			return;
		}

		_shortStopPrice = price + _stopDistance;
		_longStopPrice = null;
	}

	private void UpdateTrailingStops(ICandleMessage candle)
	{
		if (_stopDistance <= 0m)
		{
			_longStopPrice = null;
			_shortStopPrice = null;
			return;
		}

		if (Position > 0)
		{
			if (_longStopPrice is null)
			{
				InitializeLongTrailing(candle.ClosePrice);
			}
			else
			{
				var newStop = candle.ClosePrice - _stopDistance;

				if (newStop - _longStopPrice.Value >= _stepDistance)
					_longStopPrice = newStop;

				if (candle.LowPrice <= _longStopPrice.Value)
				{
					SellMarket(Math.Abs(Position));
					_longStopPrice = null;
				}
			}
		}
		else if (Position < 0)
		{
			if (_shortStopPrice is null)
			{
				InitializeShortTrailing(candle.ClosePrice);
			}
			else
			{
				var newStop = candle.ClosePrice + _stopDistance;

				if (_shortStopPrice.Value - newStop >= _stepDistance)
					_shortStopPrice = newStop;

				if (candle.HighPrice >= _shortStopPrice.Value)
				{
					BuyMarket(Math.Abs(Position));
					_shortStopPrice = null;
				}
			}
		}
		else
		{
			_longStopPrice = null;
			_shortStopPrice = null;
		}
	}
}

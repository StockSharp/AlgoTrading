using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Counter-trend EMA crossover strategy converted from the MetaTrader 4 expert "EMA_CROSS_2".
/// Buys when the long EMA rises above the short EMA, and sells when the short EMA climbs above the long EMA.
/// Incorporates MetaTrader-style risk management with point-based stop-loss, take-profit, and trailing stop levels.
/// </summary>
public class EmaCross2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<int> _shortEmaPeriod;
	private readonly StrategyParam<int> _longEmaPeriod;

	private ExponentialMovingAverage? _shortEma;
	private ExponentialMovingAverage? _longEma;

	private bool _skipFirstSignal = true;
	private int _lastDirection;
	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;
	private decimal _pointSize;

	/// <summary>
	/// Candle type used for signal detection.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Order volume applied to new market orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in broker points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in broker points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in broker points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Period of the short EMA.
	/// </summary>
	public int ShortEmaPeriod
	{
		get => _shortEmaPeriod.Value;
		set => _shortEmaPeriod.Value = value;
	}

	/// <summary>
	/// Period of the long EMA.
	/// </summary>
	public int LongEmaPeriod
	{
		get => _longEmaPeriod.Value;
		set => _longEmaPeriod.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public EmaCross2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Time frame used for EMA calculations", "General");

		_orderVolume = Param(nameof(OrderVolume), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Volume of each market order", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 5m, 0.1m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 20m)
		.SetNotNegative()
		.SetDisplay("Take Profit (points)", "Distance from entry to take-profit in broker points", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0m, 200m, 5m);

		_stopLossPoints = Param(nameof(StopLossPoints), 30m)
		.SetNotNegative()
		.SetDisplay("Stop Loss (points)", "Distance from entry to stop-loss in broker points", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0m, 200m, 5m);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 50m)
		.SetNotNegative()
		.SetDisplay("Trailing Stop (points)", "Trailing distance maintained after entry", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0m, 200m, 5m);

		_shortEmaPeriod = Param(nameof(ShortEmaPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("Short EMA", "Length of the fast EMA", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(2, 40, 1);

		_longEmaPeriod = Param(nameof(LongEmaPeriod), 60)
		.SetGreaterThanZero()
		.SetDisplay("Long EMA", "Length of the slow EMA", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 200, 5);
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

		Volume = OrderVolume;
		_shortEma = null;
		_longEma = null;
		_skipFirstSignal = true;
		_lastDirection = 0;
		_stopLossPrice = null;
		_takeProfitPrice = null;
		_pointSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;
		_pointSize = CalculatePointSize();

		_shortEma = new ExponentialMovingAverage { Length = ShortEmaPeriod };
		_longEma = new ExponentialMovingAverage { Length = LongEmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_shortEma, _longEma, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _shortEma);
			DrawIndicator(area, _longEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal shortEmaValue, decimal longEmaValue)
	{
		// Work only with finished candles to avoid repeated signals inside the same bar.
		if (candle.State != CandleStates.Finished)
		return;

		if (_pointSize <= 0m)
		_pointSize = CalculatePointSize();

		if (CheckRisk(candle))
		return;

		if (Position != 0)
		UpdateTrailingStop(candle);
		else if (_stopLossPrice.HasValue || _takeProfitPrice.HasValue)
		ResetRiskLevels();

		var signal = EvaluateCross(longEmaValue, shortEmaValue);

		if (signal == 0)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (Position != 0)
		return;

		var volume = OrderVolume;
		if (volume <= 0m)
		volume = 1m;

		if (signal == 1)
		{
			BuyMarket(volume);
			SetRiskLevels(candle.ClosePrice, true);
		}
		else if (signal == 2)
		{
			SellMarket(volume);
			SetRiskLevels(candle.ClosePrice, false);
		}
	}

	private int EvaluateCross(decimal longValue, decimal shortValue)
	{
		var currentDirection = 0;

		if (longValue > shortValue)
		currentDirection = 1;
		else if (longValue < shortValue)
		currentDirection = 2;

		if (_skipFirstSignal)
		{
			_skipFirstSignal = false;
			return 0;
		}

		if (currentDirection != 0 && currentDirection != _lastDirection)
		{
			_lastDirection = currentDirection;
			return _lastDirection;
		}

		return 0;
	}

	private bool CheckRisk(ICandleMessage candle)
	{
		if (Position > 0)
		{
			var size = Math.Abs(Position);

			if (_stopLossPrice.HasValue && candle.LowPrice <= _stopLossPrice.Value)
			{
				SellMarket(size);
				ResetRiskLevels();
				return true;
			}

			if (_takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
			{
				SellMarket(size);
				ResetRiskLevels();
				return true;
			}
		}
		else if (Position < 0)
		{
			var size = Math.Abs(Position);

			if (_stopLossPrice.HasValue && candle.HighPrice >= _stopLossPrice.Value)
			{
				BuyMarket(size);
				ResetRiskLevels();
				return true;
			}

			if (_takeProfitPrice.HasValue && candle.LowPrice <= _takeProfitPrice.Value)
			{
				BuyMarket(size);
				ResetRiskLevels();
				return true;
			}
		}
		else if (_stopLossPrice.HasValue || _takeProfitPrice.HasValue)
		{
			ResetRiskLevels();
		}

		return false;
	}

	private void UpdateTrailingStop(ICandleMessage candle)
	{
		if (TrailingStopPoints <= 0m || _pointSize <= 0m)
		return;

		var distance = TrailingStopPoints * _pointSize;
		if (distance <= 0m)
		return;

		var entryPrice = PositionPrice ?? candle.ClosePrice;

		if (Position > 0)
		{
			var profit = candle.ClosePrice - entryPrice;
			if (profit > distance)
			{
				var candidate = candle.ClosePrice - distance;
				if (!_stopLossPrice.HasValue || _stopLossPrice.Value < candidate)
				_stopLossPrice = candidate;
			}
		}
		else if (Position < 0)
		{
			var profit = entryPrice - candle.ClosePrice;
			if (profit > distance)
			{
				var candidate = candle.ClosePrice + distance;
				if (!_stopLossPrice.HasValue || _stopLossPrice.Value > candidate)
				_stopLossPrice = candidate;
			}
		}
	}

	private void SetRiskLevels(decimal executionPrice, bool isLong)
	{
		if (_pointSize <= 0m)
		{
			ResetRiskLevels();
			return;
		}

		_stopLossPrice = StopLossPoints > 0m
		? executionPrice + (isLong ? -1m : 1m) * StopLossPoints * _pointSize
		: null;

		_takeProfitPrice = TakeProfitPoints > 0m
		? executionPrice + (isLong ? 1m : -1m) * TakeProfitPoints * _pointSize
		: null;
	}

	private void ResetRiskLevels()
	{
		_stopLossPrice = null;
		_takeProfitPrice = null;
	}

	private decimal CalculatePointSize()
	{
		var security = Security;
		if (security == null)
		return 0m;

		var step = security.PriceStep ?? 0m;
		var decimals = security.Decimals;

		if (decimals.HasValue && decimals.Value >= 0)
		{
			var point = (decimal)Math.Pow(10, -decimals.Value);
			if (point > 0m)
			{
				if (step > 0m)
				return Math.Min(step, point);

				return point;
			}
		}

		return step > 0m ? step : 0m;
	}
}

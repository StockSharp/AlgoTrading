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
/// Volatility Quality inspired strategy that reacts to direction changes in a smoothed median price line.
/// </summary>
public class VqEaStrategy : Strategy
{
	private readonly StrategyParam<int> _lengthParam;
	private readonly StrategyParam<int> _smoothingParam;
	private readonly StrategyParam<int> _filterPointsParam;
	private readonly StrategyParam<int> _stopLossPointsParam;
	private readonly StrategyParam<int> _takeProfitPointsParam;
	private readonly StrategyParam<int> _trailingStopPointsParam;
	private readonly StrategyParam<bool> _useTrailingParam;
	private readonly StrategyParam<DataType> _candleTypeParam;

	private SimpleMovingAverage _baseAverage;
	private SimpleMovingAverage _smoothingAverage;

	private decimal _previousSmoothed;
	private int _previousDirection;
	private bool _hasPrevious;
	private decimal _filter;
	private decimal _point;

	/// <summary>
	/// Base moving average length applied to the median price.
	/// </summary>
	public int Length
	{
		get => _lengthParam.Value;
		set => _lengthParam.Value = value;
	}

	/// <summary>
	/// Additional smoothing period applied to the base moving average.
	/// </summary>
	public int Smoothing
	{
		get => _smoothingParam.Value;
		set => _smoothingParam.Value = value;
	}

	/// <summary>
	/// Minimum movement in points required to confirm a direction change.
	/// </summary>
	public int FilterPoints
	{
		get => _filterPointsParam.Value;
		set => _filterPointsParam.Value = value;
	}

	/// <summary>
	/// Protective stop-loss distance expressed in points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPointsParam.Value;
		set => _stopLossPointsParam.Value = value;
	}

	/// <summary>
	/// Protective take-profit distance expressed in points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPointsParam.Value;
		set => _takeProfitPointsParam.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in points.
	/// </summary>
	public int TrailingStopPoints
	{
		get => _trailingStopPointsParam.Value;
		set => _trailingStopPointsParam.Value = value;
	}

	/// <summary>
	/// Enables or disables trailing stop usage.
	/// </summary>
	public bool UseTrailing
	{
		get => _useTrailingParam.Value;
		set => _useTrailingParam.Value = value;
	}

	/// <summary>
	/// Candle type used for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="VqEaStrategy"/> class.
	/// </summary>
	public VqEaStrategy()
	{
		_lengthParam = Param(nameof(Length), 5)
		.SetGreaterThanZero()
		.SetDisplay("Length", "Base smoothing period", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(3, 20, 1);

		_smoothingParam = Param(nameof(Smoothing), 1)
		.SetGreaterThanZero()
		.SetDisplay("Smoothing", "Additional smoothing period", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(1, 10, 1);

		_filterPointsParam = Param(nameof(FilterPoints), 5)
		.SetNonNegative()
		.SetDisplay("Filter", "Minimal move in points to confirm a turn", "Indicator");

		_stopLossPointsParam = Param(nameof(StopLossPoints), 60)
		.SetNonNegative()
		.SetDisplay("Stop Loss", "Protective stop in points", "Protection");

		_takeProfitPointsParam = Param(nameof(TakeProfitPoints), 0)
		.SetNonNegative()
		.SetDisplay("Take Profit", "Protective take profit in points", "Protection");

		_trailingStopPointsParam = Param(nameof(TrailingStopPoints), 0)
		.SetNonNegative()
		.SetDisplay("Trailing Stop", "Trailing stop in points", "Protection");

		_useTrailingParam = Param(nameof(UseTrailing), false)
		.SetDisplay("Use Trailing", "Enable trailing stop", "Protection");

		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Time-frame used for analysis", "General");

		Volume = 1;
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

		_baseAverage = null;
		_smoothingAverage = null;
		_previousSmoothed = 0m;
		_previousDirection = 0;
		_hasPrevious = false;
		_filter = 0m;
		_point = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Prepare moving averages that approximate the Volatility Quality line.
		_baseAverage = new SimpleMovingAverage { Length = Length };
		_smoothingAverage = Smoothing > 1 ? new SimpleMovingAverage { Length = Smoothing } : null;

		// Convert point-based distances to absolute prices using the instrument price step.
		_point = Security?.PriceStep ?? 0m;
		if (_point <= 0m)
		_point = 1m;

		_filter = FilterPoints > 0 ? FilterPoints * _point : 0m;

		var stopUnit = StopLossPoints > 0 ? new Unit(StopLossPoints * _point, UnitTypes.Absolute) : null;
		var takeUnit = TakeProfitPoints > 0 ? new Unit(TakeProfitPoints * _point, UnitTypes.Absolute) : null;
		var trailingUnit = UseTrailing && TrailingStopPoints > 0 ? new Unit(TrailingStopPoints * _point, UnitTypes.Absolute) : null;

		if (stopUnit != null || takeUnit != null || trailingUnit != null)
		{
			// Enable protective orders to mimic the MQL stop management.
			StartProtection(
			takeProfit: takeUnit,
			stopLoss: stopUnit,
			trailingStop: trailingUnit,
			useMarketOrders: true);
		}
		else
		{
			StartProtection();
		}

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _baseAverage, "Base VQ");
			if (_smoothingAverage != null)
			{
				DrawIndicator(area, _smoothingAverage, "Smoothed VQ");
			}
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var median = (candle.HighPrice + candle.LowPrice) / 2m;

		var baseValue = _baseAverage.Process(median, candle.OpenTime, true);
		if (!_baseAverage.IsFormed)
		return;

		var smoothed = baseValue.ToDecimal();

		if (_smoothingAverage != null)
		{
			var smoothingValue = _smoothingAverage.Process(smoothed, candle.OpenTime, true);
			if (!_smoothingAverage.IsFormed)
			return;

			smoothed = smoothingValue.ToDecimal();
		}

		if (!_hasPrevious)
		{
			// Store the very first value to obtain a reference slope.
			_previousSmoothed = smoothed;
			_previousDirection = 0;
			_hasPrevious = true;
			return;
		}

		var delta = smoothed - _previousSmoothed;
		if (Math.Abs(delta) < _filter)
		delta = 0m;

		var direction = delta > 0m ? 1 : delta < 0m ? -1 : _previousDirection;
		var turned = direction != _previousDirection && direction != 0;

		_previousSmoothed = smoothed;
		_previousDirection = direction;

		if (!turned)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (direction > 0 && Position <= 0m)
		{
			// Direction turned up: close shorts and open a long.
			var orderVolume = Volume;
			if (Position < 0m)
			orderVolume += Math.Abs(Position);

			if (orderVolume > 0m)
			BuyMarket(orderVolume);
		}
		else if (direction < 0 && Position >= 0m)
		{
			// Direction turned down: close longs and open a short.
			var orderVolume = Volume;
			if (Position > 0m)
			orderVolume += Math.Abs(Position);

			if (orderVolume > 0m)
			SellMarket(orderVolume);
		}
	}
}


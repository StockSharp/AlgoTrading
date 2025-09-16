using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// 5/8 exponential moving average crossover strategy converted from MetaTrader.
/// Uses a fast EMA on close prices and a slower EMA on open prices with manual stop, take profit, and trailing logic.
/// </summary>
public class FiveEightMaCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _fastMa = null!;
	private ExponentialMovingAverage _slowMa = null!;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _isInitialized;

	private decimal _pointValue;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private decimal _trailDistance;
	private decimal _maxPrice;
	private decimal _minPrice;

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in price points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="FiveEightMaCrossStrategy"/>.
	/// </summary>
	public FiveEightMaCrossStrategy()
	{
		_fastLength = Param(nameof(FastLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA Length", "Length of the EMA calculated on closing prices", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(3, 20, 1);

		_slowLength = Param(nameof(SlowLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA Length", "Length of the EMA calculated on opening prices", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 40m)
			.SetDisplay("Take Profit (points)", "Take profit distance expressed in price points", "Risk Management")
			.SetGreaterOrEqualZero()
			.SetCanOptimize(true)
			.SetOptimize(10m, 100m, 10m);

		_stopLossPoints = Param(nameof(StopLossPoints), 0m)
			.SetDisplay("Stop Loss (points)", "Stop loss distance expressed in price points", "Risk Management")
			.SetGreaterOrEqualZero()
			.SetCanOptimize(true)
			.SetOptimize(0m, 100m, 10m);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 0m)
			.SetDisplay("Trailing Stop (points)", "Trailing stop distance expressed in price points", "Risk Management")
			.SetGreaterOrEqualZero()
			.SetCanOptimize(true)
			.SetOptimize(0m, 100m, 10m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for calculations", "General");

		Volume = 0.1m;
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

	_fastMa = null!;
	_slowMa = null!;
	_prevFast = 0m;
	_prevSlow = 0m;
	_isInitialized = false;
	_pointValue = 1m;
	ResetPositionState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	_fastMa = new ExponentialMovingAverage { Length = FastLength };
	_slowMa = new ExponentialMovingAverage { Length = SlowLength };

	_pointValue = CalculatePointValue();

	var subscription = SubscribeCandles(CandleType);
	subscription
	.Bind(ProcessCandle)
	.Start();

	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, subscription);
	DrawOwnTrades(area);
	}
	}

	private decimal CalculatePointValue()
	{
	var step = Security?.PriceStep;

	if (step == null || step.Value <= 0m)
	return 1m;

	var stepValue = step.Value;
	var stepDouble = (double)stepValue;

	if (stepDouble <= 0d)
	return stepValue;

	var digitsDouble = Math.Log10(1d / stepDouble);
	var digits = (int)Math.Round(digitsDouble, MidpointRounding.AwayFromZero);
	var multiplier = (digits == 3 || digits == 5) ? 10m : 1m;

	return stepValue * multiplier;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
	// Process only finished candles to avoid repainting.
	if (candle.State != CandleStates.Finished)
	return;

	// Feed indicators with the corresponding price source.
	var fastValue = _fastMa.Process(candle.ClosePrice, candle.OpenTime, true).ToDecimal();
	var slowValue = _slowMa.Process(candle.OpenPrice, candle.OpenTime, true).ToDecimal();

	if (!_fastMa.IsFormed || !_slowMa.IsFormed)
	{
	_prevFast = fastValue;
	_prevSlow = slowValue;
	return;
	}

	HandleRiskManagement(candle);

	if (!IsFormedAndOnlineAndAllowTrading())
	{
	_prevFast = fastValue;
	_prevSlow = slowValue;
	return;
	}

	if (!_isInitialized)
	{
	_prevFast = fastValue;
	_prevSlow = slowValue;
	_isInitialized = true;
	return;
	}

	var crossUp = _prevFast <= _prevSlow && fastValue > slowValue;
	var crossDown = _prevFast >= _prevSlow && fastValue < slowValue;

	if (crossUp && Position <= 0)
	{
	EnterLong(candle);
	}
	else if (crossDown && Position >= 0)
	{
	EnterShort(candle);
	}

	_prevFast = fastValue;
	_prevSlow = slowValue;
	}

	private void EnterLong(ICandleMessage candle)
	{
	var positionVolume = Position < 0 ? Math.Abs(Position) : 0m;
	var volume = Volume + positionVolume;

	if (volume <= 0m)
	return;

	ResetPositionState();

	// Enter long position with volume including any short covering.
	BuyMarket(volume);

	_entryPrice = candle.ClosePrice;
	_takePrice = TakeProfitPoints > 0m ? _entryPrice + TakeProfitPoints * _pointValue : null;
	_stopPrice = StopLossPoints > 0m ? _entryPrice - StopLossPoints * _pointValue : null;
	_trailDistance = TrailingStopPoints > 0m ? TrailingStopPoints * _pointValue : 0m;
	_maxPrice = candle.HighPrice;
	_minPrice = candle.LowPrice;

	if (_trailDistance > 0m)
	{
	var trailStart = _entryPrice.Value - _trailDistance;
	if (_stopPrice == null || trailStart > _stopPrice.Value)
	_stopPrice = trailStart;
	}
	}

	private void EnterShort(ICandleMessage candle)
	{
	var positionVolume = Position > 0 ? Position : 0m;
	var volume = Volume + positionVolume;

	if (volume <= 0m)
	return;

	ResetPositionState();

	// Enter short position with volume including any long exit.
	SellMarket(volume);

	_entryPrice = candle.ClosePrice;
	_takePrice = TakeProfitPoints > 0m ? _entryPrice - TakeProfitPoints * _pointValue : null;
	_stopPrice = StopLossPoints > 0m ? _entryPrice + StopLossPoints * _pointValue : null;
	_trailDistance = TrailingStopPoints > 0m ? TrailingStopPoints * _pointValue : 0m;
	_maxPrice = candle.HighPrice;
	_minPrice = candle.LowPrice;

	if (_trailDistance > 0m)
	{
	var trailStart = _entryPrice.Value + _trailDistance;
	if (_stopPrice == null || trailStart < _stopPrice.Value)
	_stopPrice = trailStart;
	}
	}

	private void HandleRiskManagement(ICandleMessage candle)
	{
	if (Position > 0 && _entryPrice.HasValue)
	{
	// Update trailing stop using the highest price reached since entry.
	_maxPrice = Math.Max(_maxPrice, candle.HighPrice);

	if (_trailDistance > 0m)
	{
	var trailCandidate = _maxPrice - _trailDistance;
	if (_stopPrice == null || trailCandidate > _stopPrice.Value)
	_stopPrice = trailCandidate;
	}

	if (_takePrice.HasValue && candle.ClosePrice >= _takePrice.Value)
	{
	SellMarket(Position);
	ResetPositionState();
	return;
	}

	if (_stopPrice.HasValue && candle.ClosePrice <= _stopPrice.Value)
	{
	SellMarket(Position);
	ResetPositionState();
	return;
	}
	}
	else if (Position < 0 && _entryPrice.HasValue)
	{
	// Update trailing stop using the lowest price reached since entry.
	_minPrice = Math.Min(_minPrice, candle.LowPrice);

	if (_trailDistance > 0m)
	{
	var trailCandidate = _minPrice + _trailDistance;
	if (_stopPrice == null || trailCandidate < _stopPrice.Value)
	_stopPrice = trailCandidate;
	}

	if (_takePrice.HasValue && candle.ClosePrice <= _takePrice.Value)
	{
	BuyMarket(-Position);
	ResetPositionState();
	return;
	}

	if (_stopPrice.HasValue && candle.ClosePrice >= _stopPrice.Value)
	{
	BuyMarket(-Position);
	ResetPositionState();
	}
	}
	}

	private void ResetPositionState()
	{
	_entryPrice = null;
	_stopPrice = null;
	_takePrice = null;
	_trailDistance = 0m;
	_maxPrice = 0m;
	_minPrice = 0m;
	}
}

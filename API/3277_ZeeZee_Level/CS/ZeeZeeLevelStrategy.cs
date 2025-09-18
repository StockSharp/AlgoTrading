using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ZigZag swing strategy converted from the "ZeeZee Level" MetaTrader expert advisor.
/// The most recent ZigZag extreme defines the trade direction while fixed stops,
/// take profits and a trailing stop manage the open position.
/// </summary>
public class ZeeZeeLevelStrategy : Strategy
{
	private readonly StrategyParam<int> _zigZagDepth;
	private readonly StrategyParam<decimal> _zigZagDeviation;
	private readonly StrategyParam<int> _zigZagBackstep;
	private readonly StrategyParam<int> _zigZagIdInterval;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<decimal> _martingaleMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<ZigZagPivot> _pivots = new();

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private decimal? _trailingDistance;
	private decimal? _trailExtreme;
	private decimal _currentVolume;
	private decimal _priceStep;
	private decimal _volumeStep;
	private decimal _minVolume;
	private decimal? _maxVolume;
	private int _barIndex;
	private int _lastHighBar;
	private int _lastLowBar;
	private bool _isLongPosition;

	/// <summary>
	/// Initializes a new instance of <see cref="ZeeZeeLevelStrategy"/>.
	/// </summary>
	public ZeeZeeLevelStrategy()
	{
		_zigZagDepth = Param(nameof(ZigZagDepth), 12)
			.SetGreaterThanZero()
			.SetDisplay("ZigZag Depth", "Number of candles used to confirm pivots", "ZigZag")
			.SetCanOptimize(true)
			.SetOptimize(5, 60, 1);

		_zigZagDeviation = Param(nameof(ZigZagDeviation), 5m)
			.SetGreaterThanZero()
			.SetDisplay("ZigZag Deviation", "Minimum distance between pivots in price steps", "ZigZag")
			.SetCanOptimize(true)
			.SetOptimize(1m, 25m, 1m);

		_zigZagBackstep = Param(nameof(ZigZagBackstep), 3)
			.SetGreaterThanZero()
			.SetDisplay("ZigZag Backstep", "Bars required before switching pivot direction", "ZigZag")
			.SetCanOptimize(true)
			.SetOptimize(1, 15, 1);

		_zigZagIdInterval = Param(nameof(ZigZagIdInterval), 200)
			.SetGreaterThanZero()
			.SetDisplay("ZigZag ID Interval", "Maximum bars used to locate the last swings", "ZigZag")
			.SetCanOptimize(false);

		_stopLossPips = Param(nameof(StopLossPips), 20m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss", "Stop loss distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0m, 100m, 5m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 30m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit", "Take profit distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0m, 150m, 5m);

		_trailingStopPips = Param(nameof(TrailingStopPips), 15m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0m, 60m, 5m);

		_initialVolume = Param(nameof(InitialVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Initial Volume", "Base trade volume", "Money Management")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 1m, 0.01m);

		_martingaleMultiplier = Param(nameof(MartingaleMultiplier), 2.5m)
			.SetGreaterThanZero()
			.SetDisplay("Martingale Multiplier", "Volume multiplier after losing trade", "Money Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to evaluate", "General");
	}

	/// <summary>
	/// ZigZag depth parameter.
	/// </summary>
	public int ZigZagDepth
	{
		get => _zigZagDepth.Value;
		set => _zigZagDepth.Value = value;
	}

	/// <summary>
	/// ZigZag deviation parameter measured in price steps.
	/// </summary>
	public decimal ZigZagDeviation
	{
		get => _zigZagDeviation.Value;
		set => _zigZagDeviation.Value = value;
	}

	/// <summary>
	/// ZigZag backstep parameter.
	/// </summary>
	public int ZigZagBackstep
	{
		get => _zigZagBackstep.Value;
		set => _zigZagBackstep.Value = value;
	}

	/// <summary>
	/// Bars searched to find the last ZigZag highs and lows.
	/// </summary>
	public int ZigZagIdInterval
	{
		get => _zigZagIdInterval.Value;
		set => _zigZagIdInterval.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Base volume for the first trade in a martingale cycle.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the next trade after a loss.
	/// </summary>
	public decimal MartingaleMultiplier
	{
		get => _martingaleMultiplier.Value;
		set => _martingaleMultiplier.Value = value;
	}

	/// <summary>
	/// Type of candles used for the calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pivots.Clear();
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
		_trailingDistance = null;
		_trailExtreme = null;
		_barIndex = 0;
		_lastHighBar = -1;
		_lastLowBar = -1;
		_isLongPosition = false;
		_priceStep = 1m;
		_volumeStep = 1m;
		_minVolume = 0m;
		_maxVolume = null;
		_currentVolume = AdjustVolume(InitialVolume);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 1m;
		if (_priceStep <= 0m)
			_priceStep = 1m;

		_volumeStep = Security?.VolumeStep ?? 1m;
		if (_volumeStep <= 0m)
			_volumeStep = 1m;

		_minVolume = Security?.MinVolume ?? _volumeStep;
		if (_minVolume <= 0m)
			_minVolume = _volumeStep;

		_maxVolume = Security?.MaxVolume;

		_currentVolume = AdjustVolume(InitialVolume);

		var zigZag = new ZigZagIndicator
		{
			Depth = ZigZagDepth,
			Deviation = ZigZagDeviation,
			BackStep = ZigZagBackstep
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(zigZag, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, zigZag);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal zigZagValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		if (zigZagValue != 0m)
			UpdatePivotHistory(zigZagValue);

		if (_entryPrice.HasValue)
		{
			ManageOpenPosition(candle);
			return;
		}

		if (Position != 0m)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_lastHighBar < 0 || _lastLowBar < 0)
			return;

		var highAge = _barIndex - _lastHighBar;
		var lowAge = _barIndex - _lastLowBar;

		if (highAge > ZigZagIdInterval || lowAge > ZigZagIdInterval)
			return;

		if (highAge < lowAge)
			EnterShort(candle);
		else if (lowAge < highAge)
			EnterLong(candle);
	}

	private void UpdatePivotHistory(decimal price)
	{
		var pivot = new ZigZagPivot(_barIndex, price);
		_pivots.Add(pivot);

		if (_pivots.Count >= 2)
		{
			var previous = _pivots[^2];

			if (previous.IsHigh.HasValue)
			{
				pivot.IsHigh = !previous.IsHigh.Value;
			}
			else if (price != previous.Price)
			{
				var isHigh = price > previous.Price;
				pivot.IsHigh = isHigh;
				previous.IsHigh = !isHigh;

				if (previous.IsHigh == true)
					_lastHighBar = previous.BarIndex;
				else if (previous.IsHigh == false)
					_lastLowBar = previous.BarIndex;
			}
		}

		if (pivot.IsHigh == true)
			_lastHighBar = pivot.BarIndex;
		else if (pivot.IsHigh == false)
			_lastLowBar = pivot.BarIndex;

		var keep = Math.Max(ZigZagIdInterval + 10, 20);
		if (_pivots.Count > keep)
			_pivots.RemoveRange(0, _pivots.Count - keep);
	}

	private void EnterLong(ICandleMessage candle)
	{
		var volume = AdjustVolume(_currentVolume);
		if (volume <= 0m)
			return;

		BuyMarket(volume);

		_entryPrice = candle.ClosePrice;
		_isLongPosition = true;

		var stopDistance = StopLossPips > 0m ? GetPipValue(StopLossPips) : (decimal?)null;
		_stopPrice = stopDistance.HasValue ? _entryPrice - stopDistance.Value : null;

		var takeDistance = TakeProfitPips > 0m ? GetPipValue(TakeProfitPips) : (decimal?)null;
		_takePrice = takeDistance.HasValue ? _entryPrice + takeDistance.Value : null;

		_trailingDistance = TrailingStopPips > 0m ? GetPipValue(TrailingStopPips) : null;
		_trailExtreme = _trailingDistance.HasValue ? candle.ClosePrice : null;
	}

	private void EnterShort(ICandleMessage candle)
	{
		var volume = AdjustVolume(_currentVolume);
		if (volume <= 0m)
			return;

		SellMarket(volume);

		_entryPrice = candle.ClosePrice;
		_isLongPosition = false;

		var stopDistance = StopLossPips > 0m ? GetPipValue(StopLossPips) : (decimal?)null;
		_stopPrice = stopDistance.HasValue ? _entryPrice + stopDistance.Value : null;

		var takeDistance = TakeProfitPips > 0m ? GetPipValue(TakeProfitPips) : (decimal?)null;
		_takePrice = takeDistance.HasValue ? _entryPrice - takeDistance.Value : null;

		_trailingDistance = TrailingStopPips > 0m ? GetPipValue(TrailingStopPips) : null;
		_trailExtreme = _trailingDistance.HasValue ? candle.ClosePrice : null;
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (!_entryPrice.HasValue)
			return;

		if (_isLongPosition)
		{
			if (Position <= 0m)
				return;

			UpdateTrailingForLong(candle);
			var exitPrice = EvaluateLongExit(candle);
			if (exitPrice.HasValue)
			{
				SellMarket(Position);
				UpdateMartingaleState(exitPrice.Value, true);
				ResetPositionState();
			}
		}
		else
		{
			if (Position >= 0m)
				return;

			UpdateTrailingForShort(candle);
			var exitPrice = EvaluateShortExit(candle);
			if (exitPrice.HasValue)
			{
				BuyMarket(Math.Abs(Position));
				UpdateMartingaleState(exitPrice.Value, false);
				ResetPositionState();
			}
		}
	}

	private void UpdateTrailingForLong(ICandleMessage candle)
	{
		if (!_trailingDistance.HasValue)
			return;

		var newExtreme = _trailExtreme.HasValue ? Math.Max(_trailExtreme.Value, candle.HighPrice) : candle.HighPrice;
		_trailExtreme = newExtreme;

		var candidate = newExtreme - _trailingDistance.Value;
		if (!_stopPrice.HasValue || candidate > _stopPrice.Value)
			_stopPrice = candidate;
	}

	private void UpdateTrailingForShort(ICandleMessage candle)
	{
		if (!_trailingDistance.HasValue)
			return;

		var newExtreme = _trailExtreme.HasValue ? Math.Min(_trailExtreme.Value, candle.LowPrice) : candle.LowPrice;
		_trailExtreme = newExtreme;

		var candidate = newExtreme + _trailingDistance.Value;
		if (!_stopPrice.HasValue || candidate < _stopPrice.Value)
			_stopPrice = candidate;
	}

	private decimal? EvaluateLongExit(ICandleMessage candle)
	{
		var stopHit = _stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value;
		var takeHit = _takePrice.HasValue && candle.HighPrice >= _takePrice.Value;

		if (!stopHit && !takeHit)
			return null;

		if (stopHit && takeHit)
		{
			var stopDistance = Math.Abs(_entryPrice!.Value - _stopPrice!.Value);
			var takeDistance = Math.Abs(_takePrice!.Value - _entryPrice.Value);
			return stopDistance <= takeDistance ? _stopPrice : _takePrice;
		}

		return stopHit ? _stopPrice : _takePrice;
	}

	private decimal? EvaluateShortExit(ICandleMessage candle)
	{
		var stopHit = _stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value;
		var takeHit = _takePrice.HasValue && candle.LowPrice <= _takePrice.Value;

		if (!stopHit && !takeHit)
			return null;

		if (stopHit && takeHit)
		{
			var stopDistance = Math.Abs(_stopPrice!.Value - _entryPrice!.Value);
			var takeDistance = Math.Abs(_entryPrice.Value - _takePrice!.Value);
			return stopDistance <= takeDistance ? _stopPrice : _takePrice;
		}

		return stopHit ? _stopPrice : _takePrice;
	}

	private void UpdateMartingaleState(decimal exitPrice, bool isLong)
	{
		if (!_entryPrice.HasValue)
			return;

		var profit = isLong
			? exitPrice - _entryPrice.Value
			: _entryPrice.Value - exitPrice;

		if (profit > 0m)
			_currentVolume = AdjustVolume(InitialVolume);
		else
			_currentVolume = AdjustVolume(_currentVolume * MartingaleMultiplier);
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
		_trailingDistance = null;
		_trailExtreme = null;
		_isLongPosition = false;
	}

	private decimal AdjustVolume(decimal volume)
	{
		var adjusted = volume;

		if (_volumeStep > 0m)
			adjusted = Math.Round(adjusted / _volumeStep) * _volumeStep;

		if (adjusted < _minVolume)
			adjusted = _minVolume;

		if (_maxVolume.HasValue && _maxVolume.Value > 0m && adjusted > _maxVolume.Value)
			adjusted = _maxVolume.Value;

		return adjusted;
	}

	private decimal GetPipValue(decimal pips)
	{
		return pips * (_priceStep > 0m ? _priceStep : 1m);
	}

	private sealed class ZigZagPivot
	{
		public ZigZagPivot(int barIndex, decimal price)
		{
			BarIndex = barIndex;
			Price = price;
		}

		public int BarIndex { get; }
		public decimal Price { get; }
		public bool? IsHigh { get; set; }
	}
}

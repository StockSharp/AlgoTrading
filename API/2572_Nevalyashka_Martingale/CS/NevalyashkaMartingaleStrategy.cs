using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Equity-based martingale strategy that alternates trade direction after losses.
/// Opens a short position on startup, resets volume after profitable cycles,
/// and increases exposure following drawdowns while managing fixed stops and targets.
/// </summary>
public class NevalyashkaMartingaleStrategy : Strategy
{
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _moveProfitPoints;
	private readonly StrategyParam<decimal> _moveStepPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _plannedVolume;
	private decimal _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private decimal _equityPeak;
	private bool _nextDirectionIsSell = true;
	private bool _initialOrderPlaced;

	/// <summary>
	/// Initializes <see cref="NevalyashkaMartingaleStrategy"/>.
	/// </summary>
	public NevalyashkaMartingaleStrategy()
	{
		_baseVolume = Param(nameof(BaseVolume), 0.1m)
			.SetDisplay("Base Volume", "Initial trade volume", "Risk")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 1.1m)
			.SetDisplay("Volume Multiplier", "Multiplier applied after losses", "Risk")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 94m)
			.SetDisplay("Take Profit Points", "Profit target in points", "Orders")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_moveProfitPoints = Param(nameof(MoveProfitPoints), 25m)
			.SetDisplay("Move Profit Points", "Profit buffer before trailing activates", "Orders")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_moveStepPoints = Param(nameof(MoveStepPoints), 11m)
			.SetDisplay("Move Step Points", "Extra buffer for trailing stop updates", "Orders")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 70m)
			.SetDisplay("Stop Loss Points", "Initial protective distance", "Orders")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for trade management", "General");
	}

	/// <summary>
	/// Base order volume.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Multiplier applied when recovering from a loss.
	/// </summary>
	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
	}

	/// <summary>
	/// Take profit distance in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Minimum profit in points before the stop is tightened.
	/// </summary>
	public decimal MoveProfitPoints
	{
		get => _moveProfitPoints.Value;
		set => _moveProfitPoints.Value = value;
	}

	/// <summary>
	/// Additional margin in points required between stop adjustments.
	/// </summary>
	public decimal MoveStepPoints
	{
		get => _moveStepPoints.Value;
		set => _moveStepPoints.Value = value;
	}

	/// <summary>
	/// Initial stop loss distance in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Candle type used to drive the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_plannedVolume = 0m;
		_entryPrice = 0m;
		_stopPrice = null;
		_takePrice = null;
		_equityPeak = 0m;
		_nextDirectionIsSell = true;
		_initialOrderPlaced = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_equityPeak = Portfolio?.CurrentValue ?? 0m;
		_plannedVolume = AdjustVolume(BaseVolume);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var point = GetPointValue();

		HandleOpenPosition(candle, point);

		if (Position != 0)
			return;

		var equity = Portfolio?.CurrentValue ?? 0m;

		if (!_initialOrderPlaced)
		{
			if (_plannedVolume == 0m)
			{
				_plannedVolume = AdjustVolume(BaseVolume);
				if (_plannedVolume == 0m)
					return;
			}

			if (OpenPosition(true, candle.ClosePrice, point))
			{
				_initialOrderPlaced = true;
				_nextDirectionIsSell = true;
			}

			return;
		}

		if (equity > _equityPeak)
		{
			_equityPeak = equity;
			_plannedVolume = AdjustVolume(BaseVolume);

			if (_plannedVolume == 0m)
				return;

			if (_nextDirectionIsSell)
			{
				if (OpenPosition(true, candle.ClosePrice, point))
					return;
			}
			else
			{
				if (OpenPosition(false, candle.ClosePrice, point))
					return;
			}
		}
		else
		{
			var increased = VolumeMultiplier > 0m ? AdjustVolume(_plannedVolume * VolumeMultiplier) : 0m;

			if (increased == 0m)
				return;

			_plannedVolume = increased;

			if (_nextDirectionIsSell)
			{
				if (OpenPosition(false, candle.ClosePrice, point))
					_nextDirectionIsSell = false;
			}
			else
			{
				if (OpenPosition(true, candle.ClosePrice, point))
					_nextDirectionIsSell = true;
			}
		}
	}

	private void HandleOpenPosition(ICandleMessage candle, decimal point)
	{
		if (Position > 0)
		{
			HandleLongPosition(candle, point);
		}
		else if (Position < 0)
		{
			HandleShortPosition(candle, point);
		}
	}

	private void HandleLongPosition(ICandleMessage candle, decimal point)
	{
		if (_stopPrice is not decimal currentStop || _takePrice is not decimal currentTake)
			return;

		var price = candle.ClosePrice;
		var moveThreshold = MoveProfitPoints * point;

		if (price - _entryPrice > moveThreshold)
		{
			var candidate = price - (StopLossPoints + MoveStepPoints) * point;

			if (candidate > currentStop)
			{
				var newStop = price - StopLossPoints * point;
				_stopPrice = newStop;

				if (_plannedVolume > AdjustVolume(BaseVolume) && newStop > _entryPrice)
					ReduceVolume();
			}
		}

		if (candle.LowPrice <= _stopPrice)
		{
			SellMarket(Math.Abs(Position));
			ResetProtection();
			return;
		}

		if (candle.HighPrice >= currentTake)
		{
			SellMarket(Math.Abs(Position));
			ResetProtection();
		}
	}

	private void HandleShortPosition(ICandleMessage candle, decimal point)
	{
		if (_stopPrice is not decimal currentStop || _takePrice is not decimal currentTake)
			return;

		var price = candle.ClosePrice;
		var moveThreshold = MoveProfitPoints * point;

		if (_entryPrice - price > moveThreshold)
		{
			var candidate = price + (StopLossPoints + MoveStepPoints) * point;

			if (candidate < currentStop)
			{
				var newStop = price + StopLossPoints * point;
				_stopPrice = newStop;

				if (_plannedVolume > AdjustVolume(BaseVolume) && newStop < _entryPrice)
					ReduceVolume();
			}
		}

		if (candle.HighPrice >= _stopPrice)
		{
			BuyMarket(Math.Abs(Position));
			ResetProtection();
			return;
		}

		if (candle.LowPrice <= currentTake)
		{
			BuyMarket(Math.Abs(Position));
			ResetProtection();
		}
	}

	private bool OpenPosition(bool isSell, decimal price, decimal point)
	{
		if (_plannedVolume <= 0m)
			return false;

		if (point <= 0m)
			return false;

		var stopOffset = StopLossPoints * point;
		var takeOffset = TakeProfitPoints * point;

		if (stopOffset <= 0m || takeOffset <= 0m)
			return false;

		if (isSell)
		{
			SellMarket(_plannedVolume);
			_stopPrice = price + stopOffset;
			_takePrice = price - takeOffset;
		}
		else
		{
			BuyMarket(_plannedVolume);
			_stopPrice = price - stopOffset;
			_takePrice = price + takeOffset;
		}

		_entryPrice = price;
		return true;
	}

	private void ReduceVolume()
	{
		if (VolumeMultiplier <= 0m)
			return;

		var baseVolume = AdjustVolume(BaseVolume);

		if (baseVolume == 0m)
			return;

		var reduced = AdjustVolume(_plannedVolume / VolumeMultiplier);

		if (reduced < baseVolume)
			reduced = baseVolume;

		_plannedVolume = reduced;
	}

	private void ResetProtection()
	{
		_stopPrice = null;
		_takePrice = null;
		_entryPrice = 0m;
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (Security is null)
			return volume;

		if (volume <= 0m)
			return 0m;

		var step = Security.VolumeStep ?? 0m;

		if (step > 0m)
			volume = Math.Floor(volume / step) * step;

		var min = Security.MinVolume ?? 0m;
		if (min > 0m && volume < min)
			return 0m;

		var max = Security.MaxVolume ?? 0m;
		if (max > 0m && volume > max)
			volume = max.Value;

		return volume;
	}

	private decimal GetPointValue()
	{
		var step = Security?.PriceStep ?? 0m;

		if (step <= 0m)
			step = Security?.Step ?? 0m;

		if (step <= 0m)
			return 1m;

		var digits = 0;
		var value = step;

		while (value < 1m && digits < 10)
		{
			value *= 10m;
			digits++;
		}

		if (digits == 3 || digits == 5)
			step *= 10m;

		return step;
	}
}

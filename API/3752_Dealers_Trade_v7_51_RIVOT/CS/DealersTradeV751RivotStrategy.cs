using System;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dealers Trade v7.51 strategy ported from MetaTrader 4 implementation.
/// Builds directional bias from classic pivot and floating pivot levels
/// and scales into the bias when price retraces by a fixed pip distance.
/// Applies martingale-style position sizing with configurable stop-loss,
/// take-profit, and trailing-stop management.
/// </summary>
public class DealersTradeV751RivotStrategy : Strategy
{
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<decimal> _pipDistance;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<decimal> _gapThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage _previousCandle;
	private decimal _pivotLevel;
	private decimal _floatingPivot;
	private decimal _gapInPips;
	private decimal _lastEntryPrice;
	private decimal _averageEntryPrice;
	private decimal? _trailingStopLevel;
	private int _direction; // -1 short, 0 neutral, 1 long
	private int _entriesInSeries;

	/// <summary>
	/// Maximum number of entries allowed in one scaling series.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Distance in pips between martingale entries.
	/// </summary>
	public decimal PipDistance
	{
		get => _pipDistance.Value;
		set => _pipDistance.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Trailing-stop distance in pips.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Multiplier applied to volume for each additional entry.
	/// </summary>
	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
	}

	/// <summary>
	/// Maximum allowed volume for a single entry.
	/// </summary>
	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}

	/// <summary>
	/// Minimum pivot gap in pips required to activate the bias.
	/// </summary>
	public decimal GapThreshold
	{
		get => _gapThreshold.Value;
		set => _gapThreshold.Value = value;
	}

	/// <summary>
	/// Type of candles used for pivot calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public DealersTradeV751RivotStrategy()
	{
		_maxTrades = Param(nameof(MaxTrades), 5)
		.SetGreaterThanZero()
		.SetDisplay("Max Trades", "Maximum number of martingale entries", "Position Sizing")
		.SetCanOptimize(true)
		.SetOptimize(1, 10, 1);

		_pipDistance = Param(nameof(PipDistance), 4m)
		.SetGreaterThanZero()
		.SetDisplay("Pip Distance", "Distance between averaged entries in pips", "Position Sizing")
		.SetCanOptimize(true)
		.SetOptimize(2m, 15m, 1m);

		_takeProfit = Param(nameof(TakeProfit), 15m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit", "Take-profit distance in pips", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(5m, 50m, 5m);

		_stopLoss = Param(nameof(StopLoss), 90m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss", "Stop-loss distance in pips", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(30m, 200m, 10m);

		_trailingStop = Param(nameof(TrailingStop), 15m)
		.SetGreaterThanZero()
		.SetDisplay("Trailing Stop", "Trailing-stop distance in pips", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(5m, 40m, 5m);

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 1.5m)
		.SetGreaterThanZero()
		.SetDisplay("Volume Multiplier", "Multiplier applied after each new entry", "Position Sizing")
		.SetCanOptimize(true)
		.SetOptimize(1.1m, 3m, 0.1m);

		_maxVolume = Param(nameof(MaxVolume), 5m)
		.SetGreaterThanZero()
		.SetDisplay("Max Volume", "Upper limit for single-entry volume", "Position Sizing");

		_gapThreshold = Param(nameof(GapThreshold), 7m)
		.SetGreaterThanZero()
		.SetDisplay("Gap Threshold", "Minimal pivot gap required to enable trading", "Signal")
		.SetCanOptimize(true)
		.SetOptimize(3m, 15m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles used for pivot calculations", "Signal");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetSeries();
		_previousCandle = null;
		_pivotLevel = 0m;
		_floatingPivot = 0m;
		_gapInPips = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawLine(area, "Pivot", () => _pivotLevel);
			DrawLine(area, "FloatingPivot", () => _floatingPivot);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			// Reset martingale state once the position is closed externally.
			ResetSeries();
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_previousCandle == null)
		{
			_previousCandle = candle;
			return;
		}

		UpdatePivots(candle);

		if (Position == 0m && _entriesInSeries > 0)
		{
			// Force reset when no exposure remains but scaling data still exists.
			ResetSeries();
		}

		if (_entriesInSeries > 0)
		{
			ManageRisk(candle.ClosePrice);
		}

		if (_entriesInSeries >= MaxTrades)
		{
			_previousCandle = candle;
			return;
		}

		if (_direction == 0)
		{
			EvaluateDirection(candle);
		}

		TryEnter(candle);

		_previousCandle = candle;
	}

	private void UpdatePivots(ICandleMessage candle)
	{
		var step = GetPriceStep();
		_pivotLevel = (_previousCandle!.HighPrice + _previousCandle.LowPrice + _previousCandle.ClosePrice + candle.OpenPrice) / 4m;
		_floatingPivot = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
		_gapInPips = step == 0m ? 0m : Math.Abs(_pivotLevel - _floatingPivot) / step;
	}

	private void EvaluateDirection(ICandleMessage candle)
	{
		var price = candle.ClosePrice;

		if (price > _pivotLevel && price > _floatingPivot && _gapInPips >= GapThreshold)
		{
			_direction = 1;
			LogInfo($"Bias switched to long. Pivot={_pivotLevel:F5}, Floating={_floatingPivot:F5}, Gap={_gapInPips:F2} pips.");
		}
		else if (price < _pivotLevel && price < _floatingPivot && _gapInPips >= GapThreshold)
		{
			_direction = -1;
			LogInfo($"Bias switched to short. Pivot={_pivotLevel:F5}, Floating={_floatingPivot:F5}, Gap={_gapInPips:F2} pips.");
		}
	}

	private void TryEnter(ICandleMessage candle)
	{
		if (_direction == 0)
		return;

		var price = candle.ClosePrice;
		var step = GetPriceStep();
		var distance = PipDistance * step;

		if (_direction > 0)
		{
			if (_entriesInSeries == 0 || (_lastEntryPrice - price) >= distance)
			{
				EnterLong(price);
			}
		}
		else
		{
			if (_entriesInSeries == 0 || (price - _lastEntryPrice) >= distance)
			{
				EnterShort(price);
			}
		}
	}

	private void EnterLong(decimal price)
	{
		var volume = CalculateNextVolume();
		_lastEntryPrice = price;
		_averageEntryPrice = UpdateAveragePrice(price, volume, true);
		_entriesInSeries++;
		LogInfo($"Opening long entry #{_entriesInSeries} at {price:F5} with volume {volume}.");
		BuyMarket(volume);
	}

	private void EnterShort(decimal price)
	{
		var volume = CalculateNextVolume();
		_lastEntryPrice = price;
		_averageEntryPrice = UpdateAveragePrice(price, volume, false);
		_entriesInSeries++;
		LogInfo($"Opening short entry #{_entriesInSeries} at {price:F5} with volume {volume}.");
		SellMarket(volume);
	}

	private decimal CalculateNextVolume()
	{
		var volume = Volume;

		for (var i = 0; i < _entriesInSeries; i++)
		{
			volume *= VolumeMultiplier;
			if (volume >= MaxVolume)
			{
				volume = MaxVolume;
				break;
			}
		}

		var volumeStep = Security?.VolumeStep ?? 0.01m;
		if (volumeStep > 0m)
		{
			volume = Math.Ceiling(volume / volumeStep) * volumeStep;
		}

		return volume;
	}

	private decimal UpdateAveragePrice(decimal price, decimal volume, bool isLong)
	{
		var existingVolume = Math.Abs(Position);
		var side = isLong ? 1m : -1m;

		if (existingVolume <= 0m)
		{
			return price;
		}

		var totalVolume = existingVolume + volume;
		var weightedAverage = ((_averageEntryPrice * existingVolume * side) + (price * volume)) / totalVolume;
		return Math.Abs(weightedAverage);
	}

	private void ManageRisk(decimal price)
	{
		if (_entriesInSeries == 0)
		{
			_trailingStopLevel = null;
			return;
		}

		var step = GetPriceStep();
		var stopDistance = StopLoss * step;
		var takeDistance = TakeProfit * step;
		var trailingDistance = TrailingStop * step;

		if (_direction > 0)
		{
			var lossLevel = _averageEntryPrice - stopDistance;
			var profitLevel = _averageEntryPrice + takeDistance;

			if (price <= lossLevel)
			{
				LogInfo($"Long stop-loss triggered at {price:F5}. Average entry {_averageEntryPrice:F5}.");
				SellMarket(Math.Abs(Position));
				ResetSeries();
				return;
			}

			if (price >= profitLevel)
			{
				LogInfo($"Long take-profit triggered at {price:F5}. Average entry {_averageEntryPrice:F5}.");
				SellMarket(Math.Abs(Position));
				ResetSeries();
				return;
			}

			if (TrailingStop > 0m)
			{
				var candidate = price - trailingDistance;
				if (_trailingStopLevel == null || candidate > _trailingStopLevel)
				{
					_trailingStopLevel = candidate;
				}

				if (_trailingStopLevel != null && price <= _trailingStopLevel)
				{
					LogInfo($"Long trailing stop activated at {price:F5}.");
					SellMarket(Math.Abs(Position));
					ResetSeries();
				}
			}
		}
		else if (_direction < 0)
		{
			var lossLevel = _averageEntryPrice + stopDistance;
			var profitLevel = _averageEntryPrice - takeDistance;

			if (price >= lossLevel)
			{
				LogInfo($"Short stop-loss triggered at {price:F5}. Average entry {_averageEntryPrice:F5}.");
				BuyMarket(Math.Abs(Position));
				ResetSeries();
				return;
			}

			if (price <= profitLevel)
			{
				LogInfo($"Short take-profit triggered at {price:F5}. Average entry {_averageEntryPrice:F5}.");
				BuyMarket(Math.Abs(Position));
				ResetSeries();
				return;
			}

			if (TrailingStop > 0m)
			{
				var candidate = price + trailingDistance;
				if (_trailingStopLevel == null || candidate < _trailingStopLevel)
				{
					_trailingStopLevel = candidate;
				}

				if (_trailingStopLevel != null && price >= _trailingStopLevel)
				{
					LogInfo($"Short trailing stop activated at {price:F5}.");
					BuyMarket(Math.Abs(Position));
					ResetSeries();
				}
			}
		}
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step == 0m)
		{
			// Fallback to four decimal places when instrument metadata is unknown.
			step = 0.0001m;
		}
		return step;
	}

	private void ResetSeries()
	{
		_direction = 0;
		_entriesInSeries = 0;
		_lastEntryPrice = 0m;
		_averageEntryPrice = 0m;
		_trailingStopLevel = null;
	}
}

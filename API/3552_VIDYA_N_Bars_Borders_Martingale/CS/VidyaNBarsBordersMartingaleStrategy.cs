using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// VIDYA N Bars Borders Martingale strategy.
/// </summary>
public class VidyaNBarsBordersMartingaleStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _vidyaCmoPeriod;
	private readonly StrategyParam<int> _vidyaEmaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<decimal> _positionIncreaseRatio;
	private readonly StrategyParam<decimal> _maximumPositionVolume;
	private readonly StrategyParam<decimal> _maximumTotalVolume;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<decimal> _minStepPoints;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<bool> _reverse;

	private KaufmanAdaptiveMovingAverage _vidya;
	private AverageTrueRange _atr;

	private decimal _priceStep;
	private decimal _lastEntryPrice;
	private decimal _currentVolume;
	private decimal _previousPosition;
	private decimal _lastRealizedPnL;

	/// <summary>
	/// Initializes a new instance of <see cref="VidyaNBarsBordersMartingaleStrategy"/>.
	/// </summary>
	public VidyaNBarsBordersMartingaleStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Trading candle type", "General");

		_vidyaCmoPeriod = Param(nameof(VidyaCmoPeriod), 15)
		.SetGreaterThanZero()
		.SetDisplay("CMO Period", "Efficiency ratio period for VIDYA", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 40, 5);

		_vidyaEmaPeriod = Param(nameof(VidyaEmaPeriod), 12)
		.SetGreaterThanZero()
		.SetDisplay("EMA Period", "Smoothing period for VIDYA", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 40, 5);

		_atrPeriod = Param(nameof(AtrPeriod), 6)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "Average True Range period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);

		_profitTarget = Param(nameof(ProfitTarget), 30m)
		.SetDisplay("Profit Target", "Money target to flatten all positions", "Risk");

		_positionIncreaseRatio = Param(nameof(PositionIncreaseRatio), 1.6m)
		.SetGreaterThanZero()
		.SetDisplay("Increase Ratio", "Multiplier applied after a losing trade", "Risk");

		_maximumPositionVolume = Param(nameof(MaximumPositionVolume), 1.5m)
		.SetDisplay("Max Position Volume", "Hard cap for a single position volume", "Risk");

		_maximumTotalVolume = Param(nameof(MaximumTotalVolume), 6.31m)
		.SetDisplay("Max Total Volume", "Cap for accumulated exposure", "Risk");

		_maxPositions = Param(nameof(MaxPositions), 1)
		.SetDisplay("Max Positions", "Maximum simultaneous positions", "Risk");

		_minStepPoints = Param(nameof(MinStepPositions), 150m)
		.SetDisplay("Minimum Step", "Minimum distance between entries (points)", "Risk");

		_baseVolume = Param(nameof(BaseVolume), 0.01m)
		.SetDisplay("Base Volume", "Initial trade size", "Trading");

		_reverse = Param(nameof(Reverse), false)
		.SetDisplay("Reverse Signals", "Invert long/short signals", "Trading");
	}

	/// <summary>
	/// Candle type to trade.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// VIDYA efficiency ratio period.
	/// </summary>
	public int VidyaCmoPeriod
	{
		get => _vidyaCmoPeriod.Value;
		set => _vidyaCmoPeriod.Value = value;
	}

	/// <summary>
	/// VIDYA smoothing period.
	/// </summary>
	public int VidyaEmaPeriod
	{
		get => _vidyaEmaPeriod.Value;
		set => _vidyaEmaPeriod.Value = value;
	}

	/// <summary>
	/// ATR length used for the channel width.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Profit target expressed in money.
	/// </summary>
	public decimal ProfitTarget
	{
		get => _profitTarget.Value;
		set => _profitTarget.Value = value;
	}

	/// <summary>
	/// Multiplier applied after a losing trade.
	/// </summary>
	public decimal PositionIncreaseRatio
	{
		get => _positionIncreaseRatio.Value;
		set => _positionIncreaseRatio.Value = value;
	}

	/// <summary>
	/// Maximum volume for a single position.
	/// </summary>
	public decimal MaximumPositionVolume
	{
		get => _maximumPositionVolume.Value;
		set => _maximumPositionVolume.Value = value;
	}

	/// <summary>
	/// Maximum aggregate exposure.
	/// </summary>
	public decimal MaximumTotalVolume
	{
		get => _maximumTotalVolume.Value;
		set => _maximumTotalVolume.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneous positions.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Minimum entry spacing measured in points.
	/// </summary>
	public decimal MinStepPositions
	{
		get => _minStepPoints.Value;
		set => _minStepPoints.Value = value;
	}

	/// <summary>
	/// Base trade volume.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Inverts long and short signals.
	/// </summary>
	public bool Reverse
	{
		get => _reverse.Value;
		set => _reverse.Value = value;
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

		_lastEntryPrice = 0m;
		_previousPosition = 0m;
		_lastRealizedPnL = 0m;
		_currentVolume = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 0m;
		if (_priceStep <= 0m)
		_priceStep = 0.0001m;

		_vidya = new KaufmanAdaptiveMovingAverage
		{
			Length = VidyaEmaPeriod,
			FastSCPeriod = Math.Max(2, VidyaCmoPeriod / 2),
			SlowSCPeriod = Math.Max(VidyaCmoPeriod, VidyaEmaPeriod)
		};

		_atr = new AverageTrueRange
		{
			Length = AtrPeriod
		};

		_currentVolume = AlignVolume(ClampVolume(BaseVolume > 0m ? BaseVolume : Volume));

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_vidya, _atr, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _vidya);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (_previousPosition == 0m && Position != 0m)
		{
			_lastRealizedPnL = PnL;
		}
		else if (_previousPosition != 0m && Position == 0m)
		{
			var tradePnL = PnL - _lastRealizedPnL;
			_lastRealizedPnL = PnL;

			if (tradePnL < 0m)
			{
				var nextVolume = _currentVolume * PositionIncreaseRatio;
				_currentVolume = AlignVolume(ClampVolume(nextVolume));
			}
			else
			{
				_currentVolume = AlignVolume(ClampVolume(BaseVolume > 0m ? BaseVolume : Volume));
			}

			_lastEntryPrice = 0m;
		}

		_previousPosition = Position;
	}

	private void ProcessCandle(ICandleMessage candle, decimal vidyaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_vidya.IsFormed || !_atr.IsFormed)
		return;

		var middle = vidyaValue;
		var upper = middle + atrValue;
		var lower = middle - atrValue;

		var openPnL = GetUnrealizedPnL(candle);

		if (Position != 0m && ProfitTarget > 0m && openPnL >= ProfitTarget)
		{
			CloseAllPositions();
			return;
		}

		var close = candle.ClosePrice;

		var longSignal = close < lower;
		var shortSignal = close > upper;

		if (Reverse)
		{
			(longSignal, shortSignal) = (shortSignal, longSignal);
		}

		if (longSignal)
		{
			ExecuteEntry(close, true);
		}
		else if (shortSignal)
		{
			ExecuteEntry(close, false);
		}
	}

	private void ExecuteEntry(decimal price, bool isLong)
	{
		if (MaxPositions == 0 && Position == 0m)
		return;

		if (isLong)
		{
			if (Position > 0m)
			return;

			var volume = GetEntryVolume();

			if (Position < 0m)
			{
				var closeQty = Math.Abs(Position);
				if (volume > 0m && IsDistanceEnough(price))
				{
					BuyMarket(closeQty + volume);
					_lastEntryPrice = price;
				}
				else
				{
					BuyMarket(closeQty);
				}

				return;
			}

			if (volume <= 0m || !IsDistanceEnough(price))
			return;

			BuyMarket(volume);
			_lastEntryPrice = price;
		}
		else
		{
			if (Position < 0m)
			return;

			var volume = GetEntryVolume();

			if (Position > 0m)
			{
				var closeQty = Position;
				if (volume > 0m && IsDistanceEnough(price))
				{
					SellMarket(closeQty + volume);
					_lastEntryPrice = price;
				}
				else
				{
					SellMarket(closeQty);
				}

				return;
			}

			if (volume <= 0m || !IsDistanceEnough(price))
			return;

			SellMarket(volume);
			_lastEntryPrice = price;
		}
	}

	private decimal GetEntryVolume()
	{
		var volume = AlignVolume(ClampVolume(_currentVolume));

		var remaining = MaximumTotalVolume;
		if (remaining > 0m)
		{
			remaining -= Math.Abs(Position);
			if (remaining <= 0m)
			return 0m;

			if (volume > remaining)
			volume = AlignVolume(remaining);
		}

		return volume;
	}

	private bool IsDistanceEnough(decimal price)
	{
		var stepPoints = MinStepPositions;
		if (stepPoints <= 0m)
		return true;

		if (_lastEntryPrice == 0m)
		return true;

		var minDistance = stepPoints * _priceStep;
		if (minDistance <= 0m)
		return true;

		return Math.Abs(price - _lastEntryPrice) >= minDistance;
	}

	private decimal ClampVolume(decimal volume)
	{
		var result = volume;

		if (MaximumPositionVolume > 0m)
		result = Math.Min(result, MaximumPositionVolume);

		if (MaximumTotalVolume > 0m)
		result = Math.Min(result, MaximumTotalVolume);

		return result;
	}

	private decimal AlignVolume(decimal volume)
	{
		var result = volume;

		var step = Security?.VolumeStep ?? 0m;
		if (step > 0m)
		result = Math.Floor(result / step) * step;

		var min = Security?.MinVolume ?? 0m;
		if (min > 0m && result < min)
		result = min;

		var max = Security?.MaxVolume ?? 0m;
		if (max > 0m && result > max)
		result = max;

		return result;
	}

	private decimal GetUnrealizedPnL(ICandleMessage candle)
	{
		if (Position == 0m)
		return 0m;

		var entry = PositionAvgPrice;
		if (entry == 0m)
		entry = _lastEntryPrice;

		var diff = candle.ClosePrice - entry;
		var multiplier = Security?.Multiplier ?? 1m;

		return diff * Position * multiplier;
	}

	private void CloseAllPositions()
	{
		if (Position > 0m)
		{
			SellMarket(Position);
		}
		else if (Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
		}
	}
}

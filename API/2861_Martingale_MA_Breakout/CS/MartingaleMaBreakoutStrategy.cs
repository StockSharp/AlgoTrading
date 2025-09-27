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
/// Martingale strategy that trades when price deviates from a moving average.
/// </summary>
public class MartingaleMaBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<int> _distanceFromMaPips;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<MovingAverageModes> _maMethod;
	private readonly StrategyParam<AppliedPrices> _appliedPrice;
	private readonly StrategyParam<decimal> _riskPercent;

	private LengthIndicator<decimal> _movingAverage;
	private readonly List<decimal> _maHistory = new();
	private decimal _pipSize;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private decimal _entryPrice;
	private decimal? _lastRecordedBalance;

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public int StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips { get => _takeProfitPips.Value; set => _takeProfitPips.Value = value; }

	/// <summary>
	/// Trailing stop offset expressed in pips.
	/// </summary>
	public int TrailingStopPips { get => _trailingStopPips.Value; set => _trailingStopPips.Value = value; }

	/// <summary>
	/// Minimal price move before the trailing stop is moved again.
	/// </summary>
	public int TrailingStepPips { get => _trailingStepPips.Value; set => _trailingStepPips.Value = value; }

	/// <summary>
	/// Required distance between price and moving average to trigger entries.
	/// </summary>
	public int DistanceFromMaPips { get => _distanceFromMaPips.Value; set => _distanceFromMaPips.Value = value; }

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }

	/// <summary>
	/// Number of bars to shift the moving average.
	/// </summary>
	public int MaShift { get => _maShift.Value; set => _maShift.Value = value; }

	/// <summary>
	/// Type of moving average smoothing.
	/// </summary>
	public MovingAverageModes MaMethod { get => _maMethod.Value; set => _maMethod.Value = value; }

	/// <summary>
	/// Price source used for moving average calculations.
	/// </summary>
	public AppliedPrices MaAppliedPrice { get => _appliedPrice.Value; set => _appliedPrice.Value = value; }

	/// <summary>
	/// Risk percentage used to calculate position size.
	/// </summary>
	public decimal RiskPercent { get => _riskPercent.Value; set => _riskPercent.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="MartingaleMaBreakoutStrategy"/> class.
	/// </summary>
	public MartingaleMaBreakoutStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 50)
			.SetDisplay("Stop Loss", "Stop loss in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
			.SetDisplay("Take Profit", "Take profit in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 5)
			.SetDisplay("Trailing Stop", "Trailing stop in pips", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
			.SetDisplay("Trailing Step", "Trailing step in pips", "Risk");

		_distanceFromMaPips = Param(nameof(DistanceFromMaPips), 5)
			.SetDisplay("Distance from MA", "Entry distance in pips", "Logic");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(6).TimeFrame())
			.SetDisplay("Candle Type", "Working candle type", "General");

		_maPeriod = Param(nameof(MaPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average period", "Indicators");

		_maShift = Param(nameof(MaShift), 3)
			.SetDisplay("MA Shift", "Moving average shift", "Indicators");

		_maMethod = Param(nameof(MaMethod), MovingAverageModes.Simple)
			.SetDisplay("MA Method", "Moving average smoothing", "Indicators");

		_appliedPrice = Param(nameof(MaAppliedPrice), AppliedPrices.Weighted)
			.SetDisplay("Applied Price", "Price used for MA", "Indicators");

		_riskPercent = Param(nameof(RiskPercent), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Risk %", "Risk percentage of equity", "Risk");
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

		// Drop all cached values before a new backtest run or restart.

		_movingAverage = null;
		_maHistory.Clear();
		_pipSize = 0m;
		_stopPrice = null;
		_takeProfitPrice = null;
		_entryPrice = 0m;
		_lastRecordedBalance = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips != 0 && TrailingStepPips == 0)
		{
			// Mirror the original validation: trailing requires a non-zero step.
			throw new InvalidOperationException("Trailing step must be greater than zero when trailing stop is enabled.");
		}

		// Reset runtime buffers before launching subscriptions.
		_maHistory.Clear();
		_stopPrice = null;
		_takeProfitPrice = null;
		_entryPrice = 0m;
		_lastRecordedBalance = null;

		// Build the moving average selected by the user.
		_movingAverage = CreateMovingAverage(MaMethod, MaPeriod);
		_pipSize = CalculatePipSize();

		// Subscribe to the candle feed and attach the processing callback.
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Calculate the input price according to the selected source.
		var price = GetAppliedPrice(candle, MaAppliedPrice);
		var maValue = _movingAverage.Process(price, candle.OpenTime, candle.State == CandleStates.Finished);

		if (Position != 0)
		{
			// Keep managing protective stops before evaluating new signals.
			ManageTrailing(candle.ClosePrice);
			if (CheckStops(candle.ClosePrice))
				return;
		}

		if (candle.State != CandleStates.Finished)
			return; // Work with closed candles only, as in the original EA.

		if (_movingAverage == null || !_movingAverage.IsFormed || maValue == null || !maValue.IsFinal)
			return; // Wait until the moving average produces stable values.

		var currentMa = maValue.ToDecimal();
		// Store values to emulate the shift parameter from the MQL implementation.
		UpdateMaHistory(currentMa);

		var shift = MaShift;
		if (_maHistory.Count <= shift)
			return; // Not enough samples to apply the requested shift.

		var shiftedMa = _maHistory[^1 - shift];
		var distance = DistanceFromMaPips * _pipSize;
		var closePrice = candle.ClosePrice; // Use the closing price as proxy for the latest tick.

		if (!IsFormedAndOnlineAndAllowTrading())
			return; // Honor trading availability rules provided by the base class.

		var aboveMa = closePrice - shiftedMa; // Positive when price trades above the shifted MA.
		var belowMa = shiftedMa - closePrice; // Positive when price trades below the shifted MA.

		if (aboveMa > distance && Position <= 0)
		{
			// Open or flip into a long position when distance exceeds the threshold.
			EnterLong(closePrice);
		}
		else if (belowMa > distance && Position >= 0)
		{
			// Open or flip into a short position when price drops far below the moving average.
			EnterShort(closePrice);
		}
	}

	private void EnterLong(decimal price)
	{
		// Determine required volume based on risk controls and martingale rules.
		var volume = CalculateVolume();
		if (volume <= 0m)
			return;

		BuyMarket(volume + Math.Abs(Position)); // Flip to a long position if we were short before.
		_entryPrice = price;

		_stopPrice = StopLossPips > 0 ? price - StopLossPips * _pipSize : null; // Configure protective levels in price units.
		_takeProfitPrice = TakeProfitPips > 0 ? price + TakeProfitPips * _pipSize : null;

		UpdateBalanceMarker();
	}

	private void EnterShort(decimal price)
	{
		// Determine required volume based on risk controls and martingale rules.
		var volume = CalculateVolume();
		if (volume <= 0m)
			return;

		SellMarket(volume + Math.Abs(Position)); // Flip to a short position if we were long before.
		_entryPrice = price;

		_stopPrice = StopLossPips > 0 ? price + StopLossPips * _pipSize : null; // Mirror the levels for short trades.
		_takeProfitPrice = TakeProfitPips > 0 ? price - TakeProfitPips * _pipSize : null;

		UpdateBalanceMarker();
	}

	private decimal CalculateVolume()
	{
		// Start from the base volume configured on the strategy.
		var baseVolume = Volume;
		var portfolioValue = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;

		if (StopLossPips > 0 && portfolioValue > 0m)
		{
			// Emulate MoneyFixedMargin by allocating a risk-based position size.
			var stopDistance = StopLossPips * _pipSize;
			if (stopDistance > 0m)
			{
				var multiplier = Security?.Multiplier ?? 1m;
				if (multiplier <= 0m)
					multiplier = 1m;

				var riskAmount = portfolioValue * RiskPercent / 100m;
				var calculated = riskAmount / (stopDistance * multiplier);
				if (calculated > 0m)
					baseVolume = calculated;
			}
		}

		baseVolume = ApplyMartingale(baseVolume); // Apply martingale step before placing the order.
		return baseVolume > 0m ? baseVolume : Volume;
	}

	private decimal ApplyMartingale(decimal volume)
	{
		// Compare the previous balance snapshot with the current one to adjust volume.
		var currentBalance = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;

		if (_lastRecordedBalance.HasValue)
		{
			if (_lastRecordedBalance.Value > currentBalance)
			{
				volume += 1m;
			}
			else if (_lastRecordedBalance.Value < currentBalance && volume > 1m)
			{
				volume -= 1m;
			}
		}

		var minVolume = Volume > 0m ? Volume : 1m;
		return volume < minVolume ? minVolume : volume;
	}

	private bool CheckStops(decimal price)
	{
		// Simulate broker side stop-loss and take-profit execution using candle close.

		if (Position > 0)
		{
			if (_stopPrice.HasValue && price <= _stopPrice.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetTradeState();
				return true;
			}

			if (_takeProfitPrice.HasValue && price >= _takeProfitPrice.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetTradeState();
				return true;
			}
		}
		else if (Position < 0)
		{
			if (_stopPrice.HasValue && price >= _stopPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetTradeState();
				return true;
			}

			if (_takeProfitPrice.HasValue && price <= _takeProfitPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetTradeState();
				return true;
			}
		}

		return false;
	}

	private void ManageTrailing(decimal price)
	{
		// Move the stop level only after price moved by the trailing step plus offset.

		if (TrailingStopPips <= 0 || Position == 0)
			return;

		var trailingStop = TrailingStopPips * _pipSize;
		var trailingStep = TrailingStepPips * _pipSize;

		if (Position > 0)
		{
			var profit = price - _entryPrice;
			if (profit > trailingStop + trailingStep)
			{
				var minTrigger = price - (trailingStop + trailingStep);
				if (!_stopPrice.HasValue || _stopPrice.Value < minTrigger)
					_stopPrice = price - trailingStop;
			}
		}
		else if (Position < 0)
		{
			var profit = _entryPrice - price;
			if (profit > trailingStop + trailingStep)
			{
				var minTrigger = price + (trailingStop + trailingStep);
				if (!_stopPrice.HasValue || _stopPrice.Value > minTrigger)
					_stopPrice = price + trailingStop;
			}
		}
	}

	private void ResetTradeState()
	{
		// Remove stored protective levels after position is closed.
		_stopPrice = null;
		_takeProfitPrice = null;
		_entryPrice = 0m;
	}

	private void UpdateMaHistory(decimal value)
	{
		// Maintain a compact buffer representing the shifted moving average.
		_maHistory.Add(value);
		var limit = Math.Max(1, MaShift + 1);
		if (_maHistory.Count > limit)
			_maHistory.RemoveAt(0);
	}

	private void UpdateBalanceMarker()
	{
		// Record the balance snapshot to evaluate martingale increments on the next entry.
		_lastRecordedBalance = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
	}

	private decimal CalculatePipSize()
	{
		// Recreate the original point adjustment for 3/5 digit forex symbols.
		var step = Security?.PriceStep ?? 0.0001m;
		var decimals = Security?.Decimals ?? 0;

		if (decimals == 3 || decimals == 5)
			return step * 10m;

		return step;
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPrices priceType)
	{
		// Derive the numeric input for the moving average from the candle data.
		return priceType switch
		{
			AppliedPrices.Close => candle.ClosePrice,
			AppliedPrices.Open => candle.OpenPrice,
			AppliedPrices.High => candle.HighPrice,
			AppliedPrices.Low => candle.LowPrice,
			AppliedPrices.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrices.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPrices.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice * 2m) / 4m,
			_ => candle.ClosePrice
		};
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageModes mode, int length)
	{
		// Instantiate the requested moving average implementation.
		return mode switch
		{
			MovingAverageModes.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageModes.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageModes.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageModes.Weighted => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length }
		};
	}

	/// <summary>
	/// Moving average smoothing types.
	/// </summary>
	public enum MovingAverageModes
	{
		Simple,
		Exponential,
		Smoothed,
		Weighted
	}

	/// <summary>
	/// Price sources used for moving average calculations.
	/// </summary>
	public enum AppliedPrices
	{
		Close,
		Open,
		High,
		Low,
		Median,
		Typical,
		Weighted
	}
}
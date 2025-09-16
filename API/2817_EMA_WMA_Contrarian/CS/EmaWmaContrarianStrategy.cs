using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Contrarian crossover between EMA and WMA calculated on candle open prices.
/// Opens a long position when EMA crosses below WMA and a short position on the opposite cross.
/// Supports fixed stop-loss, take-profit, and trailing stop management plus risk-based position sizing.
/// </summary>
public class EmaWmaContrarianStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _wmaPeriod;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _trailingStepPoints;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage? _ema;
	private WeightedMovingAverage? _wma;
	private bool _hasPrevious;
	private decimal _previousEma;
	private decimal _previousWma;

	private decimal? _entryPrice;
	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;

	/// <summary>
	/// EMA period.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// WMA period.
	/// </summary>
	public int WmaPeriod
	{
		get => _wmaPeriod.Value;
		set => _wmaPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price steps.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop step in price steps.
	/// </summary>
	public decimal TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Risk percentage used for position sizing.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Minimum contract volume used when risk sizing cannot be applied.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
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
	/// Initializes a new instance of <see cref="EmaWmaContrarianStrategy"/>.
	/// </summary>
	public EmaWmaContrarianStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 28)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA length calculated on candle open prices", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 2);

		_wmaPeriod = Param(nameof(WmaPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("WMA Period", "WMA length calculated on candle open prices", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(4, 40, 2);

		_stopLossPoints = Param(nameof(StopLossPoints), 50m)
			.SetDisplay("Stop Loss (points)", "Stop-loss distance expressed in price steps", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 150m, 10m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50m)
			.SetDisplay("Take Profit (points)", "Take-profit distance expressed in price steps", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 200m, 10m);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 50m)
			.SetDisplay("Trailing Stop (points)", "Trailing stop distance expressed in price steps", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 150m, 10m);

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 10m)
			.SetDisplay("Trailing Step (points)", "Minimal favorable move before the trailing stop is advanced", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(5m, 50m, 5m);

		_riskPercent = Param(nameof(RiskPercent), 10m)
			.SetDisplay("Risk Percent", "Portfolio percentage risked per trade", "Position Sizing")
			.SetCanOptimize(true)
			.SetOptimize(2m, 20m, 2m);

		_baseVolume = Param(nameof(BaseVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Base Volume", "Fallback volume when risk sizing is unavailable", "Position Sizing");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle type used for indicators", "General");
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

		_ema = null;
		_wma = null;
		_hasPrevious = false;
		_previousEma = 0m;
		_previousWma = 0m;
		ClearPositionState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Validate trailing configuration to match original expert advisor behaviour.
		if (TrailingStopPoints > 0 && TrailingStepPoints <= 0)
		{
			LogError("Trailing Step must be positive when Trailing Stop is enabled.");
			Stop();
			return;
		}

		_ema = new ExponentialMovingAverage { Length = EmaPeriod };
		_wma = new WeightedMovingAverage { Length = WmaPeriod };

		// Subscribe to candles and connect indicators.
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle);
		subscription.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawIndicator(area, _wma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Process only finished candles to avoid intrabar noise.
		if (candle.State != CandleStates.Finished)
			return;

		if (_ema == null || _wma == null)
			return;

		// Evaluate protective logic before generating new signals.
		ManageActivePosition(candle);

		var emaValue = _ema.Process(new CandleIndicatorValue(candle, candle.OpenPrice));
		var wmaValue = _wma.Process(new CandleIndicatorValue(candle, candle.OpenPrice));

		// Ensure indicators produced final values for this candle.
		if (!emaValue.IsFinal || !wmaValue.IsFinal)
			return;

		// Abort if the strategy is not ready to trade.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var ema = emaValue.GetValue<decimal>();
		var wma = wmaValue.GetValue<decimal>();

		if (!_hasPrevious)
		{
			_previousEma = ema;
			_previousWma = wma;
			_hasPrevious = true;
			return;
		}

		// Detect crossovers on open-price moving averages.
		var buySignal = ema < wma && _previousEma > _previousWma;
		var sellSignal = ema > wma && _previousEma < _previousWma;

		if (buySignal)
		{
			EnterLong(candle);
		}
		else if (sellSignal)
		{
			EnterShort(candle);
		}

		_previousEma = ema;
		_previousWma = wma;
	}

	private void ManageActivePosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			// Manage long position exits.
			var currentPrice = candle.ClosePrice;

			// Exit long when take-profit is reached.
			if (_takeProfitPrice is decimal tp && currentPrice >= tp)
			{
				SellMarket(Position);
				ClearPositionState();
				return;
			}

			// Exit long when stop-loss is hit.
			if (_stopLossPrice is decimal sl && currentPrice <= sl)
			{
				SellMarket(Position);
				ClearPositionState();
				return;
			}

			// Advance trailing stop for long trades.
			if (_entryPrice is decimal entry)
				UpdateTrailingForLong(currentPrice, entry);
		}
		else if (Position < 0)
		{
			// Manage short position exits.
			var currentPrice = candle.ClosePrice;

			// Exit short when take-profit is reached.
			if (_takeProfitPrice is decimal tp && currentPrice <= tp)
			{
				BuyMarket(Math.Abs(Position));
				ClearPositionState();
				return;
			}

			// Exit short when stop-loss is hit.
			if (_stopLossPrice is decimal sl && currentPrice >= sl)
			{
				BuyMarket(Math.Abs(Position));
				ClearPositionState();
				return;
			}

			// Advance trailing stop for short trades.
			if (_entryPrice is decimal entry)
				UpdateTrailingForShort(currentPrice, entry);
		}
		else
		{
			ClearPositionState();
		}
	}

	private void EnterLong(ICandleMessage candle)
	{
		var entryPrice = candle.ClosePrice;
		var volume = CalculateTradeVolume();

		if (volume <= 0)
			return;

		if (Position < 0)
		{
			// Close an existing short position before opening a new long.
			BuyMarket(Math.Abs(Position));
			ClearPositionState();
		}

		// Open the new long trade.
		BuyMarket(volume);
		SetupRiskLevels(entryPrice, true);
	}

	private void EnterShort(ICandleMessage candle)
	{
		var entryPrice = candle.ClosePrice;
		var volume = CalculateTradeVolume();

		if (volume <= 0)
			return;

		if (Position > 0)
		{
			// Close an existing long position before opening a new short.
			SellMarket(Position);
			ClearPositionState();
		}

		// Open the new short trade.
		SellMarket(volume);
		SetupRiskLevels(entryPrice, false);
	}

	private void SetupRiskLevels(decimal entryPrice, bool isLong)
	{
		var priceStep = Security?.PriceStep ?? 1m;
		var stopDistance = StopLossPoints > 0 ? StopLossPoints * priceStep : (decimal?)null;
		var takeProfitDistance = TakeProfitPoints > 0 ? TakeProfitPoints * priceStep : (decimal?)null;

		// Remember entry price for managing exits.
		_entryPrice = entryPrice;
		_stopLossPrice = stopDistance.HasValue
			? isLong ? entryPrice - stopDistance.Value : entryPrice + stopDistance.Value
			: null;

		_takeProfitPrice = takeProfitDistance.HasValue
			? isLong ? entryPrice + takeProfitDistance.Value : entryPrice - takeProfitDistance.Value
			: null;
	}

	private decimal CalculateTradeVolume()
	{
		// Default to configured base volume.
		var volume = BaseVolume;
		var portfolioValue = Portfolio?.CurrentValue ?? 0m;
		var priceStep = Security?.PriceStep ?? 1m;
		var stopDistance = StopLossPoints * priceStep;

		// Risk-based sizing uses stop distance to allocate capital.
		if (RiskPercent > 0 && portfolioValue > 0 && stopDistance > 0)
		{
			var riskCapital = portfolioValue * (RiskPercent / 100m);
			if (riskCapital > 0)
			{
				var rawVolume = riskCapital / stopDistance;
				var adjusted = AdjustVolume(rawVolume);
				if (adjusted > 0)
					volume = adjusted;
			}
		}

		return volume;
	}

	private decimal AdjustVolume(decimal volume)
	{
		// Align volume with instrument volume step.
		var step = Security?.VolumeStep ?? 1m;
		if (step <= 0)
			step = 1m;

		var adjusted = Math.Floor(volume / step) * step;
		if (adjusted <= 0)
			adjusted = step;

		var minVolume = Security?.VolumeStep ?? step;
		if (minVolume > 0 && adjusted < minVolume)
			adjusted = minVolume;

		return adjusted;
	}

	private void UpdateTrailingForLong(decimal currentPrice, decimal entryPrice)
	{
		if (TrailingStopPoints <= 0)
			return;

		var priceStep = Security?.PriceStep ?? 1m;
		var trailingDistance = TrailingStopPoints * priceStep;
		var trailingStep = TrailingStepPoints * priceStep;

		// Trailing stop only applies after sufficient favorable movement.
		if (currentPrice - entryPrice <= trailingDistance + trailingStep)
			return;

		var comparisonLevel = currentPrice - (trailingDistance + trailingStep);
		// Raise stop-loss closer to current price.
		if (_stopLossPrice is not decimal existing || existing < comparisonLevel)
			_stopLossPrice = currentPrice - trailingDistance;
	}

	private void UpdateTrailingForShort(decimal currentPrice, decimal entryPrice)
	{
		if (TrailingStopPoints <= 0)
			return;

		var priceStep = Security?.PriceStep ?? 1m;
		var trailingDistance = TrailingStopPoints * priceStep;
		var trailingStep = TrailingStepPoints * priceStep;

		// Trailing stop only applies after sufficient favorable movement.
		if (entryPrice - currentPrice <= trailingDistance + trailingStep)
			return;

		var comparisonLevel = currentPrice + trailingDistance + trailingStep;
		// Lower stop-loss toward market for short trades.
		if (_stopLossPrice is not decimal existing || existing > comparisonLevel)
			_stopLossPrice = currentPrice + trailingDistance;
	}

	private void ClearPositionState()
	{
		_entryPrice = null;
		_stopLossPrice = null;
		_takeProfitPrice = null;
	}
}

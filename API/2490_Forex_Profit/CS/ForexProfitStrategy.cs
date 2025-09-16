using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend-following strategy translated from the "Forex Profit" MQL expert.
/// Combines EMA alignment with Parabolic SAR confirmation and dynamic exits.
/// </summary>
public class ForexProfitStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _mediumEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<decimal> _takeProfitBuyPoints;
	private readonly StrategyParam<decimal> _takeProfitSellPoints;
	private readonly StrategyParam<decimal> _stopLossBuyPoints;
	private readonly StrategyParam<decimal> _stopLossSellPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _trailingStepPoints;
	private readonly StrategyParam<decimal> _profitThreshold;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _sarAcceleration;
	private readonly StrategyParam<decimal> _sarMaxAcceleration;

	private ExponentialMovingAverage _emaFast;
	private ExponentialMovingAverage _emaMedium;
	private ExponentialMovingAverage _emaSlow;
	private ParabolicSar _sar;

	private decimal? _ema10Prev;
	private decimal? _ema10PrevPrev;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	/// <summary>
	/// Medium EMA length.
	/// </summary>
	public int MediumEmaLength
	{
		get => _mediumEmaLength.Value;
		set => _mediumEmaLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowEmaLength
	{
		get => _slowEmaLength.Value;
		set => _slowEmaLength.Value = value;
	}

	/// <summary>
	/// Take profit distance for long positions in price steps.
	/// </summary>
	public decimal TakeProfitBuyPoints
	{
		get => _takeProfitBuyPoints.Value;
		set => _takeProfitBuyPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance for short positions in price steps.
	/// </summary>
	public decimal TakeProfitSellPoints
	{
		get => _takeProfitSellPoints.Value;
		set => _takeProfitSellPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance for long positions in price steps.
	/// </summary>
	public decimal StopLossBuyPoints
	{
		get => _stopLossBuyPoints.Value;
		set => _stopLossBuyPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance for short positions in price steps.
	/// </summary>
	public decimal StopLossSellPoints
	{
		get => _stopLossSellPoints.Value;
		set => _stopLossSellPoints.Value = value;
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
	/// Minimum step for trailing stop updates in price steps.
	/// </summary>
	public decimal TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Minimal profit in account currency required to exit on EMA reversal.
	/// </summary>
	public decimal ProfitThreshold
	{
		get => _profitThreshold.Value;
		set => _profitThreshold.Value = value;
	}

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initial Parabolic SAR acceleration.
	/// </summary>
	public decimal SarAcceleration
	{
		get => _sarAcceleration.Value;
		set => _sarAcceleration.Value = value;
	}

	/// <summary>
	/// Maximum Parabolic SAR acceleration.
	/// </summary>
	public decimal SarMaxAcceleration
	{
		get => _sarMaxAcceleration.Value;
		set => _sarMaxAcceleration.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="ForexProfitStrategy"/>.
	/// </summary>
	public ForexProfitStrategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 10)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA Length", "Length of the fast EMA", "Averages")
		.SetCanOptimize(true);

		_mediumEmaLength = Param(nameof(MediumEmaLength), 25)
		.SetGreaterThanZero()
		.SetDisplay("Medium EMA Length", "Length of the medium EMA", "Averages")
		.SetCanOptimize(true);

		_slowEmaLength = Param(nameof(SlowEmaLength), 50)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA Length", "Length of the slow EMA", "Averages")
		.SetCanOptimize(true);

		_takeProfitBuyPoints = Param(nameof(TakeProfitBuyPoints), 55m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit Long", "Take profit distance for buys (points)", "Risk")
		.SetCanOptimize(true);

		_takeProfitSellPoints = Param(nameof(TakeProfitSellPoints), 65m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit Short", "Take profit distance for sells (points)", "Risk")
		.SetCanOptimize(true);

		_stopLossBuyPoints = Param(nameof(StopLossBuyPoints), 60m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss Long", "Stop loss distance for buys (points)", "Risk")
		.SetCanOptimize(true);

		_stopLossSellPoints = Param(nameof(StopLossSellPoints), 85m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss Short", "Stop loss distance for sells (points)", "Risk")
		.SetCanOptimize(true);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 74m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Trailing Stop", "Trailing stop distance (points)", "Risk")
		.SetCanOptimize(true);

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 5m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Trailing Step", "Minimal trailing step (points)", "Risk")
		.SetCanOptimize(true);

		_profitThreshold = Param(nameof(ProfitThreshold), 10m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Profit Threshold", "Profit required for EMA exit", "Risk")
		.SetCanOptimize(true);

		_volume = Param(nameof(Volume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for calculations", "General");

		_sarAcceleration = Param(nameof(SarAcceleration), 0.02m)
		.SetGreaterThanZero()
		.SetDisplay("SAR Start", "Initial SAR acceleration", "Indicators")
		.SetCanOptimize(true);

		_sarMaxAcceleration = Param(nameof(SarMaxAcceleration), 0.2m)
		.SetGreaterThanZero()
		.SetDisplay("SAR Max", "Maximum SAR acceleration", "Indicators")
		.SetCanOptimize(true);
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

		_ema10Prev = null;
		_ema10PrevPrev = null;
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_emaFast = new ExponentialMovingAverage { Length = FastEmaLength };
		_emaMedium = new ExponentialMovingAverage { Length = MediumEmaLength };
		_emaSlow = new ExponentialMovingAverage { Length = SlowEmaLength };
		_sar = new ParabolicSar
		{
			Acceleration = SarAcceleration,
			AccelerationMax = SarMaxAcceleration
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.WhenNew(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _emaFast);
			DrawIndicator(area, _emaMedium);
			DrawIndicator(area, _emaSlow);
			DrawIndicator(area, _sar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var ema10Prev = _ema10Prev;
		var ema10PrevPrev = _ema10PrevPrev;

		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		var ema10Value = _emaFast.Process(median, candle.OpenTime, true).ToDecimal();
		var ema25Value = _emaMedium.Process(median, candle.OpenTime, true).ToDecimal();
		var ema50Value = _emaSlow.Process(median, candle.OpenTime, true).ToDecimal();
		var sarValue = _sar.Process(candle).ToDecimal();

		if (!_emaSlow.IsFormed || !_sar.IsFormed)
		{
			_ema10PrevPrev = ema10Prev;
			_ema10Prev = ema10Value;
			return;
		}

		var step = Security?.Step ?? 1m;
		if (step <= 0m)
			step = 1m;

		var stepPrice = Security?.StepPrice ?? step;

		var longSignal = ema10Value > ema25Value &&
			ema10Value > ema50Value &&
			ema10PrevPrev.HasValue &&
			ema10PrevPrev.Value <= ema50Value &&
			sarValue < candle.ClosePrice;

		var shortSignal = ema10Value < ema25Value &&
			ema10Value < ema50Value &&
			ema10PrevPrev.HasValue &&
			ema10PrevPrev.Value >= ema50Value &&
			sarValue > candle.ClosePrice;

		if (Position == 0m && IsFormedAndOnlineAndAllowTrading())
		{
			if (longSignal)
			{
				TryEnterLong(candle, step);
			}
			else if (shortSignal)
			{
				TryEnterShort(candle, step);
			}
		}
		else if (Position > 0m)
		{
			ManageLongPosition(candle, ema10Value, ema10Prev, step, stepPrice);
		}
		else if (Position < 0m)
		{
			ManageShortPosition(candle, ema10Value, ema10Prev, step, stepPrice);
		}

		_ema10PrevPrev = ema10Prev;
		_ema10Prev = ema10Value;
	}

	private void TryEnterLong(ICandleMessage candle, decimal step)
	{
		if (Volume <= 0m)
			return;

		CancelActiveOrders();
		BuyMarket(Volume);

		var entry = candle.ClosePrice;
		_entryPrice = entry;
		_stopPrice = entry - step * StopLossBuyPoints;
		_takeProfitPrice = entry + step * TakeProfitBuyPoints;
	}

	private void TryEnterShort(ICandleMessage candle, decimal step)
	{
		if (Volume <= 0m)
			return;

		CancelActiveOrders();
		SellMarket(Volume);

		var entry = candle.ClosePrice;
		_entryPrice = entry;
		_stopPrice = entry + step * StopLossSellPoints;
		_takeProfitPrice = entry - step * TakeProfitSellPoints;
	}

	private void ManageLongPosition(ICandleMessage candle, decimal ema10Value, decimal? ema10Prev, decimal step, decimal stepPrice)
	{
		if (_entryPrice == null)
			return;

		var profit = ComputeProfit(candle.ClosePrice, step, stepPrice);

		if (ema10Prev.HasValue && ema10Value < ema10Prev.Value && profit > ProfitThreshold)
		{
			CancelActiveOrders();
			SellMarket(Math.Abs(Position));
			ResetPositionTargets();
			return;
		}

		if (_takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
		{
			CancelActiveOrders();
			SellMarket(Math.Abs(Position));
			ResetPositionTargets();
			return;
		}

		if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
		{
			CancelActiveOrders();
			SellMarket(Math.Abs(Position));
			ResetPositionTargets();
			return;
		}

		UpdateLongTrailing(candle, step);
	}

	private void ManageShortPosition(ICandleMessage candle, decimal ema10Value, decimal? ema10Prev, decimal step, decimal stepPrice)
	{
		if (_entryPrice == null)
			return;

		var profit = ComputeProfit(candle.ClosePrice, step, stepPrice);

		if (ema10Prev.HasValue && ema10Value > ema10Prev.Value && profit > ProfitThreshold)
		{
			CancelActiveOrders();
			BuyMarket(Math.Abs(Position));
			ResetPositionTargets();
			return;
		}

		if (_takeProfitPrice.HasValue && candle.LowPrice <= _takeProfitPrice.Value)
		{
			CancelActiveOrders();
			BuyMarket(Math.Abs(Position));
			ResetPositionTargets();
			return;
		}

		if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
		{
			CancelActiveOrders();
			BuyMarket(Math.Abs(Position));
			ResetPositionTargets();
			return;
		}

		UpdateShortTrailing(candle, step);
	}

	private void UpdateLongTrailing(ICandleMessage candle, decimal step)
	{
		if (TrailingStopPoints <= 0m || _entryPrice == null)
			return;

		var trailingDistance = step * TrailingStopPoints;
		var trailingStep = step * TrailingStepPoints;
		var movement = candle.ClosePrice - _entryPrice.Value;

		if (movement > trailingDistance)
		{
			var newStop = candle.ClosePrice - trailingDistance;

			if (!_stopPrice.HasValue || newStop - _stopPrice.Value >= trailingStep)
				_stopPrice = newStop;
		}
	}

	private void UpdateShortTrailing(ICandleMessage candle, decimal step)
	{
		if (TrailingStopPoints <= 0m || _entryPrice == null)
			return;

		var trailingDistance = step * TrailingStopPoints;
		var trailingStep = step * TrailingStepPoints;
		var movement = _entryPrice.Value - candle.ClosePrice;

		if (movement > trailingDistance)
		{
			var newStop = candle.ClosePrice + trailingDistance;

			if (!_stopPrice.HasValue || _stopPrice.Value - newStop >= trailingStep)
				_stopPrice = newStop;
		}
	}

	private decimal ComputeProfit(decimal currentPrice, decimal step, decimal stepPrice)
	{
		if (_entryPrice == null || Position == 0m)
			return 0m;

		var ticks = (currentPrice - _entryPrice.Value) / step;
		return ticks * stepPrice * Position;
	}

	private void ResetPositionTargets()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
	}
}

namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// EMA(6)/EMA(12) crossover: enter while flat and close (never reverse on the
/// same crossover) when the close-signals switch is enabled.
/// </summary>
public class Ema612Strategy : Strategy
{
	private readonly StrategyParam<int> _shortEmaLength;
	private readonly StrategyParam<int> _longEmaLength;
	private readonly StrategyParam<bool> _useCloseSignals;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _trailingStopSteps;
	private readonly StrategyParam<decimal> _takeProfitSteps;
	private readonly StrategyParam<DataType> _candleType;
	private decimal? _previousFast;
	private decimal? _previousSlow;
	private Order _pendingOrder;

	public int ShortEmaLength { get => _shortEmaLength.Value; set => _shortEmaLength.Value = value; }
	public int LongEmaLength { get => _longEmaLength.Value; set => _longEmaLength.Value = value; }
	public bool UseCloseSignals { get => _useCloseSignals.Value; set => _useCloseSignals.Value = value; }
	public decimal OrderVolume { get => _orderVolume.Value; set => _orderVolume.Value = value; }
	public decimal TrailingStopSteps { get => _trailingStopSteps.Value; set => _trailingStopSteps.Value = value; }
	public decimal TakeProfitSteps { get => _takeProfitSteps.Value; set => _takeProfitSteps.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Ema612Strategy()
	{
		_shortEmaLength = Param(nameof(ShortEmaLength), 6).SetGreaterThanZero()
			.SetDisplay("Short EMA", "Period of the fast EMA", "Indicators");
		_longEmaLength = Param(nameof(LongEmaLength), 12).SetGreaterThanZero()
			.SetDisplay("Long EMA", "Period of the slow EMA", "Indicators");
		_useCloseSignals = Param(nameof(UseCloseSignals), true)
			.SetDisplay("Close Signals", "Close on an opposite cross instead of reversing", "Trading");
		_orderVolume = Param(nameof(OrderVolume), 1m).SetGreaterThanZero()
			.SetDisplay("Order Volume", "Entry size in lots", "Trading");
		_trailingStopSteps = Param(nameof(TrailingStopSteps), 0m).SetNotNegative()
			.SetDisplay("Trailing Stop Steps", "Zero disables trailing", "Protection");
		_takeProfitSteps = Param(nameof(TakeProfitSteps), 0m).SetNotNegative()
			.SetDisplay("Take Profit Steps", "Zero disables take profit", "Protection");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Input candle timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_previousFast = null;
		_previousSlow = null;
		_pendingOrder = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_previousFast = null;
		_previousSlow = null;
		_pendingOrder = null;
		Volume = NormalizeVolume(OrderVolume);
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			throw new InvalidOperationException("EMA6/12 requires a valid positive PriceStep.");

		if (TakeProfitSteps > 0m || TrailingStopSteps > 0m)
			StartProtection(
				takeProfit: TakeProfitSteps > 0m ? new Unit(TakeProfitSteps * step, UnitTypes.Absolute) : new Unit(),
				stopLoss: TrailingStopSteps > 0m ? new Unit(TrailingStopSteps * step, UnitTypes.Absolute) : new Unit(),
				isStopTrailing: TrailingStopSteps > 0m,
				useMarketOrders: true, isLocalStop: true);

		var fast = new ExponentialMovingAverage { Length = ShortEmaLength };
		var slow = new ExponentialMovingAverage { Length = LongEmaLength };
		SubscribeCandles(CandleType).Bind(fast, slow, ProcessCandle).Start();
	}

	private decimal NormalizeVolume(decimal requested)
	{
		if (Security?.VolumeStep is decimal step && step > 0m)
			requested = decimal.Floor(requested / step) * step;
		if (Security?.MinVolume is decimal min && min > 0m)
			requested = Math.Max(requested, min);
		if (Security?.MaxVolume is decimal max && max > 0m)
			requested = Math.Min(requested, max);
		return requested;
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var previousFast = _previousFast;
		var previousSlow = _previousSlow;
		_previousFast = fast;
		_previousSlow = slow;
		if (previousFast is null || previousSlow is null)
			return;

		var up = previousFast.Value <= previousSlow.Value && fast > slow;
		var down = previousFast.Value >= previousSlow.Value && fast < slow;
		if (!up && !down || _pendingOrder is not null &&
			_pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;

		var side = up ? Sides.Buy : Sides.Sell;
		if (Position == 0m)
		{
			var volume = NormalizeVolume(OrderVolume);
			if (volume > 0m)
				Submit(side, volume);
		}
		else if (UseCloseSignals && (side == Sides.Buy && Position < 0m ||
			side == Sides.Sell && Position > 0m))
		{
			Submit(side, Math.Abs(Position));
		}
	}

	private void Submit(Sides side, decimal volume)
	{
		_pendingOrder = new Order
		{
			Security = Security,
			Portfolio = Portfolio,
			Type = OrderTypes.Market,
			Side = side,
			Volume = volume,
		};
		RegisterOrder(_pendingOrder);
	}
}

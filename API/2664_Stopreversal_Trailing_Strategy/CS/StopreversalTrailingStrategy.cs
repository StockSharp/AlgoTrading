using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Stopreversal indicator based trailing stop strategy.
/// </summary>
public class StopreversalTrailingStrategy : Strategy
{
	private const int AtrPeriod = 15;

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _stopLossSteps;
	private readonly StrategyParam<int> _takeProfitSteps;
	private readonly StrategyParam<bool> _buyPositionOpen;
	private readonly StrategyParam<bool> _sellPositionOpen;
	private readonly StrategyParam<bool> _buyPositionClose;
	private readonly StrategyParam<bool> _sellPositionClose;
	private readonly StrategyParam<decimal> _npips;
	private readonly StrategyParam<AppliedPriceMode> _priceMode;
	private readonly StrategyParam<int> _signalBar;

	private readonly List<SignalInfo> _signals = new();

	private AverageTrueRange _atr = null!;
	private decimal? _previousStopLevel;
	private decimal? _previousPrice;

	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;

	/// <summary>
	/// Initializes a new instance of <see cref="StopreversalTrailingStrategy"/>.
	/// </summary>
	public StopreversalTrailingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Stopreversal timeframe", "General");

		_volume = Param(nameof(Volume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Base order volume", "Trading")
		.SetCanOptimize(true);

		_stopLossSteps = Param(nameof(StopLossSteps), 1000)
		.SetGreaterOrEqualZero()
		.SetDisplay("Stop Loss Steps", "Stop loss distance in price steps", "Risk")
		.SetCanOptimize(true);

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 2000)
		.SetGreaterOrEqualZero()
		.SetDisplay("Take Profit Steps", "Take profit distance in price steps", "Risk")
		.SetCanOptimize(true);

		_buyPositionOpen = Param(nameof(BuyPositionOpen), true)
		.SetDisplay("Open Long", "Allow opening long positions", "Trading");

		_sellPositionOpen = Param(nameof(SellPositionOpen), true)
		.SetDisplay("Open Short", "Allow opening short positions", "Trading");

		_buyPositionClose = Param(nameof(BuyPositionClose), true)
		.SetDisplay("Close Long", "Close long positions on sell signals", "Trading");

		_sellPositionClose = Param(nameof(SellPositionClose), true)
		.SetDisplay("Close Short", "Close short positions on buy signals", "Trading");

		_npips = Param(nameof(Npips), 0.004m)
		.SetGreaterThanZero()
		.SetDisplay("Trailing Offset", "Fractional offset applied to the stop line", "Indicator")
		.SetCanOptimize(true);

		_priceMode = Param(nameof(PriceMode), AppliedPriceMode.Close)
		.SetDisplay("Applied Price", "Price source used by the trailing stop", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
		.SetGreaterOrEqualZero()
		.SetDisplay("Signal Bar", "Bar delay before acting on a signal", "Indicator")
		.SetCanOptimize(true);
	}

	/// <summary>
	/// Candle subscription type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Base trading volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Stop loss distance measured in price steps.
	/// </summary>
	public int StopLossSteps
	{
		get => _stopLossSteps.Value;
		set => _stopLossSteps.Value = value;
	}

	/// <summary>
	/// Take profit distance measured in price steps.
	/// </summary>
	public int TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	/// <summary>
	/// Enable long entries.
	/// </summary>
	public bool BuyPositionOpen
	{
		get => _buyPositionOpen.Value;
		set => _buyPositionOpen.Value = value;
	}

	/// <summary>
	/// Enable short entries.
	/// </summary>
	public bool SellPositionOpen
	{
		get => _sellPositionOpen.Value;
		set => _sellPositionOpen.Value = value;
	}

	/// <summary>
	/// Close long positions on short signals.
	/// </summary>
	public bool BuyPositionClose
	{
		get => _buyPositionClose.Value;
		set => _buyPositionClose.Value = value;
	}

	/// <summary>
	/// Close short positions on long signals.
	/// </summary>
	public bool SellPositionClose
	{
		get => _sellPositionClose.Value;
		set => _sellPositionClose.Value = value;
	}

	/// <summary>
	/// Fractional offset used by the trailing stop calculation.
	/// </summary>
	public decimal Npips
	{
		get => _npips.Value;
		set => _npips.Value = value;
	}

	/// <summary>
	/// Price source used when computing the trailing level.
	/// </summary>
	public AppliedPriceMode PriceMode
	{
		get => _priceMode.Value;
		set => _priceMode.Value = value;
	}

	/// <summary>
	/// Number of bars to delay before reacting to a signal.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr = new AverageTrueRange
		{
			Length = AtrPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_atr, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateStops(candle);

		var price = GetAppliedPrice(candle);
		var prevStop = _previousStopLevel ?? price * (1m - Npips);
		var prevPrice = _previousPrice ?? price;
		var hasPrev = _previousStopLevel.HasValue && _previousPrice.HasValue;

		var stop = CalculateStop(price, prevPrice, prevStop);

		var buySignal = hasPrev && price > stop && prevPrice < prevStop && prevStop != 0m;
		var sellSignal = hasPrev && price < stop && prevPrice > prevStop && prevStop != 0m;

		_previousPrice = price;
		_previousStopLevel = stop;

		_signals.Add(new SignalInfo
		{
			BuySignal = buySignal,
			SellSignal = sellSignal,
			ClosePrice = candle.ClosePrice,
			Time = candle.CloseTime ?? candle.OpenTime
		});

		TrimSignals();

		if (_signals.Count <= SignalBar)
		return;

		var index = _signals.Count - 1 - SignalBar;
		if (index < 0)
		return;

		var signal = _signals[index];
		var allowTrading = IsFormedAndOnlineAndAllowTrading();

		ExecuteSignal(signal, allowTrading);
	}

	private void ExecuteSignal(SignalInfo signal, bool allowTrading)
	{
		if (SellPositionClose && signal.BuySignal && Position < 0)
		{
			BuyMarket(-Position);
			ResetShortStops();
		}

		if (BuyPositionClose && signal.SellSignal && Position > 0)
		{
			SellMarket(Position);
			ResetLongStops();
		}

		if (!allowTrading || Position != 0)
		return;

		if (BuyPositionOpen && signal.BuySignal)
		{
			if (Volume > 0)
			{
				BuyMarket(Volume);
				ResetShortStops();
				SetLongStops(signal.ClosePrice);
			}
		}
		else if (SellPositionOpen && signal.SellSignal)
		{
			if (Volume > 0)
			{
				SellMarket(Volume);
				ResetLongStops();
				SetShortStops(signal.ClosePrice);
			}
		}
	}

	private void UpdateStops(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_longStop is decimal longStop && candle.LowPrice <= longStop)
			{
				SellMarket(Position);
				ResetLongStops();
				return;
			}

			if (_longTake is decimal longTake && candle.HighPrice >= longTake)
			{
				SellMarket(Position);
				ResetLongStops();
			}
		}
		else if (Position < 0)
		{
			if (_shortStop is decimal shortStop && candle.HighPrice >= shortStop)
			{
				BuyMarket(-Position);
				ResetShortStops();
				return;
			}

			if (_shortTake is decimal shortTake && candle.LowPrice <= shortTake)
			{
				BuyMarket(-Position);
				ResetShortStops();
			}
		}
	}

	private void TrimSignals()
	{
		var max = Math.Max(SignalBar + 5, 10);
		if (_signals.Count > max)
		_signals.RemoveRange(0, _signals.Count - max);
	}

	private decimal CalculateStop(decimal price, decimal prevPrice, decimal prevStop)
	{
		var offset = Npips;

		if (price == prevStop)
		return prevStop;

		if (prevPrice < prevStop && price < prevStop)
		return Math.Min(prevStop, price * (1m + offset));

		if (prevPrice > prevStop && price > prevStop)
		return Math.Max(prevStop, price * (1m - offset));

		return price > prevStop
		? price * (1m - offset)
		: price * (1m + offset);
	}

	private void SetLongStops(decimal basePrice)
	{
		var step = GetEffectiveStep();

		_longStop = StopLossSteps > 0 ? basePrice - step * StopLossSteps : null;
		_longTake = TakeProfitSteps > 0 ? basePrice + step * TakeProfitSteps : null;
	}

	private void SetShortStops(decimal basePrice)
	{
		var step = GetEffectiveStep();

		_shortStop = StopLossSteps > 0 ? basePrice + step * StopLossSteps : null;
		_shortTake = TakeProfitSteps > 0 ? basePrice - step * TakeProfitSteps : null;
	}

	private void ResetLongStops()
	{
		_longStop = null;
		_longTake = null;
	}

	private void ResetShortStops()
	{
		_shortStop = null;
		_shortTake = null;
	}

	private decimal GetEffectiveStep()
	{
		var step = Security?.PriceStep;
		if (step is decimal s && s > 0)
		return s;

		step = Security?.MinPriceStep;
		if (step is decimal min && min > 0)
		return min;

		return 0.0001m;
	}

	private decimal GetAppliedPrice(ICandleMessage candle)
	{
		var open = candle.OpenPrice;
		var close = candle.ClosePrice;
		var high = candle.HighPrice;
		var low = candle.LowPrice;

		return PriceMode switch
		{
			AppliedPriceMode.Close => close,
			AppliedPriceMode.Open => open,
			AppliedPriceMode.High => high,
			AppliedPriceMode.Low => low,
			AppliedPriceMode.Median => (high + low) / 2m,
			AppliedPriceMode.Typical => (close + high + low) / 3m,
			AppliedPriceMode.Weighted => (2m * close + high + low) / 4m,
			AppliedPriceMode.Simple => (open + close) / 2m,
			AppliedPriceMode.Quarter => (open + close + high + low) / 4m,
			AppliedPriceMode.TrendFollow0 => close > open ? high : close < open ? low : close,
			AppliedPriceMode.TrendFollow1 => close > open ? (high + close) / 2m : close < open ? (low + close) / 2m : close,
			AppliedPriceMode.Demark => CalculateDemarkPrice(open, high, low, close),
			_ => close
		};
	}

	private static decimal CalculateDemarkPrice(decimal open, decimal high, decimal low, decimal close)
	{
		var result = high + low + close;

		if (close < open)
		result = (result + low) / 2m;
		else if (close > open)
		result = (result + high) / 2m;
		else
		result = (result + close) / 2m;

		return ((result - low) + (result - high)) / 2m;
	}

	private sealed class SignalInfo
	{
		public bool BuySignal { get; init; }
		public bool SellSignal { get; init; }
		public decimal ClosePrice { get; init; }
		public DateTimeOffset Time { get; init; }
	}

	/// <summary>
	/// Available price calculation modes.
	/// </summary>
	public enum AppliedPriceMode
	{
		/// <summary>
		/// Closing price.
		/// </summary>
		Close,

		/// <summary>
		/// Opening price.
		/// </summary>
		Open,

		/// <summary>
		/// Highest price.
		/// </summary>
		High,

		/// <summary>
		/// Lowest price.
		/// </summary>
		Low,

		/// <summary>
		/// Median price (high + low) / 2.
		/// </summary>
		Median,

		/// <summary>
		/// Typical price (close + high + low) / 3.
		/// </summary>
		Typical,

		/// <summary>
		/// Weighted close price (2 * close + high + low) / 4.
		/// </summary>
		Weighted,

		/// <summary>
		/// Simple average of open and close.
		/// </summary>
		Simple,

		/// <summary>
		/// Average of open, close, high and low.
		/// </summary>
		Quarter,

		/// <summary>
		/// Trend follow price variant 0.
		/// </summary>
		TrendFollow0,

		/// <summary>
		/// Trend follow price variant 1.
		/// </summary>
		TrendFollow1,

		/// <summary>
		/// Demark price formula.
		/// </summary>
		Demark
	}
}

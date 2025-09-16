using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that replicates the logic of the AverageChangeCandle expert.
/// The algorithm compares smoothed ratios of candle open/close relative to a baseline moving average.
/// </summary>
public class AverageChangeCandleStrategy : Strategy
{
	/// <summary>
	/// Available smoothing methods as in the original MQL implementation.
	/// </summary>
	public enum SmoothMethod
	{
		Sma,
		Ema,
		Smma,
		Lwma,
		Jjma,
		Jurx,
		Parma,
		T3,
		Vidya,
		Ama,
	}

	/// <summary>
	/// Price sources supported by the strategy.
	/// </summary>
	public enum AppliedPrice
	{
		Close = 1,
		Open = 2,
		High = 3,
		Low = 4,
		Median = 5,
		Typical = 6,
		Weighted = 7,
		Simpl = 8,
		Quarter = 9,
		TrendFollow0 = 10,
		TrendFollow1 = 11,
		Demark = 12,
	}

	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<SmoothMethod> _maMethod1;
	private readonly StrategyParam<int> _length1;
	private readonly StrategyParam<int> _phase1;
	private readonly StrategyParam<AppliedPrice> _appliedPrice;
	private readonly StrategyParam<SmoothMethod> _maMethod2;
	private readonly StrategyParam<int> _length2;
	private readonly StrategyParam<int> _phase2;
	private readonly StrategyParam<decimal> _pow;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private IIndicator _baseMa = null!;
	private IIndicator _openMa = null!;
	private IIndicator _closeMa = null!;
	private readonly List<int> _colorHistory = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="AverageChangeCandleStrategy"/>.
	/// </summary>
	public AverageChangeCandleStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume", "Trading");

		_maMethod1 = Param(nameof(MaMethod1), SmoothMethod.Lwma)
		.SetDisplay("Primary MA", "Smoothing method for the baseline", "Indicator");

		_length1 = Param(nameof(Length1), 12)
		.SetGreaterThanZero()
		.SetDisplay("Primary Length", "Length for the baseline moving average", "Indicator");

		_phase1 = Param(nameof(Phase1), 15)
		.SetDisplay("Primary Phase", "Phase parameter (used for Jurik variants)", "Indicator");

		_appliedPrice = Param(nameof(PriceSource), AppliedPrice.Median)
		.SetDisplay("Price Source", "Applied price for baseline smoothing", "Indicator");

		_maMethod2 = Param(nameof(MaMethod2), SmoothMethod.Jjma)
		.SetDisplay("Signal MA", "Smoothing method for ratio candles", "Indicator");

		_length2 = Param(nameof(Length2), 5)
		.SetGreaterThanZero()
		.SetDisplay("Signal Length", "Length for signal smoothing", "Indicator");

		_phase2 = Param(nameof(Phase2), 100)
		.SetDisplay("Signal Phase", "Phase parameter (used for Jurik variants)", "Indicator");

		_pow = Param(nameof(Power), 5m)
		.SetGreaterThanZero()
		.SetDisplay("Power", "Exponent applied to price ratios", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
		.SetGreaterOrEqual(0)
		.SetDisplay("Signal Bar", "Bar offset used for signals", "Trading");

		_buyOpen = Param(nameof(BuyOpenEnabled), true)
		.SetDisplay("Enable Long Entries", "Allow opening long positions", "Trading");

		_sellOpen = Param(nameof(SellOpenEnabled), true)
		.SetDisplay("Enable Short Entries", "Allow opening short positions", "Trading");

		_buyClose = Param(nameof(BuyCloseEnabled), true)
		.SetDisplay("Close Long On Short", "Close longs when a short signal appears", "Trading");

		_sellClose = Param(nameof(SellCloseEnabled), true)
		.SetDisplay("Close Short On Long", "Close shorts when a long signal appears", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 0m)
		.SetDisplay("Stop Loss", "Absolute stop-loss distance", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0m)
		.SetDisplay("Take Profit", "Absolute take-profit distance", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Candle series processed by the strategy", "Data");
	}

	/// <summary>
	/// Order volume used for entries.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Smoothing method for the primary moving average.
	/// </summary>
	public SmoothMethod MaMethod1
	{
		get => _maMethod1.Value;
		set => _maMethod1.Value = value;
	}

	/// <summary>
	/// Length of the primary moving average.
	/// </summary>
	public int Length1
	{
		get => _length1.Value;
		set => _length1.Value = value;
	}

	/// <summary>
	/// Phase parameter for the primary moving average.
	/// </summary>
	public int Phase1
	{
		get => _phase1.Value;
		set => _phase1.Value = value;
	}

	/// <summary>
	/// Applied price used for the baseline calculation.
	/// </summary>
	public AppliedPrice PriceSource
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// Smoothing method for the signal moving average.
	/// </summary>
	public SmoothMethod MaMethod2
	{
		get => _maMethod2.Value;
		set => _maMethod2.Value = value;
	}

	/// <summary>
	/// Length of the signal smoothing.
	/// </summary>
	public int Length2
	{
		get => _length2.Value;
		set => _length2.Value = value;
	}

	/// <summary>
	/// Phase parameter for the signal smoothing.
	/// </summary>
	public int Phase2
	{
		get => _phase2.Value;
		set => _phase2.Value = value;
	}

	/// <summary>
	/// Exponent applied to price ratios.
	/// </summary>
	public decimal Power
	{
		get => _pow.Value;
		set => _pow.Value = value;
	}

	/// <summary>
	/// Number of bars to skip before acting on a signal.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = Math.Max(0, value);
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyOpenEnabled
	{
		get => _buyOpen.Value;
		set => _buyOpen.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellOpenEnabled
	{
		get => _sellOpen.Value;
		set => _sellOpen.Value = value;
	}

	/// <summary>
	/// Close longs when a bearish signal appears.
	/// </summary>
	public bool BuyCloseEnabled
	{
		get => _buyClose.Value;
		set => _buyClose.Value = value;
	}

	/// <summary>
	/// Close shorts when a bullish signal appears.
	/// </summary>
	public bool SellCloseEnabled
	{
		get => _sellClose.Value;
		set => _sellClose.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in absolute price units.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = Math.Max(0m, value);
	}

	/// <summary>
	/// Take-profit distance expressed in absolute price units.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = Math.Max(0m, value);
	}

	/// <summary>
	/// Type of candles processed by the strategy.
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

		_colorHistory.Clear();
		_baseMa = null!;
		_openMa = null!;
		_closeMa = null!;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create smoothing indicators for the baseline and the transformed candles.
		_baseMa = CreateSmoothing(MaMethod1, Length1, Phase1);
		_openMa = CreateSmoothing(MaMethod2, Length2, Phase2);
		_closeMa = CreateSmoothing(MaMethod2, Length2, Phase2);

		_colorHistory.Clear();

		// Subscribe to the candle stream and process finished candles only.
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		// Configure risk protection using absolute distances.
		StartProtection(
		takeProfit: TakeProfitPoints > 0m ? new Unit(TakeProfitPoints, UnitTypes.Absolute) : new Unit(0m, UnitTypes.Absolute),
		stopLoss: StopLossPoints > 0m ? new Unit(StopLossPoints, UnitTypes.Absolute) : new Unit(0m, UnitTypes.Absolute));
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Process only completed candles.
		if (candle.State != CandleStates.Finished)
		return;

		// Skip trading when historical data is still loading or trading is paused.
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var appliedPrice = GetAppliedPrice(candle, PriceSource);
		var baseValue = ProcessIndicator(_baseMa, appliedPrice, candle.CloseTime);

		if (baseValue is null || baseValue.Value == 0m)
		return;

		// Transform open/close prices by the baseline ratio and power factor.
		var openRatio = candle.OpenPrice / baseValue.Value;
		var closeRatio = candle.ClosePrice / baseValue.Value;

		if (openRatio <= 0m || closeRatio <= 0m)
		return;

		var pow = (double)Power;
		var openPow = (decimal)Math.Pow((double)openRatio, pow);
		var closePow = (decimal)Math.Pow((double)closeRatio, pow);

		var openSmooth = ProcessIndicator(_openMa, openPow, candle.CloseTime);
		var closeSmooth = ProcessIndicator(_closeMa, closePow, candle.CloseTime);

		if (openSmooth is null || closeSmooth is null)
		return;

		var color = GetColor(openSmooth.Value, closeSmooth.Value);
		_colorHistory.Add(color);

		// Keep history compact to respect the configured offset.
		var requiredHistory = Math.Max(SignalBar + 3, 10);
		if (_colorHistory.Count > requiredHistory)
		_colorHistory.RemoveRange(0, _colorHistory.Count - requiredHistory);

		if (_colorHistory.Count <= SignalBar + 1)
		return;

		var currentIndex = _colorHistory.Count - 1 - SignalBar;
		if (currentIndex <= 0)
		return;

		var currentColor = _colorHistory[currentIndex];
		var previousColor = _colorHistory[currentIndex - 1];

		if (currentColor == previousColor)
		return;

		if (currentColor == 2)
		{
			// Bullish transition: close shorts and optionally open a long.
			if (SellCloseEnabled && Position < 0m)
			BuyMarket(Math.Abs(Position));

			if (BuyOpenEnabled && Position <= 0m)
			{
				var volume = OrderVolume + (Position < 0m ? Math.Abs(Position) : 0m);
				if (volume > 0m)
				BuyMarket(volume);
			}
		}
		else if (currentColor == 0)
		{
			// Bearish transition: close longs and optionally open a short.
			if (BuyCloseEnabled && Position > 0m)
			SellMarket(Position);

			if (SellOpenEnabled && Position >= 0m)
			{
				var volume = OrderVolume + (Position > 0m ? Position : 0m);
				if (volume > 0m)
				SellMarket(volume);
			}
		}
	}

	private static int GetColor(decimal openSmooth, decimal closeSmooth)
	{
		if (openSmooth < closeSmooth)
		return 2;

		if (openSmooth > closeSmooth)
		return 0;

		return 1;
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPrice price)
	{
		return price switch
		{
			AppliedPrice.Close => candle.ClosePrice,
			AppliedPrice.Open => candle.OpenPrice,
			AppliedPrice.High => candle.HighPrice,
			AppliedPrice.Low => candle.LowPrice,
			AppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrice.Typical => (candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 3m,
			AppliedPrice.Weighted => (2m * candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrice.Simpl => (candle.OpenPrice + candle.ClosePrice) / 2m,
			AppliedPrice.Quarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrice.TrendFollow0 => candle.ClosePrice > candle.OpenPrice
			? candle.HighPrice
			: candle.ClosePrice < candle.OpenPrice
			? candle.LowPrice
			: candle.ClosePrice,
			AppliedPrice.TrendFollow1 => candle.ClosePrice > candle.OpenPrice
			? (candle.HighPrice + candle.ClosePrice) / 2m
			: candle.ClosePrice < candle.OpenPrice
			? (candle.LowPrice + candle.ClosePrice) / 2m
			: candle.ClosePrice,
			AppliedPrice.Demark => CalculateDemarkPrice(candle),
			_ => candle.ClosePrice,
		};
	}

	private static decimal CalculateDemarkPrice(ICandleMessage candle)
	{
		var sum = candle.HighPrice + candle.LowPrice + candle.ClosePrice;

		if (candle.ClosePrice < candle.OpenPrice)
		sum = (sum + candle.LowPrice) / 2m;
		else if (candle.ClosePrice > candle.OpenPrice)
		sum = (sum + candle.HighPrice) / 2m;
		else
		sum = (sum + candle.ClosePrice) / 2m;

		return ((sum - candle.LowPrice) + (sum - candle.HighPrice)) / 2m;
	}

	private static IIndicator CreateSmoothing(SmoothMethod method, int length, int phase)
	{
		// Only a subset of smoothing methods is supported directly in StockSharp.
		// Unsupported methods fall back to EMA to keep the strategy functional.
		return method switch
		{
			SmoothMethod.Sma => new SimpleMovingAverage { Length = Math.Max(1, length) },
			SmoothMethod.Ema => new ExponentialMovingAverage { Length = Math.Max(1, length) },
			SmoothMethod.Smma => new SmoothedMovingAverage { Length = Math.Max(1, length) },
			SmoothMethod.Lwma => new WeightedMovingAverage { Length = Math.Max(1, length) },
			SmoothMethod.Jjma => new JurikMovingAverage { Length = Math.Max(1, length) },
			SmoothMethod.Ama => new KaufmanAdaptiveMovingAverage { Length = Math.Max(1, length) },
			_ => new ExponentialMovingAverage { Length = Math.Max(1, length) },
		};
	}

	private static decimal? ProcessIndicator(IIndicator indicator, decimal value, DateTimeOffset time)
	{
		var result = indicator.Process(new DecimalIndicatorValue(indicator, value, time));

		if (!result.IsFinal || !indicator.IsFormed)
		return null;

		return result.TryGetValue(out decimal output) ? output : null;
	}
}

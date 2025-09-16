namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Reflection;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Exp ColorX2MA X2 dual timeframe trend-following strategy.
/// Applies two-stage smoothing on higher and lower timeframes to detect color transitions of the ColorX2MA indicator.
/// </summary>
public class ExpColorX2MaX2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _trendCandleType;
	private readonly StrategyParam<SmoothMethod> _trendMethod1;
	private readonly StrategyParam<int> _trendLength1;
	private readonly StrategyParam<int> _trendPhase1;
	private readonly StrategyParam<SmoothMethod> _trendMethod2;
	private readonly StrategyParam<int> _trendLength2;
	private readonly StrategyParam<int> _trendPhase2;
	private readonly StrategyParam<AppliedPrice> _trendPrice;
	private readonly StrategyParam<int> _trendSignalBar;

	private readonly StrategyParam<DataType> _signalCandleType;
	private readonly StrategyParam<SmoothMethod> _signalMethod1;
	private readonly StrategyParam<int> _signalLength1;
	private readonly StrategyParam<int> _signalPhase1;
	private readonly StrategyParam<SmoothMethod> _signalMethod2;
	private readonly StrategyParam<int> _signalLength2;
	private readonly StrategyParam<int> _signalPhase2;
	private readonly StrategyParam<AppliedPrice> _signalPrice;
	private readonly StrategyParam<int> _signalSignalBar;

	private readonly StrategyParam<bool> _allowBuyOpen;
	private readonly StrategyParam<bool> _allowSellOpen;
	private readonly StrategyParam<bool> _allowBuyClosePrimary;
	private readonly StrategyParam<bool> _allowSellClosePrimary;
	private readonly StrategyParam<bool> _allowBuyCloseSecondary;
	private readonly StrategyParam<bool> _allowSellCloseSecondary;

	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;

	private IIndicator _trendMa1 = null!;
	private IIndicator _trendMa2 = null!;
	private IIndicator _signalMa1 = null!;
	private IIndicator _signalMa2 = null!;

	private readonly List<int> _trendColors = new();
	private readonly List<int> _signalColors = new();

	private decimal? _trendPrevValue;
	private decimal? _signalPrevValue;
	private int _trendDirection;

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpColorX2MaX2Strategy"/> class.
	/// </summary>
	public ExpColorX2MaX2Strategy()
	{
		_trendCandleType = Param(nameof(TrendCandleType), TimeSpan.FromHours(6).TimeFrame())
		.SetDisplay("Trend Candle", "Higher timeframe used for the trend ColorX2MA", "Trend");

		_trendMethod1 = Param(nameof(TrendMethod1), SmoothMethod.Sma)
		.SetDisplay("Trend MA 1", "First smoothing method on the trend timeframe", "Trend");
		_trendLength1 = Param(nameof(TrendLength1), 12)
		.SetGreaterThanZero()
		.SetDisplay("Trend Length 1", "Period for the first smoother", "Trend");
		_trendPhase1 = Param(nameof(TrendPhase1), 15)
		.SetDisplay("Trend Phase 1", "Phase parameter for Jurik smoothing", "Trend");
		_trendMethod2 = Param(nameof(TrendMethod2), SmoothMethod.Jurik)
		.SetDisplay("Trend MA 2", "Second smoothing method on the trend timeframe", "Trend");
		_trendLength2 = Param(nameof(TrendLength2), 5)
		.SetGreaterThanZero()
		.SetDisplay("Trend Length 2", "Period for the second smoother", "Trend");
		_trendPhase2 = Param(nameof(TrendPhase2), 15)
		.SetDisplay("Trend Phase 2", "Phase parameter for the second smoother", "Trend");
		_trendPrice = Param(nameof(TrendPrice), AppliedPrice.Close)
		.SetDisplay("Trend Price", "Applied price for the trend ColorX2MA", "Trend");
		_trendSignalBar = Param(nameof(TrendSignalBar), 1)
		.SetRange(0, 10)
		.SetDisplay("Trend Signal Bar", "Bar shift used to read the trend color", "Trend");

		_signalCandleType = Param(nameof(SignalCandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("Signal Candle", "Lower timeframe used for execution", "Signal");
		_signalMethod1 = Param(nameof(SignalMethod1), SmoothMethod.Sma)
		.SetDisplay("Signal MA 1", "First smoothing method on the signal timeframe", "Signal");
		_signalLength1 = Param(nameof(SignalLength1), 12)
		.SetGreaterThanZero()
		.SetDisplay("Signal Length 1", "Period for the first signal smoother", "Signal");
		_signalPhase1 = Param(nameof(SignalPhase1), 15)
		.SetDisplay("Signal Phase 1", "Phase parameter for the first signal smoother", "Signal");
		_signalMethod2 = Param(nameof(SignalMethod2), SmoothMethod.Jurik)
		.SetDisplay("Signal MA 2", "Second smoothing method on the signal timeframe", "Signal");
		_signalLength2 = Param(nameof(SignalLength2), 5)
		.SetGreaterThanZero()
		.SetDisplay("Signal Length 2", "Period for the second signal smoother", "Signal");
		_signalPhase2 = Param(nameof(SignalPhase2), 15)
		.SetDisplay("Signal Phase 2", "Phase parameter for the second signal smoother", "Signal");
		_signalPrice = Param(nameof(SignalPrice), AppliedPrice.Close)
		.SetDisplay("Signal Price", "Applied price for the signal ColorX2MA", "Signal");
		_signalSignalBar = Param(nameof(SignalSignalBar), 1)
		.SetRange(0, 10)
		.SetDisplay("Signal Bar", "Bar shift used to compare signal colors", "Signal");

		_allowBuyOpen = Param(nameof(AllowBuyOpen), true)
		.SetDisplay("Allow Buy", "Enable long entries", "Permissions");
		_allowSellOpen = Param(nameof(AllowSellOpen), true)
		.SetDisplay("Allow Sell", "Enable short entries", "Permissions");
		_allowBuyClosePrimary = Param(nameof(AllowBuyClosePrimary), true)
		.SetDisplay("Trend Close Buy", "Close longs when the trend turns bearish", "Permissions");
		_allowSellClosePrimary = Param(nameof(AllowSellClosePrimary), true)
		.SetDisplay("Trend Close Sell", "Close shorts when the trend turns bullish", "Permissions");
		_allowBuyCloseSecondary = Param(nameof(AllowBuyCloseSecondary), true)
		.SetDisplay("Signal Close Buy", "Close longs on a bearish signal color", "Permissions");
		_allowSellCloseSecondary = Param(nameof(AllowSellCloseSecondary), true)
		.SetDisplay("Signal Close Sell", "Close shorts on a bullish signal color", "Permissions");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
		.SetDisplay("Stop Loss", "Protective stop in points (multiplied by price step)", "Risk");
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
		.SetDisplay("Take Profit", "Profit target in points (multiplied by price step)", "Risk");
	}

	/// <summary>
	/// Higher timeframe used for trend detection.
	/// </summary>
	public DataType TrendCandleType
	{
		get => _trendCandleType.Value;
		set => _trendCandleType.Value = value;
	}

	/// <summary>
	/// First smoothing method for the trend ColorX2MA.
	/// </summary>
	public SmoothMethod TrendMethod1
	{
		get => _trendMethod1.Value;
		set => _trendMethod1.Value = value;
	}

	/// <summary>
	/// Length for the first trend smoother.
	/// </summary>
	public int TrendLength1
	{
		get => _trendLength1.Value;
		set => _trendLength1.Value = value;
	}

	/// <summary>
	/// Phase parameter for the first trend smoother.
	/// </summary>
	public int TrendPhase1
	{
		get => _trendPhase1.Value;
		set => _trendPhase1.Value = value;
	}

	/// <summary>
	/// Second smoothing method for the trend ColorX2MA.
	/// </summary>
	public SmoothMethod TrendMethod2
	{
		get => _trendMethod2.Value;
		set => _trendMethod2.Value = value;
	}

	/// <summary>
	/// Length for the second trend smoother.
	/// </summary>
	public int TrendLength2
	{
		get => _trendLength2.Value;
		set => _trendLength2.Value = value;
	}

	/// <summary>
	/// Phase parameter for the second trend smoother.
	/// </summary>
	public int TrendPhase2
	{
		get => _trendPhase2.Value;
		set => _trendPhase2.Value = value;
	}

	/// <summary>
	/// Applied price for the trend ColorX2MA.
	/// </summary>
	public AppliedPrice TrendPrice
	{
		get => _trendPrice.Value;
		set => _trendPrice.Value = value;
	}

	/// <summary>
	/// Number of bars to shift when reading the trend color.
	/// </summary>
	public int TrendSignalBar
	{
		get => _trendSignalBar.Value;
		set => _trendSignalBar.Value = value;
	}

	/// <summary>
	/// Lower timeframe used for execution.
	/// </summary>
	public DataType SignalCandleType
	{
		get => _signalCandleType.Value;
		set => _signalCandleType.Value = value;
	}

	/// <summary>
	/// First smoothing method for the signal ColorX2MA.
	/// </summary>
	public SmoothMethod SignalMethod1
	{
		get => _signalMethod1.Value;
		set => _signalMethod1.Value = value;
	}

	/// <summary>
	/// Length for the first signal smoother.
	/// </summary>
	public int SignalLength1
	{
		get => _signalLength1.Value;
		set => _signalLength1.Value = value;
	}

	/// <summary>
	/// Phase parameter for the first signal smoother.
	/// </summary>
	public int SignalPhase1
	{
		get => _signalPhase1.Value;
		set => _signalPhase1.Value = value;
	}

	/// <summary>
	/// Second smoothing method for the signal ColorX2MA.
	/// </summary>
	public SmoothMethod SignalMethod2
	{
		get => _signalMethod2.Value;
		set => _signalMethod2.Value = value;
	}

	/// <summary>
	/// Length for the second signal smoother.
	/// </summary>
	public int SignalLength2
	{
		get => _signalLength2.Value;
		set => _signalLength2.Value = value;
	}

	/// <summary>
	/// Phase parameter for the second signal smoother.
	/// </summary>
	public int SignalPhase2
	{
		get => _signalPhase2.Value;
		set => _signalPhase2.Value = value;
	}

	/// <summary>
	/// Applied price for the signal ColorX2MA.
	/// </summary>
	public AppliedPrice SignalPrice
	{
		get => _signalPrice.Value;
		set => _signalPrice.Value = value;
	}

	/// <summary>
	/// Number of bars to shift when comparing signal colors.
	/// </summary>
	public int SignalSignalBar
	{
		get => _signalSignalBar.Value;
		set => _signalSignalBar.Value = value;
	}

	/// <summary>
	/// Enable long entries.
	/// </summary>
	public bool AllowBuyOpen
	{
		get => _allowBuyOpen.Value;
		set => _allowBuyOpen.Value = value;
	}

	/// <summary>
	/// Enable short entries.
	/// </summary>
	public bool AllowSellOpen
	{
		get => _allowSellOpen.Value;
		set => _allowSellOpen.Value = value;
	}

	/// <summary>
	/// Close longs when the higher timeframe trend turns bearish.
	/// </summary>
	public bool AllowBuyClosePrimary
	{
		get => _allowBuyClosePrimary.Value;
		set => _allowBuyClosePrimary.Value = value;
	}

	/// <summary>
	/// Close shorts when the higher timeframe trend turns bullish.
	/// </summary>
	public bool AllowSellClosePrimary
	{
		get => _allowSellClosePrimary.Value;
		set => _allowSellClosePrimary.Value = value;
	}

	/// <summary>
	/// Close longs when the signal timeframe shows a bearish color.
	/// </summary>
	public bool AllowBuyCloseSecondary
	{
		get => _allowBuyCloseSecondary.Value;
		set => _allowBuyCloseSecondary.Value = value;
	}

	/// <summary>
	/// Close shorts when the signal timeframe shows a bullish color.
	/// </summary>
	public bool AllowSellCloseSecondary
	{
		get => _allowSellCloseSecondary.Value;
		set => _allowSellCloseSecondary.Value = value;
	}

	/// <summary>
	/// Protective stop in points multiplied by the price step.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Profit target in points multiplied by the price step.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security is null)
		yield break;

		yield return (Security, TrendCandleType);

		if (TrendCandleType != SignalCandleType)
		yield return (Security, SignalCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_trendColors.Clear();
		_signalColors.Clear();
		_trendPrevValue = null;
		_signalPrevValue = null;
		_trendDirection = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_trendMa1 = CreateMovingAverage(TrendMethod1, TrendLength1, TrendPhase1);
		_trendMa2 = CreateMovingAverage(TrendMethod2, TrendLength2, TrendPhase2);
		_signalMa1 = CreateMovingAverage(SignalMethod1, SignalLength1, SignalPhase1);
		_signalMa2 = CreateMovingAverage(SignalMethod2, SignalLength2, SignalPhase2);

		var trendSubscription = SubscribeCandles(TrendCandleType);
		trendSubscription.Bind(ProcessTrendCandle).Start();

		var signalSubscription = SubscribeCandles(SignalCandleType);
		signalSubscription.Bind(ProcessSignalCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, signalSubscription);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessTrendCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var price = GetAppliedPrice(candle, TrendPrice);
		var ma1Value = _trendMa1.Process(price, candle.OpenTime, true);
		if (!_trendMa1.IsFormed)
		return;

		var ma2Value = _trendMa2.Process(ma1Value.ToDecimal(), candle.OpenTime, true);
		var trendValue = ma2Value.ToDecimal();

		var color = 0;
		if (_trendPrevValue.HasValue)
		{
			if (trendValue > _trendPrevValue.Value)
			color = 1;
			else if (trendValue < _trendPrevValue.Value)
			color = 2;
		}

		_trendPrevValue = trendValue;
		_trendColors.Add(color);

		TrimBuffer(_trendColors, TrendSignalBar + 5);

		if (_trendColors.Count <= TrendSignalBar)
		return;

		var trendColor = _trendColors[^ (TrendSignalBar + 1)];

		_trendDirection = trendColor switch
		{
			1 => 1,
			2 => -1,
			_ => 0,
		};
	}

	private void ProcessSignalCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		ApplyProtectiveExits(candle);

		var price = GetAppliedPrice(candle, SignalPrice);
		var ma1Value = _signalMa1.Process(price, candle.OpenTime, true);
		if (!_signalMa1.IsFormed)
		return;

		var ma2Value = _signalMa2.Process(ma1Value.ToDecimal(), candle.OpenTime, true);
		var signalValue = ma2Value.ToDecimal();

		var color = 0;
		if (_signalPrevValue.HasValue)
		{
			if (signalValue > _signalPrevValue.Value)
			color = 1;
			else if (signalValue < _signalPrevValue.Value)
			color = 2;
		}

		_signalPrevValue = signalValue;
		_signalColors.Add(color);

		TrimBuffer(_signalColors, SignalSignalBar + 6);

		var shift0 = SignalSignalBar;
		var shift1 = SignalSignalBar + 1;
		if (_signalColors.Count <= shift1)
		return;

		var clr0 = _signalColors[^ (shift0 + 1)];
		var clr1 = _signalColors[^ (shift1 + 1)];

		var closeLong = AllowBuyCloseSecondary && clr1 == 2;
		var closeShort = AllowSellCloseSecondary && clr1 == 1;
		var openLong = false;
		var openShort = false;

		if (_trendDirection < 0)
		{
			if (AllowBuyClosePrimary)
			closeLong = true;
			if (AllowSellOpen && clr0 != 2 && clr1 == 2)
			openShort = true;
		}
		else if (_trendDirection > 0)
		{
			if (AllowSellClosePrimary)
			closeShort = true;
			if (AllowBuyOpen && clr0 != 1 && clr1 == 1)
			openLong = true;
		}

		if (closeLong && Position > 0)
		SellMarket(Position);

		if (closeShort && Position < 0)
		BuyMarket(-Position);

		if (openLong && Position <= 0)
		{
			var volume = Volume + (Position < 0 ? -Position : 0m);
			if (volume > 0)
			BuyMarket(volume);
		}
		else if (openShort && Position >= 0)
		{
			var volume = Volume + (Position > 0 ? Position : 0m);
			if (volume > 0)
			SellMarket(volume);
		}
	}

	private void ApplyProtectiveExits(ICandleMessage candle)
	{
		var step = Security?.PriceStep ?? 0m;
		var stopDistance = StopLossPoints > 0 ? (step > 0 ? StopLossPoints * step : StopLossPoints) : 0m;
		var takeDistance = TakeProfitPoints > 0 ? (step > 0 ? TakeProfitPoints * step : TakeProfitPoints) : 0m;

		if (Position > 0)
		{
			if (stopDistance > 0m && candle.LowPrice <= PositionPrice - stopDistance)
			SellMarket(Position);
			else if (takeDistance > 0m && candle.HighPrice >= PositionPrice + takeDistance)
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (stopDistance > 0m && candle.HighPrice >= PositionPrice + stopDistance)
			BuyMarket(-Position);
			else if (takeDistance > 0m && candle.LowPrice <= PositionPrice - takeDistance)
			BuyMarket(-Position);
		}
	}

	private static void TrimBuffer(List<int> buffer, int maxSize)
	{
		while (buffer.Count > maxSize && maxSize > 0)
		buffer.RemoveAt(0);
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
			AppliedPrice.Weighted => (candle.ClosePrice * 2m + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrice.Simple => (candle.OpenPrice + candle.ClosePrice) / 2m,
			AppliedPrice.Quarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrice.TrendFollow0 => candle.ClosePrice > candle.OpenPrice ? candle.HighPrice : candle.ClosePrice < candle.OpenPrice ? candle.LowPrice : candle.ClosePrice,
			AppliedPrice.TrendFollow1 => candle.ClosePrice > candle.OpenPrice ? (candle.HighPrice + candle.ClosePrice) / 2m : candle.ClosePrice < candle.OpenPrice ? (candle.LowPrice + candle.ClosePrice) / 2m : candle.ClosePrice,
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

	private static IIndicator CreateMovingAverage(SmoothMethod method, int length, int phase)
	{
		return method switch
		{
			SmoothMethod.Sma => new SimpleMovingAverage { Length = length },
			SmoothMethod.Ema => new ExponentialMovingAverage { Length = length },
			SmoothMethod.Smma => new SmoothedMovingAverage { Length = length },
			SmoothMethod.Lwma => new WeightedMovingAverage { Length = length },
			SmoothMethod.Jurik => CreateJurikMovingAverage(length, phase),
			_ => throw new NotSupportedException($"Smoothing method '{method}' is not supported."),
		};
	}

	private static IIndicator CreateJurikMovingAverage(int length, int phase)
	{
		var jma = new JurikMovingAverage { Length = length };
		var phaseProperty = jma.GetType().GetProperty("Phase", BindingFlags.Public | BindingFlags.Instance);
		if (phaseProperty != null && phaseProperty.CanWrite)
		phaseProperty.SetValue(jma, phase);
		return jma;
	}
}

/// <summary>
/// Supported smoothing methods for the ColorX2MA calculation.
/// </summary>
public enum SmoothMethod
{
	/// <summary>
	/// Simple moving average.
	/// </summary>
	Sma,

	/// <summary>
	/// Exponential moving average.
	/// </summary>
	Ema,

	/// <summary>
	/// Smoothed moving average.
	/// </summary>
	Smma,

	/// <summary>
	/// Linear weighted moving average.
	/// </summary>
	Lwma,

	/// <summary>
	/// Jurik moving average.
	/// </summary>
	Jurik
}

/// <summary>
/// Applied price options matching the original MQL indicator.
/// </summary>
public enum AppliedPrice
{
	/// <summary>
	/// Close price.
	/// </summary>
	Close,

	/// <summary>
	/// Open price.
	/// </summary>
	Open,

	/// <summary>
	/// High price.
	/// </summary>
	High,

	/// <summary>
	/// Low price.
	/// </summary>
	Low,

	/// <summary>
	/// Median price (high + low) / 2.
	/// </summary>
	Median,

	/// <summary>
	/// Typical price (high + low + close) / 3.
	/// </summary>
	Typical,

	/// <summary>
	/// Weighted price (high + low + close * 2) / 4.
	/// </summary>
	Weighted,

	/// <summary>
	/// Simple average of open and close.
	/// </summary>
	Simple,

	/// <summary>
	/// Quarted price (open + high + low + close) / 4.
	/// </summary>
	Quarter,

	/// <summary>
	/// TrendFollow 1 price.
	/// </summary>
	TrendFollow0,

	/// <summary>
	/// TrendFollow 2 price.
	/// </summary>
	TrendFollow1,

	/// <summary>
	/// Demark price.
	/// </summary>
	Demark
}

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Adaptive channel crossover strategy ported from the MQL5 expert "Exp_XAng_Zad_C_Tm_MMRec".
/// Combines the custom XAng Zad C indicator, time window filters, and money-management rules
/// that scale down position size after a configurable number of losing trades.
/// </summary>
public class XAngZadCTmMmRecStrategy : Strategy
{
	private readonly StrategyParam<decimal> _normalVolume;
	private readonly StrategyParam<decimal> _reducedVolume;
	private readonly StrategyParam<int> _buyTotalTrigger;
	private readonly StrategyParam<int> _buyLossTrigger;
	private readonly StrategyParam<int> _sellTotalTrigger;
	private readonly StrategyParam<int> _sellLossTrigger;
	private readonly StrategyParam<bool> _enableBuyEntries;
	private readonly StrategyParam<bool> _enableSellEntries;
	private readonly StrategyParam<bool> _enableBuyExit;
	private readonly StrategyParam<bool> _enableSellExit;
	private readonly StrategyParam<bool> _useTradingWindow;
	private readonly StrategyParam<TimeSpan> _windowStart;
	private readonly StrategyParam<TimeSpan> _windowEnd;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<int> _signalShift;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<XAngZadCIndicator.SmoothMethod> _smoothMethod;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _maPhase;
	private readonly StrategyParam<decimal> _ki;
	private readonly StrategyParam<XAngZadCIndicator.AppliedPrice> _appliedPrice;

	private readonly List<decimal> _buyPnls = new();
	private readonly List<decimal> _sellPnls = new();
	private readonly List<decimal> _upHistory = new();
	private readonly List<decimal> _downHistory = new();

	private Sides? _activeSide;
	private decimal _entryPrice;
	private decimal _entryVolume;

	public decimal NormalVolume
	{
		get => _normalVolume.Value;
		set => _normalVolume.Value = value;
	}

	public decimal ReducedVolume
	{
		get => _reducedVolume.Value;
		set => _reducedVolume.Value = value;
	}

	public int BuyTotalTrigger
	{
		get => _buyTotalTrigger.Value;
		set => _buyTotalTrigger.Value = value;
	}

	public int BuyLossTrigger
	{
		get => _buyLossTrigger.Value;
		set => _buyLossTrigger.Value = value;
	}

	public int SellTotalTrigger
	{
		get => _sellTotalTrigger.Value;
		set => _sellTotalTrigger.Value = value;
	}

	public int SellLossTrigger
	{
		get => _sellLossTrigger.Value;
		set => _sellLossTrigger.Value = value;
	}

	public bool EnableBuyEntries
	{
		get => _enableBuyEntries.Value;
		set => _enableBuyEntries.Value = value;
	}

	public bool EnableSellEntries
	{
		get => _enableSellEntries.Value;
		set => _enableSellEntries.Value = value;
	}

	public bool EnableBuyExit
	{
		get => _enableBuyExit.Value;
		set => _enableBuyExit.Value = value;
	}

	public bool EnableSellExit
	{
		get => _enableSellExit.Value;
		set => _enableSellExit.Value = value;
	}

	public bool UseTradingWindow
	{
		get => _useTradingWindow.Value;
		set => _useTradingWindow.Value = value;
	}

	public TimeSpan WindowStart
	{
		get => _windowStart.Value;
		set => _windowStart.Value = value;
	}

	public TimeSpan WindowEnd
	{
		get => _windowEnd.Value;
		set => _windowEnd.Value = value;
	}

	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	public int SignalShift
	{
		get => _signalShift.Value;
		set => _signalShift.Value = Math.Max(0, value);
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public XAngZadCIndicator.SmoothMethod SmoothMethod
	{
		get => _smoothMethod.Value;
		set => _smoothMethod.Value = value;
	}

	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = Math.Max(1, value);
	}

	public int MaPhase
	{
		get => _maPhase.Value;
		set => _maPhase.Value = value;
	}

	public decimal Ki
	{
		get => _ki.Value;
		set => _ki.Value = value <= 0m ? 1m : value;
	}

	public XAngZadCIndicator.AppliedPrice AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	public XAngZadCTmMmRecStrategy()
	{
		_normalVolume = Param(nameof(NormalVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Normal Volume", "Default order size", "Risk")
			.SetCanOptimize(true);

		_reducedVolume = Param(nameof(ReducedVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Reduced Volume", "Volume used after losing streak", "Risk")
			.SetCanOptimize(true);

		_buyTotalTrigger = Param(nameof(BuyTotalTrigger), 5)
			.SetGreaterThanZero()
			.SetDisplay("Buy Total Trigger", "Number of past buy trades to inspect", "Risk")
			.SetCanOptimize(true);

		_buyLossTrigger = Param(nameof(BuyLossTrigger), 3)
			.SetGreaterThanZero()
			.SetDisplay("Buy Loss Trigger", "Losing buy trades required to reduce volume", "Risk")
			.SetCanOptimize(true);

		_sellTotalTrigger = Param(nameof(SellTotalTrigger), 5)
			.SetGreaterThanZero()
			.SetDisplay("Sell Total Trigger", "Number of past sell trades to inspect", "Risk")
			.SetCanOptimize(true);

		_sellLossTrigger = Param(nameof(SellLossTrigger), 3)
			.SetGreaterThanZero()
			.SetDisplay("Sell Loss Trigger", "Losing sell trades required to reduce volume", "Risk")
			.SetCanOptimize(true);

		_enableBuyEntries = Param(nameof(EnableBuyEntries), true)
			.SetDisplay("Enable Buy Entries", "Allow long entries", "Logic");

		_enableSellEntries = Param(nameof(EnableSellEntries), true)
			.SetDisplay("Enable Sell Entries", "Allow short entries", "Logic");

		_enableBuyExit = Param(nameof(EnableBuyExit), true)
			.SetDisplay("Enable Buy Exit", "Allow closing long positions on signals", "Logic");

		_enableSellExit = Param(nameof(EnableSellExit), true)
			.SetDisplay("Enable Sell Exit", "Allow closing short positions on signals", "Logic");

		_useTradingWindow = Param(nameof(UseTradingWindow), true)
			.SetDisplay("Use Trading Window", "Restrict trading to a daily time window", "Schedule");

		_windowStart = Param(nameof(WindowStart), new TimeSpan(0, 0, 0))
			.SetDisplay("Window Start", "Session opening time (HH:MM)", "Schedule");

		_windowEnd = Param(nameof(WindowEnd), new TimeSpan(23, 59, 0))
			.SetDisplay("Window End", "Session closing time (HH:MM)", "Schedule");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetDisplay("Stop Loss", "Protective stop in price steps", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetDisplay("Take Profit", "Profit target in price steps", "Risk");

		_signalShift = Param(nameof(SignalShift), 1)
			.SetDisplay("Signal Shift", "Number of closed bars used for signal comparison", "Logic")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Indicator Timeframe", "Candle type used for the indicator", "Data");

		_smoothMethod = Param(nameof(SmoothMethod), XAngZadCIndicator.SmoothMethod.Jjma)
			.SetDisplay("Smoothing Method", "Moving average used inside the indicator", "Indicator");

		_maLength = Param(nameof(MaLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Indicator smoothing length", "Indicator");

		_maPhase = Param(nameof(MaPhase), 15)
			.SetDisplay("MA Phase", "Phase parameter for adaptive smoothing", "Indicator");

		_ki = Param(nameof(Ki), 4.000001m)
			.SetGreaterThanZero()
			.SetDisplay("Ki", "Smoothing ratio for price envelopes", "Indicator");

		_appliedPrice = Param(nameof(AppliedPrice), XAngZadCIndicator.AppliedPrice.Close)
			.SetDisplay("Applied Price", "Price source used by the indicator", "Indicator");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
	return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_buyPnls.Clear();
		_sellPnls.Clear();
		_upHistory.Clear();
		_downHistory.Clear();
		_activeSide = null;
		_entryPrice = 0m;
		_entryVolume = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var indicator = new XAngZadCIndicator
		{
			Ki = Ki,
			Length = MaLength,
			Phase = MaPhase,
			Method = SmoothMethod,
			PriceSource = AppliedPrice
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(indicator, ProcessIndicator)
			.Start();
	}

	private void ProcessIndicator(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (value is not XAngZadCValue angValue)
		return;

		var upper = angValue.Upper;
		var lower = angValue.Lower;

		// Store the most recent indicator values so we can mimic the MQL "SignalBar" shift logic.
		_upHistory.Insert(0, upper);
		_downHistory.Insert(0, lower);

		TrimHistory(_upHistory);
		TrimHistory(_downHistory);

		var shift = SignalShift;
		if (_upHistory.Count <= shift + 1 || _downHistory.Count <= shift + 1)
			return;

		var upCurrent = _upHistory[shift];
		var upPrev = _upHistory[shift + 1];
		var downCurrent = _downHistory[shift];
		var downPrev = _downHistory[shift + 1];

		// Evaluate crossover conditions using the stored history snapshots.
		var buySignal = false;
		var sellSignal = false;
		var closeBuy = false;
		var closeSell = false;

		if (upPrev > downPrev)
		{
			if (EnableBuyEntries && upCurrent <= downCurrent)
				buySignal = true;

			if (EnableSellExit)
				closeSell = true;
		}

		if (upPrev < downPrev)
		{
			if (EnableSellEntries && upCurrent >= downCurrent)
				sellSignal = true;

			if (EnableBuyExit)
				closeBuy = true;
		}

		var inWindow = !UseTradingWindow || IsWithinTradingWindow(candle);

		// Leave the market immediately when trading outside the configured hours.
		if (UseTradingWindow && !inWindow && Position != 0m)
		{
			ClosePosition(candle, candle.ClosePrice);
			buySignal = false;
			sellSignal = false;
		}

		if (Position != 0m)
		{
			if (ManageStops(candle))
			return;
		}

		if (closeBuy && Position > 0m)
		{
			ClosePosition(candle, candle.ClosePrice);
		}

		if (closeSell && Position < 0m)
		{
			ClosePosition(candle, candle.ClosePrice);
		}

		if (!inWindow || Position != 0m)
		return;

		if (buySignal)
		{
			var volume = GetBuyVolume();
			if (volume > 0m)
				OpenPosition(Sides.Buy, volume, candle.ClosePrice);
		}
		else if (sellSignal)
		{
			var volume = GetSellVolume();
			if (volume > 0m)
				OpenPosition(Sides.Sell, volume, candle.ClosePrice);
		}
	}

	private void OpenPosition(Sides side, decimal volume, decimal price)
	{
		if (side == Sides.Buy)
		BuyMarket(volume);
		else
		SellMarket(volume);

		_activeSide = side;
		_entryPrice = price;
		_entryVolume = volume;
	}

	private void ClosePosition(ICandleMessage candle, decimal exitPrice)
	{
		if (Position > 0m)
		{
			var volume = Position;
			SellMarket(volume);
			if (_activeSide == Sides.Buy)
			{
				var pnl = (exitPrice - _entryPrice) * volume;
				RegisterTradeResult(Sides.Buy, pnl);
			}
		}
		else if (Position < 0m)
		{
			var volume = Math.Abs(Position);
			BuyMarket(volume);
			if (_activeSide == Sides.Sell)
			{
				var pnl = (_entryPrice - exitPrice) * volume;
				RegisterTradeResult(Sides.Sell, pnl);
			}
		}

		_activeSide = null;
		_entryPrice = 0m;
		_entryVolume = 0m;
	}

	private bool ManageStops(ICandleMessage candle)
{
	// Nothing to do if there is no active trade to protect.
	if (_activeSide is null)
		return false;

	// Evaluate fixed stops that are defined in multiples of the instrument price step.
	var step = Security?.PriceStep ?? 1m;
	var currentSide = _activeSide.Value;
	var position = Position;

	if (currentSide == Sides.Buy && position > 0m)
	{
		if (StopLoss > 0m)
		{
			var stop = _entryPrice - StopLoss * step;
			if (candle.LowPrice <= stop)
			{
				ClosePosition(candle, stop);
				return true;
			}
		}

		if (TakeProfit > 0m)
		{
			var target = _entryPrice + TakeProfit * step;
			if (candle.HighPrice >= target)
			{
				ClosePosition(candle, target);
				return true;
			}
		}
	}
	else if (currentSide == Sides.Sell && position < 0m)
	{
		if (StopLoss > 0m)
		{
			var stop = _entryPrice + StopLoss * step;
			if (candle.HighPrice >= stop)
			{
				ClosePosition(candle, stop);
				return true;
			}
		}

		if (TakeProfit > 0m)
		{
			var target = _entryPrice - TakeProfit * step;
			if (candle.LowPrice <= target)
			{
				ClosePosition(candle, target);
				return true;
			}
		}
	}

	return false;
}
private bool IsWithinTradingWindow(ICandleMessage candle)
{
	var time = candle.OpenTime.TimeOfDay;
	var start = WindowStart;
	var end = WindowEnd;

	if (start == end)
	{
		return false;
	}

	if (start < end)
	{
		return time >= start && time < end;
	}

	return time >= start || time < end;
}
private decimal GetBuyVolume()
	{
		return ShouldReduceVolume(_buyPnls, BuyTotalTrigger, BuyLossTrigger) ? ReducedVolume : NormalVolume;
	}

	private decimal GetSellVolume()
	{
		return ShouldReduceVolume(_sellPnls, SellTotalTrigger, SellLossTrigger) ? ReducedVolume : NormalVolume;
	}

	private bool ShouldReduceVolume(List<decimal> pnls, int totalTrigger, int lossTrigger)
	{
		if (lossTrigger <= 0 || totalTrigger <= 0)
		return false;

		var count = 0;
		var losses = 0;

		for (var i = pnls.Count - 1; i >= 0 && count < totalTrigger; i--)
		{
			count++;
			if (pnls[i] < 0m)
				losses++;
		}

		return losses >= lossTrigger;
	}

	private void RegisterTradeResult(Sides side, decimal pnl)
	{
		var list = side == Sides.Buy ? _buyPnls : _sellPnls;
		list.Add(pnl);

		var total = side == Sides.Buy ? BuyTotalTrigger : SellTotalTrigger;
		var losses = side == Sides.Buy ? BuyLossTrigger : SellLossTrigger;
		var limit = Math.Max(total, losses);
		if (limit <= 0)
		limit = 1;

		while (list.Count > limit)
		list.RemoveAt(0);
	}

	private void TrimHistory(List<decimal> history)
	{
		var required = SignalShift + 2;
		while (history.Count > required)
		history.RemoveAt(history.Count - 1);
	}
}

/// <summary>
/// Indicator that replicates the MQL5 XAng Zad C channel calculation.
/// Produces adaptive upper and lower envelopes smoothed by a configurable moving average.
/// </summary>
public class XAngZadCIndicator : BaseIndicator<decimal>
{
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
		Ama
	}

	public enum AppliedPrice
	{
		Close,
		Open,
		High,
		Low,
		Median,
		Typical,
		Weighted,
		Simpl,
		Quarter,
		TrendFollow0,
		TrendFollow1,
		Demark
	}

	public decimal Ki { get; set; } = 4.000001m;
	public int Length { get; set; } = 7;
	public int Phase { get; set; } = 15;
	public SmoothMethod Method { get; set; } = SmoothMethod.Jjma;
	public AppliedPrice PriceSource { get; set; } = AppliedPrice.Close;

	private decimal? _prevPrice;
	private decimal? _prevZ;
	private decimal? _prevZ2;
	private IIndicator _upperMa = null!;
	private IIndicator _lowerMa = null!;
	private SmoothMethod _cachedMethod;
	private int _cachedLength;

	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
			return new XAngZadCValue(this, input, 0m, 0m);

		EnsureIndicators();

		var price = SelectPrice(candle);
		var prevPrice = _prevPrice ?? price;
		var prevZ = _prevZ ?? prevPrice;
		var prevZ2 = _prevZ2 ?? prevPrice;

		var newZ = prevZ;
		var newZ2 = prevZ2;

		if ((price > prevZ && price > prevPrice) || (price < prevZ && price < prevPrice))
			newZ = prevZ + (price - prevZ) / Ki;

		if ((price > prevZ2 && price < prevPrice) || (price < prevZ2 && price > prevPrice))
			newZ2 = prevZ2 + (price - prevZ2) / Ki;

		var upper = _upperMa.Process(new DecimalIndicatorValue(_upperMa, newZ, input.Time)).ToDecimal();
		var lower = _lowerMa.Process(new DecimalIndicatorValue(_lowerMa, newZ2, input.Time)).ToDecimal();

		_prevPrice = price;
		_prevZ = newZ;
		_prevZ2 = newZ2;

		return new XAngZadCValue(this, input, upper, lower);
	}

	public override void Reset()
	{
		base.Reset();

		_prevPrice = null;
		_prevZ = null;
		_prevZ2 = null;
		_upperMa = null!;
		_lowerMa = null!;
	}

	private void EnsureIndicators()
	{
		if (_upperMa != null && _cachedMethod == Method && _cachedLength == Length)
			return;

		_upperMa = CreateMovingAverage();
		_lowerMa = CreateMovingAverage();
		_cachedMethod = Method;
		_cachedLength = Length;
	}

	private IIndicator CreateMovingAverage()
	{
		// The original indicator supports many exotic smoothers. We fall back to EMA for
		// the ones that do not have direct equivalents in StockSharp.
		return Method switch
		{
			SmoothMethod.Sma => new SimpleMovingAverage { Length = Length },
			SmoothMethod.Smma => new SmoothedMovingAverage { Length = Length },
			SmoothMethod.Lwma => new WeightedMovingAverage { Length = Length },
			_ => new ExponentialMovingAverage { Length = Length }
		};
	}

	private decimal SelectPrice(ICandleMessage candle)
	{
		return PriceSource switch
		{
			AppliedPrice.Open => candle.OpenPrice,
			AppliedPrice.High => candle.HighPrice,
			AppliedPrice.Low => candle.LowPrice,
			AppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPrice.Weighted => (2m * candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrice.Simpl => (candle.OpenPrice + candle.ClosePrice) / 2m,
			AppliedPrice.Quarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrice.TrendFollow0 => candle.ClosePrice >= candle.OpenPrice ? candle.HighPrice : candle.LowPrice,
			AppliedPrice.TrendFollow1 => candle.ClosePrice >= candle.OpenPrice
				? (candle.HighPrice + candle.ClosePrice) / 2m
				: (candle.LowPrice + candle.ClosePrice) / 2m,
			AppliedPrice.Demark => CalculateDemarkPrice(candle),
			_ => candle.ClosePrice
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
}
public class XAngZadCValue : ComplexIndicatorValue
{
	public XAngZadCValue(IIndicator indicator, IIndicatorValue input, decimal upper, decimal lower)
	: base(indicator, input, (nameof(Upper), upper), (nameof(Lower), lower))
	{
	}

	public decimal Upper => (decimal)GetValue(nameof(Upper));

	public decimal Lower => (decimal)GetValue(nameof(Lower));
}

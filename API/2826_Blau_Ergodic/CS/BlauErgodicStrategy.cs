namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Blau Ergodic oscillator strategy with multiple signal modes.
/// </summary>
public class BlauErgodicStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<BlauErgodicMode> _mode;
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<int> _firstSmoothingLength;
	private readonly StrategyParam<int> _secondSmoothingLength;
	private readonly StrategyParam<int> _thirdSmoothingLength;
	private readonly StrategyParam<int> _signalSmoothingLength;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<AppliedPrice> _appliedPrice;
	private readonly StrategyParam<bool> _allowBuyEntry;
	private readonly StrategyParam<bool> _allowSellEntry;
	private readonly StrategyParam<bool> _allowBuyExit;
	private readonly StrategyParam<bool> _allowSellExit;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;

	private EMA _momEma1 = null!;
	private EMA _momEma2 = null!;
	private EMA _momEma3 = null!;
	private EMA _absMomEma1 = null!;
	private EMA _absMomEma2 = null!;
	private EMA _absMomEma3 = null!;
	private EMA _signal = null!;

	private readonly List<decimal> _priceHistory = new();
	private readonly List<decimal> _mainHistory = new();
	private readonly List<decimal?> _signalHistory = new();

	private decimal _entryPrice;

	/// <summary>
	/// Trading candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Mode that defines signal detection.
	/// </summary>
	public BlauErgodicMode Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>
	/// Momentum lookback length.
	/// </summary>
	public int MomentumLength
	{
		get => _momentumLength.Value;
		set => _momentumLength.Value = value;
	}

	/// <summary>
	/// First EMA smoothing length for momentum streams.
	/// </summary>
	public int FirstSmoothingLength
	{
		get => _firstSmoothingLength.Value;
		set => _firstSmoothingLength.Value = value;
	}

	/// <summary>
	/// Second EMA smoothing length for momentum streams.
	/// </summary>
	public int SecondSmoothingLength
	{
		get => _secondSmoothingLength.Value;
		set => _secondSmoothingLength.Value = value;
	}

	/// <summary>
	/// Third EMA smoothing length for momentum streams.
	/// </summary>
	public int ThirdSmoothingLength
	{
		get => _thirdSmoothingLength.Value;
		set => _thirdSmoothingLength.Value = value;
	}

	/// <summary>
	/// EMA length applied to the signal line.
	/// </summary>
	public int SignalSmoothingLength
	{
		get => _signalSmoothingLength.Value;
		set => _signalSmoothingLength.Value = value;
	}

	/// <summary>
	/// Number of closed candles used to read signals.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Price source used inside the indicator.
	/// </summary>
	public AppliedPrice AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// Allows opening long positions.
	/// </summary>
	public bool AllowBuyEntry
	{
		get => _allowBuyEntry.Value;
		set => _allowBuyEntry.Value = value;
	}

	/// <summary>
	/// Allows opening short positions.
	/// </summary>
	public bool AllowSellEntry
	{
		get => _allowSellEntry.Value;
		set => _allowSellEntry.Value = value;
	}

	/// <summary>
	/// Allows closing long positions on indicator signals.
	/// </summary>
	public bool AllowBuyExit
	{
		get => _allowBuyExit.Value;
		set => _allowBuyExit.Value = value;
	}

	/// <summary>
	/// Allows closing short positions on indicator signals.
	/// </summary>
	public bool AllowSellExit
	{
		get => _allowSellExit.Value;
		set => _allowSellExit.Value = value;
	}

	/// <summary>
	/// Stop loss distance in points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance in points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public BlauErgodicStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for calculations", "General");

		_mode = Param(nameof(Mode), BlauErgodicMode.Twist)
		.SetDisplay("Mode", "Signal interpretation mode", "Trading");

		_momentumLength = Param(nameof(MomentumLength), 2)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Length", "Momentum lookback for Blau Ergodic", "Indicator");

		_firstSmoothingLength = Param(nameof(FirstSmoothingLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("First Smoothing", "First EMA smoothing length", "Indicator");

		_secondSmoothingLength = Param(nameof(SecondSmoothingLength), 5)
		.SetGreaterThanZero()
		.SetDisplay("Second Smoothing", "Second EMA smoothing length", "Indicator");

		_thirdSmoothingLength = Param(nameof(ThirdSmoothingLength), 3)
		.SetGreaterThanZero()
		.SetDisplay("Third Smoothing", "Third EMA smoothing length", "Indicator");

		_signalSmoothingLength = Param(nameof(SignalSmoothingLength), 3)
		.SetGreaterThanZero()
		.SetDisplay("Signal Smoothing", "EMA length for signal line", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
		.SetGreaterThanZero()
		.SetDisplay("Signal Bar", "Completed bars back to evaluate", "Trading");

		_appliedPrice = Param(nameof(AppliedPrice), AppliedPrice.Close)
		.SetDisplay("Applied Price", "Price source for calculations", "Indicator");

		_allowBuyEntry = Param(nameof(AllowBuyEntry), true)
		.SetDisplay("Allow Buy Entry", "Allow opening long positions", "Trading");

		_allowSellEntry = Param(nameof(AllowSellEntry), true)
		.SetDisplay("Allow Sell Entry", "Allow opening short positions", "Trading");

		_allowBuyExit = Param(nameof(AllowBuyExit), true)
		.SetDisplay("Allow Buy Exit", "Allow closing long positions", "Trading");

		_allowSellExit = Param(nameof(AllowSellExit), true)
		.SetDisplay("Allow Sell Exit", "Allow closing short positions", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
		.SetDisplay("Stop Loss", "Protective stop loss distance", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
		.SetDisplay("Take Profit", "Profit target distance", "Risk");
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

		_priceHistory.Clear();
		_mainHistory.Clear();
		_signalHistory.Clear();
		_entryPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize EMA cascades for momentum and absolute momentum streams.
		_momEma1 = new EMA { Length = FirstSmoothingLength };
		_momEma2 = new EMA { Length = SecondSmoothingLength };
		_momEma3 = new EMA { Length = ThirdSmoothingLength };

		_absMomEma1 = new EMA { Length = FirstSmoothingLength };
		_absMomEma2 = new EMA { Length = SecondSmoothingLength };
		_absMomEma3 = new EMA { Length = ThirdSmoothingLength };

		_signal = new EMA { Length = SignalSmoothingLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		// Store price history for momentum calculation.
		var price = GetAppliedPrice(candle);
		_priceHistory.Add(price);
		TrimHistory(_priceHistory, MomentumLength + SignalBar + 10);

		if (MomentumLength <= 0)
		return;

		var backShift = MomentumLength - 1;
		if (_priceHistory.Count <= backShift)
		return;

		var referenceIndex = _priceHistory.Count - 1 - backShift;
		var referencePrice = _priceHistory[referenceIndex];
		var momentum = price - referencePrice;
		var absMomentum = Math.Abs(momentum);

		// Process cascaded EMA filters for momentum and absolute momentum.
		var time = candle.ServerTime;

		var mom1 = _momEma1.Process(momentum, time);
		var abs1 = _absMomEma1.Process(absMomentum, time);

		if (!mom1.IsFormed || !abs1.IsFormed)
		return;

		var mom2 = _momEma2.Process(mom1.ToDecimal(), time);
		var abs2 = _absMomEma2.Process(abs1.ToDecimal(), time);

		if (!mom2.IsFormed || !abs2.IsFormed)
		return;

		var mom3 = _momEma3.Process(mom2.ToDecimal(), time);
		var abs3 = _absMomEma3.Process(abs2.ToDecimal(), time);

		if (!mom3.IsFormed || !abs3.IsFormed)
		return;

		var smoothedMomentum = mom3.ToDecimal();
		var smoothedAbsMomentum = abs3.ToDecimal();

		var main = smoothedAbsMomentum == 0m ? 0m : 100m * smoothedMomentum / smoothedAbsMomentum;

		var signalValue = _signal.Process(main, time);
		decimal? signal = null;
		if (signalValue.IsFormed)
		signal = signalValue.ToDecimal();

		AppendIndicatorHistory(main, signal);

		EvaluateSignals(candle);
	}

	private void EvaluateSignals(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var currentIndex = SignalBar - 1;
		if (currentIndex < 0)
		return;

		if (!TryGetMainValue(currentIndex, out var currentMain))
		return;

		var buyOpen = false;
		var sellOpen = false;
		var buyClose = false;
		var sellClose = false;

		switch (Mode)
		{
			case BlauErgodicMode.Breakdown:
			{
				if (!TryGetMainValue(currentIndex + 1, out var previousMain))
				return;

				// Close shorts when histogram stays above zero and longs when it stays below zero.
				if (AllowSellExit && currentMain > 0m)
				sellClose = true;

				if (AllowBuyExit && currentMain < 0m)
				buyClose = true;

				if (AllowBuyEntry && previousMain <= 0m && currentMain > 0m)
				buyOpen = true;

				if (AllowSellEntry && previousMain >= 0m && currentMain < 0m)
				sellOpen = true;

				break;
			}
			case BlauErgodicMode.Twist:
			{
				if (!TryGetMainValue(currentIndex + 1, out var previousMain) ||
				!TryGetMainValue(currentIndex + 2, out var olderMain))
				return;

				// Detect turning points by comparing slope changes.
				if (AllowSellExit && previousMain < currentMain)
				sellClose = true;

				if (AllowBuyExit && previousMain > currentMain)
				buyClose = true;

				if (AllowBuyEntry && olderMain > previousMain && previousMain < currentMain)
				buyOpen = true;

				if (AllowSellEntry && olderMain < previousMain && previousMain > currentMain)
				sellOpen = true;

				break;
			}
			case BlauErgodicMode.CloudTwist:
			{
				if (!TryGetMainValue(currentIndex + 1, out var previousMain) ||
				!TryGetSignalValue(currentIndex, out var currentSignal) ||
				!TryGetSignalValue(currentIndex + 1, out var previousSignal))
				return;

				// Close when main line crosses the signal line.
				if (AllowSellExit && currentMain > currentSignal)
				sellClose = true;

				if (AllowBuyExit && currentMain < currentSignal)
				buyClose = true;

				if (AllowBuyEntry && previousMain <= previousSignal && currentMain > currentSignal)
				buyOpen = true;

				if (AllowSellEntry && previousMain >= previousSignal && currentMain < currentSignal)
				sellOpen = true;

				break;
			}
		}

		var (closeLongByStops, closeShortByStops) = EvaluateStops(candle);

		var forceBuyClose = closeLongByStops;
		var forceSellClose = closeShortByStops;

		if (closeLongByStops)
		buyClose = true;

		if (closeShortByStops)
		sellClose = true;

		ExecuteOrders(candle, buyOpen, sellOpen, buyClose, sellClose, forceBuyClose, forceSellClose);
	}

	private (bool closeLong, bool closeShort) EvaluateStops(ICandleMessage candle)
	{
		var closeLong = false;
		var closeShort = false;

		var priceStep = Security?.PriceStep ?? 0m;
		var stopLossDistance = priceStep > 0m && StopLossPoints > 0 ? StopLossPoints * priceStep : 0m;
		var takeProfitDistance = priceStep > 0m && TakeProfitPoints > 0 ? TakeProfitPoints * priceStep : 0m;

		// Evaluate protective levels against the current candle range.
		if (Position > 0)
		{
			if (stopLossDistance > 0m && candle.LowPrice <= _entryPrice - stopLossDistance)
			closeLong = true;

			if (takeProfitDistance > 0m && candle.HighPrice >= _entryPrice + takeProfitDistance)
			closeLong = true;
		}
		else if (Position < 0)
		{
			if (stopLossDistance > 0m && candle.HighPrice >= _entryPrice + stopLossDistance)
			closeShort = true;

			if (takeProfitDistance > 0m && candle.LowPrice <= _entryPrice - takeProfitDistance)
			closeShort = true;
		}

		return (closeLong, closeShort);
	}

	private void ExecuteOrders(ICandleMessage candle, bool buyOpen, bool sellOpen, bool buyClose, bool sellClose, bool forceBuyClose, bool forceSellClose)
	{
		if (((buyClose && AllowBuyExit) || forceBuyClose) && Position > 0)
		{
			// Close existing long position.
			SellMarket(Position);
			_entryPrice = 0m;
		}

		if (((sellClose && AllowSellExit) || forceSellClose) && Position < 0)
		{
			// Close existing short position.
			BuyMarket(-Position);
			_entryPrice = 0m;
		}

		if (buyOpen && AllowBuyEntry && Position <= 0)
		{
			// Reverse any short exposure and open a new long.
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_entryPrice = candle.ClosePrice;
		}

		if (sellOpen && AllowSellEntry && Position >= 0)
		{
			// Reverse any long exposure and open a new short.
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_entryPrice = candle.ClosePrice;
		}
	}

	private decimal GetAppliedPrice(ICandleMessage candle)
	{
		return AppliedPrice switch
		{
			AppliedPrice.Open => candle.OpenPrice,
			AppliedPrice.High => candle.HighPrice,
			AppliedPrice.Low => candle.LowPrice,
			AppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPrice.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice + candle.ClosePrice) / 4m,
			AppliedPrice.Simple => (candle.OpenPrice + candle.ClosePrice) / 2m,
			AppliedPrice.Quarter => (candle.HighPrice + candle.LowPrice + candle.OpenPrice + candle.ClosePrice) / 4m,
			_ => candle.ClosePrice,
		};
	}

	private void AppendIndicatorHistory(decimal main, decimal? signal)
	{
		_mainHistory.Add(main);
		_signalHistory.Add(signal);

		var maxSize = Math.Max(SignalBar + 5, 10);
		TrimHistory(_mainHistory, maxSize);
		TrimHistory(_signalHistory, maxSize);
	}

	private static void TrimHistory<T>(IList<T> values, int maxSize)
	{
		while (values.Count > maxSize)
		values.RemoveAt(0);
	}

	private bool TryGetMainValue(int shift, out decimal value)
	{
		value = default;
		var index = _mainHistory.Count - 1 - shift;
		if (index < 0 || index >= _mainHistory.Count)
		return false;

		value = _mainHistory[index];
		return true;
	}

	private bool TryGetSignalValue(int shift, out decimal value)
	{
		value = default;
		var index = _signalHistory.Count - 1 - shift;
		if (index < 0 || index >= _signalHistory.Count)
		return false;

		var raw = _signalHistory[index];
		if (raw is null)
		return false;

		value = raw.Value;
		return true;
	}

	/// <summary>
	/// Trading modes supported by the strategy.
	/// </summary>
	public enum BlauErgodicMode
	{
		Breakdown,
		Twist,
		CloudTwist,
	}

	/// <summary>
	/// Price types available for indicator calculation.
	/// </summary>
	public enum AppliedPrice
	{
		Close,
		Open,
		High,
		Low,
		Median,
		Typical,
		Weighted,
		Simple,
		Quarter,
	}
}

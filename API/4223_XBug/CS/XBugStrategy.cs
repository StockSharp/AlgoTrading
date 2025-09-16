namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Moving average crossover strategy converted from the MQL4 expert "X bug".
/// Uses median price averages, optional signal reversal, and protective order management.
/// </summary>
public class XBugStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _fastShift;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _slowShift;
	private readonly StrategyParam<bool> _closeOnSignal;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<CandlePrice> _appliedPrice;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage? _fastMa;
	private SimpleMovingAverage? _slowMa;
	private readonly List<decimal> _fastHistory = new();
	private readonly List<decimal> _slowHistory = new();
	private decimal _pipSize;

	/// <summary>
	/// Initializes a new instance of the <see cref="XBugStrategy"/> class.
	/// </summary>
	public XBugStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order volume", "Base volume for each market entry.", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 70m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop loss (pips)", "Distance of the protective stop in pips.", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 5000m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take profit (pips)", "Distance of the profit target in pips.", "Risk");

		_useTrailingStop = Param(nameof(UseTrailingStop), true)
			.SetDisplay("Use trailing", "Enable trailing stop management.", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 90m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing stop (pips)", "Trailing distance applied after a position moves into profit.", "Risk");

		_fastPeriod = Param(nameof(FastPeriod), 1)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA period", "Length of the fast moving average.", "Indicators");

		_fastShift = Param(nameof(FastShift), 0)
			.SetGreaterOrEqualZero()
			.SetDisplay("Fast MA shift", "Bars to shift the fast average when evaluating signals.", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA period", "Length of the slow moving average.", "Indicators");

		_slowShift = Param(nameof(SlowShift), 10)
			.SetGreaterOrEqualZero()
			.SetDisplay("Slow MA shift", "Bars to shift the slow average when evaluating signals.", "Indicators");

		_closeOnSignal = Param(nameof(CloseOnSignal), true)
			.SetDisplay("Close on signal", "Close opposite positions when a new crossover appears.", "Trading");

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse signals", "Invert buy and sell directions.", "Trading");

		_appliedPrice = Param(nameof(AppliedPrice), CandlePrice.Median)
			.SetDisplay("Applied price", "Price source fed into the moving averages.", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Primary timeframe used for signals.", "General");
	}

	/// <summary>
	/// Order volume used for new market entries.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Enables trailing stop handling when true.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Trailing stop distance measured in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Length of the fast moving average.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Shift applied to the fast moving average when evaluating crossovers.
	/// </summary>
	public int FastShift
	{
		get => _fastShift.Value;
		set => _fastShift.Value = value;
	}

	/// <summary>
	/// Length of the slow moving average.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Shift applied to the slow moving average when evaluating crossovers.
	/// </summary>
	public int SlowShift
	{
		get => _slowShift.Value;
		set => _slowShift.Value = value;
	}

	/// <summary>
	/// Determines whether the strategy closes opposite positions on a new signal.
	/// </summary>
	public bool CloseOnSignal
	{
		get => _closeOnSignal.Value;
		set => _closeOnSignal.Value = value;
	}

	/// <summary>
	/// Inverts the direction of generated signals.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Candle price used for moving average calculations.
	/// </summary>
	public CandlePrice AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// Candle type requested from the data subscription.
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

		_fastHistory.Clear();
		_slowHistory.Clear();
		_fastMa = null;
		_slowMa = null;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		var priceStep = Security?.PriceStep ?? 1m;
		if (priceStep <= 0m)
			priceStep = 1m;

		var decimals = Security?.Decimals ?? 0;
		var pipMultiplier = decimals is 5 or 3 ? 10m : 1m;
		_pipSize = priceStep * pipMultiplier;

		_fastMa = new SimpleMovingAverage { Length = FastPeriod };
		_slowMa = new SimpleMovingAverage { Length = SlowPeriod };

		_fastHistory.Clear();
		_slowHistory.Clear();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			if (_fastMa != null)
				DrawIndicator(area, _fastMa);
			if (_slowMa != null)
				DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}

		StartProtection(
			takeProfit: TakeProfitPips > 0m ? new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute) : null,
			stopLoss: StopLossPips > 0m ? new Unit(StopLossPips * _pipSize, UnitTypes.Absolute) : null,
			trailingStop: UseTrailingStop && TrailingStopPips > 0m ? new Unit(TrailingStopPips * _pipSize, UnitTypes.Absolute) : null,
			trailingStep: UseTrailingStop && TrailingStopPips > 0m ? new Unit(TrailingStopPips * _pipSize, UnitTypes.Absolute) : null,
			useMarketOrders: true);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (_fastMa == null || _slowMa == null)
			return;

		if (Volume != OrderVolume)
			Volume = OrderVolume;

		var price = GetPrice(candle, AppliedPrice);
		var isFinal = candle.State == CandleStates.Finished;

		var fastValue = _fastMa.Process(price, candle.OpenTime, isFinal);
		var slowValue = _slowMa.Process(price, candle.OpenTime, isFinal);

		if (_fastMa.Length != FastPeriod)
			_fastMa.Length = FastPeriod;
		if (_slowMa.Length != SlowPeriod)
			_slowMa.Length = SlowPeriod;

		if (!isFinal)
			return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed)
			return;

		var fast = fastValue.ToDecimal();
		var slow = slowValue.ToDecimal();

		_fastHistory.Add(fast);
		_slowHistory.Add(slow);

		TrimHistory(_fastHistory);
		TrimHistory(_slowHistory);

		var currentFast = GetShiftedValue(_fastHistory, FastShift, 0);
		var currentSlow = GetShiftedValue(_slowHistory, SlowShift, 0);
		var previousFast = GetShiftedValue(_fastHistory, FastShift, 2);
		var previousSlow = GetShiftedValue(_slowHistory, SlowShift, 2);

		if (currentFast is null || currentSlow is null || previousFast is null || previousSlow is null)
			return;

		var signal = 0;
		if (currentFast > currentSlow && previousFast < previousSlow)
			signal = 1;
		else if (currentFast < currentSlow && previousFast > previousSlow)
			signal = -1;

		if (signal == 0)
			return;

		if (ReverseSignals)
			signal = -signal;

		if (CloseOnSignal)
		{
			if (signal > 0 && Position < 0)
			{
				ClosePosition(); // Exit short positions before reversing direction.
				return;
			}

			if (signal < 0 && Position > 0)
			{
				ClosePosition(); // Exit long positions before reversing direction.
				return;
			}
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (signal > 0)
		{
			if (Position <= 0)
				BuyMarket(Volume); // Enter long when fast MA crosses above the slow MA.
		}
		else if (signal < 0)
		{
			if (Position >= 0)
				SellMarket(Volume); // Enter short when fast MA crosses below the slow MA.
		}
	}

	private static decimal GetPrice(ICandleMessage candle, CandlePrice priceType)
	{
		return priceType switch
		{
			CandlePrice.Open => candle.OpenPrice,
			CandlePrice.High => candle.HighPrice,
			CandlePrice.Low => candle.LowPrice,
			CandlePrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			CandlePrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			CandlePrice.Weighted => (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m,
			_ => candle.ClosePrice,
		};
	}

	private void TrimHistory(List<decimal> history)
	{
		var capacity = GetHistoryCapacity();
		if (history.Count <= capacity)
			return;

		history.RemoveRange(0, history.Count - capacity);
	}

	private int GetHistoryCapacity()
	{
		var maxShift = Math.Max(FastShift, SlowShift);
		return Math.Max(maxShift + 5, 10);
	}

	private static decimal? GetShiftedValue(List<decimal> history, int maShift, int barShift)
	{
		var index = history.Count - 1 - (maShift + barShift);
		if (index < 0 || index >= history.Count)
			return null;

		return history[index];
	}
}

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// XOSignal based breakout strategy with re-entry logic.
/// </summary>
public class XoSignalReopenStrategy : Strategy
{
	/// <summary>
	/// Price source applied to the XO calculation.
	/// </summary>
	public enum AppliedPriceType
	{
		Close = 1,
		Open,
		High,
		Low,
		Median,
		Typical,
		Weighted,
		Simple,
		Quarter,
		TrendFollow0,
		TrendFollow1,
		Demark
	}

	private const int AtrPeriod = 13;

	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<int> _takeProfitTicks;
	private readonly StrategyParam<int> _priceStepTicks;
	private readonly StrategyParam<int> _maxPyramidingPositions;
	private readonly StrategyParam<bool> _enableBuyEntries;
	private readonly StrategyParam<bool> _enableSellEntries;
	private readonly StrategyParam<bool> _enableBuyExits;
	private readonly StrategyParam<bool> _enableSellExits;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _range;
	private readonly StrategyParam<AppliedPriceType> _appliedPrice;
	private readonly StrategyParam<int> _signalBar;

	private readonly Queue<SignalInfo> _signalQueue = new();

	private decimal _hi;
	private decimal _lo;
	private int _kr;
	private int _no;
	private int _trend;
	private bool _initialized;
	private DateTimeOffset? _lastBuySignalTime;
	private DateTimeOffset? _lastSellSignalTime;
	private DateTimeOffset? _lastExecutedBuySignalTime;
	private DateTimeOffset? _lastExecutedSellSignalTime;
	private int _longOrderCount;
	private int _shortOrderCount;
	private decimal _lastLongEntryPrice;
	private decimal _lastShortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;

	/// <summary>
	/// Trade volume for each entry.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Stop loss distance in ticks (0 disables it).
	/// </summary>
	public int StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}

	/// <summary>
	/// Take profit distance in ticks (0 disables it).
	/// </summary>
	public int TakeProfitTicks
	{
		get => _takeProfitTicks.Value;
		set => _takeProfitTicks.Value = value;
	}

	/// <summary>
	/// Additional entry trigger distance in ticks for re-entry.
	/// </summary>
	public int PriceStepTicks
	{
		get => _priceStepTicks.Value;
		set => _priceStepTicks.Value = value;
	}

	/// <summary>
	/// Maximum number of layered positions including the first one.
	/// </summary>
	public int MaxPyramidingPositions
	{
		get => _maxPyramidingPositions.Value;
		set => _maxPyramidingPositions.Value = value;
	}

	/// <summary>
	/// Enable opening long positions on signals.
	/// </summary>
	public bool EnableBuyEntries
	{
		get => _enableBuyEntries.Value;
		set => _enableBuyEntries.Value = value;
	}

	/// <summary>
	/// Enable opening short positions on signals.
	/// </summary>
	public bool EnableSellEntries
	{
		get => _enableSellEntries.Value;
		set => _enableSellEntries.Value = value;
	}

	/// <summary>
	/// Enable closing long positions on opposite signals.
	/// </summary>
	public bool EnableBuyExits
	{
		get => _enableBuyExits.Value;
		set => _enableBuyExits.Value = value;
	}

	/// <summary>
	/// Enable closing short positions on opposite signals.
	/// </summary>
	public bool EnableSellExits
	{
		get => _enableSellExits.Value;
		set => _enableSellExits.Value = value;
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
	/// XO box range in ticks.
	/// </summary>
	public int Range
	{
		get => _range.Value;
		set => _range.Value = value;
	}

	/// <summary>
	/// Applied price mode for XO calculations.
	/// </summary>
	public AppliedPriceType AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// Number of bars to delay signals.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="XoSignalReopenStrategy"/> class.
	/// </summary>
	public XoSignalReopenStrategy()
	{
		_volume = Param(nameof(Volume), 1m)
			.SetDisplay("Volume", "Order volume", "Trading")
			.SetGreaterThanZero();

		_stopLossTicks = Param(nameof(StopLossTicks), 1000)
			.SetDisplay("Stop Loss", "Stop loss in ticks", "Risk")
			.SetGreaterOrEqualZero();

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 2000)
			.SetDisplay("Take Profit", "Take profit in ticks", "Risk")
			.SetGreaterOrEqualZero();

		_priceStepTicks = Param(nameof(PriceStepTicks), 300)
			.SetDisplay("Re-entry Step", "Ticks to add position", "Trading")
			.SetGreaterOrEqualZero();

		_maxPyramidingPositions = Param(nameof(MaxPyramidingPositions), 10)
			.SetDisplay("Max Layers", "Maximum layered entries", "Trading")
			.SetGreaterThanZero();

		_enableBuyEntries = Param(nameof(EnableBuyEntries), true)
			.SetDisplay("Enable Long", "Allow long entries", "Permissions");

		_enableSellEntries = Param(nameof(EnableSellEntries), true)
			.SetDisplay("Enable Short", "Allow short entries", "Permissions");

		_enableBuyExits = Param(nameof(EnableBuyExits), true)
			.SetDisplay("Close Long", "Close long on short signal", "Permissions");

		_enableSellExits = Param(nameof(EnableSellExits), true)
			.SetDisplay("Close Short", "Close short on long signal", "Permissions");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Working timeframe", "General");

		_range = Param(nameof(Range), 10)
			.SetDisplay("Range", "XO box height in ticks", "Indicator")
			.SetGreaterThanZero();

		_appliedPrice = Param(nameof(AppliedPrice), AppliedPriceType.Close)
			.SetDisplay("Applied Price", "Price source", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetDisplay("Signal Shift", "Bars to delay signals", "Indicator")
			.SetGreaterOrEqualZero();
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_signalQueue.Clear();
		_hi = 0m;
		_lo = 0m;
		_kr = 0;
		_no = 0;
		_trend = 0;
		_initialized = false;
		_lastBuySignalTime = null;
		_lastSellSignalTime = null;
		_lastExecutedBuySignalTime = null;
		_lastExecutedSellSignalTime = null;
		_longOrderCount = 0;
		_shortOrderCount = 0;
		_lastLongEntryPrice = 0m;
		_lastShortEntryPrice = 0m;
		_longStopPrice = null;
		_longTakePrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var atr = new AverageTrueRange { Length = AtrPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(atr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (atr <= 0m)
			return;

		var step = Security?.PriceStep ?? 1m;
		var rangeStep = Math.Max(1, Range) * step;
		var price = GetAppliedPrice(candle);

		if (!_initialized)
		{
			_hi = price;
			_lo = price;
			_initialized = true;
		}

		if (price > _hi + rangeStep)
		{
			_hi = price;
			_lo = _hi - rangeStep;
			_kr++;
			_no = 0;
		}
		else if (price < _lo - rangeStep)
		{
			_lo = price;
			_hi = _lo + rangeStep;
			_no++;
			_kr = 0;
		}

		var trend = _trend;
		if (_kr > 0)
			trend = 1;
		if (_no > 0)
			trend = -1;

		var buySignal = _trend < 0 && trend > 0;
		var sellSignal = _trend > 0 && trend < 0;
		_trend = trend;

		var closeTime = candle.OpenTime + (TimeSpan)CandleType.Arg;
		var buyTime = buySignal ? closeTime : (_lastBuySignalTime ?? closeTime);
		var sellTime = sellSignal ? closeTime : (_lastSellSignalTime ?? closeTime);
		var buyLevel = candle.LowPrice - atr * 3m / 8m;
		var sellLevel = candle.HighPrice + atr * 3m / 8m;

		var info = new SignalInfo(
			buySignal,
			sellSignal,
			sellSignal,
			buySignal,
			buyTime,
			sellTime,
			buyLevel,
			sellLevel,
			candle.ClosePrice);

		_signalQueue.Enqueue(info);

		if (_signalQueue.Count <= SignalBar)
			return;

		var activeSignal = _signalQueue.Dequeue();

		HandleStops(candle);
		ApplySignal(activeSignal, candle);
		HandleReentries(candle);
	}

	private void ApplySignal(SignalInfo signal, ICandleMessage candle)
	{
		if (signal.BuyEntry || signal.SellExit)
			_lastBuySignalTime = signal.BuySignalTime;

		if (signal.SellEntry || signal.BuyExit)
			_lastSellSignalTime = signal.SellSignalTime;

		if (signal.BuyExit && EnableBuyExits && Position > 0)
		{
			SellMarket(Position);
			ResetLongState();
		}

		if (signal.SellExit && EnableSellExits && Position < 0)
		{
			BuyMarket(-Position);
			ResetShortState();
		}

		if (signal.BuyEntry && EnableBuyEntries)
		{
			if (_lastExecutedBuySignalTime != signal.BuySignalTime)
			{
				if (Position < 0)
				{
					BuyMarket(-Position);
					ResetShortState();
				}

				if (Position <= 0)
				{
					BuyMarket(Volume);
					_lastExecutedBuySignalTime = signal.BuySignalTime;
					_longOrderCount = 1;
					_shortOrderCount = 0;
					_lastLongEntryPrice = candle.ClosePrice;
					UpdateLongRiskLevels(candle.ClosePrice);
				}
			}
		}

		if (signal.SellEntry && EnableSellEntries)
		{
			if (_lastExecutedSellSignalTime != signal.SellSignalTime)
			{
				if (Position > 0)
				{
					SellMarket(Position);
					ResetLongState();
				}

				if (Position >= 0)
				{
					SellMarket(Volume);
					_lastExecutedSellSignalTime = signal.SellSignalTime;
					_shortOrderCount = 1;
					_longOrderCount = 0;
					_lastShortEntryPrice = candle.ClosePrice;
					UpdateShortRiskLevels(candle.ClosePrice);
				}
			}
		}
	}

	private void HandleStops(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_longStopPrice.HasValue && candle.LowPrice <= _longStopPrice.Value)
			{
				SellMarket(Position);
				ResetLongState();
			}
			else if (_longTakePrice.HasValue && candle.HighPrice >= _longTakePrice.Value)
			{
				SellMarket(Position);
				ResetLongState();
			}
		}
		else
		{
			_longStopPrice = null;
			_longTakePrice = null;
		}

		if (Position < 0)
		{
			if (_shortStopPrice.HasValue && candle.HighPrice >= _shortStopPrice.Value)
			{
				BuyMarket(-Position);
				ResetShortState();
			}
			else if (_shortTakePrice.HasValue && candle.LowPrice <= _shortTakePrice.Value)
			{
				BuyMarket(-Position);
				ResetShortState();
			}
		}
		else
		{
			_shortStopPrice = null;
			_shortTakePrice = null;
		}
	}

	private void HandleReentries(ICandleMessage candle)
	{
		var step = Security?.PriceStep ?? 1m;
		var distance = PriceStepTicks * step;

		if (distance <= 0m)
			return;

		if (EnableBuyEntries && Position > 0 && _longOrderCount > 0 && _longOrderCount < MaxPyramidingPositions)
		{
			if (candle.ClosePrice >= _lastLongEntryPrice + distance)
			{
				BuyMarket(Volume);
				_longOrderCount++;
				_lastLongEntryPrice = candle.ClosePrice;
				UpdateLongRiskLevels(candle.ClosePrice);
			}
		}

		if (EnableSellEntries && Position < 0 && _shortOrderCount > 0 && _shortOrderCount < MaxPyramidingPositions)
		{
			if (candle.ClosePrice <= _lastShortEntryPrice - distance)
			{
				SellMarket(Volume);
				_shortOrderCount++;
				_lastShortEntryPrice = candle.ClosePrice;
				UpdateShortRiskLevels(candle.ClosePrice);
			}
		}
	}

	private void UpdateLongRiskLevels(decimal entryPrice)
	{
		var step = Security?.PriceStep ?? 1m;
		_longStopPrice = StopLossTicks > 0 ? entryPrice - StopLossTicks * step : null;
		_longTakePrice = TakeProfitTicks > 0 ? entryPrice + TakeProfitTicks * step : null;
	}

	private void UpdateShortRiskLevels(decimal entryPrice)
	{
		var step = Security?.PriceStep ?? 1m;
		_shortStopPrice = StopLossTicks > 0 ? entryPrice + StopLossTicks * step : null;
		_shortTakePrice = TakeProfitTicks > 0 ? entryPrice - TakeProfitTicks * step : null;
	}

	private void ResetLongState()
	{
		_longOrderCount = 0;
		_lastLongEntryPrice = 0m;
		_longStopPrice = null;
		_longTakePrice = null;
	}

	private void ResetShortState()
	{
		_shortOrderCount = 0;
		_lastShortEntryPrice = 0m;
		_shortStopPrice = null;
		_shortTakePrice = null;
	}

	private decimal GetAppliedPrice(ICandleMessage candle)
	{
		return AppliedPrice switch
		{
			AppliedPriceType.Open => candle.OpenPrice,
			AppliedPriceType.High => candle.HighPrice,
			AppliedPriceType.Low => candle.LowPrice,
			AppliedPriceType.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceType.Typical => (candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 3m,
			AppliedPriceType.Weighted => (2m * candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPriceType.Simple => (candle.OpenPrice + candle.ClosePrice) / 2m,
			AppliedPriceType.Quarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPriceType.TrendFollow0 => candle.ClosePrice > candle.OpenPrice ? candle.HighPrice : candle.ClosePrice < candle.OpenPrice ? candle.LowPrice : candle.ClosePrice,
			AppliedPriceType.TrendFollow1 => candle.ClosePrice > candle.OpenPrice ? (candle.HighPrice + candle.ClosePrice) / 2m : candle.ClosePrice < candle.OpenPrice ? (candle.LowPrice + candle.ClosePrice) / 2m : candle.ClosePrice,
			AppliedPriceType.Demark => CalculateDemarkPrice(candle),
			_ => candle.ClosePrice,
		};
	}

	private decimal CalculateDemarkPrice(ICandleMessage candle)
	{
		var res = candle.HighPrice + candle.LowPrice + candle.ClosePrice;
		if (candle.ClosePrice < candle.OpenPrice)
			res = (res + candle.LowPrice) / 2m;
		else if (candle.ClosePrice > candle.OpenPrice)
			res = (res + candle.HighPrice) / 2m;
		else
			res = (res + candle.ClosePrice) / 2m;

		return ((res - candle.LowPrice) + (res - candle.HighPrice)) / 2m;
	}

	private readonly struct SignalInfo
	{
		public SignalInfo(bool buyEntry, bool sellEntry, bool buyExit, bool sellExit, DateTimeOffset buySignalTime, DateTimeOffset sellSignalTime, decimal buyLevel, decimal sellLevel, decimal closePrice)
		{
			BuyEntry = buyEntry;
			SellEntry = sellEntry;
			BuyExit = buyExit;
			SellExit = sellExit;
			BuySignalTime = buySignalTime;
			SellSignalTime = sellSignalTime;
			BuyLevel = buyLevel;
			SellLevel = sellLevel;
			ClosePrice = closePrice;
		}

		public bool BuyEntry { get; }
		public bool SellEntry { get; }
		public bool BuyExit { get; }
		public bool SellExit { get; }
		public DateTimeOffset BuySignalTime { get; }
		public DateTimeOffset SellSignalTime { get; }
		public decimal BuyLevel { get; }
		public decimal SellLevel { get; }
		public decimal ClosePrice { get; }
	}
}

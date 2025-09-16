using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the Exp_Ang_Zad_C_Tm_MMRec expert advisor that trades based on the Ang_Zad_C indicator with time filter and loss recovery money management.
/// </summary>
public class AngZadCTimeMMRecoveryStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _ki;
	private readonly StrategyParam<AppliedPrice> _priceMode;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<TimeSpan> _tradeStart;
	private readonly StrategyParam<TimeSpan> _tradeEnd;
	private readonly StrategyParam<bool> _enableLongEntry;
	private readonly StrategyParam<bool> _enableShortEntry;
	private readonly StrategyParam<bool> _enableLongExit;
	private readonly StrategyParam<bool> _enableShortExit;
	private readonly StrategyParam<int> _buyLossTrigger;
	private readonly StrategyParam<int> _sellLossTrigger;
	private readonly StrategyParam<decimal> _smallVolume;
	private readonly StrategyParam<decimal> _normalVolume;
	private readonly StrategyParam<int> _stopLossSteps;
	private readonly StrategyParam<int> _takeProfitSteps;

	private bool _hasIndicatorState;
	private decimal _upperLine;
	private decimal _lowerLine;
	private decimal _previousPrice;
	private readonly List<(decimal Up, decimal Down)> _history = new();

	private bool _pendingLongEntry;
	private bool _pendingShortEntry;

	private int _lastDirection;
	private int _buyLossCount;
	private int _sellLossCount;
	private decimal _lastRealizedPnL;

	private Order? _stopOrder;
	private Order? _takeOrder;

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Smoothing factor of the Ang_Zad_C indicator.
	/// </summary>
	public decimal Ki { get => _ki.Value; set => _ki.Value = value; }

	/// <summary>
	/// Price type used by the indicator.
	/// </summary>
	public AppliedPrice PriceMode { get => _priceMode.Value; set => _priceMode.Value = value; }

	/// <summary>
	/// Bar shift used for signals.
	/// </summary>
	public int SignalBar { get => _signalBar.Value; set => _signalBar.Value = value; }

	/// <summary>
	/// Enables time filter for trading sessions.
	/// </summary>
	public bool UseTimeFilter { get => _useTimeFilter.Value; set => _useTimeFilter.Value = value; }

	/// <summary>
	/// Start time of the trading session.
	/// </summary>
	public TimeSpan TradeStart { get => _tradeStart.Value; set => _tradeStart.Value = value; }

	/// <summary>
	/// End time of the trading session.
	/// </summary>
	public TimeSpan TradeEnd { get => _tradeEnd.Value; set => _tradeEnd.Value = value; }

	/// <summary>
	/// Allows long entries.
	/// </summary>
	public bool EnableLongEntry { get => _enableLongEntry.Value; set => _enableLongEntry.Value = value; }

	/// <summary>
	/// Allows short entries.
	/// </summary>
	public bool EnableShortEntry { get => _enableShortEntry.Value; set => _enableShortEntry.Value = value; }

	/// <summary>
	/// Allows long exits.
	/// </summary>
	public bool EnableLongExit { get => _enableLongExit.Value; set => _enableLongExit.Value = value; }

	/// <summary>
	/// Allows short exits.
	/// </summary>
	public bool EnableShortExit { get => _enableShortExit.Value; set => _enableShortExit.Value = value; }

	/// <summary>
	/// Number of losing long trades before the reduced volume is used.
	/// </summary>
	public int BuyLossTrigger { get => _buyLossTrigger.Value; set => _buyLossTrigger.Value = value; }

	/// <summary>
	/// Number of losing short trades before the reduced volume is used.
	/// </summary>
	public int SellLossTrigger { get => _sellLossTrigger.Value; set => _sellLossTrigger.Value = value; }

	/// <summary>
	/// Volume used after a loss streak.
	/// </summary>
	public decimal SmallVolume { get => _smallVolume.Value; set => _smallVolume.Value = value; }

	/// <summary>
	/// Default trading volume.
	/// </summary>
	public decimal NormalVolume { get => _normalVolume.Value; set => _normalVolume.Value = value; }

	/// <summary>
	/// Stop loss distance expressed in price steps.
	/// </summary>
	public int StopLossSteps { get => _stopLossSteps.Value; set => _stopLossSteps.Value = value; }

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public int TakeProfitSteps { get => _takeProfitSteps.Value; set => _takeProfitSteps.Value = value; }

	/// <summary>
	/// Initializes <see cref="AngZadCTimeMMRecoveryStrategy"/>.
	/// </summary>
	public AngZadCTimeMMRecoveryStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(12).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used by the indicator.", "General");

		_ki = Param(nameof(Ki), 4.000001m)
			.SetDisplay("Ki", "Smoothing coefficient of the Ang_Zad_C indicator.", "Indicator")
			.SetGreaterThanZero();

		_priceMode = Param(nameof(PriceMode), AppliedPrice.Close)
			.SetDisplay("Applied Price", "Source price used by the indicator.", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetDisplay("Signal Bar", "Bar shift applied when evaluating signals.", "Indicator")
			.SetCanOptimize(false);

		_useTimeFilter = Param(nameof(UseTimeFilter), true)
			.SetDisplay("Use Time Filter", "Enable session based trading filter.", "Trading Hours");

		_tradeStart = Param(nameof(TradeStart), TimeSpan.Zero)
			.SetDisplay("Trade Start", "Session start time.", "Trading Hours");

		_tradeEnd = Param(nameof(TradeEnd), new TimeSpan(23, 59, 0))
			.SetDisplay("Trade End", "Session end time.", "Trading Hours");

		_enableLongEntry = Param(nameof(EnableLongEntry), true)
			.SetDisplay("Enable Long Entry", "Allow opening long positions.", "Signals");

		_enableShortEntry = Param(nameof(EnableShortEntry), true)
			.SetDisplay("Enable Short Entry", "Allow opening short positions.", "Signals");

		_enableLongExit = Param(nameof(EnableLongExit), true)
			.SetDisplay("Enable Long Exit", "Allow closing long positions on signals.", "Signals");

		_enableShortExit = Param(nameof(EnableShortExit), true)
			.SetDisplay("Enable Short Exit", "Allow closing short positions on signals.", "Signals");

		_buyLossTrigger = Param(nameof(BuyLossTrigger), 2)
			.SetDisplay("Buy Loss Trigger", "Losing long trades before volume reduction.", "Money Management")
			.SetGreaterOrEqual(0);

		_sellLossTrigger = Param(nameof(SellLossTrigger), 2)
			.SetDisplay("Sell Loss Trigger", "Losing short trades before volume reduction.", "Money Management")
			.SetGreaterOrEqual(0);

		_smallVolume = Param(nameof(SmallVolume), 0.01m)
			.SetDisplay("Small Volume", "Volume after reaching the loss trigger.", "Money Management")
			.SetGreaterThanZero();

		_normalVolume = Param(nameof(NormalVolume), 0.1m)
			.SetDisplay("Normal Volume", "Base trading volume.", "Money Management")
			.SetGreaterThanZero();

		_stopLossSteps = Param(nameof(StopLossSteps), 1000)
			.SetDisplay("Stop Loss Steps", "Stop loss distance in price steps.", "Risk")
			.SetGreaterOrEqual(0);

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 2000)
			.SetDisplay("Take Profit Steps", "Take profit distance in price steps.", "Risk")
			.SetGreaterOrEqual(0);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = GetAppliedPrice(candle);
		var (upper, lower) = UpdateIndicator(price);

		_history.Add((upper, lower));

		var minRequired = Math.Max(SignalBar + 2, 2);
		if (_history.Count < minRequired)
			return;

		var maxHistory = Math.Max(SignalBar + 2, 8);
		if (_history.Count > maxHistory)
			_history.RemoveRange(0, _history.Count - maxHistory);

		var currentIndex = _history.Count - 1 - Math.Max(SignalBar, 0);
		if (currentIndex <= 0 || currentIndex >= _history.Count)
			return;

		var previousIndex = currentIndex - 1;
		if (previousIndex < 0)
			return;

		var (upCurrent, dnCurrent) = _history[currentIndex];
		var (upPrev, dnPrev) = _history[previousIndex];

		var buySignal = false;
		var sellSignal = false;
		var closeLong = false;
		var closeShort = false;

		if (upPrev > dnPrev)
		{
			if (EnableLongEntry && upCurrent <= dnCurrent)
				buySignal = true;

			if (EnableShortExit)
				closeShort = true;
		}

		if (upPrev < dnPrev)
		{
			if (EnableShortEntry && upCurrent >= dnCurrent)
				sellSignal = true;

			if (EnableLongExit)
				closeLong = true;
		}

		var inTradeWindow = !UseTimeFilter || IsWithinTradeWindow(candle.CloseTime);

		if (UseTimeFilter && !inTradeWindow && Position != 0)
		{
			ClosePosition();
			_pendingLongEntry = false;
			_pendingShortEntry = false;
			return;
		}

		if (!inTradeWindow)
		{
			_pendingLongEntry = false;
			_pendingShortEntry = false;
			return;
		}

		if (closeLong && Position > 0)
			ClosePosition();

		if (closeShort && Position < 0)
			ClosePosition();

		if (buySignal)
		{
			_pendingLongEntry = true;
			_pendingShortEntry = false;
		}

		if (sellSignal)
		{
			_pendingShortEntry = true;
			_pendingLongEntry = false;
		}

		if (_pendingLongEntry && Position <= 0)
		{
			var volume = GetTradeVolume(true);
			if (volume > 0m)
			{
				BuyMarket(volume);
				_lastDirection = 1;
				RegisterProtection(true, candle.ClosePrice, volume);
			}

			_pendingLongEntry = false;
		}

		if (_pendingShortEntry && Position >= 0)
		{
			var volume = GetTradeVolume(false);
			if (volume > 0m)
			{
				SellMarket(volume);
				_lastDirection = -1;
				RegisterProtection(false, candle.ClosePrice, volume);
			}

			_pendingShortEntry = false;
		}
	}

	private (decimal Up, decimal Down) UpdateIndicator(decimal price)
	{
		if (!_hasIndicatorState)
		{
			_upperLine = price;
			_lowerLine = price;
			_previousPrice = price;
			_hasIndicatorState = true;
		}

		var upper = _upperLine;
		var lower = _lowerLine;

		var prevPrice = _previousPrice;
		var ki = Ki;

		if (price > upper && price > prevPrice)
			upper = _upperLine + (price - _upperLine) / ki;

		if (price < upper && price < prevPrice)
			upper = _upperLine + (price - _upperLine) / ki;

		if (price > lower && price < prevPrice)
			lower = _lowerLine + (price - _lowerLine) / ki;

		if (price < lower && price > prevPrice)
			lower = _lowerLine + (price - _lowerLine) / ki;

		_previousPrice = price;
		_upperLine = upper;
		_lowerLine = lower;

		return (upper, lower);
	}

	private decimal GetAppliedPrice(ICandleMessage candle)
	{
		return PriceMode switch
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
		var result = candle.HighPrice + candle.LowPrice + candle.ClosePrice;

		if (candle.ClosePrice < candle.OpenPrice)
			result = (result + candle.LowPrice) / 2m;
		else if (candle.ClosePrice > candle.OpenPrice)
			result = (result + candle.HighPrice) / 2m;
		else
			result = (result + candle.ClosePrice) / 2m;

		return ((result - candle.LowPrice) + (result - candle.HighPrice)) / 2m;
	}

	private bool IsWithinTradeWindow(DateTimeOffset time)
	{
		var current = time.TimeOfDay;
		var start = TradeStart;
		var end = TradeEnd;

		if (start == end)
			return false;

		if (start < end)
			return current >= start && current < end;

		return current >= start || current < end;
	}

	private decimal GetTradeVolume(bool isLong)
	{
		var trigger = isLong ? BuyLossTrigger : SellLossTrigger;
		if (trigger <= 0)
			return NormalVolume;

		var losses = isLong ? _buyLossCount : _sellLossCount;
		return losses >= trigger ? SmallVolume : NormalVolume;
	}

	private void RegisterProtection(bool isLong, decimal entryPrice, decimal volume)
	{
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);

		if (_takeOrder != null && _takeOrder.State == OrderStates.Active)
			CancelOrder(_takeOrder);

		_stopOrder = null;
		_takeOrder = null;

		var step = Security?.PriceStep ?? 1m;

		if (StopLossSteps > 0)
		{
			var stopPrice = isLong ? entryPrice - step * StopLossSteps : entryPrice + step * StopLossSteps;
			_stopOrder = isLong ? SellStop(volume, stopPrice) : BuyStop(volume, stopPrice);
		}

		if (TakeProfitSteps > 0)
		{
			var takePrice = isLong ? entryPrice + step * TakeProfitSteps : entryPrice - step * TakeProfitSteps;
			_takeOrder = isLong ? SellLimit(volume, takePrice) : BuyLimit(volume, takePrice);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position > 0)
			_lastDirection = 1;
		else if (Position < 0)
			_lastDirection = -1;

		if (Position != 0)
			return;

		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);

		if (_takeOrder != null && _takeOrder.State == OrderStates.Active)
			CancelOrder(_takeOrder);

		_stopOrder = null;
		_takeOrder = null;

		var realized = PnL - _lastRealizedPnL;

		if (_lastDirection > 0)
			_buyLossCount = realized < 0m ? _buyLossCount + 1 : 0;
		else if (_lastDirection < 0)
			_sellLossCount = realized < 0m ? _sellLossCount + 1 : 0;

		if (_lastDirection != 0)
		{
			_lastRealizedPnL = PnL;
			_lastDirection = 0;
		}
	}
}

/// <summary>
/// Price selection for the Ang_Zad_C indicator.
/// </summary>
public enum AppliedPrice
{
	/// <summary>
	/// Closing price.
	/// </summary>
	Close = 1,

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
	/// Weighted price (2 * close + high + low) / 4.
	/// </summary>
	Weighted,

	/// <summary>
	/// Simple average of open and close.
	/// </summary>
	Simple,

	/// <summary>
	/// Quarter price (open + high + low + close) / 4.
	/// </summary>
	Quarter,

	/// <summary>
	/// Trend following price variant 0.
	/// </summary>
	TrendFollow0,

	/// <summary>
	/// Trend following price variant 1.
	/// </summary>
	TrendFollow1,

	/// <summary>
	/// DeMark price calculation.
	/// </summary>
	Demark
}

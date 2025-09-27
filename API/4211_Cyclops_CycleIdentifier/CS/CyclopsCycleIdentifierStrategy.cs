using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader expert advisor Cyclops_v_1_2 that relies on the CycleIdentifier indicator.
/// Detects major and minor cycle reversals on a smoothed price series and optionally confirms them with a zero-lag filter.
/// Supports optional momentum confirmation, time based trading restrictions, and trade management with break-even and trailing stops.
/// </summary>
public class CyclopsCycleIdentifierStrategy : Strategy
{
	private readonly StrategyParam<int> _priceActionFilter;
	private readonly StrategyParam<int> _averageRangeLength;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _majorCycleStrength;
	private readonly StrategyParam<bool> _useCycleFilter;
	private readonly StrategyParam<CycleFilterMode> _cycleFilterMode;
	private readonly StrategyParam<int> _filterStrengthSma;
	private readonly StrategyParam<int> _filterStrengthRsi;
	private readonly StrategyParam<bool> _useMomentumFilter;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumTriggerLong;
	private readonly StrategyParam<decimal> _momentumTriggerShort;
	private readonly StrategyParam<bool> _useExitSignal;
	private readonly StrategyParam<bool> _useTimeRestriction;
	private readonly StrategyParam<int> _dayEnd;
	private readonly StrategyParam<int> _hourEnd;
	private readonly StrategyParam<int> _breakEvenTrigger;
	private readonly StrategyParam<int> _trailingStopTrigger;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _minBarsBetweenSignals;

	private readonly CycleIdentifierState _cycleState = new();

	private SmoothedMovingAverage _cycleSmma = null!;
	private AverageTrueRange _atr = null!;
	private Momentum _momentum = null!;

	private decimal _pipSize;

	private decimal _previousZeroLag;
	private decimal _zeroLagPrev1;
	private decimal _zeroLagPrev2;

	private decimal? _previousCycleForRsi;
	private decimal _cycleRsiAvgGain;
	private decimal _cycleRsiAvgLoss;
	private int _cycleRsiSamples;

	private decimal? _longEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _longTakeProfitPrice;
	private bool _longBreakEvenActive;

	private decimal? _shortEntryPrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakeProfitPrice;
	private bool _shortBreakEvenActive;

	private decimal _signedPosition;

	private DateTimeOffset? _lastTradeBarTime;
	private DateTimeOffset _currentBarTime;

	/// <summary>
	/// Initializes a new instance of the <see cref="CyclopsCycleIdentifierStrategy"/> class.
	/// </summary>
	public CyclopsCycleIdentifierStrategy()
	{

		_priceActionFilter = Param(nameof(PriceActionFilter), 1)
		.SetRange(1, 100)
		.SetDisplay("Price Filter", "Smoothed moving average length applied to close price", "Indicator");

		_averageRangeLength = Param(nameof(AverageRangeLength), 250)
		.SetRange(1, 1000)
		.SetDisplay("Average Range Length", "Number of candles used to compute the average range", "Indicator");

		_length = Param(nameof(Length), 3)
		.SetRange(1, 50)
		.SetDisplay("Range Multiplier", "Multiplier applied to the average range threshold", "Indicator");

		_majorCycleStrength = Param(nameof(MajorCycleStrength), 4)
		.SetRange(1, 50)
		.SetDisplay("Major Strength", "Multiplier that separates major and minor cycle swings", "Indicator");

		_useCycleFilter = Param(nameof(UseCycleFilter), false)
		.SetDisplay("Use Cycle Filter", "Enable zero-lag slope confirmation", "Indicator");

		_cycleFilterMode = Param(nameof(CycleFilterMode), CycleFilterMode.Sma)
		.SetDisplay("Filter Source", "Source used by the zero-lag filter (price or RSI)", "Indicator");

		_filterStrengthSma = Param(nameof(FilterStrengthSma), 12)
		.SetRange(1, 200)
		.SetDisplay("Zero-Lag Length", "Length of the zero-lag filter when using price", "Indicator");

		_filterStrengthRsi = Param(nameof(FilterStrengthRsi), 21)
		.SetRange(2, 200)
		.SetDisplay("RSI Filter Length", "Length applied when the zero-lag filter uses RSI", "Indicator");

		_useMomentumFilter = Param(nameof(UseMomentumFilter), true)
		.SetDisplay("Momentum Filter", "Require momentum confirmation before entries", "Filters");

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
		.SetRange(1, 200)
		.SetDisplay("Momentum Period", "Length of the momentum indicator", "Filters");

		_momentumTriggerLong = Param(nameof(MomentumTriggerLong), 100m)
		.SetDisplay("Momentum Long", "Minimum momentum required for long entries", "Filters");

		_momentumTriggerShort = Param(nameof(MomentumTriggerShort), 90m)
		.SetDisplay("Momentum Short", "Maximum momentum allowed for short entries", "Filters");

		_useExitSignal = Param(nameof(UseExitSignal), true)
		.SetDisplay("Use Exit Signal", "Allow minor cycle reversals to close profitable trades", "Risk");

		_useTimeRestriction = Param(nameof(UseTimeRestriction), false)
		.SetDisplay("Time Restriction", "Limit entries to the configured week day and hour", "Session");

		_dayEnd = Param(nameof(DayEnd), 5)
		.SetRange(0, 6)
		.SetDisplay("Last Trading Day", "Trading allowed until this day of week (0=Sunday)", "Session");

		_hourEnd = Param(nameof(HourEnd), 12)
		.SetRange(0, 23)
		.SetDisplay("Last Trading Hour", "Trading allowed until this hour on the final day", "Session");

		_breakEvenTrigger = Param(nameof(BreakEvenTrigger), 0)
		.SetRange(0, 10000)
		.SetDisplay("Break-Even Trigger", "Distance in pips to activate the break-even stop", "Risk");

		_trailingStopTrigger = Param(nameof(TrailingStopTrigger), 0)
		.SetRange(0, 10000)
		.SetDisplay("Trailing Trigger", "Distance in pips before the trailing stop is applied", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 0)
		.SetRange(0, 10000)
		.SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 0m)
		.SetRange(0m, 10000m)
		.SetCanOptimize(true)
		.SetDisplay("Take Profit", "Fixed take-profit distance in pips", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 0m)
		.SetRange(0m, 10000m)
		.SetCanOptimize(true)
		.SetDisplay("Stop Loss", "Fixed stop-loss distance in pips", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe that feeds the strategy", "General");

		_minBarsBetweenSignals = Param(nameof(MinBarsBetweenSignals), 1)
		.SetRange(0, 50)
		.SetDisplay("Min Bars Between Signals", "Minimum spacing between generated signals", "Indicator");
	}


	/// <summary>
	/// Length of the smoothed moving average applied to closing prices.
	/// </summary>
	public int PriceActionFilter
	{
		get => _priceActionFilter.Value;
		set => _priceActionFilter.Value = value;
	}

	/// <summary>
	/// Number of candles used to compute the average price range.
	/// </summary>
	public int AverageRangeLength
	{
		get => _averageRangeLength.Value;
		set => _averageRangeLength.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the average range that defines minor reversals.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Multiplier that separates major from minor swings.
	/// </summary>
	public int MajorCycleStrength
	{
		get => _majorCycleStrength.Value;
		set => _majorCycleStrength.Value = value;
	}

	/// <summary>
	/// Enables slope confirmation with the zero-lag filter.
	/// </summary>
	public bool UseCycleFilter
	{
		get => _useCycleFilter.Value;
		set => _useCycleFilter.Value = value;
	}

	/// <summary>
	/// Selects the data source for the zero-lag filter.
	/// </summary>
	public CycleFilterMode CycleFilterMode
	{
		get => _cycleFilterMode.Value;
		set => _cycleFilterMode.Value = value;
	}

	/// <summary>
	/// Length of the zero-lag filter when price is used as source.
	/// </summary>
	public int FilterStrengthSma
	{
		get => _filterStrengthSma.Value;
		set => _filterStrengthSma.Value = value;
	}

	/// <summary>
	/// Length parameter applied when the zero-lag filter receives RSI values.
	/// </summary>
	public int FilterStrengthRsi
	{
		get => _filterStrengthRsi.Value;
		set => _filterStrengthRsi.Value = value;
	}

	/// <summary>
	/// Enables the momentum confirmation filter.
	/// </summary>
	public bool UseMomentumFilter
	{
		get => _useMomentumFilter.Value;
		set => _useMomentumFilter.Value = value;
	}

	/// <summary>
	/// Length of the momentum indicator used for confirmation.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimum momentum value required to accept long entries.
	/// </summary>
	public decimal MomentumTriggerLong
	{
		get => _momentumTriggerLong.Value;
		set => _momentumTriggerLong.Value = value;
	}

	/// <summary>
	/// Maximum momentum value allowed when entering short trades.
	/// </summary>
	public decimal MomentumTriggerShort
	{
		get => _momentumTriggerShort.Value;
		set => _momentumTriggerShort.Value = value;
	}

	/// <summary>
	/// Enables closing trades with minor cycle exit signals.
	/// </summary>
	public bool UseExitSignal
	{
		get => _useExitSignal.Value;
		set => _useExitSignal.Value = value;
	}

	/// <summary>
	/// Enables the intraday trading window restriction.
	/// </summary>
	public bool UseTimeRestriction
	{
		get => _useTimeRestriction.Value;
		set => _useTimeRestriction.Value = value;
	}

	/// <summary>
	/// Last day of week (0=Sunday) when trading is permitted.
	/// </summary>
	public int DayEnd
	{
		get => _dayEnd.Value;
		set => _dayEnd.Value = value;
	}

	/// <summary>
	/// Last hour on the final trading day when new entries are allowed.
	/// </summary>
	public int HourEnd
	{
		get => _hourEnd.Value;
		set => _hourEnd.Value = value;
	}

	/// <summary>
	/// Distance in pips that activates the break-even stop.
	/// </summary>
	public int BreakEvenTrigger
	{
		get => _breakEvenTrigger.Value;
		set => _breakEvenTrigger.Value = value;
	}

	/// <summary>
	/// Distance in pips before the trailing stop starts following price.
	/// </summary>
	public int TrailingStopTrigger
	{
		get => _trailingStopTrigger.Value;
		set => _trailingStopTrigger.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips applied to new positions.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips applied to new positions.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Minimum number of bars required between consecutive signals.
	/// </summary>
	public int MinBarsBetweenSignals
	{
		get => _minBarsBetweenSignals.Value;
		set => _minBarsBetweenSignals.Value = value;
	}

	/// <summary>
	/// Candle type that feeds the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_cycleState.Reset();
		_cycleSmma = null!;
		_atr = null!;
		_momentum = null!;
		_previousZeroLag = 0m;
		_zeroLagPrev1 = 0m;
		_zeroLagPrev2 = 0m;
		_previousCycleForRsi = null;
		_cycleRsiAvgGain = 0m;
		_cycleRsiAvgLoss = 0m;
		_cycleRsiSamples = 0;
		ResetLongState();
		ResetShortState();
		_signedPosition = 0m;
		_lastTradeBarTime = null;
		_currentBarTime = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = GetPipSize();

		_cycleSmma = new SmoothedMovingAverage
		{
			Length = Math.Max(PriceActionFilter, 1)
		};

		_atr = new AverageTrueRange
		{
			Length = AverageRangeLength
		};

		_momentum = new Momentum
		{
			Length = Math.Max(MomentumPeriod, 1)
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_cycleSmma, _atr, _momentum, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _cycleSmma, "Cycle SMMA");
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cyclePrice, decimal atrValue, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_currentBarTime = candle.CloseTime;

		if (!_cycleSmma.IsFormed || !_atr.IsFormed)
		return;

		var averageRange = Math.Abs(atrValue) * Math.Max(Length, 1);
		if (averageRange <= 0m)
		return;

		var majorThreshold = averageRange * Math.Max(MajorCycleStrength, 1);
		var minorThreshold = averageRange;

		var zeroLagValue = ComputeZeroLagValue(cyclePrice);
		var allowLongSignals = !UseCycleFilter || zeroLagValue >= _previousZeroLag;
		var allowShortSignals = !UseCycleFilter || zeroLagValue <= _previousZeroLag;
		_previousZeroLag = zeroLagValue;

		var signals = _cycleState.Update(cyclePrice, minorThreshold, majorThreshold, allowLongSignals, allowShortSignals, MinBarsBetweenSignals);

		if (ManageExistingPositions(candle, signals))
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!IsWithinTradingWindow(candle.CloseTime))
		return;

		if (!IsOrderAllowed(candle))
		return;

		if (signals.MajorBuy && Position <= 0m && PassesMomentumFilter(Sides.Buy, momentumValue))
		{
			var volume = Volume + Math.Max(0m, -Position);
			if (volume > 0m)
			{
				BuyMarket(volume);
			}
		}
		else if (signals.MajorSell && Position >= 0m && PassesMomentumFilter(Sides.Sell, momentumValue))
		{
			var volume = Volume + Math.Max(0m, Position);
			if (volume > 0m)
			{
				SellMarket(volume);
			}
		}
	}

	private bool ManageExistingPositions(ICandleMessage candle, CycleSignals signals)
	{
		var price = candle.ClosePrice;

		if (Position > 0m)
		{
			var volume = Position;

			if (_longTakeProfitPrice is decimal takeProfit && price >= takeProfit)
			{
				SellMarket(volume);
				return true;
			}

			if (_longStopPrice is decimal stopPrice && price <= stopPrice)
			{
				SellMarket(volume);
				return true;
			}

			if (BreakEvenTrigger > 0 && !_longBreakEvenActive && _longEntryPrice is decimal entry)
			{
				var triggerPrice = entry + BreakEvenTrigger * _pipSize;
				if (price >= triggerPrice)
				{
					_longBreakEvenActive = true;
					_longStopPrice = entry + _pipSize;
				}
			}

			if (TrailingStopTrigger > 0 && TrailingStopPips > 0 && _longEntryPrice is decimal longEntry)
			{
				var triggerPrice = longEntry + TrailingStopTrigger * _pipSize;
				if (price >= triggerPrice)
				{
					var newStop = price - TrailingStopPips * _pipSize;
					if (!_longStopPrice.HasValue || newStop > _longStopPrice.Value)
					_longStopPrice = newStop;
				}
			}

			if (UseExitSignal && signals.MinorSellExit && _longEntryPrice is decimal longEntryPrice && price > longEntryPrice && !signals.MajorSell)
			{
				SellMarket(volume);
				return true;
			}

			if (signals.MajorSell)
			{
				SellMarket(volume);
				return true;
			}
		}
		else if (Position < 0m)
		{
			var volume = -Position;

			if (_shortTakeProfitPrice is decimal takeProfit && price <= takeProfit)
			{
				BuyMarket(volume);
				return true;
			}

			if (_shortStopPrice is decimal stopPrice && price >= stopPrice)
			{
				BuyMarket(volume);
				return true;
			}

			if (BreakEvenTrigger > 0 && !_shortBreakEvenActive && _shortEntryPrice is decimal entry)
			{
				var triggerPrice = entry - BreakEvenTrigger * _pipSize;
				if (price <= triggerPrice)
				{
					_shortBreakEvenActive = true;
					_shortStopPrice = entry - _pipSize;
				}
			}

			if (TrailingStopTrigger > 0 && TrailingStopPips > 0 && _shortEntryPrice is decimal shortEntry)
			{
				var triggerPrice = shortEntry - TrailingStopTrigger * _pipSize;
				if (price <= triggerPrice)
				{
					var newStop = price + TrailingStopPips * _pipSize;
					if (!_shortStopPrice.HasValue || newStop < _shortStopPrice.Value)
					_shortStopPrice = newStop;
				}
			}

			if (UseExitSignal && signals.MinorBuyExit && _shortEntryPrice is decimal shortEntryPrice && price < shortEntryPrice && !signals.MajorBuy)
			{
				BuyMarket(volume);
				return true;
			}

			if (signals.MajorBuy)
			{
				BuyMarket(volume);
				return true;
			}
		}

		return false;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var order = trade.Order;
		if (order == null || order.Security != Security)
		return;

		var signedDelta = trade.Volume * (order.Side == Sides.Buy ? 1m : -1m);
		var previousPosition = _signedPosition;
		_signedPosition += signedDelta;

		var tradePrice = trade.Trade?.Price ?? order.Price;

		if (previousPosition <= 0m && _signedPosition > 0m)
		{
			_longEntryPrice = tradePrice;
			_longTakeProfitPrice = TakeProfitPips > 0m ? tradePrice + TakeProfitPips * _pipSize : null;
			_longStopPrice = StopLossPips > 0m ? tradePrice - StopLossPips * _pipSize : null;
			_longBreakEvenActive = false;
			_lastTradeBarTime = _currentBarTime;
		}
		else if (previousPosition >= 0m && _signedPosition < 0m)
		{
			_shortEntryPrice = tradePrice;
			_shortTakeProfitPrice = TakeProfitPips > 0m ? tradePrice - TakeProfitPips * _pipSize : null;
			_shortStopPrice = StopLossPips > 0m ? tradePrice + StopLossPips * _pipSize : null;
			_shortBreakEvenActive = false;
			_lastTradeBarTime = _currentBarTime;
		}

		if (previousPosition > 0m && _signedPosition <= 0m)
		{
			ResetLongState();
		}

		if (previousPosition < 0m && _signedPosition >= 0m)
		{
			ResetShortState();
		}
	}

	private bool PassesMomentumFilter(Sides side, decimal momentumValue)
	{
		if (!UseMomentumFilter)
		return true;

		if (!_momentum.IsFormed)
		return false;

		return side == Sides.Buy
		? momentumValue >= MomentumTriggerLong
		: momentumValue <= MomentumTriggerShort;
	}

	private decimal ComputeZeroLagValue(decimal cyclePrice)
	{
		decimal source;
		int length;

		if (this.CycleFilterMode == CycleFilterMode.Sma)
		{
			source = cyclePrice;
			length = Math.Max(FilterStrengthSma, 1);
		}
		else
		{
			source = CalculateCycleRsi(cyclePrice);
			length = Math.Max(FilterStrengthRsi, 3);
		}

		return UpdateZeroLag(source, length);
	}

	private decimal CalculateCycleRsi(decimal cyclePrice)
	{
		var period = Math.Max(FilterStrengthRsi, 2);

		if (_previousCycleForRsi is not decimal previous)
		{
			_previousCycleForRsi = cyclePrice;
			_cycleRsiSamples = 1;
			_cycleRsiAvgGain = 0m;
			_cycleRsiAvgLoss = 0m;
			return 50m;
		}

		var change = cyclePrice - previous;
		var gain = change > 0m ? change : 0m;
		var loss = change < 0m ? -change : 0m;

		if (_cycleRsiSamples < period)
		{
			_cycleRsiAvgGain += gain;
			_cycleRsiAvgLoss += loss;
			_cycleRsiSamples++;

			if (_cycleRsiSamples == period)
			{
				_cycleRsiAvgGain /= period;
				_cycleRsiAvgLoss /= period;
			}
		}
		else
		{
			_cycleRsiAvgGain = ((_cycleRsiAvgGain * (period - 1)) + gain) / period;
			_cycleRsiAvgLoss = ((_cycleRsiAvgLoss * (period - 1)) + loss) / period;
		}

		_previousCycleForRsi = cyclePrice;

		if (_cycleRsiSamples < period)
		return 50m;

		if (_cycleRsiAvgLoss == 0m)
		return 100m;

		var rs = _cycleRsiAvgGain / _cycleRsiAvgLoss;
		var rsi = 100m - (100m / (1m + rs));
		return rsi;
	}

	private decimal UpdateZeroLag(decimal input, int length)
	{
		if (length < 3)
		{
			_zeroLagPrev2 = _zeroLagPrev1;
			_zeroLagPrev1 = input;
			return input;
		}

		var lengthD = (double)length;
		var aa = (decimal)Math.Exp(-1.4142135623730951 * Math.PI / lengthD);
		var bb = 2m * aa * (decimal)Math.Cos(1.4142135623730951 * Math.PI / lengthD);
		var cc = -aa * aa;
		var ca = 1m - bb - cc;
		var value = ca * input + bb * _zeroLagPrev1 + cc * _zeroLagPrev2;

		_zeroLagPrev2 = _zeroLagPrev1;
		_zeroLagPrev1 = value;
		return value;
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		if (!UseTimeRestriction)
		return true;

		var day = (int)time.DayOfWeek;
		if (day < DayEnd)
		return true;
		if (day > DayEnd)
		return false;

		return time.Hour <= HourEnd;
	}

	private bool IsOrderAllowed(ICandleMessage candle)
	{
		if (Position != 0m)
		return false;

		if (_lastTradeBarTime is DateTimeOffset last && last == candle.CloseTime)
		return false;

		return true;
	}

	private decimal GetPipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		return step > 0m ? step : 1m;
	}

	private void ResetLongState()
	{
		_longEntryPrice = null;
		_longStopPrice = null;
		_longTakeProfitPrice = null;
		_longBreakEvenActive = false;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = null;
		_shortStopPrice = null;
		_shortTakeProfitPrice = null;
		_shortBreakEvenActive = false;
	}
}

/// <summary>
/// Defines the data source used by the zero-lag filter.
/// </summary>
public enum CycleFilterMode
{
	/// <summary>
	/// Use the smoothed price series as filter input.
	/// </summary>
	Sma = 1,

	/// <summary>
	/// Use a Wilder style RSI computed on the smoothed price series as filter input.
	/// </summary>
	Rsi = 2,
}

internal readonly record struct CycleSignals(bool MajorBuy, bool MajorSell, bool MinorBuyExit, bool MinorSellExit);

internal sealed class CycleIdentifierState
{
	private int _barIndex;
	private decimal? _majorLow;
	private int _majorLowBar;
	private decimal? _majorHigh;
	private int _majorHighBar;
	private decimal? _minorLow;
	private int _minorLowBar;
	private decimal? _minorHigh;
	private int _minorHighBar;

	public CycleSignals Update(decimal cyclePrice, decimal minorThreshold, decimal majorThreshold, bool allowLongSignals, bool allowShortSignals, int minBarsBetweenSignals)
	{
		_barIndex++;

		if (_barIndex == 1)
		{
			_majorLow = cyclePrice;
			_majorHigh = cyclePrice;
			_minorLow = cyclePrice;
			_minorHigh = cyclePrice;
			_majorLowBar = _majorHighBar = _minorLowBar = _minorHighBar = _barIndex;
			return new CycleSignals(false, false, false, false);
		}

		var majorBuy = false;
		var majorSell = false;
		var minorBuy = false;
		var minorSell = false;

		if (_majorLow is null || cyclePrice <= _majorLow.Value)
		{
			_majorLow = cyclePrice;
			_majorLowBar = _barIndex;
		}
		else
		{
			var barsSince = _barIndex - _majorLowBar;
			if (allowLongSignals && barsSince >= minBarsBetweenSignals)
			{
				var diff = cyclePrice - _majorLow.Value;
				if (diff >= majorThreshold)
				{
					majorBuy = true;
					_majorHigh = cyclePrice;
					_majorHighBar = _barIndex;
					_majorLow = cyclePrice;
					_majorLowBar = _barIndex;
				}
			}
		}

		if (_majorHigh is null || cyclePrice >= _majorHigh.Value)
		{
			_majorHigh = cyclePrice;
			_majorHighBar = _barIndex;
		}
		else
		{
			var barsSince = _barIndex - _majorHighBar;
			if (allowShortSignals && barsSince >= minBarsBetweenSignals)
			{
				var diff = _majorHigh.Value - cyclePrice;
				if (diff >= majorThreshold)
				{
					majorSell = true;
					_majorLow = cyclePrice;
					_majorLowBar = _barIndex;
					_majorHigh = cyclePrice;
					_majorHighBar = _barIndex;
				}
			}
		}

		if (_minorLow is null || cyclePrice <= _minorLow.Value)
		{
			_minorLow = cyclePrice;
			_minorLowBar = _barIndex;
		}
		else
		{
			var barsSince = _barIndex - _minorLowBar;
			if (allowLongSignals && barsSince >= minBarsBetweenSignals)
			{
				var diff = cyclePrice - _minorLow.Value;
				if (diff >= minorThreshold)
				{
					minorBuy = true;
					_minorHigh = cyclePrice;
					_minorHighBar = _barIndex;
					_minorLow = cyclePrice;
					_minorLowBar = _barIndex;
				}
			}
		}

		if (_minorHigh is null || cyclePrice >= _minorHigh.Value)
		{
			_minorHigh = cyclePrice;
			_minorHighBar = _barIndex;
		}
		else
		{
			var barsSince = _barIndex - _minorHighBar;
			if (allowShortSignals && barsSince >= minBarsBetweenSignals)
			{
				var diff = _minorHigh.Value - cyclePrice;
				if (diff >= minorThreshold)
				{
					minorSell = true;
					_minorLow = cyclePrice;
					_minorLowBar = _barIndex;
					_minorHigh = cyclePrice;
					_minorHighBar = _barIndex;
				}
			}
		}

		return new CycleSignals(majorBuy, majorSell, minorBuy, minorSell);
	}

	public void Reset()
	{
		_barIndex = 0;
		_majorLow = null;
		_majorHigh = null;
		_minorLow = null;
		_minorHigh = null;
		_majorLowBar = 0;
		_majorHighBar = 0;
		_minorLowBar = 0;
		_minorHighBar = 0;
	}
}

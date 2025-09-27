using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader "RSI EA v2" expert advisor.
/// Trades RSI crosses with optional time filter, protective stops, trailing stop and risk based sizing.
/// </summary>
public class RsiEaV2Strategy : Strategy
{
	private readonly StrategyParam<bool> _openBuy;
	private readonly StrategyParam<bool> _openSell;
	private readonly StrategyParam<bool> _closeBySignal;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiBuyLevel;
	private readonly StrategyParam<decimal> _rsiSellLevel;
	private readonly StrategyParam<bool> _useRiskSizing;
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<bool> _useTimeControl;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private decimal? _previousRsi;

	private decimal _pipSize;
	private decimal _lastClosePrice;
	private decimal _previousPosition;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakeProfitPrice;
	private decimal? _shortTakeProfitPrice;

	private bool _longExitRequested;
	private bool _shortExitRequested;

	/// <summary>
	/// Enables long side trading when true.
	/// </summary>
	public bool OpenBuy
	{
		get => _openBuy.Value;
		set => _openBuy.Value = value;
	}

	/// <summary>
	/// Enables short side trading when true.
	/// </summary>
	public bool OpenSell
	{
		get => _openSell.Value;
		set => _openSell.Value = value;
	}

	/// <summary>
	/// Closes positions on the opposite RSI signal when enabled.
	/// </summary>
	public bool CloseBySignal
	{
		get => _closeBySignal.Value;
		set => _closeBySignal.Value = value;
	}

	/// <summary>
	/// Stop-loss distance measured in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance measured in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
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
	/// Extra price improvement in pips required before updating the trailing stop.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// RSI lookback period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Oversold level that triggers long entries.
	/// </summary>
	public decimal RsiBuyLevel
	{
		get => _rsiBuyLevel.Value;
		set => _rsiBuyLevel.Value = value;
	}

	/// <summary>
	/// Overbought level that triggers short entries.
	/// </summary>
	public decimal RsiSellLevel
	{
		get => _rsiSellLevel.Value;
		set => _rsiSellLevel.Value = value;
	}

	/// <summary>
	/// Switches between fixed volume and risk based sizing.
	/// </summary>
	public bool UseRiskSizing
	{
		get => _useRiskSizing.Value;
		set => _useRiskSizing.Value = value;
	}

	/// <summary>
	/// Default trade volume used in fixed sizing mode.
	/// </summary>
	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	/// <summary>
	/// Risk percentage of portfolio equity used when <see cref="UseRiskSizing"/> is true.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Enables the trading hours filter.
	/// </summary>
	public bool UseTimeControl
	{
		get => _useTimeControl.Value;
		set => _useTimeControl.Value = value;
	}

	/// <summary>
	/// Trading window start hour (inclusive).
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Trading window end hour (exclusive).
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Candles used for RSI calculations and decision making.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public RsiEaV2Strategy()
	{
		_openBuy = Param(nameof(OpenBuy), true)
			.SetDisplay("Enable Long", "Allow long side trades", "Trading");

		_openSell = Param(nameof(OpenSell), true)
			.SetDisplay("Enable Short", "Allow short side trades", "Trading");

		_closeBySignal = Param(nameof(CloseBySignal), true)
			.SetDisplay("Close By Signal", "Exit when RSI crosses the opposite threshold", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Distance of the protective stop", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Distance of the profit target", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetNotNegative()
			.SetDisplay("Trailing Step (pips)", "Extra move before trailing stop advances", "Risk");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetRange(2, 200)
			.SetDisplay("RSI Period", "Lookback period for RSI", "Indicator")
			.SetCanOptimize(true);

		_rsiBuyLevel = Param(nameof(RsiBuyLevel), 30m)
			.SetRange(0m, 100m)
			.SetDisplay("RSI Buy Level", "Oversold threshold", "Indicator")
			.SetCanOptimize(true);

		_rsiSellLevel = Param(nameof(RsiSellLevel), 70m)
			.SetRange(0m, 100m)
			.SetDisplay("RSI Sell Level", "Overbought threshold", "Indicator")
			.SetCanOptimize(true);

		_useRiskSizing = Param(nameof(UseRiskSizing), false)
			.SetDisplay("Use Risk Sizing", "Calculate volume from risk percent", "Money Management");

		_fixedVolume = Param(nameof(FixedVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Fixed Volume", "Default trade volume", "Money Management");

		_riskPercent = Param(nameof(RiskPercent), 1m)
			.SetNotNegative()
			.SetDisplay("Risk Percent", "Equity percentage risked when sizing trades", "Money Management");

		_useTimeControl = Param(nameof(UseTimeControl), true)
			.SetDisplay("Use Time Filter", "Restrict trading to a daily window", "Timing");

		_startHour = Param(nameof(StartHour), 10)
			.SetRange(0, 23)
			.SetDisplay("Start Hour", "Inclusive start hour (0-23)", "Timing");

		_endHour = Param(nameof(EndHour), 5)
			.SetRange(0, 23)
			.SetDisplay("End Hour", "Exclusive end hour (0-23)", "Timing");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for RSI", "General");
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

		_rsi = null;
		_previousRsi = null;
		_pipSize = 0m;
		_lastClosePrice = 0m;
		_previousPosition = 0m;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longTakeProfitPrice = null;
		_shortTakeProfitPrice = null;
		_longExitRequested = false;
		_shortExitRequested = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0m && TrailingStepPips <= 0m)
			throw new InvalidOperationException("Trailing stop requires a positive trailing step.");

		_pipSize = GetPipSize();
		Volume = FixedVolume;

		var rsi = new RSI { Length = RsiPeriod };
		_rsi = rsi;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		var position = Position;

		if (position > 0m)
		{
			if (_previousPosition <= 0m)
			{
				InitializeLongState();
				_shortEntryPrice = null;
				_shortStopPrice = null;
				_shortTakeProfitPrice = null;
				_shortExitRequested = false;
			}
			else
			{
				UpdateLongState();
			}
		}
		else if (position < 0m)
		{
			if (_previousPosition >= 0m)
			{
				InitializeShortState();
				_longEntryPrice = null;
				_longStopPrice = null;
				_longTakeProfitPrice = null;
				_longExitRequested = false;
			}
			else
			{
				UpdateShortState();
			}
		}
		else
		{
			ResetLongState();
			ResetShortState();
		}

		_previousPosition = position;
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		// Skip unfinished candles to avoid premature signals.
		if (candle.State != CandleStates.Finished)
			return;

		_lastClosePrice = candle.ClosePrice;

		var rsiIndicator = _rsi;
		if (rsiIndicator == null || !rsiIndicator.IsFormed)
		{
			_previousRsi = rsiValue;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousRsi = rsiValue;
			return;
		}

		UpdateTrailingStops(candle);

		var previousRsi = _previousRsi;
		if (previousRsi == null)
		{
			_previousRsi = rsiValue;
			return;
		}

		HandleExitSignals(candle, rsiValue, previousRsi.Value);
		HandleEntrySignals(candle, rsiValue, previousRsi.Value);

		_previousRsi = rsiValue;
	}

	private void HandleEntrySignals(ICandleMessage candle, decimal currentRsi, decimal previousRsi)
	{
		if (!IsTradingTime(candle.OpenTime))
			return;

		if (OpenBuy && currentRsi > RsiBuyLevel && previousRsi < RsiBuyLevel)
			TryOpenLong();

		if (OpenSell && currentRsi < RsiSellLevel && previousRsi > RsiSellLevel)
			TryOpenShort();
	}

	private void HandleExitSignals(ICandleMessage candle, decimal currentRsi, decimal previousRsi)
	{
		var position = Position;

		if (position > 0m)
		{
			// Check protective targets before interpreting the signal.
			if (_longTakeProfitPrice is decimal longTp && candle.HighPrice >= longTp)
				TryCloseLong("Take-profit hit");
			else if (_longStopPrice is decimal longSl && candle.LowPrice <= longSl)
				TryCloseLong("Stop-loss hit");
			else if (CloseBySignal && currentRsi < RsiSellLevel && previousRsi > RsiSellLevel)
				TryCloseLong("RSI exit signal");
		}
		else if (position < 0m)
		{
			if (_shortTakeProfitPrice is decimal shortTp && candle.LowPrice <= shortTp)
				TryCloseShort("Take-profit hit");
			else if (_shortStopPrice is decimal shortSl && candle.HighPrice >= shortSl)
				TryCloseShort("Stop-loss hit");
			else if (CloseBySignal && currentRsi > RsiBuyLevel && previousRsi < RsiBuyLevel)
				TryCloseShort("RSI exit signal");
		}
	}

	private void TryOpenLong()
	{
		var stopDistance = StopLossPips > 0m ? GetPriceOffset(StopLossPips) : 0m;
		var volume = CalculateOrderVolume(stopDistance);
		if (volume <= 0m)
			return;

		var orderVolume = Position < 0m ? volume + Math.Abs(Position) : volume;
		orderVolume = NormalizeVolume(orderVolume);
		if (orderVolume <= 0m)
			return;

		Volume = volume;
		_longExitRequested = false;
		BuyMarket(orderVolume);
	}

	private void TryOpenShort()
	{
		var stopDistance = StopLossPips > 0m ? GetPriceOffset(StopLossPips) : 0m;
		var volume = CalculateOrderVolume(stopDistance);
		if (volume <= 0m)
			return;

		var orderVolume = Position > 0m ? volume + Math.Abs(Position) : volume;
		orderVolume = NormalizeVolume(orderVolume);
		if (orderVolume <= 0m)
			return;

		Volume = volume;
		_shortExitRequested = false;
		SellMarket(orderVolume);
	}

	private void TryCloseLong(string reason)
	{
		if (_longExitRequested)
			return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		_longExitRequested = true;
		LogInfo($"Closing long position: {reason}");
		SellMarket(volume);
	}

	private void TryCloseShort(string reason)
	{
		if (_shortExitRequested)
			return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		_shortExitRequested = true;
		LogInfo($"Closing short position: {reason}");
		BuyMarket(volume);
	}

	private void UpdateTrailingStops(ICandleMessage candle)
	{
		if (Position > 0m)
			UpdateLongTrailing(candle.ClosePrice);
		else if (Position < 0m)
			UpdateShortTrailing(candle.ClosePrice);
	}

	private void UpdateLongTrailing(decimal closePrice)
	{
		if (TrailingStopPips <= 0m || _longEntryPrice is not decimal entry)
			return;

		var trailingDistance = GetPriceOffset(TrailingStopPips);
		var trailingStep = GetPriceOffset(TrailingStepPips);
		if (trailingDistance <= 0m)
			return;

		var profit = closePrice - entry;
		if (profit <= trailingDistance + trailingStep)
			return;

		var newStop = closePrice - trailingDistance;
		if (_longStopPrice is decimal currentStop && newStop - currentStop < trailingStep)
			return;

		_longStopPrice = newStop;
	}

	private void UpdateShortTrailing(decimal closePrice)
	{
		if (TrailingStopPips <= 0m || _shortEntryPrice is not decimal entry)
			return;

		var trailingDistance = GetPriceOffset(TrailingStopPips);
		var trailingStep = GetPriceOffset(TrailingStepPips);
		if (trailingDistance <= 0m)
			return;

		var profit = entry - closePrice;
		if (profit <= trailingDistance + trailingStep)
			return;

		var newStop = closePrice + trailingDistance;
		if (_shortStopPrice is decimal currentStop && currentStop - newStop < trailingStep)
			return;

		_shortStopPrice = newStop;
	}

	private void InitializeLongState()
	{
		var entryPrice = PositionPrice ?? (_lastClosePrice > 0m ? _lastClosePrice : (decimal?)null);
		if (entryPrice == null)
			return;

		_longEntryPrice = entryPrice;
		_longStopPrice = StopLossPips > 0m ? entryPrice - GetPriceOffset(StopLossPips) : null;
		_longTakeProfitPrice = TakeProfitPips > 0m ? entryPrice + GetPriceOffset(TakeProfitPips) : null;
		_longExitRequested = false;
	}

	private void InitializeShortState()
	{
		var entryPrice = PositionPrice ?? (_lastClosePrice > 0m ? _lastClosePrice : (decimal?)null);
		if (entryPrice == null)
			return;

		_shortEntryPrice = entryPrice;
		_shortStopPrice = StopLossPips > 0m ? entryPrice + GetPriceOffset(StopLossPips) : null;
		_shortTakeProfitPrice = TakeProfitPips > 0m ? entryPrice - GetPriceOffset(TakeProfitPips) : null;
		_shortExitRequested = false;
	}

	private void UpdateLongState()
	{
		if (PositionPrice is decimal avgPrice)
			_longEntryPrice = avgPrice;
	}

	private void UpdateShortState()
	{
		if (PositionPrice is decimal avgPrice)
			_shortEntryPrice = avgPrice;
	}

	private void ResetLongState()
	{
		_longEntryPrice = null;
		_longStopPrice = null;
		_longTakeProfitPrice = null;
		_longExitRequested = false;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = null;
		_shortStopPrice = null;
		_shortTakeProfitPrice = null;
		_shortExitRequested = false;
	}

	private decimal CalculateOrderVolume(decimal stopDistance)
	{
		if (UseRiskSizing)
		{
			var riskVolume = CalculateRiskVolume(stopDistance);
			if (riskVolume > 0m)
				return NormalizeVolume(riskVolume);
		}

		return NormalizeVolume(FixedVolume);
	}

	private decimal CalculateRiskVolume(decimal stopDistance)
	{
		if (stopDistance <= 0m)
			return 0m;

		var percent = RiskPercent;
		if (percent <= 0m)
			return 0m;

		var equity = Portfolio?.CurrentValue ?? 0m;
		if (equity <= 0m)
			return 0m;

		var priceStep = Security?.PriceStep ?? 0m;
		var stepPrice = Security?.StepPrice ?? 0m;
		if (priceStep <= 0m || stepPrice <= 0m)
			return 0m;

		var riskAmount = equity * (percent / 100m);
		if (riskAmount <= 0m)
			return 0m;

		var lossPerUnit = stopDistance / priceStep * stepPrice;
		if (lossPerUnit <= 0m)
			return 0m;

		return riskAmount / lossPerUnit;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var step = Security?.VolumeStep ?? 0m;
		if (step <= 0m)
			step = 1m;

		var normalized = Math.Floor(volume / step) * step;

		var minVolume = Security?.MinVolume ?? 0m;
		if (minVolume > 0m && normalized < minVolume)
			normalized = minVolume;

		var maxVolume = Security?.MaxVolume ?? 0m;
		if (maxVolume > 0m && normalized > maxVolume)
			normalized = maxVolume;

		return normalized;
	}

	private decimal GetPriceOffset(decimal value)
	{
		if (value == 0m)
			return 0m;

		return value * (_pipSize > 0m ? _pipSize : 1m);
	}

	private decimal GetPipSize()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
			return 1m;

		var decimals = Security?.Decimals;
		if (decimals == 3 || decimals == 5)
			return priceStep * 10m;

		return priceStep;
	}

	private bool IsTradingTime(DateTimeOffset time)
	{
		if (!UseTimeControl)
			return true;

		var hour = time.Hour;
		var start = StartHour;
		var end = EndHour;

		if (start == end)
			return false;

		if (start < end)
			return hour >= start && hour < end;

		return hour >= start || hour < end;
	}
}


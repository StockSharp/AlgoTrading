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
/// Accelerator Trailing TP &amp; SL strategy.
/// Combines accelerator oscillator, multi-timeframe momentum and monthly MACD filters with trailing logic.
/// </summary>
public class AcceleratorTrailingTPSLStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _breakEvenTriggerPips;
	private readonly StrategyParam<decimal> _breakEvenOffsetPips;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _lotExponent;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<bool> _closeOnMacdFlip;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _momentumCandleType;
	private readonly StrategyParam<DataType> _macdCandleType;

	private readonly LinearWeightedMovingAverage _fastMa = new();
	private readonly LinearWeightedMovingAverage _slowMa = new();
	private readonly AcceleratorOscillator _accelerator = new();
	private readonly Momentum _momentum = new() { Length = 14 };
	private readonly Macd _macd = new() { Fast = 12, Slow = 26, Signal = 9 };

	private decimal? _momentumAbs1;
	private decimal? _momentumAbs2;
	private decimal? _momentumAbs3;
	private bool _momentumReady;

	private decimal? _macdMain;
	private decimal? _macdSignal;
	private bool _macdReady;

	private decimal? _entryPrice;
	private decimal _highestSinceEntry;
	private decimal _lowestSinceEntry;
	private decimal? _breakEvenPrice;
	private int _longEntries;
	private int _shortEntries;

	/// <summary>
	/// Initializes <see cref="AcceleratorTrailingTPSLStrategy"/>.
	/// </summary>
	public AcceleratorTrailingTPSLStrategy()
	{
		_fastMaLength = Param(nameof(FastMaLength), 6)
		.SetDisplay("Fast MA", "Length of the fast LWMA", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(4, 12, 2);

		_slowMaLength = Param(nameof(SlowMaLength), 85)
		.SetDisplay("Slow MA", "Length of the slow LWMA", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(40, 120, 5);

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m)
		.SetDisplay("Momentum Threshold", "Minimum |Momentum - 100| on higher timeframe", "Filters")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 1m, 0.1m);

		_stopLossPips = Param(nameof(StopLossPips), 20m)
		.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
		.SetDisplay("Take Profit (pips)", "Profit target distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 40m)
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk");

		_breakEvenTriggerPips = Param(nameof(BreakEvenTriggerPips), 30m)
		.SetDisplay("Break-even Trigger", "Profit distance that activates break-even", "Risk");

		_breakEvenOffsetPips = Param(nameof(BreakEvenOffsetPips), 30m)
		.SetDisplay("Break-even Offset", "Offset applied to the stop once break-even is activated", "Risk");

		_maxTrades = Param(nameof(MaxTrades), 10)
		.SetDisplay("Max Trades", "Maximum layered entries per direction", "General");

		_baseVolume = Param(nameof(BaseVolume), 1m)
		.SetDisplay("Base Volume", "Volume of the first entry", "Orders");

		_lotExponent = Param(nameof(LotExponent), 1.44m)
		.SetDisplay("Lot Exponent", "Multiplier applied to each additional entry", "Orders");

		_enableTrailing = Param(nameof(EnableTrailing), true)
		.SetDisplay("Enable Trailing", "Enable trailing stop management", "Risk");

		_useBreakEven = Param(nameof(UseBreakEven), true)
		.SetDisplay("Enable Break-even", "Move the stop to break-even after profits", "Risk");

		_closeOnMacdFlip = Param(nameof(CloseOnMacdFlip), false)
		.SetDisplay("Exit On MACD Flip", "Force exit when monthly MACD turns against position", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Signal Candles", "Primary candle series", "General");

		_momentumCandleType = Param(nameof(MomentumCandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Momentum Candles", "Higher timeframe used for momentum", "General");

		_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromDays(30).TimeFrame())
		.SetDisplay("MACD Candles", "Timeframe used for MACD filter", "General");
	}

	/// <summary>
	/// Length of the fast LWMA.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Length of the slow LWMA.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// Minimum absolute momentum difference required.
	/// </summary>
	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Break-even activation distance in pips.
	/// </summary>
	public decimal BreakEvenTriggerPips
	{
		get => _breakEvenTriggerPips.Value;
		set => _breakEvenTriggerPips.Value = value;
	}

	/// <summary>
	/// Break-even offset applied to the stop.
	/// </summary>
	public decimal BreakEvenOffsetPips
	{
		get => _breakEvenOffsetPips.Value;
		set => _breakEvenOffsetPips.Value = value;
	}

	/// <summary>
	/// Maximum number of layered entries per direction.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Volume of the initial trade.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Geometric multiplier applied to each additional entry.
	/// </summary>
	public decimal LotExponent
	{
		get => _lotExponent.Value;
		set => _lotExponent.Value = value;
	}

	/// <summary>
	/// Enables or disables trailing stop management.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Enables the break-even stop movement.
	/// </summary>
	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	/// <summary>
	/// Exit position when MACD flips against it.
	/// </summary>
	public bool CloseOnMacdFlip
	{
		get => _closeOnMacdFlip.Value;
		set => _closeOnMacdFlip.Value = value;
	}

	/// <summary>
	/// Primary candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe used to evaluate momentum.
	/// </summary>
	public DataType MomentumCandleType
	{
		get => _momentumCandleType.Value;
		set => _momentumCandleType.Value = value;
	}

	/// <summary>
	/// Timeframe used for the MACD filter.
	/// </summary>
	public DataType MacdCandleType
	{
		get => _macdCandleType.Value;
		set => _macdCandleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
		(Security, CandleType),
		(Security, MomentumCandleType),
		(Security, MacdCandleType)
		];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fastMa.Length = FastMaLength;
		_slowMa.Length = SlowMaLength;

		_momentumAbs1 = null;
		_momentumAbs2 = null;
		_momentumAbs3 = null;
		_momentumReady = false;

		_macdMain = null;
		_macdSignal = null;
		_macdReady = false;

		ResetPositionState();
		_longEntries = 0;
		_shortEntries = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa.Length = FastMaLength;
		_slowMa.Length = SlowMaLength;
		_momentum.Length = 14;

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription
		.Bind(_fastMa, _slowMa, _accelerator, ProcessMainCandle)
		.Start();

		var momentumSubscription = SubscribeCandles(MomentumCandleType);
		momentumSubscription
		.Bind(_momentum, ProcessMomentum)
		.Start();

		var macdSubscription = SubscribeCandles(MacdCandleType);
		macdSubscription
		.Bind(_macd, ProcessMacd)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _accelerator);
			DrawOwnTrades(area);
		}
	}

	private void ProcessMomentum(ICandleMessage candle, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var absValue = Math.Abs(momentumValue - 100m);
		_momentumAbs3 = _momentumAbs2;
		_momentumAbs2 = _momentumAbs1;
		_momentumAbs1 = absValue;
		_momentumReady = _momentum.IsFormed;
	}

	private void ProcessMacd(ICandleMessage candle, decimal macdValue, decimal signalValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_macdMain = macdValue;
		_macdSignal = signalValue;
		_macdReady = _macd.IsFormed;
	}

	private void ProcessMainCandle(ICandleMessage candle, decimal fastMaValue, decimal slowMaValue, decimal acceleratorValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdatePositionState(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_momentumReady || !_macdReady)
		return;

		if (!HasMomentumSignal())
		return;

		if (!(_macdMain is decimal macdMain) || !(_macdSignal is decimal macdSignal))
		return;

		if (CloseOnMacdFlip)
		TryCloseOnMacd(candle, macdMain, macdSignal);

		var pip = GetPipSize();

		var macdBull = (macdMain > 0m && macdMain > macdSignal) || (macdMain < 0m && macdMain > macdSignal);
		var macdBear = (macdMain > 0m && macdMain < macdSignal) || (macdMain < 0m && macdMain < macdSignal);

		if (acceleratorValue > 0m && fastMaValue > slowMaValue && macdBull)
		{
			if (Position < 0m)
			ClosePosition();

			if (CanAddLong())
			{
				var volume = CalculateLongVolume();
				BuyMarket(volume);
				_longEntries++;
				_shortEntries = 0;
				InitializeEntryState(candle.ClosePrice);
				LogInfo($"Buy signal: AC {acceleratorValue:F4} > 0, fast LWMA {fastMaValue:F5} above slow {slowMaValue:F5}, MACD filter active.");
			}
		}
		else if (acceleratorValue < 0m && fastMaValue < slowMaValue && macdBear)
		{
			if (Position > 0m)
			ClosePosition();

			if (CanAddShort())
			{
				var volume = CalculateShortVolume();
				SellMarket(volume);
				_shortEntries++;
				_longEntries = 0;
				InitializeEntryState(candle.ClosePrice);
				LogInfo($"Sell signal: AC {acceleratorValue:F4} < 0, fast LWMA {fastMaValue:F5} below slow {slowMaValue:F5}, MACD filter active.");
			}
		}
	}

	private void TryCloseOnMacd(ICandleMessage candle, decimal macdMain, decimal macdSignal)
	{
		if (Position > 0m)
		{
			var macdBear = (macdMain > 0m && macdMain < macdSignal) || (macdMain < 0m && macdMain < macdSignal);
			if (macdBear)
			{
				LogInfo($"MACD flip detected, closing long at {candle.ClosePrice:F5}.");
				ClosePosition();
			}
		}
		else if (Position < 0m)
		{
			var macdBull = (macdMain > 0m && macdMain > macdSignal) || (macdMain < 0m && macdMain > macdSignal);
			if (macdBull)
			{
				LogInfo($"MACD flip detected, closing short at {candle.ClosePrice:F5}.");
				ClosePosition();
			}
		}
	}

	private bool HasMomentumSignal()
	{
		if (_momentumAbs1 is not decimal m1 || _momentumAbs2 is not decimal m2 || _momentumAbs3 is not decimal m3)
		return false;

		return m1 >= MomentumThreshold || m2 >= MomentumThreshold || m3 >= MomentumThreshold;
	}

	private bool CanAddLong()
	{
		if (BaseVolume <= 0m)
		return false;

		return _longEntries < MaxTrades;
	}

	private bool CanAddShort()
	{
		if (BaseVolume <= 0m)
		return false;

		return _shortEntries < MaxTrades;
	}

	private decimal CalculateLongVolume()
	{
		var exponent = (decimal)Math.Pow((double)LotExponent, _longEntries);
		return BaseVolume * exponent;
	}

	private decimal CalculateShortVolume()
	{
		var exponent = (decimal)Math.Pow((double)LotExponent, _shortEntries);
		return BaseVolume * exponent;
	}

	private void InitializeEntryState(decimal price)
	{
		_entryPrice = price;
		_highestSinceEntry = price;
		_lowestSinceEntry = price;
		_breakEvenPrice = null;
	}

	private void UpdatePositionState(ICandleMessage candle)
	{
		if (Position > 0m)
		UpdateLongState(candle);
		else if (Position < 0m)
		UpdateShortState(candle);
		else
		ResetPositionState();
	}

	private void UpdateLongState(ICandleMessage candle)
	{
		if (_entryPrice is not decimal entry)
		return;

		_highestSinceEntry = Math.Max(_highestSinceEntry, candle.HighPrice);

		var pip = GetPipSize();
		var stopLevel = entry - StopLossPips * pip;
		var takeLevel = entry + TakeProfitPips * pip;

		if (StopLossPips > 0m && candle.LowPrice <= stopLevel)
		{
			LogInfo($"Stop loss hit for long position at {stopLevel:F5}.");
			ClosePosition();
			return;
		}

		if (TakeProfitPips > 0m && candle.HighPrice >= takeLevel)
		{
			LogInfo($"Take profit reached for long position at {takeLevel:F5}.");
			ClosePosition();
			return;
		}

		if (UseBreakEven && BreakEvenTriggerPips > 0m && _breakEvenPrice is null)
		{
			var trigger = entry + BreakEvenTriggerPips * pip;
			if (candle.HighPrice >= trigger)
			{
				_breakEvenPrice = entry + BreakEvenOffsetPips * pip;
				LogInfo($"Break-even activated for long position at {_breakEvenPrice:F5}.");
			}
		}

		if (_breakEvenPrice is decimal bePrice && candle.ClosePrice <= bePrice)
		{
			LogInfo($"Break-even exit triggered for long position at {bePrice:F5}.");
			ClosePosition();
			return;
		}

		if (EnableTrailing && TrailingStopPips > 0m)
		{
			var trailingLevel = _highestSinceEntry - TrailingStopPips * pip;
			if (candle.ClosePrice <= trailingLevel)
			{
				LogInfo($"Trailing stop exit for long position at {trailingLevel:F5}.");
				ClosePosition();
			}
		}
	}

	private void UpdateShortState(ICandleMessage candle)
	{
		if (_entryPrice is not decimal entry)
		return;

		_lowestSinceEntry = Math.Min(_lowestSinceEntry, candle.LowPrice);

		var pip = GetPipSize();
		var stopLevel = entry + StopLossPips * pip;
		var takeLevel = entry - TakeProfitPips * pip;

		if (StopLossPips > 0m && candle.HighPrice >= stopLevel)
		{
			LogInfo($"Stop loss hit for short position at {stopLevel:F5}.");
			ClosePosition();
			return;
		}

		if (TakeProfitPips > 0m && candle.LowPrice <= takeLevel)
		{
			LogInfo($"Take profit reached for short position at {takeLevel:F5}.");
			ClosePosition();
			return;
		}

		if (UseBreakEven && BreakEvenTriggerPips > 0m && _breakEvenPrice is null)
		{
			var trigger = entry - BreakEvenTriggerPips * pip;
			if (candle.LowPrice <= trigger)
			{
				_breakEvenPrice = entry - BreakEvenOffsetPips * pip;
				LogInfo($"Break-even activated for short position at {_breakEvenPrice:F5}.");
			}
		}

		if (_breakEvenPrice is decimal bePrice && candle.ClosePrice >= bePrice)
		{
			LogInfo($"Break-even exit triggered for short position at {bePrice:F5}.");
			ClosePosition();
			return;
		}

		if (EnableTrailing && TrailingStopPips > 0m)
		{
			var trailingLevel = _lowestSinceEntry + TrailingStopPips * pip;
			if (candle.ClosePrice >= trailingLevel)
			{
				LogInfo($"Trailing stop exit for short position at {trailingLevel:F5}.");
				ClosePosition();
			}
		}
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_highestSinceEntry = 0m;
		_lowestSinceEntry = 0m;
		_breakEvenPrice = null;
	}

	private decimal GetPipSize()
	{
		var step = Security?.PriceStep;
		if (step is null || step == 0m)
		return 1m;

		return step.Value;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			_longEntries = 0;
			_shortEntries = 0;
			ResetPositionState();
		}
	}
}


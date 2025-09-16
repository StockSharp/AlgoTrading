using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-indicator strategy that combines EMA, Stochastic, and RSI confirmations.
/// </summary>
public class VrZverStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _useMovingAverage;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _verySlowMaPeriod;
	private readonly StrategyParam<bool> _useStochastic;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<int> _stochasticSlowing;
	private readonly StrategyParam<int> _stochasticUpperLevel;
	private readonly StrategyParam<int> _stochasticLowerLevel;
	private readonly StrategyParam<bool> _useRsi;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _rsiUpperLevel;
	private readonly StrategyParam<int> _rsiLowerLevel;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _breakevenPips;
	private readonly StrategyParam<decimal> _volume;

	private ExponentialMovingAverage _fastMa = null!;
	private ExponentialMovingAverage _slowMa = null!;
	private ExponentialMovingAverage _verySlowMa = null!;
	private StochasticOscillator _stochastic = null!;
	private RelativeStrengthIndex _rsi = null!;

	private decimal _pipSize;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortTakePrice;
	private decimal? _longBreakevenTrigger;
	private decimal? _shortBreakevenTrigger;
	private bool _longBreakevenArmed;
	private bool _shortBreakevenArmed;

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Enable moving average confirmation.
	/// </summary>
	public bool UseMovingAverage
	{
		get => _useMovingAverage.Value;
		set => _useMovingAverage.Value = value;
	}

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Very slow EMA period.
	/// </summary>
	public int VerySlowMaPeriod
	{
		get => _verySlowMaPeriod.Value;
		set => _verySlowMaPeriod.Value = value;
	}

	/// <summary>
	/// Enable Stochastic confirmation.
	/// </summary>
	public bool UseStochastic
	{
		get => _useStochastic.Value;
		set => _useStochastic.Value = value;
	}

	/// <summary>
	/// Stochastic oscillator %K period.
	/// </summary>
	public int StochasticKPeriod
	{
		get => _stochasticKPeriod.Value;
		set => _stochasticKPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic %D smoothing period.
	/// </summary>
	public int StochasticDPeriod
	{
		get => _stochasticDPeriod.Value;
		set => _stochasticDPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic %K smoothing period.
	/// </summary>
	public int StochasticSlowing
	{
		get => _stochasticSlowing.Value;
		set => _stochasticSlowing.Value = value;
	}

	/// <summary>
	/// Upper threshold for Stochastic %K.
	/// </summary>
	public int StochasticUpperLevel
	{
		get => _stochasticUpperLevel.Value;
		set => _stochasticUpperLevel.Value = value;
	}

	/// <summary>
	/// Lower threshold for Stochastic %K.
	/// </summary>
	public int StochasticLowerLevel
	{
		get => _stochasticLowerLevel.Value;
		set => _stochasticLowerLevel.Value = value;
	}

	/// <summary>
	/// Enable RSI confirmation.
	/// </summary>
	public bool UseRsi
	{
		get => _useRsi.Value;
		set => _useRsi.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI upper threshold.
	/// </summary>
	public int RsiUpperLevel
	{
		get => _rsiUpperLevel.Value;
		set => _rsiUpperLevel.Value = value;
	}

	/// <summary>
	/// RSI lower threshold.
	/// </summary>
	public int RsiLowerLevel
	{
		get => _rsiLowerLevel.Value;
		set => _rsiLowerLevel.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Breakeven activation distance in pips.
	/// </summary>
	public int BreakevenPips
	{
		get => _breakevenPips.Value;
		set => _breakevenPips.Value = value;
	}

	/// <summary>
	/// Trading volume per entry.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="VrZverStrategy"/>.
	/// </summary>
	public VrZverStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");

		_useMovingAverage = Param(nameof(UseMovingAverage), true)
		.SetDisplay("Use EMA", "Enable EMA trend filter", "Signals");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 3)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA", "Fast EMA period", "Signals");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA", "Slow EMA period", "Signals");

		_verySlowMaPeriod = Param(nameof(VerySlowMaPeriod), 7)
		.SetGreaterThanZero()
		.SetDisplay("Very Slow EMA", "Very slow EMA period", "Signals");

		_useStochastic = Param(nameof(UseStochastic), true)
		.SetDisplay("Use Stochastic", "Enable stochastic confirmation", "Signals");

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 42)
		.SetGreaterThanZero()
		.SetDisplay("Stoch %K", "Stochastic %K period", "Signals");

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("Stoch %D", "Stochastic %D period", "Signals");

		_stochasticSlowing = Param(nameof(StochasticSlowing), 7)
		.SetGreaterThanZero()
		.SetDisplay("Stoch Smoothing", "Stochastic %K smoothing", "Signals");

		_stochasticUpperLevel = Param(nameof(StochasticUpperLevel), 60)
		.SetDisplay("Stoch Upper", "Upper stochastic threshold", "Signals");

		_stochasticLowerLevel = Param(nameof(StochasticLowerLevel), 40)
		.SetDisplay("Stoch Lower", "Lower stochastic threshold", "Signals");

		_useRsi = Param(nameof(UseRsi), true)
		.SetDisplay("Use RSI", "Enable RSI confirmation", "Signals");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI Period", "RSI averaging period", "Signals");

		_rsiUpperLevel = Param(nameof(RsiUpperLevel), 60)
		.SetDisplay("RSI Upper", "Upper RSI threshold", "Signals");

		_rsiLowerLevel = Param(nameof(RsiLowerLevel), 40)
		.SetDisplay("RSI Lower", "Lower RSI threshold", "Signals");

		_stopLossPips = Param(nameof(StopLossPips), 50)
		.SetNotNegative()
		.SetDisplay("Stop Loss", "Stop loss distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 70)
		.SetNotNegative()
		.SetDisplay("Take Profit", "Take profit distance in pips", "Risk");

		_breakevenPips = Param(nameof(BreakevenPips), 20)
		.SetNotNegative()
		.SetDisplay("Breakeven", "Breakeven activation distance", "Risk");

		_volume = Param(nameof(Volume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume", "Trading");
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

		ResetPositionState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new ExponentialMovingAverage { Length = FastMaPeriod };
		_slowMa = new ExponentialMovingAverage { Length = SlowMaPeriod };
		_verySlowMa = new ExponentialMovingAverage { Length = VerySlowMaPeriod };

		_stochastic = new StochasticOscillator
		{
			Length = StochasticKPeriod,
			K = { Length = StochasticSlowing },
			D = { Length = StochasticDPeriod },
		};

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_fastMa, _slowMa, _verySlowMa, _stochastic, _rsi, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _verySlowMa);
			DrawIndicator(area, _stochastic);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(
	ICandleMessage candle,
	IIndicatorValue fastValue,
	IIndicatorValue slowValue,
	IIndicatorValue verySlowValue,
	IIndicatorValue stochasticValue,
	IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!fastValue.IsFinal || !slowValue.IsFinal || !verySlowValue.IsFinal || !stochasticValue.IsFinal || !rsiValue.IsFinal)
		return;

		// Extract indicator outputs for the completed candle.
		var fast = fastValue.GetValue<decimal>();
		var slow = slowValue.GetValue<decimal>();
		var verySlow = verySlowValue.GetValue<decimal>();

		var stochastic = (StochasticOscillatorValue)stochasticValue;
		if (stochastic.K is not decimal stochK || stochastic.D is not decimal stochD)
		return;

		var rsi = rsiValue.GetValue<decimal>();

		var longClosed = HandleLongPosition(candle);
		if (!longClosed && Position > 0)
		return;

		var shortClosed = HandleShortPosition(candle);
		if (!shortClosed && Position < 0)
		return;

		if (Position == 0)
		ResetPositionState();

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (Position != 0)
		return;

		CancelActiveOrders();

		if (!UseMovingAverage && !UseStochastic && !UseRsi)
		return;

		// All enabled filters must align to confirm the long scenario.
		var longSignal = (!UseMovingAverage || fast > slow && slow > verySlow)
		&& (!UseStochastic || stochD < stochK && stochK < StochasticLowerLevel)
		&& (!UseRsi || rsi < RsiLowerLevel);

		var shortSignal = (!UseMovingAverage || verySlow > slow && slow > fast)
		&& (!UseStochastic || stochD > stochK && stochK > StochasticUpperLevel)
		&& (!UseRsi || rsi > RsiUpperLevel);

		if (longSignal && Volume > 0)
		{
			EnterLong(candle.ClosePrice);
		}
		else if (shortSignal && Volume > 0)
		{
			EnterShort(candle.ClosePrice);
		}
	}

	private void EnterLong(decimal price)
	{
		BuyMarket(Volume);

		_longEntryPrice = price;
		_longStopPrice = StopLossPips > 0 ? price - StopLossPips * _pipSize : null;
		_longTakePrice = TakeProfitPips > 0 ? price + TakeProfitPips * _pipSize : null;
		_longBreakevenTrigger = BreakevenPips > 0 ? price + BreakevenPips * _pipSize : null;
		_longBreakevenArmed = false;
	}

	private void EnterShort(decimal price)
	{
		SellMarket(Volume);

		_shortEntryPrice = price;
		_shortStopPrice = StopLossPips > 0 ? price + StopLossPips * _pipSize : null;
		_shortTakePrice = TakeProfitPips > 0 ? price - TakeProfitPips * _pipSize : null;
		_shortBreakevenTrigger = BreakevenPips > 0 ? price - BreakevenPips * _pipSize : null;
		_shortBreakevenArmed = false;
	}

	private bool HandleLongPosition(ICandleMessage candle)
	{
		if (Position <= 0 || _longEntryPrice is null)
		return false;

		var positionVolume = Position;

		// Exit the long trade if the protective stop is touched within the candle range.
		if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
		{
			SellMarket(positionVolume);
			ResetPositionState();
			return true;
		}

		// Take profit when the upper target is reached.
		if (_longTakePrice is decimal take && candle.HighPrice >= take)
		{
			SellMarket(positionVolume);
			ResetPositionState();
			return true;
		}

		if (!_longBreakevenArmed && _longBreakevenTrigger is decimal trigger && candle.HighPrice >= trigger)
		_longBreakevenArmed = true;

		// Once breakeven is armed, protect the entry price if momentum fades.
		if (_longBreakevenArmed && candle.LowPrice <= _longEntryPrice.Value)
		{
			SellMarket(positionVolume);
			ResetPositionState();
			return true;
		}

		return false;
	}

	private bool HandleShortPosition(ICandleMessage candle)
	{
		if (Position >= 0 || _shortEntryPrice is null)
		return false;

		var positionVolume = Math.Abs(Position);

		// Cover the short trade if the stop level is breached.
		if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
		{
			BuyMarket(positionVolume);
			ResetPositionState();
			return true;
		}

		// Lock in profits if the target is met on the downside move.
		if (_shortTakePrice is decimal take && candle.LowPrice <= take)
		{
			BuyMarket(positionVolume);
			ResetPositionState();
			return true;
		}

		if (!_shortBreakevenArmed && _shortBreakevenTrigger is decimal trigger && candle.LowPrice <= trigger)
		_shortBreakevenArmed = true;

		// Breakeven logic mirrors the long side to guard against reversals.
		if (_shortBreakevenArmed && candle.HighPrice >= _shortEntryPrice.Value)
		{
			BuyMarket(positionVolume);
			ResetPositionState();
			return true;
		}

		return false;
	}

	private void ResetPositionState()
	{
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longTakePrice = null;
		_shortTakePrice = null;
		_longBreakevenTrigger = null;
		_shortBreakevenTrigger = null;
		_longBreakevenArmed = false;
		_shortBreakevenArmed = false;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0.0001m;
		if (step <= 0)
		step = 0.0001m;

		var decimals = Security?.Decimals ?? 0;
		return decimals is 3 or 5 ? step * 10m : step;
	}
}

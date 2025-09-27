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
/// Port of the "Day Trading PAMXA" MetaTrader strategy.
/// Combines Awesome Oscillator reversals with a stochastic filter and pip-based risk controls.
/// </summary>
public class DayTradingPamxaStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<PositionSizingModes> _moneyMode;
	private readonly StrategyParam<decimal> _moneyValue;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<int> _stochasticSlowing;
	private readonly StrategyParam<decimal> _stochasticLevelUp;
	private readonly StrategyParam<decimal> _stochasticLevelDown;
	private readonly StrategyParam<DataType> _signalCandleType;
	private readonly StrategyParam<DataType> _stochasticCandleType;
	private readonly StrategyParam<DataType> _aoCandleType;
	private readonly StrategyParam<int> _aoShortPeriod;
	private readonly StrategyParam<int> _aoLongPeriod;

	private AwesomeOscillator _awesome = null!;
	private StochasticOscillator _stochastic = null!;

	private decimal? _aoPrevious;
	private decimal? _aoPreviousPrevious;
	private decimal? _stochasticLastK;
	private decimal? _stochasticLastD;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	private decimal _pipValue;
	private decimal _stopLossDistance;
	private decimal _takeProfitDistance;
	private decimal _trailingStopDistance;
	private decimal _trailingStepDistance;

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop activation distance in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Extra distance in pips required before trailing stop advancement.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Position sizing approach.
	/// </summary>
	public PositionSizingModes MoneyMode
	{
		get => _moneyMode.Value;
		set => _moneyMode.Value = value;
	}

	/// <summary>
	/// Lot size or risk percentage depending on <see cref="MoneyMode"/>.
	/// </summary>
	public decimal MoneyValue
	{
		get => _moneyValue.Value;
		set => _moneyValue.Value = value;
	}

	/// <summary>
	/// Fixed trading volume used when <see cref="MoneyMode"/> is <see cref="PositionSizingModes.FixedVolume"/>.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stochastic oscillator %K lookback length.
	/// </summary>
	public int StochasticKPeriod
	{
		get => _stochasticKPeriod.Value;
		set => _stochasticKPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic oscillator %D smoothing length.
	/// </summary>
	public int StochasticDPeriod
	{
		get => _stochasticDPeriod.Value;
		set => _stochasticDPeriod.Value = value;
	}

	/// <summary>
	/// Additional smoothing applied to the stochastic calculation.
	/// </summary>
	public int StochasticSlowing
	{
		get => _stochasticSlowing.Value;
		set => _stochasticSlowing.Value = value;
	}

	/// <summary>
	/// Upper stochastic threshold that enables short entries.
	/// </summary>
	public decimal StochasticLevelUp
	{
		get => _stochasticLevelUp.Value;
		set => _stochasticLevelUp.Value = value;
	}

	/// <summary>
	/// Lower stochastic threshold that enables long entries.
	/// </summary>
	public decimal StochasticLevelDown
	{
		get => _stochasticLevelDown.Value;
		set => _stochasticLevelDown.Value = value;
	}

	/// <summary>
	/// Candle type that triggers the main trading logic.
	/// </summary>
	public DataType SignalCandleType
	{
		get => _signalCandleType.Value;
		set => _signalCandleType.Value = value;
	}

	/// <summary>
	/// Candle type used to feed the stochastic oscillator.
	/// </summary>
	public DataType StochasticCandleType
	{
		get => _stochasticCandleType.Value;
		set => _stochasticCandleType.Value = value;
	}

	/// <summary>
	/// Candle type used to calculate the Awesome Oscillator.
	/// </summary>
	public DataType AoCandleType
	{
		get => _aoCandleType.Value;
		set => _aoCandleType.Value = value;
	}

	/// <summary>
	/// Short moving average length inside Awesome Oscillator.
	/// </summary>
	public int AoShortPeriod
	{
		get => _aoShortPeriod.Value;
		set => _aoShortPeriod.Value = value;
	}

	/// <summary>
	/// Long moving average length inside Awesome Oscillator.
	/// </summary>
	public int AoLongPeriod
	{
		get => _aoLongPeriod.Value;
		set => _aoLongPeriod.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="DayTradingPamxaStrategy"/> with defaults aligned to the MQL version.
	/// </summary>
	public DayTradingPamxaStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 50)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop-loss distance in pips", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10, 150, 10);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take-profit distance in pips", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10, 200, 10);

		_trailingStopPips = Param(nameof(TrailingStopPips), 25)
			.SetNotNegative()
			.SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0, 100, 5);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
			.SetNotNegative()
			.SetDisplay("Trailing Step", "Additional pips required before trailing adjusts", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1, 50, 1);

		_moneyMode = Param(nameof(MoneyMode), PositionSizingModes.FixedVolume)
			.SetDisplay("Money Mode", "Choose between fixed volume or risk percentage", "Risk Management");

		_moneyValue = Param(nameof(MoneyValue), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Money Value", "Lot size or risk percentage depending on Money Mode", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);

		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Base trade volume in lots", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 5m, 0.1m);

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %K", "%K calculation length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(3, 21, 1);

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %D", "%D smoothing length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_stochasticSlowing = Param(nameof(StochasticSlowing), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Slow", "Final smoothing applied to stochastic", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_stochasticLevelUp = Param(nameof(StochasticLevelUp), 75m)
			.SetDisplay("Level Up", "Upper stochastic threshold", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(60m, 90m, 5m);

		_stochasticLevelDown = Param(nameof(StochasticLevelDown), 25m)
			.SetDisplay("Level Down", "Lower stochastic threshold", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10m, 40m, 5m);

		_signalCandleType = Param(nameof(SignalCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Signal Candles", "Primary timeframe for trade logic", "General");

		_stochasticCandleType = Param(nameof(StochasticCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Stochastic Candles", "Timeframe feeding the stochastic oscillator", "General");

		_aoCandleType = Param(nameof(AoCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("AO Candles", "Timeframe feeding the Awesome Oscillator", "General");

		_aoShortPeriod = Param(nameof(AoShortPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("AO Fast", "Short period for Awesome Oscillator", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(3, 21, 1);

		_aoLongPeriod = Param(nameof(AoLongPeriod), 34)
			.SetGreaterThanZero()
			.SetDisplay("AO Slow", "Long period for Awesome Oscillator", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(21, 55, 1);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var security = Security;
		if (security == null)
			yield break;

		var requested = new HashSet<DataType>();
		DataType[] series = [SignalCandleType, StochasticCandleType, AoCandleType];
		foreach (var dataType in series)
		{
			if (requested.Add(dataType))
				yield return (security, dataType);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_aoPrevious = null;
		_aoPreviousPrevious = null;
		_stochasticLastK = null;
		_stochasticLastD = null;
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
		_pipValue = 0m;
		_stopLossDistance = 0m;
		_takeProfitDistance = 0m;
		_trailingStopDistance = 0m;
		_trailingStepDistance = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0 && TrailingStepPips <= 0)
			throw new InvalidOperationException("Trailing step must be positive when trailing stop is enabled.");

		Volume = OrderVolume;

		_pipValue = CalculatePipValue();
		_stopLossDistance = StopLossPips * _pipValue;
		_takeProfitDistance = TakeProfitPips * _pipValue;
		_trailingStopDistance = TrailingStopPips * _pipValue;
		_trailingStepDistance = TrailingStepPips * _pipValue;

		_stochastic = new StochasticOscillator
		{
			Length = Math.Max(1, StochasticSlowing),
			KPeriod = StochasticKPeriod,
			DPeriod = StochasticDPeriod
		};

		_awesome = new AwesomeOscillator
		{
			ShortPeriod = AoShortPeriod,
			LongPeriod = AoLongPeriod
		};

		var signalSubscription = SubscribeCandles(SignalCandleType);
		signalSubscription
			.Bind(ProcessSignal)
			.Start();

		var stochasticSubscription = SubscribeCandles(StochasticCandleType);
		stochasticSubscription
			.BindEx(_stochastic, ProcessStochastic)
			.Start();

		var aoSubscription = SubscribeCandles(AoCandleType);
		aoSubscription
			.Bind(_awesome, ProcessAwesome)
			.Start();

		var priceArea = CreateChartArea();
	if (priceArea != null)
		{
			DrawCandles(priceArea, signalSubscription);
			DrawOwnTrades(priceArea);

			var aoArea = CreateChartArea();
			DrawIndicator(aoArea ?? priceArea, _awesome);

			var stochasticArea = CreateChartArea();
			DrawIndicator(stochasticArea ?? priceArea, _stochastic);
		}

		StartProtection();
	}

	private void ProcessAwesome(ICandleMessage candle, decimal aoValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_aoPreviousPrevious = _aoPrevious;
		_aoPrevious = aoValue;
	}

	private void ProcessStochastic(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished || !indicatorValue.IsFinal)
			return;

		var value = (StochasticOscillatorValue)indicatorValue;
		if (value.K is decimal k)
			_stochasticLastK = k;
		if (value.D is decimal d)
			_stochasticLastD = d;
	}

	private void ProcessSignal(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		ApplyTrailing(candle);

		if (CheckExit(candle))
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_aoPreviousPrevious is null || _aoPrevious is null)
			return;
		if (_stochasticLastK is null || _stochasticLastD is null)
			return;

		var bullishAoCross = _aoPreviousPrevious.Value < 0m && _aoPrevious.Value > 0m;
		var bearishAoCross = _aoPreviousPrevious.Value > 0m && _aoPrevious.Value < 0m;

		var stochasticOversold = _stochasticLastK.Value < StochasticLevelDown || _stochasticLastD.Value < StochasticLevelDown;
		var stochasticOverbought = _stochasticLastK.Value > StochasticLevelUp || _stochasticLastD.Value > StochasticLevelUp;

		if (bullishAoCross)
		{
			if (Position < 0m)
			{
				// Close any existing short exposure before switching long.
				var volumeToCover = Math.Abs(Position);
				if (volumeToCover > 0m)
					BuyMarket(volumeToCover);
				ResetTradeState();
			}

			if (Position == 0m && stochasticOversold)
			{
				// Size the new long position according to the selected money management mode.
				var volume = CalculateEntryVolume();
				if (volume > 0m)
				{
					Volume = volume;
					BuyMarket(volume);
					InitializeTradeState(candle.ClosePrice, true);
				}
			}
		}
		else if (bearishAoCross)
		{
			if (Position > 0m)
			{
				// Exit any outstanding long trades before entering short.
				var volumeToCover = Position;
				if (volumeToCover > 0m)
					SellMarket(volumeToCover);
				ResetTradeState();
			}

			if (Position == 0m && stochasticOverbought)
			{
				// Size the new short position respecting the configured risk settings.
				var volume = CalculateEntryVolume();
				if (volume > 0m)
				{
					Volume = volume;
					SellMarket(volume);
					InitializeTradeState(candle.ClosePrice, false);
				}
			}
		}
	}

	private void ApplyTrailing(ICandleMessage candle)
	{
		if (_trailingStopDistance <= 0m)
			return;
		if (_entryPrice is null)
			return;
		if (Position == 0m)
			return;

		if (Position > 0m)
		{
			// Monitor bullish progress and trail the stop once price advances far enough.
			var move = candle.HighPrice - _entryPrice.Value;
			if (move < _trailingStopDistance + _trailingStepDistance)
				return;

			var candidate = candle.HighPrice - _trailingStopDistance;
			if (_stopPrice is not decimal currentStop || candidate - currentStop >= _trailingStepDistance)
				_stopPrice = candidate;
		}
		else if (Position < 0m)
		{
			// Mirror the trailing logic for short positions using the candle low.
			var move = _entryPrice.Value - candle.LowPrice;
			if (move < _trailingStopDistance + _trailingStepDistance)
				return;

			var candidate = candle.LowPrice + _trailingStopDistance;
			if (_stopPrice is not decimal currentStop || currentStop - candidate >= _trailingStepDistance)
				_stopPrice = candidate;
		}
	}

	private bool CheckExit(ICandleMessage candle)
	{
		if (Position == 0m)
		{
			ResetTradeState();
			return false;
		}

		if (_entryPrice is null)
			return false;

		if (Position > 0m)
		{
			// Liquidate longs when the protective stop or target is touched.
			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				var volume = Position;
				if (volume > 0m)
					SellMarket(volume);
				ResetTradeState();
				return true;
			}

			if (_takePrice is decimal take && take > 0m && candle.HighPrice >= take)
			{
				var volume = Position;
				if (volume > 0m)
					SellMarket(volume);
				ResetTradeState();
				return true;
			}
		}
		else if (Position < 0m)
		{
			// Cover shorts when the stop-loss or take-profit levels are breached.
			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				var volume = Math.Abs(Position);
				if (volume > 0m)
					BuyMarket(volume);
				ResetTradeState();
				return true;
			}

			if (_takePrice is decimal take && take > 0m && candle.LowPrice <= take)
			{
				var volume = Math.Abs(Position);
				if (volume > 0m)
					BuyMarket(volume);
				ResetTradeState();
				return true;
			}
		}

		return false;
	}

	private void InitializeTradeState(decimal price, bool isLong)
	{
		// Store entry price and derive static stop/target offsets in price units.
		_entryPrice = price;
		_stopPrice = _stopLossDistance > 0m ? price + (isLong ? -_stopLossDistance : _stopLossDistance) : null;
		_takePrice = _takeProfitDistance > 0m ? price + (isLong ? _takeProfitDistance : -_takeProfitDistance) : null;
	}

	private void ResetTradeState()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
	}

	private decimal CalculateEntryVolume()
	{
		if (MoneyMode == PositionSizingModes.RiskPercent)
		{
			// Convert the configured risk percentage into trade size using the stop distance.
			if (_stopLossDistance <= 0m)
				return OrderVolume;

			var portfolio = Portfolio;
			var portfolioValue = portfolio?.CurrentValue ?? portfolio?.BeginValue ?? 0m;
			if (portfolioValue <= 0m)
				return OrderVolume;

			var riskCapital = portfolioValue * MoneyValue / 100m;
			if (riskCapital <= 0m)
				return OrderVolume;

			var rawVolume = riskCapital / _stopLossDistance;
			var volumeStep = Security?.VolumeStep ?? 1m;
			if (volumeStep <= 0m)
				volumeStep = 1m;

			var rounded = Math.Floor(rawVolume / volumeStep) * volumeStep;
			if (rounded <= 0m)
				rounded = volumeStep;

			return rounded;
		}

		return OrderVolume;
	}

	private decimal CalculatePipValue()
	{
		// Approximate a pip by considering the security step and typical FX decimal formats.
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 1m;

		var decimals = GetDecimalPlaces(step);
		return decimals is 3 or 5 ? step * 10m : step;
	}

	private static int GetDecimalPlaces(decimal value)
	{
		var bits = decimal.GetBits(value);
		return (bits[3] >> 16) & 31;
	}
}

/// <summary>
/// Position sizing modes supported by <see cref="DayTradingPamxaStrategy"/>.
/// </summary>
public enum PositionSizingModes
{
	/// <summary>
	/// Always trade a fixed volume regardless of the stop distance.
	/// </summary>
	FixedVolume,

	/// <summary>
	/// Size the position so the configured percentage of equity is lost if the stop hits.
	/// </summary>
	RiskPercent
}



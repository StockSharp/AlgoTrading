using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Altarius RSI Stochastic strategy converted from the original MQL implementation.
/// Combines two Stochastic oscillators with RSI exits and adaptive position sizing.
/// </summary>
public class AltariusRsiStochasticStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _minimumVolume;
	private readonly StrategyParam<decimal> _maximumRisk;
	private readonly StrategyParam<decimal> _decreaseFactor;
	private readonly StrategyParam<int> _primaryStochasticLength;
	private readonly StrategyParam<int> _primaryStochasticKPeriod;
	private readonly StrategyParam<int> _primaryStochasticDPeriod;
	private readonly StrategyParam<int> _secondaryStochasticLength;
	private readonly StrategyParam<int> _secondaryStochasticKPeriod;
	private readonly StrategyParam<int> _secondaryStochasticDPeriod;
	private readonly StrategyParam<decimal> _differenceThreshold;
	private readonly StrategyParam<decimal> _primaryBuyLimit;
	private readonly StrategyParam<decimal> _primarySellLimit;
	private readonly StrategyParam<decimal> _primaryExitUpper;
	private readonly StrategyParam<decimal> _primaryExitLower;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _longExitRsi;
	private readonly StrategyParam<decimal> _shortExitRsi;

	private decimal _prevPrimarySignal;
	private bool _hasPrevSignal;
	private decimal _entryPrice;
	private int _positionDirection;
	private int _lossStreak;

	/// <summary>
	/// Type of candles used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Base volume used when account information is not available.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Minimum allowed trade volume.
	/// </summary>
	public decimal MinimumVolume
	{
		get => _minimumVolume.Value;
		set => _minimumVolume.Value = value;
	}

	/// <summary>
	/// Risk multiplier used for volume sizing and drawdown exit.
	/// </summary>
	public decimal MaximumRisk
	{
		get => _maximumRisk.Value;
		set => _maximumRisk.Value = value;
	}

	/// <summary>
	/// Factor that reduces volume after consecutive losses.
	/// </summary>
	public decimal DecreaseFactor
	{
		get => _decreaseFactor.Value;
		set => _decreaseFactor.Value = value;
	}

	/// <summary>
	/// Period for the primary Stochastic oscillator.
	/// </summary>
	public int PrimaryStochasticLength
	{
		get => _primaryStochasticLength.Value;
		set => _primaryStochasticLength.Value = value;
	}

	/// <summary>
	/// %K smoothing period for the primary Stochastic oscillator.
	/// </summary>
	public int PrimaryStochasticKPeriod
	{
		get => _primaryStochasticKPeriod.Value;
		set => _primaryStochasticKPeriod.Value = value;
	}

	/// <summary>
	/// %D period for the primary Stochastic oscillator.
	/// </summary>
	public int PrimaryStochasticDPeriod
	{
		get => _primaryStochasticDPeriod.Value;
		set => _primaryStochasticDPeriod.Value = value;
	}

	/// <summary>
	/// Period for the secondary Stochastic oscillator.
	/// </summary>
	public int SecondaryStochasticLength
	{
		get => _secondaryStochasticLength.Value;
		set => _secondaryStochasticLength.Value = value;
	}

	/// <summary>
	/// %K smoothing period for the secondary Stochastic oscillator.
	/// </summary>
	public int SecondaryStochasticKPeriod
	{
		get => _secondaryStochasticKPeriod.Value;
		set => _secondaryStochasticKPeriod.Value = value;
	}

	/// <summary>
	/// %D period for the secondary Stochastic oscillator.
	/// </summary>
	public int SecondaryStochasticDPeriod
	{
		get => _secondaryStochasticDPeriod.Value;
		set => _secondaryStochasticDPeriod.Value = value;
	}

	/// <summary>
	/// Minimum gap between %K and %D on the secondary Stochastic to confirm momentum.
	/// </summary>
	public decimal DifferenceThreshold
	{
		get => _differenceThreshold.Value;
		set => _differenceThreshold.Value = value;
	}

	/// <summary>
	/// Upper bound for primary %K during long entries.
	/// </summary>
	public decimal PrimaryBuyLimit
	{
		get => _primaryBuyLimit.Value;
		set => _primaryBuyLimit.Value = value;
	}

	/// <summary>
	/// Lower bound for primary %K during short entries.
	/// </summary>
	public decimal PrimarySellLimit
	{
		get => _primarySellLimit.Value;
		set => _primarySellLimit.Value = value;
	}

	/// <summary>
	/// Minimum primary %D level to trigger long exits.
	/// </summary>
	public decimal PrimaryExitUpper
	{
		get => _primaryExitUpper.Value;
		set => _primaryExitUpper.Value = value;
	}

	/// <summary>
	/// Maximum primary %D level to trigger short exits.
	/// </summary>
	public decimal PrimaryExitLower
	{
		get => _primaryExitLower.Value;
		set => _primaryExitLower.Value = value;
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
	/// RSI threshold that closes long positions.
	/// </summary>
	public decimal LongExitRsi
	{
		get => _longExitRsi.Value;
		set => _longExitRsi.Value = value;
	}

	/// <summary>
	/// RSI threshold that closes short positions.
	/// </summary>
	public decimal ShortExitRsi
	{
		get => _shortExitRsi.Value;
		set => _shortExitRsi.Value = value;
	}

	/// <summary>
	/// Initialize parameters for the strategy.
	/// </summary>
	public AltariusRsiStochasticStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for calculations", "General");

		_baseVolume = Param(nameof(BaseVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Base Volume", "Fallback volume when portfolio data is unavailable", "Position Sizing");

		_minimumVolume = Param(nameof(MinimumVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Minimum Volume", "Smallest volume allowed for orders", "Position Sizing");

		_maximumRisk = Param(nameof(MaximumRisk), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Factor", "Risk multiplier used for sizing and drawdown control", "Risk");

		_decreaseFactor = Param(nameof(DecreaseFactor), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Decrease Factor", "Divider applied after losing trades", "Risk");

		_primaryStochasticLength = Param(nameof(PrimaryStochasticLength), 15)
			.SetGreaterThanZero()
			.SetDisplay("Primary %K Length", "Lookback for primary Stochastic", "Primary Stochastic");

		_primaryStochasticKPeriod = Param(nameof(PrimaryStochasticKPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Primary %K Smoothing", "Smoothing for primary %K", "Primary Stochastic");

		_primaryStochasticDPeriod = Param(nameof(PrimaryStochasticDPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Primary %D Period", "Signal period for primary Stochastic", "Primary Stochastic");

		_secondaryStochasticLength = Param(nameof(SecondaryStochasticLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Secondary %K Length", "Lookback for secondary Stochastic", "Secondary Stochastic");

		_secondaryStochasticKPeriod = Param(nameof(SecondaryStochasticKPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Secondary %K Smoothing", "Smoothing for secondary %K", "Secondary Stochastic");

		_secondaryStochasticDPeriod = Param(nameof(SecondaryStochasticDPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Secondary %D Period", "Signal period for secondary Stochastic", "Secondary Stochastic");

		_differenceThreshold = Param(nameof(DifferenceThreshold), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Signal Gap", "Minimum gap between %K and %D on the fast Stochastic", "Entries");

		_primaryBuyLimit = Param(nameof(PrimaryBuyLimit), 50m)
			.SetDisplay("Primary Buy Cap", "Primary %K must stay below this level for longs", "Entries");

		_primarySellLimit = Param(nameof(PrimarySellLimit), 55m)
			.SetDisplay("Primary Sell Floor", "Primary %K must stay above this level for shorts", "Entries");

		_primaryExitUpper = Param(nameof(PrimaryExitUpper), 70m)
			.SetDisplay("Long Exit %D", "Primary %D threshold that ends long trades", "Exits");

		_primaryExitLower = Param(nameof(PrimaryExitLower), 30m)
			.SetDisplay("Short Exit %D", "Primary %D threshold that ends short trades", "Exits");

		_rsiPeriod = Param(nameof(RsiPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Lookback for RSI filter", "Indicators");

		_longExitRsi = Param(nameof(LongExitRsi), 60m)
			.SetDisplay("RSI Exit Long", "RSI value that closes long positions", "Exits");

		_shortExitRsi = Param(nameof(ShortExitRsi), 40m)
			.SetDisplay("RSI Exit Short", "RSI value that closes short positions", "Exits");
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
		_prevPrimarySignal = 0m;
		_hasPrevSignal = false;
		_entryPrice = 0m;
		_positionDirection = 0;
		_lossStreak = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var primaryStochastic = new Stochastic
		{
			Length = PrimaryStochasticLength,
			KPeriod = PrimaryStochasticKPeriod,
			DPeriod = PrimaryStochasticDPeriod,
		};

		var secondaryStochastic = new Stochastic
		{
			Length = SecondaryStochasticLength,
			KPeriod = SecondaryStochasticKPeriod,
			DPeriod = SecondaryStochasticDPeriod,
		};

		var rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(primaryStochastic, secondaryStochastic, rsi, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue primaryValue, IIndicatorValue secondaryValue, IIndicatorValue rsiValue)
	{
		// Trade only on finished candles to avoid intrabar noise.
		if (candle.State != CandleStates.Finished)
			return;

		if (!primaryValue.IsFinal || !secondaryValue.IsFinal || !rsiValue.IsFinal)
			return;

		var primary = (StochasticValue)primaryValue;
		var secondary = (StochasticValue)secondaryValue;

		if (primary.K is not decimal primaryMain || primary.D is not decimal primarySignal)
			return;

		if (secondary.K is not decimal secondaryMain || secondary.D is not decimal secondarySignal)
			return;

		var rsi = rsiValue.GetValue<decimal>();
		var difference = Math.Abs(secondaryMain - secondarySignal);

		// Emergency drawdown exit replicates the account-level risk guard from MQL.
		if (Position != 0)
		{
			var accountValue = Portfolio?.CurrentValue ?? 0m;
			var riskLimit = accountValue * MaximumRisk;
			if (PnL < 0m && riskLimit > 0m && Math.Abs(PnL) >= riskLimit)
			{
				ClosePosition(candle.ClosePrice);
				UpdatePrimarySignal(primarySignal);
				return;
			}
		}

	var canTrade = IsFormedAndOnlineAndAllowTrading();

	if (Position == 0)
	{
		if (!canTrade)
		{
			UpdatePrimarySignal(primarySignal);
			return;
		}

		var bullishSetup = primaryMain > primarySignal && primaryMain < PrimaryBuyLimit && difference > DifferenceThreshold;
		var bearishSetup = primaryMain < primarySignal && primaryMain > PrimarySellLimit && difference > DifferenceThreshold;

		if (bullishSetup)
		{
			var volume = CalculateTradeVolume();
			if (volume > 0m)
			{
				BuyMarket(volume);
				_entryPrice = candle.ClosePrice;
				_positionDirection = 1;
			}
		}
		else if (bearishSetup)
		{
			var volume = CalculateTradeVolume();
			if (volume > 0m)
			{
				SellMarket(volume);
				_entryPrice = candle.ClosePrice;
				_positionDirection = -1;
			}
		}
	}
	else if (canTrade)
	{
		if (Position > 0)
		{
			var exitSignal = rsi > LongExitRsi && _hasPrevSignal && primarySignal < _prevPrimarySignal && primarySignal > PrimaryExitUpper;
			if (exitSignal)
				ClosePosition(candle.ClosePrice);
		}
		else if (Position < 0)
		{
			var exitSignal = rsi < ShortExitRsi && _hasPrevSignal && primarySignal > _prevPrimarySignal && primarySignal < PrimaryExitLower;
			if (exitSignal)
				ClosePosition(candle.ClosePrice);
		}
	}

	UpdatePrimarySignal(primarySignal);
	}

	private decimal CalculateTradeVolume()
	{
		var volume = BaseVolume;
		var accountValue = Portfolio?.CurrentValue;

		// Derive lot size from account equity similar to the original MQL logic.
		if (accountValue is decimal value && value > 0m)
		{
			var riskVolume = Math.Round(value * MaximumRisk / 1000m, 2, MidpointRounding.AwayFromZero);
			if (riskVolume > 0m)
				volume = riskVolume;
		}

		if (DecreaseFactor > 0m && _lossStreak > 1)
		{
			var reduction = volume * _lossStreak / DecreaseFactor;
			volume = Math.Max(volume - reduction, MinimumVolume);
		}

		if (volume < MinimumVolume)
			volume = MinimumVolume;

		return volume;
	}

	private void ClosePosition(decimal exitPrice)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
		{
			_positionDirection = 0;
			_entryPrice = 0m;
			return;
		}

		var direction = _positionDirection;
		var entryPrice = _entryPrice;

		if (Position > 0)
			SellMarket(volume);
		else
			BuyMarket(volume);

		if (entryPrice > 0m)
		{
			if (direction > 0)
			{
				var profit = exitPrice - entryPrice;
				if (profit < 0m)
					_lossStreak++;
				else if (profit > 0m)
					_lossStreak = 0;
			}
			else if (direction < 0)
			{
				var profit = entryPrice - exitPrice;
				if (profit < 0m)
					_lossStreak++;
				else if (profit > 0m)
					_lossStreak = 0;
			}
		}

		_entryPrice = 0m;
		_positionDirection = 0;
	}

	private void UpdatePrimarySignal(decimal signal)
	{
		_prevPrimarySignal = signal;
		_hasPrevSignal = true;
	}
}

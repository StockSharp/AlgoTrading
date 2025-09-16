using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Vortex oscillator trading system ported from the MetaTrader implementation.
/// Opens long positions when the oscillator drops below a configured level and
/// shorts when the oscillator rises above the upper threshold.
/// Optional stop-loss and take-profit rules monitor the oscillator value to exit positions.
/// </summary>
public class VortexOscillatorSystemStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _useBuyStopLoss;
	private readonly StrategyParam<bool> _useBuyTakeProfit;
	private readonly StrategyParam<bool> _useSellStopLoss;
	private readonly StrategyParam<bool> _useSellTakeProfit;
	private readonly StrategyParam<decimal> _buyThreshold;
	private readonly StrategyParam<decimal> _buyStopLossLevel;
	private readonly StrategyParam<decimal> _buyTakeProfitLevel;
	private readonly StrategyParam<decimal> _sellThreshold;
	private readonly StrategyParam<decimal> _sellStopLossLevel;
	private readonly StrategyParam<decimal> _sellTakeProfitLevel;

	private VortexIndicator _vortexIndicator = null!;

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public VortexOscillatorSystemStrategy()
	{
		_length = Param(nameof(Length), 14)
			.SetGreaterThanZero()
			.SetDisplay("Vortex Length", "Period used for the Vortex indicator.", "General")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 7);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used to build candles for calculations.", "General");

		_useBuyStopLoss = Param(nameof(UseBuyStopLoss), false)
			.SetDisplay("Use Buy Stop Loss", "Enable oscillator-based stop loss for long positions.", "Risk Management");

		_useBuyTakeProfit = Param(nameof(UseBuyTakeProfit), false)
			.SetDisplay("Use Buy Take Profit", "Enable oscillator-based take profit for long positions.", "Risk Management");

		_useSellStopLoss = Param(nameof(UseSellStopLoss), false)
			.SetDisplay("Use Sell Stop Loss", "Enable oscillator-based stop loss for short positions.", "Risk Management");

		_useSellTakeProfit = Param(nameof(UseSellTakeProfit), false)
			.SetDisplay("Use Sell Take Profit", "Enable oscillator-based take profit for short positions.", "Risk Management");

		_buyThreshold = Param(nameof(BuyThreshold), -0.75m)
			.SetDisplay("Buy Threshold", "Oscillator value that triggers a long setup.", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(-1.5m, -0.25m, 0.25m);

		_buyStopLossLevel = Param(nameof(BuyStopLossLevel), -1m)
			.SetDisplay("Buy Stop Loss Level", "Oscillator value that closes long trades when stop loss is enabled.", "Signals");

		_buyTakeProfitLevel = Param(nameof(BuyTakeProfitLevel), 0m)
			.SetDisplay("Buy Take Profit Level", "Oscillator value that closes long trades when take profit is enabled.", "Signals");

		_sellThreshold = Param(nameof(SellThreshold), 0.75m)
			.SetDisplay("Sell Threshold", "Oscillator value that triggers a short setup.", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(0.25m, 1.5m, 0.25m);

		_sellStopLossLevel = Param(nameof(SellStopLossLevel), 1m)
			.SetDisplay("Sell Stop Loss Level", "Oscillator value that closes short trades when stop loss is enabled.", "Signals");

		_sellTakeProfitLevel = Param(nameof(SellTakeProfitLevel), 0m)
			.SetDisplay("Sell Take Profit Level", "Oscillator value that closes short trades when take profit is enabled.", "Signals");

		Volume = 0.1m;
	}

	/// <summary>
	/// Vortex indicator lookback length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Enable oscillator-based stop loss for long positions.
	/// </summary>
	public bool UseBuyStopLoss
	{
		get => _useBuyStopLoss.Value;
		set => _useBuyStopLoss.Value = value;
	}

	/// <summary>
	/// Enable oscillator-based take profit for long positions.
	/// </summary>
	public bool UseBuyTakeProfit
	{
		get => _useBuyTakeProfit.Value;
		set => _useBuyTakeProfit.Value = value;
	}

	/// <summary>
	/// Enable oscillator-based stop loss for short positions.
	/// </summary>
	public bool UseSellStopLoss
	{
		get => _useSellStopLoss.Value;
		set => _useSellStopLoss.Value = value;
	}

	/// <summary>
	/// Enable oscillator-based take profit for short positions.
	/// </summary>
	public bool UseSellTakeProfit
	{
		get => _useSellTakeProfit.Value;
		set => _useSellTakeProfit.Value = value;
	}

	/// <summary>
	/// Oscillator level used to open long trades.
	/// </summary>
	public decimal BuyThreshold
	{
		get => _buyThreshold.Value;
		set => _buyThreshold.Value = value;
	}

	/// <summary>
	/// Oscillator level that closes long trades when stop loss is enabled.
	/// </summary>
	public decimal BuyStopLossLevel
	{
		get => _buyStopLossLevel.Value;
		set => _buyStopLossLevel.Value = value;
	}

	/// <summary>
	/// Oscillator level that closes long trades when take profit is enabled.
	/// </summary>
	public decimal BuyTakeProfitLevel
	{
		get => _buyTakeProfitLevel.Value;
		set => _buyTakeProfitLevel.Value = value;
	}

	/// <summary>
	/// Oscillator level used to open short trades.
	/// </summary>
	public decimal SellThreshold
	{
		get => _sellThreshold.Value;
		set => _sellThreshold.Value = value;
	}

	/// <summary>
	/// Oscillator level that closes short trades when stop loss is enabled.
	/// </summary>
	public decimal SellStopLossLevel
	{
		get => _sellStopLossLevel.Value;
		set => _sellStopLossLevel.Value = value;
	}

	/// <summary>
	/// Oscillator level that closes short trades when take profit is enabled.
	/// </summary>
	public decimal SellTakeProfitLevel
	{
		get => _sellTakeProfitLevel.Value;
		set => _sellTakeProfitLevel.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_vortexIndicator = new VortexIndicator
		{
			Length = Length,
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_vortexIndicator, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal viPlus, decimal viMinus)
	{
		// Process only finished candles to mirror bar-close logic from the original script.
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure the strategy is ready for trading and the indicator has accumulated enough data.
		if (!IsFormedAndOnlineAndAllowTrading() || !_vortexIndicator.IsFormed)
			return;

		// Vortex oscillator equals the difference between VI+ and VI- lines.
		var oscillator = viPlus - viMinus;

		var longSetupExists = false;
		var shortSetupExists = false;

		// Long setups are considered when the oscillator falls below the buy threshold.
		if (UseBuyStopLoss)
		{
			if (oscillator <= BuyThreshold && oscillator > BuyStopLossLevel)
			{
				longSetupExists = true;
				shortSetupExists = false;
			}
		}
		else if (oscillator <= BuyThreshold)
		{
			longSetupExists = true;
			shortSetupExists = false;
		}

		// Short setups require the oscillator to rise above the sell threshold.
		if (UseSellStopLoss)
		{
			if (oscillator >= SellThreshold && oscillator < SellStopLossLevel)
			{
				shortSetupExists = true;
				longSetupExists = false;
			}
		}
		else if (oscillator >= SellThreshold)
		{
			shortSetupExists = true;
			longSetupExists = false;
		}

		// Neutral zone cancels both long and short intentions.
		if (oscillator >= BuyThreshold && oscillator <= SellThreshold)
		{
			longSetupExists = false;
			shortSetupExists = false;
		}

		var currentPosition = Position;

		if (longSetupExists && currentPosition <= 0)
		{
			var volumeToBuy = Volume + Math.Abs(currentPosition);
			// Close existing shorts and open a long position when a valid long setup appears.
			BuyMarket(volumeToBuy);
		}
		else if (shortSetupExists && currentPosition >= 0)
		{
			var volumeToSell = Volume + Math.Abs(currentPosition);
			// Close existing longs and open a short position when a valid short setup appears.
			SellMarket(volumeToSell);
		}

		currentPosition = Position;

		if (currentPosition > 0)
		{
			// Manage long positions with oscillator-based stops and targets.
			if (UseBuyStopLoss && oscillator <= BuyStopLossLevel)
			{
				SellMarket(currentPosition);
				return;
			}

			if (UseBuyTakeProfit && oscillator >= BuyTakeProfitLevel)
			{
				SellMarket(currentPosition);
				return;
			}
		}
		else if (currentPosition < 0)
		{
			var absPosition = Math.Abs(currentPosition);
			// Manage short positions with oscillator-based stops and targets.
			if (UseSellStopLoss && oscillator >= SellStopLossLevel)
			{
				BuyMarket(absPosition);
				return;
			}

			if (UseSellTakeProfit && oscillator <= SellTakeProfitLevel)
			{
				BuyMarket(absPosition);
			}
		}
	}
}

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD and Parabolic SAR trend-following strategy with configurable comparison switches.
/// Opens new positions when indicator conditions align and limits the total number of stacked entries.
/// </summary>
public class MacdAndSarStrategy : Strategy
{
	private const decimal VolumeTolerance = 0.0000001m;

	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMaximum;
	private readonly StrategyParam<bool> _buyMacdGreaterSignal;
	private readonly StrategyParam<bool> _buySignalPositive;
	private readonly StrategyParam<bool> _buySarAbovePrice;
	private readonly StrategyParam<bool> _sellMacdGreaterSignal;
	private readonly StrategyParam<bool> _sellSignalPositive;
	private readonly StrategyParam<bool> _sellSarAbovePrice;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private ParabolicSar _parabolicSar = null!;

	/// <summary>
	/// Volume per individual trade.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneously stacked positions.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Fast period for MACD calculation.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow period for MACD calculation.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal smoothing period for MACD.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Parabolic SAR acceleration step.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	/// <summary>
	/// Parabolic SAR maximum acceleration.
	/// </summary>
	public decimal SarMaximum
	{
		get => _sarMaximum.Value;
		set => _sarMaximum.Value = value;
	}

	/// <summary>
	/// Require MACD main line to be greater than the signal line for buy entries.
	/// </summary>
	public bool BuyMacdGreaterSignal
	{
		get => _buyMacdGreaterSignal.Value;
		set => _buyMacdGreaterSignal.Value = value;
	}

	/// <summary>
	/// Require MACD signal line to be positive for buy entries.
	/// </summary>
	public bool BuySignalPositive
	{
		get => _buySignalPositive.Value;
		set => _buySignalPositive.Value = value;
	}

	/// <summary>
	/// Require Parabolic SAR value to stay above price for buy entries.
	/// When false the strategy expects price to be above the SAR level.
	/// </summary>
	public bool BuySarAbovePrice
	{
		get => _buySarAbovePrice.Value;
		set => _buySarAbovePrice.Value = value;
	}

	/// <summary>
	/// Require MACD main line to be greater than the signal line for sell entries.
	/// </summary>
	public bool SellMacdGreaterSignal
	{
		get => _sellMacdGreaterSignal.Value;
		set => _sellMacdGreaterSignal.Value = value;
	}

	/// <summary>
	/// Require MACD signal line to be positive for sell entries.
	/// </summary>
	public bool SellSignalPositive
	{
		get => _sellSignalPositive.Value;
		set => _sellSignalPositive.Value = value;
	}

	/// <summary>
	/// Require Parabolic SAR value to stay above price for sell entries.
	/// When false the strategy expects price to be below the SAR level.
	/// </summary>
	public bool SellSarAbovePrice
	{
		get => _sellSarAbovePrice.Value;
		set => _sellSarAbovePrice.Value = value;
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
	/// Initializes a new instance of the <see cref="MacdAndSarStrategy"/> class.
	/// </summary>
	public MacdAndSarStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Volume per trade", "Trading");

		_maxPositions = Param(nameof(MaxPositions), 10)
			.SetGreaterThanZero()
			.SetDisplay("Max Positions", "Maximum stacked entries", "Risk");

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(6, 18, 2);

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 40, 2);

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal smoothing length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_sarStep = Param(nameof(SarStep), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Step", "Parabolic SAR acceleration", "Indicators");

		_sarMaximum = Param(nameof(SarMaximum), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Max", "Maximum SAR acceleration", "Indicators");

		_buyMacdGreaterSignal = Param(nameof(BuyMacdGreaterSignal), true)
			.SetDisplay("Buy MACD > Signal", "Require MACD above signal", "Signals");

		_buySignalPositive = Param(nameof(BuySignalPositive), false)
			.SetDisplay("Buy Signal > 0", "Require MACD signal above zero", "Signals");

		_buySarAbovePrice = Param(nameof(BuySarAbovePrice), false)
			.SetDisplay("Buy SAR Above Price", "Require SAR above price for longs", "Signals");

		_sellMacdGreaterSignal = Param(nameof(SellMacdGreaterSignal), false)
			.SetDisplay("Sell MACD > Signal", "Require MACD above signal for shorts", "Signals");

		_sellSignalPositive = Param(nameof(SellSignalPositive), true)
			.SetDisplay("Sell Signal > 0", "Require MACD signal above zero for shorts", "Signals");

		_sellSarAbovePrice = Param(nameof(SellSarAbovePrice), true)
			.SetDisplay("Sell SAR Above Price", "Require SAR above price for shorts", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candles used for analysis", "General");
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

		_macd = null!;
		_parabolicSar = null!;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Fast = MacdFastPeriod,
			Slow = MacdSlowPeriod,
			Signal = MacdSignalPeriod
		};

		_parabolicSar = new ParabolicSar
		{
			Acceleration = SarStep,
			AccelerationMax = SarMaximum
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_macd, _parabolicSar, ProcessCandle)
			.Start();

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, subscription);
			DrawIndicator(priceArea, _parabolicSar);
			DrawOwnTrades(priceArea);

			var macdArea = CreateChartArea("MACD");
			if (macdArea != null)
			{
				DrawIndicator(macdArea, _macd);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal macdValue, decimal signalValue, decimal histogramValue, decimal sarValue)
	{
		// Process only finished candles to mirror the original bar-based logic.
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure indicators are fully formed before trading.
		if (!_macd.IsFormed || !_parabolicSar.IsFormed)
			return;

		// Skip trading when strategy is not ready (no connection, warm-up, etc.).
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price = candle.ClosePrice;

		// Evaluate configurable buy conditions.
		var buyMacdCondition = BuyMacdGreaterSignal ? macdValue > signalValue : macdValue < signalValue;
		var buySignalCondition = BuySignalPositive ? signalValue > 0m : signalValue < 0m;
		var buySarCondition = BuySarAbovePrice ? sarValue > price : sarValue < price;

		// Evaluate configurable sell conditions.
		var sellMacdCondition = SellMacdGreaterSignal ? macdValue > signalValue : macdValue < signalValue;
		var sellSignalCondition = SellSignalPositive ? signalValue > 0m : signalValue < 0m;
		var sellSarCondition = SellSarAbovePrice ? sarValue > price : sarValue < price;

		var openBuy = buyMacdCondition && buySignalCondition && buySarCondition;
		var openSell = sellMacdCondition && sellSignalCondition && sellSarCondition;

		if (TradeVolume <= 0m)
			return;

		var maxPositionVolume = MaxPositions * TradeVolume;
		if (maxPositionVolume <= 0m)
			return;

		var absPosition = Math.Abs(Position);

		if (openBuy)
		{
			var hasShortPosition = Position < 0m;
			var hasCapacity = Position >= 0m && absPosition + TradeVolume <= maxPositionVolume + VolumeTolerance;

			if (hasShortPosition || hasCapacity)
			{
				var volume = TradeVolume + (hasShortPosition ? absPosition : 0m);
				if (volume > 0m)
				{
					// Offset existing shorts and stack an additional long entry.
					BuyMarket(volume);
				}
			}
		}
		else if (openSell)
		{
			var hasLongPosition = Position > 0m;
			var hasCapacity = Position <= 0m && absPosition + TradeVolume <= maxPositionVolume + VolumeTolerance;

			if (hasLongPosition || hasCapacity)
			{
				var volume = TradeVolume + (hasLongPosition ? absPosition : 0m);
				if (volume > 0m)
				{
					// Offset existing longs and stack an additional short entry.
					SellMarket(volume);
				}
			}
		}
	}
}

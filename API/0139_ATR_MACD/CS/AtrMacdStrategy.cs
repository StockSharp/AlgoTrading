using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that uses ATR (Average True Range) for volatility detection
/// and MACD for trend direction confirmation.
/// Enters positions when volatility increases and MACD confirms trend direction.
/// </summary>
public class AtrMacdStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _atrAvgPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<decimal> _stopLossAtr;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _atrDeltaPercent;

	private decimal _prevAtrAvg;
	private decimal _prevAtr;
	private SimpleMovingAverage _atrAvg;
	private int _barsSinceLastTrade;

	/// <summary>
	/// ATR indicator period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Period for averaging ATR values.
	/// </summary>
	public int AtrAvgPeriod
	{
		get => _atrAvgPeriod.Value;
		set => _atrAvgPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier for ATR comparison.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// MACD fast period.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// MACD slow period.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// MACD signal period.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// Stop loss in ATR multiples.
	/// </summary>
	public decimal StopLossAtr
	{
		get => _stopLossAtr.Value;
		set => _stopLossAtr.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Minimum percentage increase in ATR relative to the previous value.
	/// </summary>
	public decimal AtrDeltaPercent
	{
		get => _atrDeltaPercent.Value;
		set => _atrDeltaPercent.Value = value;
	}

	/// <summary>
	/// Strategy constructor.
	/// </summary>
	public AtrMacdStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR indicator period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 21, 7);

		_atrAvgPeriod = Param(nameof(AtrAvgPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("ATR Avg Period", "ATR average period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 5);

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR comparison multiplier", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1.0m, 2.0m, 0.1m);

		_macdFast = Param(nameof(MacdFast), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "MACD fast period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(8, 16, 4);

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "MACD slow period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 32, 4);

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "MACD signal period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 13, 4);

		_stopLossAtr = Param(nameof(StopLossAtr), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss ATR", "Stop loss as ATR multiplier", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1.0m, 3.0m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_atrDeltaPercent = Param(nameof(AtrDeltaPercent), 10.0m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Delta %", "Minimum ATR increase percent compared to previous value", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5.0m, 20.0m, 1.0m);
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

		// Initialize variables
		_prevAtrAvg = 0;
		_prevAtr = 0;
		_barsSinceLastTrade = 1000;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create indicators
		var atr = new AverageTrueRange
		{
			Length = AtrPeriod
		};

		_atrAvg = new SimpleMovingAverage
		{
			Length = AtrAvgPeriod
		};

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow },
			},
			SignalMa = { Length = MacdSignal }
		};
		// Create subscription and bind indicators
		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(atr, macd, ProcessIndicators)
			.Start();

		// Setup position protection
		StartProtection(
			takeProfit: new Unit(0, UnitTypes.Absolute), // No take profit
			stopLoss: new Unit(StopLossAtr, UnitTypes.Absolute) // Stop loss as ATR multiplier
		);

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, atr);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	/// <summary>
	/// Process MACD indicator values.
	/// </summary>
	private void ProcessIndicators(ICandleMessage candle, IIndicatorValue atrValue, IIndicatorValue macdValue)
	{
		if (!atrValue.IsFinal)
			return;

		// Process ATR through averaging indicator
		var currentAtr = atrValue.ToDecimal();
		var avgValue = _atrAvg.Process(atrValue);
		if (!avgValue.IsFinal)
			return;

		// Store current ATR average value
		var currentAtrAvg = avgValue.ToDecimal();
		_prevAtrAvg = currentAtrAvg;

		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading() || _prevAtrAvg == 0)
			return;

		_barsSinceLastTrade++;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macdDec = macdTyped.Macd;
		var signalValue = macdTyped.Signal;

		// ATR must be greater than average * multiplier AND greater than previous ATR by AtrDeltaPercent
		var atrDelta = _prevAtr == 0 ? 0 : (currentAtr - _prevAtr) / _prevAtr * 100m;
		var isVolatilityIncreasing =
			currentAtr > currentAtrAvg * AtrMultiplier &&
			(_prevAtr != 0 && currentAtr > _prevAtr * (1m + AtrDeltaPercent / 100m));
		LogInfo($"ATR: {currentAtr:F4}, PrevATR: {_prevAtr:F4}, ATRAvg: {currentAtrAvg:F4}, ATRDelta: {atrDelta:F2}%, isVolatilityIncreasing: {isVolatilityIncreasing}, BarsSinceLastTrade: {_barsSinceLastTrade}");

		_prevAtr = currentAtr;
		_prevAtrAvg = currentAtrAvg;

		const int barsBetweenTrades = 300;

		if (isVolatilityIncreasing && _barsSinceLastTrade >= barsBetweenTrades)
		{
			// Long entry: MACD above Signal in rising volatility
			if (macdDec > signalValue && Position <= 0)
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
				LogInfo($"Buy: MACD {macdDec:F4} > Signal {signalValue:F4}");
				_barsSinceLastTrade = 0;
			}
			// Short entry: MACD below Signal in rising volatility
			else if (macdDec < signalValue && Position >= 0)
			{
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
				LogInfo($"Sell: MACD {macdDec:F4} < Signal {signalValue:F4}");
				_barsSinceLastTrade = 0;
			}
		}

		// Exit conditions based on MACD crossovers
		if (Position > 0 && macdDec < signalValue)
		{
			SellMarket(Position);
			LogInfo($"Exit Long: MACD {macdDec:F4} < Signal {signalValue:F4}");
			_barsSinceLastTrade = 0;
		}
		else if (Position < 0 && macdDec > signalValue)
		{
			BuyMarket(Math.Abs(Position));
			LogInfo($"Exit Short: MACD {macdDec:F4} > Signal {signalValue:F4}");
			_barsSinceLastTrade = 0;
		}
	}
}
using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that enters when a candle opens at its high or low and exits on Parabolic SAR reversal.
/// </summary>
public class CandelsHighOpenStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<decimal> _stopLevel;
	private readonly StrategyParam<decimal> _takeLevel;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMax;

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Reverse entry signals.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Stop loss level in absolute price.
	/// </summary>
	public decimal StopLevel
	{
		get => _stopLevel.Value;
		set => _stopLevel.Value = value;
	}

	/// <summary>
	/// Take profit level in absolute price.
	/// </summary>
	public decimal TakeLevel
	{
		get => _takeLevel.Value;
		set => _takeLevel.Value = value;
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
	public decimal SarMax
	{
		get => _sarMax.Value;
		set => _sarMax.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public CandelsHighOpenStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for processing", "General")
			.SetCanOptimize(true);

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse Signals", "Invert long and short signals", "General")
			.SetCanOptimize(true);

		_stopLevel = Param(nameof(StopLevel), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Level", "Absolute stop loss distance", "Protection")
			.SetCanOptimize(true);

		_takeLevel = Param(nameof(TakeLevel), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Take Level", "Absolute take profit distance", "Protection")
			.SetCanOptimize(true);

		_sarStep = Param(nameof(SarStep), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Step", "Acceleration factor step for Parabolic SAR", "Indicators")
			.SetCanOptimize(true);

		_sarMax = Param(nameof(SarMax), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Max", "Maximum acceleration factor for Parabolic SAR", "Indicators")
			.SetCanOptimize(true);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var psar = new ParabolicSar
		{
			Acceleration = SarStep,
			AccelerationMax = SarMax,
			AccelerationStep = SarStep
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(psar, ProcessCandle)
			.Start();

		StartProtection(
			stopLoss: new Unit(StopLevel, UnitTypes.Absolute),
			takeProfit: new Unit(TakeLevel, UnitTypes.Absolute));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, psar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal psarValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Exit conditions based on Parabolic SAR reversal
		if (Position > 0 && candle.ClosePrice < psarValue)
		{
			SellMarket(Math.Abs(Position));
			return;
		}
		if (Position < 0 && candle.ClosePrice > psarValue)
		{
			BuyMarket(Math.Abs(Position));
			return;
		}

		var openAtHigh = candle.OpenPrice == candle.HighPrice;
		var openAtLow = candle.OpenPrice == candle.LowPrice;

		if (ReverseSignals)
		{
			var tmp = openAtHigh;
			openAtHigh = openAtLow;
			openAtLow = tmp;
		}

		var volume = Volume + Math.Abs(Position);

		if (openAtLow && Position <= 0)
		{
			BuyMarket(volume);
		}
		else if (openAtHigh && Position >= 0)
		{
			SellMarket(volume);
		}
	}
}

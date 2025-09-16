using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Jurik moving average slope reversals.
/// Generates signals when the JMA line changes direction.
/// </summary>
public class ColorJVariationStrategy : Strategy
{
	private readonly StrategyParam<int> _jmaPeriod;
	private readonly StrategyParam<int> _jmaPhase;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;

	private decimal? _prevJma;
	private decimal? _prevPrevJma;

	/// <summary>
	/// JMA averaging period.
	/// </summary>
	public int JmaPeriod
	{
		get => _jmaPeriod.Value;
		set => _jmaPeriod.Value = value;
	}

	/// <summary>
	/// JMA phase from -100 to +100.
	/// </summary>
	public int JmaPhase
	{
		get => _jmaPhase.Value;
		set => _jmaPhase.Value = value;
	}

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Absolute stop loss in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Absolute take profit in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	public ColorJVariationStrategy()
	{
		_jmaPeriod = Param(nameof(JmaPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("JMA Period", "JMA averaging period", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 50, 5);

		_jmaPhase = Param(nameof(JmaPhase), 100)
			.SetDisplay("JMA Phase", "Phase for JMA", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis", "Parameters");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Absolute stop loss", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(100m, 2000m, 100m);

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Absolute take profit", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(200m, 4000m, 100m);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var jma = new JurikMovingAverage
		{
			Length = JmaPeriod,
			Phase = JmaPhase
		};

		var candleSub = SubscribeCandles(CandleType);
		candleSub
			.Bind(jma, ProcessCandle)
			.Start();

		StartProtection(
			stopLoss: new Unit(StopLoss, UnitTypes.Price),
			takeProfit: new Unit(TakeProfit, UnitTypes.Price));
	}

	private void ProcessCandle(ICandleMessage candle, decimal jmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevJma is decimal prev && _prevPrevJma is decimal prev2)
		{
			var wasDown = prev < prev2;
			var turnedUp = jmaValue > prev;

			var wasUp = prev > prev2;
			var turnedDown = jmaValue < prev;

			if (wasDown && turnedUp && Position <= 0)
				BuyMarket();
			else if (wasUp && turnedDown && Position >= 0)
				SellMarket();
		}

		_prevPrevJma = _prevJma;
		_prevJma = jmaValue;
	}
}

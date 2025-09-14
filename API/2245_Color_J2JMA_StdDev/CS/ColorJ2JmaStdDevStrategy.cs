using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the slope of Jurik moving average and its standard deviation.
/// Opens a long position when the JMA slope rises above the high threshold.
/// Opens a short position when the JMA slope falls below the negative high threshold.
/// Existing positions are closed when the slope crosses the opposite low threshold.
/// </summary>
public class ColorJ2JmaStdDevStrategy : Strategy
{
	private readonly StrategyParam<int> _jmaLength;
	private readonly StrategyParam<int> _stdDevPeriod;
	private readonly StrategyParam<decimal> _k1;
	private readonly StrategyParam<decimal> _k2;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;

	private decimal? _prevJma;
	private readonly JurikMovingAverage _jma;
	private readonly StandardDeviation _stdDev;

	/// <summary>
	/// Jurik moving average period.
	/// </summary>
	public int JmaLength
	{
		get => _jmaLength.Value;
		set => _jmaLength.Value = value;
	}

	/// <summary>
	/// Period for standard deviation of JMA differences.
	/// </summary>
	public int StdDevPeriod
	{
		get => _stdDevPeriod.Value;
		set => _stdDevPeriod.Value = value;
	}

	/// <summary>
	/// First multiplier for standard deviation threshold.
	/// </summary>
	public decimal K1
	{
		get => _k1.Value;
		set => _k1.Value = value;
	}

	/// <summary>
	/// Second multiplier for strong signals.
	/// </summary>
	public decimal K2
	{
		get => _k2.Value;
		set => _k2.Value = value;
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
	/// Stop loss in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public ColorJ2JmaStdDevStrategy()
	{
		_jmaLength = Param(nameof(JmaLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("JMA Length", "Period of JMA", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(3, 20, 1);

		_stdDevPeriod = Param(nameof(StdDevPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("StdDev Period", "Period of standard deviation", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_k1 = Param(nameof(K1), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("K1", "First threshold multiplier", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_k2 = Param(nameof(K2), 2.5m)
			.SetGreaterThanZero()
			.SetDisplay("K2", "Second threshold multiplier", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(2m, 5m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "Parameters");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(500m, 2000m, 500m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1000m, 4000m, 500m);

		_jma = new JurikMovingAverage();
		_stdDev = new StandardDeviation();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_jma.Length = JmaLength;
		_stdDev.Length = StdDevPeriod;

		SubscribeCandles(CandleType)
			.Bind(_jma, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPoints, UnitTypes.Point),
			stopLoss: new Unit(StopLossPoints, UnitTypes.Point));
	}

	private void ProcessCandle(ICandleMessage candle, decimal jmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevJma is not decimal prev)
		{
			_prevJma = jmaValue;
			return;
		}

		var diff = jmaValue - prev;
		_prevJma = jmaValue;

		var stdValue = _stdDev.Process(candle, diff);
		if (!stdValue.IsFinal)
			return;

		var stDev = stdValue.GetValue<decimal>();
		var lowThreshold = K1 * stDev;
		var highThreshold = K2 * stDev;

		// Close existing long when slope turns strongly down
		if (Position > 0 && diff < -lowThreshold)
		{
			SellMarket();
			return;
		}

		// Close existing short when slope turns strongly up
		if (Position < 0 && diff > lowThreshold)
		{
			BuyMarket();
			return;
		}

		// Open new long on strong positive slope
		if (Position <= 0 && diff > highThreshold)
		{
			BuyMarket();
		}
		// Open new short on strong negative slope
		else if (Position >= 0 && diff < -highThreshold)
		{
			SellMarket();
		}
	}
}

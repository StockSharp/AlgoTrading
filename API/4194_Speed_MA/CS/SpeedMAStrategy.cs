namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Speed moving average strategy converted from the MetaTrader expert "ytg_Speed_MA_ea".
/// Measures the slope of a shifted simple moving average and opens trades when the change exceeds a threshold.
/// </summary>
public class SpeedMAStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _movingAveragePeriod;
	private readonly StrategyParam<int> _shift;
	private readonly StrategyParam<decimal> _slopeThresholdPoints;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _movingAverage;
	private readonly List<decimal> _maHistory = new();
	private decimal _pointValue;

	/// <summary>
	/// Initializes a new instance of the <see cref="SpeedMAStrategy"/> class.
	/// </summary>
	public SpeedMAStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order volume", "Base volume used for market entries.", "Trading");

		_movingAveragePeriod = Param(nameof(MovingAveragePeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("MA period", "Length of the simple moving average.", "Indicator")
			.SetCanOptimize(true);

		_shift = Param(nameof(Shift), 1)
			.SetNotNegative()
			.SetDisplay("MA shift", "Number of completed bars used as the moving average offset.", "Indicator");

		_slopeThresholdPoints = Param(nameof(SlopeThresholdPoints), 10m)
			.SetNotNegative()
			.SetDisplay("Slope threshold (points)", "Minimum difference between shifted MA values required to trigger a signal.", "Logic")
			.SetCanOptimize(true);

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse signals", "Invert generated buy and sell directions.", "Logic");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 500m)
			.SetNotNegative()
			.SetDisplay("Take profit (points)", "Distance to the profit target expressed in points.", "Risk")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 490m)
			.SetNotNegative()
			.SetDisplay("Stop loss (points)", "Distance to the protective stop expressed in points.", "Risk")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Primary timeframe used for signal generation.", "General");
	}

	/// <summary>
	/// Trade volume used for new orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Length of the simple moving average.
	/// </summary>
	public int MovingAveragePeriod
	{
		get => _movingAveragePeriod.Value;
		set => _movingAveragePeriod.Value = value;
	}

	/// <summary>
	/// Shift applied to the moving average when evaluating the slope.
	/// </summary>
	public int Shift
	{
		get => _shift.Value;
		set => _shift.Value = value;
	}

	/// <summary>
	/// Minimum slope difference expressed in points.
	/// </summary>
	public decimal SlopeThresholdPoints
	{
		get => _slopeThresholdPoints.Value;
		set => _slopeThresholdPoints.Value = value;
	}

	/// <summary>
	/// Inverts buy and sell actions when enabled.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Candle type requested from the market data subscription.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_movingAverage = null;
		_maHistory.Clear();
		_pointValue = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (MovingAveragePeriod <= 0)
			throw new InvalidOperationException("MovingAveragePeriod must be greater than zero.");

		// Align the base strategy volume with the configured parameter.
		Volume = OrderVolume;

		// Create the moving average that replicates the MetaTrader indicator.
		_movingAverage = new SimpleMovingAverage
		{
			Length = MovingAveragePeriod
		};

		_maHistory.Clear();
		// Recalculate the MetaTrader point size for price-based thresholds and protection.
		_pointValue = CalculatePointValue();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_movingAverage, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _movingAverage);
			DrawOwnTrades(area);
		}

		// Configure take-profit and stop-loss distances in absolute price units.
		StartProtection(
			takeProfit: TakeProfitPoints > 0m ? new Unit(TakeProfitPoints * _pointValue, UnitTypes.Absolute) : null,
			stopLoss: StopLossPoints > 0m ? new Unit(StopLossPoints * _pointValue, UnitTypes.Absolute) : null,
			useMarketOrders: true);
	}

	private decimal CalculatePointValue()
	{
		// Reconstruct the MetaTrader "Point" constant from the security metadata.
		var security = Security;
		var priceStep = security?.PriceStep ?? 1m;
		if (priceStep <= 0m)
			priceStep = 1m;

		var decimals = security?.Decimals;
		if (decimals is > 0)
		{
			var point = 1m;
			for (var i = 0; i < decimals; i++)
				point /= 10m;

			if (point <= 0m)
				point = priceStep;

			return point;
		}

		return priceStep;
	}

	private void ProcessCandle(ICandleMessage candle, decimal movingAverageValue)
	{
		if (candle.State != CandleStates.Finished || _movingAverage == null)
			return;

		if (_movingAverage.Length != MovingAveragePeriod)
			_movingAverage.Length = MovingAveragePeriod;

		if (Volume != OrderVolume)
			Volume = OrderVolume;

		// Store the completed moving average value for shift-based comparisons.
		_maHistory.Add(movingAverageValue);
		TrimHistory();

		var shift = Shift;
		var currentIndex = _maHistory.Count - 1 - shift;
		if (currentIndex <= 0)
			return;

		var previousIndex = currentIndex - 1;
		if (previousIndex < 0)
			return;

		var delta = _maHistory[currentIndex] - _maHistory[previousIndex];

		var threshold = SlopeThresholdPoints * _pointValue;
		if (threshold < 0m)
			threshold = 0m;

		var signal = 0;
		if (delta > threshold)
			signal = 1;
		else if (delta < -threshold)
			signal = -1;

		if (signal == 0)
			return;

		if (ReverseSignals)
			signal = -signal;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
			return; // Mirror the original EA: open trades only when no position is active.

		if (signal > 0)
			BuyMarket(OrderVolume); // Enter long when the moving average slope turns sharply upward.
		else if (signal < 0)
			SellMarket(OrderVolume); // Enter short when the moving average slope turns sharply downward.
	}

	private void TrimHistory()
	{
		// Keep only the values required for the configured shift distance.
		var maxCount = Math.Max(Shift + 3, 3);
		while (_maHistory.Count > maxCount)
			_maHistory.RemoveAt(0);
	}
}

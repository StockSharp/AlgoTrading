using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Forex Line trend-following strategy based on double-smoothed weighted moving averages.
/// Enters long when the fast line crosses above the slow line.
/// Enters short when the fast line crosses below the slow line.
/// </summary>
public class ForexLineStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength1;
	private readonly StrategyParam<int> _fastLength2;
	private readonly StrategyParam<int> _slowLength1;
	private readonly StrategyParam<int> _slowLength2;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevFast;
	private decimal? _prevSlow;

	/// <summary>
	/// First smoothing period for the fast line.
	/// </summary>
	public int FastLength1
	{
		get => _fastLength1.Value;
		set => _fastLength1.Value = value;
	}

	/// <summary>
	/// Second smoothing period for the fast line.
	/// </summary>
	public int FastLength2
	{
		get => _fastLength2.Value;
		set => _fastLength2.Value = value;
	}

	/// <summary>
	/// First smoothing period for the slow line.
	/// </summary>
	public int SlowLength1
	{
		get => _slowLength1.Value;
		set => _slowLength1.Value = value;
	}

	/// <summary>
	/// Second smoothing period for the slow line.
	/// </summary>
	public int SlowLength2
	{
		get => _slowLength2.Value;
		set => _slowLength2.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="ForexLineStrategy"/>.
	/// </summary>
	public ForexLineStrategy()
	{
		_fastLength1 = Param(nameof(FastLength1), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA1 Length", "First smoothing length for fast line", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(3, 20, 1);

		_fastLength2 = Param(nameof(FastLength2), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA2 Length", "Second smoothing length for fast line", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_slowLength1 = Param(nameof(SlowLength1), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA1 Length", "First smoothing length for slow line", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 1);

		_slowLength2 = Param(nameof(SlowLength2), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA2 Length", "Second smoothing length for slow line", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to analyze", "General");
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
		_prevFast = null;
		_prevSlow = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize moving averages for double smoothing
		var ma11 = new WeightedMovingAverage { Length = FastLength1 };
		var ma12 = new WeightedMovingAverage { Length = FastLength2 };
		var ma21 = new WeightedMovingAverage { Length = SlowLength1 };
		var ma22 = new WeightedMovingAverage { Length = SlowLength2 };

		// Subscribe to candle data
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(candle => ProcessCandle(candle, ma11, ma12, ma21, ma22))
			.Start();

		// Visual elements if charting is available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma22); // slow line
			DrawIndicator(area, ma12); // fast line
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, WeightedMovingAverage ma11, WeightedMovingAverage ma12, WeightedMovingAverage ma21, WeightedMovingAverage ma22)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;

		// First smoothing for the fast line
		var v1 = ma11.Process(new DecimalIndicatorValue(ma11, price, candle.OpenTime));
		if (!v1.IsFormed)
			return;

		var l1 = v1.ToDecimal();

		// Second smoothing for the fast line
		var v2 = ma12.Process(new DecimalIndicatorValue(ma12, l1, candle.OpenTime));
		if (!v2.IsFormed)
			return;

		var fast = v2.ToDecimal();

		// First smoothing for the slow line
		var v3 = ma21.Process(new DecimalIndicatorValue(ma21, price, candle.OpenTime));
		if (!v3.IsFormed)
			return;

		var l3 = v3.ToDecimal();

		// Second smoothing for the slow line
		var v4 = ma22.Process(new DecimalIndicatorValue(ma22, l3, candle.OpenTime));
		if (!v4.IsFormed)
			return;

		var slow = v4.ToDecimal();

		// Initialize previous values
		if (_prevFast is null || _prevSlow is null)
		{
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		var wasFastBelow = _prevFast < _prevSlow;
		var isFastBelow = fast < slow;

		// Detect crossovers and open positions
		if (wasFastBelow && !isFastBelow && Position <= 0)
		{
			// Fast line crossed above slow line -> bullish
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (!wasFastBelow && isFastBelow && Position >= 0)
		{
			// Fast line crossed below slow line -> bearish
			SellMarket(Volume + Math.Abs(Position));
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}

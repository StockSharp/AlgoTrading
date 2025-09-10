using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// AO Divergence strategy.
/// Detects bullish and bearish divergences on Awesome Oscillator.
/// </summary>
public class AoDivergenceStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<bool> _useEma;

	private MedianPrice _medianPrice;
	private SimpleMovingAverage _fastSma;
	private SimpleMovingAverage _slowSma;
	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;

	private readonly Queue<decimal> _aoValues = [];
	private readonly Queue<decimal> _lowValues = [];
	private readonly Queue<decimal> _highValues = [];

	private decimal? _prevAoLow;
	private decimal? _prevPriceLow;
	private decimal? _prevAoHigh;
	private decimal? _prevPriceHigh;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast moving average length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow moving average length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Lookback for pivot detection.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	/// <summary>
	/// Use EMA instead of SMA.
	/// </summary>
	public bool UseEma
	{
		get => _useEma.Value;
		set => _useEma.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public AoDivergenceStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_fastLength = Param(nameof(FastLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast MA length", "Awesome Oscillator")
			.SetCanOptimize(true)
			.SetOptimize(3, 10, 1);

		_slowLength = Param(nameof(SlowLength), 34)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow MA length", "Awesome Oscillator")
			.SetCanOptimize(true)
			.SetOptimize(20, 50, 2);

		_lookback = Param(nameof(Lookback), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Pivot lookback bars", "Divergence")
			.SetCanOptimize(true)
			.SetOptimize(3, 10, 1);

		_useEma = Param(nameof(UseEma), false)
			.SetDisplay("Use EMA", "Use EMA instead of SMA", "Awesome Oscillator");
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

		_aoValues.Clear();
		_lowValues.Clear();
		_highValues.Clear();
		_prevAoLow = null;
		_prevPriceLow = null;
		_prevAoHigh = null;
		_prevPriceHigh = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_medianPrice = new MedianPrice();
		_fastSma = new SimpleMovingAverage { Length = FastLength };
		_slowSma = new SimpleMovingAverage { Length = SlowLength };
		_fastEma = new ExponentialMovingAverage { Length = FastLength };
		_slowEma = new ExponentialMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var median = (candle.HighPrice + candle.LowPrice) / 2m;

		var fast = UseEma
			? _fastEma.Process(median, candle.OpenTime, true).ToDecimal()
			: _fastSma.Process(median, candle.OpenTime, true).ToDecimal();

		var slow = UseEma
			? _slowEma.Process(median, candle.OpenTime, true).ToDecimal()
			: _slowSma.Process(median, candle.OpenTime, true).ToDecimal();

		var ao = fast - slow;

		_aoValues.Enqueue(ao);
		_lowValues.Enqueue(candle.LowPrice);
		_highValues.Enqueue(candle.HighPrice);

		var window = Lookback * 2 + 1;
		if (_aoValues.Count < window)
			return;

		var aoArr = _aoValues.ToArray();
		var lowArr = _lowValues.ToArray();
		var highArr = _highValues.ToArray();

		var idx = Lookback;
		var aoCenter = aoArr[idx];

		var isPivotLow = true;
		var isPivotHigh = true;

		for (var i = 0; i < window; i++)
		{
			if (i == idx)
				continue;

			if (aoArr[i] <= aoCenter)
				isPivotLow = false;

			if (aoArr[i] >= aoCenter)
				isPivotHigh = false;

			if (!isPivotLow && !isPivotHigh)
				break;
		}

		var priceLow = lowArr[idx];
		var priceHigh = highArr[idx];

		if (isPivotLow)
		{
			if (_prevAoLow != null && _prevPriceLow != null)
			{
				var oscHl = aoCenter > _prevAoLow.Value;
				var priceLl = priceLow < _prevPriceLow.Value;

				if (oscHl && priceLl && Position <= 0)
					BuyMarket();
			}

			_prevAoLow = aoCenter;
			_prevPriceLow = priceLow;
		}

		if (isPivotHigh)
		{
			if (_prevAoHigh != null && _prevPriceHigh != null)
			{
				var oscLh = aoCenter < _prevAoHigh.Value;
				var priceHh = priceHigh > _prevPriceHigh.Value;

				if (oscLh && priceHh && Position >= 0)
					SellMarket();
			}

			_prevAoHigh = aoCenter;
			_prevPriceHigh = priceHigh;
		}

		_aoValues.Dequeue();
		_lowValues.Dequeue();
		_highValues.Dequeue();
	}
}

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fisher Cyber Cycle crossover strategy.
/// Buys when Fisher line crosses above its trigger and sells on cross below.
/// </summary>
public class FisherCyberCycleStrategy : Strategy
{
	private readonly StrategyParam<decimal> _alpha;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	private FisherCyberCycleIndicator _indicator = null!;
	private decimal _prevFisher;
	private decimal _prevTrigger;
	private bool _initialized;

	/// <summary>
	/// Smoothing factor for cycle calculation.
	/// </summary>
	public decimal Alpha
	{
		get => _alpha.Value;
		set => _alpha.Value = value;
	}

	/// <summary>
	/// Normalization window length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Type of candles to use for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="FisherCyberCycleStrategy"/>.
	/// </summary>
	public FisherCyberCycleStrategy()
	{
		_alpha = Param(nameof(Alpha), 0.07m)
			.SetDisplay("Alpha", "Smoothing factor", "Indicators")
			.SetRange(0.01m, 0.5m)
			.SetCanOptimize(true);

		_length = Param(nameof(Length), 8)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Normalization window", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_prevFisher = 0m;
		_prevTrigger = 0m;
		_initialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_indicator = new FisherCyberCycleIndicator
		{
			Alpha = Alpha,
			Length = Length
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_indicator, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _indicator);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fisher, decimal trigger)
	{
		if (candle.State != CandleStates.Finished || !_indicator.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_initialized)
		{
			_prevFisher = fisher;
			_prevTrigger = trigger;
			_initialized = true;
			return;
		}

		var crossUp = _prevFisher <= _prevTrigger && fisher > trigger;
		var crossDown = _prevFisher >= _prevTrigger && fisher < trigger;

		if (crossUp && Position <= 0)
		{
			var volume = Position < 0 ? Math.Abs(Position) + Volume : Volume;
			BuyMarket(volume);
		}
		else if (crossDown && Position >= 0)
		{
			var volume = Position > 0 ? Math.Abs(Position) + Volume : Volume;
			SellMarket(volume);
		}

		_prevFisher = fisher;
		_prevTrigger = trigger;
	}
}

/// <summary>
/// Indicator calculating Fisher Transform of Ehlers' Cyber Cycle.
/// </summary>
public class FisherCyberCycleIndicator : LengthIndicator<decimal>
{
	public decimal Alpha { get; set; } = 0.07m;

	private readonly decimal[] _price = new decimal[4];
	private readonly decimal[] _smooth = new decimal[4];
	private readonly decimal[] _cycle = new decimal[3];
	private readonly Highest _highest = new();
	private readonly Lowest _lowest = new();
	private decimal _prevFish;
	private int _count;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
			return new DecimalIndicatorValue(this, default, input.Time);

		// Median price of the candle.
		var price = (candle.HighPrice + candle.LowPrice) / 2m;

		// Shift stored values.
		_price[3] = _price[2];
		_price[2] = _price[1];
		_price[1] = _price[0];
		_price[0] = price;

		_smooth[3] = _smooth[2];
		_smooth[2] = _smooth[1];
		_smooth[1] = _smooth[0];
		_smooth[0] = (_price[0] + 2m * _price[1] + 2m * _price[2] + _price[3]) / 6m;

		_cycle[2] = _cycle[1];
		_cycle[1] = _cycle[0];

		if (_count < 3)
			_cycle[0] = (_price[0] + 2m * _price[1] + _price[2]) / 4m;
		else
		{
			var k0 = (decimal)Math.Pow((double)(1m - 0.5m * Alpha), 2);
			var k1 = 2m;
			var k2 = 2m * (1m - Alpha);
			var k3 = (decimal)Math.Pow((double)(1m - Alpha), 2);
			_cycle[0] = k0 * (_smooth[0] - k1 * _smooth[1] + _smooth[2]) + k2 * _cycle[1] - k3 * _cycle[2];
		}

		_count++;

		_highest.Length = Length;
		_lowest.Length = Length;
		var hh = _highest.Process(_cycle[0]).ToDecimal();
		var ll = _lowest.Process(_cycle[0]).ToDecimal();

		var value1 = hh != ll ? (_cycle[0] - ll) / (hh - ll) : 0m;
		var fish = 0.5m * (decimal)Math.Log((double)((1m + 1.98m * (value1 - 0.5m)) / (1m - 1.98m * (value1 - 0.5m))));
		var trigger = _prevFish;
		_prevFish = fish;

		return new FisherCyberCycleValue(this, input, fish, trigger);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();
		Array.Clear(_price, 0, _price.Length);
		Array.Clear(_smooth, 0, _smooth.Length);
		Array.Clear(_cycle, 0, _cycle.Length);
		_prevFish = 0m;
		_highest.Reset();
		_lowest.Reset();
		_count = 0;
	}
}

/// <summary>
/// Indicator value for <see cref="FisherCyberCycleIndicator"/>.
/// </summary>
public class FisherCyberCycleValue : ComplexIndicatorValue
{
	public FisherCyberCycleValue(IIndicator indicator, IIndicatorValue input, decimal fisher, decimal trigger)
		: base(indicator, input, (nameof(Fisher), fisher), (nameof(Trigger), trigger))
	{
	}

	/// <summary>
	/// Fisher line value.
	/// </summary>
	public decimal Fisher => (decimal)GetValue(nameof(Fisher));

	/// <summary>
	/// Trigger line value.
	/// </summary>
	public decimal Trigger => (decimal)GetValue(nameof(Trigger));
}


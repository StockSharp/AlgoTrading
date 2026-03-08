using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
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
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFisher;
	private decimal _prevTrigger;
	private bool _initialized;

	// Cyber cycle state
	private readonly decimal[] _price = new decimal[4];
	private readonly decimal[] _smooth = new decimal[4];
	private readonly decimal[] _cycle = new decimal[3];
	private decimal _prevFish;
	private int _count;
	private int _barsSinceTrade;

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
	/// Bars to wait after a completed trade.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
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
			.SetRange(0.01m, 0.5m);

		_length = Param(nameof(Length), 8)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Normalization window", "Indicators")
			.SetOptimize(5, 20, 1);

		_cooldownBars = Param(nameof(CooldownBars), 1)
			.SetDisplay("Cooldown Bars", "Bars to wait after a completed trade", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevFisher = 0m;
		_prevTrigger = 0m;
		_initialized = false;
		_prevFish = 0m;
		_count = 0;
		_barsSinceTrade = CooldownBars;
		Array.Clear(_price, 0, _price.Length);
		Array.Clear(_smooth, 0, _smooth.Length);
		Array.Clear(_cycle, 0, _cycle.Length);
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		OnReseted();

		var highest = new Highest { Length = Length };
		var lowest = new Lowest { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		void ProcessCandle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
				return;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			if (_barsSinceTrade < CooldownBars)
				_barsSinceTrade++;

			var price = (candle.HighPrice + candle.LowPrice) / 2m;
			var t = candle.OpenTime;

			// Shift stored values
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

			var hhResult = highest.Process(new DecimalIndicatorValue(highest, _cycle[0], t) { IsFinal = true });
			var llResult = lowest.Process(new DecimalIndicatorValue(lowest, _cycle[0], t) { IsFinal = true });

			if (!highest.IsFormed || !lowest.IsFormed)
				return;

			var hh = hhResult.ToDecimal();
			var ll = llResult.ToDecimal();

			var value1 = hh != ll ? (_cycle[0] - ll) / (hh - ll) : 0m;

			// Clamp to avoid log domain error
			var normalized = 1.98m * (value1 - 0.5m);
			if (normalized >= 0.999m) normalized = 0.999m;
			if (normalized <= -0.999m) normalized = -0.999m;

			var fish = 0.5m * (decimal)Math.Log((double)((1m + normalized) / (1m - normalized)));
			var trigger = _prevFish;
			_prevFish = fish;

			if (!_initialized)
			{
				_prevFisher = fish;
				_prevTrigger = trigger;
				_initialized = true;
				return;
			}

			var crossUp = _prevFisher <= _prevTrigger && fish > trigger;
			var crossDown = _prevFisher >= _prevTrigger && fish < trigger;

			if (_barsSinceTrade >= CooldownBars)
			{
				if (crossUp && Position <= 0)
				{
					BuyMarket(Volume + Math.Abs(Position));
					_barsSinceTrade = 0;
				}
				else if (crossDown && Position >= 0)
				{
					SellMarket(Volume + Math.Abs(Position));
					_barsSinceTrade = 0;
				}
			}

			_prevFisher = fish;
			_prevTrigger = trigger;
		}
	}
}

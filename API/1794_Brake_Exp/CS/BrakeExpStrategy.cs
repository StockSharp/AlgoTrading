using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on custom BrakeExp indicator.
/// Opens a long position when the indicator switches to an up trend
/// and opens a short position on a down trend switch.
/// </summary>
public class BrakeExpStrategy : Strategy
{
	private readonly StrategyParam<decimal> _a;
	private readonly StrategyParam<decimal> _b;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// A parameter of BrakeExp indicator.
	/// </summary>
	public decimal A
	{
		get => _a.Value;
		set => _a.Value = value;
	}

	/// <summary>
	/// B parameter of BrakeExp indicator.
	/// </summary>
	public decimal B
	{
		get => _b.Value;
		set => _b.Value = value;
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
	/// Initialize the strategy with default parameters.
	/// </summary>
	public BrakeExpStrategy()
	{
		_a = Param(nameof(A), 3m)
		.SetDisplay("A", "A parameter of BrakeExp indicator", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(1m, 5m, 1m);

		_b = Param(nameof(B), 1m)
		.SetDisplay("B", "B parameter of BrakeExp indicator", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 2m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Time frame for BrakeExp indicator", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var indicator = new BrakeExpIndicator
		{
			A = A,
			B = B,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(indicator, ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var brake = (BrakeExpValue)value;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (brake.UpSignal)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (brake.DownSignal)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
		else if (brake.UpTrend && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}
		else if (brake.DownTrend && Position > 0)
		{
			SellMarket(Position);
		}
	}

	/// <summary>
	/// Custom BrakeExp indicator implementation.
	/// </summary>
	private class BrakeExpIndicator : Indicator<ICandleMessage>
	{
		private int _barIndex;
		private decimal _maxPrice = decimal.MinValue;
		private decimal _minPrice = decimal.MaxValue;
		private decimal _beginPrice;
		private bool _isLong = true;
		private int _beginBar;
		private bool _prevIsLong = true;
		private decimal _a;
		private decimal _b;

		public decimal A { get; set; } = 3m;
		public decimal B { get; set; } = 1m;

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<ICandleMessage>();

			if (_barIndex == 0)
			{
				_a = A * 0.1m;
				_b = B;
				_beginPrice = candle.LowPrice;
				_beginBar = 0;
				_prevIsLong = _isLong;
			}

			if (_maxPrice < candle.HighPrice)
			_maxPrice = candle.HighPrice;
			if (_minPrice > candle.LowPrice)
			_minPrice = candle.LowPrice;

			var exp = (decimal)(Math.Exp((_beginBar - _barIndex) * (double)_a) - 1m) * _b;
			var value = _isLong ? _beginPrice + exp : _beginPrice - exp;

			if (_isLong && value > candle.LowPrice)
			{
				_isLong = false;
				_beginPrice = _maxPrice;
				value = _beginPrice;
				_beginBar = _barIndex;
				_maxPrice = decimal.MinValue;
				_minPrice = decimal.MaxValue;
			}
			else if (!_isLong && value < candle.HighPrice)
			{
				_isLong = true;
				_beginPrice = _minPrice;
				value = _beginPrice;
				_beginBar = _barIndex;
				_maxPrice = decimal.MinValue;
				_minPrice = decimal.MaxValue;
			}

			var upTrend = _isLong;
			var downTrend = !_isLong;
			var upSignal = !_prevIsLong && _isLong;
			var downSignal = _prevIsLong && !_isLong;

			_prevIsLong = _isLong;
			_barIndex++;
			IsFormed = _barIndex > 1;

			return new BrakeExpValue(this, input, upTrend, downTrend, upSignal, downSignal);
		}

		public override void Reset()
		{
			base.Reset();
			_barIndex = 0;
			_maxPrice = decimal.MinValue;
			_minPrice = decimal.MaxValue;
			_beginPrice = 0m;
			_isLong = true;
			_beginBar = 0;
			_prevIsLong = true;
		}
	}

	/// <summary>
	/// Indicator value containing BrakeExp signals.
	/// </summary>
	private class BrakeExpValue : ComplexIndicatorValue
	{
		public BrakeExpValue(IIndicator indicator, IIndicatorValue input, bool upTrend, bool downTrend, bool upSignal, bool downSignal)
		: base(indicator, input, (nameof(UpTrend), upTrend), (nameof(DownTrend), downTrend), (nameof(UpSignal), upSignal), (nameof(DownSignal), downSignal))
		{
		}

		/// <summary>
		/// Indicator shows up trend.
		/// </summary>
		public bool UpTrend => (bool)GetValue(nameof(UpTrend));

		/// <summary>
		/// Indicator shows down trend.
		/// </summary>
		public bool DownTrend => (bool)GetValue(nameof(DownTrend));

		/// <summary>
		/// Indicator generated buy signal.
		/// </summary>
		public bool UpSignal => (bool)GetValue(nameof(UpSignal));

		/// <summary>
		/// Indicator generated sell signal.
		/// </summary>
		public bool DownSignal => (bool)GetValue(nameof(DownSignal));
	}
}

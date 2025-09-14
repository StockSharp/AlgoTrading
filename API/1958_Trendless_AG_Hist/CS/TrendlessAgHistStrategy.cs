using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Trendless AG Histogram indicator.
/// Opens long when the histogram forms a trough and starts rising.
/// Opens short when it forms a peak and starts falling.
/// Includes optional stop-loss and take-profit levels.
/// </summary>
public class TrendlessAgHistStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private TrendlessAgHist _indicator;
	private decimal _prev1;
	private decimal _prev2;
	private bool _initialized;
	private decimal _entryPrice;
	private bool _isLong;

	/// <summary>
	/// Fast smoothing period.
	/// </summary>
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }

	/// <summary>
	/// Slow smoothing period.
	/// </summary>
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }

	/// <summary>
	/// Stop loss in price units.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Take profit in price units.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref=\"TrendlessAgHistStrategy\"/>.
	/// </summary>
	public TrendlessAgHistStrategy()
	{
		_fastLength = Param(nameof(FastLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Period of the first smoothing", "Parameters");

		_slowLength = Param(nameof(SlowLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Period of the second smoothing", "Parameters");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Loss limit in price units", "Risk Management");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Profit target in price units", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(12).TimeFrame())
			.SetDisplay("Candle Type", "Working candle timeframe", "General");
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
		_indicator?.Reset();
		_prev1 = 0m;
		_prev2 = 0m;
		_initialized = false;
		_entryPrice = 0m;
		_isLong = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_indicator = new TrendlessAgHist
		{
			FastLength = FastLength,
			SlowLength = SlowLength
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_indicator, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _indicator);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_indicator.IsFormed)
		{
			_prev2 = _prev1;
			_prev1 = value;
			return;
		}

		if (!_initialized)
		{
			_prev2 = _prev1;
			_prev1 = value;
			_initialized = true;
			return;
		}

		if (_prev1 < _prev2 && value > _prev1)
		{
			if (Position <= 0)
			{
				_entryPrice = candle.ClosePrice;
				_isLong = true;
				BuyMarket(Volume + Math.Abs(Position));
			}
		}
		else if (_prev1 > _prev2 && value < _prev1)
		{
			if (Position >= 0)
			{
				_entryPrice = candle.ClosePrice;
				_isLong = false;
				SellMarket(Volume + Math.Abs(Position));
			}
		}

		_prev2 = _prev1;
		_prev1 = value;

		if (Position != 0 && _entryPrice != 0m)
			CheckRisk(candle.ClosePrice);
	}

	private void CheckRisk(decimal price)
	{
		if (_isLong && Position > 0)
		{
			if (price <= _entryPrice - StopLoss || price >= _entryPrice + TakeProfit)
				SellMarket(Position);
		}
		else if (!_isLong && Position < 0)
		{
			if (price >= _entryPrice + StopLoss || price <= _entryPrice - TakeProfit)
				BuyMarket(Math.Abs(Position));
		}
	}

	private class TrendlessAgHist : Indicator<ICandleMessage>
	{
		public int FastLength { get; set; } = 7;
		public int SlowLength { get; set; } = 5;

		private readonly ExponentialMovingAverage _fast = new() { Length = 7 };
		private readonly ExponentialMovingAverage _slow = new() { Length = 5 };

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<ICandleMessage>();
			var price = candle.ClosePrice;

			_fast.Length = FastLength;
			_slow.Length = SlowLength;

			var fastVal = _fast.Process(input.Time, price).GetValue<decimal>();
			var diff = price - fastVal;
			var slowVal = _slow.Process(input.Time, diff).GetValue<decimal>();

			IsFormed = _slow.IsFormed;
			return new DecimalIndicatorValue(this, slowVal, input.Time);
		}

		public override void Reset()
		{
			base.Reset();
			_fast.Reset();
			_slow.Reset();
		}
	}
}

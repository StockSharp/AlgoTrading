using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Dots indicator which trades reversals on color changes.
/// </summary>
public class DotsStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _filter;
	private readonly StrategyParam<DataType> _candleType;

	private DotsIndicator _dots;
	private decimal? _prevColor;

	/// <summary>
	/// Dots indicator calculation length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Minimal change required to flip color.
	/// </summary>
	public decimal Filter
	{
		get => _filter.Value;
		set => _filter.Value = value;
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
	/// Initializes a new instance of the strategy.
	/// </summary>
	public DotsStrategy()
	{
		_length = Param(nameof(Length), 10)
			.SetDisplay("Length", "Dots calculation length", "Parameters");

		_filter = Param(nameof(Filter), 0m)
			.SetDisplay("Filter", "Minimal delta to change color", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
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
		_dots = null;
		_prevColor = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_dots = new DotsIndicator
		{
			Length = Length,
			Filter = Filter
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_dots, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _dots);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal color)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var curr = color;

		if (_prevColor is null)
		{
			_prevColor = curr;
			return;
		}

		if (_prevColor == 0m && curr == 1m && Position <= 0)
		{
			// Trend switched from up to down, open long position.
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (_prevColor == 1m && curr == 0m && Position >= 0)
		{
			// Trend switched from down to up, open short position.
			SellMarket(Volume + Math.Abs(Position));
		}

		_prevColor = curr;
	}

	private class DotsIndicator : Indicator<decimal>
	{
		public int Length { get; set; } = 10;
		public decimal Filter { get; set; } = 0m;

		private readonly List<decimal> _prices = new();
		private decimal? _prevMa;
		private decimal _prevColor;

		private int Len => (int)(Length * 4 + (Length - 1));
		private double Res1 => 1.0 / Math.Max(1.0, Length - 2);
		private double Res2 => (2.0 * 4 - 1.0) / (4 * Length - 1.0);
		private const double Coeff = 3.0 * Math.PI;

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<ICandleMessage>();
			var price = candle.ClosePrice;

			_prices.Insert(0, price);
			if (_prices.Count > Len)
				_prices.RemoveAt(_prices.Count - 1);

			if (_prices.Count < Len)
				return new DecimalIndicatorValue(this, _prevColor, input.Time);

			double t = 0, sum = 0, weight = 0;
			for (var i = 0; i < Len; i++)
			{
				var g = 1.0 / (Coeff * t + 1.0);
				if (t <= 0.5)
					g = 1.0;
				var beta = Math.Cos(Math.PI * t);
				var alfa = g * beta;
				sum += alfa * (double)_prices[i];
				weight += alfa;
				if (t < 1.0)
					t += Res1;
				else if (t < Len - 1)
					t += Res2;
			}

			var maPrev = (double)(_prevMa ?? _prices[1]);
			var ma = weight != 0 ? sum / Math.Abs(weight) : maPrev;
			if (Filter > 0m && Math.Abs(ma - maPrev) < (double)Filter)
				ma = maPrev;

			decimal color;
			if (ma - maPrev > (double)Filter)
				color = 0m;
			else if (maPrev - ma > (double)Filter)
				color = 1m;
			else
				color = _prevColor;

			_prevMa = (decimal)ma;
			_prevColor = color;
			return new DecimalIndicatorValue(this, color, input.Time);
		}

		public override void Reset()
		{
			base.Reset();
			_prices.Clear();
			_prevMa = null;
			_prevColor = 0m;
		}
	}
}

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend following strategy based on the Modified Optimum Elliptic Filter indicator.
/// The indicator is a digital filter applied to the mid price of each candle.
/// A long position is opened when the filter is rising and the latest value exceeds the previous one.
/// A short position is opened on the opposite condition.
/// </summary>
public class ModifiedOptimumEllipticFilterStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly ModifiedOptimumEllipticFilter _filter = new();
	private readonly List<decimal> _values = new();

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="ModifiedOptimumEllipticFilterStrategy"/>.
	/// </summary>
	public ModifiedOptimumEllipticFilterStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame()).SetDisplay("Candle Type");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_filter, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _filter);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal filterValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_values.Insert(0, filterValue);
		if (_values.Count > 3)
			_values.RemoveAt(3);

		if (_values.Count < 3)
			return;

		var current = _values[0];
		var prev1 = _values[1];
		var prev2 = _values[2];

		if (prev1 < prev2)
		{
			if (current > prev1 && Position <= 0)
				BuyMarket();
		}
		else if (prev1 > prev2)
		{
			if (current < prev1 && Position >= 0)
				SellMarket();
		}
	}

	private class ModifiedOptimumEllipticFilter : Indicator<decimal>
	{
		private readonly List<decimal> _prices = new();
		private readonly List<decimal> _filterValues = new();

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<ICandleMessage>();
			var price = (candle.HighPrice + candle.LowPrice) / 2m;

			_prices.Insert(0, price);
			if (_prices.Count > 4)
				_prices.RemoveAt(4);

			decimal value;

			if (_prices.Count < 4 || _filterValues.Count < 2)
			{
				value = price;
				IsFormed = false;
			}
			else
			{
				value = 0.13785m * (2m * _prices[0] - _prices[1])
				+ 0.0007m * (2m * _prices[1] - _prices[2])
				+ 0.13785m * (2m * _prices[2] - _prices[3])
				+ 1.2103m * _filterValues[0]
				- 0.4867m * _filterValues[1];
				IsFormed = true;
			}

			_filterValues.Insert(0, value);
			if (_filterValues.Count > 2)
				_filterValues.RemoveAt(2);

			return new DecimalIndicatorValue(this, value, input.Time);
		}

		public override void Reset()
		{
			base.Reset();
			_prices.Clear();
			_filterValues.Clear();
		}
	}
}

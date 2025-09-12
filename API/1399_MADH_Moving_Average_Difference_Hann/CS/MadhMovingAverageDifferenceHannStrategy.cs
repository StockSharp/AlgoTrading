namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on the MADH (Moving Average Difference, Hann) indicator.
/// Goes long when the MADH value is positive and short when negative.
/// </summary>
public class MadhMovingAverageDifferenceHannStrategy : Strategy
{
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _dominantCycle;
	private readonly StrategyParam<DataType> _candleType;

	private MadhIndicator _madh;

	/// <summary>
	/// Short length of the Hann filter.
	/// </summary>
	public int ShortLength
	{
		get => _shortLength.Value;
		set => _shortLength.Value = value;
	}

	/// <summary>
	/// Dominant cycle used to compute long length.
	/// </summary>
	public int DominantCycle
	{
		get => _dominantCycle.Value;
		set => _dominantCycle.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="MadhMovingAverageDifferenceHannStrategy"/>.
	/// </summary>
	public MadhMovingAverageDifferenceHannStrategy()
	{
		_shortLength = Param(nameof(ShortLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Short Length", "Short Hann window", "Parameters");

		_dominantCycle = Param(nameof(DominantCycle), 27)
			.SetGreaterThanZero()
			.SetDisplay("Dominant Cycle", "Dominant cycle length", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_madh?.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_madh = new MadhIndicator
		{
			ShortLength = ShortLength,
			DominantCycle = DominantCycle
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_madh, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _madh);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal madhValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_madh.IsFormed)
			return;

		if (madhValue > 0 && Position <= 0)
		{
			BuyMarket();
		}
		else if (madhValue < 0 && Position >= 0)
		{
			SellMarket();
		}
	}

	private class MadhIndicator : Indicator<decimal>
	{
		public int ShortLength { get; set; } = 8;
		public int DominantCycle { get; set; } = 27;

		private decimal[] _shortWeights;
		private decimal[] _longWeights;
		private readonly List<decimal> _shortValues = new();
		private readonly List<decimal> _longValues = new();

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var price = input.GetValue<decimal>();

			PrepareWeights();

			_shortValues.Insert(0, price);
			if (_shortValues.Count > ShortLength)
				_shortValues.RemoveAt(_shortValues.Count - 1);

			var longLen = LongLength;
			_longValues.Insert(0, price);
			if (_longValues.Count > longLen)
				_longValues.RemoveAt(_longValues.Count - 1);

			if (_shortValues.Count < ShortLength || _longValues.Count < longLen)
			{
				IsFormed = false;
				return new DecimalIndicatorValue(this, 0m, input.Time);
			}

			double filt1 = 0.0;
			double coefs1 = 0.0;
			for (var i = 0; i < ShortLength; i++)
			{
				var coef = (double)_shortWeights[i];
				filt1 += coef * (double)_shortValues[i];
				coefs1 += coef;
			}
			filt1 /= coefs1;

			double filt2 = 0.0;
			double coefs2 = 0.0;
			for (var i = 0; i < longLen; i++)
			{
				var coef = (double)_longWeights[i];
				filt2 += coef * (double)_longValues[i];
				coefs2 += coef;
			}
			filt2 /= coefs2;

			IsFormed = true;
			var madh = (decimal)((filt1 - filt2) / filt2 * 100.0);
			return new DecimalIndicatorValue(this, madh, input.Time);
		}

		private int LongLength => (int)(DominantCycle * 0.5m + ShortLength);

		private void PrepareWeights()
		{
			if (_shortWeights == null)
				_shortWeights = CalcWeights(ShortLength);

			var longLen = LongLength;
			if (_longWeights == null || _longWeights.Length != longLen)
				_longWeights = CalcWeights(longLen);
		}

		private static decimal[] CalcWeights(int length)
		{
			var arr = new decimal[length];
			var factor = (decimal)(Math.PI * 2.0 / (length + 1));
			for (var i = 1; i <= length; i++)
				arr[i - 1] = 1m - (decimal)Math.Cos(i * (double)factor);
			return arr;
		}

		public override void Reset()
		{
			base.Reset();
			_shortValues.Clear();
			_longValues.Clear();
			_shortWeights = null;
			_longWeights = null;
		}
	}
}

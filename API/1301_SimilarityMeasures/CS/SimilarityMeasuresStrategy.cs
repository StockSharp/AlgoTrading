using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades based on Euclidean distance between price and its SMA.
/// Buys when distance is below the threshold and sells when above.
/// </summary>
public class SimilarityMeasuresStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _prices = [];
	private readonly List<decimal> _smaValues = [];

	/// <summary>
	/// Number of bars to calculate distance.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Distance threshold.
	/// </summary>
	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
	}

	/// <summary>
	/// Candle type to use for calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SimilarityMeasuresStrategy"/> class.
	/// </summary>
	public SimilarityMeasuresStrategy()
	{
		_length = Param(nameof(Length), 10)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Number of bars for distance", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 5);

		_threshold = Param(nameof(Threshold), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Threshold", "Euclidean distance threshold", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var sma = new SMA { Length = Length };
		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		_prices.Add(candle.ClosePrice);
		_smaValues.Add(smaValue);

		if (_prices.Count > Length)
		{
			_prices.RemoveAt(0);
			_smaValues.RemoveAt(0);
		}

		if (_prices.Count < Length)
			return;

		var distance = Euclidean(_prices, _smaValues);

		if (distance < Threshold && Position <= 0)
		{
			BuyMarket();
		}
		else if (distance > Threshold && Position >= 0)
		{
			SellMarket();
		}
	}

	private static decimal Ssd(IList<decimal> p, IList<decimal> q)
	{
		if (p.Count != q.Count || p.Count < 1)
			throw new ArgumentException("Invalid array size.");

		var dist = 0m;
		for (var i = 0; i < p.Count; i++)
		{
			var diff = p[i] - q[i];
			dist += diff * diff;
		}

		return dist;
	}

	private static decimal Euclidean(IList<decimal> p, IList<decimal> q)
		=> (decimal)Math.Sqrt((double)Ssd(p, q));

	private static decimal Manhattan(IList<decimal> p, IList<decimal> q)
	{
		if (p.Count != q.Count || p.Count < 1)
			throw new ArgumentException("Invalid array size.");

		var dist = 0m;
		for (var i = 0; i < p.Count; i++)
		{
			dist += Math.Abs(p[i] - q[i]);
		}

		return dist;
	}
}

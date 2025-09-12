using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breadth score based on moving averages.
/// Buys when score turns positive and sells when negative.
/// </summary>
public class ZahorchakMeasureStrategy : Strategy
{
	private readonly StrategyParam<decimal> _points;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _weight1;
	private readonly StrategyParam<decimal> _weight2;
	private readonly StrategyParam<decimal> _weight3;
	private readonly StrategyParam<decimal> _weight4;
	private readonly StrategyParam<decimal> _weight5;
	private readonly StrategyParam<decimal> _weight6;
	private readonly StrategyParam<DataType> _candleType;

	private readonly SMA _short = new() { Length = 25 };
	private readonly SMA _medium = new() { Length = 75 };
	private readonly SMA _long = new() { Length = 200 };
	private readonly ExponentialMovingAverage _ema = new();

	private decimal? _prevScore;

	public decimal Points { get => _points.Value; set => _points.Value = value; }
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public decimal Weight1 { get => _weight1.Value; set => _weight1.Value = value; }
	public decimal Weight2 { get => _weight2.Value; set => _weight2.Value = value; }
	public decimal Weight3 { get => _weight3.Value; set => _weight3.Value = value; }
	public decimal Weight4 { get => _weight4.Value; set => _weight4.Value = value; }
	public decimal Weight5 { get => _weight5.Value; set => _weight5.Value = value; }
	public decimal Weight6 { get => _weight6.Value; set => _weight6.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ZahorchakMeasureStrategy()
	{
		_points = Param(nameof(Points), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Point Value", "Score per condition", "Scoring");

		_emaLength = Param(nameof(EmaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Smoothing length", "Indicators")
			.SetCanOptimize(true);

		_weight1 = Param(nameof(Weight1), 1m);
		_weight2 = Param(nameof(Weight2), 1m);
		_weight3 = Param(nameof(Weight3), 1m);
		_weight4 = Param(nameof(Weight4), 1m);
		_weight5 = Param(nameof(Weight5), 1m);
		_weight6 = Param(nameof(Weight6), 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_ema.Length = EmaLength;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_short, _medium, _long, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal shortValue, decimal mediumValue, decimal longValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var score = 0m;
		score += (candle.ClosePrice > shortValue ? Points : -Points) * Weight1;
		score += (candle.ClosePrice > mediumValue ? Points : -Points) * Weight2;
		score += (candle.ClosePrice > longValue ? Points : -Points) * Weight3;
		score += (shortValue > mediumValue ? Points : -Points) * Weight4;
		score += (mediumValue > longValue ? Points : -Points) * Weight5;
		score += (shortValue > longValue ? Points : -Points) * Weight6;

		var maxScore = Points * (Weight1 + Weight2 + Weight3 + Weight4 + Weight5 + Weight6);
		var normalized = 10m * score / maxScore;
		var emaVal = _ema.Process(candle.OpenTime, normalized);
		if (!emaVal.IsFinal)
			return;
		var measure = emaVal.GetValue<decimal>();

		if (_prevScore is null)
		{
			_prevScore = measure;
			return;
		}

		if (_prevScore <= 0 && measure > 0 && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (_prevScore >= 0 && measure < 0 && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		_prevScore = measure;
	}
}

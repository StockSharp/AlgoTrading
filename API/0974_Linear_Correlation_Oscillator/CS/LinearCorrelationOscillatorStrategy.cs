using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Linear Correlation Oscillator strategy.
/// Goes long when correlation crosses above zero and shorts on cross below.
/// </summary>
public class LinearCorrelationOscillatorStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	private decimal[] _prices;
	private int _index;
	private decimal _prevCorrelation;

	/// <summary>
	/// Lookback period for correlation calculation.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set
		{
			_length.Value = value;
			_prices = new decimal[value];
		}
	}

	/// <summary>
	/// Candle type used for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public LinearCorrelationOscillatorStrategy()
	{
		_length = Param(nameof(Length), 14).SetDisplay("Length").SetCanOptimize(true);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle type");

		_prices = new decimal[Length];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection();

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

		_prices[_index % Length] = candle.ClosePrice;
		_index++;

		if (_index < Length)
		{
			_prevCorrelation = 0m;
			return;
		}

		var correlation = CalculateCorrelation();

		if (IsFormedAndOnlineAndAllowTrading())
		{
			if (_prevCorrelation <= 0m && correlation > 0m && Position <= 0)
				BuyMarket();
			else if (_prevCorrelation >= 0m && correlation < 0m && Position >= 0)
				SellMarket();
		}

		_prevCorrelation = correlation;
	}

	private decimal CalculateCorrelation()
	{
		var n = Length;
		decimal sumY = 0m;
		decimal sumY2 = 0m;
		decimal sumXY = 0m;

		for (var i = 0; i < n; i++)
		{
			var price = _prices[( _index - n + i) % n];
			var x = i + 1;
			sumY += price;
			sumY2 += price * price;
			sumXY += price * x;
		}

		var sumX = n * (n + 1m) / 2m;
		var sumX2 = n * (n + 1m) * (2m * n + 1m) / 6m;

		var numerator = n * sumXY - sumX * sumY;
		var denominator = (decimal)Math.Sqrt((double)((n * sumX2 - sumX * sumX) * (n * sumY2 - sumY * sumY)));

		return denominator == 0m ? 0m : numerator / denominator;
	}
}

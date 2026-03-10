using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
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
	private readonly StrategyParam<decimal> _entryLevel;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal[] _prices;
	private int _index;
	private decimal _prevCorrelation;
	private int _barsFromSignal;

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
	/// Absolute correlation level required to open a position.
	/// </summary>
	public decimal EntryLevel
	{
		get => _entryLevel.Value;
		set => _entryLevel.Value = value;
	}

	/// <summary>
	/// Minimum number of bars between entry signals.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
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
		_length = Param(nameof(Length), 20)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Lookback length", "General")
			
			.SetOptimize(18, 60, 2);

		_entryLevel = Param(nameof(EntryLevel), 0.08m)
			.SetGreaterThanZero()
			.SetDisplay("Entry Level", "Absolute level required for entry", "General")
			
			.SetOptimize(0.10m, 0.40m, 0.05m);

		_cooldownBars = Param(nameof(CooldownBars), 4)
			.SetGreaterThanZero()
			.SetDisplay("Cooldown Bars", "Bars between entry signals", "General")
			
			.SetOptimize(4, 20, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle type", "Candle type", "General");

		_prices = new decimal[Length];
		_barsFromSignal = int.MaxValue;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prices = new decimal[Length];
		_index = 0;
		_prevCorrelation = 0m;
		_barsFromSignal = int.MaxValue;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var dummyEma1 = new ExponentialMovingAverage { Length = 10 };
		var dummyEma2 = new ExponentialMovingAverage { Length = 20 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(dummyEma1, dummyEma2, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal d1, decimal d2)
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
		_barsFromSignal++;

		if (_barsFromSignal >= CooldownBars)
		{
			if (_prevCorrelation <= EntryLevel && correlation > EntryLevel && Position <= 0)
			{
				BuyMarket();
				_barsFromSignal = 0;
			}
			else if (_prevCorrelation >= -EntryLevel && correlation < -EntryLevel && Position >= 0)
			{
				SellMarket();
				_barsFromSignal = 0;
			}
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

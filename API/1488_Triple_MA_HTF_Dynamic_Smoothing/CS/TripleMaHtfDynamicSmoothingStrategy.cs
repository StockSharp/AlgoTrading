using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Triple moving average trend following using EMA crossovers with RSI confirmation.
/// </summary>
public class TripleMaHtfDynamicSmoothingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length1;
	private readonly StrategyParam<int> _length2;
	private readonly StrategyParam<int> _length3;

	private decimal _prevMa1;
	private decimal _prevMa2;
	private decimal _prevRsi;
	private int _cooldown;

	public TripleMaHtfDynamicSmoothingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Base timeframe", "General");

		_length1 = Param(nameof(Length1), 10)
			.SetDisplay("MA1 Length", "Length for fast EMA", "Trend");

		_length2 = Param(nameof(Length2), 30)
			.SetDisplay("MA2 Length", "Length for slow EMA", "Trend");

		_length3 = Param(nameof(Length3), 14)
			.SetDisplay("RSI Length", "RSI period", "Trend");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int Length1
	{
		get => _length1.Value;
		set => _length1.Value = value;
	}

	public int Length2
	{
		get => _length2.Value;
		set => _length2.Value = value;
	}

	public int Length3
	{
		get => _length3.Value;
		set => _length3.Value = value;
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
		_prevMa1 = 0;
		_prevMa2 = 0;
		_prevRsi = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema1 = new ExponentialMovingAverage { Length = Length1 };
		var ema2 = new ExponentialMovingAverage { Length = Length2 };
		var rsi = new RelativeStrengthIndex { Length = Length3 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema1, ema2, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema1);
			DrawIndicator(area, ema2);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma1, decimal ma2, decimal rsiVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevMa1 == 0 || _prevMa2 == 0 || _prevRsi == 0)
		{
			_prevMa1 = ma1;
			_prevMa2 = ma2;
			_prevRsi = rsiVal;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevMa1 = ma1;
			_prevMa2 = ma2;
			_prevRsi = rsiVal;
			return;
		}

		var price = candle.ClosePrice;

		// EMA crossover
		var crossUp = _prevMa1 <= _prevMa2 && ma1 > ma2;
		var crossDown = _prevMa1 >= _prevMa2 && ma1 < ma2;

		// EMA trend direction
		var trendUp = ma1 > ma2;
		var trendDown = ma1 < ma2;

		// Exit on EMA cross in opposite direction
		if (Position > 0 && crossDown)
		{
			SellMarket();
			_cooldown = 30;
		}
		else if (Position < 0 && crossUp)
		{
			BuyMarket();
			_cooldown = 30;
		}

		// Entry: EMA crossover + RSI filter
		if (Position == 0)
		{
			if (crossUp && rsiVal > 45m && rsiVal < 75m)
			{
				BuyMarket();
				_cooldown = 30;
			}
			else if (crossDown && rsiVal > 25m && rsiVal < 55m)
			{
				SellMarket();
				_cooldown = 30;
			}
			// Re-entry: RSI cross 50 in trend direction when flat
			else if (trendUp && _prevRsi <= 50m && rsiVal > 50m)
			{
				BuyMarket();
				_cooldown = 30;
			}
			else if (trendDown && _prevRsi >= 50m && rsiVal < 50m)
			{
				SellMarket();
				_cooldown = 30;
			}
		}

		_prevMa1 = ma1;
		_prevMa2 = ma2;
		_prevRsi = rsiVal;
	}
}

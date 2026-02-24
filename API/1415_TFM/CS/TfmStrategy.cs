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
/// Timeframe multiplier breakout strategy.
/// </summary>
public class TfmStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _multiplier;
	private readonly StrategyParam<bool> _allowShort;

	private decimal _highLevel;
	private decimal _lowLevel;

	/// <summary>
	/// Base candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Higher timeframe multiplier.
	/// </summary>
	public int Multiplier { get => _multiplier.Value; set => _multiplier.Value = value; }

	/// <summary>
	/// Enable short trades.
	/// </summary>
	public bool AllowShort { get => _allowShort.Value; set => _allowShort.Value = value; }

	public TfmStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Base candle timeframe", "General");

		_multiplier = Param(nameof(Multiplier), 2)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "Higher timeframe multiplier", "Parameters");

		_allowShort = Param(nameof(AllowShort), true)
			.SetDisplay("Allow Short", "Enable short trades", "Parameters");
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
		_highLevel = 0m;
		_lowLevel = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Use a dummy SMA on base candles so Bind delivers candles
		var sma = new SimpleMovingAverage { Length = 10 };

		var baseSub = SubscribeCandles(CandleType);
		baseSub.Bind(sma, ProcessBase).Start();
	}

	private void ProcessBase(ICandleMessage candle, decimal smaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Update higher timeframe levels using rolling window approach
		// Use the close prices to track high/low over a window of Multiplier candles
		UpdateHighLowLevels(candle);

		if (_highLevel == 0 || _lowLevel == 0)
			return;

		if (candle.ClosePrice > _highLevel && Position <= 0)
		{
			BuyMarket();
		}
		else if (candle.ClosePrice < _lowLevel)
		{
			if (AllowShort && Position >= 0)
				SellMarket();
			else if (!AllowShort && Position > 0)
				SellMarket();
		}
	}

	private readonly List<(decimal high, decimal low)> _candleBuffer = new();

	private void UpdateHighLowLevels(ICandleMessage candle)
	{
		_candleBuffer.Add((candle.HighPrice, candle.LowPrice));

		// Keep a window of Multiplier candles to simulate the higher timeframe
		var windowSize = Multiplier;
		if (_candleBuffer.Count < windowSize)
			return;

		// When we have enough candles, compute high/low of the completed "higher tf" bar
		if (_candleBuffer.Count % windowSize == 0)
		{
			var start = _candleBuffer.Count - windowSize;
			var high = decimal.MinValue;
			var low = decimal.MaxValue;

			for (var i = start; i < _candleBuffer.Count; i++)
			{
				if (_candleBuffer[i].high > high) high = _candleBuffer[i].high;
				if (_candleBuffer[i].low < low) low = _candleBuffer[i].low;
			}

			_highLevel = high;
			_lowLevel = low;
		}
	}
}

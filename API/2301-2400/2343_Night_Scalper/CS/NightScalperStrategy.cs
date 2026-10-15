using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Night scalping strategy using Bollinger Bands.
/// </summary>
public class NightScalperStrategy : Strategy
{
	private const int BufferSize = 128;

	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<decimal> _rangeThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private readonly decimal[] _closes = new decimal[BufferSize];
	private int _closeIndex;
	private int _closeCount;

	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	public decimal RangeThreshold
	{
		get => _rangeThreshold.Value;
		set => _rangeThreshold.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public NightScalperStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetDisplay("BB Period", "Bollinger period", "Indicators");

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
			.SetDisplay("BB Deviation", "Bollinger deviation", "Indicators");

		_rangeThreshold = Param(nameof(RangeThreshold), 3000m)
			.SetDisplay("Range Threshold", "Maximum band width", "Risk");

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

		Array.Clear(_closes);
		_closeIndex = 0;
		_closeCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		Array.Clear(_closes);
		_closeIndex = 0;
		_closeCount = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

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

		PushClose(candle.ClosePrice);

		if (_closeCount < BollingerPeriod)
			return;

		var mean = GetAverage(BollingerPeriod);
		var deviation = GetStandardDeviation(BollingerPeriod, mean);
		var upper = mean + (deviation * BollingerDeviation);
		var lower = mean - (deviation * BollingerDeviation);
		var width = upper - lower;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position == 0 && width <= RangeThreshold)
		{
			if (candle.LowPrice <= lower)
				BuyMarket();
			else if (candle.HighPrice >= upper)
				SellMarket();
		}
		else if (Position > 0 && candle.ClosePrice >= mean)
		{
			SellMarket();
		}
		else if (Position < 0 && candle.ClosePrice <= mean)
		{
			BuyMarket();
		}
	}

	private void PushClose(decimal close)
	{
		_closes[_closeIndex] = close;
		_closeIndex = (_closeIndex + 1) % BufferSize;

		if (_closeCount < BufferSize)
			_closeCount++;
	}

	private decimal GetAverage(int period)
	{
		var count = Math.Min(period, _closeCount);
		var sum = 0m;

		for (var i = 0; i < count; i++)
		{
			var idx = (_closeIndex - 1 - i + BufferSize) % BufferSize;
			sum += _closes[idx];
		}

		return sum / count;
	}

	private decimal GetStandardDeviation(int period, decimal mean)
	{
		var count = Math.Min(period, _closeCount);
		var sum = 0m;

		for (var i = 0; i < count; i++)
		{
			var idx = (_closeIndex - 1 - i + BufferSize) % BufferSize;
			var diff = _closes[idx] - mean;
			sum += diff * diff;
		}

		return (decimal)Math.Sqrt((double)(sum / count));
	}
}

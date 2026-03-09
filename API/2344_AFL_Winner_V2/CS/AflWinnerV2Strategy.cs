using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the AFL Winner indicator approximation using a stochastic oscillator.
/// </summary>
public class AflWinnerV2Strategy : Strategy
{
	private const int BufferSize = 64;

	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<DataType> _candleType;

	private readonly decimal[] _highs = new decimal[BufferSize];
	private readonly decimal[] _lows = new decimal[BufferSize];
	private readonly decimal[] _rawK = new decimal[BufferSize];

	private int _priceIndex;
	private int _priceCount;
	private int _kIndex;
	private int _kCount;
	private int _prevColor;

	public int KPeriod { get => _kPeriod.Value; set => _kPeriod.Value = value; }
	public int DPeriod { get => _dPeriod.Value; set => _dPeriod.Value = value; }
	public decimal HighLevel { get => _highLevel.Value; set => _highLevel.Value = value; }
	public decimal LowLevel { get => _lowLevel.Value; set => _lowLevel.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public AflWinnerV2Strategy()
	{
		_kPeriod = Param<int>(nameof(KPeriod), 5).SetDisplay("%K Period", "%K Period", "General");
		_dPeriod = Param<int>(nameof(DPeriod), 3).SetDisplay("%D Period", "%D Period", "General");
		_highLevel = Param<decimal>(nameof(HighLevel), 40m).SetDisplay("High Level", "High Level", "General");
		_lowLevel = Param<decimal>(nameof(LowLevel), -40m).SetDisplay("Low Level", "Low Level", "General");
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

		Array.Clear(_highs);
		Array.Clear(_lows);
		Array.Clear(_rawK);
		_priceIndex = 0;
		_priceCount = 0;
		_kIndex = 0;
		_kCount = 0;
		_prevColor = -1;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		Array.Clear(_highs);
		Array.Clear(_lows);
		Array.Clear(_rawK);
		_priceIndex = 0;
		_priceCount = 0;
		_kIndex = 0;
		_kCount = 0;
		_prevColor = -1;

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

		PushPrice(candle.HighPrice, candle.LowPrice);

		if (_priceCount < KPeriod)
			return;

		var highest = GetHighest(KPeriod);
		var lowest = GetLowest(KPeriod);
		var range = highest - lowest;
		var rawK = range > 0 ? (candle.ClosePrice - lowest) / range * 100m : 50m;

		PushRawK(rawK);

		if (_kCount < DPeriod)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var k = rawK - 50m;
		var d = GetRawKAverage(DPeriod) - 50m;

		int color;

		if (k > d)
			color = (k > HighLevel || (k > LowLevel && d <= LowLevel)) ? 3 : 2;
		else
			color = (k < LowLevel || (d > HighLevel && k <= HighLevel)) ? 0 : 1;

		if (color == 3 && _prevColor != 3 && Position <= 0)
		{
			BuyMarket();
		}
		else if (color == 0 && _prevColor != 0 && Position >= 0)
		{
			SellMarket();
		}
		else if (color < 2 && Position > 0)
		{
			SellMarket();
		}
		else if (color > 1 && Position < 0)
		{
			BuyMarket();
		}

		_prevColor = color;
	}

	private void PushPrice(decimal high, decimal low)
	{
		_highs[_priceIndex] = high;
		_lows[_priceIndex] = low;
		_priceIndex = (_priceIndex + 1) % BufferSize;

		if (_priceCount < BufferSize)
			_priceCount++;
	}

	private void PushRawK(decimal value)
	{
		_rawK[_kIndex] = value;
		_kIndex = (_kIndex + 1) % BufferSize;

		if (_kCount < BufferSize)
			_kCount++;
	}

	private decimal GetHighest(int period)
	{
		var highest = decimal.MinValue;
		var count = Math.Min(period, _priceCount);

		for (var i = 0; i < count; i++)
		{
			var idx = (_priceIndex - 1 - i + BufferSize) % BufferSize;
			if (_highs[idx] > highest)
				highest = _highs[idx];
		}

		return highest;
	}

	private decimal GetLowest(int period)
	{
		var lowest = decimal.MaxValue;
		var count = Math.Min(period, _priceCount);

		for (var i = 0; i < count; i++)
		{
			var idx = (_priceIndex - 1 - i + BufferSize) % BufferSize;
			if (_lows[idx] < lowest)
				lowest = _lows[idx];
		}

		return lowest;
	}

	private decimal GetRawKAverage(int period)
	{
		var count = Math.Min(period, _kCount);
		var sum = 0m;

		for (var i = 0; i < count; i++)
		{
			var idx = (_kIndex - 1 - i + BufferSize) % BufferSize;
			sum += _rawK[idx];
		}

		return sum / count;
	}
}

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ZPF Volume Filter strategy.
/// Opens long positions when the ZPF indicator crosses above zero and
/// short positions when it crosses below.
/// </summary>
public class ZpfStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _volumeMa;
	private decimal _prevZpf;
	private bool _isFirst = true;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ZpfStrategy()
	{
		_length = Param(nameof(Length), 12)
			.SetRange(5, 50)
			.SetDisplay("Length", "Base moving average length", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_isFirst = true;
		_prevZpf = 0;

		var fastMa = new SimpleMovingAverage { Length = Length };
		var slowMa = new SimpleMovingAverage { Length = Length * 2 };
		_volumeMa = new SimpleMovingAverage { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastMa, slowMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var volResult = _volumeMa.Process(candle.TotalVolume, candle.OpenTime, true);
		if (!volResult.IsFormed)
			return;

		var volumeAvg = volResult.ToDecimal();

		// Calculate ZPF value
		var zpf = volumeAvg * (fast - slow) / 2m;

		if (_isFirst)
		{
			_prevZpf = zpf;
			_isFirst = false;
			return;
		}

		// Detect zero line cross
		if (_prevZpf <= 0 && zpf > 0 && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (_prevZpf >= 0 && zpf < 0 && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevZpf = zpf;
	}
}

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Cycle Biologique strategy.
/// The cycle is Amplitude * sin(2 * pi * (bar index + Offset) / CycleLength).
/// A cross above zero opens a long and a cross below zero closes it.
/// </summary>
public class CycleBiologiqueStrategy : Strategy
{
	private readonly StrategyParam<int> _cycleLength;
	private readonly StrategyParam<decimal> _amplitude;
	private readonly StrategyParam<int> _offset;
	private readonly StrategyParam<DataType> _candleType;

	private int _barIndex;
	private double? _prevCycle;

	/// <summary>
	/// Bars in one full cycle.
	/// </summary>
	public int CycleLength
	{
		get => _cycleLength.Value;
		set => _cycleLength.Value = value;
	}

	/// <summary>
	/// Cycle amplitude.
	/// </summary>
	public decimal Amplitude
	{
		get => _amplitude.Value;
		set => _amplitude.Value = value;
	}

	/// <summary>
	/// Phase shift of the cycle in bars.
	/// </summary>
	public int Offset
	{
		get => _offset.Value;
		set => _offset.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public CycleBiologiqueStrategy()
	{
		_cycleLength = Param(nameof(CycleLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Cycle Length", "Bars in one full cycle", "Cycle");

		_amplitude = Param(nameof(Amplitude), 1.0m)
			.SetGreaterThanZero()
			.SetDisplay("Amplitude", "Cycle amplitude", "Cycle");

		_offset = Param(nameof(Offset), 0)
			.SetDisplay("Offset", "Phase shift of the cycle in bars", "Cycle");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_barIndex = 0;
		_prevCycle = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_barIndex = 0;
		_prevCycle = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

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

		var cycle = (double)Amplitude * Math.Sin(2 * Math.PI * (_barIndex + Offset) / CycleLength);
		_barIndex++;

		var prev = _prevCycle;
		_prevCycle = cycle;

		if (prev is not double prevCycle)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (prevCycle <= 0 && cycle > 0 && Position == 0)
			BuyMarket();
		else if (prevCycle >= 0 && cycle < 0 && Position > 0)
			SellMarket(Position);
	}
}

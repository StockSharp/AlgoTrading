using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Cycle Biologique Strategy - trades based on a sinusoidal cycle crossing zero.
/// </summary>
public class CycleBiologiqueStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cycleLength;
	private readonly StrategyParam<decimal> _amplitude;
	private readonly StrategyParam<int> _offset;

	private int _barIndex;
	private decimal? _prevCycleValue;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of bars forming the cycle.
	/// </summary>
	public int CycleLength
	{
		get => _cycleLength.Value;
		set => _cycleLength.Value = value;
	}

	/// <summary>
	/// Amplitude of the cycle.
	/// </summary>
	public decimal Amplitude
	{
		get => _amplitude.Value;
		set => _amplitude.Value = value;
	}

	/// <summary>
	/// Offset applied to the cycle in bars.
	/// </summary>
	public int Offset
	{
		get => _offset.Value;
		set => _offset.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public CycleBiologiqueStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_cycleLength = Param(nameof(CycleLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Cycle Length", "Number of bars in the cycle", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 100, 5);

		_amplitude = Param(nameof(Amplitude), 1m)
			.SetDisplay("Amplitude", "Amplitude of the cycle", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);

		_offset = Param(nameof(Offset), 0)
			.SetDisplay("Offset", "Cycle offset in bars", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(-360, 360, 5);
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
		_prevCycleValue = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var angle = 2.0 * Math.PI * (_barIndex + Offset) / CycleLength;
		var cycleValue = Amplitude * (decimal)Math.Sin(angle);

		if (_prevCycleValue is decimal prev)
		{
			if (prev <= 0m && cycleValue > 0m && Position <= 0)
				BuyMarket();

			if (prev >= 0m && cycleValue < 0m && Position > 0)
				ClosePosition();
		}

		_prevCycleValue = cycleValue;
		_barIndex++;
	}
}

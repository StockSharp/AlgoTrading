using System;
using System.Collections.Generic;

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
	// Strategy parameters
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	// Indicators
	private SimpleMovingAverage _fastMa;
	private SimpleMovingAverage _slowMa;
	private SimpleMovingAverage _volumeMa;

	private decimal _prevZpf;

	/// <summary>
	/// Base moving average length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ZpfStrategy"/>.
	/// </summary>
	public ZpfStrategy()
	{
		_length = Param(nameof(Length), 12)
			.SetRange(5, 50)
			.SetDisplay("Length", "Base moving average length", "Indicators")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
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

		_prevZpf = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new() { Length = Length };
		_slowMa = new() { Length = Length * 2 };
		_volumeMa = new() { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastMa, _slowMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		// Process only finished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Update volume moving average
		var volumeAvg = _volumeMa.Process(candle.TotalVolume).ToDecimal();

		// Check readiness of indicators and trading state
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_volumeMa.IsFormed)
			return;

		// Calculate ZPF value
		var zpf = volumeAvg * (fast - slow) / 2m;

		// Determine order volume
		var volume = Volume + Math.Abs(Position);

		// Detect zero line cross and open positions
		if (_prevZpf <= 0 && zpf > 0 && Position <= 0)
			BuyMarket(volume);
		else if (_prevZpf >= 0 && zpf < 0 && Position >= 0)
			SellMarket(volume);

		// Exit positions on opposite signal
		if (_prevZpf > 0 && zpf <= 0 && Position > 0)
			SellMarket(Math.Abs(Position));
		else if (_prevZpf < 0 && zpf >= 0 && Position < 0)
			BuyMarket(Math.Abs(Position));

		_prevZpf = zpf;
	}
}

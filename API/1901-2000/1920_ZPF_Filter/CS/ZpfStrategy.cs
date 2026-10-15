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
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _fastMa;
	private SimpleMovingAverage _slowMa;
	private SimpleMovingAverage _volumeMa;
	private int _barsSinceTrade;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ZpfStrategy()
	{
		_length = Param(nameof(Length), 12)
			.SetRange(5, 50)
			.SetDisplay("Length", "Base moving average length", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 6)
			.SetGreaterThanZero()
			.SetDisplay("Cooldown Bars", "Bars between trades", "Trading");

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
		_fastMa = default;
		_slowMa = default;
		_volumeMa = default;
		_barsSinceTrade = CooldownBars;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_barsSinceTrade = CooldownBars;

		_fastMa = new SimpleMovingAverage { Length = Length };
		_slowMa = new SimpleMovingAverage { Length = Length * 2 };
		_volumeMa = new SimpleMovingAverage { Length = Length };

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
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barsSinceTrade++;

		var volResult = _volumeMa.Process(candle.TotalVolume, candle.OpenTime, true);
		if (!volResult.IsFormed)
			return;

		var volumeAvg = volResult.ToDecimal();

		// Calculate ZPF value
		var zpf = volumeAvg * (fast - slow) / 2m;

		// Detect zero line cross
		if (zpf > 0 && Position <= 0 && _barsSinceTrade >= CooldownBars)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
			_barsSinceTrade = 0;
		}
		else if (zpf < 0 && Position >= 0 && _barsSinceTrade >= CooldownBars)
		{
			if (Position > 0) SellMarket();
			SellMarket();
			_barsSinceTrade = 0;
		}
	}
}

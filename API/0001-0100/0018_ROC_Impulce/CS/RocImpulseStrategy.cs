using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Rate of Change / Momentum impulse.
/// Uses Momentum indicator crossing zero as signal for entries.
/// </summary>
public class RocImpulseStrategy : Strategy
{
	private readonly StrategyParam<int> _rocPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevMom;
	private bool _hasPrevValues;
	private int _cooldown;

	/// <summary>
	/// Momentum period.
	/// </summary>
	public int RocPeriod
	{
		get => _rocPeriod.Value;
		set => _rocPeriod.Value = value;
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
	/// Initializes a new instance of the <see cref="RocImpulseStrategy"/>.
	/// </summary>
	public RocImpulseStrategy()
	{
		_rocPeriod = Param(nameof(RocPeriod), 12)
			.SetDisplay("Momentum Period", "Period for Momentum calculation", "Indicators")
			.SetOptimize(8, 20, 4);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_prevMom = default;
		_hasPrevValues = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var momentum = new Momentum { Length = RocPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(momentum, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, momentum);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal momValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_hasPrevValues)
		{
			_hasPrevValues = true;
			_prevMom = momValue;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevMom = momValue;
			return;
		}

		// Momentum crosses above zero - buy signal
		if (_prevMom <= 0 && momValue > 0 && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_cooldown = 55;
		}
		// Momentum crosses below zero - sell signal
		else if (_prevMom >= 0 && momValue < 0 && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_cooldown = 55;
		}

		_prevMom = momValue;
	}
}

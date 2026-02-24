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
/// True Strength Index MACD crossover strategy.
/// Generates buy when TSI crosses above its signal line and sell on opposite cross.
/// </summary>
public class TsiMacdCrossoverStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevTsi;
	private decimal _prevSignal;
	private bool _initialized;

	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public TsiMacdCrossoverStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		var tsi = new TrueStrengthIndex();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(tsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, tsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue tsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!tsiValue.IsFinal)
			return;

		var tv = (ITrueStrengthIndexValue)tsiValue;
		if (tv.Tsi is not decimal tsi || tv.Signal is not decimal signal)
			return;

		if (!_initialized)
		{
			_prevTsi = tsi;
			_prevSignal = signal;
			_initialized = true;
			return;
		}

		var crossUp = _prevTsi <= _prevSignal && tsi > signal;
		var crossDown = _prevTsi >= _prevSignal && tsi < signal;

		if (crossUp && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		else if (crossDown && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevTsi = tsi;
		_prevSignal = signal;
	}
}

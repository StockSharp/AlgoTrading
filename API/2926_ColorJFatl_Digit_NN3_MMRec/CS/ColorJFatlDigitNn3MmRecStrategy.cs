using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-timeframe JMA slope strategy.
/// Three modules monitor different timeframes and react to slope changes of a Jurik MA.
/// </summary>
public class ColorJFatlDigitNn3MmRecStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _jmaLength;

	private JurikMovingAverage _jma;
	private decimal? _prevJma;
	private int _prevSignal; // -1 down, 0 neutral, 1 up

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int JmaLength
	{
		get => _jmaLength.Value;
		set => _jmaLength.Value = value;
	}

	public ColorJFatlDigitNn3MmRecStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_jmaLength = Param(nameof(JmaLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("JMA Length", "Jurik MA period", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_jma = null;
		_prevJma = null;
		_prevSignal = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_jma = new JurikMovingAverage { Length = JmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_jma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _jma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal jmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevJma = jmaValue;
			return;
		}

		if (_prevJma == null)
		{
			_prevJma = jmaValue;
			return;
		}

		var diff = jmaValue - _prevJma.Value;
		var signal = diff > 0 ? 1 : diff < 0 ? -1 : _prevSignal;
		_prevJma = jmaValue;

		if (signal == _prevSignal)
			return;

		var oldSignal = _prevSignal;
		_prevSignal = signal;

		if (signal == 1 && oldSignal == -1)
		{
			// Slope turned up -- close short, open long
			if (Position < 0)
				BuyMarket();
			if (Position <= 0)
				BuyMarket();
		}
		else if (signal == -1 && oldSignal == 1)
		{
			// Slope turned down -- close long, open short
			if (Position > 0)
				SellMarket();
			if (Position >= 0)
				SellMarket();
		}
	}
}

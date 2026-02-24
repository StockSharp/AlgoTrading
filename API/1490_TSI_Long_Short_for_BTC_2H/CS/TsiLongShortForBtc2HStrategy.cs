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
/// True Strength Index-inspired breakout strategy.
/// Uses RSI as momentum oscillator, tracks its rolling highest/lowest.
/// Opens long when RSI breaks above prior high, short when breaks below prior low.
/// </summary>
public class TsiLongShortForBtc2HStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _lookback;

	private readonly List<decimal> _rsiHistory = new();
	private decimal _prevRsi;
	private decimal _prevHigh;
	private decimal _prevLow;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int Lookback { get => _lookback.Value; set => _lookback.Value = value; }

	public TsiLongShortForBtc2HStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetDisplay("RSI Length", "RSI period", "Indicators");
		_lookback = Param(nameof(Lookback), 50)
			.SetDisplay("Lookback", "Bars for highest/lowest RSI", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_rsiHistory.Clear();
		_prevRsi = 0;
		_prevHigh = 0;
		_prevLow = 100;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_rsiHistory.Add(rsiVal);
		if (_rsiHistory.Count > Lookback)
			_rsiHistory.RemoveAt(0);

		if (_rsiHistory.Count < Lookback)
		{
			_prevRsi = rsiVal;
			return;
		}

		// Calculate rolling highest and lowest RSI
		var high = decimal.MinValue;
		var low = decimal.MaxValue;
		for (var i = 0; i < _rsiHistory.Count - 1; i++)
		{
			if (_rsiHistory[i] > high) high = _rsiHistory[i];
			if (_rsiHistory[i] < low) low = _rsiHistory[i];
		}

		// Breakout conditions
		var longCond = _prevRsi <= _prevHigh && rsiVal > high;
		var shortCond = _prevRsi >= _prevLow && rsiVal < low;

		if (longCond && Position <= 0)
		{
			BuyMarket();
		}
		else if (shortCond && Position >= 0)
		{
			SellMarket();
		}

		_prevRsi = rsiVal;
		_prevHigh = high;
		_prevLow = low;
	}
}

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
/// Strategy with EMA, RSI and MACD crossover filters with percent-based TP/SL.
/// </summary>
public class TpcXauusdStrategy : Strategy
{
	private readonly StrategyParam<int> _ema200Len;
	private readonly StrategyParam<int> _ema21Len;
	private readonly StrategyParam<int> _rsiLen;
	private readonly StrategyParam<decimal> _slPercent;
	private readonly StrategyParam<decimal> _tpPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevShortEma;
	private decimal _prevLongEma;
	private decimal _entryPrice;

	public int Ema200Length { get => _ema200Len.Value; set => _ema200Len.Value = value; }
	public int Ema21Length { get => _ema21Len.Value; set => _ema21Len.Value = value; }
	public int RsiLength { get => _rsiLen.Value; set => _rsiLen.Value = value; }
	public decimal SlPercent { get => _slPercent.Value; set => _slPercent.Value = value; }
	public decimal TpPercent { get => _tpPercent.Value; set => _tpPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TpcXauusdStrategy()
	{
		_ema200Len = Param(nameof(Ema200Length), 100);
		_ema21Len = Param(nameof(Ema21Length), 21);
		_rsiLen = Param(nameof(RsiLength), 14);
		_slPercent = Param(nameof(SlPercent), 0.5m);
		_tpPercent = Param(nameof(TpPercent), 0.75m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
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
		_prevShortEma = 0;
		_prevLongEma = 0;
		_entryPrice = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var emaLong = new ExponentialMovingAverage { Length = Ema200Length };
		var emaShort = new ExponentialMovingAverage { Length = Ema21Length };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(emaLong, emaShort, rsi, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaLong);
			DrawIndicator(area, emaShort);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaLongVal, decimal emaShortVal, decimal rsiVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		// Check TP/SL exits first
		if (Position > 0 && _entryPrice > 0)
		{
			var sl = _entryPrice * (1 - SlPercent / 100m);
			var tp = _entryPrice * (1 + TpPercent / 100m);
			if (candle.LowPrice <= sl || candle.HighPrice >= tp)
			{
				SellMarket();
				_entryPrice = 0;
				_prevShortEma = emaShortVal;
				_prevLongEma = emaLongVal;
				return;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			var sl = _entryPrice * (1 + SlPercent / 100m);
			var tp = _entryPrice * (1 - TpPercent / 100m);
			if (candle.HighPrice >= sl || candle.LowPrice <= tp)
			{
				BuyMarket();
				_entryPrice = 0;
				_prevShortEma = emaShortVal;
				_prevLongEma = emaLongVal;
				return;
			}
		}

		if (_prevShortEma == 0)
		{
			_prevShortEma = emaShortVal;
			_prevLongEma = emaLongVal;
			return;
		}

		// EMA crossover + trend + RSI filter
		var shortCrossAboveLong = _prevShortEma <= _prevLongEma && emaShortVal > emaLongVal;
		var shortCrossBelowLong = _prevShortEma >= _prevLongEma && emaShortVal < emaLongVal;

		var longCond = close > emaLongVal && shortCrossAboveLong && rsiVal > 50m;
		var shortCond = close < emaLongVal && shortCrossBelowLong && rsiVal < 50m;

		if (longCond && Position <= 0)
		{
			BuyMarket();
			_entryPrice = close;
		}
		else if (shortCond && Position >= 0)
		{
			SellMarket();
			_entryPrice = close;
		}

		_prevShortEma = emaShortVal;
		_prevLongEma = emaLongVal;
	}
}

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
/// TEMA OBOS strategy with ATR based targets.
/// Uses triple EMA cross with RSI-based overbought/oversold filter.
/// </summary>
public class TemaObosPakunStrategy : Strategy
{
	private readonly StrategyParam<int> _temaLength;
	private readonly StrategyParam<decimal> _tpMultiplier;
	private readonly StrategyParam<decimal> _slMultiplier;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _entryPrice;

	public int TemaLength { get => _temaLength.Value; set => _temaLength.Value = value; }
	public decimal TpMultiplier { get => _tpMultiplier.Value; set => _tpMultiplier.Value = value; }
	public decimal SlMultiplier { get => _slMultiplier.Value; set => _slMultiplier.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TemaObosPakunStrategy()
	{
		_temaLength = Param(nameof(TemaLength), 25);
		_tpMultiplier = Param(nameof(TpMultiplier), 5m);
		_slMultiplier = Param(nameof(SlMultiplier), 2m);
		_rsiLength = Param(nameof(RsiLength), 14);
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
		_prevFast = 0m;
		_prevSlow = 0m;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast = new TripleExponentialMovingAverage { Length = TemaLength };
		var slow = new TripleExponentialMovingAverage { Length = TemaLength * 2 };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var atr = new AverageTrueRange { Length = 14 };

		var sub = SubscribeCandles(CandleType);
		sub.Bind(fast, slow, rsi, atr, Process).Start();
	}

	private void Process(ICandleMessage candle, decimal fastVal, decimal slowVal, decimal rsiVal, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Check exits first
		if (Position > 0 && _entryPrice > 0)
		{
			var stop = _entryPrice - atrVal * SlMultiplier;
			var tp = _entryPrice + atrVal * TpMultiplier;
			if (candle.ClosePrice <= stop || candle.ClosePrice >= tp)
			{
				SellMarket();
				_entryPrice = 0m;
				_prevFast = fastVal;
				_prevSlow = slowVal;
				return;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			var stop = _entryPrice + atrVal * SlMultiplier;
			var tp = _entryPrice - atrVal * TpMultiplier;
			if (candle.ClosePrice >= stop || candle.ClosePrice <= tp)
			{
				BuyMarket();
				_entryPrice = 0m;
				_prevFast = fastVal;
				_prevSlow = slowVal;
				return;
			}
		}

		// Check entries when flat
		if (Position == 0 && _prevFast != 0 && _prevSlow != 0)
		{
			var longCond = _prevFast <= _prevSlow && fastVal > slowVal && rsiVal < 70;
			var shortCond = _prevFast >= _prevSlow && fastVal < slowVal && rsiVal > 30;

			if (longCond)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
			}
			else if (shortCond)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
			}
		}

		_prevFast = fastVal;
		_prevSlow = slowVal;
	}
}

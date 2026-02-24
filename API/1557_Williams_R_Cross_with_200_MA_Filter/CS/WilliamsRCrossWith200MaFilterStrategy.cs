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
/// Williams %R cross strategy with SMA filter.
/// Uses RSI as proxy for Williams %R. Enters on oversold/overbought crossover with SMA trend filter.
/// Exits on percent-based TP/SL.
/// </summary>
public class WilliamsRCrossWith200MaFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<decimal> _stopPct;
	private readonly StrategyParam<decimal> _tpPct;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi;
	private decimal _entryPrice;
	private decimal _stopDist;

	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }
	public decimal Oversold { get => _oversold.Value; set => _oversold.Value = value; }
	public decimal Overbought { get => _overbought.Value; set => _overbought.Value = value; }
	public decimal StopPct { get => _stopPct.Value; set => _stopPct.Value = value; }
	public decimal TpPct { get => _tpPct.Value; set => _tpPct.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public WilliamsRCrossWith200MaFilterStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "General");

		_maLength = Param(nameof(MaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "SMA filter period", "General");

		_oversold = Param(nameof(Oversold), 30m)
			.SetDisplay("Oversold", "Oversold level", "General");

		_overbought = Param(nameof(Overbought), 70m)
			.SetDisplay("Overbought", "Overbought level", "General");

		_stopPct = Param(nameof(StopPct), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("Stop %", "Stop loss percent", "Risk");

		_tpPct = Param(nameof(TpPct), 1m)
			.SetGreaterThanZero()
			.SetDisplay("TP %", "Take profit percent", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevRsi = 0;
		_entryPrice = 0;
		_stopDist = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var sma = new SimpleMovingAverage { Length = MaLength };

		_prevRsi = 0;
		_entryPrice = 0;
		_stopDist = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, sma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiVal, decimal smaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// TP/SL management
		if (Position > 0 && _entryPrice > 0 && _stopDist > 0)
		{
			if (candle.ClosePrice <= _entryPrice - _stopDist || candle.ClosePrice >= _entryPrice + _stopDist * (TpPct / StopPct))
			{
				SellMarket();
				_entryPrice = 0;
				_stopDist = 0;
			}
		}
		else if (Position < 0 && _entryPrice > 0 && _stopDist > 0)
		{
			if (candle.ClosePrice >= _entryPrice + _stopDist || candle.ClosePrice <= _entryPrice - _stopDist * (TpPct / StopPct))
			{
				BuyMarket();
				_entryPrice = 0;
				_stopDist = 0;
			}
		}

		if (_prevRsi == 0)
		{
			_prevRsi = rsiVal;
			return;
		}

		// Crossover signals with MA filter
		var enterLong = _prevRsi < Oversold && rsiVal >= Oversold && candle.ClosePrice > smaVal;
		var enterShort = _prevRsi > Overbought && rsiVal <= Overbought && candle.ClosePrice < smaVal;

		if (enterLong && Position <= 0)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
			_stopDist = candle.ClosePrice * StopPct / 100m;
		}
		else if (enterShort && Position >= 0)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
			_stopDist = candle.ClosePrice * StopPct / 100m;
		}

		_prevRsi = rsiVal;
	}
}

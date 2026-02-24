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
/// VoVix DEVMA strategy.
/// Uses fast/slow StdDev deviation crossover as volatility regime shift signal.
/// Enters on deviation crossover, exits on percent TP/SL.
/// </summary>
public class VoVixDevmaStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _stopPct;
	private readonly StrategyParam<decimal> _tpMult;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFastStd;
	private decimal _prevSlowStd;
	private decimal _entryPrice;
	private decimal _stopDist;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public decimal StopPct { get => _stopPct.Value; set => _stopPct.Value = value; }
	public decimal TpMult { get => _tpMult.Value; set => _tpMult.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VoVixDevmaStrategy()
	{
		_fastLength = Param(nameof(FastLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast StdDev period", "DEVMA");

		_slowLength = Param(nameof(SlowLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow StdDev period", "DEVMA");

		_stopPct = Param(nameof(StopPct), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Stop %", "Stop loss percent", "Risk");

		_tpMult = Param(nameof(TpMult), 2m)
			.SetGreaterThanZero()
			.SetDisplay("TP Mult", "Take profit as multiple of stop", "Risk");

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
		_prevFastStd = 0;
		_prevSlowStd = 0;
		_entryPrice = 0;
		_stopDist = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastStd = new StandardDeviation { Length = FastLength };
		var slowStd = new StandardDeviation { Length = SlowLength };
		var ema = new ExponentialMovingAverage { Length = FastLength };

		_prevFastStd = 0;
		_prevSlowStd = 0;
		_entryPrice = 0;
		_stopDist = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fastStd, slowStd, ema, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastStdVal, decimal slowStdVal, decimal emaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// TP/SL management
		if (Position > 0 && _entryPrice > 0 && _stopDist > 0)
		{
			if (candle.ClosePrice <= _entryPrice - _stopDist || candle.ClosePrice >= _entryPrice + _stopDist * TpMult)
			{
				SellMarket();
				_entryPrice = 0;
				_stopDist = 0;
			}
		}
		else if (Position < 0 && _entryPrice > 0 && _stopDist > 0)
		{
			if (candle.ClosePrice >= _entryPrice + _stopDist || candle.ClosePrice <= _entryPrice - _stopDist * TpMult)
			{
				BuyMarket();
				_entryPrice = 0;
				_stopDist = 0;
			}
		}

		if (_prevFastStd == 0 || _prevSlowStd == 0 || fastStdVal <= 0 || slowStdVal <= 0)
		{
			_prevFastStd = fastStdVal;
			_prevSlowStd = slowStdVal;
			return;
		}

		// Deviation crossover: fast vol crossing above slow vol = regime shift
		// Use price direction relative to EMA for trade direction
		var volExpanding = fastStdVal > slowStdVal;
		var wasContracting = _prevFastStd <= _prevSlowStd;
		var bullCross = wasContracting && volExpanding && candle.ClosePrice > emaVal;
		var bearCross = wasContracting && volExpanding && candle.ClosePrice < emaVal;

		if (bullCross && Position <= 0)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
			_stopDist = candle.ClosePrice * StopPct / 100m;
		}
		else if (bearCross && Position >= 0)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
			_stopDist = candle.ClosePrice * StopPct / 100m;
		}

		_prevFastStd = fastStdVal;
		_prevSlowStd = slowStdVal;
	}
}

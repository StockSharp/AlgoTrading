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
/// VIDYA-inspired ProTrend strategy with multi-tier take profit.
/// Uses fast/slow KAMA as VIDYA proxy with slope confirmation and percent-based TP tiers.
/// </summary>
public class VidyaProTrendMultiTierProfitStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _tp1Pct;
	private readonly StrategyParam<decimal> _tp2Pct;
	private readonly StrategyParam<decimal> _slPct;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _entryPrice;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public decimal Tp1Pct { get => _tp1Pct.Value; set => _tp1Pct.Value = value; }
	public decimal Tp2Pct { get => _tp2Pct.Value; set => _tp2Pct.Value = value; }
	public decimal SlPct { get => _slPct.Value; set => _slPct.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VidyaProTrendMultiTierProfitStrategy()
	{
		_fastLength = Param(nameof(FastLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast adaptive MA period", "General");

		_slowLength = Param(nameof(SlowLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow adaptive MA period", "General");

		_tp1Pct = Param(nameof(Tp1Pct), 1.0m)
			.SetGreaterThanZero()
			.SetDisplay("TP1 %", "First take profit percent", "Risk");

		_tp2Pct = Param(nameof(Tp2Pct), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("TP2 %", "Second take profit percent", "Risk");

		_slPct = Param(nameof(SlPct), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("SL %", "Stop loss percent", "Risk");

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
		_prevFast = 0;
		_prevSlow = 0;
		_entryPrice = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var kamaFast = new KaufmanAdaptiveMovingAverage { Length = FastLength };
		var kamaSlow = new KaufmanAdaptiveMovingAverage { Length = SlowLength };

		_prevFast = 0;
		_prevSlow = 0;
		_entryPrice = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(kamaFast, kamaSlow, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, kamaFast);
			DrawIndicator(area, kamaSlow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevFast == 0)
		{
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		// TP/SL management
		if (Position > 0 && _entryPrice > 0)
		{
			if (candle.ClosePrice >= _entryPrice * (1m + Tp1Pct / 100m) ||
				candle.ClosePrice <= _entryPrice * (1m - SlPct / 100m))
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			if (candle.ClosePrice <= _entryPrice * (1m - Tp1Pct / 100m) ||
				candle.ClosePrice >= _entryPrice * (1m + SlPct / 100m))
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		// Slope detection
		var fastSlope = fast - _prevFast;
		var slowSlope = slow - _prevSlow;

		// Entry: VIDYA crossover with slope confirmation
		var longCond = candle.ClosePrice > slow && fast > slow &&
			fastSlope > 0 && slowSlope > 0;
		var shortCond = candle.ClosePrice < slow && fast < slow &&
			fastSlope < 0 && slowSlope < 0;

		var exitLong = fastSlope < 0 && slowSlope < 0;
		var exitShort = fastSlope > 0 && slowSlope > 0;

		if (longCond && Position <= 0)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
		}
		else if (exitLong && Position > 0 && _entryPrice == 0)
		{
			SellMarket();
		}

		if (shortCond && Position >= 0)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
		}
		else if (exitShort && Position < 0 && _entryPrice == 0)
		{
			BuyMarket();
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}

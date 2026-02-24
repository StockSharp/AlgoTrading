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
/// Stealth strategy based on trend and engulfing patterns.
/// Uses EMA for trend, engulfing candles for entry, with percent TP/SL.
/// </summary>
public class Us30StealthStrategy : Strategy
{
	private readonly StrategyParam<int> _maLen;
	private readonly StrategyParam<decimal> _tpPct;
	private readonly StrategyParam<decimal> _slPct;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevOpen;
	private decimal _prevClose;
	private decimal _entryPrice;

	public int MaLen { get => _maLen.Value; set => _maLen.Value = value; }
	public decimal TpPct { get => _tpPct.Value; set => _tpPct.Value = value; }
	public decimal SlPct { get => _slPct.Value; set => _slPct.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Us30StealthStrategy()
	{
		_maLen = Param(nameof(MaLen), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average length", "Indicators");

		_tpPct = Param(nameof(TpPct), 1.0m)
			.SetGreaterThanZero()
			.SetDisplay("TP %", "Take profit percent", "Risk");

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
		_prevOpen = 0;
		_prevClose = 0;
		_entryPrice = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = MaLen };

		_prevOpen = 0;
		_prevClose = 0;
		_entryPrice = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevOpen == 0)
		{
			_prevOpen = candle.OpenPrice;
			_prevClose = candle.ClosePrice;
			return;
		}

		// TP/SL management
		if (Position > 0 && _entryPrice > 0)
		{
			if (candle.ClosePrice >= _entryPrice * (1m + TpPct / 100m) ||
				candle.ClosePrice <= _entryPrice * (1m - SlPct / 100m))
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			if (candle.ClosePrice <= _entryPrice * (1m - TpPct / 100m) ||
				candle.ClosePrice >= _entryPrice * (1m + SlPct / 100m))
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		// Trend detection
		var isUpTrend = candle.ClosePrice > emaVal;
		var isDownTrend = candle.ClosePrice < emaVal;

		// Engulfing patterns
		var bullEng = candle.ClosePrice > candle.OpenPrice &&
			_prevClose < _prevOpen &&
			candle.ClosePrice > _prevOpen &&
			candle.OpenPrice <= _prevClose;

		var bearEng = candle.ClosePrice < candle.OpenPrice &&
			_prevClose > _prevOpen &&
			candle.ClosePrice < _prevOpen &&
			candle.OpenPrice >= _prevClose;

		// Entry signals
		if (bullEng && isUpTrend && Position <= 0)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
		}
		else if (bearEng && isDownTrend && Position >= 0)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
		}

		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;
	}
}

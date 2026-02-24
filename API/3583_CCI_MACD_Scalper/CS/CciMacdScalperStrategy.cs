using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified from "CCI MACD Scalper" MetaTrader expert.
/// Uses CCI zero-line crossover with EMA trend filter for scalping entries.
/// </summary>
public class CciMacdScalperStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _cciPeriod;

	private ExponentialMovingAverage _ema;
	private CommodityChannelIndex _cci;
	private decimal? _prevCci;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	public CciMacdScalperStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for scalping", "General");

		_emaPeriod = Param(nameof(EmaPeriod), 34)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA trend filter period", "Indicators");

		_cciPeriod = Param(nameof(CciPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "CCI period for zero-line crosses", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevCci = null;
		_ema = new ExponentialMovingAverage { Length = EmaPeriod };
		_cci = new CommodityChannelIndex { Length = CciPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, _cci, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ema.IsFormed || !_cci.IsFormed)
		{
			_prevCci = cciValue;
			return;
		}

		if (_prevCci is null)
		{
			_prevCci = cciValue;
			return;
		}

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		var close = candle.ClosePrice;

		// CCI crosses above zero + price above EMA -> buy
		var cciCrossUp = _prevCci.Value <= 0 && cciValue > 0;
		// CCI crosses below zero + price below EMA -> sell
		var cciCrossDown = _prevCci.Value >= 0 && cciValue < 0;

		if (cciCrossUp && close > emaValue)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			if (Position <= 0)
				BuyMarket(volume);
		}
		else if (cciCrossDown && close < emaValue)
		{
			if (Position > 0)
				SellMarket(Position);

			if (Position >= 0)
				SellMarket(volume);
		}

		_prevCci = cciValue;
	}
}

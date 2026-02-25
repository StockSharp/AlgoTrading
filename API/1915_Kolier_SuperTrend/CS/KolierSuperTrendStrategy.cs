using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Kolier SuperTrend strategy based on ATR bands.
/// Enters long when price crosses above the SuperTrend line and short when crossing below.
/// </summary>
public class KolierSuperTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr;
	private bool _prevPriceAbove;
	private decimal _prevSupertrend;
	private bool _isInitialized;

	public int Period { get => _period.Value; set => _period.Value = value; }
	public decimal Multiplier { get => _multiplier.Value; set => _multiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public KolierSuperTrendStrategy()
	{
		_period = Param(nameof(Period), 10)
			.SetDisplay("ATR Period", "ATR period for SuperTrend", "Indicators")
			.SetOptimize(5, 20, 1);

		_multiplier = Param(nameof(Multiplier), 3.0m)
			.SetDisplay("ATR Multiplier", "ATR multiplier for SuperTrend", "Indicators")
			.SetOptimize(2.0m, 4.0m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevPriceAbove = false;
		_prevSupertrend = 0m;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_atr = new AverageTrueRange { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var atrResult = _atr.Process(candle);
		if (!atrResult.IsFormed)
			return;

		var atrValue = atrResult.ToDecimal();
		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		var upper = median + Multiplier * atrValue;
		var lower = median - Multiplier * atrValue;

		decimal supertrend;

		if (!_isInitialized)
		{
			supertrend = candle.ClosePrice > median ? lower : upper;
			_prevSupertrend = supertrend;
			_prevPriceAbove = candle.ClosePrice > supertrend;
			_isInitialized = true;
			return;
		}

		if (_prevSupertrend <= candle.HighPrice)
			supertrend = Math.Max(lower, _prevSupertrend);
		else if (_prevSupertrend >= candle.LowPrice)
			supertrend = Math.Min(upper, _prevSupertrend);
		else
			supertrend = candle.ClosePrice > _prevSupertrend ? lower : upper;

		var priceAbove = candle.ClosePrice > supertrend;
		var crossUp = priceAbove && !_prevPriceAbove;
		var crossDown = !priceAbove && _prevPriceAbove;

		if (crossUp && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (crossDown && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevSupertrend = supertrend;
		_prevPriceAbove = priceAbove;
	}
}

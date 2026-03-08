using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// News-style volatility breakout strategy.
/// Monitors ATR for volatility expansion and trades breakouts
/// when price moves beyond recent range.
/// </summary>
public class SimpleNewsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;

	private decimal _prevAtr;
	private decimal _prevHigh;
	private decimal _prevLow;
	private decimal _entryPrice;
	private bool _hasPrev;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	public SimpleNewsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "ATR period for volatility", "Parameters");

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.0m)
			.SetDisplay("ATR Multiplier", "Multiplier for breakout distance", "Parameters");
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
		_prevAtr = 0;
		_prevHigh = 0;
		_prevLow = 0;
		_entryPrice = 0;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevAtr = 0;
		_prevHigh = 0;
		_prevLow = 0;
		_entryPrice = 0;
		_hasPrev = false;

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrInd)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!atrInd.IsFormed)
			return;

		var atrValue = atrInd.ToDecimal();
		var price = candle.ClosePrice;

		// Exit logic
		if (Position > 0 && _entryPrice > 0)
		{
			if (price <= _entryPrice - atrValue * 2m || price >= _entryPrice + atrValue * 3m)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			if (price >= _entryPrice + atrValue * 2m || price <= _entryPrice - atrValue * 3m)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		if (!_hasPrev)
		{
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			_prevAtr = atrValue;
			_hasPrev = true;
			return;
		}

		// Entry: breakout above previous high or below previous low
		if (Position == 0)
		{
			var breakoutDist = atrValue * AtrMultiplier;
			if (price > _prevHigh + breakoutDist)
			{
				BuyMarket();
				_entryPrice = price;
			}
			else if (price < _prevLow - breakoutDist)
			{
				SellMarket();
				_entryPrice = price;
			}
		}

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_prevAtr = atrValue;
	}
}

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// OCO-style breakout strategy. Calculates dynamic buy-stop and sell-stop levels
/// from recent high/low and enters on breakout, with StdDev-based stop-loss and take-profit.
/// </summary>
public class OcoOrderStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _stdMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _recentHigh;
	private decimal _recentLow;
	private decimal _entryPrice;
	private int _barCount;

	public int LookbackPeriod { get => _lookbackPeriod.Value; set => _lookbackPeriod.Value = value; }
	public decimal StdMultiplier { get => _stdMultiplier.Value; set => _stdMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public OcoOrderStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Bars for high/low calculation", "General");

		_stdMultiplier = Param(nameof(StdMultiplier), 1.5m)
			.SetDisplay("StdDev Multiplier", "Multiplier for SL/TP distance", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_recentHigh = 0;
		_recentLow = decimal.MaxValue;
		_entryPrice = 0;
		_barCount = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var stdDev = new StandardDeviation { Length = LookbackPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(stdDev, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal stdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barCount++;

		// Track rolling high/low
		if (_barCount <= LookbackPeriod)
		{
			if (candle.HighPrice > _recentHigh)
				_recentHigh = candle.HighPrice;
			if (candle.LowPrice < _recentLow)
				_recentLow = candle.LowPrice;
			return;
		}

		if (stdValue <= 0)
			return;

		var close = candle.ClosePrice;
		var slDistance = stdValue * StdMultiplier;

		// Exit logic
		if (Position > 0)
		{
			if (close <= _entryPrice - slDistance || close >= _entryPrice + slDistance * 2)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			if (close >= _entryPrice + slDistance || close <= _entryPrice - slDistance * 2)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		// Entry: breakout above recent high or below recent low
		if (Position == 0)
		{
			if (close > _recentHigh)
			{
				BuyMarket();
				_entryPrice = close;
			}
			else if (close < _recentLow)
			{
				SellMarket();
				_entryPrice = close;
			}
		}

		// Update high/low
		if (candle.HighPrice > _recentHigh)
			_recentHigh = candle.HighPrice;
		if (candle.LowPrice < _recentLow)
			_recentLow = candle.LowPrice;
	}
}

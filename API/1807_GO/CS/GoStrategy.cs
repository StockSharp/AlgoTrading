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
/// GO-based trading strategy.
/// Calculates a composite GO value from EMA of OHLC prices and volume.
/// Opens long when GO rises above OpenLevel, and short when below -OpenLevel.
/// Positions are closed when GO crosses back inside closing levels.
/// </summary>
public class GoStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _openLevel;
	private readonly StrategyParam<decimal> _closeLevelDiff;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _emaHigh;
	private decimal _emaLow;
	private decimal _emaOpen;
	private decimal _emaClose;
	private int _count;
	private decimal _k;

	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public decimal OpenLevel { get => _openLevel.Value; set => _openLevel.Value = value; }
	public decimal CloseLevelDiff { get => _closeLevelDiff.Value; set => _closeLevelDiff.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public GoStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 174)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Length of EMA for price smoothing", "Parameters");

		_openLevel = Param(nameof(OpenLevel), 0m)
			.SetDisplay("Open Level", "GO level to open positions", "Parameters");

		_closeLevelDiff = Param(nameof(CloseLevelDiff), 0m)
			.SetDisplay("Close Level Diff", "Difference between open and close levels", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_count = 0;
		_k = 2m / (MaPeriod + 1m);

		// Use a dummy SMA just as a binding mechanism to get candle events
		var sma = new SimpleMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_count++;

		if (_count == 1)
		{
			_emaHigh = candle.HighPrice;
			_emaLow = candle.LowPrice;
			_emaOpen = candle.OpenPrice;
			_emaClose = candle.ClosePrice;
			return;
		}

		_emaHigh = candle.HighPrice * _k + _emaHigh * (1m - _k);
		_emaLow = candle.LowPrice * _k + _emaLow * (1m - _k);
		_emaOpen = candle.OpenPrice * _k + _emaOpen * (1m - _k);
		_emaClose = candle.ClosePrice * _k + _emaClose * (1m - _k);

		if (_count < MaPeriod)
			return;

		var vol = candle.TotalVolume == 0 ? 1m : candle.TotalVolume;
		var go = ((_emaClose - _emaOpen) + (_emaHigh - _emaOpen) + (_emaLow - _emaOpen) + (_emaClose - _emaLow) + (_emaClose - _emaHigh)) * vol;

		var closeBuy = go < (OpenLevel - CloseLevelDiff);
		var closeSell = go > -(OpenLevel - CloseLevelDiff);
		var openBuy = go > OpenLevel;
		var openSell = go < -OpenLevel;

		if (Position > 0 && closeBuy)
		{
			SellMarket();
		}
		else if (Position < 0 && closeSell)
		{
			BuyMarket();
		}
		else if (Position == 0)
		{
			if (openBuy && !openSell)
				BuyMarket();
			else if (openSell && !openBuy)
				SellMarket();
		}
	}
}

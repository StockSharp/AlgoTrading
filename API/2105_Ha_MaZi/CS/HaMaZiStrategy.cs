using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Heikin Ashi with moving average and ZigZag confirmation.
/// </summary>
public class HaMaZiStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _zigzagLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;

	private decimal _haOpenPrev;
	private decimal _haClosePrev;
	private decimal _lastZigzag;
	private decimal _lastZigzagHigh;
	private decimal _lastZigzagLow;
	private decimal _entryPrice;

	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	public int ZigzagLength
	{
		get => _zigzagLength.Value;
		set => _zigzagLength.Value = value;
	}
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	public HaMaZiStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 40)
		.SetDisplay("MA Period", "EMA period", "General")
		.SetCanOptimize(true);
		_zigzagLength = Param(nameof(ZigzagLength), 13)
		.SetDisplay("ZigZag Length", "Lookback for pivot search", "ZigZag")
		.SetCanOptimize(true);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
		_stopLoss = Param(nameof(StopLoss), 70m)
		.SetDisplay("Stop Loss", "Stop loss in price units", "Risk")
		.SetCanOptimize(true);
		_takeProfit = Param(nameof(TakeProfit), 200m)
		.SetDisplay("Take Profit", "Take profit in price units", "Risk")
		.SetCanOptimize(true);
	}
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_haOpenPrev = 0m;
		_haClosePrev = 0m;
		_lastZigzag = 0m;
		_lastZigzagHigh = 0m;
		_lastZigzagLow = 0m;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ema = new ExponentialMovingAverage { Length = MaPeriod };
		var highest = new Highest { Length = ZigzagLength };
		var lowest = new Lowest { Length = ZigzagLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ema, highest, lowest, ProcessCandle)
		.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		var haOpen = (_haOpenPrev == 0m && _haClosePrev == 0m) ? (candle.OpenPrice + candle.ClosePrice) / 2m : (_haOpenPrev + _haClosePrev) / 2m;
		var haBull = haClose > haOpen;
		var haBear = haClose < haOpen;

		if (candle.HighPrice >= highest && _lastZigzag != candle.HighPrice)
		{
			_lastZigzag = candle.HighPrice;
			_lastZigzagHigh = candle.HighPrice;
			_lastZigzagLow = 0m;
		}
		else if (candle.LowPrice <= lowest && _lastZigzag != candle.LowPrice)
		{
			_lastZigzag = candle.LowPrice;
			_lastZigzagLow = candle.LowPrice;
			_lastZigzagHigh = 0m;
		}

		if (_lastZigzag == _lastZigzagLow && haBull && candle.ClosePrice > ema && Position <= 0)
		{
			_entryPrice = candle.ClosePrice;
			BuyMarket();
		}
		else if (_lastZigzag == _lastZigzagHigh && haBear && candle.ClosePrice < ema && Position >= 0)
		{
			_entryPrice = candle.ClosePrice;
			SellMarket();
		}

		if (Position > 0)
		{
			var stop = _entryPrice - StopLoss;
			var target = _entryPrice + TakeProfit;
			if (candle.LowPrice <= stop || candle.HighPrice >= target)
			SellMarket();
		}
		else if (Position < 0)
		{
			var stop = _entryPrice + StopLoss;
			var target = _entryPrice - TakeProfit;
			if (candle.HighPrice >= stop || candle.LowPrice <= target)
			BuyMarket();
		}

		_haOpenPrev = haOpen;
		_haClosePrev = haClose;
	}
}

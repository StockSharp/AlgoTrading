using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Heikin Ashi with moving averages.
/// </summary>
public class ThreeCommasHaMaStrategy : Strategy
{
	private readonly StrategyParam<int> _maFast;
	private readonly StrategyParam<int> _maSlow;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _haOpenPrev;
	private decimal _haClosePrev;
	private decimal _stopPrice;

	public int MaFast
	{
		get => _maFast.Value;
		set => _maFast.Value = value;
	}

	public int MaSlow
	{
		get => _maSlow.Value;
		set => _maSlow.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ThreeCommasHaMaStrategy()
	{
		_maFast = Param(nameof(MaFast), 9)
			.SetDisplay("MA Fast", "Fast moving average period", "MA");
		_maSlow = Param(nameof(MaSlow), 18)
			.SetDisplay("MA Slow", "Slow moving average period", "MA");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_stopPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ma1 = new ExponentialMovingAverage { Length = MaFast };
		var ma2 = new ExponentialMovingAverage { Length = MaSlow };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ma1, ma2, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma1);
			DrawIndicator(area, ma2);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma1, decimal ma2)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		var haOpen = (_haOpenPrev == 0m && _haClosePrev == 0m) ? (candle.OpenPrice + candle.ClosePrice) / 2m : (_haOpenPrev + _haClosePrev) / 2m;
		var haBull = haClose > haOpen;
		var haBearPrev = _haClosePrev < _haOpenPrev;
		var haBullPrev = _haClosePrev > _haOpenPrev;

		if (haBearPrev && ma1 > ma2 && haBull && candle.ClosePrice > ma1 && Position <= 0)
		{
			_stopPrice = candle.LowPrice;
			BuyMarket();
		}
		else if (haBullPrev && ma1 < ma2 && !haBull && candle.ClosePrice < ma1 && Position >= 0)
		{
			_stopPrice = candle.HighPrice;
			SellMarket();
		}

		if (Position > 0 && (candle.ClosePrice < ma2 || candle.ClosePrice <= _stopPrice))
			SellMarket();
		else if (Position < 0 && (candle.ClosePrice > ma2 || candle.ClosePrice >= _stopPrice))
			BuyMarket();

		_haOpenPrev = haOpen;
		_haClosePrev = haClose;
	}
}

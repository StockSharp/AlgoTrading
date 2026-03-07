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
/// Simple Forecast - Keltner Worms Strategy - trades when price crosses dynamic Keltner Channel boundaries.
/// </summary>
public class SimpleForecastKeltnerWormsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _multiplier;

	private decimal _prevClose;
	private decimal _prevUpper;
	private decimal _prevLower;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Length { get => _length.Value; set => _length.Value = value; }
	public decimal Multiplier { get => _multiplier.Value; set => _multiplier.Value = value; }

	public SimpleForecastKeltnerWormsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for processing", "General");

		_length = Param(nameof(Length), 20)
			.SetDisplay("Length", "Channel calculation period", "Indicators");

		_multiplier = Param(nameof(Multiplier), 2m)
			.SetDisplay("Multiplier", "ATR multiplier for bands", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose = 0;
		_prevUpper = 0;
		_prevLower = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = Length };
		var atr = new AverageTrueRange { Length = Length };

		_prevClose = 0;
		_prevUpper = 0;
		_prevLower = 0;
		_hasPrev = false;

		var sub = SubscribeCandles(CandleType);
		sub.Bind(ema, atr, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaVal, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var upper = emaVal + Multiplier * atrVal;
		var lower = emaVal - Multiplier * atrVal;

		if (!_hasPrev)
		{
			_prevClose = candle.ClosePrice;
			_prevUpper = upper;
			_prevLower = lower;
			_hasPrev = true;
			return;
		}

		// Breakout above upper Keltner band
		if (_prevClose <= _prevUpper && candle.ClosePrice > upper && Position <= 0)
			BuyMarket();
		// Breakdown below lower Keltner band
		else if (_prevClose >= _prevLower && candle.ClosePrice < lower && Position >= 0)
			SellMarket();

		_prevClose = candle.ClosePrice;
		_prevUpper = upper;
		_prevLower = lower;
	}
}

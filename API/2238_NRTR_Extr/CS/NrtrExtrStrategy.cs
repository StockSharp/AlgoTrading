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
/// NRTR Extr strategy - trend following based on ATR-based trailing levels.
/// Opens long when trend turns up, short when trend turns down.
/// </summary>
public class NrtrExtrStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _price;
	private decimal _value;
	private int _trend;
	private int _trendPrev;
	private bool _initialized;

	public int Period { get => _period.Value; set => _period.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public NrtrExtrStrategy()
	{
		_period = Param(nameof(Period), 10)
			.SetGreaterThanZero()
			.SetDisplay("Period", "ATR period for NRTR", "Indicator")
			.SetOptimize(5, 20, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time frame", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_price = 0;
		_value = 0;
		_trend = 0;
		_trendPrev = 0;
		_initialized = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var atr = new AverageTrueRange { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!atrValue.IsFormed)
			return;

		var atr = atrValue.GetValue<decimal>();

		if (!_initialized)
		{
			_price = candle.ClosePrice;
			_value = candle.ClosePrice;
			_trend = 0;
			_trendPrev = 0;
			_initialized = true;
			return;
		}

		var dK = atr / Period;

		if (_trend >= 0)
		{
			_price = Math.Max(_price, candle.HighPrice);
			_value = Math.Max(_value, _price * (1m - dK));

			if (candle.HighPrice < _value)
			{
				_price = candle.HighPrice;
				_value = _price * (1m + dK);
				_trend = -1;
			}
		}
		else
		{
			_price = Math.Min(_price, candle.LowPrice);
			_value = Math.Min(_value, _price * (1m + dK));

			if (candle.LowPrice > _value)
			{
				_price = candle.LowPrice;
				_value = _price * (1m - dK);
				_trend = 1;
			}
		}

		var buySignal = _trendPrev < 0 && _trend > 0;
		var sellSignal = _trendPrev > 0 && _trend < 0;

		if (IsFormedAndOnlineAndAllowTrading())
		{
			if (buySignal && Position <= 0)
				BuyMarket();
			else if (sellSignal && Position >= 0)
				SellMarket();
		}

		_trendPrev = _trend;
	}
}

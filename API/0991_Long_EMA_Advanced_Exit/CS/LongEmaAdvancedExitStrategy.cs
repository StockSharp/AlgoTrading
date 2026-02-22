using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Long-only EMA strategy with MA cross entry and trailing exit.
/// </summary>
public class LongEmaAdvancedExitStrategy : Strategy
{
	private readonly StrategyParam<int> _shortPeriod;
	private readonly StrategyParam<int> _midPeriod;
	private readonly StrategyParam<int> _longPeriod;
	private readonly StrategyParam<decimal> _trailingPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevShort;
	private decimal _prevMid;
	private decimal _maxPrice;

	public int ShortPeriod { get => _shortPeriod.Value; set => _shortPeriod.Value = value; }
	public int MidPeriod { get => _midPeriod.Value; set => _midPeriod.Value = value; }
	public int LongPeriod { get => _longPeriod.Value; set => _longPeriod.Value = value; }
	public decimal TrailingPercent { get => _trailingPercent.Value; set => _trailingPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LongEmaAdvancedExitStrategy()
	{
		_shortPeriod = Param(nameof(ShortPeriod), 5)
			.SetDisplay("Short EMA", "Short EMA period", "Indicators");
		_midPeriod = Param(nameof(MidPeriod), 10)
			.SetDisplay("Mid EMA", "Mid EMA period", "Indicators");
		_longPeriod = Param(nameof(LongPeriod), 50)
			.SetDisplay("Long EMA", "Long EMA period", "Indicators");
		_trailingPercent = Param(nameof(TrailingPercent), 2m)
			.SetDisplay("Trail %", "Trailing stop percent", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevShort = 0;
		_prevMid = 0;
		_maxPrice = 0;

		var emaShort = new EMA { Length = ShortPeriod };
		var emaMid = new EMA { Length = MidPeriod };
		var emaLong = new EMA { Length = LongPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(emaShort, emaMid, emaLong, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaShort);
			DrawIndicator(area, emaMid);
			DrawIndicator(area, emaLong);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal shortVal, decimal midVal, decimal longVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevShort == 0 || _prevMid == 0)
		{
			_prevShort = shortVal;
			_prevMid = midVal;
			return;
		}

		// Entry: short crosses above mid, price above long EMA
		var crossUp = _prevShort <= _prevMid && shortVal > midVal;
		if (crossUp && candle.ClosePrice > longVal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_maxPrice = candle.HighPrice;
		}

		// Exit: MA cross down or trailing stop
		if (Position > 0)
		{
			_maxPrice = Math.Max(_maxPrice, candle.HighPrice);
			var trailStop = _maxPrice * (1m - TrailingPercent / 100m);

			var crossDown = _prevShort >= _prevMid && shortVal < midVal;
			if (crossDown || candle.ClosePrice <= trailStop)
			{
				SellMarket(Math.Abs(Position));
				_maxPrice = 0;
			}
		}

		_prevShort = shortVal;
		_prevMid = midVal;
	}
}

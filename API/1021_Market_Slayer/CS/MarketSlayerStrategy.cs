using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MarketSlayerStrategy : Strategy
{
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<int> _confirmationTrendValue;
	private readonly StrategyParam<DataType> _candleType;

	private WeightedMovingAverage _trendWmaHigh;
	private WeightedMovingAverage _trendWmaLow;
	private WeightedMovingAverage _shortWma;
	private WeightedMovingAverage _longWma;
	private int _trendHlv;
	private bool _isTrendBullish;
	private decimal? _prevShort;
	private decimal? _prevLong;

	public int ShortLength { get => _shortLength.Value; set => _shortLength.Value = value; }
	public int LongLength { get => _longLength.Value; set => _longLength.Value = value; }
	public int ConfirmationTrendValue { get => _confirmationTrendValue.Value; set => _confirmationTrendValue.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MarketSlayerStrategy()
	{
		_shortLength = Param(nameof(ShortLength), 10);
		_longLength = Param(nameof(LongLength), 20);
		_confirmationTrendValue = Param(nameof(ConfirmationTrendValue), 2);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_shortWma = new WeightedMovingAverage { Length = ShortLength };
		_longWma = new WeightedMovingAverage { Length = LongLength };
		_trendWmaHigh = new WeightedMovingAverage { Length = ConfirmationTrendValue };
		_trendWmaLow = new WeightedMovingAverage { Length = ConfirmationTrendValue };

		_prevShort = _prevLong = null;
		_trendHlv = 0;
		_isTrendBullish = false;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_shortWma, _longWma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal shortWma, decimal longWma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Update trend using candle high/low
		var t = candle.ServerTime;
		var highVal = _trendWmaHigh.Process(new DecimalIndicatorValue(_trendWmaHigh, candle.HighPrice, t));
		var lowVal = _trendWmaLow.Process(new DecimalIndicatorValue(_trendWmaLow, candle.LowPrice, t));

		if (_trendWmaHigh.IsFormed && _trendWmaLow.IsFormed)
		{
			var high = highVal.ToDecimal();
			var low = lowVal.ToDecimal();

			if (candle.ClosePrice > high)
				_trendHlv = 1;
			else if (candle.ClosePrice < low)
				_trendHlv = -1;

			var sslDown = _trendHlv < 0 ? high : low;
			var sslUp = _trendHlv < 0 ? low : high;
			_isTrendBullish = sslUp > sslDown;
		}

		if (!_shortWma.IsFormed || !_longWma.IsFormed)
		{
			_prevShort = shortWma;
			_prevLong = longWma;
			return;
		}

		if (_prevShort.HasValue && _prevLong.HasValue)
		{
			var crossUp = _prevShort <= _prevLong && shortWma > longWma;
			var crossDown = _prevShort >= _prevLong && shortWma < longWma;

			if (crossUp && _isTrendBullish && Position <= 0)
				BuyMarket();

			if (crossDown && !_isTrendBullish && Position >= 0)
				SellMarket();

			if (Position > 0 && !_isTrendBullish)
				SellMarket();

			if (Position < 0 && _isTrendBullish)
				BuyMarket();
		}

		_prevShort = shortWma;
		_prevLong = longWma;
	}
}

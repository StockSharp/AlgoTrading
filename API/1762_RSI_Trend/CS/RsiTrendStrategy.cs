using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI trend strategy with StdDev trailing stop.
/// </summary>
public class RsiTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiBuyLevel;
	private readonly StrategyParam<decimal> _rsiSellLevel;
	private readonly StrategyParam<int> _stdevPeriod;
	private readonly StrategyParam<decimal> _stdevMultiple;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _previousRsi;
	private bool _isRsiInitialized;
	private decimal _stopPrice;

	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public decimal RsiBuyLevel { get => _rsiBuyLevel.Value; set => _rsiBuyLevel.Value = value; }
	public decimal RsiSellLevel { get => _rsiSellLevel.Value; set => _rsiSellLevel.Value = value; }
	public int StdevPeriod { get => _stdevPeriod.Value; set => _stdevPeriod.Value = value; }
	public decimal StdevMultiple { get => _stdevMultiple.Value; set => _stdevMultiple.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RsiTrendStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Period for RSI calculation", "RSI Settings");

		_rsiBuyLevel = Param(nameof(RsiBuyLevel), 60m)
			.SetDisplay("RSI Buy Level", "Upper RSI barrier for long entries", "RSI Settings");

		_rsiSellLevel = Param(nameof(RsiSellLevel), 40m)
			.SetDisplay("RSI Sell Level", "Lower RSI barrier for short entries", "RSI Settings");

		_stdevPeriod = Param(nameof(StdevPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("StdDev Period", "StdDev period for trailing stop", "Settings");

		_stdevMultiple = Param(nameof(StdevMultiple), 2m)
			.SetGreaterThanZero()
			.SetDisplay("StdDev Multiple", "StdDev multiplier for trailing stop", "Settings");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for processing", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_previousRsi = 0;
		_isRsiInitialized = false;
		_stopPrice = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var stdev = new StandardDeviation { Length = StdevPeriod };

		SubscribeCandles(CandleType)
			.Bind(rsi, stdev, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal stdevValue)
	{
		if (candle.State != CandleStates.Finished) return;
		if (stdevValue <= 0) return;

		if (!_isRsiInitialized)
		{
			_previousRsi = rsiValue;
			_isRsiInitialized = true;
			return;
		}

		var bullish = rsiValue > RsiBuyLevel && _previousRsi <= RsiBuyLevel;
		var bearish = rsiValue < RsiSellLevel && _previousRsi >= RsiSellLevel;

		if (bullish && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
			_stopPrice = candle.ClosePrice - stdevValue * StdevMultiple;
		}
		else if (bearish && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
			_stopPrice = candle.ClosePrice + stdevValue * StdevMultiple;
		}

		if (Position > 0)
		{
			var newStop = candle.ClosePrice - stdevValue * StdevMultiple;
			if (newStop > _stopPrice) _stopPrice = newStop;
			if (candle.ClosePrice <= _stopPrice) SellMarket();
		}
		else if (Position < 0)
		{
			var newStop = candle.ClosePrice + stdevValue * StdevMultiple;
			if (newStop < _stopPrice) _stopPrice = newStop;
			if (candle.ClosePrice >= _stopPrice) BuyMarket();
		}

		_previousRsi = rsiValue;
	}
}

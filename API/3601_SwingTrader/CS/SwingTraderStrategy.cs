using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified from "SwingTrader" MetaTrader expert.
/// Uses Bollinger Band touches to detect swing direction, then enters on middle-band cross.
/// </summary>
public class SwingTraderStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerWidth;

	private BollingerBands _bollinger;
	private bool _upTouch;
	private bool _downTouch;
	private decimal? _prevClose;
	private decimal? _prevMiddle;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	public decimal BollingerWidth
	{
		get => _bollingerWidth.Value;
		set => _bollingerWidth.Value = value;
	}

	public SwingTraderStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for signals", "General");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Bands period", "Indicators");

		_bollingerWidth = Param(nameof(BollingerWidth), 2m)
			.SetGreaterThanZero()
			.SetDisplay("BB Width", "Bollinger Bands deviation", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_bollinger = new BollingerBands { Length = BollingerPeriod, Width = BollingerWidth };
		_upTouch = false;
		_downTouch = false;
		_prevClose = null;
		_prevMiddle = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_bollinger, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!bbValue.IsFinal)
			return;

		if (bbValue is not BollingerBandsValue bbVal)
			return;

		if (bbVal.UpBand is not decimal upper || bbVal.LowBand is not decimal lower || bbVal.MovingAverage is not decimal middle)
			return;

		if (!_bollinger.IsFormed)
		{
			_prevClose = candle.ClosePrice;
			_prevMiddle = middle;
			return;
		}

		var close = candle.ClosePrice;

		// Track Bollinger touches
		if (candle.HighPrice > upper)
		{
			_upTouch = true;
			_downTouch = false;
		}
		if (candle.LowPrice < lower)
		{
			_downTouch = true;
			_upTouch = false;
		}

		if (_prevClose is null || _prevMiddle is null)
		{
			_prevClose = close;
			_prevMiddle = middle;
			return;
		}

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		// Buy: had a lower band touch, now price crosses above middle
		var buySignal = _downTouch && _prevClose.Value < _prevMiddle.Value && close > middle;
		// Sell: had an upper band touch, now price crosses below middle
		var sellSignal = _upTouch && _prevClose.Value > _prevMiddle.Value && close < middle;

		if (buySignal)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			if (Position <= 0)
				BuyMarket(volume);

			_downTouch = false;
		}
		else if (sellSignal)
		{
			if (Position > 0)
				SellMarket(Position);

			if (Position >= 0)
				SellMarket(volume);

			_upTouch = false;
		}

		_prevClose = close;
		_prevMiddle = middle;
	}
}

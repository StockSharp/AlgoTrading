using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified from "Reverse" MetaTrader expert.
/// Uses Bollinger Band touches with RSI confirmation for mean-reversion entries.
/// Enters long when price crosses above lower band with RSI oversold,
/// enters short when price crosses below upper band with RSI overbought.
/// </summary>
public class ReverseStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerWidth;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;

	private BollingerBands _bollinger;
	private RelativeStrengthIndex _rsi;

	private decimal _prevClose;
	private decimal _prevRsi;
	private decimal _prevLower;
	private decimal _prevUpper;
	private bool _hasPrev;

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

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	public ReverseStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for signals", "General");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "MA length for Bollinger Bands", "Indicators");

		_bollingerWidth = Param(nameof(BollingerWidth), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Width", "Standard deviation multiplier", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "Upper threshold for short signals", "Signals");

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "Lower threshold for long signals", "Signals");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_bollinger = new BollingerBands { Length = BollingerPeriod, Width = BollingerWidth };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_hasPrev = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_rsi.IsFormed)
			return;

		var close = candle.ClosePrice;

		// Manually compute Bollinger Bands via indicator
		var bbInput = new CandleIndicatorValue(_bollinger, candle) { IsFinal = true };
		var bbResult = _bollinger.Process(bbInput);

		if (!_bollinger.IsFormed)
		{
			_prevClose = close;
			_prevRsi = rsiValue;
			return;
		}

		if (bbResult is not BollingerBandsValue bbVal)
		{
			_prevClose = close;
			_prevRsi = rsiValue;
			return;
		}

		if (bbVal.UpBand is not decimal upperBand || bbVal.LowBand is not decimal lowerBand)
		{
			_prevClose = close;
			_prevRsi = rsiValue;
			return;
		}

		if (!_hasPrev)
		{
			_prevClose = close;
			_prevRsi = rsiValue;
			_prevLower = lowerBand;
			_prevUpper = upperBand;
			_hasPrev = true;
			return;
		}

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		// Long: price crosses up from below lower band + RSI was oversold
		var longSignal = _prevClose < _prevLower && close >= lowerBand && _prevRsi < RsiOversold;
		// Short: price crosses down from above upper band + RSI was overbought
		var shortSignal = _prevClose > _prevUpper && close <= upperBand && _prevRsi > RsiOverbought;

		if (longSignal)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			if (Position <= 0)
				BuyMarket(volume);
		}
		else if (shortSignal)
		{
			if (Position > 0)
				SellMarket(Position);

			if (Position >= 0)
				SellMarket(volume);
		}

		// Exit long at upper band
		if (Position > 0 && close >= upperBand)
			SellMarket(Position);

		// Exit short at lower band
		if (Position < 0 && close <= lowerBand)
			BuyMarket(Math.Abs(Position));

		_prevClose = close;
		_prevRsi = rsiValue;
		_prevLower = lowerBand;
		_prevUpper = upperBand;
	}
}

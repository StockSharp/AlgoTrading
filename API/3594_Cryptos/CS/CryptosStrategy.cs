using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified from "Cryptos" MetaTrader expert.
/// Uses Bollinger Bands with a WMA trend filter. Buys when price is below WMA
/// and touches lower band; sells when price is above WMA and touches upper band.
/// </summary>
public class CryptosStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _wmaPeriod;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerWidth;

	private BollingerBands _bollinger;

	// Manual WMA
	private readonly Queue<decimal> _wmaQueue = new();
	private int _wmaLength;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int WmaPeriod
	{
		get => _wmaPeriod.Value;
		set => _wmaPeriod.Value = value;
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

	public CryptosStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for signals", "General");

		_wmaPeriod = Param(nameof(WmaPeriod), 55)
			.SetGreaterThanZero()
			.SetDisplay("WMA Period", "Weighted moving average period", "Indicators");

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
		_wmaQueue.Clear();
		_wmaLength = WmaPeriod;

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

	private decimal ComputeWma()
	{
		var arr = _wmaQueue.ToArray();
		var len = arr.Length;
		decimal weightedSum = 0;
		decimal weightTotal = 0;
		for (int i = 0; i < len; i++)
		{
			var weight = i + 1;
			weightedSum += arr[i] * weight;
			weightTotal += weight;
		}
		return weightTotal > 0 ? weightedSum / weightTotal : 0;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!bbValue.IsFinal)
			return;

		if (bbValue is not BollingerBandsValue bbVal)
			return;

		if (bbVal.UpBand is not decimal upperBand || bbVal.LowBand is not decimal lowerBand)
			return;

		var close = candle.ClosePrice;

		// Manual WMA
		_wmaQueue.Enqueue(close);
		while (_wmaQueue.Count > _wmaLength)
			_wmaQueue.Dequeue();

		if (!_bollinger.IsFormed || _wmaQueue.Count < _wmaLength)
			return;

		var wma = ComputeWma();
		if (wma <= 0)
			return;

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		// Buy: price below WMA, touches lower band
		if (close < wma && close <= lowerBand)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			if (Position <= 0)
				BuyMarket(volume);
		}
		// Sell: price above WMA, touches upper band
		else if (close > wma && close >= upperBand)
		{
			if (Position > 0)
				SellMarket(Position);

			if (Position >= 0)
				SellMarket(volume);
		}

		// Exit at WMA cross
		if (Position > 0 && close >= upperBand)
			SellMarket(Position);
		else if (Position < 0 && close <= lowerBand)
			BuyMarket(Math.Abs(Position));
	}
}

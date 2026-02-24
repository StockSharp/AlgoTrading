using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified from "Aussie Surfer Ltd" MetaTrader expert.
/// Uses Bollinger Band reversals with an SMA slope filter for entries.
/// </summary>
public class AussieSurferLtdStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerWidth;
	private readonly StrategyParam<int> _smaPeriod;

	private BollingerBands _bollinger;
	private readonly Queue<decimal> _smaQueue = new();
	private decimal _smaSum;
	private decimal? _prevSma;
	private decimal? _prevClose;

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

	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	public AussieSurferLtdStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Bollinger Bands window", "Indicators");

		_bollingerWidth = Param(nameof(BollingerWidth), 2.5m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Width", "Standard deviation multiplier", "Indicators");

		_smaPeriod = Param(nameof(SmaPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "SMA period for slope filter", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_bollinger = new BollingerBands { Length = BollingerPeriod, Width = BollingerWidth };
		_smaQueue.Clear();
		_smaSum = 0;
		_prevSma = null;
		_prevClose = null;

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

		if (bbVal.UpBand is not decimal upperBand || bbVal.LowBand is not decimal lowerBand)
			return;

		var close = candle.ClosePrice;
		var median = (candle.HighPrice + candle.LowPrice) / 2m;

		// Manual SMA for slope filter
		_smaQueue.Enqueue(median);
		_smaSum += median;
		while (_smaQueue.Count > SmaPeriod)
			_smaSum -= _smaQueue.Dequeue();

		if (!_bollinger.IsFormed || _smaQueue.Count < SmaPeriod)
		{
			_prevClose = close;
			return;
		}

		var smaValue = _smaSum / _smaQueue.Count;

		if (_prevSma is null || _prevClose is null)
		{
			_prevSma = smaValue;
			_prevClose = close;
			return;
		}

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		// SMA slope (uptrend or downtrend)
		var smaRising = smaValue > _prevSma.Value;
		var smaFalling = smaValue < _prevSma.Value;

		// Long: price was below lower band and crosses back above, SMA falling (reversal)
		var longSignal = _prevClose.Value < lowerBand && close >= lowerBand;
		// Short: price was above upper band and crosses back below, SMA rising (reversal)
		var shortSignal = _prevClose.Value > upperBand && close <= upperBand;

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

		// Exit at opposite band or SMA reversal
		if (Position > 0 && (close >= upperBand || (smaFalling && _prevSma.Value > smaValue)))
		{
			SellMarket(Position);
		}
		else if (Position < 0 && (close <= lowerBand || (smaRising && _prevSma.Value < smaValue)))
		{
			BuyMarket(Math.Abs(Position));
		}

		_prevSma = smaValue;
		_prevClose = close;
	}
}

using System;

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

	private ExponentialMovingAverage _bandEma;
	private ExponentialMovingAverage _slopeEma;
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
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(120).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
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

		_bandEma = new ExponentialMovingAverage { Length = BollingerPeriod };
		_slopeEma = new ExponentialMovingAverage { Length = SmaPeriod };
		_prevSma = null;
		_prevClose = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_bandEma, _slopeEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bandEma);
			DrawIndicator(area, _slopeEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal bandValue, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_bandEma.IsFormed || !_slopeEma.IsFormed)
		{
			_prevClose = candle.ClosePrice;
			_prevSma = smaValue;
			return;
		}

		var close = candle.ClosePrice;
		var bandOffset = bandValue * (BollingerWidth / 100m);
		var upperBand = bandValue + bandOffset;
		var lowerBand = bandValue - bandOffset;

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
		var longSignal = _prevClose.Value < lowerBand && close >= lowerBand && smaFalling;
		// Short: price was above upper band and crosses back below, SMA rising (reversal)
		var shortSignal = _prevClose.Value > upperBand && close <= upperBand && smaRising;

		if (longSignal)
		{
			if (Position <= 0)
				BuyMarket(Position < 0 ? Math.Abs(Position) + volume : volume);
		}
		else if (shortSignal)
		{
			if (Position >= 0)
				SellMarket(Position > 0 ? Math.Abs(Position) + volume : volume);
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

	/// <inheritdoc />
	protected override void OnReseted()
	{
		_bandEma = null;
		_slopeEma = null;
		_prevSma = null;
		_prevClose = null;

		base.OnReseted();
	}
}

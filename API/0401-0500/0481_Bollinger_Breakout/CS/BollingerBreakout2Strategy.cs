using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// 4H Bollinger Breakout strategy.
/// A long opens when the close crosses above the lower band with volume above its SMA and price above the trend SMA. A short opens
/// when the close crosses below the upper band with volume above its SMA, price below the trend SMA and RSI below 85. A long closes
/// when the close crosses above the upper band and a short when it crosses below the lower band.
/// </summary>
public class BollingerBreakout2Strategy : Strategy
{
	private const decimal _rsiShortLimit = 85m;

	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerMultiplier;
	private readonly StrategyParam<int> _volumeLength;
	private readonly StrategyParam<int> _trendLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<bool> _useLongSignals;
	private readonly StrategyParam<bool> _useShortSignals;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _volumeSma;
	private decimal? _prevClose;
	private decimal? _prevUpper;
	private decimal? _prevLower;

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerLength
	{
		get => _bollingerLength.Value;
		set => _bollingerLength.Value = value;
	}

	/// <summary>
	/// Bollinger Bands standard deviation multiplier.
	/// </summary>
	public decimal BollingerMultiplier
	{
		get => _bollingerMultiplier.Value;
		set => _bollingerMultiplier.Value = value;
	}

	/// <summary>
	/// Period of the volume SMA.
	/// </summary>
	public int VolumeLength
	{
		get => _volumeLength.Value;
		set => _volumeLength.Value = value;
	}

	/// <summary>
	/// Period of the trend SMA.
	/// </summary>
	public int TrendLength
	{
		get => _trendLength.Value;
		set => _trendLength.Value = value;
	}

	/// <summary>
	/// Period of RSI.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Allow long trades.
	/// </summary>
	public bool UseLongSignals
	{
		get => _useLongSignals.Value;
		set => _useLongSignals.Value = value;
	}

	/// <summary>
	/// Allow short trades.
	/// </summary>
	public bool UseShortSignals
	{
		get => _useShortSignals.Value;
		set => _useShortSignals.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public BollingerBreakout2Strategy()
	{
		_bollingerLength = Param(nameof(BollingerLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Length", "Bollinger Bands period", "Bollinger Bands");

		_bollingerMultiplier = Param(nameof(BollingerMultiplier), 1.8m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Multiplier", "Standard deviation multiplier", "Bollinger Bands");

		_volumeLength = Param(nameof(VolumeLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume Length", "Period of the volume SMA", "Filters");

		_trendLength = Param(nameof(TrendLength), 80)
			.SetGreaterThanZero()
			.SetDisplay("Trend Length", "Period of the trend SMA", "Filters");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "Period of RSI", "Filters");

		_useLongSignals = Param(nameof(UseLongSignals), true)
			.SetDisplay("Use Long Signals", "Allow long trades", "General");

		_useShortSignals = Param(nameof(UseShortSignals), true)
			.SetDisplay("Use Short Signals", "Allow short trades", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_volumeSma = null;
		_prevClose = null;
		_prevUpper = null;
		_prevLower = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = null;
		_prevUpper = null;
		_prevLower = null;

		var bollinger = new BollingerBands { Length = BollingerLength, Width = BollingerMultiplier };
		var trendSma = new SimpleMovingAverage { Length = TrendLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		_volumeSma = new SimpleMovingAverage { Length = VolumeLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, trendSma, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawIndicator(area, trendSma);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, rsi);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue trendValue, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var volumeAverage = _volumeSma.Process(candle.TotalVolume, candle.ServerTime, true).ToDecimal();

		if (!bollingerValue.IsFormed || bollingerValue is not BollingerBandsValue { UpBand: decimal upper, LowBand: decimal lower })
			return;

		var close = candle.ClosePrice;
		var prevClose = _prevClose;
		var prevUpper = _prevUpper;
		var prevLower = _prevLower;
		_prevClose = close;
		_prevUpper = upper;
		_prevLower = lower;

		if (!_volumeSma.IsFormed || !trendValue.IsFormed || !rsiValue.IsFormed)
			return;

		if (prevClose is not decimal pc || prevUpper is not decimal pu || prevLower is not decimal pl)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var trend = trendValue.ToDecimal();
		var rsi = rsiValue.ToDecimal();
		var volumeHigh = candle.TotalVolume > volumeAverage;

		var crossAboveLower = pc <= pl && close > lower;
		var crossBelowUpper = pc >= pu && close < upper;
		var crossAboveUpper = pc <= pu && close > upper;
		var crossBelowLower = pc >= pl && close < lower;

		var longSignal = UseLongSignals && crossAboveLower && volumeHigh && close > trend;
		var shortSignal = UseShortSignals && crossBelowUpper && volumeHigh && close < trend && rsi < _rsiShortLimit;

		if (longSignal && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortSignal && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && crossAboveUpper)
			SellMarket(Position);
		else if (Position < 0 && crossBelowLower)
			BuyMarket(-Position);
	}
}

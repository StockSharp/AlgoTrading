using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Adaptive SMI Ergodic strategy.
/// The True Strength Index (long smoothing LongLength, short smoothing ShortLength) is compared with its EMA signal line. A long opens
/// when TSI crosses above OversoldThreshold while above the signal line, a short when it crosses below OverboughtThreshold while below
/// the signal line; the opposite signal reverses the position. Thresholds are on the -1..1 ergodic scale.
/// </summary>
public class AdaptiveSmiErgodicStrategy : Strategy
{
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<decimal> _oversoldThreshold;
	private readonly StrategyParam<decimal> _overboughtThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevTsi;

	/// <summary>
	/// Long smoothing length of TSI.
	/// </summary>
	public int LongLength
	{
		get => _longLength.Value;
		set => _longLength.Value = value;
	}

	/// <summary>
	/// Short smoothing length of TSI.
	/// </summary>
	public int ShortLength
	{
		get => _shortLength.Value;
		set => _shortLength.Value = value;
	}

	/// <summary>
	/// EMA length of the signal line.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Oversold level on the -1..1 scale.
	/// </summary>
	public decimal OversoldThreshold
	{
		get => _oversoldThreshold.Value;
		set => _oversoldThreshold.Value = value;
	}

	/// <summary>
	/// Overbought level on the -1..1 scale.
	/// </summary>
	public decimal OverboughtThreshold
	{
		get => _overboughtThreshold.Value;
		set => _overboughtThreshold.Value = value;
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
	public AdaptiveSmiErgodicStrategy()
	{
		_longLength = Param(nameof(LongLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Long Length", "Long smoothing length of TSI", "TSI");

		_shortLength = Param(nameof(ShortLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Short Length", "Short smoothing length of TSI", "TSI");

		_signalLength = Param(nameof(SignalLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "EMA length of the signal line", "TSI");

		_oversoldThreshold = Param(nameof(OversoldThreshold), -0.4m)
			.SetDisplay("Oversold Threshold", "Oversold level on the -1..1 scale", "Levels");

		_overboughtThreshold = Param(nameof(OverboughtThreshold), 0.4m)
			.SetDisplay("Overbought Threshold", "Overbought level on the -1..1 scale", "Levels");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
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
		_prevTsi = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevTsi = null;

		var tsi = new TrueStrengthIndex
		{
			FirstLength = LongLength,
			SecondLength = ShortLength,
			SignalLength = SignalLength,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(tsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, tsi);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue tsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (tsiValue is not ITrueStrengthIndexValue { Tsi: decimal rawTsi, Signal: decimal rawSignal })
			return;

		// The indicator reports TSI in percent; the thresholds use the -1..1 ergodic scale.
		var tsi = rawTsi / 100m;
		var signal = rawSignal / 100m;

		var prevTsi = _prevTsi;
		_prevTsi = tsi;

		if (prevTsi is not decimal prev || !IsFormedAndOnlineAndAllowTrading())
			return;

		var crossAboveOversold = prev <= OversoldThreshold && tsi > OversoldThreshold;
		var crossBelowOverbought = prev >= OverboughtThreshold && tsi < OverboughtThreshold;

		if (crossAboveOversold && tsi > signal && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossBelowOverbought && tsi < signal && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}

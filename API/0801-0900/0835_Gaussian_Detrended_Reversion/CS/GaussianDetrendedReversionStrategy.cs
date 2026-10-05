using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Gaussian detrended reversion strategy.
/// The detrended price oscillator is the close minus the PriceLength EMA from PriceLength / 2 + 1 candles ago. It is smoothed with an
/// ALMA of SmoothingLength, and the lag line is that value LagLength candles ago. The smoothed oscillator crossing above the lag line
/// below zero goes long, crossing below it above zero goes short. A long closes on a cross below the lag line or above zero, a short
/// on a cross above the lag line or below zero.
/// </summary>
public class GaussianDetrendedReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _priceLength;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<int> _lagLength;
	private readonly StrategyParam<DataType> _candleType;

	private ArnaudLegouxMovingAverage _alma;
	private readonly List<decimal> _emaHistory = [];
	private readonly List<decimal> _smoothHistory = [];

	/// <summary>
	/// EMA length used to detrend the price.
	/// </summary>
	public int PriceLength
	{
		get => _priceLength.Value;
		set => _priceLength.Value = value;
	}

	/// <summary>
	/// ALMA smoothing length.
	/// </summary>
	public int SmoothingLength
	{
		get => _smoothingLength.Value;
		set => _smoothingLength.Value = value;
	}

	/// <summary>
	/// Candles back for the lag line.
	/// </summary>
	public int LagLength
	{
		get => _lagLength.Value;
		set => _lagLength.Value = value;
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
	public GaussianDetrendedReversionStrategy()
	{
		_priceLength = Param(nameof(PriceLength), 52)
			.SetGreaterThanZero()
			.SetDisplay("Price Length", "EMA length used to detrend the price", "Indicators");

		_smoothingLength = Param(nameof(SmoothingLength), 52)
			.SetGreaterThanZero()
			.SetDisplay("Smoothing Length", "ALMA smoothing length", "Indicators");

		_lagLength = Param(nameof(LagLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("Lag Length", "Candles back for the lag line", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_emaHistory.Clear();
		_smoothHistory.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_emaHistory.Clear();
		_smoothHistory.Clear();

		var ema = new ExponentialMovingAverage { Length = PriceLength };
		_alma = new ArnaudLegouxMovingAverage { Length = SmoothingLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var barsBack = PriceLength / 2 + 1;

		_emaHistory.Add(ema);
		if (_emaHistory.Count > barsBack + 1)
			_emaHistory.RemoveAt(0);

		if (_emaHistory.Count <= barsBack)
			return;

		var dpo = candle.ClosePrice - _emaHistory[0];
		var almaValue = _alma.Process(new DecimalIndicatorValue(_alma, dpo, candle.OpenTime) { IsFinal = true });

		if (!_alma.IsFormed)
			return;

		// The history keeps the current value, the previous one and the lag values for both.
		_smoothHistory.Add(almaValue.GetValue<decimal>());
		if (_smoothHistory.Count > LagLength + 2)
			_smoothHistory.RemoveAt(0);

		if (_smoothHistory.Count < LagLength + 2)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var count = _smoothHistory.Count;
		var smooth = _smoothHistory[count - 1];
		var prevSmooth = _smoothHistory[count - 2];
		var lag = _smoothHistory[count - 1 - LagLength];
		var prevLag = _smoothHistory[count - 2 - LagLength];

		var crossUp = prevSmooth <= prevLag && smooth > lag;
		var crossDown = prevSmooth >= prevLag && smooth < lag;
		var zeroUp = prevSmooth <= 0 && smooth > 0;
		var zeroDown = prevSmooth >= 0 && smooth < 0;

		if (crossUp && smooth < 0 && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossDown && smooth > 0 && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && (crossDown || zeroUp))
			SellMarket(Position);
		else if (Position < 0 && (crossUp || zeroDown))
			BuyMarket(Math.Abs(Position));
	}
}

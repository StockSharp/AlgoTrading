using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Gaussian detrended price oscillator with ALMA smoothing.
/// </summary>
public class GaussianDetrendedReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _priceLength;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<int> _lagLength;
	private readonly StrategyParam<DataType> _candleType;

	private readonly ExponentialMovingAverage _ema = new();
	private readonly ArnaudLegouxMovingAverage _alma = new() { Offset = 0.85m, Sigma = 6m };

	private readonly Queue<decimal> _emaValues = new();
	private readonly Queue<decimal> _almaValues = new();

	private decimal _prevAlma;
	private decimal _prevAlmaLag;
	private bool _isInitialized;

	public GaussianDetrendedReversionStrategy()
	{
		_priceLength = Param(nameof(PriceLength), 52)
			.SetGreaterThanZero()
			.SetDisplay("Price Length", "EMA length for DPO calculation", "Parameters");

		_smoothingLength = Param(nameof(SmoothingLength), 52)
			.SetGreaterThanZero()
			.SetDisplay("Smoothing Length", "ALMA smoothing length", "Parameters");

		_lagLength = Param(nameof(LagLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("Lag Length", "Lag for ALMA line", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for strategy calculation", "General");
	}

	/// <summary>
	/// EMA length used in detrended price oscillator.
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
	/// Lag length for ALMA line.
	/// </summary>
	public int LagLength
	{
		get => _lagLength.Value;
		set => _lagLength.Value = value;
	}

	/// <summary>
	/// Candle type for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_emaValues.Clear();
		_almaValues.Clear();
		_prevAlma = 0m;
		_prevAlmaLag = 0m;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema.Length = PriceLength;
		_alma.Length = SmoothingLength;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _alma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var barsBack = PriceLength / 2 + 1;

		_emaValues.Enqueue(emaValue);
		if (_emaValues.Count > barsBack + 1)
			_emaValues.Dequeue();

		if (_emaValues.Count <= barsBack)
			return;

		var emaLag = _emaValues.Peek();
		var dpo = candle.ClosePrice - emaLag;

		var almaValue = _alma.Process(new DecimalIndicatorValue(_alma, dpo)).ToDecimal();

		_almaValues.Enqueue(almaValue);
		if (_almaValues.Count > LagLength + 1)
			_almaValues.Dequeue();

		if (_almaValues.Count <= LagLength)
			return;

		var almaLag = _almaValues.Peek();

		if (!_isInitialized)
		{
			_prevAlma = almaValue;
			_prevAlmaLag = almaLag;
			_isInitialized = true;
			return;
		}

		var crossOverLag = _prevAlma < _prevAlmaLag && almaValue > almaLag;
		var crossUnderLag = _prevAlma > _prevAlmaLag && almaValue < almaLag;
		var crossOverZero = _prevAlma < 0 && almaValue > 0;
		var crossUnderZero = _prevAlma > 0 && almaValue < 0;

		var entryLong = crossOverLag && almaValue < 0;
		var exitLong = crossUnderLag || crossUnderZero;
		var entryShort = crossUnderLag && almaValue > 0;
		var exitShort = crossOverLag || crossOverZero;

		if (exitLong && Position > 0)
			SellMarket(Position);

		if (exitShort && Position < 0)
			BuyMarket(Math.Abs(Position));

		if (entryLong && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (entryShort && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevAlma = almaValue;
		_prevAlmaLag = almaLag;
	}
}


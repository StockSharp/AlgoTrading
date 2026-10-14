using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Linear Cross Trading strategy.
/// Regresses the close on volume over Length candles and predicts the price for the current volume. A long opens when the
/// predicted price crosses above its LinearLength WMA while MACD is above its signal and rising. A short opens when MACD is
/// below its signal and falling while the low is lower than the previous low. Opposite signals reverse the position.
/// </summary>
public class LinearCrossTradingStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _linearLength;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<(decimal close, decimal volume)> _window = new();
	private WeightedMovingAverage _wma;
	private decimal? _prevPredicted;
	private decimal? _prevWma;
	private decimal? _prevMacd;
	private decimal? _prevLow;

	/// <summary>
	/// Number of candles in the price-on-volume regression.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// WMA period applied to the predicted price.
	/// </summary>
	public int LinearLength
	{
		get => _linearLength.Value;
		set => _linearLength.Value = value;
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
	public LinearCrossTradingStrategy()
	{
		_length = Param(nameof(Length), 21)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Number of candles in the price-on-volume regression", "Indicators");

		_linearLength = Param(nameof(LinearLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Linear Length", "WMA period applied to the predicted price", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		ResetState();
	}

	private void ResetState()
	{
		_window.Clear();
		_prevPredicted = null;
		_prevWma = null;
		_prevMacd = null;
		_prevLow = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		_wma = new WeightedMovingAverage { Length = LinearLength };
		var macd = new MovingAverageConvergenceDivergenceSignal();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _wma);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, macd);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prevLow = _prevLow;
		_prevLow = candle.LowPrice;

		_window.Enqueue((candle.ClosePrice, candle.TotalVolume));
		while (_window.Count > Length)
			_window.Dequeue();

		decimal? predicted = null;
		decimal? wma = null;

		if (_window.Count == Length)
		{
			predicted = Predict(candle.TotalVolume);
			var wmaValue = _wma.Process(new DecimalIndicatorValue(_wma, predicted.Value, candle.OpenTime) { IsFinal = true });
			if (_wma.IsFormed)
				wma = wmaValue.GetValue<decimal>();
		}

		var prevPredicted = _prevPredicted;
		var prevWma = _prevWma;
		_prevPredicted = predicted;
		_prevWma = wma;

		if (!macdValue.IsFormed || macdValue is not IMovingAverageConvergenceDivergenceSignalValue { Macd: decimal macd, Signal: decimal signal })
			return;

		var prevMacd = _prevMacd;
		_prevMacd = macd;

		if (prevMacd is not decimal lastMacd || !IsFormedAndOnlineAndAllowTrading())
			return;

		var crossUp = predicted is decimal p && wma is decimal w && prevPredicted is decimal pp && prevWma is decimal pw && pp <= pw && p > w;
		var macdUp = macd > signal && macd > lastMacd;
		var macdDown = macd < signal && macd < lastMacd;
		var lowerLow = prevLow is decimal pl && candle.LowPrice < pl;

		if (crossUp && macdUp && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (macdDown && lowerLow && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}

	private decimal Predict(decimal volume)
	{
		// Least-squares fit close = a + b * volume over the window, evaluated at the current volume.
		var n = _window.Count;
		var meanVolume = _window.Sum(x => x.volume) / n;
		var meanClose = _window.Sum(x => x.close) / n;

		var covariance = 0m;
		var variance = 0m;
		foreach (var (close, vol) in _window)
		{
			var dv = vol - meanVolume;
			covariance += dv * (close - meanClose);
			variance += dv * dv;
		}

		var slope = variance == 0 ? 0 : covariance / variance;
		return meanClose + slope * (volume - meanVolume);
	}
}
